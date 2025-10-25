using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Miller_Craft_Tools.Model;

namespace Miller_Craft_Tools.Utils
{
    /// <summary>
    /// Helper class for HTTP communication with the Miller Craft Assistant server
    /// </summary>
    public static class HttpClientHelper
    {
        private static readonly HttpClient _httpClient;
        
        // Default timeout for HTTP requests
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
        
        // Initialize HttpClient once for better performance
        static HttpClientHelper()
        {
            try
            {
                // Create handler with proxy support
                var handler = ProxyHelper.CreateProxyEnabledHandler();
                
                // Create client with handler
                _httpClient = new HttpClient(handler);
                _httpClient.Timeout = DefaultTimeout;
                
                Logger.LogInfo("HttpClient initialized with proxy configuration: " + 
                               ProxyHelper.GetProxyAddressForDiagnostics());
            }
            catch (Exception ex)
            {
                // Fallback to default HttpClient if proxy configuration fails
                _httpClient = new HttpClient();
                _httpClient.Timeout = DefaultTimeout;
                
                Logger.LogError($"Error configuring HttpClient with proxy: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send a JSON request to the specified endpoint
        /// </summary>
        public static async Task<string> SendJsonRequestAsync(string url, string jsonContent, string authToken, CancellationToken cancellationToken = default)
        {
            // Log the request (non-blocking)
            Logger.LogJson(new { 
                Url = url, 
                Method = "POST", 
                ContentType = "application/json", 
                RequestTime = DateTime.Now 
            }, "http_request_json");
            
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                // Add authorization header
                request.Headers.Add("Authorization", $"Bearer {authToken}");
                
                // Add JSON content
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                try
                {
                    // Non-blocking logging of the full request
                    Logger.LogHttpRequestNonBlocking(request);
                    
                    // Send the request
                    var response = await _httpClient.SendAsync(request, cancellationToken);
                    
                    // Non-blocking logging of the response
                    Logger.LogHttpResponseNonBlocking(response);
                    
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Check for error response
                    if (!response.IsSuccessStatusCode)
                    {
                        try
                        {
                            // Try to parse as error response
                            var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                            if (errorResponse != null && errorResponse.Code > 0)
                            {
                                throw new HttpRequestException($"Request failed with error code {errorResponse.Code}: {errorResponse.Message}");
                            }
                        }
                        catch (JsonException)
                        {
                            // If it's not a valid JSON error response, throw generic exception
                        }
                        
                        throw new HttpRequestException($"Request failed with status {response.StatusCode}: {responseContent}");
                    }
                    
                    return responseContent;
                }
                catch (TaskCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Logger.LogError("Request was canceled by user");
                        throw new OperationCanceledException("The request was canceled by the user.", cancellationToken);
                    }
                    else
                    {
                        Logger.LogError("Request timed out");
                        throw new TimeoutException("The request timed out. Please check your internet connection and try again.");
                    }
                }
                catch (Exception ex)
                {
                    // Use enhanced network error logging
                    NetworkErrorLogger.LogNetworkException(ex, url, "SendJsonRequestAsync");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Send a multipart request with a JSON file
        /// </summary>
        public static async Task<string> SendMultipartRequestAsync(string url, string jsonContent, string filename, string authToken, CancellationToken cancellationToken = default)
        {
            // Log the request (non-blocking)
            Logger.LogJson(new { 
                Url = url, 
                Method = "POST", 
                ContentType = "multipart/form-data", 
                Filename = filename,
                RequestTime = DateTime.Now 
            }, "http_request_multipart");
            
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                // Add authorization header
                request.Headers.Add("Authorization", $"Bearer {authToken}");
                
                // Create multipart content
                using (var formData = new MultipartFormDataContent())
                {
                    // Create file content from the JSON string
                    var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(jsonContent));
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    
                    // Add the file to the form data
                    formData.Add(fileContent, "file", filename);
                    
                    // Set the request content
                    request.Content = formData;
                    
                    try
                    {
                        // Non-blocking logging of the full request
                        Logger.LogHttpRequestNonBlocking(request);
                        
                        // Send the request
                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        
                        // Non-blocking logging of the response
                        Logger.LogHttpResponseNonBlocking(response);
                        
                        // Read the response content
                        string responseContent = await response.Content.ReadAsStringAsync();
                        
                        // Check for error response
                        if (!response.IsSuccessStatusCode)
                        {
                            try
                            {
                                // Try to parse as error response
                                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                                if (errorResponse != null && errorResponse.Code > 0)
                                {
                                    throw new HttpRequestException($"Upload failed with error code {errorResponse.Code}: {errorResponse.Message}");
                                }
                            }
                            catch (JsonException)
                            {
                                // If it's not a valid JSON error response, throw generic exception
                            }
                            
                            throw new HttpRequestException($"Upload failed with status {response.StatusCode}: {responseContent}");
                        }
                        
                        return responseContent;
                    }
                    catch (TaskCanceledException)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Logger.LogError("Upload was canceled by user");
                            throw new OperationCanceledException("The upload was canceled by the user.", cancellationToken);
                        }
                        else
                        {
                            Logger.LogError("Upload timed out");
                            throw new TimeoutException("The upload timed out. Please check your internet connection and try again.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Use enhanced network error logging
                        NetworkErrorLogger.LogNetworkException(ex, url, "SendMultipartRequestAsync");
                        throw;
                    }
                }
            }
        }
        
        /// <summary>
        /// Send a chunked multipart request for large files
        /// </summary>
        public static async Task<ChunkTracker> SendChunkedMultipartRequestAsync(
            string url, 
            string jsonContent, 
            string filename, 
            string sessionId, 
            string authToken, 
            int maxChunkSize = 5 * 1024 * 1024, // Default to 5MB chunks
            CancellationToken cancellationToken = default)
        {
            // Create a temporary file with the JSON content
            string tempFilePath = Path.Combine(Path.GetTempPath(), filename);
            File.WriteAllText(tempFilePath, jsonContent);
            
            // Get the file size
            long fileSize = new FileInfo(tempFilePath).Length;
            
            // Calculate the number of chunks
            int totalChunks = (int)Math.Ceiling((double)fileSize / maxChunkSize);
            
            // Create a chunk tracker
            var chunkTracker = new ChunkTracker
            {
                SessionId = sessionId,
                FilePath = tempFilePath,
                TotalChunks = totalChunks
            };
            
            Logger.LogJson(new { 
                Action = "Starting chunked upload",
                Filename = filename,
                TotalChunks = totalChunks,
                FileSize = fileSize,
                MaxChunkSize = maxChunkSize
            }, "chunked_upload_start");
            
            // Upload the first chunk to get started
            if (totalChunks > 0)
            {
                await UploadChunkAsync(url, tempFilePath, 0, totalChunks, sessionId, authToken, chunkTracker, cancellationToken);
            }
            
            return chunkTracker;
        }
        
        /// <summary>
        /// Upload a specific chunk of a file
        /// </summary>
        private static async Task UploadChunkAsync(
            string url, 
            string filePath, 
            int chunkIndex, 
            int totalChunks, 
            string sessionId, 
            string authToken, 
            ChunkTracker chunkTracker,
            CancellationToken cancellationToken)
        {
            // Log the chunk upload (non-blocking)
            Logger.LogJson(new { 
                Action = "Uploading chunk",
                ChunkIndex = chunkIndex,
                TotalChunks = totalChunks,
                SessionId = sessionId,
                Filename = Path.GetFileName(filePath)
            }, "chunk_upload");
            
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                // Add headers
                request.Headers.Add("Authorization", $"Bearer {authToken}");
                request.Headers.Add("X-Session-Id", sessionId);
                request.Headers.Add("X-Chunk-Index", chunkIndex.ToString());
                request.Headers.Add("X-Total-Chunks", totalChunks.ToString());
                
                // Calculate chunk boundaries
                long fileSize = new FileInfo(filePath).Length;
                int chunkSize = 5 * 1024 * 1024; // 5MB chunks
                long startPosition = chunkIndex * chunkSize;
                long endPosition = Math.Min(startPosition + chunkSize, fileSize);
                int currentChunkSize = (int)(endPosition - startPosition);
                
                // Create multipart content
                using (var formData = new MultipartFormDataContent())
                {
                    // Read the chunk from the file
                    byte[] chunkData = new byte[currentChunkSize];
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.Seek(startPosition, SeekOrigin.Begin);
                        fileStream.Read(chunkData, 0, currentChunkSize);
                    }
                    
                    // Create content for the chunk
                    var chunkContent = new ByteArrayContent(chunkData);
                    chunkContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    
                    // Add the chunk to the form data
                    formData.Add(chunkContent, "chunk", Path.GetFileName(filePath));
                    
                    // Set the request content
                    request.Content = formData;
                    
                    try
                    {
                        // Send the request
                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        
                        // Non-blocking logging of the response
                        Logger.LogHttpResponseNonBlocking(response);
                        
                        // Read the response content
                        string responseContent = await response.Content.ReadAsStringAsync();
                        
                        // Check for error response
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException($"Chunk upload failed with status {response.StatusCode}: {responseContent}");
                        }
                        
                        // Mark the chunk as uploaded
                        chunkTracker.UploadedChunks.Add(chunkIndex);
                        
                        Logger.LogJson(new { 
                            Action = "Chunk upload complete",
                            ChunkIndex = chunkIndex,
                            TotalChunks = totalChunks,
                            Progress = $"{chunkTracker.UploadedChunks.Count}/{totalChunks}",
                            IsComplete = chunkTracker.IsComplete
                        }, "chunk_upload_complete");
                    }
                    catch (Exception ex)
                    {
                        // Log the exception
                        Logger.LogError($"Error uploading chunk {chunkIndex}/{totalChunks}: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        
        /// <summary>
        /// Continue uploading chunks until complete
        /// </summary>
        public static async Task<string> ContinueChunkedUploadAsync(
            string url, 
            ChunkTracker chunkTracker, 
            string authToken, 
            IProgress<(int current, int total)> progress = null, 
            CancellationToken cancellationToken = default)
        {
            // Upload all remaining chunks
            for (int i = 0; i < chunkTracker.TotalChunks; i++)
            {
                if (!chunkTracker.UploadedChunks.Contains(i))
                {
                    await UploadChunkAsync(
                        url, 
                        chunkTracker.FilePath, 
                        i, 
                        chunkTracker.TotalChunks, 
                        chunkTracker.SessionId, 
                        authToken, 
                        chunkTracker,
                        cancellationToken);
                    
                    // Report progress
                    progress?.Report((chunkTracker.UploadedChunks.Count, chunkTracker.TotalChunks));
                    
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            
            // Finalize the upload
            using (var request = new HttpRequestMessage(HttpMethod.Post, $"{url}/finalize"))
            {
                // Add headers
                request.Headers.Add("Authorization", $"Bearer {authToken}");
                request.Headers.Add("X-Session-Id", chunkTracker.SessionId);
                
                try
                {
                    // Send the request
                    var response = await _httpClient.SendAsync(request, cancellationToken);
                    
                    // Non-blocking logging of the response
                    Logger.LogHttpResponseNonBlocking(response);
                    
                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Check for error response
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Finalize upload failed with status {response.StatusCode}: {responseContent}");
                    }
                    
                    Logger.LogJson(new { 
                        Action = "Chunked upload finalized",
                        SessionId = chunkTracker.SessionId,
                        Filename = Path.GetFileName(chunkTracker.FilePath),
                        TotalChunks = chunkTracker.TotalChunks
                    }, "chunked_upload_finalized");
                    
                    // Clean up the temporary file
                    try
                    {
                        File.Delete(chunkTracker.FilePath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to delete temporary file: {ex.Message}");
                    }
                    
                    return responseContent;
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Logger.LogError($"Error finalizing chunked upload: {ex.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Check if a session is still valid
        /// </summary>
        public static bool IsSessionValid(DateTime expiresAt)
        {
            // Add a 5-minute buffer to avoid edge cases
            return DateTime.UtcNow.AddMinutes(5) < expiresAt;
        }
        
        /// <summary>
        /// Get a user-friendly error message from an error code
        /// </summary>
        public static string GetUserFriendlyErrorMessage(int errorCode, string serverMessage)
        {
            switch (errorCode)
            {
                case int code when code >= 1000 && code < 2000:
                    return $"Authentication error: {serverMessage}";
                case int code when code >= 2000 && code < 3000:
                    return $"Validation error: {serverMessage}";
                case int code when code >= 3000 && code < 4000:
                    return $"Processing error: {serverMessage}";
                case int code when code >= 4000 && code < 5000:
                    return $"Server error: {serverMessage}";
                default:
                    return $"Error: {serverMessage}";
            }
        }
    }
}
