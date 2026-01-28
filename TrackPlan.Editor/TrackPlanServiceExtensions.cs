// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor;

using Microsoft.Extensions.DependencyInjection;

using Moba.TrackLibrary.Base.TrackSystem;
using Moba.TrackPlan.Constraint;
using Moba.TrackPlan.Editor.Service;
using Moba.TrackPlan.Editor.ViewModel;
using Moba.TrackPlan.Renderer.Layout;
using Moba.TrackPlan.Renderer.Rendering;
using Moba.TrackPlan.Renderer.Service;

/// <summary>
/// Extension methods for registering TrackPlan services with DI container.
/// </summary>
public static class TrackPlanServiceExtensions
{
    /// <summary>
    /// Adds all TrackPlan services (Domain, Renderer, Editor) to the service collection.
    /// 
    /// NOTE: ITrackCatalog must be registered separately by the application layer 
    /// (e.g., WinUI, Blazor) to allow different track systems (PikoA, Märklin, etc.).
    /// 
    /// Example in App layer:
    /// services.AddSingleton&lt;ITrackCatalog, PikoATrackCatalog&gt;();
    /// </summary>
    public static IServiceCollection AddTrackPlanServices(this IServiceCollection services)
    {
        // NOTE: ITrackCatalog registration removed - must be done in app layer
        // to support multiple track systems (PikoA, Märklin, Fleischmann, etc.)

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

        // Register array of all ITopologyConstraint implementations
        services.AddSingleton(sp => sp.GetRequiredService<IEnumerable<ITopologyConstraint>>().ToArray());

        // ViewModel (transient - one per editor instance)
        services.AddTransient<TrackPlanEditorViewModel>();

        return services;
    }
}
