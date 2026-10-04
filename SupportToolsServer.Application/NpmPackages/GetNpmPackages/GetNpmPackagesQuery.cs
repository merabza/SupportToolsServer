using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.NpmPackages.GetNpmPackages;

public sealed class GetNpmPackagesQuery : IQuery<List<StsNpmPackageDataModel>>;
