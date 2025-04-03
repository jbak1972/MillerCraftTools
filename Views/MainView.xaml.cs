using System;
using System.Windows;
using System.Windows.Controls;

namespace Miller_Craft_Tools.Views
{
    public partial class MainView : Window
    {
        public event EventHandler SyncFilledRegionsClicked;
        public event EventHandler RenumberWindowsClicked;
        public event EventHandler RenumberViewsClicked;
        public event EventHandler GroupElementsByLevelClicked;
        public event EventHandler ExportStandardsClicked;
        public event EventHandler CopyToSheetsClicked;
        public event EventHandler SetupStandardsClicked; // New event

        public MainView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                switch (button.Tag?.ToString())
                {
                    case "SyncFilledRegions":
                        SyncFilledRegionsClicked?.Invoke(this, EventArgs.Empty);
                        break;
                    case "RenumberWindows":
                        RenumberWindowsClicked?.Invoke(this, EventArgs.Empty);
                        break;
                    case "RenumberViews":
                        RenumberViewsClicked?.Invoke(this, EventArgs.Empty);
                        break;
                    case "GroupByLevel":
                        GroupElementsByLevelClicked?.Invoke(this, EventArgs.Empty);
                        break;
                    case "ExportStandards":
                        ExportStandardsClicked?.Invoke(this, EventArgs.Empty);
                        break;
                    case "CopyToSheets":
                        CopyToSheetsClicked?.Invoke(this, EventArgs.Empty);
                        break;
                    case "SetupStandards": // New case
                        SetupStandardsClicked?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }
        }

        public void HideDialog() => Hide();
        public void ShowDialogAgain() => Show();
    }
}