using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Utils;
using Miller_Craft_Tools.UI.Dialogs;

namespace Miller_Craft_Tools.Command
{
    /// <summary>
    /// Command to test the API token functionality against various endpoints
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class TestApiTokenCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Create and show progress dialog
                var progressDialog = new ApiTestProgressDialog("API Token Test");
                
                // Run token test with progress updates
                var result = progressDialog.RunTaskWithProgress<TokenTestResult>(
                    async (progress, cancellationToken) => 
                    {
                        // Create result object
                        var result = new TokenTestResult();
                        
                        try
                        {
                            progress?.Report("Checking for API token...");
                             
                            // Get the stored token
                            var apiTokenService = new Services.ApiTokenService();
                            string token = apiTokenService.GetToken();
                             
                            if (string.IsNullOrEmpty(token))
                            {
                                result.Success = false;
                                result.Message = "No API token found. Please add a token using the API Token Management dialog.";
                                return result;
                            }
                             
                            cancellationToken.ThrowIfCancellationRequested();
                             
                            // Test token validation endpoint
                            progress?.Report("Testing token validation endpoint...");
                            var validationResult = await SimpleApiTester.TestEndpointGetAsync("/api/tokens/validate", token);
                             
                            result.TokenValid = validationResult.IsSuccessful;
                            result.ValidationMessage = validationResult.IsSuccessful 
                                ? "Token validation successful" 
                                : $"Token validation failed with status code: {validationResult.StatusCode}";
                             
                            if (!validationResult.IsSuccessful)
                            {
                                result.Success = false;
                                result.Message = "API token is not valid or has expired.";
                                return result;
                            }
                             
                            cancellationToken.ThrowIfCancellationRequested();
                             
                            // Test parameter mappings endpoint
                            progress?.Report("Testing parameter mappings endpoint...");
                            var mappingsResult = await SimpleApiTester.TestEndpointGetAsync("/api/parameter-mappings", token);
                             
                            if (mappingsResult.IsSuccessful)
                            {
                                result.ParameterMappingsEndpointMessage = "Parameter mappings endpoint accessible";
                                result.ParameterMappingsResponseSample = TruncateResponse(mappingsResult.ResponseContent);
                            }
                            else
                            {
                                result.ParameterMappingsEndpointMessage = $"Parameter mappings endpoint returned status code: {mappingsResult.StatusCode}";
                            }
                             
                            cancellationToken.ThrowIfCancellationRequested();
                             
                            // Test project-specific endpoint with a test GUID
                            progress?.Report("Testing project-specific endpoint...");
                            var projectGuid = "00000000-0000-0000-0000-000000000000"; // Test GUID
                            var projectResult = await SimpleApiTester.TestEndpointGetAsync($"/api/projects/{projectGuid}/parameter-mappings", token);
                             
                            if (projectResult.IsSuccessful)
                            {
                                result.ProjectEndpointMessage = "Project-specific endpoint accessible";
                                result.ProjectEndpointResponseSample = TruncateResponse(projectResult.ResponseContent);
                            }
                            else
                            {
                                // 404 is expected for a test GUID, but other errors indicate problems
                                if (projectResult.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    result.ProjectEndpointMessage = "Project endpoint returned 404 Not Found (expected for test GUID)";
                                }
                                else
                                {
                                    result.ProjectEndpointMessage = $"Project endpoint returned status code: {projectResult.StatusCode}";
                                }
                            }
                             
                            progress?.Report("Finalizing test results...");
                            result.Success = true;
                            result.Message = "API token testing completed successfully.";
                        }
                        catch (OperationCanceledException)
                        {
                            result.Success = false;
                            result.Message = "API token test was canceled by user.";
                            throw; // Rethrow to signal cancellation to caller
                        }
                        catch (Exception ex)
                        {
                            result.Success = false;
                            result.Message = $"Error testing API token: {ex.Message}";
                            Logger.LogError(result.Message, LogSeverity.Error);
                        }
                        
                        return result;
                    }, 
                    testResult => 
                    {
                        // This runs after the test completes and dialog closes
                        // Display the results in a new dialog
                        ShowTestResults(testResult);
                    }
                );
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Log the error
                Utils.Logger.LogError($"Error testing API token: {ex.Message}", Utils.LogSeverity.Error);
                
                // Show error message to user
                Autodesk.Revit.UI.TaskDialog.Show(
                    "API Token Test Error",
                    $"An error occurred while testing the API token: {ex.Message}");
                
                return Result.Failed;
            }
        }
        
        /// <summary>
        /// Shows test results in a TaskDialog
        /// </summary>
        private void ShowTestResults(TokenTestResult result)
        {
            if (result == null) return;
            
            // Build the result message
            var sb = new StringBuilder();
            sb.AppendLine("API Token Test Results:");
            sb.AppendLine();
            
            if (result.TokenValid)
            {
                sb.AppendLine("✅ Token Validation: PASSED");
                sb.AppendLine($"    {result.ValidationMessage}");
            }
            else
            {
                sb.AppendLine("❌ Token Validation: FAILED");
                sb.AppendLine($"    {result.ValidationMessage}");
            }
            
            sb.AppendLine();
            sb.AppendLine("Endpoint Tests:");
            sb.AppendLine();
            
            // Parameter mappings endpoint
            if (!string.IsNullOrEmpty(result.ParameterMappingsResponseSample))
            {
                sb.AppendLine("✅ Parameter Mappings Endpoint: ACCESSIBLE");
                sb.AppendLine($"    {result.ParameterMappingsEndpointMessage}");
                sb.AppendLine("    Response Sample:");
                sb.AppendLine($"    {result.ParameterMappingsResponseSample}");
            }
            else
            {
                sb.AppendLine("ℹ️ Parameter Mappings Endpoint:");
                sb.AppendLine($"    {result.ParameterMappingsEndpointMessage}");
            }
            
            sb.AppendLine();
            
            // Project-specific endpoint
            if (!string.IsNullOrEmpty(result.ProjectEndpointResponseSample))
            {
                sb.AppendLine("✅ Project Endpoint: ACCESSIBLE");
                sb.AppendLine($"    {result.ProjectEndpointMessage}");
                sb.AppendLine("    Response Sample:");
                sb.AppendLine($"    {result.ProjectEndpointResponseSample}");
            }
            else
            {
                sb.AppendLine("ℹ️ Project Endpoint:");
                sb.AppendLine($"    {result.ProjectEndpointMessage}");
                // Note: 404 is expected for the test GUID
                if (result.ProjectEndpointMessage?.Contains("404") == true)
                {
                    sb.AppendLine("    (This is normal with a test GUID; it confirms the endpoint exists but no project was found)");
                }
            }
            
            // Show the results in a task dialog
            Autodesk.Revit.UI.TaskDialog resultDialog = new Autodesk.Revit.UI.TaskDialog("API Token Test Results");
            resultDialog.MainInstruction = result.Success ? "Token Test Completed Successfully" : "Token Test Issues Detected";
            resultDialog.MainContent = sb.ToString();
            resultDialog.CommonButtons = TaskDialogCommonButtons.Ok;
            resultDialog.DefaultButton = TaskDialogResult.Ok;
            
            resultDialog.Show();
        }
        
        /// <summary>
        /// Truncates a JSON response to a reasonable length for display
        /// </summary>
        private string TruncateResponse(string response, int maxLength = 200)
        {
            try
            {
                // Try to parse and format JSON for better display
                var parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                var formatted = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                
                if (formatted.Length <= maxLength)
                {
                    return formatted;
                }
                
                return formatted.Substring(0, maxLength) + "...";
            }
            catch
            {
                // If JSON parsing fails, just truncate the string
                if (response.Length <= maxLength)
                {
                    return response;
                }
                
                return response.Substring(0, maxLength) + "...";
            }
        }
    }
}