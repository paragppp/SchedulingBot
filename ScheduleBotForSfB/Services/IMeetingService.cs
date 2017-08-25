using System.Threading.Tasks;
using Office=Microsoft.Office365.OutlookServices;
using Microsoft.Graph;

namespace SchedulingBot.Services
{
    /// <summary>
    /// Meeting Service 
    /// </summary>
    public interface IMeetingService
    {
        /// <summary>
        /// Get meeting time suggestions
        /// </summary>
        /// <param name="accessToken">Access token for underlying API</param>
        /// <param name="userFindMeetingTimesRequestBody">Request object</param>
        /// <returns>Task of <see cref="MeetingTimeSuggestionsResult"/></returns>
        Task<MeetingTimeSuggestionsResult> GetMeetingsTimeSuggestions(string accessToken, UserFindMeetingTimesRequestBody userFindMeetingTimesRequestBody);

        /// <summary>
        /// Schedule a meeting
        /// </summary>
        /// <param name="accessToken">Access token for underlying API</param>
        /// <param name="meeting">Request object for scheduling meeting</param>
        /// <returns>Task of <see cref="Event"/></returns>
        Task<Office.Event> ScheduleMeeting(string accessToken, Office.Event meeting);
    }
}
