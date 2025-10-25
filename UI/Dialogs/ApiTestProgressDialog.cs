using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Miller_Craft_Tools.UI.Dialogs
{
    /// <summary>
    /// Dialog that shows progress during API testing operations
    /// </summary>
    public class ApiTestProgressDialog : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button cancelButton;
        private CancellationTokenSource cancellationTokenSource;
        private Task runningTask;
        
        public ApiTestProgressDialog(string title)
        {
            InitializeComponent();
            titleLabel.Text = title;
            statusLabel.Text = "Initializing...";
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Run a task with progress updates
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="taskFunc">Function that performs the operation</param>
        /// <param name="progressCallback">Optional callback for progress updates</param>
        /// <returns>Result of the task</returns>
        public T RunTaskWithProgress<T>(Func<IProgress<string>, CancellationToken, Task<T>> taskFunc, Action<T> completionCallback = null)
        {
            T result = default(T);
            var progress = new Progress<string>(status => 
            {
                if (this.IsDisposed) return;
                
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => UpdateStatus(status)));
                }
                else
                {
                    UpdateStatus(status);
                }
            });
            
            runningTask = Task.Run(async () => 
            {
                try
                {
                    result = await taskFunc(progress, cancellationTokenSource.Token);
                    
                    if (this.IsDisposed) return;
                    
                    this.BeginInvoke(new Action(() => 
                    {
                        progressBar.Style = System.Windows.Forms.ProgressBarStyle.Blocks;
                        progressBar.Value = 100;
                        statusLabel.Text = "Testing complete!";
                        
                        // Call completion callback if provided
                        completionCallback?.Invoke(result);
                        
                        // Close the form after a short delay
                        Task.Delay(500).ContinueWith(_ => 
                        {
                            if (this.IsDisposed) return;
                            this.BeginInvoke(new Action(() => this.Close()));
                        });
                    }));
                }
                catch (OperationCanceledException)
                {
                    if (this.IsDisposed) return;
                    
                    this.BeginInvoke(new Action(() => 
                    {
                        statusLabel.Text = "Operation cancelled.";
                        this.Close();
                    }));
                }
                catch (Exception ex)
                {
                    if (this.IsDisposed) return;
                    
                    this.BeginInvoke(new Action(() => 
                    {
                        statusLabel.Text = "Error: " + ex.Message;
                        progressBar.Style = System.Windows.Forms.ProgressBarStyle.Blocks;
                        progressBar.Value = 0;
                        cancelButton.Text = "Close";
                    }));
                }
            });
            
            this.ShowDialog();
            return result;
        }
        
        private void UpdateStatus(string status)
        {
            statusLabel.Text = status;
            Application.DoEvents();
        }
        
        private void cancelButton_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                this.Close();
                return;
            }
            
            cancelButton.Text = "Closing...";
            cancelButton.Enabled = false;
            cancellationTokenSource.Cancel();
        }
        
        protected override void OnFormClosing(System.Windows.Forms.FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        }
        
        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(12, 14);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(165, 21);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Testing API Token...";
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(13, 48);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(226, 15);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Testing connection to API endpoints...";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(16, 75);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(406, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(337, 114);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(85, 27);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // ApiTestProgressDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 153);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.titleLabel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ApiTestProgressDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Miller Craft API Test";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
