using System;
using System.Collections.Generic;

namespace Miller_Craft_Tools.Core.Infrastructure.Events
{
    public static class EventManager
    {
        // Dictionary to store event handlers
        private static Dictionary<string, List<Delegate>> _eventHandlers =
            new Dictionary<string, List<Delegate>>();

        // Initialize the event manager
        public static void Initialize()
        {
            _eventHandlers.Clear();
        }

        // Subscribe to an event
        public static void Subscribe<T>(string eventName, Action<T> handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }

            _eventHandlers[eventName].Add(handler);
        }

        // Unsubscribe from an event
        public static void Unsubscribe<T>(string eventName, Action<T> handler)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName].Remove(handler);
            }
        }

        // Publish an event
        public static void Publish<T>(string eventName, T data)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                foreach (var handler in _eventHandlers[eventName])
                {
                    if (handler is Action<T> typedHandler)
                    {
                        try
                        {
                            typedHandler(data);
                        }
                        catch (Exception ex)
                        {
                            // Log the exception
                            LogManager.LogError($"Error in event handler for {eventName}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    // Predefined event names
    public static class EventNames
    {
        // Application events
        public const string ApplicationStartup = "ApplicationStartup";
        public const string ApplicationShutdown = "ApplicationShutdown";

        // Command events
        public const string CommandExecuted = "CommandExecuted";

        // Document events
        public const string DocumentOpened = "DocumentOpened";
        public const string DocumentSaved = "DocumentSaved";
        public const string DocumentClosed = "DocumentClosed";

        // Feature-specific events
        public const string TemplateUpdated = "TemplateUpdated";
        public const string StandardsChecked = "StandardsChecked";
    }
}