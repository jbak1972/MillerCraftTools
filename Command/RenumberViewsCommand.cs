using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.Command
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RenumberViewsCommand : IExternalCommand
    {
        // Use a static class for event handlers to avoid garbage collection issues
        public static class CommandController
        {
            // State properties
            public static bool IsActive { get; set; } = false;
            public static bool IsFinished { get; set; } = false;
            public static bool IsCancelled { get; set; } = false;

            // Synchronization event for command completion
            private static ManualResetEvent _commandCompletionEvent = new ManualResetEvent(false);
            public static ManualResetEvent CommandCompletionEvent { get { return _commandCompletionEvent; } }

            // UI references - these may be null if app hasn't fully initialized
            public static RibbonPanel ContextPanel { get; set; } = null;
            public static PushButton FinishButton { get; set; } = null;
            public static PushButton CancelButton { get; set; } = null;

            public static void OnFinishCommand(object sender, EventArgs e)
            {
                IsFinished = true;
                CommandCompletionEvent.Set();
            }

            public static void OnCancelCommand(object sender, EventArgs e)
            {
                IsCancelled = true;
                CommandCompletionEvent.Set();
            }

            public static void Reset()
            {
                IsActive = false; // Start with inactive state
                IsFinished = false;
                IsCancelled = false;
                CommandCompletionEvent.Reset();

                // Don't clear panel and button references - these come from app initialization
                // and need to be preserved between command executions
            }
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Set up unique error identifier for tracking in journal
                string errorId = Guid.NewGuid().ToString().Substring(0, 8);
                int startTick = Environment.TickCount; // For timing command execution

                try
                {
                    // Log start of command execution using consolidated logging
                    Logger.LogJsonConsolidatedNonBlocking(new { Action = "Command Start", Id = errorId, Command = "RenumberViewsCommand" }, "cmd_start", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                    // Reset CommandController state before starting
                    CommandController.Reset();

                    // Get basic Revit context
                    UIApplication uiapp = commandData.Application;
                    UIDocument uidoc = uiapp.ActiveUIDocument;
                    if (uidoc == null)
                    {
                        message = "No active document found.";
                        return Result.Failed;
                    }

                    Document doc = uidoc.Document;

                    // Log document and view info
                    Logger.LogJsonConsolidatedNonBlocking(new
                    {
                        Action = "Document Info",
                        Id = errorId,
                        Document = doc.Title,
                        ViewName = doc.ActiveView?.Name,
                        ViewId = doc.ActiveView != null ? doc.ActiveView.Id.ToString() : "",
                        ViewType = doc.ActiveView?.GetType().Name
                    }, "cmd_context", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                    // Verify we are in a sheet view before continuing
                    Autodesk.Revit.DB.View activeView = doc.ActiveView;
                    if (!(activeView is ViewSheet))
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("View Renumbering",
                            "This command can only be used when a sheet is active.\n\n" +
                            "Please open a sheet view and try again.");
                        return Result.Cancelled;
                    }

                    // Enable the buttons on the ribbon if they exist
                    try
                    {
                        CreateContextualRibbon(uiapp);
                        Logger.LogJsonConsolidatedNonBlocking(new { Action = "UI Buttons Enabled", Id = errorId }, "cmd_ui", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                    }
                    catch (Exception ex)
                    {
                        // Log but continue - the command can still work without UI buttons
                        Logger.LogJsonConsolidatedNonBlocking(new { Action = "UI Error", Id = errorId, Error = ex.Message }, "ribbon_error", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                        Logger.LogError($"ID: {errorId} - Could not enable command buttons: {ex.Message}");

                        // Still show a warning to the user so they know
                        Autodesk.Revit.UI.TaskDialog.Show("UI Warning",
                            "Could not enable command buttons.\n\n" +
                            "The command will continue but you may need to use ESC to exit.");
                    }

                    // Set a timeout for the command
                    // Allow longer timeout for renumbering workflow which may involve multiple selections
                    int timeoutSeconds = 120;
                    bool commandTimedOut = false;
                    System.Threading.Timer timeoutTimer = new System.Threading.Timer(state =>
                    {
                        commandTimedOut = true;
                        CommandController.IsCancelled = true;
                        CommandController.CommandCompletionEvent.Set();
                    }, null, timeoutSeconds * 1000, Timeout.Infinite);

                    // Mark command as active
                    CommandController.IsActive = true;

                    try
                    {
                        // Create the drafting controller directly in the main thread
                        // This avoids threading issues with the Revit API
                        DraftingController controller = new DraftingController(doc, uidoc);
                        RenumberViewsContextHandler handler = new RenumberViewsContextHandler(controller);

                        // Execute the handler directly
                        Logger.LogJsonConsolidatedNonBlocking(new { Action = "Handler Execution Start", Id = errorId }, "cmd_execution", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                        handler.Execute();
                        Logger.LogJsonConsolidatedNonBlocking(new { Action = "Handler Execution Complete", Id = errorId }, "cmd_execution", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                        // If we get here normally and command hasn't been finalized, mark it as finished
                        if (CommandController.IsActive && !CommandController.IsFinished && !CommandController.IsCancelled)
                        {
                            CommandController.IsFinished = true;
                            CommandController.CommandCompletionEvent.Set();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log detailed error information
                        Logger.LogJsonConsolidatedNonBlocking(new
                        {
                            Action = "Handler Error",
                            Id = errorId,
                            Error = ex.Message,
                            StackTrace = ex.StackTrace,
                            InnerException = ex.InnerException?.Message
                        }, "cmd_error", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                        // Show a simpler error message to the user
                        Autodesk.Revit.UI.TaskDialog.Show("Renumbering Error",
                            $"An error occurred during the renumbering process: {ex.Message}\n\n" +
                            "Check the log files for more details.");

                        // Signal completion with failure
                        CommandController.IsCancelled = true;
                        CommandController.CommandCompletionEvent.Set();
                    }

                    // Wait for completion or cancellation or timeout
                    bool signalReceived = CommandController.CommandCompletionEvent.WaitOne(timeoutSeconds * 1000);
                    timeoutTimer.Dispose();

                    if (commandTimedOut)
                    {
                        Logger.LogJsonConsolidatedNonBlocking(new
                        {
                            Action = "Command Timeout",
                            Id = errorId,
                            TimeoutSeconds = timeoutSeconds
                        }, "cmd_timeout", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                        Autodesk.Revit.UI.TaskDialog.Show("Operation Timeout",
                            "The renumbering command has timed out.\n\n" +
                            "Please try again or check the log files for details.");
                    }

                    // Log command completion status
                    Logger.LogJsonConsolidatedNonBlocking(new
                    {
                        Action = "Command Status",
                        Id = errorId,
                        SignalReceived = signalReceived,
                        IsFinished = CommandController.IsFinished,
                        IsCancelled = CommandController.IsCancelled,
                        ElapsedMilliseconds = Environment.TickCount - startTick
                    }, "cmd_status", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                    CommandController.IsActive = false;

                    // Clean up UI
                    try
                    {
                        RemoveContextualRibbon(uiapp);
                        Logger.LogJsonConsolidatedNonBlocking(new { Action = "UI Cleanup Success", Id = errorId }, "cmd_ui", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogJsonConsolidatedNonBlocking(new { Action = "UI Cleanup Error", Id = errorId, Error = ex.Message }, "ribbon_error", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                        Logger.LogError($"ID: {errorId} - Error during UI cleanup: {ex.Message}");
                    }

                    // Return the appropriate result
                    return CommandController.IsFinished ? Result.Succeeded : Result.Cancelled;
                }
                catch (Exception ex)
                {
                    // Log detailed error information for uncaught exceptions
                    Logger.LogJsonConsolidatedNonBlocking(new
                    {
                        Action = "Unhandled Command Error",
                        Id = errorId,
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        InnerException = ex.InnerException?.Message,
                        ElapsedMilliseconds = Environment.TickCount - startTick
                    }, "cmd_error", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                    // Show a simplified error to the user
                    Autodesk.Revit.UI.TaskDialog.Show("Renumbering Error",
                        "An unexpected error occurred during the renumbering process.\n\n" +
                        "Please check the log files for details.");

                    message = $"Error ID {errorId}: {ex.Message}";
                    return Result.Failed;
                }
            }
            catch (Exception outerEx)
            {
                // Last-resort catch for truly catastrophic errors
                Logger.LogError($"CRITICAL ERROR: {outerEx.Message}");
                Logger.LogJsonConsolidatedNonBlocking(new
                {
                    Action = "Critical Error",
                    Error = outerEx.Message,
                    StackTrace = outerEx.StackTrace,
                    InnerException = outerEx.InnerException?.Message
                }, "critical_error", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                // Still show an error dialog to the user
                Autodesk.Revit.UI.TaskDialog.Show("Critical Error",
                    "A critical error occurred during command execution.\n\n" +
                    "Please report this error to technical support.");

                message = $"Critical error: {outerEx.Message}";
                return Result.Failed;
            }
        }
        /// <summary>
        /// Enables the existing Finish and Cancel buttons on the ribbon when command is active
        /// </summary>
        private void CreateContextualRibbon(UIApplication uiApplication)
        {
            // Generate a unique operation ID for tracking
            string operationId = Guid.NewGuid().ToString().Substring(0, 8);
            Logger.LogJsonConsolidatedNonBlocking(new
            {
                Action = "Enable Ribbon Buttons",
                Id = operationId,
                FinishButtonExists = CommandController.FinishButton != null,
                CancelButtonExists = CommandController.CancelButton != null
            }, "ribbon_ui", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

            try
            {
                // Enable the buttons that were created during application startup
                if (CommandController.FinishButton != null)
                {
                    CommandController.FinishButton.Enabled = true;
                    Logger.LogJsonConsolidatedNonBlocking(new { Action = "Button Enabled", Id = operationId, Button = "Finish" }, "ribbon_ui", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                }

                if (CommandController.CancelButton != null)
                {
                    CommandController.CancelButton.Enabled = true;
                    Logger.LogJsonConsolidatedNonBlocking(new { Action = "Button Enabled", Id = operationId, Button = "Cancel" }, "ribbon_ui", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't let it stop execution
                Logger.LogJsonConsolidatedNonBlocking(new
                {
                    Action = "Ribbon UI Error",
                    Id = operationId,
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                }, "ribbon_error", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                // Only show a UI error if this is critical
                if (ex is System.NullReferenceException)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("UI Error", "Could not enable command buttons. Command will continue.");
                }
            }
        }

        /// <summary>
        /// Disables the Finish and Cancel buttons when command completes
        /// </summary>
        private void RemoveContextualRibbon(UIApplication application)
        {
            // Generate a unique operation ID for tracking
            string operationId = Guid.NewGuid().ToString().Substring(0, 8);
            Logger.LogJsonConsolidatedNonBlocking(new
            {
                Action = "Disable Ribbon Buttons",
                Id = operationId,
                FinishButtonExists = CommandController.FinishButton != null,
                CancelButtonExists = CommandController.CancelButton != null
            }, "ribbon_ui", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

            try
            {
                // Disable the buttons when the command is not active
                if (CommandController.FinishButton != null)
                {
                    CommandController.FinishButton.Enabled = false;
                    Logger.LogJsonConsolidatedNonBlocking(new { Action = "Button Disabled", Id = operationId, Button = "Finish" }, "ribbon_ui", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                }

                if (CommandController.CancelButton != null)
                {
                    CommandController.CancelButton.Enabled = false;
                    Logger.LogJsonConsolidatedNonBlocking(new { Action = "Button Disabled", Id = operationId, Button = "Cancel" }, "ribbon_ui", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");
                }

                // We keep the references since they're created during application startup
                // and will be reused the next time the command is run
            }
            catch (Exception ex)
            {
                // Log the error but don't let it stop execution
                Logger.LogJsonConsolidatedNonBlocking(new
                {
                    Action = "Ribbon UI Error",
                    Id = operationId,
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                }, "ribbon_error", $"renumber_views_{DateTime.Now:yyyy-MM-dd}.json");

                // Only show a UI error if this is critical
                if (ex is System.NullReferenceException)
                {
                    Autodesk.Revit.UI.TaskDialog.Show("UI Error", "Could not access ribbon UI elements.");
                }
            }
        }

        /// <summary>
        /// Gets the image source for the specified image name
        /// </summary>
        private BitmapImage GetImageSource(string imageName)
        {
            return new BitmapImage(new Uri(
                $"pack://application:,,,/{GetType().Assembly.GetName().Name};component/Resources/{imageName}",
                UriKind.Absolute));
        }
    }
}