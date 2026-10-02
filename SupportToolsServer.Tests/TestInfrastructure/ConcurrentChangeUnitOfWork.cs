using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using SystemTools.Domain.Abstractions;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Saves through the real unit of work, but first runs the change of another request (on its own context), as if that
//request had saved between the read and the save of the handler under test
internal sealed class ConcurrentChangeUnitOfWork : IUnitOfWork
{
    private readonly Func<Task> _concurrentChange;
    private readonly IUnitOfWork _inner;

    public ConcurrentChangeUnitOfWork(IUnitOfWork inner, Func<Task> concurrentChange)
    {
        _inner = inner;
        _concurrentChange = concurrentChange;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _concurrentChange();
        await _inner.SaveChangesAsync(cancellationToken);
    }

    public string GetTableName<T>() where T : class
    {
        return _inner.GetTableName<T>();
    }

    public IEntityType? GetEntityTypeByTableName(string tableName)
    {
        return _inner.GetEntityTypeByTableName(tableName);
    }

    public void SetCommandTimeout(TimeSpan timeout)
    {
        _inner.SetCommandTimeout(timeout);
    }
}
