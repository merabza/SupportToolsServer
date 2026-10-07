using System;
using System.Linq;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitRepoProjects;

namespace SupportToolsServer.Application.GitRepos;

internal static class GitRepoProjectContractMapper
{
    //კონტრაქტში რეპოზიტორია სახელით გადაიცემა. დამოკიდებულებები ანბანით (OrdinalIgnoreCase) ლაგდება: კლიენტის სიაში
    //მათი რიგი წინა სკანირებებზეა დამოკიდებული და მნიშვნელობა არ აქვს
    public static StsGitProjectDataModel ToContractModel(this GitRepoProject gitRepoProject, string gitName)
    {
        return new StsGitProjectDataModel
        {
            GitName = gitName,
            ProjectRelativePath = gitRepoProject.ProjectRelativePath,
            ProjectFileName = gitRepoProject.ProjectFileName,
            DependsOnProjectNames =
            [
                .. gitRepoProject.Dependencies.Select(x => x.ProjectName).Order(StringComparer.OrdinalIgnoreCase)
            ]
        };
    }
}
