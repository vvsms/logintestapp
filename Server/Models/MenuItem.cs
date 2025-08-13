namespace Server.Models;

public class MenuItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string AllowedRolesCsv { get; set; } = "";
    public string AllowedPoliciesCsv { get; set; } = "";
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
