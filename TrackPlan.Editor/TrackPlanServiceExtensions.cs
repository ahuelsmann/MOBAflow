// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor;

using Microsoft.Extensions.DependencyInjection;

using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Editor.Service;
using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.Renderer.Geometry;
using Moba.TrackPlan.Renderer.Layout;

/// <summary>
/// Extension methods for registering TrackPlan services with DI container.
/// </summary>
public static class TrackPlanServiceExtensions
{
    /// <summary>
    /// Adds all TrackPlan services (Domain, Renderer, Editor) to the service collection.
    /// </summary>
    public static IServiceCollection AddTrackPlanServices(this IServiceCollection services)
    {
        // Renderer services
        services.AddSingleton<TrackGeometryRenderer>();
        services.AddSingleton<ILayoutEngine, CircularLayoutEngine>();

        // Editor services
        services.AddSingleton<ValidationService>();
        services.AddSingleton<SerializationService>();

        // Constraints
        services.AddSingleton<ITopologyConstraint, DuplicateFeedbackPointNumberConstraint>();

        // ViewModel (transient - one per editor instance)
        services.AddTransient<TrackPlanEditorViewModel2>();

        return services;
    }
}
