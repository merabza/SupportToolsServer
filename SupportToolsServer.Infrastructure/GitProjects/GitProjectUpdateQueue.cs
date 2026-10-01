using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;

namespace SupportToolsServer.Infrastructure.GitProjects;

//ერთადერთი მკითხველი (GitProjectUpdateBackgroundService) ბრძანებებს თანმიმდევრობით ასრულებს,
//ამიტომ ერთი და იგივე ფოლდერი ორ git პროცესს ერთდროულად არ ეხება
public sealed class GitProjectUpdateQueue : IGitProjectUpdateQueue
{
    private readonly Channel<UpdateGitProjectCommand> _channel =
        Channel.CreateUnbounded<UpdateGitProjectCommand>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask Enqueue(UpdateGitProjectCommand command, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(command, cancellationToken);
    }

    public IAsyncEnumerable<UpdateGitProjectCommand> ReadAll(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
