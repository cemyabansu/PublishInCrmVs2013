using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CemYabansu.PublishInCrm
{
    class ConnectionProfile
    {
        public string Tag { get; set; }
        public string ServerUrl { get; set; }
        public string Port { get; set; }
        public bool UseSSL { get; set; }
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string OrganizationName { get; set; }
        public bool IsDefault { get; set; }
    }
}
