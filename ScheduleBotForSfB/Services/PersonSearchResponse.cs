using System;
using System.Collections.Generic;

namespace SchedulingBot.Services
{
    /// <summary>
    /// Person Search Response
    /// </summary>
    [Serializable]
    public class PersonSearchResponse
    {
        /// <summary>
        /// List of users 
        /// </summary>
        public  List<Person> Value { get; set; }

    }
}