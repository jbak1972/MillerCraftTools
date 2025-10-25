using System;

namespace Miller_Craft_Tools.UI.Styles
{
    /// <summary>
    /// Defines consistent terminology to match the web application
    /// </summary>
    public static class Terms
    {
        // Match these exactly with web app terms
        public static string SyncButtonText = "Sync with Miller Craft";
        public static string ProcessingText = "Processing...";
        public static string SuccessText = "Success";
        public static string ErrorText = "Error";
        public static string AssociateText = "Associate with Project";
        public static string ReviewChangesText = "Review Changes";
        
        // Authentication terms
        public static string LoginText = "Log In";
        public static string LogoutText = "Log Out";
        public static string UsernameText = "Username";
        public static string PasswordText = "Password";
        public static string AuthenticatedText = "Authenticated";
        public static string NotAuthenticatedText = "Not Authenticated";
        
        // Sync status terms
        public static string SyncStatusIdle = "Ready to Sync";
        public static string SyncStatusUploading = "Uploading";
        public static string SyncStatusPending = "Awaiting Admin Review";
        public static string SyncStatusProcessing = "Processing";
        public static string SyncStatusComplete = "Complete";
        public static string SyncStatusError = "Error";
        
        // Common messages
        public static string SyncCompleteMessage = "Synchronization complete";
        public static string SyncErrorMessage = "Unable to complete synchronization";
        public static string AuthRequiredMessage = "Please log in to sync your project";
        public static string NetworkErrorMessage = "Network error. Please check your connection";
        public static string TimeoutMessage = "The operation timed out";
        public static string ChangesAppliedMessage = "Changes have been applied successfully";
        public static string NoChangesMessage = "No changes to apply";
        
        // Dialog titles
        public static string SyncDialogTitle = "Miller Craft Assistant - Sync";
        public static string AuthDialogTitle = "Miller Craft Assistant - Authentication";
        public static string SettingsDialogTitle = "Miller Craft Assistant - Settings";
        public static string ReviewChangesDialogTitle = "Miller Craft Assistant - Review Changes";
        public static string ErrorDialogTitle = "Miller Craft Assistant - Error";
        
        // Button text
        public static string ApplyButtonText = "Apply Changes";
        public static string CancelButtonText = "Cancel";
        public static string CloseButtonText = "Close";
        public static string SaveButtonText = "Save";
        public static string RefreshButtonText = "Refresh";
        public static string CheckStatusButtonText = "Check Status";
    }
}
