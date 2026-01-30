namespace CrudDemo.Models;

public class CyberFeedItem
{
    public string? Source { get; set; }
    public string? Title { get; set; }
    public string? Link { get; set; }
    public string PublishedDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
}
