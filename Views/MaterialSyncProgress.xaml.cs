using System.Windows;
using System.Windows.Controls;

namespace Miller_Craft_Tools.Views
{
    public partial class MaterialSyncProgress : Window
    {
        public bool CancelRequested { get; private set; }

        public MaterialSyncProgress(int maximum)
        {
            InitializeComponent();
            ProgressBar.Maximum = maximum;
            CancelButton.Click += (s, e) =>
            {
                CancelRequested = true;
                CancelButton.IsEnabled = false;
                StatusText.Text = "Cancelling…";
            };
        }

        public void Report(int value, string status)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = value;
                StatusText.Text = status;
            });
        }
    }
}
