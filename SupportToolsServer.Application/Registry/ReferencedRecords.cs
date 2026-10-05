using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Registry;

//სახელით მითითებული სხვა აგრეგატების ჩანაწერები upsert-ში (CLAUDE.md, Registry conventions). handler-ი ჯერ ყველა
//მითითებას ეძებს, მერე კი ყველა არარსებულ სახელს ერთ 404 ReferencedRecordsNotFound-ში აბრუნებს, ჩანაწერის ტიპებად
//დაჯგუფებულს, თითო სახელს ერთხელ
internal sealed class ReferencedRecords
{
    private readonly List<(string EntityName, string Name)> _missing = [];

    public bool AreAllFound => _missing.Count == 0;

    //სახელით მითითებული ჩანაწერი. ცარიელი სახელი მითითება არ არის, არარსებული სახელი კი აკლებულებს ემატება
    public async Task<TEntity?> Find<TEntity>(string? name, string entityName,
        Func<string, CancellationToken, Task<TEntity?>> getByName, CancellationToken cancellationToken)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return Found(entityName, name, await getByName(name, cancellationToken));
    }

    //იგივე უკვე წაკითხული ჩანაწერებიდან: მაგალითად, პროექტის git-ების სიისთვის ყველა git ერთხელ იკითხება
    public TEntity? Find<TEntity>(string? name, string entityName, Func<string, TEntity?> findByName)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return Found(entityName, name, findByName(name));
    }

    private TEntity? Found<TEntity>(string entityName, string name, TEntity? entity) where TEntity : class
    {
        if (entity is null)
        {
            _missing.Add((entityName, name));
        }

        return entity;
    }

    //ყველა არარსებული სახელი. ერთი სახელი ორ ველში შეიძლება იყოს (მაგალითად, ორივე ვებაგენტი ერთი ApiClient-ია),
    //ამიტომ ის ერთხელ იწერება
    public Error MissingError()
    {
        return SupportToolsServerApiClientErrors.ReferencedRecordsNotFound(_missing.Distinct()
            .ToLookup(x => x.EntityName, x => x.Name));
    }
}
