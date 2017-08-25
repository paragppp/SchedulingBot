using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using LanguageDetection;
using System.Configuration;
using Newtonsoft.Json.Linq;
using static System.Int32;

// required for the string extension below. reSharper is not detecting it but
// without the reference we get an error
using SchedulingBot.Extensions;
using System.Net.Http;
using SchedulingBot.Services;

namespace SchedulingBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<string>
    {
        //normalized inputs
        private Dictionary<string, string> _roomsDictionary;
        //Localization
        private string _detectedCulture;

        //Scheduling
        private string accessToken = "";
        private string accessToken_office = "";

        //For displaying current input table
        private string _displaySubject = "";
        private string _displayDuration = "";
        private string _displayEmail = "";
        private string _displaySchedule = "";

        private readonly IMeetingService _meetingService;
        private readonly ILoggingService _loggingService;
        private readonly IEmailService _emailService;
        private readonly IRoomService _roomService;
        private readonly IHttpService _httpService;

        public RootDialog(IMeetingService meetingService, IRoomService roomService, IEmailService emailService, IHttpService httpService, ILoggingService loggingService)
        {
            _meetingService = meetingService;
            _roomService = roomService;
            _emailService = emailService;
            _httpService = httpService;
            _loggingService = loggingService;
        }

        public void Reset(IDialogContext context)
        {
            context.PrivateConversationData.RemoveValue(Util.DataName.InvitationsEmailsStringArray);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeeintingSubjectString);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingDurationInt);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingInvitationsNumInt);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingSelectedDateDatetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingSelectedEndTimeDatetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.MeetingSelectedStartTimeDatetime);
            context.PrivateConversationData.RemoveValue(Util.DataName.UserEmailString);
            context.PrivateConversationData.RemoveValue(Util.DataName.UserNameString);
            _displaySubject = "";
            _displayDuration = "";
            _displayEmail = "";
            _displaySchedule = "";
        }

        public async Task StartAsync(IDialogContext context)
        {
            //await context.PostAsync("in new code");
            Reset(context);
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            _roomsDictionary = new Dictionary<string, string>();

            var rooms = _roomService.GetRooms();

            foreach (var room in rooms)
            {
                _roomsDictionary.Add(room.Address, room.Name);
            }

            var message = await item;

            var detector = new LanguageDetector();
            var defaultLanguage = ConfigurationManager.AppSettings["BotDefaultLanguage"];
            var localLanguage = ConfigurationManager.AppSettings["BotLocalLanguage"];

            detector.AddLanguages(defaultLanguage, localLanguage);
            
            // issue; when message.Text is in Japanese.Detect(message.Text)) will give null
            _detectedCulture = Equals(defaultLanguage, detector.Detect(message.Text)) ? ConfigurationManager.AppSettings["BotDefaultCulture"] : ConfigurationManager.AppSettings["BotLocalCulture"];

            SetCulture(_detectedCulture);

            accessToken = await GetAccessToken("graph");

            accessToken_office = await GetAccessToken("office");

            PromptDialog.Text(context, SubjectMessageReceivedAsync, Properties.Resources.Text_PleaseEnterSubject);
        }

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

        public async Task SubjectMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            SetCulture(_detectedCulture);
            var message = await argument;
            context.PrivateConversationData.SetValue(Util.DataName.MeeintingSubjectString, message);
            _displaySubject = message;
            await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayEmail, _displaySchedule));
            PromptDialog.Text(context, DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
        }

        public async Task DurationReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            SetCulture(_detectedCulture);
            var message = await argument;
            if (message.IsNaturalNumber())
            {
                var normalizedDuration = Parse(message);
                context.PrivateConversationData.SetValue(Util.DataName.MeetingDurationInt, normalizedDuration);
                _displayDuration = normalizedDuration.ToString();
                await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayEmail, _displaySchedule));
                PromptDialog.Text(context, EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertDuration);
                PromptDialog.Text(context, DurationReceivedAsync, Properties.Resources.Text_PleaseEnterDuration);
            }
        }

        public async Task EmailsMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            SetCulture(_detectedCulture);
            var message = await argument;
            var normalizedEmails = await _emailService.GetEmails(message, accessToken);
            if (normalizedEmails.Count > 0)
            {
              context.PrivateConversationData.SetValue(Util.DataName.InvitationsEmailsStringArray, normalizedEmails);
              var stringBuilder = new System.Text.StringBuilder();
              foreach (var i in normalizedEmails)
              {
                  stringBuilder.Append($"{i}<br>");
              }
              _displayEmail = stringBuilder.ToString();
              await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayEmail, _displaySchedule));
              PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen); 
            }
            else
            {
                await context.PostAsync(Properties.Resources.Text_AlertEmailAddresses);
                PromptDialog.Text(context, EmailsMessageReceivedAsync, Properties.Resources.Text_PleaseEnterEmailAddresses);
            }
        }

        public async Task DateMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            SetCulture(_detectedCulture);
            var message = await argument;
            DateTime.TryParse(message, out DateTime dateTime);
            if (dateTime != DateTime.MinValue && dateTime != DateTime.MaxValue)
            {
                context.PrivateConversationData.SetValue(Util.DataName.MeetingSelectedDateDatetime, dateTime);                
                await context.PostAsync($"{Properties.Resources.Text_CheckWhen1} {message} {Properties.Resources.Text_CheckWhen2}");
                await GetMeetingSuggestions(context);
            }
            else
            {
                PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
        }

        public async Task ScheduleMessageReceivedAsync(IDialogContext context, IAwaitable<MeetingSchedule> argument)
        {
            SetCulture(_detectedCulture);
            var date = await argument;
            context.PrivateConversationData.SetValue(Util.DataName.MeetingSelectedStartTimeDatetime, date.StartTime);
            context.PrivateConversationData.SetValue(Util.DataName.MeetingSelectedEndTimeDatetime, date.EndTime);           
            PromptDialog.Choice(context, ConfirmationAsync, date.Rooms, Properties.Resources.Text_PleaseSelectRoom, null, 3, PromptStyle.PerLine);
        }

        private async Task GetMeetingSuggestions(IDialogContext context)
        {
            SetCulture(_detectedCulture);
            var savedDuration = context.PrivateConversationData.GetValue<int>(Util.DataName.MeetingDurationInt);
            var savedEmails = context.PrivateConversationData.GetValue<string[]>(Util.DataName.InvitationsEmailsStringArray);
            var savedDate = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedDateDatetime);

            var userFindMeetingTimesRequestBody = Util.DataConverter.GetUserFindMeetingTimesRequestBody(savedDate, savedEmails, savedDuration);
            var meetingTimeSuggestion = await _meetingService.GetMeetingsTimeSuggestions(accessToken, userFindMeetingTimesRequestBody);
            var meetingScheduleSuggestions = new List<MeetingSchedule>();
            var counter = 1;
            foreach (var suggestion in meetingTimeSuggestion.MeetingTimeSuggestions)
            {
                DateTime.TryParse(suggestion.MeetingTimeSlot.Start.DateTime, out DateTime startTime);
                DateTime.TryParse(suggestion.MeetingTimeSlot.End.DateTime, out DateTime endTime);

                meetingScheduleSuggestions.Add(new MeetingSchedule()
                                    {
                                        StartTime = startTime,
                                        EndTime = endTime,
                                        Time = Util.DataConverter.GetFormatedTime(startTime, endTime, counter++),
                                        Rooms = Util.DataConverter.GetMeetingSuggestionRooms(suggestion, _roomsDictionary)
                                    });
            }
            if (meetingScheduleSuggestions.Count != 0)
            {
                PromptDialog.Choice(context, ScheduleMessageReceivedAsync, meetingScheduleSuggestions, Properties.Resources.Text_PleaseSelectSchedule, null, 3, PromptStyle.PerLine);
            }
            else
            {
                await context.PostAsync($"{Properties.Resources.Text_AlertNoAvailableTime}");
                PromptDialog.Text(context, DateMessageReceivedAsync, Properties.Resources.Text_PleaseEnterWhen);
            }
        }

        public async Task ConfirmationAsync(IDialogContext context, IAwaitable<Room> message)
        {
            SetCulture(_detectedCulture);

            try
            {
                var selectedRoom = await message;
                context.PrivateConversationData.SetValue(Util.DataName.MeetingSelectedRoomRoom, selectedRoom);
                var savedStartTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedStartTimeDatetime);
                var savedEndTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedEndTimeDatetime);
                _displaySchedule = $"{Util.DataConverter.GetFormatedTime(savedStartTime, savedEndTime, null)}<br>{selectedRoom.Name}";
                await context.PostAsync(Util.DataConverter.GetScheduleTicket(_displaySubject, _displayDuration, _displayEmail, _displaySchedule));
                PromptDialog.Confirm(context, ScheduleMeetingAsync, Properties.Resources.Text_FinalConfirmation, null, 3, PromptStyle.AutoText);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                throw;
            }
        }

        public async Task ScheduleMeetingAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            SetCulture(_detectedCulture);

            try
            {
                var answer = await argument;
                if (answer)
                {
                    try
                    {
                        var selectedRoom = context.PrivateConversationData.GetValue<Room>(Util.DataName.MeetingSelectedRoomRoom);
                        var savedSubject = context.PrivateConversationData.GetValue<string>(Util.DataName.MeeintingSubjectString);
                        var savedEmails = context.PrivateConversationData.GetValue<string[]>(Util.DataName.InvitationsEmailsStringArray);
                        var savedStartTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedStartTimeDatetime);
                        var savedEndTime = context.PrivateConversationData.GetValue<DateTime>(Util.DataName.MeetingSelectedEndTimeDatetime);

                        var meeting = Util.DataConverter.GetEvent(selectedRoom, savedEmails, savedSubject, savedStartTime, savedEndTime);

                        await _meetingService.ScheduleMeeting(accessToken_office, meeting);

                        await context.PostAsync($"{Properties.Resources.Text_Confirmation1} '{savedSubject}' {Properties.Resources.Text_Confirmation2} {Util.DataConverter.GetFormatedTime(savedStartTime, savedEndTime, null)}({selectedRoom.Name}) {Properties.Resources.Text_Confirmation3} {String.Join(",", savedEmails)} {Properties.Resources.Text_Confirmation4}");
                        Reset(context);
                    }
                    catch (Exception ex)
                    {
                        _loggingService.Error(ex);
                        throw;
                    }
                }
                else
                {
                    await context.PostAsync(Properties.Resources.Text_Canceled);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Setting culture for a specified method
        /// </summary>
        /// <param name="cultureValue"></param>
        private static void SetCulture(string cultureValue)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(cultureValue);
        }
    }
}