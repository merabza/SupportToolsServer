using System;
using System.Collections.Generic;
using SupportToolsServer.Domain.Primitives;

namespace SupportToolsServer.Domain.GitIgnoreFileTypes;

public class GitIgnoreFileTypeId : ValueObject
{
    public GitIgnoreFileTypeId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static GitIgnoreFileTypeId CreateUnique()
    {
        return new GitIgnoreFileTypeId(Guid.NewGuid());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
