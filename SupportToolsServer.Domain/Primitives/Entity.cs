using System;
using System.Diagnostics.CodeAnalysis;

namespace SupportToolsServer.Domain.Primitives;

[SuppressMessage("Minor Code Smell", "S4035:Classes implementing 'IEquatable<T>' should be sealed",
    Justification =
        "This is an abstract base entity class. Equality members are sealed to prevent derived classes from changing equality semantics.")]
public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; }

    public bool Equals(Entity<TId>? other)
    {
        return Equals((object?)other);
    }

    public sealed override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Id.Equals(entity.Id);
    }

    public static bool operator ==(Entity<TId> left, Entity<TId> right) => Equals(left, right);

    public static bool operator !=(Entity<TId> left, Entity<TId> right) => !Equals(left, right);

    public sealed override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
