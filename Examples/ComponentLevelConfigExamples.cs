using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Components;

namespace Blazor.WhyDidYouRender.Examples;

/// <summary>
/// Example component with state tracking completely disabled.
/// This is useful for performance-critical components or components
/// where state tracking would provide no value.
/// </summary>
[IgnoreStateTracking("Performance-critical component with frequent updates")]
public class HighPerformanceComponent : TrackedComponentBase
{
	// Even though these are simple types that would normally be auto-tracked,
	// they will be ignored due to the component-level attribute
	private int _frameCount = 0;
	private DateTime _lastUpdate = DateTime.Now;
	private string _status = "Running";

	protected override void OnAfterRender(bool firstRender)
	{
		_frameCount++;
		_lastUpdate = DateTime.Now;
		_status = $"Frame {_frameCount}";

		base.OnAfterRender(firstRender);
	}
}

/// <summary>
/// Example component with custom state tracking configuration.
/// This demonstrates fine-grained control over state tracking behavior.
/// </summary>
[StateTrackingOptions(
	MaxFields = 5,
	AutoTrackSimpleTypes = false,
	LogStateChanges = true,
	MaxComparisonDepth = 1,
	Description = "Custom configuration for selective state tracking"
)]
public class CustomStateTrackingComponent : TrackedComponentBase
{
	// These simple types won't be auto-tracked due to AutoTrackSimpleTypes = false
	private int _counter = 0;
	private string _message = "Hello";

	// Only this field will be tracked because it has the explicit attribute
	[TrackState("Critical state that affects rendering")]
	private ImportantData? _importantData = new();

	// This will be ignored even with explicit tracking due to component-level limits
	[TrackState]
	private string _extraField1 = "Extra 1";

	[TrackState]
	private string _extraField2 = "Extra 2";

	[TrackState]
	private string _extraField3 = "Extra 3";

	[TrackState]
	private string _extraField4 = "Extra 4";

	// This field exceeds MaxFields = 5, so it may not be tracked
	[TrackState]
	private string _extraField5 = "Extra 5";

	public void UpdateCounter()
	{
		_counter++; // Won't trigger state change detection
	}

	public void UpdateImportantData()
	{
		_importantData = new ImportantData { Value = Random.Shared.Next(1, 100) };
		// Will trigger state change detection
	}

	public class ImportantData
	{
		public int Value { get; set; }

		public override bool Equals(object? obj)
		{
			return obj is ImportantData other && Value == other.Value;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}

/// <summary>
/// Example component that inherits state tracking configuration.
/// This shows how component-level attributes work with inheritance.
/// </summary>
public class InheritedComponent : CustomStateTrackingComponent
{
	// This component inherits the state tracking configuration from its base class
	// The MaxFields = 5 and AutoTrackSimpleTypes = false settings apply here too

	private string _childField = "Child data";

	[TrackState("Child-specific tracked field")]
	private ChildData? _childData = new();

	public void UpdateChildData()
	{
		_childData = new ChildData { Name = $"Child {Random.Shared.Next(1, 100)}" };
	}

	public class ChildData
	{
		public string Name { get; set; } = string.Empty;

		public override bool Equals(object? obj)
		{
			return obj is ChildData other && Name == other.Name;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}

/// <summary>
/// Example component that demonstrates collection state tracking.
/// This shows how to track changes to collection contents.
/// </summary>
[StateTrackingOptions(EnableStateTracking = true, LogStateChanges = true, Description = "Component optimized for collection tracking")]
public class CollectionTrackingComponent : TrackedComponentBase
{
	// Basic collection tracking (reference changes only)
	[TrackState("List of items - reference tracking")]
	private List<string>? _items = new();

	// Advanced collection tracking (content changes)
	[TrackState(TrackCollectionContents = true, UseCustomComparer = true, Description = "Detailed item tracking with content comparison")]
	private List<TrackedItem>? _trackedItems = new();

	// Dictionary tracking
	[TrackState("Configuration dictionary")]
	private Dictionary<string, object>? _config = new();

	public void AddItem(string item)
	{
		_items?.Add(item);
	}

	public void AddTrackedItem(string name, int value)
	{
		_trackedItems?.Add(new TrackedItem { Name = name, Value = value });
	}

	public void UpdateConfig(string key, object value)
	{
		if (_config != null)
		{
			_config[key] = value;
		}
	}

	public class TrackedItem
	{
		public string Name { get; set; } = string.Empty;
		public int Value { get; set; }

		public override bool Equals(object? obj)
		{
			return obj is TrackedItem other && Name == other.Name && Value == other.Value;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Value);
		}
	}
}

/// <summary>
/// Example component that shows mixed tracking strategies.
/// This demonstrates how different fields can have different tracking approaches.
/// </summary>
public class MixedTrackingComponent : TrackedComponentBase
{
	// Auto-tracked simple types
	private int _autoTrackedCounter = 0;
	private string _autoTrackedMessage = "Auto";

	// Explicitly tracked complex type with custom comparison
	[TrackState(UseCustomComparer = true, MaxComparisonDepth = 2, Description = "Deep comparison for nested object")]
	private NestedData? _nestedData = new();

	// Explicitly ignored field
	[IgnoreState("Temporary calculation cache")]
	private Dictionary<string, object>? _cache = new();

	// Collection with content tracking
	[TrackState(TrackCollectionContents = true)]
	private HashSet<string>? _tags = new();

	public void UpdateAll()
	{
		_autoTrackedCounter++;
		_autoTrackedMessage = $"Updated {_autoTrackedCounter}";

		if (_nestedData != null)
		{
			_nestedData.Level1.Value = Random.Shared.Next(1, 100);
			_nestedData.Level1.Level2.Data = $"Data {Random.Shared.Next(1, 100)}";
		}

		_cache?.Clear(); // Won't trigger state change detection
		_tags?.Add($"Tag{Random.Shared.Next(1, 100)}");
	}

	public class NestedData
	{
		public Level1Data Level1 { get; set; } = new();

		public override bool Equals(object? obj)
		{
			return obj is NestedData other && Level1.Equals(other.Level1);
		}

		public override int GetHashCode()
		{
			return Level1.GetHashCode();
		}
	}

	public class Level1Data
	{
		public int Value { get; set; }
		public Level2Data Level2 { get; set; } = new();

		public override bool Equals(object? obj)
		{
			return obj is Level1Data other && Value == other.Value && Level2.Equals(other.Level2);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Value, Level2);
		}
	}

	public class Level2Data
	{
		public string Data { get; set; } = string.Empty;

		public override bool Equals(object? obj)
		{
			return obj is Level2Data other && Data == other.Data;
		}

		public override int GetHashCode()
		{
			return Data.GetHashCode();
		}
	}
}
