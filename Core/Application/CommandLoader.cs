using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Miller_Craft_Tools.Core.Application
{
    public static class CommandLoader
    {
        // Dictionary to store registered commands
        private static Dictionary<string, Type> _registeredCommands = new Dictionary<string, Type>();

        // Register commands from specified assemblies
        public static void RegisterCommands(UIControlledApplication application)
        {
            // Get all command assemblies
            var assemblies = new List<Assembly>
            {
                typeof(MillerCraftApp).Assembly, // Core assembly
                // Add other feature assemblies here as they're developed
            };

            foreach (var assembly in assemblies)
            {
                RegisterCommandsFromAssembly(application, assembly);
            }
        }

        // Register commands from a specific assembly
        private static void RegisterCommandsFromAssembly(UIControlledApplication application, Assembly assembly)
        {
            try
            {
                // Find all types that implement IExternalCommand
                var commandTypes = assembly.GetTypes()
                    .Where(t => t.GetInterfaces().Contains(typeof(IExternalCommand)) &&
                                !t.IsAbstract);

                foreach (var commandType in commandTypes)
                {
                    // Look for command attributes
                    var commandAttr = commandType.GetCustomAttribute<CommandAttributeBase>();
                    if (commandAttr != null)
                    {
                        // Register the command with Revit
                        var commandId = commandAttr.CommandId ?? commandType.Name;
                        _registeredCommands[commandId] = commandType;

                        // Create push button for the command
                        RegisterCommandButton(application, commandType, commandAttr);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                LogManager.LogError($"Error registering commands from {assembly.FullName}: {ex.Message}");
            }
        }

        // Register a command button on the ribbon
        private static void RegisterCommandButton(
            UIControlledApplication application,
            Type commandType,
            CommandAttributeBase commandAttr)
        {
            // Get or create the panel
            var panel = GetOrCreateRibbonPanel(application, commandAttr.PanelName);

            // Create the push button data
            var buttonData = new PushButtonData(
                commandAttr.CommandId ?? commandType.Name,
                commandAttr.ButtonText,
                commandType.Assembly.Location,
                commandType.FullName);

            // Set the tooltip and image
            buttonData.ToolTip = commandAttr.ToolTip;

            // Set the image if provided
            if (!string.IsNullOrEmpty(commandAttr.IconName))
            {
                buttonData.Image = GetCommandImage(commandAttr.IconName, CommandImageSize.Small);
                buttonData.LargeImage = GetCommandImage(commandAttr.IconName, CommandImageSize.Large);
            }

            // Add the button to the panel
            panel.AddItem(buttonData);
        }

        // Helper to get or create a ribbon panel
        private static RibbonPanel GetOrCreateRibbonPanel(
            UIControlledApplication application,
            string panelName)
        {
            // Default panel name
            if (string.IsNullOrEmpty(panelName))
                panelName = "Miller Craft";

            // Try to find an existing panel
            RibbonPanel panel = null;
            try
            {
                panel = application.GetRibbonPanels().FirstOrDefault(p => p.Name == panelName);
            }
            catch { }

            // Create the panel if it doesn't exist
            if (panel == null)
            {
                panel = application.CreateRibbonPanel(panelName);
            }

            return panel;
        }

        // Helper to get command images
        private static System.Drawing.Image GetCommandImage(string iconName, CommandImageSize size)
        {
            // Implementation to load image resources
            return null; // Placeholder
        }
    }

    // Command attribute for decorating command classes
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttributeBase : Attribute
    {
        public string CommandId { get; set; }
        public string ButtonText { get; set; }
        public string ToolTip { get; set; }
        public string PanelName { get; set; }
        public string IconName { get; set; }
    }

    // Image size enum
    public enum CommandImageSize
    {
        Small,
        Large
    }
}