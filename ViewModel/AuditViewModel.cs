using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Miller_Craft_Tools.ViewModel
{
    public class SchemaInfo
    {
        public string Name { get; set; }
        public string Size { get; set; }
    }

    public class AuditViewModel : ViewModelBase
    {
        public string FileSize { get; set; }
        public int ElementCount { get; set; }
        public int FamilyCount { get; set; }
        public int WarningCount { get; set; }
        public int DwgImportCount { get; set; }
        public List<SchemaInfo> Schemas { get; set; }
         public AuditViewModel(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            // Calculate file size
            string filePath = doc.PathName;
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                FileSize = $"{fileInfo.Length / (1024.0 * 1024.0):F2} MB";
            }
            else
            {
                FileSize = "Unknown";
            }

            // Count elements
            ElementCount = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElementIds()
                .Count;

            // Count families
            FamilyCount = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .ToElements()
                .Count;

            // Count warnings
            WarningCount = doc.GetWarnings().Count;

            // Count DWG imports
            DwgImportCount = new FilteredElementCollector(doc)
                .OfClass(typeof(ImportInstance))
                .Where(i => i.Category != null && i.Category.Name.Contains("DWG"))
                .AsEnumerable()
                .Count();
            /**
            // List schemas and their sizes
            IList<Schema> schemas = Schema.ListSchemas();

            foreach (Schema schema in schemas)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Schema Name: {schema.SchemaName}");
                sb.AppendLine($"GUID: {schema.GUID}");
                sb.AppendLine($"Documentation: {schema.Documentation}");
                sb.AppendLine($"Vendor ID: {schema.VendorId}");
                sb.AppendLine($"Read Access Level: {schema.ReadAccessLevel}");
                sb.AppendLine($"Write Access Level: {schema.WriteAccessLevel}");
                sb.AppendLine($"Field Count: {schema.ListFields().Count}");

                TaskDialog.Show("Schema Summary", sb.ToString());
            }
            **/
        }

        private int EstimateTypeSize(Type type)
        {
            if (type == typeof(int) || type == typeof(ElementId))
                return 4;
            if (type == typeof(double))
                return 8;
            if (type == typeof(string))
                return 50; // average string length guess
            if (type == typeof(bool))
                return 1;
            if (type == typeof(Guid))
                return 16;

            // Fallback for unknown types (e.g., custom structs, entities)
            return 32;
        }
    }
}