using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class ConnectionSettingsEventArgs : EventArgs
    {
        public ConnectionSettingsEventArgs(string port, string ip)
        {
            Port = port;
            IP = ip;
        }

        public string Port { get; set; }
        public string IP { get; set; }
    }
}
