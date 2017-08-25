using System;
using System.Collections.Generic;
using Microsoft.Graph;

namespace SchedulingBot.Services
{
    /// <summary>
    /// Group Member Response
    /// </summary>
    public class GroupMemberResponse
    {
        /// <summary>
        /// List of users
        /// </summary>
        public  List<User> Value { get; set; }
    }
}