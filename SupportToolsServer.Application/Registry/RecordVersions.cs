using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerApiContracts.Errors;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Registry;

//რეესტრის ჩანაწერის ვერსიის წესები (CLAUDE.md, Registry conventions)
internal static class RecordVersions
{
    //მოსალოდნელი ვერსიის შემოწმება. expectedVersion 0 ნიშნავს, რომ ჩანაწერი არ უნდა არსებობდეს, N კი იმას, რომ
    //შენახულის ვერსია N უნდა იყოს. storedVersion null ნიშნავს, რომ ჩანაწერი არ არსებობს
    public static Result Check(string entityName, string name, int expectedVersion, int? storedVersion)
    {
        if (storedVersion is null)
        {
            return expectedVersion == 0
                ? Result.Success()
                : SupportToolsServerApiClientErrors.RecordWithNameNotFound(entityName, name);
        }

        return storedVersion.Value == expectedVersion
            ? Result.Success()
            : SupportToolsServerApiClientErrors.ConcurrencyConflict(entityName, name, expectedVersion,
                storedVersion.Value);
    }

    //ცვლილებების შენახვა. ვერსია წაკითხვისას შემოწმდა, მაგრამ შენახვამდე სხვა მოთხოვნას ჩანაწერი შეიძლება შეეცვალა,
    //წაეშალა ან იმავე სახელით შეექმნა. მაშინ ბაზა ცვლილებას არ იღებს: UPDATE-სა და DELETE-ს Version-ის concurrency
    //token-ი აჩერებს (DbUpdateConcurrencyException), INSERT-ს კი სახელის unique index-ი (DbUpdateException). ასეთ დროს
    //შენახული ვერსია თავიდან იკითხება და შემოწმება მეორდება. მისი შეცდომა (ConcurrencyConflict, RecordWithNameNotFound)
    //ბრუნდება, ხოლო თუ შემოწმება ისევ გადის, ჩავარდნის მიზეზი სხვაა და გამონაკლისი გადის
    public static async Task<Result> SaveChanges(IUnitOfWork unitOfWork, string entityName, string name,
        int expectedVersion, Func<CancellationToken, Task<int?>> readStoredVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            Result checkResult = Check(entityName, name, expectedVersion, await readStoredVersion(cancellationToken));
            if (checkResult.IsFailure)
            {
                return checkResult;
            }

            throw;
        }
    }
}
