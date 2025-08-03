namespace Blazor.WhyDidYouRender.Records;

/// <summary>
/// Represents memory usage measurement results from state tracking operations.
/// This record provides immutable memory usage data for performance analysis.
/// </summary>
public record MemoryUsageResults
{
    /// <summary>
    /// Gets the initial memory usage before operations (in bytes).
    /// </summary>
    public required long InitialMemory { get; init; }

    /// <summary>
    /// Gets the final memory usage after operations (in bytes).
    /// </summary>
    public required long FinalMemory { get; init; }

    /// <summary>
    /// Gets the total memory used by operations (in bytes).
    /// </summary>
    public required long MemoryUsed { get; init; }

    /// <summary>
    /// Gets the average memory used per operation (in bytes).
    /// </summary>
    public required long MemoryPerOperation { get; init; }

    /// <summary>
    /// Gets the number of operations performed during measurement.
    /// </summary>
    public required int OperationCount { get; init; }

    /// <summary>
    /// Gets a formatted summary of the memory usage results.
    /// </summary>
    /// <returns>A formatted string with memory usage information.</returns>
    public string GetFormattedSummary()
    {
        return $"Memory Usage Results:\n" +
               $"  Initial Memory: {InitialMemory:N0} bytes\n" +
               $"  Final Memory: {FinalMemory:N0} bytes\n" +
               $"  Memory Used: {MemoryUsed:N0} bytes\n" +
               $"  Memory Per Operation: {MemoryPerOperation:N0} bytes\n" +
               $"  Operation Count: {OperationCount:N0}";
    }
}
