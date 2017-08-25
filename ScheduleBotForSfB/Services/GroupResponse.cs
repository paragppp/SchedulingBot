using System;
using System.Collections.Generic;
using Microsoft.Graph;

namespace SchedulingBot.Services
{
    /// <summary>
    /// Group Response 
    /// </summary>
    public class GroupResponse
    {
        /// <summary>
        /// List of groups
        /// </summary>
        public  List<Group> Value { get; set; }
    }

}