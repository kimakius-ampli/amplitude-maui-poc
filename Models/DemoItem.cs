namespace AmplitudeMauiPoc.Models;

public class DemoItem
{
    public string ItemCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string Category { get; set; } = "Standard";
    public DateTime LastUpdated { get; set; }
    public List<ActivityEntry> Activity { get; set; } = new();
}

public class ActivityEntry
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = "";
    public string Description { get; set; } = "";
}
