using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Miller_Craft_Tools.Model
{
    /// <summary>
    /// Response from the initial handshake request
    /// </summary>
    public class SessionResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
        
        [JsonProperty("uploadUrl")]
        public string UploadUrl { get; set; }
        
        [JsonProperty("maxChunkSize")]
        public int MaxChunkSize { get; set; }
        
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
        
        [JsonProperty("expiresAt")]
        public DateTime ExpiresAt { get; set; }
        
        [JsonProperty("code")]
        public int Code { get; set; }
        
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// Response from the upload request
    /// </summary>
    public class UploadResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("nextStep")]
        public string NextStep { get; set; }
        
        [JsonProperty("code")]
        public int? Code { get; set; }
        
        [JsonProperty("details")]
        public object Details { get; set; }
    }

    /// <summary>
    /// Response from the processing request
    /// </summary>
    public class ProcessingResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("result")]
        public dynamic Result { get; set; }
        
        [JsonProperty("code")]
        public int? Code { get; set; }
        
        [JsonProperty("details")]
        public object Details { get; set; }
    }

    /// <summary>
    /// Class for tracking upload chunks for large parameter sets
    /// </summary>
    public class ChunkTracker
    {
        public string SessionId { get; set; }
        public string FilePath { get; set; }
        public List<int> UploadedChunks { get; set; } = new List<int>();
        public int TotalChunks { get; set; }
        public bool IsComplete => UploadedChunks.Count == TotalChunks;
    }
    
    /// <summary>
    /// Wrapper for error responses with standard error codes
    /// </summary>
    public class ErrorResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "error";
        
        [JsonProperty("code")]
        public int Code { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("details")]
        public object Details { get; set; }
    }
}
