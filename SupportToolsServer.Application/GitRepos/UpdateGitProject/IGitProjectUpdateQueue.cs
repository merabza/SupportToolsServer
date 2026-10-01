using System.Threading;
using System.Threading.Tasks;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//კლონირება და pull დიდხანს გრძელდება, ამიტომ ბრძანებები რიგში დგება და მოთხოვნის დამუშავებას არ აყოვნებს
public interface IGitProjectUpdateQueue
{
    ValueTask Enqueue(UpdateGitProjectCommand command, CancellationToken cancellationToken);
}
