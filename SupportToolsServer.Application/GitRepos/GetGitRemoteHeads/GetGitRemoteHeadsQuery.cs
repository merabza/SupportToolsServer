using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.GetGitRemoteHeads;

//GitNames ცარიელია: ყველა რეპოზიტორია. სხვა შემთხვევაში მხოლოდ ეს სახელები (OrdinalIgnoreCase), რომ ერთი პროექტის
//სინქრონიზაციამ ყველა რეპოზიტორიის remote არ შეამოწმოს
public sealed record GetGitRemoteHeadsQuery(IReadOnlyCollection<string> GitNames)
    : IQuery<List<StsGitRemoteHeadDataModel>>;
