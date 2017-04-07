// The MIT License
//
// Copyright (c) 2006-2008 DevDefined Limited.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using QueryParameter = System.Collections.Generic.KeyValuePair<string, string>;

namespace DevDefined.OAuth.Framework
{
    public static class UriUtility
    {
        private const string AuthorizationHeaderRealmParameter = "realm";
        private const string OAuthAuthorizationHeaderStart = "OAuth";
        private const string UnreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        private static readonly string[] QuoteCharacters = new[] { "\"", "'" };

        /// <summary>
        /// Takes an http method, url and a set of parameters and formats them as a signature base as
        /// per the OAuth core spec.
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string FormatParameters(string httpMethod, Uri url, List<QueryParameter> parameters)
        {
            string normalizedRequestParameters = NormalizeRequestParameters(parameters);

            var signatureBase = new StringBuilder();
            signatureBase.AppendFormat("{0}&", httpMethod.ToUpper());

            signatureBase.AppendFormat("{0}&", UrlEncode(NormalizeUri(url)));
            signatureBase.AppendFormat("{0}", UrlEncode(normalizedRequestParameters));

            return signatureBase.ToString();
        }

        /// <summary>
        /// Formats a set of query parameters, as per query string encoding.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string FormatQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var builder = new StringBuilder();

            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    if (builder.Length > 0) builder.Append("&");
                    builder.Append(pair.Key).Append("=");
                    builder.Append(UrlEncode(pair.Value));
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Formats a set of query parameters, as per query string encoding.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string FormatQueryString(NameValueCollection parameters)
        {
            var builder = new StringBuilder();

            if (parameters != null)
            {
                foreach (string key in parameters.Keys)
                {
                    if (builder.Length > 0) builder.Append("&");
                    builder.Append(key).Append("=");
                    builder.Append(UrlEncode(parameters[key]));
                }
            }

            return builder.ToString();
        }

        public static string FormatTokenForResponse(IToken token)
        {
            var builder = new StringBuilder();

            builder.Append(Parameters.OAuth_Token).Append("=").Append(UrlEncode(token.Token))
              .Append("&").Append(Parameters.OAuth_Token_Secret).Append("=").Append(UrlEncode(token.TokenSecret));

            return builder.ToString();
        }

        /// <summary>
        /// Extracts all the parameters from the supplied encoded parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static List<QueryParameter> GetHeaderParameters(string parameters)
        {
            parameters = parameters.Trim();

            var result = new List<QueryParameter>();

            if (!parameters.StartsWith(OAuthAuthorizationHeaderStart, StringComparison.InvariantCultureIgnoreCase))
            {
                return result;
            }

            parameters = parameters.Substring(OAuthAuthorizationHeaderStart.Length).Trim();

            if (!string.IsNullOrEmpty(parameters))
            {
                string[] p = parameters.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string s in p)
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    QueryParameter parameter = ParseAuthorizationHeaderKeyValuePair(s);
                    if (parameter.Key != AuthorizationHeaderRealmParameter)
                    {
                        result.Add(parameter);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts all the parameters from the supplied encoded parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static List<QueryParameter> GetQueryParameters(string parameters)
        {
            if (parameters.StartsWith("?"))
            {
                parameters = parameters.Remove(0, 1);
            }

            var result = new List<QueryParameter>();

            if (!string.IsNullOrEmpty(parameters))
            {
                string[] p = parameters.Split('&');
                foreach (string s in p)
                {
                    if (!string.IsNullOrEmpty(s) && !s.StartsWith(Parameters.OAuthParameterPrefix))
                    {
                        if (s.IndexOf('=') > -1)
                        {
                            string[] temp = s.Split('=');
                            result.Add(new QueryParameter(temp[0], temp[1]));
                        }
                        else
                        {
                            result.Add(new QueryParameter(s, string.Empty));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Normalizes a sequence of key/value pair parameters as per the OAuth core specification.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string NormalizeRequestParameters(IEnumerable<QueryParameter> parameters)
        {
            IComparer<string> stringComparer = new LexicographicalByteValueStringComparer();

            IEnumerable<QueryParameter> orderedParameters = parameters
              .OrderBy(x => x.Key, stringComparer)
              .ThenBy(x => x.Value, stringComparer)
              .Select(x => new QueryParameter(x.Key, UrlEncode(x.Value)));

            var builder = new StringBuilder();

            foreach (var parameter in orderedParameters)
            {
                if (builder.Length > 0) builder.Append("&");

                builder.Append(parameter.Key).Append("=").Append(parameter.Value);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Normalizes a Url according to the OAuth specification (this ensures http or https on a
        /// default port is not displayed with the :XXX following the host in the url).
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string NormalizeUri(Uri uri)
        {
            string normalizedUrl = string.Format("{0}://{1}", uri.Scheme, uri.Host);

            if (!((uri.Scheme == "http" && uri.Port == 80) ||
                  (uri.Scheme == "https" && uri.Port == 443)))
            {
                normalizedUrl += ":" + uri.Port;
            }

            return normalizedUrl + ((uri.AbsolutePath == "/") ? "" : uri.AbsolutePath);
        }

        public static NameValueCollection ParseQueryString(string queryString)
        {
            if (queryString.StartsWith("?"))
                queryString = queryString.Remove(0, 1);

            NameValueCollection collection = new NameValueCollection();

            if (string.IsNullOrEmpty(queryString))
                return collection;

            foreach (string s in queryString.Split('&').Where(s => !string.IsNullOrEmpty(s)))
            {
                if (s.IndexOf('=') > -1)
                {
                    string[] temp = s.Split('=');
                    collection.Add(Uri.UnescapeDataString(temp[0]), Uri.UnescapeDataString(temp[1]));
                }
                else
                {
                    collection.Add(s, string.Empty);
                }
            }

            return collection;
        }

        public static NameValueCollection ToNameValueCollection(this IEnumerable<QueryParameter> source)
        {
            var collection = new NameValueCollection();

            foreach (var parameter in source)
            {
                collection[parameter.Key] = parameter.Value;
            }

            return collection;
        }

        public static IEnumerable<QueryParameter> ToQueryParameters(this NameValueCollection source)
        {
            foreach (string key in source.AllKeys)
            {
                yield return new QueryParameter(key, source[key]);
            }
        }

        public static IEnumerable<QueryParameter> ToQueryParametersExcludingTokenSecret(this NameValueCollection source)
        {
            foreach (string key in source.AllKeys)
            {
                if (key != Parameters.OAuth_Token_Secret)
                {
                    yield return new QueryParameter(key, source[key]);
                }
            }
        }

        /// <summary>
        /// An OAuth specific implementation of Url Encoding - we need this to produce consistent
        /// base strings to other platforms such as php or ruby, as the HttpUtility.UrlEncode doesn't
        /// encode in the same way.
        /// </summary>
        /// <remarks>
        /// Taken from: http://oauth.net/core/1.0a/#encoding_parameters All parameter names and
        /// values are escaped using the [RFC3986] percent-encoding (%xx) mechanism. Characters not
        /// in the unreserved character set ([RFC3986] section 2.3) MUST be encoded. Characters in
        /// the unreserved character set MUST NOT be encoded. Hexadecimal characters in encodings
        /// MUST be upper case. Text names and values MUST be encoded as UTF-8 octets before
        /// percent-encoding them per [RFC3629].
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UrlEncode(string value)
        {
            if (value == null) return null;

            var result = new StringBuilder();

            foreach (char symbol in value)
            {
                if (UnreservedChars.IndexOf(symbol) != -1)
                {
                    // Characters in the unreserved character set MUST NOT be encoded.
                    result.Append(symbol);
                }
                else
                {
                    // Text names and values MUST be encoded as UTF-8 octets before percent-encoding them
                    byte[] utf8Octets = Encoding.UTF8.GetBytes(new[] { symbol });

                    foreach (byte utf8Octet in utf8Octets)
                    {
                        // Second, the each octet should be %nn encoded
                        result.Append('%' + String.Format("{0:X2}", (int)utf8Octet));
                    }
                }
            }

            return result.ToString();
        }

        private static QueryParameter ParseAuthorizationHeaderKeyValuePair(string keyEqualValuePair)
        {
            if (keyEqualValuePair.IndexOf('=') > -1)
            {
                string[] temp = keyEqualValuePair.Split('=');

                string itemValue = temp[1];
                itemValue = StripQuotes(itemValue);
                itemValue = Uri.UnescapeDataString(itemValue); // HttpUtility.UrlDecode(itemValue)

                return new QueryParameter(temp[0].Trim(), itemValue);
            }
            return new QueryParameter(keyEqualValuePair.Trim(), string.Empty);
        }

        private static string StripQuotes(string quotedValue)
        {
            foreach (string quoteCharacter in QuoteCharacters)
            {
                if (quotedValue.StartsWith(quoteCharacter) && quotedValue.EndsWith(quoteCharacter) && quotedValue.Length > 1)
                {
                    return quotedValue.Substring(1, quotedValue.Length - 2);
                }
            }

            return quotedValue;
        }

        /// <summary>
        /// Lexicographical byte value string comparer
        /// </summary>
        /// <remarks>Adapted from http://stackoverflow.com/questions/839429/oauth-lexicographical-byte-value-ordering-in-c</remarks>
        private class LexicographicalByteValueStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }
    }
}
