using System.Collections.Generic;

namespace Miller_Craft_Tools.Model
{
    public class ProjectStandards
    {
        public IdentityInformation IdentityInformation { get; set; }
        public Dictionary<string, List<FamilyStandard>> Families { get; set; } = new Dictionary<string, List<FamilyStandard>>();
        public List<ObjectStyle> ModelObjectStyles { get; set; } = new List<ObjectStyle>();
        public List<ObjectStyle> AnnotationObjectStyles { get; set; } = new List<ObjectStyle>();
        public List<FillStyleStandard> FillStyles { get; set; } = new List<FillStyleStandard>();
        public List<ProjectParameterStandard> SharedProjectParameters { get; set; } = new List<ProjectParameterStandard>();
        public List<ProjectParameterStandard> NonSharedProjectParameters { get; set; } = new List<ProjectParameterStandard>();
        public List<LineStyleStandard> LineStyles { get; set; } = new List<LineStyleStandard>(); // New
        public List<LinePatternStandard> LinePatterns { get; set; } = new List<LinePatternStandard>(); // New
    }

    public class LineStyleStandard
    {
        public string Name { get; set; }
        public int? LineWeight { get; set; }
        public string LineColor { get; set; }
        public string LinePattern { get; set; }
    }

    public class LinePatternStandard
    {
        public string Name { get; set; }
    }

    public class IdentityInformation
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string ExportDate { get; set; }
        public string ExportTime { get; set; }
    }

    public class FamilyStandard
    {
        public string Name { get; set; }
        public List<FamilyTypeStandard> Types { get; set; } = new List<FamilyTypeStandard>();
    }

    public class FamilyTypeStandard
    {
        public string Name { get; set; }
        public List<ParameterStandard> Parameters { get; set; } = new List<ParameterStandard>();
    }

    public class ParameterStandard
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ObjectStyle
    {
        public string Category { get; set; }
        public int? ProjectionLineWeight { get; set; }
        public int? CutLineWeight { get; set; }
        public string LineColor { get; set; }
        public string LinePattern { get; set; }
        public string Material { get; set; }
        public List<ObjectStyle> SubCategories { get; set; } = new List<ObjectStyle>();
    }

    public class FillStyleStandard
    {
        public string Name { get; set; }
        public string ForegroundPattern { get; set; }
        public string BackgroundPattern { get; set; }
        public string Color { get; set; }
    }

    public class ProjectParameterStandard
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Group { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        public bool IsInstance { get; set; }
        public bool IsShared { get; set; } // New
    }
}