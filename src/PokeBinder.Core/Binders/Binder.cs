namespace PokeBinder.Core.Binders;

public class Binder
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ColourHex { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int Columns { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }

    public ICollection<BinderPage> Pages { get; set; } = new List<BinderPage>();
}
