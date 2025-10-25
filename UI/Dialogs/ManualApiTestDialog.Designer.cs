namespace Miller_Craft_Tools.UI.Dialogs
{
    partial class ManualApiTestDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblEndpoint = new System.Windows.Forms.Label();
            this.cmbEndpoint = new System.Windows.Forms.ComboBox();
            this.lblAuthType = new System.Windows.Forms.Label();
            this.cmbAuthType = new System.Windows.Forms.ComboBox();
            this.lblCustomToken = new System.Windows.Forms.Label();
            this.txtCustomToken = new System.Windows.Forms.TextBox();
            this.btnTestEndpoint = new System.Windows.Forms.Button();
            this.btnRunSequentialTests = new System.Windows.Forms.Button();
            this.lblResults = new System.Windows.Forms.Label();
            this.txtResults = new System.Windows.Forms.RichTextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblEndpoint
            // 
            this.lblEndpoint.AutoSize = true;
            this.lblEndpoint.Location = new System.Drawing.Point(12, 15);
            this.lblEndpoint.Name = "lblEndpoint";
            this.lblEndpoint.Size = new System.Drawing.Size(52, 13);
            this.lblEndpoint.TabIndex = 0;
            this.lblEndpoint.Text = "Endpoint:";
            // 
            // cmbEndpoint
            // 
            this.cmbEndpoint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbEndpoint.FormattingEnabled = true;
            this.cmbEndpoint.Location = new System.Drawing.Point(108, 12);
            this.cmbEndpoint.Name = "cmbEndpoint";
            this.cmbEndpoint.Size = new System.Drawing.Size(464, 21);
            this.cmbEndpoint.TabIndex = 1;
            // 
            // lblAuthType
            // 
            this.lblAuthType.AutoSize = true;
            this.lblAuthType.Location = new System.Drawing.Point(12, 42);
            this.lblAuthType.Name = "lblAuthType";
            this.lblAuthType.Size = new System.Drawing.Size(81, 13);
            this.lblAuthType.TabIndex = 2;
            this.lblAuthType.Text = "Authentication:";
            // 
            // cmbAuthType
            // 
            this.cmbAuthType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAuthType.FormattingEnabled = true;
            this.cmbAuthType.Location = new System.Drawing.Point(108, 39);
            this.cmbAuthType.Name = "cmbAuthType";
            this.cmbAuthType.Size = new System.Drawing.Size(200, 21);
            this.cmbAuthType.TabIndex = 3;
            this.cmbAuthType.SelectedIndexChanged += new System.EventHandler(this.cmbAuthType_SelectedIndexChanged);
            // 
            // lblCustomToken
            // 
            this.lblCustomToken.AutoSize = true;
            this.lblCustomToken.Location = new System.Drawing.Point(12, 69);
            this.lblCustomToken.Name = "lblCustomToken";
            this.lblCustomToken.Size = new System.Drawing.Size(82, 13);
            this.lblCustomToken.TabIndex = 4;
            this.lblCustomToken.Text = "Custom Token:";
            // 
            // txtCustomToken
            // 
            this.txtCustomToken.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCustomToken.Location = new System.Drawing.Point(108, 66);
            this.txtCustomToken.Name = "txtCustomToken";
            this.txtCustomToken.Size = new System.Drawing.Size(464, 20);
            this.txtCustomToken.TabIndex = 5;
            this.txtCustomToken.TextChanged += new System.EventHandler(this.txtCustomToken_TextChanged);
            // 
            // btnTestEndpoint
            // 
            this.btnTestEndpoint.Location = new System.Drawing.Point(12, 7);
            this.btnTestEndpoint.Name = "btnTestEndpoint";
            this.btnTestEndpoint.Size = new System.Drawing.Size(143, 23);
            this.btnTestEndpoint.TabIndex = 6;
            this.btnTestEndpoint.Text = "Test Selected Endpoint";
            this.btnTestEndpoint.UseVisualStyleBackColor = true;
            this.btnTestEndpoint.Click += new System.EventHandler(this.btnTestEndpoint_Click);
            // 
            // btnRunSequentialTests
            // 
            this.btnRunSequentialTests.Location = new System.Drawing.Point(161, 7);
            this.btnRunSequentialTests.Name = "btnRunSequentialTests";
            this.btnRunSequentialTests.Size = new System.Drawing.Size(143, 23);
            this.btnRunSequentialTests.TabIndex = 7;
            this.btnRunSequentialTests.Text = "Run Sequential Tests";
            this.btnRunSequentialTests.UseVisualStyleBackColor = true;
            this.btnRunSequentialTests.Click += new System.EventHandler(this.btnRunSequentialTests_Click);
            // 
            // lblResults
            // 
            this.lblResults.AutoSize = true;
            this.lblResults.Location = new System.Drawing.Point(12, 128);
            this.lblResults.Name = "lblResults";
            this.lblResults.Size = new System.Drawing.Size(45, 13);
            this.lblResults.TabIndex = 8;
            this.lblResults.Text = "Results:";
            // 
            // txtResults
            // 
            this.txtResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtResults.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtResults.Location = new System.Drawing.Point(12, 144);
            this.txtResults.Name = "txtResults";
            this.txtResults.ReadOnly = true;
            this.txtResults.Size = new System.Drawing.Size(560, 309);
            this.txtResults.TabIndex = 9;
            this.txtResults.Text = "";
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(497, 7);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 10;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.lblEndpoint);
            this.panel1.Controls.Add(this.cmbEndpoint);
            this.panel1.Controls.Add(this.lblAuthType);
            this.panel1.Controls.Add(this.cmbAuthType);
            this.panel1.Controls.Add(this.lblCustomToken);
            this.panel1.Controls.Add(this.txtCustomToken);
            this.panel1.Location = new System.Drawing.Point(12, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(584, 101);
            this.panel1.TabIndex = 11;
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.btnTestEndpoint);
            this.panel2.Controls.Add(this.btnRunSequentialTests);
            this.panel2.Controls.Add(this.btnClose);
            this.panel2.Location = new System.Drawing.Point(12, 459);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(584, 40);
            this.panel2.TabIndex = 12;
            // 
            // ManualApiTestDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(608, 511);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.txtResults);
            this.Controls.Add(this.lblResults);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(624, 550);
            this.Name = "ManualApiTestDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manual API Test";
            this.Load += new System.EventHandler(this.ManualApiTestDialog_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblEndpoint;
        private System.Windows.Forms.ComboBox cmbEndpoint;
        private System.Windows.Forms.Label lblAuthType;
        private System.Windows.Forms.ComboBox cmbAuthType;
        private System.Windows.Forms.Label lblCustomToken;
        private System.Windows.Forms.TextBox txtCustomToken;
        private System.Windows.Forms.Button btnTestEndpoint;
        private System.Windows.Forms.Button btnRunSequentialTests;
        private System.Windows.Forms.Label lblResults;
        private System.Windows.Forms.RichTextBox txtResults;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
    }
}
