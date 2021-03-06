﻿using System;

namespace SchedulingBot.Services
{
    /// <summary>
    /// Room record represents room for displaying purposes
    /// </summary>
    [Serializable]
    public class RoomRecord : Room
    {
        /// <summary>
        /// Counter of a room in collection
        /// </summary>
        public int Counter { get; set;  }

        public override string ToString()
        {

            string sentence = null;
            sentence = "<b>"+Counter + ":</b> " + Name;
            return sentence;
            // If we write like $"{Counter}. {Name}" , it does not work
        }
    }
}