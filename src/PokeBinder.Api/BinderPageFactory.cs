using PokeBinder.Core.Binders;
using PokeBinder.Infrastructure;

namespace PokeBinder.Api;

public static class BinderPageFactory
{
    /// <summary>
    /// Creates new pages (and their empty slots) and adds them to the binder.
    /// Explicitly Add()s each entity to the DbContext rather than relying solely
    /// on collection-navigation fixup: when appending to a binder that's already
    /// tracked (loaded via a query, not context.Add()'d itself in this unit of
    /// work), EF's DetectChanges can mark the new rows Modified instead of Added,
    /// producing a 0-rows-affected concurrency exception on save.
    /// </summary>
    public static void AppendPages(PokeBinderDbContext db, Binder binder, int startingPageNumber, int count)
    {
        var slotsPerPage = binder.Rows * binder.Columns;

        for (var i = 0; i < count; i++)
        {
            var page = new BinderPage
            {
                Id = Guid.NewGuid(),
                BinderId = binder.Id,
                PageNumber = startingPageNumber + i
            };

            for (var position = 0; position < slotsPerPage; position++)
            {
                var slot = new BinderSlot { Id = Guid.NewGuid(), PageId = page.Id, Position = position };
                page.Slots.Add(slot);
                db.BinderSlots.Add(slot);
            }

            binder.Pages.Add(page);
            db.BinderPages.Add(page);
        }
    }

    /// <summary>Rounds a raw page requirement up to the next even number, since binder page counts must always stay even (pages exist in physical sheet-pairs).</summary>
    public static int RoundUpToEven(int pageCount) => pageCount % 2 == 0 ? pageCount : pageCount + 1;
}
