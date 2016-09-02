using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CemYabansu.PublishInCrm.Helpers
{
    public class ProfileManager
    {
        private string filePath;
        public List<ConnectionProfile> Profiles { get; set; }
        
        public int Count
        {
            get
            {
                return (Profiles != null) ? Profiles.Count : 0;
            }
        }
        public ConnectionProfile DefaultProfile
        {
            get
            {
                return Profiles.Find(p => p.IsDefault);
            }
        }

        public ProfileManager(string path)
        {
            Profiles = new List<ConnectionProfile>();
            filePath = path + "\\credential.xml";
            if (File.Exists(filePath))
            {
                var document = new XmlDocument();
                document.LoadXml(File.ReadAllText(filePath));
                foreach (XmlNode node in document.GetElementsByTagName("string"))
                {
                    var profile = new ConnectionProfile(node as XmlElement);
                    Profiles.Add(profile);
                }
            }
        }
        
        public void Add(ConnectionProfile profile)
        {
            var existingProfile = Get(profile.Tag);
            if (existingProfile != null)
            {
                int index = Profiles.IndexOf(existingProfile);
                Profiles.Remove(existingProfile);
                Profiles.Insert(index, profile);
            }
            else
            {
                Profiles.Add(profile);
            }
        }
        
        public ConnectionProfile Get(string tag)
        {
            return Profiles.Find(p => p.Tag.Equals(tag));
        }
        
        public void Remove(string tag)
        {
            var profile = Get(tag);

            if (profile != null)
                Profiles.Remove(profile);
        }

        public void SaveChanges()
        {
            SaveChanges(filePath);
        }

        public void SaveChanges(string otherFilePath)
        {
            var newDoc = new XmlDocument();

            var rootNode = newDoc.CreateElement("connectionString");
            newDoc.AppendChild(rootNode);

            var nameNode = newDoc.CreateElement("name");
            nameNode.InnerText = otherFilePath;
            rootNode.AppendChild(nameNode);

            foreach (var profile in Profiles)
            {
                var connectionStringNode = newDoc.CreateElement("string");
                connectionStringNode.InnerText = profile.ConnectionString;
                connectionStringNode.SetAttribute("tag", profile.Tag);
                connectionStringNode.SetAttribute("default", profile.IsDefault.ToString());
                connectionStringNode.SetAttribute("ifd", profile.UseIFD.ToString());
                rootNode.AppendChild(connectionStringNode);
            }

            newDoc.Save(otherFilePath);
        }

        public void Reload()
        {
            Profiles = new List<ConnectionProfile>();
            if (File.Exists(filePath))
            {
                var document = new XmlDocument();
                document.LoadXml(File.ReadAllText(filePath));
                foreach (XmlNode node in document.GetElementsByTagName("string"))
                {
                    var profile = new ConnectionProfile(node as XmlElement);
                    Profiles.Add(profile);
                }
            }
        }
    }
}
