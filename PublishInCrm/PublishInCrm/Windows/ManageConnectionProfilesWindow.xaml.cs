using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Description;
using System.Threading;
using System.Windows;
using System.Xml;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using System.Threading.Tasks;
using System.Windows.Controls;
using Task = System.Threading.Tasks.Task;
using System.Text.RegularExpressions;
using CemYabansu.PublishInCrm.Helpers;

//For login, I used microsoft's code which is in CRM SDK.

namespace CemYabansu.PublishInCrm.Windows
{
    public partial class ManageConnectionProfilesWindow
    {
        private ProfileManager ProfileManager { get; set; }
        private ConnectionProfile SelectedProfile { get; set; }
        private Dictionary<string, string> _organizationsDictionary;
        public string ConnectionString { get; set; }

        private string _projectPath;
        private string _savePath;
        private string _username;
        private string _password;
        private string _domain;
        private string _serverUrl;
        private string _portNumber;
        private bool _isSsl;

        public ManageConnectionProfilesWindow(string solutionPath)
        {
            InitializeComponent();

            _projectPath = solutionPath;
            _savePath = (_projectPath.EndsWith(".sln")) ? Path.GetDirectoryName(solutionPath) : solutionPath;

            ProfileManager = new ProfileManager(_savePath);

            InitializeProfileList();
        }

        private void InitializeProfileList()
        {
            // Refill the combobox
            ConnectionStringCombobox.Items.Clear();
            foreach (var profile in ProfileManager.Profiles)
            {
                ConnectionStringCombobox.Items.Add(profile.Tag);
            }

            if (ProfileManager.DefaultProfile != null)
            {
                ConnectionStringCombobox.SelectedItem = ProfileManager.DefaultProfile.Tag;
                InitializeInputs(ProfileManager.DefaultProfile);
            }
        }

        private void InitializeInputs(ConnectionProfile profile)
        {
            DefaultProfileCheckBox.IsChecked = profile.IsDefault;
            ServerTextBox.Text = profile.ServerUrl;
            PortTextBox.Text = profile.Port;
            SslCheckBox.IsChecked = profile.UseSSL;
            IfdCheckBox.IsChecked = profile.UseIFD;
            DomainTextBox.Text = profile.Domain;
            UsernameTextBox.Text = profile.Username;
            PasswordTextBox.Password = profile.Password;

            OrganizationsComboBox.Items.Clear();
            if (profile.OrganizationName != null)
            {
                OrganizationsComboBox.Items.Add(profile.OrganizationName);
                OrganizationsComboBox.SelectedItem = profile.OrganizationName;
            }

            // Editing an existing profile, we should be able to save.
            SaveButton.IsEnabled = true;

            SelectedProfile = profile;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update the profile and save changes.
            Update(SelectedProfile);
            ProfileManager.SaveChanges();

            // Backup the selected profile tag
            string tag = SelectedProfile.Tag;

            InitializeProfileList();

            // Re-select the profile.
            ConnectionStringCombobox.SelectedItem = tag;
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            //System.Threading.Tasks.Task.Factory.StartNew(() => SaveConnectionString(selectedOrganizationUrl, selectedConnectionStringTag, isDefault));            
            System.Threading.Tasks.Task.Factory.StartNew(() => SaveConnectionString());
        }

        private void Update(ConnectionProfile profile)
        {
            profile.Tag = !string.IsNullOrEmpty(ConnectionStringCombobox.Text) ? ConnectionStringCombobox.Text : profile.Tag;
            profile.IsDefault = DefaultProfileCheckBox.IsChecked ?? false;
            profile.ServerUrl = ServerTextBox.Text;
            profile.Port = PortTextBox.Text;
            profile.UseSSL = SslCheckBox.IsChecked ?? false;
            profile.UseIFD = IfdCheckBox.IsChecked ?? false;
            profile.Domain = DomainTextBox.Text;
            profile.Username = UsernameTextBox.Text;
            profile.Password = PasswordTextBox.Password;
        }

        private async void SaveConnectionString()
        {
            SetEnableToUIElement(SaveButton, false);
            SetActivateToConnectionProgressRing(true);
            SetConnectionStatus("Testing connection..");

            var result = await System.Threading.Tasks.Task.FromResult(TestConnection(SelectedProfile.ConnectionString));

            SetActivateToConnectionProgressRing(false);
            if (!result)
            {
                SetConnectionStatus("Connection failed.");
                return;
            }

            //ProfileManager.SaveChanges();

            Dispatcher.Invoke(Close);
        }

        private async void SaveConnectionString(string selectedOrganizationUrl, string connectionStringTag, bool isDefault)
        {
            SetEnableToUIElement(SaveButton, false);
            var connectionString = string.Format("Server={0}; Domain={1}; Username={2}; Password={3}",
                                                    selectedOrganizationUrl, _domain, _username, _password);

            SetActivateToConnectionProgressRing(true);
            SetConnectionStatus("Testing connection..");

            var result = await System.Threading.Tasks.Task.FromResult(TestConnection(connectionString));

            SetActivateToConnectionProgressRing(false);
            if (!result)
            {
                SetConnectionStatus("Connection failed.");
                return;
            }

            WriteConnectionStringToFile(connectionStringTag, ConnectionString, _savePath, isDefault);
            Dispatcher.Invoke(Close);
        }

        private void WriteConnectionStringToFile(string connectionStringTag, string connectionString, string path, bool isDefault)
        {
            string filePath = path + "\\credential.xml";

            var newDoc = new XmlDocument();

            var rootNode = newDoc.CreateElement("connectionString");
            newDoc.AppendChild(rootNode);

            var nameNode = newDoc.CreateElement("name");
            nameNode.InnerText = _projectPath;
            rootNode.AppendChild(nameNode);

            // Append the active connection string first.
            var connectionStringNode = newDoc.CreateElement("string");
            connectionStringNode.InnerText = connectionString;
            connectionStringNode.SetAttribute("tag", connectionStringTag);
            connectionStringNode.SetAttribute("default", isDefault.ToString());
            rootNode.AppendChild(connectionStringNode);

            // If the credential file exists, append the other profiles as well.
            if (File.Exists(filePath))
            {
                var existingDoc = new XmlDocument();
                existingDoc.LoadXml(File.ReadAllText(filePath));
                foreach (XmlNode node in existingDoc.GetElementsByTagName("string"))
                {
                    // Skip the active profile as it's already been appended.
                    if (node.Attributes["tag"] != null && node.Attributes["tag"].Value != connectionStringTag)
                    {
                        // If the one we are editing is the default profile, we must set the default properties of other profiles to false.
                        if (isDefault)
                        {
                            ((XmlElement)node).SetAttribute("default", false.ToString());
                        }
                        var importedNode = newDoc.ImportNode(node, true);
                        rootNode.AppendChild(importedNode);
                    }
                }
            }

            newDoc.Save(filePath);
        }

        public bool TestConnection(string server, string domain, string username, string password)
        {
            var connectionString = string.Format("Server={0}; Domain={1}; Username={2}; Password={3}",
                                                    server, domain, username, password);
            return TestConnection(connectionString);
        }

        public bool TestConnection(string connectionString)
        {
            SetActivateToConnectionProgressRing(true);
            try
            {
                var crmConnection = CrmConnection.Parse(connectionString);
                //to escape "another assembly" exception
                crmConnection.ProxyTypesAssembly = Assembly.GetExecutingAssembly();
                OrganizationService orgService;
                using (orgService = new OrganizationService(crmConnection))
                {
                    orgService.Execute(new WhoAmIRequest());
                    ConnectionString = connectionString;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void GetOrganizationsButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareUiForGetOrganizations();

            System.Threading.Tasks.Task.Factory.StartNew(GetOrganizationsTask);

            CheckDefaultOrganization();
        }

        private void PrepareUiForGetOrganizations()
        {
            SetConnectionStatus("");
            SetActivateToProgressRing(true);

            _serverUrl = ServerTextBox.Text;
            _portNumber = PortTextBox.Text;
            _isSsl = (bool)SslCheckBox.IsChecked;
            _username = UsernameTextBox.Text;
            _password = PasswordTextBox.Password;
            _domain = DomainTextBox.Text;

            OrganizationsComboBox.Items.Clear();
            OrganizationsComboBox.SelectedIndex = -1;
            OrganizationsComboBox.IsEnabled = false;
            SaveButton.IsEnabled = false;
            GetOrganizationsButton.IsEnabled = false;
        }

        private async void GetOrganizationsTask()
        {
            var result = await System.Threading.Tasks.Task.FromResult(GetOrganizations());


            SetEnableToUIElement(SaveButton, result.Item1);
            EnableComboBox(result.Item1);
            SetEnableToUIElement(GetOrganizationsButton, true);
            SetConnectionStatus(result.Item2);
        }

        private Tuple<bool, string> GetOrganizations()
        {
            try
            {
                //creating discovery url
                var serverUrl = _serverUrl;
                if (!serverUrl.StartsWith("http"))
                {
                    serverUrl = string.Format("{0}{1}", _isSsl ? "https://" : "http://", serverUrl);
                }
                var portNumber = string.IsNullOrWhiteSpace(_portNumber) ? "" : ":" + _portNumber;
                var discoveryUri = new Uri(string.Format("{0}{1}/XrmServices/2011/Discovery.svc", serverUrl, portNumber));

                //getting organizations with 10 seconds timeout
                OrganizationDetailCollection orgs = new OrganizationDetailCollection();
                object monitorSync = new object();
                Action longMethod = () => GetOrganizationCollection(monitorSync, discoveryUri, out orgs);
                bool timedOut;
                lock (monitorSync)
                {
                    longMethod.BeginInvoke(null, null);
                    timedOut = !Monitor.Wait(monitorSync, TimeSpan.FromSeconds(15)); // waiting 15 secs
                }
                if (timedOut)
                {
                    return Tuple.Create(false, "Error : Timeout(15 s)");
                }
                if (orgs == null)
                {
                    return Tuple.Create(false, "Error : Organization not found.");
                }
                _organizationsDictionary = new Dictionary<string, string>();
                foreach (var org in orgs)
                {
                    AddItemToComboBox(org.FriendlyName);
                    _organizationsDictionary.Add(org.FriendlyName, org.Endpoints[EndpointType.WebApplication]);
                }

                return Tuple.Create(true, "Successfully connected.");
            }
            catch (Exception)
            {
                return Tuple.Create(false, "Error : Connection failed.");
            }
        }

        private void CheckDefaultOrganization()
        {
            var selectedProfileName = ConnectionStringCombobox.Text;
            var selectedProfile = ProfileManager.Profiles.Find(p => p.Tag.Equals(selectedProfileName));

            if (selectedProfile != null)
            {
                if (!string.IsNullOrEmpty(selectedProfile.OrganizationName))
                {
                    for (int i = 0; i < OrganizationsComboBox.Items.Count; i++)
                    {
                        if ((OrganizationsComboBox.Items[i] ?? string.Empty).ToString() == selectedProfile.OrganizationName)
                        {
                            OrganizationsComboBox.SelectedIndex = i;
                        }
                    }
                }
            }
        }

        private void GetOrganizationCollection(object monitorSync, Uri discoveryUri, out OrganizationDetailCollection orgs)
        {
            IServiceManagement<IDiscoveryService> serviceManagement;
            try
            {
                serviceManagement = ServiceConfigurationFactory.CreateManagement<IDiscoveryService>(discoveryUri);
            }
            catch (Exception)
            {
                orgs = null;
                return;
            }
            AuthenticationProviderType endpointType = serviceManagement.AuthenticationType;

            AuthenticationCredentials authCredentials = GetCredentials(serviceManagement, endpointType);

            using (DiscoveryServiceProxy discoveryProxy =
                    GetProxy<IDiscoveryService, DiscoveryServiceProxy>(serviceManagement, authCredentials))
            {
                orgs = DiscoverOrganizations(discoveryProxy);
            }
            lock (monitorSync)
            {
                Monitor.Pulse(monitorSync);
            }
        }


        private OrganizationDetailCollection DiscoverOrganizations(IDiscoveryService service)
        {
            if (service == null) throw new ArgumentNullException("service");
            RetrieveOrganizationsRequest orgRequest = new RetrieveOrganizationsRequest();
            RetrieveOrganizationsResponse orgResponse =
                (RetrieveOrganizationsResponse)service.Execute(orgRequest);

            return orgResponse.Details;
        }

        private TProxy GetProxy<TService, TProxy>(IServiceManagement<TService> serviceManagement,
    AuthenticationCredentials authCredentials)
            where TService : class
            where TProxy : ServiceProxy<TService>
        {
            Type classType = typeof(TProxy);

            if (serviceManagement.AuthenticationType !=
                AuthenticationProviderType.ActiveDirectory)
            {
                AuthenticationCredentials tokenCredentials =
                    serviceManagement.Authenticate(authCredentials);
                // Obtain discovery/organization service proxy for Federated, LiveId and OnlineFederated environments. 
                // Instantiate a new class of type using the 2 parameter constructor of type IServiceManagement and SecurityTokenResponse.
                return (TProxy)classType
                    .GetConstructor(new Type[] { typeof(IServiceManagement<TService>), typeof(SecurityTokenResponse) })
                    .Invoke(new object[] { serviceManagement, tokenCredentials.SecurityTokenResponse });
            }

            // Obtain discovery/organization service proxy for ActiveDirectory environment.
            // Instantiate a new class of type using the 2 parameter constructor of type IServiceManagement and ClientCredentials.
            return (TProxy)classType
                .GetConstructor(new Type[] { typeof(IServiceManagement<TService>), typeof(ClientCredentials) })
                .Invoke(new object[] { serviceManagement, authCredentials.ClientCredentials });
        }


        //<snippetAuthenticateWithNoHelp2>
        /// <summary>
        /// Obtain the AuthenticationCredentials based on AuthenticationProviderType.
        /// </summary>
        /// <param name="service">A service management object.</param>
        /// <param name="endpointType">An AuthenticationProviderType of the CRM environment.</param>
        /// <returns>Get filled credentials.</returns>
        private AuthenticationCredentials GetCredentials<TService>(IServiceManagement<TService> service, AuthenticationProviderType endpointType)
        {
            AuthenticationCredentials authCredentials = new AuthenticationCredentials();

            switch (endpointType)
            {
                case AuthenticationProviderType.ActiveDirectory:
                    authCredentials.ClientCredentials.Windows.ClientCredential =
                        new NetworkCredential(_username, _password, _domain);
                    break;
                case AuthenticationProviderType.LiveId:
                    authCredentials.ClientCredentials.UserName.UserName = _username;
                    authCredentials.ClientCredentials.UserName.Password = _password;
                    authCredentials.SupportingCredentials = new AuthenticationCredentials
                    {
                        ClientCredentials = Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice()
                    };
                    break;
                default: // For Federated and OnlineFederated environments.                    
                    authCredentials.ClientCredentials.UserName.UserName = _username;
                    authCredentials.ClientCredentials.UserName.Password = _password;
                    // For OnlineFederated single-sign on, you could just use current UserPrincipalName instead of passing user name and password.
                    // authCredentials.UserPrincipalName = UserPrincipal.Current.UserPrincipalName;  // Windows Kerberos

                    // The service is configured for User Id authentication, but the user might provide Microsoft
                    // account credentials. If so, the supporting credentials must contain the device credentials.
                    if (endpointType == AuthenticationProviderType.OnlineFederation)
                    {
                        IdentityProvider provider = service.GetIdentityProvider(authCredentials.ClientCredentials.UserName.UserName);
                        if (provider != null && provider.IdentityProviderType == IdentityProviderType.LiveId)
                        {
                            authCredentials.SupportingCredentials = new AuthenticationCredentials();
                            authCredentials.SupportingCredentials.ClientCredentials =
                                Microsoft.Crm.Services.Utility.DeviceIdManager.LoadOrRegisterDevice();
                        }
                    }
                    break;
            }

            return authCredentials;
        }

        public void SetConnectionStatus(string text)
        {
            Dispatcher.Invoke(() => ConnectionStatusLabel.Content = text);
            SetActivateToProgressRing(false);
        }

        public void SetActivateToProgressRing(bool isActive)
        {
            Dispatcher.Invoke(() => ProgressRing.IsActive = isActive);
        }

        public void SetActivateToConnectionProgressRing(bool isActive)
        {
            Dispatcher.Invoke(() => ConnectionProgressRing.IsActive = isActive);
        }

        public void AddItemToComboBox(string item)
        {
            Dispatcher.Invoke(() => OrganizationsComboBox.Items.Add(item));
        }

        private void EnableComboBox(bool isEnable)
        {
            Dispatcher.Invoke(() => OrganizationsComboBox.SelectedIndex = isEnable ? 0 : -1);
            Dispatcher.Invoke(() => OrganizationsComboBox.IsEnabled = isEnable);
        }

        private void SetEnableToUIElement(UIElement button, bool isEnable)
        {
            Dispatcher.Invoke(() => button.IsEnabled = isEnable);
        }

        private void ConnectionStringCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tag = (ConnectionStringCombobox.SelectedItem ?? string.Empty).ToString();

            var profile = ProfileManager.Get(tag);

            if (profile != null)
            {
                InitializeInputs(profile);
            }
        }

        private void IfdCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SslCheckBox.IsChecked = true;
            SslCheckBox.IsEnabled = false;
        }

        private void IfdCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SslCheckBox.IsEnabled = true;
        }

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            var profile = new ConnectionProfile()
            {
                Tag = "New Profile"
            };

            ProfileManager.Profiles.Add(profile);
            ConnectionStringCombobox.Items.Add(profile.Tag);
            ConnectionStringCombobox.SelectedItem = profile.Tag;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string) ConnectionStringCombobox.SelectedItem;

            ProfileManager.Remove(tag);
            ConnectionStringCombobox.Items.Remove(tag);
            if (ProfileManager.Count > 0)
            {
                var profile = ProfileManager.Profiles.Last();
                ConnectionStringCombobox.SelectedItem = profile.Tag;
                InitializeInputs(profile);
            }
        }
    }
}