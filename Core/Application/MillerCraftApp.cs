using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Windows;

namespace Miller_Craft_Tools.Core.Application
{
    public class MillerCraftApp : IExternalApplication
    {
        // Singleton instance
        public static MillerCraftApp Instance { get; private set; }

        // Application-level properties
        public UIControlledApplication RevitApplication { get; private set; }
        public string PluginPath { get; private set; }

        // Result for Revit's startup
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                Instance = this;
                RevitApplication = application;
                PluginPath = Assembly.GetExecutingAssembly().Location;

                // Initialize core services
                InitializeServices();

                // Register commands
                CommandLoader.RegisterCommands(application);

                // Create UI
                CreateUserInterface(application);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Log the exception
                TaskDialog.Show("Miller Craft Tools Error",
                    $"Error initializing plugin: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Clean up resources
            return Result.Succeeded;
        }

        private void InitializeServices()
        {
            // Initialize logging
            LogManager.Initialize(PluginPath);

            // Initialize configuration
            ConfigManager.Initialize(PluginPath);

            // Initialize event system
            EventManager.Initialize();
        }

        private void CreateUserInterface(UIControlledApplication application)
        {
            // Create ribbon panel
            RibbonPanel panel = application.CreateRibbonPanel("Miller Craft");

            // Add buttons and controls...
        }
    }
}