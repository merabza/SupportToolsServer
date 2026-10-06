using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Collects the SQL of the commands that read rows, which shows the columns that a query reads
internal sealed class ReaderCommandCollector : DbCommandInterceptor
{
    private readonly ConcurrentQueue<string> _commandTexts = new();

    public IReadOnlyCollection<string> CommandTexts => _commandTexts;

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        _commandTexts.Enqueue(command.CommandText);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _commandTexts.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }
}
