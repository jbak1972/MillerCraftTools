using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miller_Craft_Tools.Model
{
    public class ProjectData
    {
        private string? projectId;

        public string? ProjectName { get; set; }
        public string? ProjectNumber { get; set; }
        public string? ClientName { get; set; }
        public string? ClientAddress { get; set; }
        public string? ProjectManager { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Budget { get; set; }
        public decimal Cost { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public int NumberOfFloors { get; set; }
        public int NumberOfUnits { get; set; }
        public string? Architect { get; set; }
        public string? Contractor { get; set; }
        public string? StructuralEngineer { get; set; }
        public string? MEPEngineer { get; set; }
        public string? CivilEngineer { get; set; }
        public string? LandscapeArchitect { get; set; }
        public string? Owner { get; set; }
        public string? Developer { get; set; }
        public string? ProjectType { get; set; }
        public string? ProjectCategory { get; set; }
        public string? ProjectDescription { get; set; }
        public string? ParcelNumber { get; set; }
        public string? SiteAddress { get; set; }
        public string? LegalDescription { get; set; }
        public string? Zoning { get; set; }
        public string? LandUse { get; set; }
        public string? SiteArea { get; set; }
        public string? Jurisdiction { get; set; }
        public string? WaterServiceProvider { get; set; }
        public string? SewerServiceProvider { get; set; }
        public string? StormwaterServiceProvider { get; set; }
        public string? Setbacks { get; set; }
        public string? CodeRequirements { get; set; }
        public DateTime CreatedTimeStamp { get; set; }
        public DateTime UpdatedTimeStamp { get; set; }
        public string? UpdatedUserId { get; set; }
        public string ProjectId
        {
            get => projectId ?? string.Empty;
            set => projectId = value;
        }

    }//ProjectData.cs// 
}

