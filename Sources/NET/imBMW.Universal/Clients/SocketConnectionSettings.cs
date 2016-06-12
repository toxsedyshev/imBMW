using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;

namespace imBMW.Clients
{
    public class SocketConnectionSettings
    {
        public SocketConnectionSettings(HostName hostName, string serviceName)
        {
            HostName = hostName;
            ServiceName = serviceName;
        }

        public HostName HostName { get; protected set; }

        public string ServiceName { get; protected set; }
    }
}
