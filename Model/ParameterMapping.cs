using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Miller_Craft_Tools.Utils;

namespace Miller_Craft_Tools.Model
{
    /// <summary>
    /// Defines the sync direction for a parameter
    /// </summary>
    public enum SyncDirection
    {
        /// <summary>
        /// Sync from Revit to web application
        /// </summary>
        RevitToWeb,
        
        /// <summary>
        /// Sync from web application to Revit
        /// </summary>
        WebToRevit,
        
        /// <summary>
        /// Sync in both directions
        /// </summary>
        Both
    }
    
    /// <summary>
    /// Defines a mapping between a Revit parameter and a web application field
    /// </summary>
    public class ParameterMappingRule
    {
        /// <summary>
        /// The category of the Revit parameter (e.g., "Project Information")
        /// </summary>
        [JsonProperty("revitCategory")]
        public string RevitCategory { get; set; }
        
        /// <summary>
        /// The name of the Revit parameter
        /// </summary>
        [JsonProperty("revitParameterName")]
        public string RevitParameterName { get; set; }
        
        /// <summary>
        /// The field name in the web application
        /// </summary>
        [JsonProperty("webAppField")]
        public string WebAppField { get; set; }
        
        /// <summary>
        /// The direction for syncing this parameter
        /// </summary>
        [JsonProperty("syncDirection")]
        public SyncDirection SyncDirection { get; set; }
        
        /// <summary>
        /// Whether this parameter is required for sync
        /// </summary>
        [JsonProperty("isRequired")]
        public bool IsRequired { get; set; }
    }
    
    /// <summary>
    /// Manages parameter mappings between Revit and the web application
    /// </summary>
    public class ParameterMappingConfiguration
    {
        private readonly List<ParameterMappingRule> _mappingRules;
        
        /// <summary>
        /// Creates a new parameter mapping configuration with predefined rules from the integration guide
        /// </summary>
        public ParameterMappingConfiguration()
        {
            _mappingRules = GenerateDefaultMappingRules();
        }
        
        /// <summary>
        /// Gets all mapping rules
        /// </summary>
        public IReadOnlyList<ParameterMappingRule> MappingRules => _mappingRules.AsReadOnly();
        
        /// <summary>
        /// Gets mapping rules for Revit to web direction (including bidirectional)
        /// </summary>
        public IEnumerable<ParameterMappingRule> RevitToWebRules => 
            _mappingRules.Where(r => r.SyncDirection == SyncDirection.RevitToWeb || r.SyncDirection == SyncDirection.Both);
            
        /// <summary>
        /// Gets mapping rules for web to Revit direction (including bidirectional)
        /// </summary>
        public IEnumerable<ParameterMappingRule> WebToRevitRules => 
            _mappingRules.Where(r => r.SyncDirection == SyncDirection.WebToRevit || r.SyncDirection == SyncDirection.Both);
        
        /// <summary>
        /// Gets a mapping rule for a given Revit parameter
        /// </summary>
        public ParameterMappingRule GetRuleForRevitParameter(string category, string parameterName)
        {
            return _mappingRules.FirstOrDefault(r => 
                string.Equals(r.RevitCategory, category, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.RevitParameterName, parameterName, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Gets a mapping rule for a given web application field
        /// </summary>
        public ParameterMappingRule GetRuleForWebField(string webAppField)
        {
            return _mappingRules.FirstOrDefault(r => 
                string.Equals(r.WebAppField, webAppField, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Determines if a given Revit parameter should be synced to the web
        /// </summary>
        public bool ShouldSyncToWeb(string category, string parameterName)
        {
            var rule = GetRuleForRevitParameter(category, parameterName);
            return rule != null && (rule.SyncDirection == SyncDirection.RevitToWeb || rule.SyncDirection == SyncDirection.Both);
        }
        
        /// <summary>
        /// Determines if a given web field should be synced to Revit
        /// </summary>
        public bool ShouldSyncToRevit(string webAppField)
        {
            var rule = GetRuleForWebField(webAppField);
            return rule != null && (rule.SyncDirection == SyncDirection.WebToRevit || rule.SyncDirection == SyncDirection.Both);
        }
        
        /// <summary>
        /// Gets the web field name for a given Revit parameter
        /// </summary>
        public string GetWebFieldForRevitParameter(string category, string parameterName)
        {
            var rule = GetRuleForRevitParameter(category, parameterName);
            return rule?.WebAppField;
        }
        
        /// <summary>
        /// Gets the Revit parameter info for a given web field
        /// </summary>
        public (string category, string parameterName) GetRevitParameterForWebField(string webAppField)
        {
            var rule = GetRuleForWebField(webAppField);
            return rule != null ? (rule.RevitCategory, rule.RevitParameterName) : (null, null);
        }
        
        /// <summary>
        /// Adds a custom mapping rule
        /// </summary>
        public void AddMappingRule(ParameterMappingRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            
            // Remove any existing rule for the same parameter
            _mappingRules.RemoveAll(r => 
                string.Equals(r.RevitCategory, rule.RevitCategory, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.RevitParameterName, rule.RevitParameterName, StringComparison.OrdinalIgnoreCase));
                
            // Add the new rule
            _mappingRules.Add(rule);
        }
        
        /// <summary>
        /// Creates the default mapping rules for all Miller Craft shared parameters
        /// Based on Project Parameters.png - 46 parameters total
        /// </summary>
        private static List<ParameterMappingRule> GenerateDefaultMappingRules()
        {
            return new List<ParameterMappingRule>
            {
                // ===== CONTACT / PEOPLE PARAMETERS =====
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Client.Name",
                    WebAppField = "clientName",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Contact.Contractor",
                    WebAppField = "contactContractor",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Contact.Designer",
                    WebAppField = "contactDesigner",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Contact.Drafter",
                    WebAppField = "contactDrafter",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Contact.Energy",
                    WebAppField = "contactEnergy",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Contact.Owner",
                    WebAppField = "contactOwner",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Contact.Owner.Address",
                    WebAppField = "contactOwnerAddress",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Contact.Stormwater",
                    WebAppField = "contactStormwater",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Contact.Structural",
                    WebAppField = "contactStructural",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Project.Engineer",
                    WebAppField = "projectEngineer",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Property.Owner",
                    WebAppField = "propertyOwner",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                
                // ===== ENERGY ANALYSIS PARAMETERS =====
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Energy.Area.Wall",
                    WebAppField = "energyAreaWall",
                    SyncDirection = SyncDirection.RevitToWeb,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Energy.Area.Wall.Garage",
                    WebAppField = "energyAreaWallGarage",
                    SyncDirection = SyncDirection.RevitToWeb,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Energy.Calc",
                    WebAppField = "energyCalc",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Energy.U.Fenestration",
                    WebAppField = "energyUFenestration",
                    SyncDirection = SyncDirection.RevitToWeb,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Energy.U.Walls",
                    WebAppField = "energyUWalls",
                    SyncDirection = SyncDirection.RevitToWeb,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Energy.U.Walls.Garage",
                    WebAppField = "energyUWallsGarage",
                    SyncDirection = SyncDirection.RevitToWeb,
                    IsRequired = false
                },
                
                // ===== EXISTING CONDITIONS PARAMETERS =====
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Existing.Attached.Garage.Area",
                    WebAppField = "existingAttachedGarageArea",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Existing.Bathrooms",
                    WebAppField = "existingBathrooms",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Existing.Bedrooms",
                    WebAppField = "existingBedrooms",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Existing.Residence.Area",
                    WebAppField = "existingResidenceArea",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                
                // ===== PROJECT INFO / DESCRIPTION PARAMETERS =====
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Code.Requirements",
                    WebAppField = "codeRequirements",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Info.Project.Description",
                    WebAppField = "projectDescription",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Legal.Text",
                    WebAppField = "legalText",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Name",
                    WebAppField = "name",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = true
                },
                
                // ===== PROPERTY / SITE PARAMETERS =====
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Area",
                    WebAppField = "area",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Jurisdiction",
                    WebAppField = "jurisdiction",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Land.Use",
                    WebAppField = "landUse",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Local.Order",
                    WebAppField = "localOrder",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Lot.Coverage",
                    WebAppField = "lotCoverage",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Lot.Size",
                    WebAppField = "lotSize",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Parcel.Number",
                    WebAppField = "parcelNumber",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Property.Type",
                    WebAppField = "propertyType",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Setbacks",
                    WebAppField = "setbacks",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Zoning",
                    WebAppField = "zoning",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                
                // ===== UTILITIES / SERVICES PARAMETERS =====
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Sewer.Septic",
                    WebAppField = "sewerSeptic",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Vitality.Service",
                    WebAppField = "vitalityService",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                
                // ===== TAG / ANNOTATION PARAMETERS =====
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Filter.Tag",
                    WebAppField = "filterTag",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Finish.Exterior",
                    WebAppField = "finishExterior",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Finishes.Interior",
                    WebAppField = "finishesInterior",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Tag.Text.Instance",
                    WebAppField = "tagTextInstance",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Tag.Text.Type",
                    WebAppField = "tagTextType",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Text",
                    WebAppField = "text",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Text.Multiline",
                    WebAppField = "textMultiline",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                },
                new ParameterMappingRule
                {
                    RevitCategory = "Project Information",
                    RevitParameterName = "sp.Visible",
                    WebAppField = "visible",
                    SyncDirection = SyncDirection.Both,
                    IsRequired = false
                }
            };
        }
        
        /// <summary>
        /// Serializes the mapping rules to JSON
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(_mappingRules, Formatting.Indented);
        }
        
        /// <summary>
        /// Creates a mapping configuration from JSON
        /// </summary>
        public static ParameterMappingConfiguration FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new ParameterMappingConfiguration();
            
            var config = new ParameterMappingConfiguration();
            try
            {
                var rules = JsonConvert.DeserializeObject<List<ParameterMappingRule>>(json);
                if (rules != null && rules.Count > 0)
                {
                    config._mappingRules.Clear();
                    config._mappingRules.AddRange(rules);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error parsing mapping rules: {ex.Message}");
            }
            
            return config;
        }
    }
}
