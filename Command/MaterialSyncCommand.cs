using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Miller_Craft_Tools.Views;

namespace Miller_Craft_Tools.Command
{
    [Transaction(TransactionMode.Manual)]
    public class MaterialSyncCommand : IExternalCommand
    {
        // names of the four global parameters
        private readonly string[] _gpNames = {
            "Fenestration.Jamb",
            "Fenestration.Panel",
            "Fenestration.Glass",
            "Fenestration.Hardware"
        };

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // --- 1) Ensure GPs are enabled ---
            if (!GlobalParametersManager.AreGlobalParametersAllowed(doc))
            {
                TaskDialog.Show("MatSynch", "Global Parameters are not enabled in this document.");
                return Result.Failed;
            }

            // --- 2) Read each named global parameter into a material ElementId ---
            var gpValues = new Dictionary<string, ElementId>();
            foreach (var name in _gpNames)
            {
                ElementId gpId = GlobalParametersManager.FindByName(doc, name);
                if (gpId == ElementId.InvalidElementId)
                {
                    Console.WriteLine($"[MatSynch] WARNING: GP '{name}' not found.");
                    gpValues[name] = ElementId.InvalidElementId;
                    continue;
                }

                var gp = doc.GetElement(gpId) as GlobalParameter;
                if (gp == null)
                {
                    Console.WriteLine($"[MatSynch] WARNING: '{name}' is not a GlobalParameter.");
                    gpValues[name] = ElementId.InvalidElementId;
                    continue;
                }

                // extract its ElementId value
                var val = gp.GetValue() as ElementIdParameterValue;
                gpValues[name] = val?.Value ?? ElementId.InvalidElementId;
                Console.WriteLine($"[MatSynch] GP '{name}' → {gpValues[name].Value}");
            }

            // --- 3) Gather all window & door TYPES ---
            var windowTypes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Cast<FamilySymbol>();

            var doorTypes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Cast<FamilySymbol>();

            var allTypes = windowTypes.Concat(doorTypes).ToList();
            if (!allTypes.Any())
            {
                TaskDialog.Show("MatSynch", "No window or door types found in the model.");
                return Result.Succeeded;
            }

            // --- 4) Show progress dialog ---
            var progressWin = new MaterialSyncProgress(allTypes.Count);
            progressWin.Show();

            int updated = 0, skipped = 0, errors = 0;
            try
            {
                for (int i = 0; i < allTypes.Count; i++)
                {
                    if (progressWin.CancelRequested)
                    {
                        Console.WriteLine("[MatSynch] Operation canceled by user.");
                        break;
                    }

                    var sym = allTypes[i];
                    string status = $"{i + 1}/{allTypes.Count}: {sym.Name}";
                    progressWin.Report(i + 1, status);
                    Console.WriteLine($"[MatSynch] {status}");

                    using (Transaction tx = new Transaction(doc, "MatSynch: " + sym.Name))
                    {
                        // Correctly check TransactionStatus
                        TransactionStatus tStatus = tx.Start();
                        if (tStatus != TransactionStatus.Started)
                        {
                            Console.WriteLine($"[MatSynch] ERROR starting tx for {sym.Name}: {tStatus}");
                            errors++;
                            continue;
                        }

                        bool changed = false;

                        // map parameters:
                        // sp.Exterior, sp.Interior, sp.Jamb, sp.Rail  <- Fen.Jamb
                        // sp.Rail.Panel                         <- Fen.Glass
                        // sp.Hardware                           <- Fen.Hardware
                        TrySetMat(sym, "sp.Exterior", gpValues["Fenestration.Jamb"], ref changed);
                        TrySetMat(sym, "sp.Interior", gpValues["Fenestration.Jamb"], ref changed);
                        TrySetMat(sym, "sp.Jamb", gpValues["Fenestration.Jamb"], ref changed);
                        TrySetMat(sym, "sp.Rail", gpValues["Fenestration.Jamb"], ref changed);
                        TrySetMat(sym, "sp.Rail.Panel", gpValues["Fenestration.Glass"], ref changed);
                        TrySetMat(sym, "sp.Glass", gpValues["Fenestration.Glass"], ref changed);
                        TrySetMat(sym, "sp.Hardware", gpValues["Fenestration.Hardware"], ref changed);

                        if (changed)
                        {
                            tx.Commit();
                            updated++;
                        }
                        else
                        {
                            tx.RollBack();
                            skipped++;
                        }
                    }
                }
            }
            finally
            {
                progressWin.Close();
            }

            // --- 5) Final summary ---
            string summary =
                $"Material sync complete.\n\n" +
                $"Types updated: {updated}\n" +
                $"Types skipped: {skipped}\n" +
                $"Errors:        {errors}";
            TaskDialog.Show("MatSynch", summary);
            Console.WriteLine("[MatSynch] " + summary);

            return Result.Succeeded;
        }

        private void TrySetMat(
            FamilySymbol sym,
            string paramName,
            ElementId targetMat,
            ref bool changedFlag)
        {
            try
            {
                var p = sym.LookupParameter(paramName);
                if (p == null) return;

                if (p.AsElementId() != targetMat)
                {
                    p.Set(targetMat);
                    changedFlag = true;
                    Console.WriteLine($"  → {sym.Name}:{paramName} = {targetMat.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] {sym.Name}:{paramName} → {ex.Message}");
            }
        }
    }
}
