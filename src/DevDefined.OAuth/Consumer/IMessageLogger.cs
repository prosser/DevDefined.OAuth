using System;
using System.Collections.Generic;
using System.Linq;

namespace DevDefined.OAuth.Consumer
{
    public interface IMessageLogger
    {
        void LogMessage(IConsumerRequest request, IConsumerResponse response);
    }
}