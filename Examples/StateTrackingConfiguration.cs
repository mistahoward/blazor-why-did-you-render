using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Blazor.WhyDidYouRender.Configuration;
using Blazor.WhyDidYouRender.Extensions;

namespace Blazor.WhyDidYouRender.Examples;

/// <summary>
/// Examples of how to configure state tracking in different scenarios.
/// </summary>
public static class StateTrackingConfiguration {
    /// <summary>
    /// Basic state tracking configuration for development.
    /// </summary>
    public static void ConfigureBasicStateTracking(IServiceCollection services) {
        services.AddWhyDidYouRender(config => {
            // Enable state tracking with default settings
            config.EnableStateTracking = true;
            config.AutoTrackSimpleTypes = true;
            config.LogStateChanges = true;

            // Basic configuration
            config.Enabled = true;
            config.Verbosity = TrackingVerbosity.Normal;
            config.Output = TrackingOutput.Both;
            config.TrackParameterChanges = true;
            config.DetectUnnecessaryRerenders = true;
        });
    }

    /// <summary>
    /// Advanced state tracking configuration with performance optimizations.
    /// </summary>
    public static void ConfigureAdvancedStateTracking(IServiceCollection services) {
        services.AddWhyDidYouRender(config => {
            // State tracking configuration
            config.EnableStateTracking = true;
            config.AutoTrackSimpleTypes = true;
            config.MaxTrackedFieldsPerComponent = 25;
            config.LogStateChanges = true;
            config.LogDetailedStateChanges = false;
            config.TrackInheritedFields = true;

            config.MaxStateComparisonDepth = 2;
            config.EnableCollectionContentTracking = false;
            config.MaxTrackedComponents = 500;

            config.StateSnapshotCleanupIntervalMinutes = 5;
            config.MaxStateSnapshotAgeMinutes = 15;

            // Component filtering for state tracking
            config.ExcludeFromStateTracking = new List<string>
            {
                "Microsoft.*",
                "System.*",
                "*Layout*",
                "*Router*"
            };

            // General configuration
            config.Enabled = true;
            config.Verbosity = TrackingVerbosity.Normal;
            config.Output = TrackingOutput.Both;
            config.TrackParameterChanges = true;
            config.DetectUnnecessaryRerenders = true;
            config.TrackPerformance = true;
        });
    }

    /// <summary>
    /// Production-optimized configuration with minimal state tracking.
    /// </summary>
    public static void ConfigureProductionStateTracking(IServiceCollection services) {
        services.AddWhyDidYouRender(config => {
            config.EnableStateTracking = false;
            config.LogStateChanges = false;
            config.LogDetailedStateChanges = false;

            config.Enabled = false;
            config.TrackParameterChanges = false;
            config.DetectUnnecessaryRerenders = false;
            config.TrackPerformance = false;

            config.EnableSecurityMode = true;
            config.IncludeUserInfo = false;
            config.IncludeClientInfo = false;
            config.IncludeSessionInfo = false;
        });
    }

    /// <summary>
    /// Configuration for debugging specific components.
    /// </summary>
    public static void ConfigureTargetedStateTracking(IServiceCollection services) {
        services.AddWhyDidYouRender(config => {
            // Enable state tracking only for specific components
            config.EnableStateTracking = true;
            config.AutoTrackSimpleTypes = true;
            config.LogStateChanges = true;
            config.LogDetailedStateChanges = true; // Detailed logging for debugging

            // Target specific components for state tracking
            config.IncludeInStateTracking = new List<string>
            {
                "Counter*",
                "UserProfile*",
                "DataGrid*",
                "*Form*"
            };

            // Exclude problematic components
            config.ExcludeFromStateTracking = new List<string>
            {
                "HighFrequency*",
                "*Animation*",
                "*Chart*"
            };

            // Increased limits for debugging
            config.MaxTrackedFieldsPerComponent = 100;
            config.MaxStateComparisonDepth = 5;
            config.EnableCollectionContentTracking = true;

            // General debugging configuration
            config.Enabled = true;
            config.Verbosity = TrackingVerbosity.Verbose;
            config.Output = TrackingOutput.Both;
            config.TrackParameterChanges = true;
            config.DetectUnnecessaryRerenders = true;
            config.HighlightUnnecessaryRerenders = true;
        });
    }

    /// <summary>
    /// Configuration for performance testing and benchmarking.
    /// </summary>
    public static void ConfigurePerformanceTestingStateTracking(IServiceCollection services) {
        services.AddWhyDidYouRender(config => {
            // State tracking with performance focus
            config.EnableStateTracking = true;
            config.AutoTrackSimpleTypes = true;
            config.LogStateChanges = false; // Disable logging to reduce overhead
            config.LogDetailedStateChanges = false;

            // Minimal tracking for performance testing
            config.MaxTrackedFieldsPerComponent = 10;
            config.MaxStateComparisonDepth = 1;
            config.EnableCollectionContentTracking = false;
            config.MaxTrackedComponents = 100;

            // Aggressive cleanup
            config.StateSnapshotCleanupIntervalMinutes = 1;
            config.MaxStateSnapshotAgeMinutes = 5;

            // Performance-focused general settings
            config.Enabled = true;
            config.Verbosity = TrackingVerbosity.Minimal;
            config.Output = TrackingOutput.Console; // Console only for performance
            config.TrackParameterChanges = true;
            config.DetectUnnecessaryRerenders = true;
            config.TrackPerformance = true;
            config.FrequentRerenderThreshold = 10.0; // Higher threshold
        });
    }

    /// <summary>
    /// Configuration from appsettings.json example.
    /// </summary>
    public static void ConfigureFromAppSettings(IServiceCollection services, IConfiguration configuration) {
        services.AddWhyDidYouRender(config => {
            // Bind from configuration
            configuration.GetSection("WhyDidYouRender").Bind(config);

            // Override specific state tracking settings if needed
            var stateTrackingSection = configuration.GetSection("WhyDidYouRender:StateTracking");
            if (stateTrackingSection.Exists()) {
                config.EnableStateTracking = stateTrackingSection.GetValue<bool>("Enabled", true);
                config.AutoTrackSimpleTypes = stateTrackingSection.GetValue<bool>("AutoTrackSimpleTypes", true);
                config.LogStateChanges = stateTrackingSection.GetValue<bool>("LogStateChanges", true);
                config.MaxTrackedFieldsPerComponent = stateTrackingSection.GetValue<int>("MaxFieldsPerComponent", 50);
            }
        });
    }

    /// <summary>
    /// Example appsettings.json configuration for state tracking.
    /// </summary>
    public static string GetExampleAppSettingsJson() {
        return """
        {
          "WhyDidYouRender": {
            "Enabled": true,
            "Verbosity": "Normal",
            "Output": "Both",
            "TrackParameterChanges": true,
            "TrackPerformance": true,
            "DetectUnnecessaryRerenders": true,
            "EnableStateTracking": true,
            "AutoTrackSimpleTypes": true,
            "MaxTrackedFieldsPerComponent": 50,
            "LogStateChanges": true,
            "LogDetailedStateChanges": false,
            "TrackInheritedFields": true,
            "MaxStateComparisonDepth": 3,
            "EnableCollectionContentTracking": false,
            "StateSnapshotCleanupIntervalMinutes": 10,
            "MaxStateSnapshotAgeMinutes": 30,
            "MaxTrackedComponents": 1000,
            "ExcludeFromStateTracking": [
              "Microsoft.*",
              "System.*",
              "*Layout*"
            ],
            "IncludeInStateTracking": [
              "MyApp.*"
            ]
          }
        }
        """;
    }
}

/// <summary>
/// Extension methods for easier configuration.
/// </summary>
public static class StateTrackingConfigurationExtensions {
    /// <summary>
    /// Configures state tracking for development environment.
    /// </summary>
    public static IServiceCollection AddWhyDidYouRenderForDevelopment(this IServiceCollection services) {
        StateTrackingConfiguration.ConfigureAdvancedStateTracking(services);
        return services;
    }

    /// <summary>
    /// Configures state tracking for production environment.
    /// </summary>
    public static IServiceCollection AddWhyDidYouRenderForProduction(this IServiceCollection services) {
        StateTrackingConfiguration.ConfigureProductionStateTracking(services);
        return services;
    }

    /// <summary>
    /// Configures state tracking for debugging specific issues.
    /// </summary>
    public static IServiceCollection AddWhyDidYouRenderForDebugging(this IServiceCollection services) {
        StateTrackingConfiguration.ConfigureTargetedStateTracking(services);
        return services;
    }
}
