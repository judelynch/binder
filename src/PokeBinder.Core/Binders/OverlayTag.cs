namespace PokeBinder.Core.Binders;

public class OverlayTag
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ColourHex { get; set; } = string.Empty;
}
