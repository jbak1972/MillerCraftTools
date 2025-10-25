using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Miller_Craft_Tools.Model
{
    public class UserSettings
    {
        public bool Open3DViewsForRenumbering { get; set; } = true;
        
        /// <summary>
        /// Username or email for authentication
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// OAuth2 access token for API authentication
        /// </summary>
        public string ApiToken { get; set; }
        
        /// <summary>
        /// OAuth2 refresh token for renewing access
        /// </summary>
        public string RefreshToken { get; set; }
        
        /// <summary>
        /// ISO 8601 string representing token expiration time (UTC)
        /// </summary>
        public string TokenExpiration { get; set; }
        
        /// <summary>
        /// Legacy web session cookie
        /// </summary>
        public string WebSessionCookie { get; set; }
        
        /// <summary>
        /// History of sync operations (most recent first, max 10)
        /// </summary>
        public List<string> SyncHistory { get; set; } = new List<string>();

        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Miller Craft Assistant", "settings.json");

        public static UserSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<UserSettings>(json) ?? new UserSettings();
                }
            }
            catch { }
            return new UserSettings();
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
        
        /// <summary>
        /// Checks if the authentication token is valid and not expired
        /// </summary>
        /// <returns>True if token exists and is not expired</returns>
        public bool HasValidToken()
        {
            if (string.IsNullOrEmpty(ApiToken))
            {
                return false;
            }
            
            // Check if token is expired
            if (!string.IsNullOrEmpty(TokenExpiration))
            {
                if (DateTime.TryParse(TokenExpiration, out DateTime expirationTime))
                {
                    // Add buffer time (1 minute) to ensure we don't use a token that's about to expire
                    if (DateTime.UtcNow.AddMinutes(1) >= expirationTime)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Clears all authentication data (tokens and expiration)
        /// </summary>
        public void ClearAuthData()
        {
            ApiToken = null;
            RefreshToken = null;
            TokenExpiration = null;
            Save();
        }
        
        /// <summary>
        /// Adds a sync history entry (keeps max 10 most recent)
        /// </summary>
        /// <param name="entry">Sync history entry text</param>
        public void AddSyncHistoryEntry(string entry)
        {
            if (SyncHistory == null)
                SyncHistory = new List<string>();
                
            // Add to beginning
            SyncHistory.Insert(0, entry);
            
            // Keep only last 10
            if (SyncHistory.Count > 10)
            {
                SyncHistory = SyncHistory.GetRange(0, 10);
            }
            
            Save();
        }
    }
}
