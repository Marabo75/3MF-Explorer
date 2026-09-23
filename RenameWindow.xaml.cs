using System.Windows;
using System.Windows.Input;

namespace ThreeMFExplorer
{
    public partial class RenameWindow : Window
    {
        public string NewFileName { get; private set; } = string.Empty;

        public RenameWindow(string currentFileName)
        {
            InitializeComponent();

            TxtFileName.Text = currentFileName;

            ApplyLanguage();

            Loaded += RenameWindow_Loaded;
        }

        private void ApplyLanguage()
        {
            Title = Localization.Get("RenameFile");
            TxtWindowTitle.Text = Localization.Get("RenameFile");
            LblNewFileName.Text = Localization.Get("NewFileName");
            BtnCancel.Content = Localization.Get("Cancel");
            BtnRename.Content = Localization.Get("RenameButton");
            BtnClose.ToolTip = Localization.Get("Close");
        }

        private void RenameWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            TxtFileName.Focus();
            TxtFileName.SelectAll();
        }

        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            try
            {
                DragMove();
            }
            catch
            {
            }
        }

        private void BtnRename_Click(
            object sender,
            RoutedEventArgs e)
        {
            string newName =
                TxtFileName.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show(
                    this,
                    Localization.Get("EnterFileName"),
                    Localization.Get("RenameFile"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                TxtFileName.Focus();

                return;
            }

            NewFileName = newName;

            DialogResult = true;
        }

        private void BtnCancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnClose_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}