namespace Miller_Craft_Tools.Core.Application
{
    public interface IPluginModule
    {
        string ModuleName { get; }
        void Initialize();
        void Shutdown();
    }

    // Sample implementation for a module
    public class EfficiencyToolsModule : IPluginModule
    {
        public string ModuleName => "Efficiency Tools";

        public void Initialize()
        {
            // Register commands
            // Set up event handlers
            // Initialize module-specific resources
        }

        public void Shutdown()
        {
            // Clean up module-specific resources
        }
    }
}