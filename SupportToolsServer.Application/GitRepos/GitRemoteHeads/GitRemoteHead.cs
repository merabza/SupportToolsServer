namespace SupportToolsServer.Application.GitRepos.GitRemoteHeads;

//remote რეპოზიტორიის default branch (HEAD-ის მიერ მითითებული) და მისი commit-ის იდენტიფიკატორი
public sealed record GitRemoteHead(string BranchName, string CommitId);
