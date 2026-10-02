using Microsoft.EntityFrameworkCore.ChangeTracking;
using SupportToolsServerCore.Domain.Primitives;

namespace SupportToolsServer.Infrastructure.Repositories;

internal static class VersionedEntityEntryExtensions
{
    //ვერსიიანი ჩანაწერის Update (CLAUDE.md, Registry conventions). DbSet.Update ჩანაწერის ორიგინალ მნიშვნელობებად
    //მიმდინარეს იღებს, ჩანაწერი კი no-tracking-ით იკითხება და Update-მდე მისი ვერსია ზუსტად ერთხელ იზრდება (დომენის
    //მეთოდით, ან ახალი ეგზემპლარი შენახული Version + 1-ით იქმნება). ამიტომ concurrency token-ის ორიგინალი წინა
    //ვერსიაა: UPDATE ... WHERE Id = @Id AND Version = <წინა>. თუ ჩანაწერი წაკითხვის შემდეგ სხვამ შეცვალა ან წაშალა,
    //SaveChanges DbUpdateConcurrencyException-ს ისვრის
    public static void ExpectPreviousVersion<TEntity>(this EntityEntry<TEntity> entry) where TEntity : class
    {
        PropertyEntry<TEntity, int> version = entry.Property<int>(nameof(VersionedEntity<>.Version));
        version.OriginalValue = version.CurrentValue - 1;
    }
}
