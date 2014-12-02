#region License

// The MIT License
//
// Copyright (c) 2006-2008 DevDefined Limited.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion License

using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Storage.Basic;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Authentication;
using XeroApi.OAuth.Framework;

namespace DevDefined.OAuth.Consumer
{
    [Serializable]
    public class OAuthSession : IOAuthSession
    {
        private readonly NameValueCollection _cookies = new NameValueCollection();
        private readonly NameValueCollection _formParameters = new NameValueCollection();
        private readonly NameValueCollection _headers = new NameValueCollection();
        private readonly NameValueCollection _queryParameters = new NameValueCollection();
        private readonly ITokenRepository _tokenRepository;

        public OAuthSession(IOAuthConsumerContext consumerContext, ITokenRepository tokenRepository)
        {
            ConsumerContext = consumerContext;
            ConsumerRequestFactory = DefaultConsumerRequestFactory.Instance;
            ConsumerRequestRunner = new DefaultConsumerRequestRunner();
            _tokenRepository = tokenRepository ?? new InMemoryTokenRepository();
        }

        [Obsolete("Use the constructor with ITokenRepository parameter")]
        public OAuthSession(IOAuthConsumerContext consumerContext)
            : this(consumerContext, (ITokenRepository)null)
        {
        }

        [Obsolete("Use the constructor with ITokenReposiory parameter")]
        public OAuthSession(IOAuthConsumerContext consumerContext, string endPointUrl)
            : this(consumerContext, endPointUrl, endPointUrl, endPointUrl, null)
        {
        }

        [Obsolete("Use the constructor with ITokenRepository parameter")]
        public OAuthSession(IOAuthConsumerContext consumerContext, string requestTokenUrl, string userAuthorizeUrl, string accessTokenUrl)
            : this(consumerContext, requestTokenUrl, userAuthorizeUrl, accessTokenUrl, null)
        {
        }

        [Obsolete("Use the constructor with ITokenRepository parameter")]
        public OAuthSession(IOAuthConsumerContext consumerContext, string requestTokenUrl, string userAuthorizeUrl, string accessTokenUrl, string callBackUrl)
            : this(consumerContext, SafeCreateUri(requestTokenUrl), SafeCreateUri(userAuthorizeUrl), SafeCreateUri(accessTokenUrl), SafeCreateUri(callBackUrl))
        {
        }

        [Obsolete("Use the constructor with ITokenReposiory parameter")]
        public OAuthSession(IOAuthConsumerContext consumerContext, Uri endPointUri)
            : this(consumerContext, endPointUri, endPointUri, endPointUri, null)
        {
        }

        [Obsolete("Use the constructor with ITokenRepository parameter")]
        public OAuthSession(IOAuthConsumerContext consumerContext, Uri requestTokenUri, Uri userAuthorizeUri, Uri accessTokenUri)
            : this(consumerContext, requestTokenUri, userAuthorizeUri, accessTokenUri, null)
        {
        }

        [Obsolete("Use the constructor with ITokenRepository parameter")]
        public OAuthSession(IOAuthConsumerContext consumerContext, Uri requestTokenUri, Uri userAuthorizeUri, Uri accessTokenUri, Uri callBackUri)
            : this(consumerContext, (ITokenRepository)null)
        {
            RequestTokenUri = requestTokenUri;
            AccessTokenUri = accessTokenUri;
            UserAuthorizeUri = userAuthorizeUri;
            CallbackUri = callBackUri;
        }

        public IToken AccessToken
        {
            get { return _tokenRepository.GetAccessToken(); }
            set { _tokenRepository.SaveAccessToken(value as AccessToken); }
        }

        [Obsolete]
        public Uri AccessTokenUri { get; set; }

        public bool CallbackMustBeConfirmed { get; set; }

        [Obsolete]
        public Uri CallbackUri { get; set; }

        public IOAuthConsumerContext ConsumerContext { get; set; }

        public IConsumerRequestFactory ConsumerRequestFactory { get; set; }

        public IConsumerRequestRunner ConsumerRequestRunner { get; set; }

        public bool HasValidAccessToken
        {
            get
            {
                return (TokenRepository != null) && (TokenRepository.GetAccessToken() != null) && (!TokenRepository.GetAccessToken().HasExpired() ?? false);
            }
        }

        public IMessageLogger MessageLogger { get; set; }

        [Obsolete]
        public Uri RequestTokenUri { get; set; }

        public ITokenRepository TokenRepository { get { return _tokenRepository; } }

        [Obsolete]
        public Uri UserAuthorizeUri { get; set; }

        public IConsumerRequest BuildAccessTokenContext(string method, string xAuthMode, string xAuthUsername, string xAuthPassword)
        {
            return Request()
               .ForMethod(method)
               .AlterContext(context => context.XAuthUsername = xAuthUsername)
               .AlterContext(context => context.XAuthPassword = xAuthPassword)
               .AlterContext(context => context.XAuthMode = xAuthMode)
               .ForUri(AccessTokenUri)
               .SignWithoutToken();
        }

        public IConsumerRequest BuildExchangeRequestTokenForAccessTokenContext(IToken requestToken, string verificationCode)
        {
            return Request()
              .ForMethod("GET")
              .AlterContext(context => context.Verifier = verificationCode)
              .ForUri(ConsumerContext.AccessTokenUri)
              .SignWithToken(requestToken);
        }

        public IConsumerRequest BuildRenewAccessTokenContext(IToken requestToken, string sessionHandle)
        {
            return Request()
              .ForMethod("GET")
              .AlterContext(context => context.SessionHandle = sessionHandle)
              .ForUri(ConsumerContext.AccessTokenUri)
              .SignWithToken(requestToken);
        }

        [Obsolete]
        public IConsumerRequest BuildRequestTokenContext(string method)
        {
            return Request()
                .ForMethod(method)
                .AlterContext(context => context.CallbackUrl = (CallbackUri == null) ? "oob" : CallbackUri.ToString())
                .ForUri(RequestTokenUri)
                    .SignWithoutToken();
        }

        [Obsolete("The request token is stored in the TokenRepository, use the overloaded method that only uses a verificationCode parameter")]
        public AccessToken ExchangeRequestTokenForAccessToken(IToken requestToken)
        {
            return ExchangeRequestTokenForAccessToken(requestToken, null);
        }

        [Obsolete("The request token is stored in the TokenRepository, use the overloaded method that only uses a verificationCode parameter")]
        public AccessToken ExchangeRequestTokenForAccessToken(IToken requestToken, string verificationCode)
        {
            AccessToken token = BuildExchangeRequestTokenForAccessTokenContext(requestToken, verificationCode)
              .Select(collection =>
                      new AccessToken
                        {
                            ConsumerKey = requestToken.ConsumerKey,
                            Token = ParseResponseParameter(collection, Parameters.OAuth_Token),
                            TokenSecret = ParseResponseParameter(collection, Parameters.OAuth_Token_Secret),
                            SessionHandle = ParseResponseParameter(collection, Parameters.OAuth_Session_Handle),
                            SessionExpiresIn = ParseResponseParameter(collection, Parameters.OAuth_Authorization_Expires_In),
                            ExpiresIn = ParseResponseParameter(collection, Parameters.OAuth_Expires_In),
                            CreatedDateUtc = DateTime.UtcNow
                        });

            AssertValidAccessToken(token);
            TokenRepository.SaveAccessToken(token);

            return token;
        }

        public AccessToken ExchangeRequestTokenForAccessToken(string verificationCode)
        {
            var requestToken = TokenRepository.GetRequestToken();

            if (requestToken == null)
            {
                throw new ApplicationException("The current TokenRepository doesn't have a current request token");
            }

            AccessToken token = BuildExchangeRequestTokenForAccessTokenContext(requestToken, verificationCode)
            .Select(collection =>
                    new AccessToken
                      {
                          ConsumerKey = requestToken.ConsumerKey,
                          Token = ParseResponseParameter(collection, Parameters.OAuth_Token),
                          TokenSecret = ParseResponseParameter(collection, Parameters.OAuth_Token_Secret),
                          SessionHandle = ParseResponseParameter(collection, Parameters.OAuth_Session_Handle),
                          SessionExpiresIn = ParseResponseParameter(collection, Parameters.OAuth_Authorization_Expires_In),
                          ExpiresIn = ParseResponseParameter(collection, Parameters.OAuth_Expires_In),
                          CreatedDateUtc = DateTime.UtcNow
                      });

            AssertValidAccessToken(token);
            TokenRepository.SaveAccessToken(token);

            return token;
        }

        public AccessToken GetAccessToken()
        {
            var accessToken = _tokenRepository.GetAccessToken();

            if (accessToken == null)
            {
                throw new MissingTokenException("The token repository doesn't contain a valid access token");
            }

            if ((accessToken.HasExpired() ?? false) && accessToken.CanRefresh)
            {
                // Proactively refresh the access token before using it.
                return RenewAccessToken();
            }

            return accessToken;
        }

        [Obsolete]
        public IToken GetAccessTokenUsingXAuth(string authMode, string username, string password)
        {
            TokenBase token = BuildAccessTokenContext("GET", authMode, username, password)
               .Select(collection =>
                       new TokenBase
                       {
                           ConsumerKey = ConsumerContext.ConsumerKey,
                           Token = ParseResponseParameter(collection, Parameters.OAuth_Token),
                           TokenSecret = ParseResponseParameter(collection, Parameters.OAuth_Token_Secret),
                           SessionHandle = ParseResponseParameter(collection, Parameters.OAuth_Session_Handle)
                       });

            AccessToken = token;
            return token;
        }

        public RequestToken GetRequestToken()
        {
            return GetRequestToken(null);
        }

        public RequestToken GetRequestToken(Uri callbackUri)
        {
            IConsumerRequest request = Request()
              .ForMethod("GET")
              .AlterContext(context => context.CallbackUrl = (callbackUri == null) ? "oob" : callbackUri.ToString())
              .AlterContext(context => context.Token = null)
              .ForUri(ConsumerContext.RequestTokenUri)
              .SignWithoutToken();

            var results = request.Select(collection =>
            new
            {
                ConsumerContext.ConsumerKey,
                Token = ParseResponseParameter(collection, Parameters.OAuth_Token),
                TokenSecret = ParseResponseParameter(collection, Parameters.OAuth_Token_Secret),
                CallackConfirmed = WasCallbackConfimed(collection)
            });

            if (!results.CallackConfirmed && CallbackMustBeConfirmed)
            {
                throw Error.CallbackWasNotConfirmed();
            }

            var requestToken = new RequestToken
            {
                ConsumerKey = results.ConsumerKey,
                Token = results.Token,
                TokenSecret = results.TokenSecret
            };

            TokenRepository.SaveRequestToken(requestToken);

            return requestToken;
        }

        public string GetUserAuthorizationUrl()
        {
            var requestToken = TokenRepository.GetRequestToken();

            if (requestToken == null)
            {
                throw new MissingTokenException("The token repository does not contain a valid request token");
            }

            return GetUserAuthorizationUrl(requestToken);
        }

        [Obsolete("Use the GetUserAuthorizationUrl method instead")]
        public string GetUserAuthorizationUrlForToken(IToken token)
        {
            return GetUserAuthorizationUrl(token);
        }

        [Obsolete("Use the GetUserAuthorizationUrl method instead")]
        public string GetUserAuthorizationUrlForToken(IToken token, string callbackUrl)
        {
            return GetUserAuthorizationUrl(token);
        }

        public IConsumerResponse LogMessage(IConsumerRequest request, IConsumerResponse response)
        {
            if (MessageLogger != null)
            {
                MessageLogger.LogMessage(request, response);
            }

            return response;
        }

        public AccessToken RenewAccessToken(IToken accessToken, string sessionHandle)
        {
            var requestToken = TokenRepository.GetRequestToken();

            if (requestToken == null)
            {
                throw new ApplicationException("The token repository doesn't have a current request token");
            }

            AccessToken token = BuildRenewAccessTokenContext(accessToken, sessionHandle)
                .Select(collection =>
                        new AccessToken
                        {
                            ConsumerKey = accessToken.ConsumerKey,
                            Token = ParseResponseParameter(collection, Parameters.OAuth_Token),
                            TokenSecret = ParseResponseParameter(collection, Parameters.OAuth_Token_Secret),
                            SessionHandle = ParseResponseParameter(collection, Parameters.OAuth_Session_Handle),
                            SessionExpiresIn = ParseResponseParameter(collection, Parameters.OAuth_Authorization_Expires_In),
                            ExpiresIn = ParseResponseParameter(collection, Parameters.OAuth_Expires_In),
                            CreatedDateUtc = DateTime.UtcNow
                        });

            AssertValidAccessToken(token);
            TokenRepository.SaveAccessToken(token);

            return token;
        }

        public AccessToken RenewAccessToken()
        {
            var currentAccessToken = TokenRepository.GetAccessToken();

            if (currentAccessToken == null)
            {
                throw new ApplicationException("The token repository doesn't have a current access token");
            }

            return RenewAccessToken(currentAccessToken, currentAccessToken.SessionHandle);
        }

        [Obsolete("Use the overloaded method without using an access token")]
        public virtual IConsumerRequest Request(IToken accessToken)
        {
            var context = new OAuthContext
              {
                  UseAuthorizationHeader = ConsumerContext.UseHeaderForOAuthParameters
              };

            context.Cookies.Add(_cookies);
            context.FormEncodedParameters.Add(_formParameters);
            context.Headers.Add(_headers);
            context.QueryParameters.Add(_queryParameters);

            return ConsumerRequestFactory.CreateConsumerRequest(this, context, ConsumerContext);
        }

        public virtual IConsumerRequest Request()
        {
            var context = new OAuthContext
            {
                UseAuthorizationHeader = ConsumerContext.UseHeaderForOAuthParameters
            };

            context.Cookies.Add(_cookies);
            context.FormEncodedParameters.Add(_formParameters);
            context.Headers.Add(_headers);
            context.QueryParameters.Add(_queryParameters);

            return ConsumerRequestFactory.CreateConsumerRequest(this, context, ConsumerContext);
        }

        public IOAuthSession RequiresCallbackConfirmation()
        {
            CallbackMustBeConfirmed = true;
            return this;
        }

        public IConsumerResponse RunConsumerRequest(IConsumerRequest consumerRequest)
        {
            int retryCounter = 2;
            while (retryCounter-- > 0)
            {
                IConsumerResponse consumerResponse = ConsumerRequestRunner.Run(consumerRequest);
                LogMessage(consumerRequest, consumerResponse);

                if (consumerResponse.IsForbiddenResponse)
                {
                    // Catch http 403 errors generated by IIS that are actually html pages warning about certificate issues..
                    throw new AuthenticationException(string.Format("The API server returned http {0} with content type {1}. See the inner exception for more details.", (int)consumerResponse.ResponseCode, consumerResponse.ContentType), consumerResponse.WebException);
                }

                if (consumerResponse.IsTokenExpiredResponse && !string.IsNullOrEmpty(consumerRequest.Context.SessionHandle))
                {
                    // Refresh the access token and try again..
                    AccessToken newAccessToken = RenewAccessToken();
                    consumerRequest.SignWithToken(newAccessToken, false);
                    continue;
                }

                if (consumerResponse.IsOAuthProblemResponse)
                {
                    // A usable response wasn't returned..
                    throw new OAuthException(consumerResponse, consumerRequest.Context, consumerResponse.ToProblemReport());
                }

                return consumerResponse;
            }

            throw new ApplicationException("The consumer request could not be executed into a valid consumer response");
        }

        public IOAuthSession WithCookies(IDictionary<string, string> dictionary)
        {
            return AddItems(_cookies, dictionary);
        }

        public IOAuthSession WithFormParameters(IDictionary<string, string> dictionary)
        {
            return AddItems(_formParameters, dictionary);
        }

        public IOAuthSession WithHeaders(IDictionary<string, string> dictionary)
        {
            return AddItems(_headers, dictionary);
        }

        public IOAuthSession WithQueryParameters(IDictionary<string, string> dictionary)
        {
            return AddItems(_queryParameters, dictionary);
        }

        private static void AssertValidAccessToken(AccessToken token)
        {
            if (token == null)
            {
                throw new MissingTokenException("The access token could not be obtained");
            }

            string expiryDateString = token.ExpiryDateUtc.HasValue ? token.ExpiryDateUtc.ToString() : "n/a";
            string usableTimespan = token.SessionTimespan.ToString();

            Debug.WriteLine(string.Format("Access token {0} will last for {1} and will expire at {2} UTC.", token.Token, usableTimespan, expiryDateString));
        }

        private static string ParseResponseParameter(NameValueCollection collection, string parameter)
        {
            string value = (collection[parameter] ?? "").Trim();
            return (value.Length > 0) ? value : null;
        }

        private static Uri SafeCreateUri(string url)
        {
            return url == null ? null : new Uri(url);
        }

        private static bool WasCallbackConfimed(NameValueCollection parameters)
        {
            string value = ParseResponseParameter(parameters, Parameters.OAuth_Callback_Confirmed);
            return (value == "true");
        }

        private OAuthSession AddItems(NameValueCollection destination, IDictionary<string, string> additions)
        {
            foreach (string parameter in additions.Keys)
            {
                destination[parameter] = additions[parameter];
            }

            return this;
        }

        private string GetUserAuthorizationUrl(IToken requestToken)
        {
            var builder = new UriBuilder(ConsumerContext.UserAuthorizeUri);

            var collection = new NameValueCollection();

            if (builder.Query != null)
            {
                collection.Add(UriUtility.ParseQueryString(builder.Query));
            }

            if (_queryParameters != null) collection.Add(_queryParameters);

            collection[Parameters.OAuth_Token] = requestToken.Token;

            builder.Query = UriUtility.FormatQueryString(collection);

            return builder.Uri.ToString();
        }
    }
}