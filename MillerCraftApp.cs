using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.ViewModel;
using Miller_Craft_Tools.Views;
using Miller_Craft_Tools.UI.Controls;
using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace Miller_Craft_Tools
{
    public class MillerCraftApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Configure TLS to use modern protocols (TLS 1.2 and TLS 1.3)
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                
                // Log TLS configuration
                Utils.Logger.LogInfo($"TLS Configuration: {ServicePointManager.SecurityProtocol}");

                // Create a custom ribbon tab
                string tabName = "Miller Craft Tools";
                application.CreateRibbonTab(tabName);

                // Create a ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Project Maintenance");

                // Add "Audit Model" button
                PushButtonData auditButtonData = new PushButtonData("AuditModelButton", "Audit Model", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Command.AuditModelCommand");
                auditButtonData.ToolTip = "Audit the model and display statistics like file size and element counts.";
                auditButtonData.LongDescription = "This tool analyzes the current Revit model and provides statistics such as file size, element count, family count, warnings, DWG imports, and schema sizes.";
                
                string auditIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "Audit_Model_32.png"
                );
                if (System.IO.File.Exists(auditIconPath))
                {
                    var auditImage = new System.Windows.Media.Imaging.BitmapImage();
                    auditImage.BeginInit();
                    auditImage.UriSource = new Uri(auditIconPath, UriKind.Absolute);
                    auditImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    auditImage.EndInit();
                    auditButtonData.LargeImage = auditImage;
                }
                
                PushButton auditButton = panel.AddItem(auditButtonData) as PushButton;

                // Add "Renumber Views on Sheet" button
                PushButtonData renumberViewsButtonData = new PushButtonData("RenumberViewsButton", "# Views", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Command.RenumberViewsCommand");
                renumberViewsButtonData.ToolTip = "Renumber views on a selected sheet.";
                renumberViewsButtonData.LongDescription = "This tool allows you to renumber the detail numbers of viewports on a sheet by selecting them in sequence.";

                string viewIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "RenumViews32.png"
                    );

                if (System.IO.File.Exists(viewIconPath))
                {
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(viewIconPath, UriKind.Absolute);
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.EndInit();
                    renumberViewsButtonData.LargeImage = image;
                }
                panel.AddItem(renumberViewsButtonData);
                
                // Add Finish and Cancel buttons (initially disabled)
                PushButtonData finishButtonData = new PushButtonData(
                    "FinishRenumberCommand",
                    "Finish #",
                    Assembly.GetExecutingAssembly().Location,
                    "Miller_Craft_Tools.Command.FinishRenumberingHandler");
                
                finishButtonData.ToolTip = "Finish renumbering and commit changes";
                
                // Load the check.png image from resources
                Uri checkIconUri = new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Resources/check.png", UriKind.Absolute);
                finishButtonData.LargeImage = new System.Windows.Media.Imaging.BitmapImage(checkIconUri);
                
                PushButton finishButton = panel.AddItem(finishButtonData) as PushButton;
                finishButton.Enabled = false; // Initially disabled
                
                PushButtonData cancelButtonData = new PushButtonData(
                    "CancelRenumberCommand",
                    "Cancel #",
                    Assembly.GetExecutingAssembly().Location,
                    "Miller_Craft_Tools.Command.CancelRenumberingHandler");
                
                cancelButtonData.ToolTip = "Cancel renumbering and discard changes";
                
                // Load the cancel.png image from resources
                Uri cancelIconUri = new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Resources/cancel.png", UriKind.Absolute);
                cancelButtonData.LargeImage = new System.Windows.Media.Imaging.BitmapImage(cancelIconUri);
                
                PushButton cancelButton = panel.AddItem(cancelButtonData) as PushButton;
                cancelButton.Enabled = false; // Initially disabled
                
                // Store references to these buttons in a static class for later access
                Command.RenumberViewsCommand.CommandController.FinishButton = finishButton;
                Command.RenumberViewsCommand.CommandController.CancelButton = cancelButton;
                Command.RenumberViewsCommand.CommandController.ContextPanel = panel;

                // Add "Renumber Windows" button
                PushButtonData renumberWindowsButtonData = new PushButtonData("RenumberWindowsButton", "# Windows", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Command.RenumberWindowsCommand");
                renumberWindowsButtonData.ToolTip = "Renumber windows in the model.";
                renumberWindowsButtonData.LongDescription = "This tool allows you to renumber windows by assigning new mark values, resolving conflicts automatically.";

                string windowIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "RenumWindows32.png"
                    );

                if (System.IO.File.Exists(viewIconPath))
                {
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(windowIconPath, UriKind.Absolute);
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.EndInit();
                    renumberWindowsButtonData.LargeImage = image;
                }
                panel.AddItem(renumberWindowsButtonData);

                // Add "Sync sp.Area" button
                PushButtonData syncAreaButtonData = new PushButtonData("SyncAreaButton", "Sync sp.Area", Assembly.GetExecutingAssembly().Location, "Miller_Craft_Tools.Command.SyncFilledRegionsCommand");
                syncAreaButtonData.ToolTip = "Sync sp.Area parameter with Area for filled regions.";
                syncAreaButtonData.LongDescription = "This tool updates the sp.Area parameter of filled regions to match their Area parameter.";
                
                string syncAreaIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "Synch_Area_32.png"
                );
                if (System.IO.File.Exists(syncAreaIconPath))
                {
                    var syncAreaImage = new System.Windows.Media.Imaging.BitmapImage();
                    syncAreaImage.BeginInit();
                    syncAreaImage.UriSource = new Uri(syncAreaIconPath, UriKind.Absolute);
                    syncAreaImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    syncAreaImage.EndInit();
                    syncAreaButtonData.LargeImage = syncAreaImage;
                }
                
                PushButton syncAreaButton = panel.AddItem(syncAreaButtonData) as PushButton;

                // Add "MatSynch" button
                var matSynchData = new PushButtonData(
                    "MatSynchButton",
                    "MatSynch",
                    Assembly.GetExecutingAssembly().Location,
                    "Miller_Craft_Tools.Command.MaterialSyncCommand"
                )
                {
                    ToolTip = "Synchronize window & door materials from Global Parameters",
                    LongDescription = "Reads the four Fenestration global parameters and maps their material values onto each placed window and door type's shared parameters."
                };
                
                string matSynchIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "Material_Synch_32.png"
                );
                if (System.IO.File.Exists(matSynchIconPath))
                {
                    var matSynchImage = new System.Windows.Media.Imaging.BitmapImage();
                    matSynchImage.BeginInit();
                    matSynchImage.UriSource = new Uri(matSynchIconPath, UriKind.Absolute);
                    matSynchImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    matSynchImage.EndInit();
                    matSynchData.LargeImage = matSynchImage;
                }
                
                panel.AddItem(matSynchData);

                // Add "Material Management" button
                var materialManagementData = new PushButtonData(
                    "MaterialManagementButton",
                    "Mat Manage",
                    Assembly.GetExecutingAssembly().Location,
                    "Miller_Craft_Tools.Command.MaterialManagementCommand"
                )
                {
                    ToolTip = "Material management utilities",
                    LongDescription = "Purge materials with non-English characters and standardize material names with proper spacing."
                };
                
                string matManageIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "Material_Manage_32.png"
                );
                if (System.IO.File.Exists(matManageIconPath))
                {
                    var matManageImage = new System.Windows.Media.Imaging.BitmapImage();
                    matManageImage.BeginInit();
                    matManageImage.UriSource = new Uri(matManageIconPath, UriKind.Absolute);
                    matManageImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    matManageImage.EndInit();
                    materialManagementData.LargeImage = matManageImage;
                }
                
                panel.AddItem(materialManagementData);

                // Add "Wall Assembly Standardizer" button
                var wallAssemblyStandardizerData = new PushButtonData(
                    "WallAssemblyStandardizerButton",
                    "Wall Std",
                    Assembly.GetExecutingAssembly().Location,
                    "Miller_Craft_Tools.Command.WallAssemblyStandardizerCommand"
                )
                {
                    ToolTip = "Standardize wall assemblies",
                    LongDescription = "Rename existing wall types to standard naming conventions and create standard wall assemblies using materials with 'ZOOT - ' prefix."
                };
                
                string wallStdIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "Wall_Standard_32.png"
                );
                if (System.IO.File.Exists(wallStdIconPath))
                {
                    var wallStdImage = new System.Windows.Media.Imaging.BitmapImage();
                    wallStdImage.BeginInit();
                    wallStdImage.UriSource = new Uri(wallStdIconPath, UriKind.Absolute);
                    wallStdImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    wallStdImage.EndInit();
                    wallAssemblyStandardizerData.LargeImage = wallStdImage;
                }
                
                panel.AddItem(wallAssemblyStandardizerData);

                // Add "Web App" button - Unified web integration dialog
                var webAppData = new PushButtonData(
                    "WebAppButton",
                    "Web App",
                    Assembly.GetExecutingAssembly().Location,
                    "Miller_Craft_Tools.Command.WebAppSyncCommand"
                )
                {
                    ToolTip = "Web App Integration - Sync, Connection, and Diagnostics",
                    LongDescription = "Opens the unified Web App Integration dialog for connection management, project synchronization, and API diagnostics."
                };

                // Set icon for the button
                string webAppIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "Globe_Synch_32.png"
                );
                if (System.IO.File.Exists(webAppIconPath))
                {
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(webAppIconPath, UriKind.Absolute);
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.EndInit();
                    webAppData.LargeImage = image;
                }
                panel.AddItem(webAppData);

                // Add "Clear Project Info" button
                var clearProjectInfoData = new PushButtonData(
                    "ClearProjectInfoButton",
                    "Clr Info",
                    Assembly.GetExecutingAssembly().Location,
                    "Miller_Craft_Tools.Command.ClearProjectInfoCommand"
                )
                {
                    ToolTip = "Clear all project-specific information, including the MC Project GUID. Use this when starting a new project from a copy.",
                    LongDescription = "Removes all editable Project Information parameters, including the MC Project GUID. Use before first sync when starting a new project from a copy."
                };
                string clearIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "Clean32.png"
                );
                if (System.IO.File.Exists(clearIconPath))
                {
                    var clearImage = new System.Windows.Media.Imaging.BitmapImage();
                    clearImage.BeginInit();
                    clearImage.UriSource = new Uri(clearIconPath, UriKind.Absolute);
                    clearImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    clearImage.EndInit();
                    clearProjectInfoData.LargeImage = clearImage;
                }
                panel.AddItem(clearProjectInfoData);

                // Add "Compare View Templates" button
                var compareViewTemplatesData = new PushButtonData(
                    "CompareViewTemplatesButton",
                    "Compare Templates",
                    Assembly.GetExecutingAssembly().Location,
                    "Miller_Craft_Tools.Command.CompareViewTemplatesCommand"
                )
                {
                    ToolTip = "Compare settings between two view templates",
                    LongDescription = "Creates a comparison report that highlights differences between two selected view templates to help troubleshoot visibility issues."
                };

                // Set icon for the Compare View Templates button if the file exists
                string compareIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "CompareTemplate32.png"
                );
                if (System.IO.File.Exists(compareIconPath))
                {
                    var compareImage = new System.Windows.Media.Imaging.BitmapImage();
                    compareImage.BeginInit();
                    compareImage.UriSource = new Uri(compareIconPath, UriKind.Absolute);
                    compareImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    compareImage.EndInit();
                    compareViewTemplatesData.LargeImage = compareImage;
                }

                // Also add small icon (16x16) for when the ribbon is collapsed
                string compareSmallIconPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Resources",
                    "CompareTemplate16.png"
                );
                if (System.IO.File.Exists(compareSmallIconPath))
                {
                    var compareSmallImage = new System.Windows.Media.Imaging.BitmapImage();
                    compareSmallImage.BeginInit();
                    compareSmallImage.UriSource = new Uri(compareSmallIconPath, UriKind.Absolute);
                    compareSmallImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    compareSmallImage.EndInit();
                    compareViewTemplatesData.Image = compareSmallImage;
                }
                panel.AddItem(compareViewTemplatesData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Error", $"Failed to initialize Miller Craft Tools: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}