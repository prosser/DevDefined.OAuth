using System;
using System.Collections.Specialized;
using System.IO;

namespace Booyami.DevDefined.OAuth.Consumer
{
    public class RequestDescription
    {
        public RequestDescription()
        {
            Headers = new NameValueCollection();
        }

        public string Body { get; set; }

        public string ContentType { get; set; }

        public NameValueCollection Headers { get; private set; }

        public string Method { get; set; }

        public Stream RequestStream { get; set; }

        public Uri Url { get; set; }
    }
}