using System;

namespace Blazor.WhyDidYouRender.Records.StateTracking;

/// <summary>
/// Represents configurable performance thresholds for alerting and monitoring.
/// This record provides immutable threshold configuration for performance analysis.
/// </summary>
public record PerformanceThresholds
{
    /// <summary>
    /// Gets the maximum acceptable average operation time before triggering warnings.
    /// Default is 10 milliseconds.
    /// </summary>
    public TimeSpan MaxAverageTime { get; init; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Gets the maximum acceptable operation time for any single operation before triggering errors.
    /// Default is 100 milliseconds.
    /// </summary>
    public TimeSpan MaxOperationTime { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets the maximum acceptable failure rate (0.0 to 1.0) before triggering errors.
    /// Default is 5% (0.05).
    /// </summary>
    public double MaxFailureRate { get; init; } = 0.05;

    /// <summary>
    /// Gets the minimum number of operations required before thresholds are evaluated.
    /// This prevents false alerts for operations with very few samples.
    /// Default is 10 operations.
    /// </summary>
    public int MinOperationsForEvaluation { get; init; } = 10;

    /// <summary>
    /// Gets the time window for evaluating recent performance trends.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan EvaluationWindow { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets whether the thresholds are considered strict (lower tolerance for performance issues).
    /// </summary>
    public bool IsStrict => MaxAverageTime <= TimeSpan.FromMilliseconds(5) && 
                           MaxOperationTime <= TimeSpan.FromMilliseconds(50) && 
                           MaxFailureRate <= 0.01;

    /// <summary>
    /// Gets whether the thresholds are considered lenient (higher tolerance for performance issues).
    /// </summary>
    public bool IsLenient => MaxAverageTime >= TimeSpan.FromMilliseconds(50) && 
                            MaxOperationTime >= TimeSpan.FromMilliseconds(500) && 
                            MaxFailureRate >= 0.10;

    /// <summary>
    /// Gets predefined strict thresholds for high-performance requirements.
    /// </summary>
    public static PerformanceThresholds Strict => new()
    {
        MaxAverageTime = TimeSpan.FromMilliseconds(5),
        MaxOperationTime = TimeSpan.FromMilliseconds(25),
        MaxFailureRate = 0.01, // 1%
        MinOperationsForEvaluation = 20,
        EvaluationWindow = TimeSpan.FromMinutes(2)
    };

    /// <summary>
    /// Gets predefined moderate thresholds for balanced performance monitoring.
    /// </summary>
    public static PerformanceThresholds Moderate => new()
    {
        MaxAverageTime = TimeSpan.FromMilliseconds(10),
        MaxOperationTime = TimeSpan.FromMilliseconds(100),
        MaxFailureRate = 0.05, // 5%
        MinOperationsForEvaluation = 10,
        EvaluationWindow = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// Gets predefined lenient thresholds for development or low-performance environments.
    /// </summary>
    public static PerformanceThresholds Lenient => new()
    {
        MaxAverageTime = TimeSpan.FromMilliseconds(50),
        MaxOperationTime = TimeSpan.FromMilliseconds(500),
        MaxFailureRate = 0.10, // 10%
        MinOperationsForEvaluation = 5,
        EvaluationWindow = TimeSpan.FromMinutes(10)
    };

    /// <summary>
    /// Gets predefined thresholds optimized for development environments.
    /// </summary>
    public static PerformanceThresholds Development => new()
    {
        MaxAverageTime = TimeSpan.FromMilliseconds(25),
        MaxOperationTime = TimeSpan.FromMilliseconds(250),
        MaxFailureRate = 0.15, // 15% - higher tolerance for debugging
        MinOperationsForEvaluation = 3,
        EvaluationWindow = TimeSpan.FromMinutes(15)
    };

    /// <summary>
    /// Gets predefined thresholds optimized for production environments.
    /// </summary>
    public static PerformanceThresholds Production => new()
    {
        MaxAverageTime = TimeSpan.FromMilliseconds(8),
        MaxOperationTime = TimeSpan.FromMilliseconds(75),
        MaxFailureRate = 0.02, // 2% - strict for production
        MinOperationsForEvaluation = 25,
        EvaluationWindow = TimeSpan.FromMinutes(3)
    };

    /// <summary>
    /// Determines if an operation meets the performance thresholds.
    /// </summary>
    /// <param name="metrics">The operation metrics to evaluate.</param>
    /// <returns>True if the operation meets all thresholds; otherwise, false.</returns>
    public bool MeetsThresholds(OperationMetrics metrics)
    {
        if (metrics.TotalOperations < MinOperationsForEvaluation)
            return true; // Not enough data to evaluate

        return metrics.AverageTime <= MaxAverageTime &&
               metrics.MaxTime <= MaxOperationTime &&
               metrics.FailureRate <= MaxFailureRate;
    }

    /// <summary>
    /// Gets a formatted summary of the threshold configuration.
    /// </summary>
    /// <returns>A formatted string with threshold information.</returns>
    public string GetFormattedSummary()
    {
        var strictness = IsStrict ? "Strict" : IsLenient ? "Lenient" : "Moderate";
        
        return $"Performance Thresholds ({strictness}):\n" +
               $"  Max Average Time: {MaxAverageTime.TotalMilliseconds:F1}ms\n" +
               $"  Max Operation Time: {MaxOperationTime.TotalMilliseconds:F1}ms\n" +
               $"  Max Failure Rate: {MaxFailureRate:P2}\n" +
               $"  Min Operations for Evaluation: {MinOperationsForEvaluation}\n" +
               $"  Evaluation Window: {EvaluationWindow}";
    }
}
