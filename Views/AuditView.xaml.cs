using System.Windows;

namespace Miller_Craft_Tools.Views
{
    public partial class AuditView : Window
    {
        public AuditView()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}