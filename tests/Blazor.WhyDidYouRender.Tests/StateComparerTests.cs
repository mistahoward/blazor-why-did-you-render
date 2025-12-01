using System.Collections;
using System.Collections.Generic;
using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Core.StateTracking;
using Blazor.WhyDidYouRender.Records;
using Blazor.WhyDidYouRender.Records.StateTracking;

namespace Blazor.WhyDidYouRender.Tests;

public class StateComparerTests
{
	private readonly StateComparer _comparer = new();

	private sealed class EquatableModel : IEquatable<EquatableModel>
	{
		public int Id { get; set; }
		public string? Name { get; set; }

		public bool Equals(EquatableModel? other)
		{
			if (other is null)
				return false;
			return Id == other.Id && Name == other.Name;
		}

		public override bool Equals(object? obj) => Equals(obj as EquatableModel);

		public override int GetHashCode() => HashCode.Combine(Id, Name);
	}

	[Fact]
	public void AreEqual_SimpleValues_UseValueEquality()
	{
		Assert.True(_comparer.AreEqual(1, 1, typeof(int)));
		Assert.False(_comparer.AreEqual(1, 2, typeof(int)));
		Assert.True(_comparer.AreEqual("a", "a", typeof(string)));
		Assert.False(_comparer.AreEqual("a", "b", typeof(string)));
	}

	[Fact]
	public void AreEqual_Collections_WithTrackCollectionContents_CompareContents()
	{
		var fieldInfo = new FieldTrackingInfo(
			fieldInfo: typeof(StateComparerTests).GetField(
				"_listField",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
			)!,
			strategy: TrackingStrategy.ExplicitTrack,
			trackStateAttribute: new TrackStateAttribute
			{
				UseCustomComparer = true,
				TrackCollectionContents = true,
				MaxComparisonDepth = 1,
			}
		);

		var previous = new List<int> { 1, 2, 3 };
		var currentSame = new List<int> { 1, 2, 3 };
		var currentDifferent = new List<int> { 1, 2, 4 };

		Assert.True(_comparer.AreEqual(previous, currentSame, typeof(List<int>), fieldInfo));
		Assert.False(_comparer.AreEqual(previous, currentDifferent, typeof(List<int>), fieldInfo));
	}

	// backing field used only to obtain FieldInfo for collection tests above
	private readonly List<int> _listField = new();

	[Fact]
	public void AreParameterValuesEqual_Unannotated_UsesSimpleEquals()
	{
		var previous = new EquatableModel { Id = 1, Name = "A" };
		var currentSame = new EquatableModel { Id = 1, Name = "A" };
		var currentDifferent = new EquatableModel { Id = 2, Name = "B" };

		// No TrackStateAttribute -> falls back to previous.Equals(current)
		Assert.True(_comparer.AreParameterValuesEqual(previous, currentSame, typeof(EquatableModel), trackStateAttribute: null));
		Assert.False(_comparer.AreParameterValuesEqual(previous, currentDifferent, typeof(EquatableModel), trackStateAttribute: null));
	}

	[Fact]
	public void AreParameterValuesEqual_AnnotatedCollection_UsesContentComparison()
	{
		var attr = new TrackStateAttribute { MaxComparisonDepth = 1 };

		IEnumerable prev = new List<int> { 1, 2, 3 };
		IEnumerable currSame = new List<int> { 1, 2, 3 };
		IEnumerable currDifferent = new List<int> { 1, 2, 4 };

		Assert.True(_comparer.AreParameterValuesEqual(prev, currSame, typeof(List<int>), attr));
		Assert.False(_comparer.AreParameterValuesEqual(prev, currDifferent, typeof(List<int>), attr));
	}

	[Fact]
	public void AreParameterValuesEqual_AnnotatedNonCollection_UsesIEquatableWhenRequested()
	{
		var attr = new TrackStateAttribute { UseCustomComparer = true };

		var previous = new EquatableModel { Id = 1, Name = "A" };
		var currentSame = new EquatableModel { Id = 1, Name = "A" };
		var currentDifferent = new EquatableModel { Id = 2, Name = "B" };

		Assert.True(_comparer.AreParameterValuesEqual(previous, currentSame, typeof(EquatableModel), attr));
		Assert.False(_comparer.AreParameterValuesEqual(previous, currentDifferent, typeof(EquatableModel), attr));
	}

	[Fact]
	public void AreParameterValuesEqual_MaxDepthZero_StillPerformsContentComparison()
	{
		// For parameter comparison, MaxComparisonDepth <= 0 is normalized to 1,
		// so annotated collections still use content comparison by default.
		var attr = new TrackStateAttribute { MaxComparisonDepth = 0 };

		var previous = new List<int> { 1, 2, 3 };
		var sameRef = previous;
		var newRefSameContents = new List<int> { 1, 2, 3 };

		Assert.True(_comparer.AreParameterValuesEqual(previous, sameRef, typeof(List<int>), attr));
		Assert.True(_comparer.AreParameterValuesEqual(previous, newRefSameContents, typeof(List<int>), attr));
	}
}
