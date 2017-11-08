namespace Booyami.DevDefined.OAuth.Storage.Basic
{
    /// <summary>
    /// In-Memory implementation of a token repository
    /// </summary>
    public class InMemoryTokenRepository : ITokenRepository
    {
        private AccessToken _accessToken;
        private RequestToken _requestToken;

        public AccessToken GetAccessToken()
        {
            return _accessToken;
        }

        public RequestToken GetRequestToken()
        {
            return _requestToken;
        }

        public void SaveAccessToken(AccessToken accessToken)
        {
            _accessToken = accessToken;
        }

        public void SaveRequestToken(RequestToken requestToken)
        {
            _requestToken = requestToken;
        }
    }
}