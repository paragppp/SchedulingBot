﻿using System.Diagnostics;
using System.Web.Http.ExceptionHandling;
using Microsoft.ApplicationInsights;

namespace SchedulingBot.Exceptions
{
    /// <summary>
    /// Logger for tracking all underhanded exceptions 
    /// </summary>
    public class TraceExceptionLogger : ExceptionLogger
    {
        private readonly TelemetryClient _telemetryClient = new TelemetryClient();

        /// <summary>
        /// Log all unhanded exceptions 
        /// </summary>
        /// <param name="context"></param>
        public override void Log(ExceptionLoggerContext context)
        {
            var exception = context.ExceptionContext.Exception;
            _telemetryClient.TrackException(exception);
            Trace.TraceError(exception.ToString());
        }
    }
}