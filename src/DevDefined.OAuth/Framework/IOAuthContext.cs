using System;
using System.Collections.Specialized;

namespace Booyami.DevDefined.OAuth.Framework
{
    public interface IOAuthContext : IToken
    {
        NameValueCollection AuthorizationHeaderParameters { get; set; }

        string CallbackUrl { get; set; }

        NameValueCollection Cookies { get; set; }

        NameValueCollection FormEncodedParameters { get; set; }

        NameValueCollection Headers { get; set; }

        DateTime? IfModifiedSince { get; set; }

        string Nonce { get; set; }

        string NormalizedRequestUrl { get; }

        NameValueCollection QueryParameters { get; set; }

        Uri RawUri { get; set; }

        string RequestMethod { get; set; }

        string SessionHandle { get; set; }

        string Signature { get; set; }

        string SignatureMethod { get; set; }

        string Timestamp { get; set; }

        bool UseAuthorizationHeader { get; set; }

        string Verifier { get; set; }

        string Version { get; set; }

        string XAuthMode { get; set; }

        string XAuthPassword { get; set; }

        string XAuthUsername { get; set; }

        string GenerateOAuthParametersForHeader();

        string GenerateSignatureBase();

        Uri GenerateUri();

        Uri GenerateUriWithoutOAuthParameters();

        string GenerateUrl();

        string ToString();
    }
}