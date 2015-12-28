using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using CemYabansu.PublishInCrm.Controls;

namespace CemYabansu.PublishInCrm.Windows
{
    /// <summary>
    /// Interaction logic for SelectOrganizationsWindow.xaml
    /// </summary>
    public partial class SelectOrganizationsWindow
    {

        public ObservableCollection<CheckedListItem<ConnectionProfile>> ProfileItems { get; set; }

        public SelectOrganizationsWindow(string path)
        {
            InitializeComponent();

            // Amateur solution explorer / editor hack
            if (path.EndsWith(".sln"))
                path = System.IO.Path.GetDirectoryName(path);

            GetProfiles(path);

            DataContext = this;
        }

        private void GetProfiles(string path)
        {
            // TODO: Need a helper class for this type of XML actions.
            string filePath = path + "\\credential.xml";
            if (File.Exists(filePath))
            {
                ProfileItems = new ObservableCollection<CheckedListItem<ConnectionProfile>>();
                var document = new XmlDocument();
                document.LoadXml(File.ReadAllText(filePath));
                foreach (XmlNode node in document.GetElementsByTagName("string"))
                {
                    var profile = ParseElement(node as XmlElement);
                    ProfileItems.Add(new CheckedListItem<ConnectionProfile>(profile));
                }
            }
        }

        private ConnectionProfile ParseElement(XmlElement element)
        {
            string connectionString = element.InnerText;
            Dictionary<string, string> atoms = new Dictionary<string, string>();
            
            foreach (string molecule in connectionString.Split(';'))
            {
                string [] atom = molecule.Split('=');
                atoms.Add(atom[0].Trim(), atom[1].Trim());
            }

            atoms.Add("Port", "");
            atoms.Add("UseSSL", "");
            atoms.Add("OrganizationName", "");

            if(atoms.ContainsKey("Server")) 
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
                
                if(match.Success) {
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

            return new ConnectionProfile
            {
                Tag = !string.IsNullOrEmpty(element.GetAttribute("tag")) ? element.GetAttribute("tag") : "(unnamed)",
                IsDefault = element.GetAttribute("default").Equals(true.ToString()),
                ServerUrl = atoms["Server"],
                Port = atoms["Port"],
                UseSSL = atoms["UseSSL"].Equals(true.ToString()),
                Domain = atoms["Domain"],
                Username = atoms["Username"],
                Password = atoms["Password"],
                OrganizationName = atoms["OrganizationName"]
            };
        }

        private void ToggleSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileItems.Count(p => !p.IsChecked) > 0)
            {
                foreach (var item in ProfileItems)
                {
                    item.IsChecked = true;
                }
            }
            else
            {
                foreach (var item in ProfileItems)
                {
                    item.IsChecked = false;
                }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Check all profile items, if every profile item is checked, rename the toggle button to "Clear All". Otherwise, "Select All".
            ToggleSelectionButton.Content = (ProfileItems.Count(p => !p.IsChecked) > 0) ? "Select All" : "Clear All";
            PublishButton.IsEnabled = ProfileItems.Count(p => p.IsChecked) > 0;
        }

        private void PublishButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

    }
}
