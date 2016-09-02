using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;

namespace CemYabansu.PublishInCrm.Windows
{
    public partial class OutputWindow
    {
        private enum CurrentStatus
        {
            Connection,
            GettingWebresources,
            CreatingWebresources,
            UpdatingWebresources,
            Publishing
        }

        private static string _errorImagePath = @"..\Resources\error.png";
        private static string _doneImagePath = @"..\Resources\done.png";

        public OutputWindow()
        {
            InitializeComponent();
        }

        private CurrentStatus _currentStatus;

        public void AddLineToTextBox(string text)
        {
            OutputTextBox.Dispatcher.Invoke(() => AddNewLine(text));
        }

        public void AddErrorLineToTextBox(string errorMessage)
        {
            OutputTextBox.Dispatcher.Invoke(() => AddNewLine(errorMessage));
        }

        private void AddNewLine(string text)
        {
            OutputTextBox.AppendText(text + Environment.NewLine);
        }

        public void SetConnectionLabelText(string text, bool isSucceed)
        {
            SetUiElementEnabled(ConnectionLabel, true);
            SetLabelText(ConnectionLabel, text);
            var uri = new Uri(isSucceed ? _doneImagePath : _errorImagePath, UriKind.RelativeOrAbsolute);
            SetImageSourceToImage(ConnectionImage, uri);
        }

        private void SetImageSourceToImage(Image image, Uri uri)
        {
            Dispatcher.Invoke(() => image.Source = new BitmapImage(uri));
        }

        public void StartUpdating()
        {
            _currentStatus = CurrentStatus.UpdatingWebresources;
            SetActivityToProgressRing(UpdateProgressRing, true);
            SetUiElementEnabled(UpdateLabel, true);
        }

        public void FinishUpdating(bool isSucceed)
        {
            SetUiElementVisibility(UpdateProgressRing, Visibility.Collapsed);
            SetUiElementVisibility(UpdateImage, Visibility.Visible);
            var uri = new Uri(isSucceed ? _doneImagePath : _errorImagePath, UriKind.RelativeOrAbsolute);
            SetImageSourceToImage(UpdateImage, uri);
        }

        public void StartGettingWebresources()
        {
            _currentStatus = CurrentStatus.GettingWebresources;
            SetActivityToProgressRing(GettingWebresourcesProgressRing, true);
            SetUiElementEnabled(GettingWebresourcesLabel, true);
        }

        public void FinishGettingWebresources(bool isSucceed)
        {
            SetUiElementVisibility(GettingWebresourcesProgressRing, Visibility.Collapsed);
            SetUiElementVisibility(GettingWebresourcesImage, Visibility.Visible);
            var uri = new Uri(isSucceed ? _doneImagePath : _errorImagePath, UriKind.RelativeOrAbsolute);
            SetImageSourceToImage(GettingWebresourcesImage, uri);
        }

        public void StartCreating()
        {
            _currentStatus = CurrentStatus.CreatingWebresources;
            SetActivityToProgressRing(CreateProgressRing, true);
            SetUiElementEnabled(CreateLabel, true);
        }

        public void FinishCreating(bool isSucceed)
        {
            SetUiElementVisibility(CreateProgressRing, Visibility.Collapsed);
            SetUiElementVisibility(CreateImage, Visibility.Visible);
            var uri = new Uri(isSucceed ? _doneImagePath : _errorImagePath, UriKind.RelativeOrAbsolute);
            SetImageSourceToImage(CreateImage, uri);
        }

        public void StartPublishing()
        {
            _currentStatus = CurrentStatus.Publishing;
            SetActivityToProgressRing(PublishProgressRing, true);
            SetUiElementEnabled(PublishLabel, true);
        }

        public void FinishPublishing(bool isSucceed, string text)
        {
            if (!string.IsNullOrEmpty(text)) SetLabelText(PublishLabel, text);
            SetUiElementVisibility(PublishProgressRing, Visibility.Collapsed);
            SetUiElementVisibility(PublishImage, Visibility.Visible);
            var uri = new Uri(isSucceed ? _doneImagePath : _errorImagePath, UriKind.RelativeOrAbsolute);
            SetImageSourceToImage(PublishImage, uri);
        }

        public void AddErrorText(string message)
        {
            SetUiElementVisibility(ErrorImage, Visibility.Visible);
            SetUiElementVisibility(ErrorLabel, Visibility.Visible);
            SetLabelText(ErrorLabel, message);
            SetErrorToCurrentProcess();
        }

        private void SetErrorToCurrentProcess()
        {
            var uri = new Uri(_errorImagePath, UriKind.RelativeOrAbsolute);
            switch (_currentStatus)
            {
                case CurrentStatus.Connection:
                    SetImageSourceToImage(ConnectionImage, uri);
                    SetUiElementVisibility(ConnectionImage, Visibility.Visible);
                    break;
                case CurrentStatus.GettingWebresources:
                    SetImageSourceToImage(GettingWebresourcesImage, uri);
                    SetUiElementVisibility(GettingWebresourcesImage, Visibility.Visible);
                    SetUiElementVisibility(GettingWebresourcesProgressRing, Visibility.Collapsed);
                    break;
                case CurrentStatus.CreatingWebresources:
                    SetImageSourceToImage(CreateImage, uri);
                    SetUiElementVisibility(CreateImage, Visibility.Visible);
                    SetUiElementVisibility(CreateProgressRing, Visibility.Collapsed);
                    break;
                case CurrentStatus.UpdatingWebresources:
                    SetImageSourceToImage(UpdateImage, uri);
                    SetUiElementVisibility(UpdateImage, Visibility.Visible);
                    SetUiElementVisibility(UpdateProgressRing, Visibility.Collapsed);
                    break;
                case CurrentStatus.Publishing:
                    SetImageSourceToImage(PublishImage, uri);
                    SetUiElementVisibility(PublishImage, Visibility.Visible);
                    SetUiElementVisibility(PublishProgressRing, Visibility.Collapsed);
                    break;
            }
        }

        private void SetLabelText(ContentControl label, string text)
        {
            Dispatcher.Invoke(() => label.Content = text);
        }

        private void SetUiElementVisibility(UIElement uiElement, Visibility visibility)
        {
            Dispatcher.Invoke(() => uiElement.Visibility = visibility);
        }

        private void SetUiElementEnabled(UIElement uiElement, bool isEnabled)
        {
            Dispatcher.Invoke(() => uiElement.IsEnabled = isEnabled);
        }

        private void SetActivityToProgressRing(ProgressRing progressRing, bool isActive)
        {
            Dispatcher.Invoke(() => progressRing.IsActive = isActive);
        }

        private void ShowDetails_Click(object sender, RoutedEventArgs e)
        {
            if (ShowDetailsButton.IsChecked == true)
            {
                SetUiElementVisibility(OutputTextBox, Visibility.Visible);
                Height += 180;
            }
            else
            {
                SetUiElementVisibility(OutputTextBox, Visibility.Hidden);
                Height -= 180;
            }
        }
    }
}