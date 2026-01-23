// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor;

using Microsoft.Extensions.DependencyInjection;

using Moba.TrackLibrary.PikoA.Catalog;
using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Editor.Service;
using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.Renderer.Layout;
using Moba.TrackPlan.Renderer.Rendering;
using Moba.TrackPlan.Renderer.Service;
using Moba.TrackPlan.TrackSystem;

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
        // Track Catalog (Geometry Library)
        services.AddSingleton<ITrackCatalog, PikoATrackCatalog>();

        // Layout Engines - multiple implementations available
        // Default: CircularLayoutEngine (simple visualization)
        // Alternative: SimpleLayoutEngine (geometry-based positioning)
        services.AddSingleton<ILayoutEngine, CircularLayoutEngine>();
        services.AddKeyedSingleton<ILayoutEngine, CircularLayoutEngine>("Circular");
        services.AddKeyedSingleton<ILayoutEngine, SimpleLayoutEngine>("Simple");

        // Renderer services
        services.AddSingleton<TrackPlanLayoutEngine>();
        services.AddSingleton<SkiaSharpCanvasRenderer>();

        // Editor services
        services.AddSingleton<ValidationService>();
        services.AddSingleton<SerializationService>();

        // Constraints
        services.AddSingleton<ITopologyConstraint, DuplicateFeedbackPointNumberConstraint>();
        services.AddSingleton<ITopologyConstraint, GeometryConnectionConstraint>();

        // ViewModel (transient - one per editor instance)
        services.AddTransient<TrackPlanEditorViewModel>();

        return services;
    }
}
