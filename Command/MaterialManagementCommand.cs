using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Views;

namespace Miller_Craft_Tools.Command
{
    [Transaction(TransactionMode.Manual)]
    public class MaterialManagementCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Get all materials in the project
            var materialCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .ToList();

            if (!materialCollector.Any())
            {
                Autodesk.Revit.UI.TaskDialog.Show("Material Management", "No materials found in the model.");
                return Result.Succeeded;
            }

            // Show progress dialog
            var progressWin = new MaterialSyncProgress(materialCollector.Count);
            progressWin.Show();

            int purged = 0, renamed = 0, errors = 0;
            List<string> purgedMaterials = new List<string>();
            List<string> renamedMaterials = new List<string>();

            try
            {
                using (Transaction tx = new Transaction(doc, "Material Management"))
                {
                    // Correctly check TransactionStatus
                    TransactionStatus tStatus = tx.Start();
                    if (tStatus != TransactionStatus.Started)
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Material Management", 
                            $"ERROR starting transaction: {tStatus}");
                        return Result.Failed;
                    }

                    for (int i = 0; i < materialCollector.Count; i++)
                    {
                        if (progressWin.CancelRequested)
                        {
                            Console.WriteLine("[MatManage] Operation canceled by user.");
                            break;
                        }

                        var material = materialCollector[i];
                        string materialName = material.Name;
                        string status = $"{i + 1}/{materialCollector.Count}: {materialName}";
                        progressWin.Report(i + 1, status);

                        try
                        {
                            // Check if material has non-English characters
                            if (ContainsNonEnglishCharacters(materialName))
                            {
                                // Delete material with non-English characters
                                doc.Delete(material.Id);
                                purged++;
                                purgedMaterials.Add(materialName);
                                Console.WriteLine($"[MatManage] Purged material: {materialName}");
                            }
                            // Check if material contains specific patterns to purge
                            else if (materialName.Contains("- AR -") || 
                                    materialName.Contains("- CD -") || 
                                    materialName.Contains("- ST -"))
                            {
                                doc.Delete(material.Id);
                                purged++;
                                purgedMaterials.Add(materialName);
                                Console.WriteLine($"[MatManage] Purged material with pattern: {materialName}");
                            }
                            // Check if material name starts with "ZOOT-"
                            else if (materialName.StartsWith("ZOOT-"))
                            {
                                // Rename material to have spaces before and after the hyphen
                                string newName = materialName.Replace("ZOOT-", "ZOOT - ");
                                material.Name = newName;
                                renamed++;
                                renamedMaterials.Add($"{materialName} → {newName}");
                                Console.WriteLine($"[MatManage] Renamed material: {materialName} → {newName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[MatManage] ERROR with {materialName}: {ex.Message}");
                            errors++;
                        }
                    }

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Material Management Error", ex.Message);
                return Result.Failed;
            }
            finally
            {
                progressWin.Close();
            }

            // Final summary
            string summary = 
                $"Material management complete.\n\n" +
                $"Materials purged: {purged}\n" +
                $"Materials renamed: {renamed}\n" +
                $"Errors: {errors}\n\n";
            
            if (purgedMaterials.Count > 0)
            {
                summary += "Purged materials:\n" + string.Join("\n", purgedMaterials.Take(10));
                if (purgedMaterials.Count > 10)
                    summary += $"\n... and {purgedMaterials.Count - 10} more";
                summary += "\n\n";
            }
            
            if (renamedMaterials.Count > 0)
            {
                summary += "Renamed materials:\n" + string.Join("\n", renamedMaterials.Take(10));
                if (renamedMaterials.Count > 10)
                    summary += $"\n... and {renamedMaterials.Count - 10} more";
            }
            
            Autodesk.Revit.UI.TaskDialog.Show("Material Management", summary);

            return Result.Succeeded;
        }

        /// <summary>
        /// Check if a string contains non-English characters
        /// </summary>
        private bool ContainsNonEnglishCharacters(string text)
        {
            // Regex pattern to match any character outside basic Latin alphabet, numbers and common symbols
            // This includes accented characters, non-Latin scripts, etc.
            string pattern = @"[^\x00-\x7F]";
            return Regex.IsMatch(text, pattern);
        }
    }
}
