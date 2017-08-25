using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using SchedulingBot.Dialogs;
using SchedulingBot.Services;

namespace SchedulingBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private readonly IMeetingService _meetingService;
        private readonly IRoomService _roomService;
        private readonly IEmailService _emailService;
        private readonly IHttpService _httpService;
        private readonly ILoggingService _loggingService;

        public MessagesController(IMeetingService meetingService, IRoomService roomService, IEmailService emailService, IHttpService httpService, ILoggingService loggingService)
        {
            _meetingService = meetingService;
            _roomService = roomService;
            _emailService = emailService;
            _httpService = httpService;
            _loggingService = loggingService;
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.From.Id.Contains("sip:"))
            {
                activity.From.Id = activity.From.Id.Replace("sip:", "");
            }

            if (activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new RootDialog(_meetingService, _roomService, _emailService, _httpService, _loggingService));
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private static void HandleSystemMessage(IActivity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened               
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }
        }
    }
}