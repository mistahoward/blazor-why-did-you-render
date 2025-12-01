using System.Collections.Generic;
using System.Linq;
using Blazor.WhyDidYouRender.Attributes;
using Blazor.WhyDidYouRender.Core;
using Microsoft.AspNetCore.Components;

namespace Blazor.WhyDidYouRender.Tests;

public class ParameterChangeDetectorTests
{
    private sealed class SimpleValueComponent : ComponentBase
    {
        [Parameter]
        public int Count { get; set; }
    }

    private sealed class SimpleRefComponent : ComponentBase
    {
        public sealed class Model
        {
            public int Id { get; set; }
        }

        [Parameter]
        public Model? Data { get; set; }
    }

    private sealed class TrackedCollectionComponent : ComponentBase
    {
        [Parameter]
        [TrackState] // opt-in deep comparison; collections compare contents
        public List<int>? Items { get; set; }
    }

    [Fact]
    public void DetectParameterChanges_ValueType_Unannotated_UsesValueEquality()
    {
        var component = new SimpleValueComponent { Count = 1 };
        var detector = new ParameterChangeDetector();

        // First call: initial non-null value should be recorded as a meaningful change
        var first = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.NotNull(first);
        var firstChange = Assert.Single(first!);
        Assert.Equal("Count", firstChange.Key);

        component.Count = 1;
        var second = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.Null(second);

        component.Count = 2;
        var third = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.NotNull(third);
        var thirdChange = Assert.Single(third!);
        Assert.Equal("Count", thirdChange.Key);
    }

    [Fact]
    public void DetectParameterChanges_RefType_Unannotated_UsesReferenceEquality()
    {
        var model = new SimpleRefComponent.Model { Id = 1 };
        var component = new SimpleRefComponent { Data = model };
        var detector = new ParameterChangeDetector();

        var first = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.NotNull(first);
        Assert.Single(first!);

        // Same reference -> no change
        component.Data = model;
        var second = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.Null(second);

        // New instance with same values -> treated as a change for unannotated parameters
        component.Data = new SimpleRefComponent.Model { Id = 1 };
        var third = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.NotNull(third);
        Assert.Single(third!);

        var changeEntry = third!.Single().Value!;
        Assert.True(ParameterChangeDetector.HasMeaningfulParameterChange(changeEntry));
    }

    [Fact]
    public void DetectParameterChanges_TrackedCollection_UsesDeepComparisonForMeaningfulness()
    {
        var component = new TrackedCollectionComponent
        {
            Items = new List<int> { 1, 2, 3 },
        };
        var detector = new ParameterChangeDetector();

        // First call: initial non-null value is a meaningful change
        var first = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.NotNull(first);
        var firstChange = Assert.Single(first!);
        Assert.Equal("Items", firstChange.Key);

        // New list instance with the same contents -> change detected, but not meaningful
        component.Items = new List<int> { 1, 2, 3 };
        var second = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.NotNull(second);
        var secondChange = Assert.Single(second!);

        var secondChangeValue = secondChange.Value!;
        var changeType = secondChangeValue.GetType();
        var isMeaningfulProp = changeType.GetProperty("IsMeaningful");
        Assert.NotNull(isMeaningfulProp);
        var isMeaningful = (bool?)isMeaningfulProp!.GetValue(secondChangeValue);
        Assert.False(isMeaningful);
        Assert.False(ParameterChangeDetector.HasMeaningfulParameterChange(secondChangeValue));

        // New list instance with different contents -> meaningful change
        component.Items = new List<int> { 1, 2, 4 };
        var third = detector.DetectParameterChanges(component, "OnParametersSet");
        Assert.NotNull(third);
        var thirdChange = Assert.Single(third!);

        var thirdChangeValue = thirdChange.Value!;
        var isMeaningful2 = (bool?)isMeaningfulProp!.GetValue(thirdChangeValue);
        Assert.True(isMeaningful2);
        Assert.True(ParameterChangeDetector.HasMeaningfulParameterChange(thirdChangeValue));
    }
}

