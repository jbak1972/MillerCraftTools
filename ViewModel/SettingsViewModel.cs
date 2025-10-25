using System.ComponentModel;
using System.Runtime.CompilerServices;
using Miller_Craft_Tools.Model;

namespace Miller_Craft_Tools.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private UserSettings _settings;
        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsViewModel()
        {
            _settings = UserSettings.Load();
        }

        public bool Open3DViewsForRenumbering
        {
            get => _settings.Open3DViewsForRenumbering;
            set { _settings.Open3DViewsForRenumbering = value; OnPropertyChanged(); }
        }

        public string ApiToken
        {
            get => _settings.ApiToken;
            set { _settings.ApiToken = value; OnPropertyChanged(); }
        }

        public string WebSessionCookie
        {
            get => _settings.WebSessionCookie;
            set { _settings.WebSessionCookie = value; OnPropertyChanged(); }
        }

        public void Save() => _settings.Save();

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
