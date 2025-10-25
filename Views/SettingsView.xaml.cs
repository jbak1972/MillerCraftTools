using System.Windows;
using System.Windows.Controls;
using Miller_Craft_Tools.ViewModel;

namespace Miller_Craft_Tools.Views
{
    public partial class SettingsView : Window
    {
        private readonly SettingsViewModel _viewModel;
        private bool _isLoaded;

        public SettingsView()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;
            _isLoaded = false;
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            TokenBox.Password = _viewModel.ApiToken ?? string.Empty;
            _isLoaded = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ApiToken = TokenBox.Password;
            _viewModel.Save();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TokenBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
                _viewModel.ApiToken = TokenBox.Password;
        }

        private void CopyToken_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(TokenBox.Password);
        }

        private void LoginWebApp_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for future web login dialog
            System.Windows.MessageBox.Show("Web login feature coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
