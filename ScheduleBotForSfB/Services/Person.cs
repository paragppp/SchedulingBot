using System;
using System.Collections.Generic;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace SchedulingBot.Services
{
    /// <summary>
    /// Person object 
    /// </summary>
    [Serializable]
    public class Person
    {
        /// <summary>
        /// Person's display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Email address object 
        /// </summary>
        [JsonProperty("emailAddresses")]
        public List<EmailAddress> EmailAddresses { get; set; }
    }
}