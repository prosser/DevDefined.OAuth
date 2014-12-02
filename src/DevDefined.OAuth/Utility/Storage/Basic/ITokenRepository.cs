namespace DevDefined.OAuth.Storage.Basic
{
    /// <summary>
    /// A simplistic repository for access and request of token models.
    /// </summary>
    public interface ITokenRepository
    {
        AccessToken GetAccessToken();

        RequestToken GetRequestToken();

        void SaveAccessToken(AccessToken accessToken);

        void SaveRequestToken(RequestToken requestToken);
    }
}