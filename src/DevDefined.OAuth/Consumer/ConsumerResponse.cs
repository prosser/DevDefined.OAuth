using Booyami.DevDefined.OAuth.Framework;
using Booyami.DevDefined.OAuth.Utility;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace Booyami.DevDefined.OAuth.Consumer
{
    public interface IConsumerResponse
    {
        byte[] ByteArray { get; }

        string Content { get; }

        int ContentLength { get; }

        string ContentType { get; }

        WebHeaderCollection Headers { get; }

        bool IsClientError { get; }

        bool IsForbiddenResponse { get; }

        bool IsGoodResponse { get; }

        bool IsOAuthProblemResponse { get; }

        bool IsServerError { get; }

        bool IsTokenExpiredResponse { get; }

        HttpStatusCode ResponseCode { get; }

        Stream Stream { get; }

        TimeSpan TimeTaken { get; }

        WebException WebException { get; }

        T DeSerialiseTo<T>();

        NameValueCollection ToBodyParameters();

        OAuthProblemReport ToProblemReport();

        XDocument ToXDocument();
    }

    public class ConsumerResponse : IConsumerResponse
    {
        private readonly OAuthProblemReport _problemReport = null;
        private readonly MemoryStream _responseContentStream = new MemoryStream();
        private readonly WebException _webException = null;

        public ConsumerResponse(HttpWebResponse webResponse, WebException webException, TimeSpan timeTaken = default(TimeSpan))
            : this(webResponse, timeTaken)
        {
            _webException = webException;
        }

        public ConsumerResponse(HttpWebResponse webResponse, TimeSpan timeTaken = default(TimeSpan))
        {
            webResponse.GetResponseStream().CopyTo(_responseContentStream);

            if (!string.IsNullOrEmpty(webResponse.Headers["Content-Type"]))
                ContentType = webResponse.Headers["Content-Type"];

            if (!string.IsNullOrEmpty(webResponse.Headers["Content-Length"]))
                ContentLength = int.Parse(webResponse.Headers["Content-Length"]);

            TimeTaken = timeTaken;
            ContentEncoding = webResponse.ContentEncoding;
            ResponseCode = webResponse.StatusCode;
            Headers = webResponse.Headers;

            // Look for 'oauth_problem' in the message response for http 401 and 400 responses
            if (ResponseCode == HttpStatusCode.Unauthorized || ResponseCode == HttpStatusCode.BadRequest)
            {
                _problemReport = Content.Contains(Parameters.OAuth_Problem) ? new OAuthProblemReport(Content) : null;
            }
        }

        public Byte[] ByteArray
        {
            get
            {
                return _responseContentStream.ToArray();
            }
        }

        public string Content
        {
            get
            {
                return Stream.ReadToEnd();
            }
        }

        public string ContentEncoding
        {
            get;
            private set;
        }

        public int ContentLength
        {
            get;
            private set;
        }

        public string ContentType
        {
            get;
            private set;
        }

        public WebHeaderCollection Headers
        {
            get;
            set;
        }

        public bool IsClientError
        {
            get { return (int)ResponseCode >= 400 && (int)ResponseCode <= 499; }
        }

        public bool IsForbiddenResponse
        {
            get { return (int)ResponseCode == 403; }
        }

        public bool IsGoodResponse
        {
            get { return (int)ResponseCode >= 200 && (int)ResponseCode <= 299; }
        }

        public bool IsOAuthProblemResponse
        {
            get { return _problemReport != null; }
        }

        public bool IsServerError
        {
            get { return (int)ResponseCode >= 500; }
        }

        public bool IsTokenExpiredResponse
        {
            get
            {
                return (_problemReport != null)
                    && (string.Compare(_problemReport.Problem, OAuthProblems.TokenExpired, true) == 0)
                    && (_problemReport.ProblemAdvice.Contains("expired"));
            }
        }

        public HttpStatusCode ResponseCode
        {
            get;
            set;
        }

        public Stream Stream
        {
            get
            {
                return _responseContentStream;
            }
        }

        public TimeSpan TimeTaken
        {
            get;
            private set;
        }

        public WebException WebException
        {
            get { return _webException; }
        }

        public T DeSerialiseTo<T>()
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (StreamReader sr = new StreamReader(Stream))
            using (XmlReader xr = new XmlTextReader(sr))
            {
                return (T)serializer.Deserialize(xr);
            }
        }

        public NameValueCollection ToBodyParameters()
        {
            return UriUtility.ParseQueryString(Content);
        }

        public OAuthProblemReport ToProblemReport()
        {
            return _problemReport;
        }

        public XDocument ToXDocument()
        {
            return XDocument.Parse(Content);
        }
    }
}