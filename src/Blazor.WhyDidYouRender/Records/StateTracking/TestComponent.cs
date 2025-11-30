using System;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// A test component specifically designed for stress testing state tracking operations.
/// This component provides controlled state modification capabilities for testing scenarios.
/// </summary>
/// <remarks>
/// This component is intended for testing purposes only and should not be used in production applications.
/// It provides simple state modification methods to simulate real component behavior during stress tests.
/// </remarks>
public class TestComponent : ComponentBase
{
	/// <summary>
	/// Gets or sets the unique identifier for this test component.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets the current counter value.
	/// </summary>
	public int Counter => _counter;

	/// <summary>
	/// Gets the current message value.
	/// </summary>
	public string Message => _message;

	/// <summary>
	/// Gets the time when the component was last modified.
	/// </summary>
	public DateTime LastModified => _lastModified;

	/// <summary>
	/// Gets the number of times the component state has been modified.
	/// </summary>
	public int ModificationCount => _counter;

	/// <summary>
	/// Gets whether the component has been modified since creation.
	/// </summary>
	public bool HasBeenModified => _counter > 0;

	/// <summary>
	/// Gets the age of the component since last modification.
	/// </summary>
	public TimeSpan AgeSinceLastModification => DateTime.UtcNow - _lastModified;

	private int _counter = 0;
	private string _message = "Initial";
	private DateTime _lastModified = DateTime.UtcNow;
	private readonly DateTime _createdAt = DateTime.UtcNow;

	/// <summary>
	/// Modifies the component state by incrementing the counter and updating the message.
	/// This method is used during stress testing to simulate state changes.
	/// </summary>
	public void ModifyState()
	{
		_counter++;
		_message = $"Modified {_counter}";
		_lastModified = DateTime.UtcNow;
	}

	/// <summary>
	/// Modifies the component state with a custom message.
	/// </summary>
	/// <param name="customMessage">The custom message to set.</param>
	public void ModifyState(string customMessage)
	{
		_counter++;
		_message = customMessage;
		_lastModified = DateTime.UtcNow;
	}

	/// <summary>
	/// Resets the component state to its initial values.
	/// </summary>
	public void ResetState()
	{
		_counter = 0;
		_message = "Initial";
		_lastModified = DateTime.UtcNow;
	}

	/// <summary>
	/// Performs multiple state modifications in sequence.
	/// This is useful for testing rapid state changes.
	/// </summary>
	/// <param name="count">The number of modifications to perform.</param>
	public void ModifyStateMultiple(int count)
	{
		for (int i = 0; i < count; i++)
		{
			ModifyState();
		}
	}

	/// <summary>
	/// Simulates a heavy state modification that might cause performance issues.
	/// This method creates temporary objects to simulate memory pressure.
	/// </summary>
	public void ModifyStateHeavy()
	{
		// Simulate some heavy work
		var tempData = new byte[1024]; // 1KB of temporary data
		for (int i = 0; i < tempData.Length; i++)
		{
			tempData[i] = (byte)(i % 256);
		}

		_counter++;
		_message = $"Heavy modification {_counter} with {tempData.Length} bytes";
		_lastModified = DateTime.UtcNow;

		// Let GC handle the temporary data
		GC.KeepAlive(tempData);
	}

	/// <summary>
	/// Gets a summary of the component's current state.
	/// </summary>
	/// <returns>A formatted string with component state information.</returns>
	public string GetStateSummary()
	{
		return $"TestComponent {Id}:\n"
			+ $"  Counter: {_counter}\n"
			+ $"  Message: {_message}\n"
			+ $"  Last Modified: {_lastModified:HH:mm:ss.fff}\n"
			+ $"  Created At: {_createdAt:HH:mm:ss.fff}\n"
			+ $"  Age Since Modification: {AgeSinceLastModification}\n"
			+ $"  Has Been Modified: {HasBeenModified}";
	}

	/// <summary>
	/// Creates a collection of test components for stress testing.
	/// </summary>
	/// <param name="count">The number of components to create.</param>
	/// <returns>An array of test components.</returns>
	public static TestComponent[] CreateTestComponents(int count)
	{
		var components = new TestComponent[count];
		for (int i = 0; i < count; i++)
		{
			components[i] = new TestComponent { Id = i };
		}
		return components;
	}

	/// <summary>
	/// Creates a collection of test components with initial state modifications.
	/// </summary>
	/// <param name="count">The number of components to create.</param>
	/// <param name="initialModifications">The number of initial modifications to apply to each component.</param>
	/// <returns>An array of test components with modified state.</returns>
	public static TestComponent[] CreateModifiedTestComponents(int count, int initialModifications = 1)
	{
		var components = CreateTestComponents(count);
		foreach (var component in components)
		{
			component.ModifyStateMultiple(initialModifications);
		}
		return components;
	}

	/// <summary>
	/// Returns a string representation of the test component.
	/// </summary>
	/// <returns>A string representation including the component ID and current state.</returns>
	public override string ToString()
	{
		return $"TestComponent[{Id}] - Counter: {_counter}, Message: '{_message}'";
	}
}
