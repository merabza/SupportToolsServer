using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitRepos;

namespace SupportToolsServer.Application.GitRepos.GitRemoteHeads;

//რეპოზიტორიების remote-ის ვერსიების ქეში (V3 სინქრონიზაციისთვის). ჩანაწერს ვადა აქვს (AppOptions), ვადაგასული ან
//არარსებული ჩანაწერი git ls-remote-ით ახლდება. შეცდომა არ ქეშირდება: მომდევნო მოთხოვნა ისევ სცდის
public interface IGitRemoteHeadsCache
{
    //თითო ჩანაწერი თითო რეპოზიტორიისთვის, გადაცემული რიგით
    Task<List<StsGitRemoteHeadDataModel>> GetHeads(IReadOnlyList<GitRepo> gitRepos,
        CancellationToken cancellationToken);

    //push-ის შემდეგ ჩანაწერი უქმდება, რომ მომდევნო მოთხოვნამ ახალი ვერსია remote-იდან წაიკითხოს
    Task Invalidate(string gitName, CancellationToken cancellationToken);
}
