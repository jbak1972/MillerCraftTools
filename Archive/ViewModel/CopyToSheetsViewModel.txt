using System.Windows;
using System.Windows.Input;
using Miller_Craft_Tools.Command;

namespace Miller_Craft_Tools.ViewModel
{
    public class CopyToSheetsViewModel : ViewModelBase
    {
        private readonly Window _dialog;
        private bool _copyRevision;
        private bool _copyLegend;

        public bool CopyRevision
        {
            get => _copyRevision;
            set
            {
                _copyRevision = value;
                OnPropertyChanged(nameof(CopyRevision));
            }
        }

        public bool CopyLegend
        {
            get => _copyLegend;
            set
            {
                _copyLegend = value;
                OnPropertyChanged(nameof(CopyLegend));
            }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public CopyToSheetsViewModel(Window dialog)
        {
            _dialog = dialog;
            // Default values
            CopyRevision = true;
            CopyLegend = false;

            OkCommand = new RelayCommand(OkExecute);
            CancelCommand = new RelayCommand(CancelExecute);
        }

        private void OkExecute(object parameter)
        {
            _dialog.DialogResult = true;
            _dialog.Close();
        }

        private void CancelExecute(object parameter)
        {
            _dialog.DialogResult = false;
            _dialog.Close();
        }
    }
}