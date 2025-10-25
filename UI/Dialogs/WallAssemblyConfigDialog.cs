using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Miller_Craft_Tools.Command;
using Miller_Craft_Tools.UI.Styles;
using Autodesk.Revit.DB;

namespace Miller_Craft_Tools.UI.Dialogs
{
    public partial class WallAssemblyConfigDialog : System.Windows.Forms.Form
    {
        private Document _doc;
        private List<WallAssemblyStandardizerCommand.WallAssemblyTemplate> _templates;
        private List<Material> _zootMaterials;

        public List<WallAssemblyStandardizerCommand.WallAssemblyTemplate> SelectedTemplates { get; private set; }

        public WallAssemblyConfigDialog(Document doc, List<WallAssemblyStandardizerCommand.WallAssemblyTemplate> templates, List<Material> zootMaterials)
        {
            InitializeComponent();
            
            _doc = doc;
            _templates = templates;
            _zootMaterials = zootMaterials;
            SelectedTemplates = new List<WallAssemblyStandardizerCommand.WallAssemblyTemplate>();
            
            // Apply branding colors
            this.BackColor = BrandColors.PrimaryColor;
            this.ForeColor = System.Drawing.Color.White;
            
            // Fill the listview with template data
            PopulateTemplateList();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Main form properties
            this.Text = "Wall Assembly Configuration";
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Template ListView
            this.lvTemplates = new ListView();
            this.lvTemplates.Location = new System.Drawing.Point(12, 12);
            this.lvTemplates.Size = new System.Drawing.Size(776, 300);
            this.lvTemplates.View = System.Windows.Forms.View.Details;
            this.lvTemplates.FullRowSelect = true;
            this.lvTemplates.CheckBoxes = true;
            this.lvTemplates.Columns.Add("Category", 100);
            this.lvTemplates.Columns.Add("Name", 150);
            this.lvTemplates.Columns.Add("Description", 330);
            this.lvTemplates.Columns.Add("Width (in)", 80);
            this.lvTemplates.Columns.Add("Layers", 80);
            this.Controls.Add(this.lvTemplates);
            
            // Status label
            this.lblStatus = new Label();
            this.lblStatus.Location = new System.Drawing.Point(12, 320);
            this.lblStatus.Size = new System.Drawing.Size(776, 40);
            this.lblStatus.Text = "Select wall templates to standardize. Templates will be applied to matching walls or created as new types.";
            this.Controls.Add(this.lblStatus);
            
            // Group Box for Material Availability
            this.gbMaterials = new GroupBox();
            this.gbMaterials.Text = "Available ZOOT Materials";
            this.gbMaterials.Location = new System.Drawing.Point(12, 370);
            this.gbMaterials.Size = new System.Drawing.Size(450, 80);
            this.Controls.Add(this.gbMaterials);
            
            // Material count label
            this.lblMaterialCount = new Label();
            this.lblMaterialCount.Location = new System.Drawing.Point(10, 20);
            this.lblMaterialCount.Size = new System.Drawing.Size(430, 20);
            this.gbMaterials.Controls.Add(this.lblMaterialCount);
            
            // Material examples label
            this.lblMaterialExamples = new Label();
            this.lblMaterialExamples.Location = new System.Drawing.Point(10, 45);
            this.lblMaterialExamples.Size = new System.Drawing.Size(430, 25);
            this.gbMaterials.Controls.Add(this.lblMaterialExamples);
            
            // Select All button
            this.btnSelectAll = new Button();
            this.btnSelectAll.Text = "Select All";
            this.btnSelectAll.Location = new System.Drawing.Point(480, 380);
            this.btnSelectAll.Size = new System.Drawing.Size(100, 30);
            this.btnSelectAll.Click += new EventHandler(btnSelectAll_Click);
            this.Controls.Add(this.btnSelectAll);
            
            // Select None button
            this.btnSelectNone = new Button();
            this.btnSelectNone.Text = "Select None";
            this.btnSelectNone.Location = new System.Drawing.Point(590, 380);
            this.btnSelectNone.Size = new System.Drawing.Size(100, 30);
            this.btnSelectNone.Click += new EventHandler(btnSelectNone_Click);
            this.Controls.Add(this.btnSelectNone);
            
            // OK Button
            this.btnOK = new Button();
            this.btnOK.Text = "OK";
            this.btnOK.Location = new System.Drawing.Point(598, 430);
            this.btnOK.Size = new System.Drawing.Size(90, 30);
            this.btnOK.DialogResult = DialogResult.OK;
            this.btnOK.Click += new EventHandler(btnOK_Click);
            this.Controls.Add(this.btnOK);
            
            // Cancel Button
            this.btnCancel = new Button();
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Location = new System.Drawing.Point(698, 430);
            this.btnCancel.Size = new System.Drawing.Size(90, 30);
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(this.btnCancel);
            
            // Accept and Cancel buttons
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            
            this.ResumeLayout(false);
        }

        private void PopulateTemplateList()
        {
            lvTemplates.Items.Clear();
            
            foreach (var template in _templates)
            {
                string categoryPrefix = template.Category switch
                {
                    WallAssemblyStandardizerCommand.WallAssemblyCategory.Exterior => "Exterior",
                    WallAssemblyStandardizerCommand.WallAssemblyCategory.ExteriorFinish => "Ext. Finish",
                    WallAssemblyStandardizerCommand.WallAssemblyCategory.Interior => "Interior",
                    WallAssemblyStandardizerCommand.WallAssemblyCategory.InteriorFinish => "Int. Finish",
                    WallAssemblyStandardizerCommand.WallAssemblyCategory.Structural => "Structural",
                    _ => "Unknown"
                };
                
                var item = new ListViewItem(new string[]
                {
                    categoryPrefix,
                    template.Name,
                    template.Description,
                    (template.Width * 12).ToString("F2"), // Convert to inches for display
                    template.Layers?.Count.ToString() ?? "0"
                });
                
                item.Tag = template;
                item.Checked = true; // Default to all templates selected
                lvTemplates.Items.Add(item);
            }
            
            // Update material info
            lblMaterialCount.Text = $"Found {_zootMaterials.Count} materials with 'ZOOT - ' prefix.";
            
            if (_zootMaterials.Count > 0)
            {
                // Show a few examples
                string examples = string.Join(", ", _zootMaterials.Take(3).Select(m => m.Name));
                if (_zootMaterials.Count > 3)
                    examples += ", ...";
                    
                lblMaterialExamples.Text = $"Examples: {examples}";
            }
            else
            {
                lblMaterialExamples.Text = "No materials found. Please create materials with 'ZOOT - ' prefix first.";
            }
        }
        
        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvTemplates.Items)
            {
                item.Checked = true;
            }
        }
        
        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvTemplates.Items)
            {
                item.Checked = false;
            }
        }
        
        private void btnOK_Click(object sender, EventArgs e)
        {
            // Build the list of selected templates
            SelectedTemplates = new List<WallAssemblyStandardizerCommand.WallAssemblyTemplate>();
            
            foreach (ListViewItem item in lvTemplates.Items)
            {
                if (item.Checked && item.Tag is WallAssemblyStandardizerCommand.WallAssemblyTemplate template)
                {
                    SelectedTemplates.Add(template);
                }
            }
            
            if (SelectedTemplates.Count == 0)
            {
                MessageBox.Show("No templates selected. Please select at least one template or click Cancel.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
            }
        }
        
        // Form controls
        private ListView lvTemplates;
        private Label lblStatus;
        private GroupBox gbMaterials;
        private Label lblMaterialCount;
        private Label lblMaterialExamples;
        private Button btnSelectAll;
        private Button btnSelectNone;
        private Button btnOK;
        private Button btnCancel;
    }
}
