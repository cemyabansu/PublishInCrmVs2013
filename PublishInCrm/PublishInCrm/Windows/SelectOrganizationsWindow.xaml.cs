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
using CemYabansu.PublishInCrm.Helpers;

namespace CemYabansu.PublishInCrm.Windows
{
    /// <summary>
    /// Interaction logic for SelectOrganizationsWindow.xaml
    /// </summary>
    public partial class SelectOrganizationsWindow
    {
        private ProfileManager ProfileManager { get; set; }

        public ObservableCollection<CheckedListItem<ConnectionProfile>> ProfileItems { get; set; }

        public SelectOrganizationsWindow(string path)
        {
            InitializeComponent();

            // Amateur solution explorer / editor hack
            if (path.EndsWith(".sln"))
                path = System.IO.Path.GetDirectoryName(path);

            ProfileManager = new ProfileManager(path);
            ProfileItems = new ObservableCollection<CheckedListItem<ConnectionProfile>>();

            foreach (var profile in ProfileManager.Profiles)
            {
                ProfileItems.Add(new CheckedListItem<ConnectionProfile>(profile));
            }

            DataContext = this;
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
