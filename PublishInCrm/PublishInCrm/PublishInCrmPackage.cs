using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using CemYabansu.PublishInCrm.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.Crm.Sdk.Messages;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using OutputWindow = CemYabansu.PublishInCrm.Windows.OutputWindow;
using Thread = System.Threading.Thread;

namespace CemYabansu.PublishInCrm
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidPublishInCrmPkgString)]
    public sealed class PublishInCrmPackage : Package
    {
        private readonly string[] _expectedExtensions = { ".js", ".htm", ".html", ".css", ".png", ".jpg", ".jpeg", ".gif", ".xml" };

        private bool _error = false, _success = true;

        public PublishInCrmPackage()
        {
        }

        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this));
            base.Initialize();

            

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {

                // Create the command for the publish in crm.
                CommandID publishInCrmCommandID = new CommandID(GuidList.guidPublishInCrmCmdSet, (int)PkgCmdIDList.cmdidPublishInCrm);
                MenuCommand publishInCrm = new MenuCommand(PublishInCrmCallback, publishInCrmCommandID);
                mcs.AddCommand(publishInCrm);

                // Create the command for the publish in crm(solution explorer).
                CommandID publishInCrmMultipleCommandID = new CommandID(GuidList.guidPublishInCrmCmdSet, (int)PkgCmdIDList.cmdidPublishInCrmMultiple);
                MenuCommand publishInCrmMultiple = new MenuCommand(PublishInCrmMultipleCallback, publishInCrmMultipleCommandID);
                mcs.AddCommand(publishInCrmMultiple);

                CommandID publishToDefaultOrganizationCommandID = new CommandID(GuidList.guidPublishInCrmCmdSet, (int)PkgCmdIDList.cmdidPublishInDefaultOrganization);
                MenuCommand publishToDefaultOrganization = new MenuCommand(PublishInCrmCallback, publishToDefaultOrganizationCommandID);
                mcs.AddCommand(publishToDefaultOrganization);

                CommandID publishToDefaultOrganizationMultipleCommandID = new CommandID(GuidList.guidPublishInCrmCmdSet, (int)PkgCmdIDList.cmdidPublishInDefaultOrganizationMultiple);
                MenuCommand publishToDefaultOrganizationMultiple = new MenuCommand(PublishInCrmMultipleCallback, publishToDefaultOrganizationMultipleCommandID);
                mcs.AddCommand(publishToDefaultOrganizationMultiple);

                CommandID publishToCmd = new CommandID(GuidList.guidPublishInCrmCmdSet, (int)PkgCmdIDList.cmdidPublishIn);
                MenuCommand publishTo = new MenuCommand(PublishToCallback, publishToCmd);
                mcs.AddCommand(publishTo);

                CommandID publishToMultipleCmd = new CommandID(GuidList.guidPublishInCrmCmdSet, (int)PkgCmdIDList.cmdidPublishInMultiple);
                MenuCommand publishToMultiple = new MenuCommand(PublishToMultipleCallback, publishToMultipleCmd);
                mcs.AddCommand(publishToMultiple);

                CommandID manageConProfilesCmd = new CommandID(GuidList.guidPublishInCrmCmdSet, (int)PkgCmdIDList.cmdidManageConnectionProfiles);
                MenuCommand manageConProfiles = new MenuCommand(ManageConnectionProfilesCallback, manageConProfilesCmd);
                mcs.AddCommand(manageConProfiles);

                CommandID manageConProfilesMultipleCmd = new CommandID(GuidList.guidPublishInCrmCmdSet, (int)PkgCmdIDList.cmdidManageConnectionProfilesMultiple);
                MenuCommand manageConProfilesMultiple = new MenuCommand(ManageConnectionProfilesCallback, manageConProfilesMultipleCmd);
                mcs.AddCommand(manageConProfilesMultiple);
            }
        }

        private void PublishToCallback(object sender, EventArgs e)
        {
            PublishTo(false);
        }

        private void PublishToMultipleCallback(object sender, EventArgs e)
        {
            PublishTo(true);
        }

        private void PublishTo(bool isFromSolutionExplorer)
        {
            var window = new SelectOrganizationsWindow(GetSolutionPath());
            bool? publish = window.ShowDialog();
            if (publish.HasValue && publish.Value)
            {
                // Publish using each selected connection profile
                foreach (var profileItem in window.ProfileItems.Where(p => p.IsChecked))
                {
                    var profile = profileItem.Item;

                    // Publishing synchronously because the output window 
                    // is commonly used amongst the publish calls.
                    PublishInCrm(isFromSolutionExplorer, profile.ConnectionString);
                }

            }

        }

        private void ManageConnectionProfilesCallback(object sender, EventArgs e)
        {
            (new ManageConnectionProfilesWindow(GetSolutionPath())).ShowDialog();
        }

        private void PublishInCrmMultipleCallback(object sender, EventArgs e)
        {
            PublishInCrm(true);
        }

        private void PublishInCrmCallback(object sender, EventArgs e)
        {
            PublishInCrm(false);
        }

        private void PublishInCrm(bool isFromSolutionExplorer)
        {
            var outputWindow = new OutputWindow();
            outputWindow.Show();

            //getting selected files
            List<string> selectedFiles = GetSelectedFilesPath(isFromSolutionExplorer);

            //checking selected files extensions 
            var inValidFiles = CheckFilesExtension(selectedFiles);
            if (inValidFiles.Count > 0)
            {
                outputWindow.AddErrorText(string.Format("Invalid file extensions : {0}", string.Join(", ", inValidFiles)));
                outputWindow.AddErrorLineToTextBox(string.Format("Error : Invalid file extensions : \n\t- {0}", string.Join("\n\t- ", inValidFiles)));
                return;
            }

            //getting connection string
            // Get default connection string
            var solutionPath = GetSolutionPath();
            var connectionString = GetConnectionString(solutionPath);
            if (connectionString == string.Empty)
            {
                outputWindow.SetConnectionLabelText("Connection string was not provided.", _error);
                outputWindow.AddErrorLineToTextBox("Error : Connection string was not provided.");

                var userCredential = new ManageConnectionProfilesWindow(solutionPath);
                userCredential.ShowDialog();

                if (string.IsNullOrEmpty(userCredential.ConnectionString))
                {
                    outputWindow.SetConnectionLabelText("Connection failed.", _error);
                    outputWindow.AddErrorLineToTextBox("Error : Connection failed.");
                    return;
                }
                connectionString = userCredential.ConnectionString;
            }

            //updating/creating files one by one
            var thread = new Thread(o => UpdateWebResources(connectionString, selectedFiles, outputWindow));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private Thread PublishInCrm(bool isFromSolutionExplorer, string connectionString)
        {
            var outputWindow = new OutputWindow();
            outputWindow.Show();

            //getting selected files
            List<string> selectedFiles = GetSelectedFilesPath(isFromSolutionExplorer);

            //checking selected files extensions 
            var inValidFiles = CheckFilesExtension(selectedFiles);
            if (inValidFiles.Count > 0)
            {
                
                outputWindow.AddErrorText(string.Format("Invalid file extensions : {0}", string.Join(", ", inValidFiles)));
                outputWindow.AddErrorLineToTextBox(string.Format("Error : Invalid file extensions : \n\t- {0}", string.Join("\n\t- ", inValidFiles)));
                return null;
            }

            // Check connection string
            if (connectionString == string.Empty)
            {
                outputWindow.SetConnectionLabelText("Connection string was not provided.", _error);
                outputWindow.AddErrorLineToTextBox("Error : Connection string was not provided.");

                var userCredential = new ManageConnectionProfilesWindow(GetSolutionPath());
                userCredential.ShowDialog();

                if (string.IsNullOrEmpty(userCredential.ConnectionString))
                {
                    outputWindow.SetConnectionLabelText("Connection failed.", _error);
                    outputWindow.AddErrorLineToTextBox("Error : Connection failed.");
                    return null;
                }
                connectionString = userCredential.ConnectionString;
            }

            //updating/creating files one by one
            var thread = new Thread(o => UpdateWebResources(connectionString, selectedFiles, outputWindow));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return thread;
        }
        
        /// <returns>List of selected file or active file</returns>
        private List<string> GetSelectedFilesPath(bool isFromSolutionExplorer)
        {
            var dte = (DTE2)GetService(typeof(SDTE));
            if (isFromSolutionExplorer)
            {
                var selectedItems = dte.SelectedItems;
                List<string> list = new List<string>();
                foreach (SelectedItem selItem in selectedItems)
                {
                    try
                    {
                        selItem.ProjectItem.Save();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    list.Add(selItem.ProjectItem.FileNames[0]);
                }
                return list;
            }
            dte.ActiveDocument.Save();
            return new List<string> { dte.ActiveDocument.FullName };
        }

        /// <returns>List of invalid files according to _expectedExtensions</returns>
        private List<string> CheckFilesExtension(List<string> selectedFilesPaths)
        {
            var invalidFiles = new List<string>();
            for (var i = 0; i < selectedFilesPaths.Count; i++)
            {
                var selectedFileExtension = Path.GetExtension(selectedFilesPaths[i]);
                if (_expectedExtensions.All(t => t != selectedFileExtension))
                    invalidFiles.Add(Path.GetFileName(selectedFilesPaths[i]));
            }
            return invalidFiles;
        }

        private string GetSolutionPath()
        {
            var dte = (DTE2)GetService(typeof(SDTE));
            return dte.Solution.FullName;
        }

        /// <summary>
        /// Gets the closest "credential.xml" file to the current project.
        /// </summary>
        /// <returns>The full path of the "credential.xml" file.</returns>
        private string GetCredentialsFilePath()
        {
            string projectPath = GetSolutionPath();

            if (Path.HasExtension(projectPath))
                projectPath = Path.GetDirectoryName(projectPath);

            var filePath = projectPath + "\\credential.xml";

            while (!File.Exists(filePath))
            {
                projectPath = Directory.GetParent(projectPath).FullName;
                
                if (projectPath == Path.GetPathRoot(projectPath)) 
                    return string.Empty;
                
                filePath = projectPath + "\\credential.xml";
            }

            return filePath;
        }

        /// <summary>
        /// This function reads the projectPath\credential.xml file.
        /// Gets the connection string and return it. If it doesn't exist, returns String.Empty
        /// </summary>
        /// <param name="projectPath">Path of project file.</param>
        private string GetConnectionString(string projectPath)
        {
            if (Path.HasExtension(projectPath))
                projectPath = Path.GetDirectoryName(projectPath);

            var filePath = projectPath + "\\credential.xml";

            while (!File.Exists(filePath))
            {
                projectPath = Directory.GetParent(projectPath).FullName;
                if (projectPath == Path.GetPathRoot(projectPath)) return string.Empty;
                filePath = projectPath + "\\credential.xml";
            }

            var reader = new StreamReader
                (
                new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read)
                );
            var doc = new XmlDocument();
            var xmlIn = reader.ReadToEnd();
            reader.Close();

            try
            {
                doc.LoadXml(xmlIn);
            }
            catch (XmlException)
            {
                return string.Empty;
            }

            var nodes = doc.GetElementsByTagName("string");
            foreach (XmlNode value in nodes)
            {
                var reStr = value.ChildNodes[0].Value;
                return reStr;
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets available connection strings from the closest "credential.xml" file.
        /// </summary>
        /// <returns>A dictionary of connection strings identified by tags.</returns>
        private Dictionary<string, string> GetAvailableConnectionStrings()
        {
            var connectionStrings = new Dictionary<string, string>();
            try
            {
                XmlDocument credentialsXml = new XmlDocument();
                credentialsXml.LoadXml(File.ReadAllText(GetCredentialsFilePath()));

                foreach(XmlNode node in credentialsXml.GetElementsByTagName("string")) {

                    string tag = node.Attributes["tag"].Value;
                    string connectionString = node.ChildNodes[0].Value;

                    connectionStrings.Add(tag, connectionString);
                }
            }
            catch
            {
                // TODO: Add an error logging & handling mechanism.
                
                // Replacing connection strings dictionary with an empty one.
                connectionStrings = new Dictionary<string, string>();
            }

            return connectionStrings;
        }

        private void UpdateWebResources(string connectionString, List<string> selectedFiles, OutputWindow outputWindow)
        {
            try
            {
                var toBePublishedWebResources = new List<WebResource>();
                OrganizationService orgService;
                var crmConnection = CrmConnection.Parse(connectionString);
                // To escape "another assembly" exception
                crmConnection.ProxyTypesAssembly = Assembly.GetExecutingAssembly();
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                using (orgService = new OrganizationService(crmConnection))
                {

                    outputWindow.SetConnectionLabelText(string.Format("Connected to : {0}", crmConnection.ServiceUri), _success);
                    outputWindow.AddLineToTextBox(string.Format("Connected to : {0}", crmConnection.ServiceUri));

                    Dictionary<string, WebResource> toBeCreatedList;
                    Dictionary<string, WebResource> toBeUpdatedList;

                    GetWebresources(orgService, selectedFiles, out toBeCreatedList, out toBeUpdatedList, outputWindow);

                    CreateWebresources(toBeCreatedList, orgService, toBePublishedWebResources, outputWindow);

                    UpdateWebresources(toBeUpdatedList, orgService, toBePublishedWebResources, outputWindow);

                    PublishWebResources(orgService, toBePublishedWebResources, outputWindow);
                }
                stopwatch.Stop();
                outputWindow.AddLineToTextBox(string.Format("Time : {0}", stopwatch.Elapsed));
            }
            catch (Exception ex)
            {
                outputWindow.AddErrorText(ex.Message);
                outputWindow.AddErrorLineToTextBox("Error : " + ex.Message);
            }
        }

        private void UpdateWebresources(Dictionary<string, WebResource> toBeUpdatedList, OrganizationService orgService,
            List<WebResource> toBePublishedWebResources, OutputWindow outputWindow)
        {
            if (toBeUpdatedList.Count > 0)
            {
                outputWindow.StartUpdating();
                foreach (var toBeUpdated in toBeUpdatedList)
                {
                    outputWindow.AddLineToTextBox(string.Format("Updating to webresource({0}) ..", Path.GetFileName(toBeUpdated.Key)));
                    UpdateWebResource(orgService, toBeUpdated.Value, toBeUpdated.Key);
                    outputWindow.AddLineToTextBox(string.Format("{0} is updated.", toBeUpdated.Value.Name));
                    toBePublishedWebResources.Add(toBeUpdatedList[toBeUpdated.Key]);
                }
                outputWindow.FinishUpdating(_success);
            }
        }

        private void CreateWebresources(Dictionary<string, WebResource> toBeCreatedList, OrganizationService orgService,
            List<WebResource> toBePublishedWebResources, OutputWindow outputWindow)
        {
            if (toBeCreatedList.Count > 0)
            {
                outputWindow.StartCreating();
                List<string> keys = new List<string>(toBeCreatedList.Keys);
                foreach (var key in keys)
                {
                    outputWindow.AddLineToTextBox(string.Format("Creating new webresource({0})..", Path.GetFileName(key)));
                    toBeCreatedList[key] = CreateWebResource(Path.GetFileName(key), orgService, key);
                    if (toBeCreatedList[key] == null)
                    {
                        outputWindow.AddLineToTextBox(string.Format("Creating new webresource({0}) is cancelled.", Path.GetFileName(key)));
                        continue;
                    }
                    outputWindow.AddLineToTextBox(string.Format("{0} is created.", Path.GetFileName(key)));
                    toBePublishedWebResources.Add(toBeCreatedList[key]);
                }
                outputWindow.FinishCreating(_success);
            }
        }

        private void GetWebresources(OrganizationService orgService, List<string> selectedFiles, out Dictionary<string, WebResource> toBeCreatedList, out Dictionary<string, WebResource> toBeUpdatedList, OutputWindow outputWindow)
        {
            outputWindow.StartGettingWebresources();
            toBeCreatedList = new Dictionary<string, WebResource>();
            toBeUpdatedList = new Dictionary<string, WebResource>();
            for (int i = 0; i < selectedFiles.Count; i++)
            {
                var fileName = Path.GetFileName(selectedFiles[i]);
                var chosenWebResource = GetWebresource(orgService, fileName);
                if (chosenWebResource == null)
                {
                    outputWindow.AddErrorLineToTextBox(string.Format("Error : {0} is not exist in CRM.", fileName));
                    toBeCreatedList.Add(selectedFiles[i], null);
                }
                else
                {
                    toBeUpdatedList.Add(selectedFiles[i], chosenWebResource);
                }
            }
            outputWindow.FinishGettingWebresources(_success);
        }

        /// <returns>Webresource which has equal name with "filename" which with or without extension</returns>
        private WebResource GetWebresource(OrganizationService orgService, string filename)
        {
            var webresourceResult = WebresourceResult(orgService, filename);
            if (webresourceResult.Entities.Count == 0)
            {
                filename = Path.GetFileNameWithoutExtension(filename);
                webresourceResult = WebresourceResult(orgService, filename);
                if (webresourceResult.Entities.Count == 0)
                    return null;
            }

            return new WebResource()
            {
                Name = webresourceResult[0].GetAttributeValue<string>("name"),
                DisplayName = webresourceResult[0].GetAttributeValue<string>("displayname"),
                Id = webresourceResult[0].GetAttributeValue<Guid>("webresourceid")
            };
        }

        private WebResource CreateWebResource(string fileName, OrganizationService orgService, string filePath)
        {
            var createWebresoruce = new CreateWebResourceWindow(fileName);
            createWebresoruce.ShowDialog();

            if (createWebresoruce.CreatedWebResource == null)
                return null;

            var createdWebresource = createWebresoruce.CreatedWebResource;
            createdWebresource.Content = GetEncodedFileContents(filePath);
            createdWebresource.Id = orgService.Create(createdWebresource);
            return createdWebresource;
        }

        private void UpdateWebResource(OrganizationService orgService, WebResource choosenWebresource, string selectedFile)
        {
            choosenWebresource.Content = GetEncodedFileContents(selectedFile);
            var updateRequest = new UpdateRequest
            {
                Target = choosenWebresource
            };
            orgService.Execute(updateRequest);
        }

        private void PublishWebResources(OrganizationService orgService, List<WebResource> toBePublishedWebResources, OutputWindow outputWindow)
        {
            if (toBePublishedWebResources.Count < 1)
            {
                outputWindow.FinishPublishing(_error, "There is no webresource to publish.");
                outputWindow.AddLineToTextBox("There is no webresource to publish.");
                return;
            }

            outputWindow.StartPublishing();
            var webResourcesString = "";
            foreach (var webResource in toBePublishedWebResources)
                webResourcesString = webResourcesString + string.Format("<webresource>{0}</webresource>", webResource.Id);

            var prequest = new PublishXmlRequest
            {
                ParameterXml = string.Format("<importexportxml><webresources>{0}</webresources></importexportxml>", webResourcesString)
            };
            orgService.Execute(prequest);
            outputWindow.FinishPublishing(_success, null);

            var webResourcesNames = new string[toBePublishedWebResources.Count];
            for (var i = 0; i < toBePublishedWebResources.Count; i++)
            {
                webResourcesNames[i] = toBePublishedWebResources[i].Name;
            }
            outputWindow.AddLineToTextBox(string.Format("Published webresources : \n\t- {0}", string.Join("\n\t- ", webResourcesNames)));
        }

        private static EntityCollection WebresourceResult(OrganizationService orgService, string filename)
        {
            string fetchXml = string.Format(@"<fetch mapping='logical' version='1.0' >
                            <entity name='webresource' >
                                <attribute name='webresourceid' />
                                <attribute name='name' />
                                <attribute name='displayname' />
                                <filter type='and' >
                                    <condition attribute='name' operator='eq' value='{0}' />
                                </filter>
                            </entity>
                        </fetch>", filename);

            QueryBase query = new FetchExpression(fetchXml);

            var webresourceResult = orgService.RetrieveMultiple(query);
            return webresourceResult;
        }

        public string GetEncodedFileContents(string pathToFile)
        {
            var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read);
            byte[] binaryData = new byte[fs.Length];
            fs.Read(binaryData, 0, (int)fs.Length);
            fs.Close();
            return Convert.ToBase64String(binaryData, 0, binaryData.Length);
        }
    }
}