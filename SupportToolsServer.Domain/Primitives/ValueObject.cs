using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SupportToolsServer.Domain.Primitives;

[SuppressMessage("SonarQube", "S4035:Implement IEqualityComparer<T> instead",
    Justification =
        "This is an abstract base class for value objects. The GetType() check in Equals ensures type safety, and derived classes define equality through GetEqualityComponents().")]
public abstract class ValueObject : IEquatable<ValueObject>
{
    public bool Equals(ValueObject? other)
    {
        return Equals((object?)other);
    }

    public abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public static bool operator ==(ValueObject left, ValueObject right) => Equals(left, right);

    public static bool operator !=(ValueObject left, ValueObject right) => !Equals(left, right);

    public override int GetHashCode()
    {
        return GetEqualityComponents().Select(obj => obj.GetHashCode()).Aggregate((x, y) => x ^ y);
    }
}
