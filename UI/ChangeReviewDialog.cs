using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.Services;
using Miller_Craft_Tools.Utils;
using Autodesk.Revit.DB;

namespace Miller_Craft_Tools.UI
{
    /// <summary>
    /// Dialog for reviewing and applying parameter changes from the web application
    /// </summary>
    public partial class ChangeReviewDialog : System.Windows.Forms.Form
    {
        private readonly Document _document;
        private readonly string _syncId;
        private readonly List<WebParameterChange> _changes;
        private readonly SyncServiceV2 _syncService;
        
        /// <summary>
        /// Gets the list of applied changes after the dialog is closed
        /// </summary>
        public List<AppliedChange> AppliedChanges { get; private set; }
        
        /// <summary>
        /// Creates a new change review dialog
        /// </summary>
        /// <param name="document">Revit document to apply changes to</param>
        /// <param name="syncId">The ID of the sync operation</param>
        /// <param name="changes">List of parameter changes to review</param>
        /// <param name="syncService">The sync service instance</param>
        public ChangeReviewDialog(Document document, string syncId, List<WebParameterChange> changes, SyncServiceV2 syncService)
        {
            InitializeComponent();
            _document = document;
            _syncId = syncId;
            _changes = changes;
            _syncService = syncService;
            AppliedChanges = new List<AppliedChange>();
            
            // Set up the form
            Text = "Miller Craft Assistant - Review Changes";
            Size = new Size(800, 500);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            
            // Create controls
            CreateControls();
            
            // Populate the list with changes
            PopulateChangesList();
        }
        
        private void InitializeComponent()
        {
            // This method is called by the constructor
            // and creates the necessary components
            
            // Changes list view
            changesListView = new ListView();
            changesListView.Dock = DockStyle.Fill;
            changesListView.View = System.Windows.Forms.View.Details;
            changesListView.FullRowSelect = true;
            changesListView.GridLines = true;
            changesListView.CheckBoxes = true;
            
            // Add columns
            changesListView.Columns.Add("Apply", 50);
            changesListView.Columns.Add("Parameter", 150);
            changesListView.Columns.Add("Category", 120);
            changesListView.Columns.Add("Current Value", 150);
            changesListView.Columns.Add("New Value", 150);
            changesListView.Columns.Add("Modified By", 100);
            changesListView.Columns.Add("Modified At", 150);
            
            // Select All checkbox
            selectAllCheckBox = new CheckBox();
            selectAllCheckBox.Text = "Select All";
            selectAllCheckBox.Checked = true;
            selectAllCheckBox.Location = new System.Drawing.Point(10, 10);
            selectAllCheckBox.AutoSize = true;
            selectAllCheckBox.CheckedChanged += SelectAllCheckBox_CheckedChanged;
            
            // Info label
            infoLabel = new Label();
            infoLabel.Text = $"The following changes were made in the Miller Craft Assistant web application. Select the changes you want to apply to this Revit model.";
            infoLabel.Location = new System.Drawing.Point(150, 10);
            infoLabel.AutoSize = true;
            
            // Button panel
            buttonPanel = new System.Windows.Forms.Panel();
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Height = 50;
            
            // Apply button
            applyButton = new Button();
            applyButton.Text = "Apply Selected Changes";
            applyButton.Location = new System.Drawing.Point(560, 10);
            applyButton.Size = new System.Drawing.Size(150, 30);
            applyButton.Click += ApplyButton_Click;
            
            // Cancel button
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new System.Drawing.Point(720, 10);
            cancelButton.Size = new System.Drawing.Size(70, 30);
            cancelButton.Click += CancelButton_Click;
            
            // Status label
            statusLabel = new Label();
            statusLabel.Text = "";
            statusLabel.Location = new System.Drawing.Point(10, 20);
            statusLabel.AutoSize = true;
            
            // Add buttons to panel
            buttonPanel.Controls.Add(applyButton);
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(statusLabel);
            
            // Main panel for list view
            mainPanel = new System.Windows.Forms.Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new System.Windows.Forms.Padding(10);
            mainPanel.Controls.Add(changesListView);
            
            // Header panel
            headerPanel = new System.Windows.Forms.Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 40;
            headerPanel.Controls.Add(selectAllCheckBox);
            headerPanel.Controls.Add(infoLabel);
            
            // Add panels to form
            Controls.Add(mainPanel);
            Controls.Add(headerPanel);
            Controls.Add(buttonPanel);
        }
        
        private void CreateControls()
        {
            // Controls are already created in InitializeComponent
        }
        
        private void PopulateChangesList()
        {
            changesListView.Items.Clear();
            
            // Load current values for display
            LoadCurrentValues();
            
            // Add each change to the list
            foreach (var change in _changes)
            {
                var item = new ListViewItem();
                item.Checked = change.IsSelected;
                item.SubItems.Add(change.Name);
                item.SubItems.Add(change.Category);
                item.SubItems.Add(change.CurrentValue);
                item.SubItems.Add(change.Value);
                item.SubItems.Add(change.ModifiedBy);
                item.SubItems.Add(change.FormattedModifiedAt);
                item.Tag = change;
                
                changesListView.Items.Add(item);
            }
            
            // Auto-resize columns
            foreach (ColumnHeader column in changesListView.Columns)
            {
                column.Width = -2; // Auto-size to fit content
            }
        }
        
        private void LoadCurrentValues()
        {
            // Load current values from the Revit model
            try
            {
                foreach (var change in _changes)
                {
                    // Find the parameter in the Revit model
                    Parameter parameter = null;
                    
                    if (change.Category.Equals("Project Information", StringComparison.OrdinalIgnoreCase))
                    {
                        ProjectInfo projInfo = _document.ProjectInformation;
                        parameter = projInfo.LookupParameter(change.Name);
                    }
                    
                    // TODO: Add support for other parameter categories
                    
                    if (parameter != null)
                    {
                        string currentValue = parameter.AsValueString() ?? parameter.AsString() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(currentValue) || currentValue.Trim() == "-")
                            currentValue = string.Empty;
                            
                        change.CurrentValue = currentValue;
                    }
                    else
                    {
                        change.CurrentValue = "(Not found)";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading current values: {ex.Message}");
            }
        }
        
        private void SelectAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Update all checkboxes
            foreach (ListViewItem item in changesListView.Items)
            {
                item.Checked = selectAllCheckBox.Checked;
                
                // Update the model
                if (item.Tag is WebParameterChange change)
                {
                    change.IsSelected = selectAllCheckBox.Checked;
                }
            }
        }
        
        private async void ApplyButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable buttons
                applyButton.Enabled = false;
                cancelButton.Enabled = false;
                
                // Update status
                statusLabel.Text = "Applying changes...";
                statusLabel.ForeColor = System.Drawing.Color.Blue;
                
                // Get selected changes
                var selectedChanges = new List<WebParameterChange>();
                foreach (ListViewItem item in changesListView.Items)
                {
                    if (item.Checked && item.Tag is WebParameterChange change)
                    {
                        selectedChanges.Add(change);
                    }
                }
                
                if (selectedChanges.Count == 0)
                {
                    // No changes selected
                    MessageBox.Show(
                        "No changes were selected to apply.",
                        "No Changes Selected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                        
                    // Re-enable buttons
                    applyButton.Enabled = true;
                    cancelButton.Enabled = true;
                    statusLabel.Text = "";
                    
                    return;
                }
                
                // Apply the changes to the Revit model
                AppliedChanges = _syncService.ApplyParameterChanges(_document, _changes);
                
                // Send acknowledgment to the server
                bool acknowledged = await _syncService.AcknowledgeChangesAsync(_syncId, AppliedChanges);
                
                if (acknowledged)
                {
                    // Success
                    MessageBox.Show(
                        $"Successfully applied {AppliedChanges.Count} changes to the Revit model.",
                        "Changes Applied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                        
                    // Close the dialog
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    // Failed to acknowledge
                    MessageBox.Show(
                        "Changes were applied to the Revit model but failed to acknowledge to the server.",
                        "Acknowledgment Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                        
                    // Close anyway as changes were applied
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                // Show error
                MessageBox.Show(
                    $"Error applying changes: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                    
                // Re-enable buttons
                applyButton.Enabled = true;
                cancelButton.Enabled = true;
                statusLabel.Text = "Error applying changes.";
                statusLabel.ForeColor = System.Drawing.Color.Red;
            }
        }
        
        private void CancelButton_Click(object sender, EventArgs e)
        {
            // User canceled, close dialog
            DialogResult = DialogResult.Cancel;
            Close();
        }
        
        #region Form Controls
        private ListView changesListView;
        private CheckBox selectAllCheckBox;
        private Button applyButton;
        private Button cancelButton;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.Panel headerPanel;
        private Label infoLabel;
        private Label statusLabel;
        #endregion
    }
}
