using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Miller_Craft_Tools.Views
{
    public partial class ResultsView : Window
    {
        private readonly UIDocument _uidoc;

        public ResultsView(UIDocument uidoc, List<Miller_Craft_Tools.ViewModel.LevelNode> levelNodes)
        {
            InitializeComponent();
            _uidoc = uidoc;
            ResultsTreeView.ItemsSource = levelNodes;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Get the ElementId from the NavigateUri (stored in ElementId property)
            string elementIdString = e.Uri.ToString();
            if (int.TryParse(elementIdString, out int elementIdValue))
            {
                ElementId elementId = new ElementId(elementIdValue);

                // Select the element in the Revit model
                _uidoc.Selection.SetElementIds(new List<ElementId> { elementId });

                // Bring the Revit window back into focus
                IntPtr revitWindowHandle = _uidoc.Application.MainWindowHandle;
                if (revitWindowHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(revitWindowHandle);
                }
            }

            e.Handled = true;
        }

        private void SelectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string elementIdString)
            {
                if (int.TryParse(elementIdString, out int elementIdValue))
                {
                    ElementId elementId = new ElementId(elementIdValue);
                    _uidoc.Selection.SetElementIds(new List<ElementId> { elementId });

                    // Bring the Revit window back into focus
                    IntPtr revitWindowHandle = _uidoc.Application.MainWindowHandle;
                    if (revitWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(revitWindowHandle);
                    }
                }
            }
        }

        private void ZoomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string elementIdString)
            {
                if (int.TryParse(elementIdString, out int elementIdValue))
                {
                    ElementId elementId = new ElementId(elementIdValue);

                    // Zoom to the element in the active view
                    _uidoc.ShowElements(elementId);

                    // Bring the Revit window back into focus
                    IntPtr revitWindowHandle = _uidoc.Application.MainWindowHandle;
                    if (revitWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(revitWindowHandle);
                    }
                }
            }
        }

        private void IsolateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string elementIdString)
            {
                if (int.TryParse(elementIdString, out int elementIdValue))
                {
                    ElementId elementId = new ElementId(elementIdValue);

                    // Isolate the element in the active view
                    using (Transaction trans = new Transaction(_uidoc.Document, "Isolate Element"))
                    {
                        trans.Start();
                        List<ElementId> elementIds = new List<ElementId> { elementId };
                        _uidoc.ActiveView.IsolateElementsTemporary(elementIds);
                        trans.Commit();
                    }

                    // Bring the Revit window back into focus
                    IntPtr revitWindowHandle = _uidoc.Application.MainWindowHandle;
                    if (revitWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(revitWindowHandle);
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset any temporary view modes (e.g., isolate) when closing the window
            using (Transaction trans = new Transaction(_uidoc.Document, "Reset Temporary View Modes"))
            {
                trans.Start();
                _uidoc.ActiveView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                trans.Commit();
            }

            Close();
        }

        // Import the SetForegroundWindow function from user32.dll to bring Revit window into focus
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public void ShowDialogAgain() => Show();
        public void HideDialog() => Hide();
    }
}