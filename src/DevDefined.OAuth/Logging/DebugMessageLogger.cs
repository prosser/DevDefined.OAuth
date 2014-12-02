﻿using DevDefined.OAuth.Consumer;

namespace DevDefined.OAuth.Logging
{
    public class DebugMessageLogger : IMessageLogger
    {
        public void LogMessage(IConsumerRequest request, IConsumerResponse response)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("{0} {1}", request.Context.RequestMethod, request.Context.GenerateUrl()));
            System.Diagnostics.Debug.WriteLine(string.Format("HTTP {0} {1} Content-Type:{2} Content-Length:{3} Time-Taken:{4:F2}s",
                (int)response.ResponseCode, response.ResponseCode, response.ContentType, response.ContentLength, response.TimeTaken.TotalSeconds));

            if (!string.IsNullOrEmpty(response.Content))
            {
                System.Diagnostics.Debug.WriteLine(string.Concat("Response body starts:", (response.Content.Length > 100) ? response.Content.Substring(0, 100) : response.Content));
            }
        }
    }
}
