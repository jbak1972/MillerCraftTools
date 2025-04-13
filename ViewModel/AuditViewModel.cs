using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Miller_Craft_Tools.ViewModel
{
    public class SchemaInfo
    {
        public string Name { get; set; }
        public string Size { get; set; } // Size in KB
    }

    public class AuditViewModel : INotifyPropertyChanged
    {
        private readonly Document _doc;
        private string _fileSize;
        private string _elementCount;
        private string _familyCount;
        private string _warningCount;
        private string _dwgImportCount;
        private List<SchemaInfo> _schemas; // New property for schemas

        public string FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        public string ElementCount
        {
            get => _elementCount;
            set { _elementCount = value; OnPropertyChanged(); }
        }

        public string FamilyCount
        {
            get => _familyCount;
            set { _familyCount = value; OnPropertyChanged(); }
        }

        public string WarningCount
        {
            get => _warningCount;
            set { _warningCount = value; OnPropertyChanged(); }
        }

        public string DwgImportCount
        {
            get => _dwgImportCount;
            set { _dwgImportCount = value; OnPropertyChanged(); }
        }

        public List<SchemaInfo> Schemas
        {
            get => _schemas;
            set { _schemas = value; OnPropertyChanged(); }
        }

        public AuditViewModel(Document doc)
        {
            _doc = doc;
            GatherStatistics();
        }

        private void GatherStatistics()
        {
            // File size
            string filePath = _doc.PathName;
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                FileSize = $"{fileInfo.Length / (1024.0 * 1024.0):F2} MB";
            }
            else
            {
                FileSize = "Not saved";
            }

            // Element count
            FilteredElementCollector collector = new FilteredElementCollector(_doc);
            ElementCount = collector.WhereElementIsNotElementType().GetElementCount().ToString();

            // Family count
            FilteredElementCollector familyCollector = new FilteredElementCollector(_doc)
                .OfClass(typeof(Family));
            FamilyCount = familyCollector.GetElementCount().ToString();

            // Warning count
            WarningCount = _doc.GetWarnings().Count.ToString();

            // DWG import count
            var dwgImports = new FilteredElementCollector(_doc)
                .OfClass(typeof(ImportInstance))
                .Where(e => e.Category != null && e.Category.Name.Contains("DWG"));
            DwgImportCount = dwgImports.Count().ToString();

            // Schemas and their sizes
            Schemas = new List<SchemaInfo>();
            var allSchemas = Schema.ListSchemas(); // Get all schemas in the document
            foreach (var schema in allSchemas)
            {
                double schemaSizeKB = 0;

                // Find all elements that have data for this schema
                var elementsWithSchema = new FilteredElementCollector(_doc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.GetEntity(schema) != null);

                foreach (Element element in elementsWithSchema)
                {
                    Entity entity = element.GetEntity(schema);
                    if (entity != null && entity.IsValid())
                    {
                        // Estimate size by serializing the entity's data
                        schemaSizeKB += EstimateEntitySize(entity);
                    }
                }

                Schemas.Add(new SchemaInfo
                {
                    Name = schema.SchemaName,
                    Size = $"{schemaSizeKB:F2} KB"
                });
            }

            // Sort schemas by size (descending)
            Schemas = Schemas.OrderByDescending(s => double.Parse(s.Size.Split(' ')[0])).ToList();
        }

        // Helper method to estimate the size of an Entity (extensible storage data)
        private double EstimateEntitySize(Entity entity)
        {
            double sizeInBytes = 0;

            // Iterate through all fields in the entity
            foreach (var field in entity.Schema.ListFields())
            {
                if (!entity.IsValid()) continue;

                // Get the value of the field
                object value = entity.Get<object>(field);
                if (value == null) continue;

                // Estimate size based on the type of data
                if (value is string str)
                {
                    sizeInBytes += Encoding.UTF8.GetByteCount(str);
                }
                else if (value is int || value is float)
                {
                    sizeInBytes += 4; // 4 bytes for int/float
                }
                else if (value is double)
                {
                    sizeInBytes += 8; // 8 bytes for double
                }
                else if (value is IList<int> intList)
                {
                    sizeInBytes += intList.Count * 4;
                }
                else if (value is IList<double> doubleList)
                {
                    sizeInBytes += doubleList.Count * 8;
                }
                else if (value is IList<string> stringList)
                {
                    sizeInBytes += stringList.Sum(s => Encoding.UTF8.GetByteCount(s));
                }
                // Add more types as needed (e.g., XYZ, UV, etc.)
            }

            return sizeInBytes / 1024.0; // Convert to KB
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}