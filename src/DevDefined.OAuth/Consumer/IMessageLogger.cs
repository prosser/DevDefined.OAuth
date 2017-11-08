using System;
using System.Collections.Generic;
using System.Linq;

namespace Booyami.DevDefined.OAuth.Consumer
{
    public interface IMessageLogger
    {
        void LogMessage(IConsumerRequest request, IConsumerResponse response);
    }
}