using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Environments;
using SupportToolsServer.Application.Servers;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;

namespace SupportToolsServer.Application.Projects;

//ServerInfo-ების სერვერებისა და გარემოების სახელები Id-ების მიხედვით: ჩანაწერის წაშლისას მომხმარებლების სიისთვის (409
//RecordIsInUse), სადაც ServerInfo თავისი "<სერვერი>|<გარემო>" გასაღებით ჩანს
internal sealed class ServerInfoKeyNames
{
    public required IReadOnlyDictionary<ServerId, string> Servers { get; init; }
    public required IReadOnlyDictionary<DeploymentEnvironmentId, string> Environments { get; init; }

    public static async Task<ServerInfoKeyNames> Read(IServerRepository serverRepository,
        IDeploymentEnvironmentRepository environmentRepository, CancellationToken cancellationToken)
    {
        return new ServerInfoKeyNames
        {
            Servers = (await serverRepository.GetAll(cancellationToken)).ToNamesById(),
            Environments = (await environmentRepository.GetAll(cancellationToken)).ToNamesById()
        };
    }

    //ServerInfo-ების გასაღებები სერვერისა და გარემოს სახელების რიგით, როგორც კონტრაქტში
    public IEnumerable<string> SortedKeys(IEnumerable<ServerInfo> serverInfos)
    {
        return serverInfos
            .Select(x => (ServerName: Servers[x.ServerId], EnvironmentName: Environments[x.EnvironmentId]))
            .OrderBy(x => x.ServerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.EnvironmentName, StringComparer.OrdinalIgnoreCase)
            .Select(x => ServerInfoContractMapper.Key(x.ServerName, x.EnvironmentName));
    }
}
