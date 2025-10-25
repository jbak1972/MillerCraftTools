using System;
using System.Windows.Forms;

namespace Miller_Craft_Tools.Command.UI
{
    public class RenumberingControlForm : Form
    {
        public event EventHandler FinishClicked;
        public event EventHandler FormCancelled;

        private Button finishButton;
        private Label statusLabel;

        public RenumberingControlForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.finishButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // finishButton
            // 
            this.finishButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.finishButton.Location = new System.Drawing.Point(12, 38);
            this.finishButton.Name = "finishButton";
            this.finishButton.Size = new System.Drawing.Size(260, 23);
            this.finishButton.TabIndex = 0;
            this.finishButton.Text = "Finish Renumbering";
            this.finishButton.UseVisualStyleBackColor = true;
            this.finishButton.Click += new System.EventHandler(this.FinishButton_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(12, 9);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(120, 13);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Initializing...";
            // 
            // RenumberingControlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 73);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.finishButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "RenumberingControlForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Renumber Control";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RenumberingControlForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void FinishButton_Click(object sender, EventArgs e)
        {
            FinishClicked?.Invoke(this, EventArgs.Empty);
        }

        private void RenumberingControlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If the form is closing due to user action (e.g., 'X' button) and not programmatically,
            // signal cancellation.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                FormCancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetStatus(string message)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                if (statusLabel.InvokeRequired)
                {
                    statusLabel.Invoke(new Action(() => statusLabel.Text = message));
                }
                else
                {
                    statusLabel.Text = message;
                }
            }
        }
    }
}
