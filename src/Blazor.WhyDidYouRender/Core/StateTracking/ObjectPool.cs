using System;
using System.Collections.Concurrent;

namespace Blazor.WhyDidYouRender.Core.StateTracking;

/// <summary>
/// Simple object pool for reusing instances to reduce garbage collection pressure.
/// Provides thread-safe pooling of objects with configurable creation strategy.
/// </summary>
/// <typeparam name="T">The type of objects to pool. Must be a reference type with a parameterless constructor.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ObjectPool{T}"/> class with custom object creation.
/// </remarks>
/// <param name="objectGenerator">Factory function for creating new instances.</param>
/// <exception cref="ArgumentNullException">Thrown when objectGenerator is null.</exception>
public class ObjectPool<T>(Func<T> objectGenerator) where T : class, new() {
	/// <summary>
	/// Thread-safe collection of pooled objects.
	/// </summary>
	private readonly ConcurrentBag<T> _objects = [];

	/// <summary>
	/// Factory function for creating new instances when the pool is empty.
	/// </summary>
	private readonly Func<T> _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));

	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectPool{T}"/> class with default object creation.
	/// </summary>
	public ObjectPool() : this(() => new T()) { }

	/// <summary>
	/// Gets an object from the pool, creating a new one if the pool is empty.
	/// </summary>
	/// <returns>An object instance from the pool or newly created.</returns>
	public T Get() =>
		_objects.TryTake(out var item) ? item : _objectGenerator();

	/// <summary>
	/// Returns an object to the pool for reuse.
	/// </summary>
	/// <param name="item">The object to return to the pool.</param>
	/// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
	public void Return(T item) {
		ArgumentNullException.ThrowIfNull(item);

		_objects.Add(item);
	}

	/// <summary>
	/// Gets the approximate number of objects currently in the pool.
	/// </summary>
	/// <remarks>
	/// This is an approximate count due to the concurrent nature of the collection.
	/// The actual count may vary between calls in multi-threaded scenarios.
	/// </remarks>
	public int Count => _objects.Count;

	/// <summary>
	/// Clears all objects from the pool.
	/// </summary>
	public void Clear() {
		while (_objects.TryTake(out _)) { }
	}
}
