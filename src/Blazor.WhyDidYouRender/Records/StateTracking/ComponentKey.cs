using System;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents a unique key for identifying a component instance in thread-safe operations.
/// This struct provides efficient equality comparison and hashing for component tracking.
/// </summary>
/// <remarks>
/// The ComponentKey uses the component's runtime hash code and type to create a unique identifier.
/// This allows for efficient lookup and comparison in concurrent collections while maintaining
/// thread safety and avoiding memory leaks from holding direct component references.
/// </remarks>
public readonly struct ComponentKey : IEquatable<ComponentKey>
{
    /// <summary>
    /// Gets the runtime hash code of the component instance.
    /// </summary>
    private readonly int _hashCode;

    /// <summary>
    /// Gets the type of the component.
    /// </summary>
    private readonly Type _componentType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentKey"/> struct.
    /// </summary>
    /// <param name="component">The component to create a key for.</param>
    /// <exception cref="ArgumentNullException">Thrown when component is null.</exception>
    public ComponentKey(ComponentBase component)
    {
        ArgumentNullException.ThrowIfNull(component);
        
        _hashCode = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(component);
        _componentType = component.GetType();
    }

    /// <summary>
    /// Gets the component type associated with this key.
    /// </summary>
    public Type ComponentType => _componentType;

    /// <summary>
    /// Gets the runtime hash code used for this key.
    /// </summary>
    public int RuntimeHashCode => _hashCode;

    /// <summary>
    /// Determines whether the specified ComponentKey is equal to this instance.
    /// </summary>
    /// <param name="other">The ComponentKey to compare with this instance.</param>
    /// <returns>True if the specified ComponentKey is equal to this instance; otherwise, false.</returns>
    public bool Equals(ComponentKey other)
    {
        return _hashCode == other._hashCode && _componentType == other._componentType;
    }

    /// <summary>
    /// Determines whether the specified object is equal to this instance.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is ComponentKey other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(_hashCode, _componentType);
    }

    /// <summary>
    /// Determines whether two ComponentKey instances are equal.
    /// </summary>
    /// <param name="left">The first ComponentKey to compare.</param>
    /// <param name="right">The second ComponentKey to compare.</param>
    /// <returns>True if the ComponentKey instances are equal; otherwise, false.</returns>
    public static bool operator ==(ComponentKey left, ComponentKey right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two ComponentKey instances are not equal.
    /// </summary>
    /// <param name="left">The first ComponentKey to compare.</param>
    /// <param name="right">The second ComponentKey to compare.</param>
    /// <returns>True if the ComponentKey instances are not equal; otherwise, false.</returns>
    public static bool operator !=(ComponentKey left, ComponentKey right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Returns a string representation of this ComponentKey.
    /// </summary>
    /// <returns>A string that represents this ComponentKey.</returns>
    public override string ToString()
    {
        return $"ComponentKey[{_componentType.Name}:{_hashCode:X8}]";
    }

    /// <summary>
    /// Creates a ComponentKey from a component instance.
    /// </summary>
    /// <param name="component">The component to create a key for.</param>
    /// <returns>A ComponentKey for the specified component.</returns>
    /// <exception cref="ArgumentNullException">Thrown when component is null.</exception>
    public static ComponentKey FromComponent(ComponentBase component) => new(component);

    /// <summary>
    /// Determines whether this key represents a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <returns>True if this key represents a component of type T; otherwise, false.</returns>
    public bool IsComponentType<T>() where T : ComponentBase => _componentType == typeof(T);

    /// <summary>
    /// Determines whether this key represents a component of the specified type.
    /// </summary>
    /// <param name="componentType">The component type to check.</param>
    /// <returns>True if this key represents a component of the specified type; otherwise, false.</returns>
    public bool IsComponentType(Type componentType) => _componentType == componentType;
}
