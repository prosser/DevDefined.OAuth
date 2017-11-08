using Booyami.DevDefined.OAuth.Framework;

namespace Booyami.DevDefined.OAuth.Consumer
{
    public interface IConsumerRequestFactory
    {
        IConsumerRequest CreateConsumerRequest(IOAuthSession session, IOAuthContext context, IOAuthConsumerContext consumerContext);
    }
}