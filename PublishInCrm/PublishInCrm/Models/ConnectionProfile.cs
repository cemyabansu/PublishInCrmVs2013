using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace CemYabansu.PublishInCrm
{
    public class ConnectionProfile
    {
        public string Tag { get; set; }
        public string ServerUrl { get; set; }
        public string Port { get; set; }
        public bool UseSSL { get; set; }
        public bool UseIFD { get; set; }
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string OrganizationName { get; set; }
        public bool IsDefault { get; set; }

        public string ConnectionString
        {
            get
            {
                return string.Format(
                        "Server={0}; Domain={1}; Username={2}; Password={3}",
                        ServerUrl,
                        Domain,
                        Username,
                        Password
                    );
            }
        }

        public ConnectionProfile()
        {

        }

        public ConnectionProfile(XmlElement element)
        {
            string connectionString = element.InnerText;
            Dictionary<string, string> atoms = new Dictionary<string, string>();

            foreach (string molecule in connectionString.Split(';'))
            {
                string[] atom = molecule.Split('=');
                atoms.Add(atom[0].Trim(), atom[1].Trim());
            }

            atoms.Add("Port", "");
            atoms.Add("UseSSL", "");
            atoms.Add("UseIFD", "");
            atoms.Add("OrganizationName", "");

            if (atoms.ContainsKey("Server"))
            {
                var serverUrl = atoms["Server"];

                // Parse the server url with a regular expression.
                // Accepts IFD and non-IFD URLs, e.g. :
                // http(s)://myorg.contoso.com
                // http(s)://www.contoso.com/myorg
                // http(s)://myorg.contoso.com:9999
                // http(s)://www.contoso.com:9999/myorg
                // The groupings for this regular expression is below.
                // +--------------+---------------+
                // |    Data      |    Groups     |
                // |    Type      | (IFD/Non-IFD) |
                // | -------------|---------------|
                // | Protocol     |     3 / 11    |                
                // | ServerUrl    |     4 / 12    |
                // | OrgName      |     5 / 15    |
                // | Port Number  |     8 / 14    |
                // +--------------+---------------+
                string urlExpression = @"^(((http|https):\/\/)(([a-zA-Z0-9]+)\.([a-zA-Z0-9\.]+))(:(\d+))?)\/?$|^(((http|https):\/\/)([a-zA-Z0-9\.]+)(:(\d+))?\/([a-zA-Z0-9]+))$";
                var match = Regex.Match(serverUrl, urlExpression);

                if (match.Success)
                {
                    string protocol = match.Groups[3].Value + match.Groups[11].Value;
                    string server = match.Groups[4].Value + match.Groups[12].Value;
                    string orgName = match.Groups[5].Value + match.Groups[15].Value;
                    string port = match.Groups[8].Value + match.Groups[14].Value;

                    atoms["Server"] = server;
                    atoms["Port"] = port;
                    atoms["UseSSL"] = protocol.Equals("https").ToString();
                    atoms["OrganizationName"] = orgName;
                }
            }

            Tag = !string.IsNullOrEmpty(element.GetAttribute("tag")) ? element.GetAttribute("tag") : "(unnamed)";
            IsDefault = element.GetAttribute("default").Equals(true.ToString());
            ServerUrl = atoms["Server"];
            Port = atoms["Port"];
            UseSSL = atoms["UseSSL"].Equals(true.ToString());
            UseIFD = element.GetAttribute("ifd").Equals(true.ToString());
            Domain = atoms["Domain"];
            Username = atoms["Username"];
            Password = atoms["Password"];
            OrganizationName = atoms["OrganizationName"];
        }
    }
}
