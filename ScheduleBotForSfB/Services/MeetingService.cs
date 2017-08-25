using System;
using Microsoft.Graph;
using Office = Microsoft.Office365.OutlookServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Configuration;
using Newtonsoft.Json.Linq;

namespace SchedulingBot.Services
{
    /// <summary>
    /// Service responsible for scheduling meetings 
    /// </summary>
    [Serializable]
    public class MeetingService : IMeetingService
    {
        private const string FindsMeetingTimeEndpoint = "https://graph.microsoft.com/v1.0/me/findMeetingTimes";
        //private const string ScheduleMeetingEndpoint = "https://graph.microsoft.com/v1.0/me/events";
        private const string ScheduleMeetingEndpoint = "https://outlook.office.com/api/v2.0/me/events";
        private readonly IRoomService _roomService;
        private readonly IHttpService _httpService;
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Meeting Service Constructor
        /// </summary>
        /// <param name="httpService">HTTP Service instance</param>
        /// <param name="roomService">Room Service instance</param>
        public MeetingService(IHttpService httpService, IRoomService roomService, ILoggingService loggingService)
        {
            _roomService = roomService;
            _httpService = httpService;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Provides meeting times suggestions
        /// </summary>
        /// <param name="accessToken">Access Token for API</param>
        /// <param name="userFindMeetingTimesRequestBody">Request object for calling Find Meeting Times API</param>
        /// <returns>Task of <see cref="MeetingTimeSuggestionsResult"/></returns>
        public async Task<MeetingTimeSuggestionsResult> GetMeetingsTimeSuggestions(string accessToken, UserFindMeetingTimesRequestBody userFindMeetingTimesRequestBody)
        {
            try
            {
                var rooms = _roomService.GetRooms();
                _roomService.AddRooms(userFindMeetingTimesRequestBody, rooms);
                var httpResponseMessage = await _httpService.AuthenticatedPost(FindsMeetingTimeEndpoint, accessToken, userFindMeetingTimesRequestBody, string.Empty);
                var meetingTimeSuggestionsResult = JsonConvert.DeserializeObject<MeetingTimeSuggestionsResult>(await httpResponseMessage.Content.ReadAsStringAsync());
                return meetingTimeSuggestionsResult;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Schedules meeting
        /// </summary>
        /// <param name="accessToken">Access Token for API</param>
        /// <param name="meeting">Meeting object containing all required data for scheduling meeting</param>
        /// <returns>Task of <see cref="Event"/></returns>
        public async Task<Office.Event> ScheduleMeeting(string accessToken, Office.Event meeting)
        {
            try
            {
                Office.Recipient organizer = new Office.Recipient();
                Office.EmailAddress organizerEmail = new Office.EmailAddress();

                organizerEmail.Address = "sample@tenant.onmicrosoft.com"; // organizer email address
                organizerEmail.Name = "Display Name"; // organizer display name

                organizer.EmailAddress = organizerEmail;
                
                meeting.IsOrganizer = false;
                meeting.Organizer = organizer;

                Office.OutlookServicesClient sc = new Office.OutlookServicesClient(new Uri("https://outlook.office.com/api/v2.0/me/events"), () => GetAccessToken("office"));

                await sc.Me.Events.AddEventAsync(meeting);

                //var httpResponseMessage = await _httpService.AuthenticatedPost(ScheduleMeetingEndpoint, accessToken, meeting, "UTC");
                //var scheduledMeeting = JsonConvert.DeserializeObject<Office.Event>(await httpResponseMessage.Content.ReadAsStringAsync());

                return null;// scheduledMeeting;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                throw;
            }

            
        }

        // this method has been repeated here for quick testing, can do into Utils/etc in future and shared with RootDialog
        private async Task<string> GetAccessToken(string resource)
        {
            string tokenEndpointUri = "https://login.windows.net/common/oauth2/token";

            var content = new FormUrlEncodedContent(new[]
                {
                        new KeyValuePair<string, string>("grant_type", "password"),
                        new KeyValuePair<string, string>("username", ConfigurationManager.AppSettings["BotUserName"]),
                        new KeyValuePair<string, string>("password", ConfigurationManager.AppSettings["BotUserPass"]),
                        new KeyValuePair<string, string>("client_id", ConfigurationManager.AppSettings["AADAppID"]),
                        new KeyValuePair<string, string>("client_secret", ConfigurationManager.AppSettings["AADAppKey"]),
                        new KeyValuePair<string, string>("resource", resource == "graph" ? "https://graph.microsoft.com" : "https://outlook.office.com")
                    }
            );

            using (var client = new HttpClient())
            {
                HttpResponseMessage res = await client.PostAsync(tokenEndpointUri, content);

                string json = await res.Content.ReadAsStringAsync();

                if (json.Contains("access_token"))
                {
                    JObject jsonObject = JObject.Parse(json);

                    string token = jsonObject["access_token"].ToString();

                    return token;
                }
                else
                    throw new Exception("Access Token Error");
            }
        }
    }
}