namespace Miller_Craft_Tools.ViewModel
{
    // Represents a level node in the TreeView
    public class LevelNode
    {
        public string LevelName { get; set; }
        public int ElementCount { get; set; }
        public List<CategoryNode> Categories { get; set; } = new List<CategoryNode>();
    }

    // Represents a category node under a level
    public class CategoryNode
    {
        public string CategoryName { get; set; }
        public int ElementCount { get; set; }
        public List<ElementNode> Elements { get; set; } = new List<ElementNode>();
    }

    // Represents an element node under a category
    public class ElementNode
    {
        public string ElementName { get; set; }
        public string ElementId { get; set; }
    }
}