using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Services;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.Model;
using Miller_Craft_Tools.UI;
using Miller_Craft_Tools.Services.SyncUtilities;
using Miller_Craft_Tools.UI.Dialogs;
using Miller_Craft_Tools.UI.Styles;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Helper class to hold state for timer callbacks
    /// </summary>
    internal class TimerState
    {
        public SyncServiceV2 SyncService { get; set; }
        public string SyncId { get; set; }
        public Document Document { get; set; }
        public System.Threading.Timer Timer { get; set; }
    }

    /// <summary>
    /// External command for bidirectional sync with the Miller Craft Assistant web application
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class SyncWithWebCommand : IExternalCommand
    {        
        // Cancellation token source for cancelling operations
        private CancellationTokenSource _cancellationTokenSource;
        
        // Progress reporter for displaying progress in the UI
        private IProgress<Tuple<string, int>> _progressReporter;
        
        // Latest sync ID for status checking
        private string _latestSyncId;
        
        // Reference to the UI application for threading
        private Autodesk.Revit.UI.UIApplication _uiApplication;
        
        // Document reference for status checking
        private Document _currentDocument;
        
        // Flag indicating if idling event is registered
        private bool _idlingRegistered;
        
        // Flag to show success dialog on next idle
        private bool _showSuccessDialog;
        
        // Flag to show error dialog on next idle
        private bool _showErrorDialog;
        
        // Flag to show cancel dialog on next idle
        private bool _showCancelDialog;
        
        // Result from sync operation
        private SyncResult _syncCompletedResult;
        
        // Error from sync operation
        private Exception _syncError;
        
        /// <summary>
        /// Handles Revit application idling events to show dialogs on the main thread
        /// </summary>
        private void OnApplicationIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            try
            {
                // Process only one dialog at a time
                if (_showSuccessDialog)
                {
                    _showSuccessDialog = false;
                    
                    // Success dialog
                    ShowSuccessDialog(_syncCompletedResult);
                    
                    // Unregister from idling
                    UnregisterIdling();
                }
                else if (_showErrorDialog && _syncError != null)
                {
                    _showErrorDialog = false;
                    
                    // Show error dialog
                    ShowErrorDialog(_syncError.Message);
                    
                    // Unregister from idling
                    UnregisterIdling();
                }
                else if (_showCancelDialog)
                {
                    _showCancelDialog = false;
                    
                    // Show cancellation message using our method
                    ShowCancellationDialog();
                    
                    // Unregister from idling
                    UnregisterIdling();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in idling event handler: {ex.Message}");
                
                // Unregister to prevent further errors
                UnregisterIdling();
            }
        }
        
        /// <summary>
        /// Unregisters from the Idling event
        /// </summary>
        private void UnregisterIdling()
        {
            if (_uiApplication != null && _idlingRegistered)
            {
                _uiApplication.Idling -= OnApplicationIdling;
                _idlingRegistered = false;
            }
        }
        
        /// <summary>
        /// Shows an error dialog with formatted error details
        /// </summary>
        /// <param name="errorMessage">Error message to display</param>
        private void ShowErrorDialog(string errorMessage)
        {
            // Format the error message for better readability
            string formattedError = errorMessage;
            string expandedInfo = string.Empty;
            
            // Split error details if message contains multiple parts
            if (errorMessage.Contains("\n"))
            {
                var parts = errorMessage.Split(new[] { "\n" }, 2, StringSplitOptions.None);
                formattedError = parts[0];
                expandedInfo = parts[1];
            }

            Autodesk.Revit.UI.TaskDialog dialog = new Autodesk.Revit.UI.TaskDialog("Sync Error")
            {
                MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning,
                MainInstruction = "Error synchronizing with Miller Craft Assistant",
                MainContent = formattedError,
                FooterText = $"Error time: {DateTime.Now:g}"
            };

            if (!string.IsNullOrEmpty(expandedInfo))
            {
                dialog.ExpandedContent = expandedInfo;
            }
            else
            {
                dialog.ExpandedContent = "Please check your network connection and try again.\n\n" +
                                        "If the problem persists, please contact Miller Craft support.";
            }

            dialog.Show();
        }

        /// <summary>
        /// Shows a success dialog with formatted details from the sync operation
        /// </summary>
        private void ShowSuccessDialog(SyncResult result)
        {
            // Handle different response actions per web app spec
            if (result.Action == "queue")
            {
                // New project - needs association in web app
                string mainContent = result.Message + "\n\n" +
                                   $"Queue ID: {result.QueueId}\n\n" +
                                   "Please open the web app to associate this Revit project with a Miller Craft project.";
                
                if (result.AvailableProjects != null && result.AvailableProjects.Count > 0)
                {
                    mainContent += "\n\nAvailable Projects:\n";
                    foreach (var proj in result.AvailableProjects)
                    {
                        mainContent += $"• {proj.Name}";
                        if (!string.IsNullOrEmpty(proj.Description))
                            mainContent += $" - {proj.Description}";
                        mainContent += "\n";
                    }
                }
                
                Autodesk.Revit.UI.TaskDialog dialog = new Autodesk.Revit.UI.TaskDialog("Project Association Required")
                {
                    MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconInformation,
                    MainInstruction = "New Project Detected",
                    MainContent = mainContent,
                    FooterText = "Click OK to open the web app",
                    CommonButtons = Autodesk.Revit.UI.TaskDialogCommonButtons.Ok | Autodesk.Revit.UI.TaskDialogCommonButtons.Cancel
                };
                
                Autodesk.Revit.UI.TaskDialogResult dialogResult = dialog.Show();
                
                // Open browser to queue page if user clicks OK
                if (dialogResult == Autodesk.Revit.UI.TaskDialogResult.Ok)
                {
                    try
                    {
                        System.Diagnostics.Process.Start("https://app.millercraftllc.com/revit/queue");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to open browser: {ex.Message}");
                    }
                }
            }
            else if (result.Action == "sync")
            {
                // Existing project - sync successful
                string mainContent = result.Message + "\n\n" +
                                   $"Project: {result.ProjectName}\n" +
                                   $"Parameters Updated: {result.Data?.ChangesApplied ?? 0}";
                
                // If formatted details aren't set, generate them
                if (string.IsNullOrEmpty(result.FormattedDetails))
                {
                    result.FormattedDetails = SyncResponseHandler.FormatSyncInitiationResult(result);
                }
                
                Autodesk.Revit.UI.TaskDialog dialog = new Autodesk.Revit.UI.TaskDialog("Sync Complete")
                {
                    MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconInformation,
                    MainInstruction = "Synchronization Successful",
                    MainContent = mainContent,
                    ExpandedContent = result.FormattedDetails,
                    FooterText = $"Sync time: {DateTime.Now:g}"
                };
                
                dialog.Show();
            }
            else
            {
                // Fallback for unexpected action types
                Autodesk.Revit.UI.TaskDialog dialog = new Autodesk.Revit.UI.TaskDialog("Sync Result")
                {
                    MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconInformation,
                    MainInstruction = result.Message ?? "Sync operation completed",
                    MainContent = result.FormattedDetails ?? "",
                    FooterText = $"Time: {DateTime.Now:g}"
                };
                
                dialog.Show();
            }
        }

        /// <summary>
        /// Shows a cancellation dialog when the user cancels the sync operation
        /// </summary>
        private void ShowCancellationDialog()
        {
            Autodesk.Revit.UI.TaskDialog dialog = new Autodesk.Revit.UI.TaskDialog("Sync Cancelled")
            {
                MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconInformation,
                MainInstruction = "Synchronization was cancelled",
                MainContent = "The sync operation was cancelled by user request.",
                FooterText = $"Cancelled at: {DateTime.Now:g}"
            };

            dialog.Show();
        }

        /// <summary>
        /// Main execution point for the Revit command
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Store UI application reference for threading
                _uiApplication = commandData.Application;
                
                // Get active document and project info
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                ProjectInfo projectInfo = doc.ProjectInformation;
                
                // Parameter name we're looking for
                string paramName = "sp.MC.ProjectGUID";
                
                // Check if parameter exists
                Parameter mcProjectGuidParam = projectInfo.LookupParameter(paramName);
                string existingGuid = "";
                bool hasExistingGuid = false;
                
                if (mcProjectGuidParam != null && !string.IsNullOrWhiteSpace(mcProjectGuidParam.AsString()))
                {
                    existingGuid = mcProjectGuidParam.AsString();
                    hasExistingGuid = true;
                }
                
                // Display dialog with current status and sync options
                string mainInstr = hasExistingGuid ? "Project GUID Found" : "Project GUID Not Found";
                string mainCont = hasExistingGuid 
                    ? $"Current GUID: {existingGuid}\n\nWhat would you like to do?" 
                    : "No Project GUID exists. A new one must be created before syncing.";
                
                // Create TaskDialog with appropriate options
                Autodesk.Revit.UI.TaskDialog taskDialog = new Autodesk.Revit.UI.TaskDialog("Miller Craft Tools");
                
                // Declare this variable in the outer scope so it's available throughout the method
                bool shouldRegenerateGuid = false;
                
                if (hasExistingGuid)
                {
                    // If GUID exists, give sync and regenerate options
                    taskDialog.MainInstruction = mainInstr;
                    taskDialog.MainContent = mainCont;
                    taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Sync with current GUID", 
                        "Sync project parameters to Miller Craft Assistant using the existing GUID.");
                    taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Regenerate GUID and sync", 
                        "Create a new GUID and then sync to Miller Craft Assistant.");
                    taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Cancel", 
                        "Do not sync at this time.");
                    taskDialog.DefaultButton = TaskDialogResult.CommandLink1;
                    
                    TaskDialogResult result = taskDialog.Show();
                    
                    if (result == TaskDialogResult.CommandLink3)
                    {
                        // User doesn't want to proceed
                        return Result.Cancelled;
                    }
                    
                    // If user chose to regenerate, set flag
                    shouldRegenerateGuid = (result == TaskDialogResult.CommandLink2);
                    
                    if (!shouldRegenerateGuid)
                    {
                        // User wants to sync with existing GUID
                        return SyncWithServer(doc, existingGuid);
                    }
                    // Otherwise fall through to regenerate code
                }
                else
                {
                    // If no GUID exists, simpler dialog
                    taskDialog.MainInstruction = mainInstr;
                    taskDialog.MainContent = mainCont;
                    taskDialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
                    taskDialog.DefaultButton = TaskDialogResult.Yes;
                    
                    TaskDialogResult result = taskDialog.Show();
                        
                    if (result == TaskDialogResult.No)
                    {
                        // User doesn't want to proceed
                        return Result.Cancelled;
                    }
                    
                    // If we're here and no existing GUID, we need to regenerate
                    shouldRegenerateGuid = true;
                }
                
                // Generate new GUID if needed
                if (shouldRegenerateGuid)
                {
                    using (Transaction tx = new Transaction(doc, "Set Miller Craft Project GUID"))
                    {
                        tx.Start();
                        
                        // Generate a new GUID
                        string newGuid = Guid.NewGuid().ToString();
                        
                        // Log for debugging
                        Logger.LogJson(new { Action = "Generated new ProjectGUID", GUID = newGuid }, "guid_generation");
                        
                        // Try to set the parameter
                        bool success = false;
                        string resultMessage = "";
                        
                        // First check if the parameter exists but is empty
                        if (mcProjectGuidParam != null)
                        {
                            if (!mcProjectGuidParam.IsReadOnly)
                            {
                                mcProjectGuidParam.Set(newGuid);
                                success = true;
                                resultMessage = $"Updated existing '{paramName}' parameter with new GUID.";
                                Logger.LogJson(new { Action = "Updated existing parameter", Parameter = paramName, GUID = newGuid }, "guid_storage");
                            }
                            else
                            {
                                resultMessage = $"Parameter '{paramName}' exists but is read-only.";
                                Logger.LogError(resultMessage);
                            }
                        }
                        else
                        {
                            // Check for other parameters we could use
                            string[] altParams = new[] { "MC.ProjectGUID", "MCProjectGUID", "Miller Craft GUID" };
                            foreach (string altParam in altParams)
                            {
                                Parameter alt = projectInfo.LookupParameter(altParam);
                                if (alt != null && !alt.IsReadOnly)
                                {
                                    alt.Set(newGuid);
                                    success = true;
                                    resultMessage = $"Used alternative parameter '{altParam}' for GUID storage.";
                                    Logger.LogJson(new { Action = "Used alternative parameter", Parameter = altParam, GUID = newGuid }, "guid_storage");
                                    break;
                                }
                            }
                            
                            // If no parameter, try to store in Project Name
                            if (!success)
                            {
                                Parameter projectNameParam = projectInfo.LookupParameter("Project Name");
                                if (projectNameParam != null && !projectNameParam.IsReadOnly)
                                {
                                    string currentName = projectNameParam.AsString() ?? string.Empty;
                                    
                                    // Create a marker string for our GUID
                                    string guidMarker = "[MC_GUID:";
                                    int markerIndex = currentName.IndexOf(guidMarker);
                                    
                                    // If the marker already exists, remove it
                                    if (markerIndex >= 0)
                                    {
                                        int endIndex = currentName.IndexOf("]", markerIndex);
                                        if (endIndex >= 0)
                                        {
                                            currentName = currentName.Substring(0, markerIndex).TrimEnd() + 
                                                        currentName.Substring(endIndex + 1);
                                        }
                                    }
                                    
                                    // Add the GUID to the project name
                                    string newName = currentName.TrimEnd() + " " + guidMarker + newGuid + "]";
                                    projectNameParam.Set(newName);
                                    success = true;
                                    resultMessage = "Stored GUID in Project Name parameter.";
                                    Logger.LogJson(new { Action = "Stored GUID in Project Name", GUID = newGuid }, "guid_storage");
                                }
                            }
                        }
                        
                        tx.Commit();
                        
                        // Show the result to the user
                        if (success)
                        {
                            Autodesk.Revit.UI.TaskDialog successDialog = new Autodesk.Revit.UI.TaskDialog("GUID Operation Successful")
                            {
                                MainInstruction = "Successfully generated and stored Project GUID",
                                MainContent = $"{newGuid}\n\n{resultMessage}"
                            };
                            successDialog.Show();
                            
                            // Ask if they want to sync now
                            Autodesk.Revit.UI.TaskDialog syncPrompt = new Autodesk.Revit.UI.TaskDialog("Sync Project")
                            {
                                MainInstruction = "Would you like to sync project data now?",
                                MainContent = "This will send your project parameters to Miller Craft Assistant.",
                                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                                DefaultButton = TaskDialogResult.Yes
                            };
                            
                            if (syncPrompt.Show() == TaskDialogResult.Yes)
                            {
                                return SyncWithServer(doc, newGuid);
                            }
                            
                            return Result.Succeeded;
                        }
                        else
                        {
                            Autodesk.Revit.UI.TaskDialog failureDialog = new Autodesk.Revit.UI.TaskDialog("GUID Operation Failed")
                            {
                                MainInstruction = "Failed to store Project GUID",
                                MainContent = resultMessage
                            };
                            failureDialog.Show();
                            return Result.Failed;
                        }
                    }
                }
                else
                {
                    // This code is not reached in the new flow
                }
            }
            catch (Exception ex)
            {
                // Create error dialog
                Autodesk.Revit.UI.TaskDialog errorDialog = new Autodesk.Revit.UI.TaskDialog("Error")
                {
                    MainInstruction = "Failed to work with Project GUID",
                    MainContent = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                errorDialog.Show();
                
                return Result.Failed;
            }
            
            return Result.Succeeded;
        }
        
        /// <summary>
        /// Syncs the project data with the Miller Craft Assistant server
        /// </summary>
        private Result SyncWithServer(Document doc, string projectGuid)
        {
            // Create a TaskCompletionSource for sync result
            TaskCompletionSource<SyncResult> syncTcs = new TaskCompletionSource<SyncResult>();
            
            // Create cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                // Create non-modal progress form
                System.Windows.Forms.Form progressForm = new System.Windows.Forms.Form()
                {
                    Text = "Miller Craft Tools",
                    Width = 400,
                    Height = 150,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ControlBox = true
                };
                
                // Add progress components
                System.Windows.Forms.Label titleLabel = new System.Windows.Forms.Label()
                {
                    Text = "Syncing with Miller Craft Assistant",
                    AutoSize = true,
                    Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold),
                    Location = new System.Drawing.Point(20, 15)
                };
                
                System.Windows.Forms.Label statusLabel = new System.Windows.Forms.Label()
                {
                    Text = "Starting sync process...",
                    AutoSize = true,
                    Location = new System.Drawing.Point(20, 45)
                };
                
                System.Windows.Forms.ProgressBar progressBar = new System.Windows.Forms.ProgressBar()
                {
                    Width = 360,
                    Height = 20,
                    Location = new System.Drawing.Point(20, 75),
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };
                
                System.Windows.Forms.Button cancelButton = new System.Windows.Forms.Button()
                {
                    Text = "Cancel",
                    Location = new System.Drawing.Point(290, 75),
                    Width = 90
                };
                
                progressForm.Controls.Add(titleLabel);
                progressForm.Controls.Add(statusLabel);
                progressForm.Controls.Add(progressBar);
                progressForm.Controls.Add(cancelButton);
                
                // Create a sync complete flag to track when we're done
                bool syncComplete = false;
                
                // Set up cancel button handler
                cancelButton.Click += (s, e) => 
                {
                    _cancellationTokenSource.Cancel();
                    statusLabel.Text = "Cancelling...";
                    cancelButton.Enabled = false;
                };
                
                // Set up form closing handler
                progressForm.FormClosing += (s, e) =>
                {
                    // Only cancel if we're not already done
                    if (!syncComplete)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                };
                
                // Create progress handler to update the UI using a proper tuple type
                // Use explicit Tuple<T1,T2> instead of ValueTuple to avoid C# 7 tuple syntax issues
                _progressReporter = (IProgress<Tuple<string, int>>)new Progress<Tuple<string, int>>(progress => 
                {
                    // Check if the form is still visible
                    if (progressForm.IsDisposed) return;
                    
                    // Use BeginInvoke to avoid cross-thread operation issues
                    progressForm.BeginInvoke(new Action(() =>
                    {
                        statusLabel.Text = progress.Item1; // Item1 = message
                        progressBar.Value = progress.Item2;  // Item2 = progressPercent
                    }));
                });
                
                // Show the progress form
                progressForm.Show();
                
                // Start the sync operation on a background thread
                Task.Run(async () =>
                {
                    try
                    {
                        // Create the sync service with the unified API endpoints
                        var syncService = new SyncServiceV2(
                            progressHandler: _progressReporter,
                            cancellationToken: _cancellationTokenSource.Token,
                            useNewEndpoints: true); // Use new unified endpoints as primary
                        
                        // Execute sync operation
                        SyncResult result = await syncService.InitiateSyncAsync(doc, projectGuid);
                        
                        // Store the sync ID for status checking
                        _latestSyncId = result.SyncId;
                        
                        // Set sync as complete
                        syncComplete = true;
                        
                        // Complete the task with the result
                        syncTcs.SetResult(result);
                        
                        // Close the progress form
                        progressForm.BeginInvoke(new Action(() => 
                        {
                            try
                            {
                                progressForm.Close();
                                progressForm.Dispose();
                            }
                            catch {}
                        }));
                        
                        // Set result data for showing dialogs
                        _syncCompletedResult = result;
                        
                        // We can't directly show the dialog from a background thread
                        // Instead, we'll use Revit's External Events pattern
                        // For this quick fix, we'll just set a flag to show the dialog on the next Idling event
                        _showSuccessDialog = true;
                        
                        // Register for the application Idling event if not already registered
                        if (_uiApplication != null && !_idlingRegistered)
                        {
                            _uiApplication.Idling += OnApplicationIdling;
                            _idlingRegistered = true;
                        }
                        else
                        {
                            TelemetryLogger.LogInfo("Unable to register for Idling events - success dialog will not be shown");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Set sync as complete
                        syncComplete = true;
                        
                        // User canceled
                        Logger.LogError("Sync operation was canceled by the user");
                        
                        // Close the progress form
                        progressForm.BeginInvoke(new Action(() => 
                        {
                            try
                            {
                                progressForm.Close();
                                progressForm.Dispose();
                            }
                            catch {}
                        }));
                        
                        // Save document reference for the Idling handler
                        _currentDocument = doc;
                        
                        // Set flag to show cancellation dialog on next Idling event
                        _showCancelDialog = true;
                        
                        // Register for the application Idling event if not already registered
                        if (_uiApplication != null && !_idlingRegistered)
                        {
                            _uiApplication.Idling += OnApplicationIdling;
                            _idlingRegistered = true;
                        }
                        else
                        {
                            TelemetryLogger.LogInfo("Unable to register for Idling events - cancellation dialog will not be shown");
                        }
                        
                        // Signal cancellation
                        syncTcs.SetCanceled();
                    }
                    catch (Exception ex)
                    {
                        // Set sync as complete
                        syncComplete = true;
                        
                        // Log error and complete with failure
                        Logger.LogError($"Sync failed: {ex.Message}");
                        
                        // Close the progress form
                        progressForm.BeginInvoke(new Action(() => 
                        {
                            try
                            {
                                progressForm.Close();
                                progressForm.Dispose();
                            }
                            catch {}
                        }));
                        
                        // Save document reference and error for the Idling handler
                        _currentDocument = doc;
                        _syncError = ex;
                        
                        // Set flag to show error dialog on next Idling event
                        _showErrorDialog = true;
                        
                        // Register for the application Idling event if not already registered
                        if (_uiApplication != null && !_idlingRegistered)
                        {
                            _uiApplication.Idling += OnApplicationIdling;
                            _idlingRegistered = true;
                        }
                        else
                        {
                            TelemetryLogger.LogInfo("Unable to register for Idling events - error dialog will not be shown");
                        }
                        
                        // Signal error
                        syncTcs.SetException(ex);
                    }
                }, _cancellationTokenSource.Token);
                
                // Return Success immediately - the background task will handle the UI feedback
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Cleanup
                _cancellationTokenSource?.Cancel();
                
                // Create error dialog for initialization errors
                Autodesk.Revit.UI.TaskDialog errorDialog = new Autodesk.Revit.UI.TaskDialog("Error")
                {
                    MainInstruction = "Failed to start sync with server",
                    MainContent = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                errorDialog.Show();
                
                Logger.LogError($"Failed to start sync with server: {ex.Message}");
                return Result.Failed;
            }
        }
        
        /// <summary>
        /// Starts the status checking process for a sync operation
        /// </summary>
        private void StartStatusChecking(Document doc, string syncId)
        {
            try
            {
                // Create the dialog first to avoid issues with cross-thread UI operations
                Autodesk.Revit.UI.TaskDialog statusCheckDialog = new Autodesk.Revit.UI.TaskDialog("Miller Craft Tools")
                {
                    MainInstruction = "Checking Sync Status",
                    MainContent = "Waiting for updates from Miller Craft Assistant...",
                    CommonButtons = TaskDialogCommonButtons.Close,
                    FooterText = "The plugin will periodically check for updates."
                };
                
                // Create progress reporter for status updates using standard Tuple
                IProgress<Tuple<string, int>> statusProgress = new Progress<Tuple<string, int>>(progress =>
                {
                    // Update dialog content
                    statusCheckDialog.MainContent = progress.Item1; // Item1 = message
                });
                
                // Create cancellation token source
                var statusCts = new CancellationTokenSource();
                
                // Create the sync service
                var syncService = new SyncServiceV2(
                    progressHandler: statusProgress,
                    cancellationToken: statusCts.Token);
                
                // Create state object to hold references needed by the timer callback
                var timerState = new TimerState {
                    SyncService = syncService,
                    SyncId = syncId,
                    Document = doc
                };
                
                // Create the callback
                System.Threading.TimerCallback timerCallback = new System.Threading.TimerCallback(async obj => 
                {
                    // Get our state object
                    var state = (TimerState)obj;
                    
                    try
                    {
                        // Check the status
                        Miller_Craft_Tools.Model.SyncStatus status = await state.SyncService.CheckSyncStatusAsync(state.SyncId);
                        
                        // If there are changes to apply, show the review dialog
                        if (status.HasChangesToApply)
                        {
                            // Stop the timer
                            state.Timer?.Dispose();
                            
                            // Show the change review dialog on the UI thread
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                try
                                {
                                    // Create the change review dialog
                                    using (var reviewDialog = new ChangeReviewDialog(state.Document, state.SyncId, status.WebChanges, state.SyncService))
                                    {
                                        // Show the dialog
                                        reviewDialog.ShowDialog();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError($"Error showing change review dialog: {ex.Message}");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error checking sync status: {ex.Message}");
                    }
                });
                
                // Create and start the timer
                System.Threading.Timer statusTimer = new System.Threading.Timer(
                    timerCallback,
                    timerState,
                    TimeSpan.FromSeconds(5), // Start after 5 seconds
                    TimeSpan.FromMinutes(5)); // Check every 5 minutes
                
                // Store the timer in the state object so it can be disposed in the callback
                timerState.Timer = statusTimer;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error starting status checking: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Called when progress is reported from the sync service
        /// </summary>
        private void ProgressCallback((string message, int progressPercent) progress)
        {
            // Create a proper Tuple from the ValueTuple parameter
            Tuple<string, int> progressTuple = new Tuple<string, int>(progress.message, progress.progressPercent);
            
            // Format the progress message using the SyncResponseHandler if needed
            string formattedMessage = progress.message;
            int progressPercent = progress.progressPercent;
            
            // Check for additional explanation to display
            var explanation = SyncResponseHandler.GetStatusExplanation(formattedMessage);
            if (!string.IsNullOrEmpty(explanation))
            {
                formattedMessage = $"{formattedMessage} - {explanation}";
            }
            
            // Use standard Tuple instead of ValueTuple to ensure compatibility
            var progressData = new Tuple<string, int>(formattedMessage, progressPercent);
            _progressReporter?.Report(progressData);
            
            // Log progress for telemetry
            Logger.LogInfo($"Sync progress: {formattedMessage} ({progressPercent}%)");
        }
    }
}
