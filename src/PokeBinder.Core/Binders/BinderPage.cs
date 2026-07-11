namespace PokeBinder.Core.Binders;

public class BinderPage
{
    public Guid Id { get; set; }
    public Guid BinderId { get; set; }
    public Binder? Binder { get; set; }

    /// <summary>1-based. Pages always exist in physical sheet-pairs: leaf k = pages (2k-1, 2k).</summary>
    public int PageNumber { get; set; }

    public ICollection<BinderSlot> Slots { get; set; } = new List<BinderSlot>();
}
