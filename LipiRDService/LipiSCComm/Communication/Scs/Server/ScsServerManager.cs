using System.Threading;
using System.Collections.Generic;

namespace Lipi.Communication.Scs.Server
{
    /// <summary>
    /// Provides some functionality that are used by servers.
    /// </summary>
    internal class ScsServerManager
    {
        static List<ClientUniqueId> objClientArray = null;
        
        /// <summary>
        /// Used to set an auto incremential unique identifier to clients.
        /// </summary>
        private static long _lastClientId;

        /// <summary>
        /// Gets an unique number to be used as idenfitier of a client.
        /// </summary>
        /// <returns></returns>
        public static long GetClientId()
        {
            return Interlocked.Increment(ref _lastClientId);
        }

        /// <summary>
        /// Gets an unique number to be used as idenfitier of a client. If IP already connected then get its already given identifier. //For V1.0.0.3
        /// </summary>
        /// <param name="strIP">search identifier for the specified IpAddress</param>
        /// <returns>Unique identifier as index position of array</returns>
        public static long GetClientId(string strIP)
        {
            if (objClientArray == null)
                objClientArray = new List<ClientUniqueId>();

            foreach (var curClient in objClientArray)
            {
                if (curClient.strIP == strIP)
                    return curClient.iUniqueId;
            }

            ClientUniqueId objClient = new ClientUniqueId();
            objClient.iUniqueId = Interlocked.Increment(ref _lastClientId);
            objClient.strIP = strIP;
            objClientArray.Add(objClient);
            return objClient.iUniqueId;
            
            //return Interlocked.Increment(ref _lastClientId);
        }
    }

    //For V1.0.0.3
    class ClientUniqueId
    {
        public string strIP;
        public long iUniqueId;
    }

    ///// <summary>
    ///// Provides some functionality that are used by servers.
    ///// </summary>
    //internal static class ScsServerManager
    //{
    //    /// <summary>
    //    /// Used to set an auto incremential unique identifier to clients.
    //    /// </summary>
    //    private static long _lastClientId;

    //    /// <summary>
    //    /// Gets an unique number to be used as idenfitier of a client.
    //    /// </summary>
    //    /// <returns></returns>
    //    public static long GetClientId()
    //    {
    //        return Interlocked.Increment(ref _lastClientId);
    //    }
    //}
}
