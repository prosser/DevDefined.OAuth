using Booyami.DevDefined.OAuth.Framework;
using System;
using System.IO;
using System.Net;

namespace Booyami.DevDefined.OAuth.Consumer
{
    public interface IConsumerRequest
    {
        string AcceptsType { get; set; }

        IOAuthContext Context { get; }

        Uri ProxyServerUri { get; set; }

        string RequestBody { get; set; }

        Stream RequestStream { get; set; }

        RequestDescription GetRequestDescription();

        IConsumerRequest SignWithoutToken();

        IConsumerRequest SignWithToken();

        IConsumerRequest SignWithToken(IToken token, bool checkForExistingSignature = true);

        IConsumerResponse ToConsumerResponse();

        HttpWebRequest ToWebRequest();

        [Obsolete("Prefer ToConsumerResponse instead as this has more error handling built in")]
        HttpWebResponse ToWebResponse();
    }
}