using System.Collections.Generic;

namespace Miller_Craft_Tools.Model
{
    public class ProjectInfoExportModel
    {
        public string ProjectId { get; set; }
        public string FileName { get; set; }
        public List<ProjectParameterExport> Parameters { get; set; } = new List<ProjectParameterExport>();
    }

    public class ProjectParameterExport
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public bool Update { get; set; } = false;
    }
}
