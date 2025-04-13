using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;


namespace Miller_Craft_Tools.Controller
{
    internal class FamilyController
    {
        private static FamilyController _instance;
        private static readonly object _lock = new object();

        private FamilyController() { }

        public static FamilyController Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new FamilyController();
                }
            }
        }
        public void ExportFamilyParameters(UIDocument uiDoc)
{
    Document doc = uiDoc.Document;

    if (!doc.IsFamilyDocument)
    {
        MessageBox.Show("Error", "The active document is not a family document.", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    FamilyManager familyManager = doc.FamilyManager;
    IList<FamilyParameter> familyParameters = familyManager.Parameters.Cast<FamilyParameter>().ToList();
            /*
    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
    {
        saveFileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
        saveFileDialog.Title = "Save Family Parameters in JSON";

        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var parametersJson = new List<Dictionary<string, object>>();

                foreach (FamilyParameter param in familyParameters)
                { 
                    try
                    {
                        var paramData = new Dictionary<string, object>
                        {
                            ["Name"] = param.Definition.Name,
                            ["Type"] = param.Definition.GetDataType().TypeId,
                            ["Group"] = param.Definition.GetGroupTypeId().TypeId,
                            ["GUID"] = param.GUID.ToString(),
                            ["StorageType"] = param.StorageType.ToString(),
                            ["IsReadOnly"] = param.IsReadOnly,
                            ["IsShared"] = param.IsShared,
                            ["IsInstance"] = param.IsInstance,
                            ["UserModifiable"] = param.UserModifiable
                        };

                        // Check for formula
                        foreach (FamilyType type in familyManager.Types)
                        {
                            familyManager.CurrentType = type; // Set the current type
                            Autodesk.Revit.DB.    instanceParam = familyManager.get_Parameter(param.GUID);
                            if (instanceParam != null)
                            {
                                string formula = instanceParam.Formula;
                                if (!string.IsNullOrEmpty(formula))
                                {
                                    paramData["Formula"] = formula;
                                    break; // Found formula, no need to check other types
                                }
                            }
                        }
                        familyManager.CurrentType = null; // Reset type

                        // Add value for instance parameters
                        if (param.IsInstance)
                        {
                            foreach (FamilyType type in familyManager.Types)
                            {
                                if (type.HasValue(param))
                                {
                                    familyManager.CurrentType = type; // Set the current type

                                    Parameter instanceParam = familyManager.get_Parameter(param.Definition.GetDataType());
                                    if (instanceParam != null)
                                    {
                                        switch (param.StorageType)
                                        {
                                            case StorageType.Double:
                                                paramData["Value"] = instanceParam.AsDouble();
                                                break;
                                            case StorageType.Integer:
                                                paramData["Value"] = instanceParam.AsInteger();
                                                break;
                                            case StorageType.String:
                                                paramData["Value"] = instanceParam.AsString();
                                                break;
                                            case StorageType.ElementId:
                                                paramData["Value"] = instanceParam.AsElementId().IntegerValue.ToString();
                                                break;
                                            default:
                                                paramData["Value"] = "Unsupported Storage Type";
                                                break;
                                        }
                                    }

                                    familyManager.CurrentType = null; // Reset type
                                    break; // Only process one type for simplicity
                                }
                            }
                        }

                        parametersJson.Add(paramData); 
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"An error occurred processing parameter '{param.Definition.Name}': {ex.Message}";
                        MessageBox.Show(errorMsg, "Error Processing Parameter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                } 

                if (parametersJson.Count > 0)
                {
                    string jsonOutput = JsonConvert.SerializeObject(parametersJson, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(saveFileDialog.FileName, jsonOutput);
                    MessageBox.Show("Success", "Family parameters exported to JSON successfully.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Warning", "No parameters were exported. The file might be empty.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", $"An error occurred while exporting parameters to JSON: {ex.Message}", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    } */
}

        public void ImportFamilyParameters(UIDocument uiDoc)
        {
            Document doc = uiDoc.Document;

            if (!doc.IsFamilyDocument)
            {
                MessageBox.Show("The active document is not a family document.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                openFileDialog.Title = "Open Family Parameters";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var parametersToAdd = new List<FamilyParameterData>();
                        using (StreamReader reader = new StreamReader(openFileDialog.FileName))
                        {
                            FamilyParameterData paramData = null;
                            string line;

                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.StartsWith("Parameter Name: "))
                                {
                                    if (paramData != null) parametersToAdd.Add(paramData);
                                    paramData = new FamilyParameterData
                                    {
                                        Name = line.Substring("Parameter Name: ".Length).Trim()
                                    };
                                }
                                else if (line.StartsWith("Parameter Type: "))
                                {
                                    paramData.Type = line.Substring("Parameter Type: ".Length).Trim();
                                }
                                else if (line.StartsWith("Parameter Group: "))
                                {
                                    paramData.Group = line.Substring("Parameter Group: ".Length).Trim();
                                }
                                else if (line.StartsWith("Parameter GUID: "))
                                {
                                    paramData.GUID = line.Substring("Parameter GUID: ".Length).Trim();
                                }
                            }
                            if (paramData != null) parametersToAdd.Add(paramData);
                        }

                        FamilyManager familyManager = doc.FamilyManager;

                        using (Transaction trans = new Transaction(doc, "Import Family Parameters"))
                        {
                            trans.Start();

                            foreach (var paramData in parametersToAdd)
                            {
                                if (familyManager.Parameters.Cast<FamilyParameter>().All(p => p.Definition.Name != paramData.Name))
                                {
                                    try
                                    {
                                        // Convert string representation of group to ForgeTypeId
                                        using ForgeTypeId groupTypeId = new ForgeTypeId(paramData.Group);

                                        // Here we assume 'Text' as the default type; adjust as needed for other types
                                        familyManager.AddParameter(paramData.Name, groupTypeId, SpecTypeId.String.Text, false);
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Error adding parameter '{paramData.Name}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                            }

                            trans.Commit();
                        }
                        MessageBox.Show("Family parameters imported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while importing parameters: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        private ForgeTypeId ConvertStringToSpecTypeId(string typeString)
        {
            switch (typeString.ToLower())
            {
                case "length":
                    return SpecTypeId.Length;
                case "angle":
                    return SpecTypeId.Angle;
                case "text":
                    return SpecTypeId.String.Text;
                // Add more cases for other parameter types
                default:
                    return SpecTypeId.String.Text; // Default to text if not recognized
            }
        }

        private class FamilyParameterData
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Group { get; set; }
            public string GUID { get; set; }
        }
    }
}