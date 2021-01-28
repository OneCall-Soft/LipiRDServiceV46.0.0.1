using Lipi.Communication.Scs.Communication;
using Lipi.Communication.Scs.Communication.Messengers;

namespace Lipi.Communication.Scs.Client
{
    /// <summary>
    /// Represents a client to connect to server.
    /// </summary>
    public interface IScsClient : IMessenger, IConnectableClient
    {
        //Does not define any additional member
    }
}
