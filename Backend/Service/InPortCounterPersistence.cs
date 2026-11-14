// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Backend.Service;

using Interface;

/// <summary>
/// Where an <see cref="InPortCounterService"/> keeps its counts. The host states the choice; the counter service
/// never decides on its own that counts are not persisted.
/// </summary>
public sealed class InPortCounterPersistence
{
    private InPortCounterPersistence(IInPortCounterStore? store, Func<Guid, IInPortCounterStore>? projectStoreFactory)
    {
        Store = store;
        ProjectStoreFactory = projectStoreFactory;
    }

    /// <summary>Counts live only as long as the counter service; nothing is loaded or saved.</summary>
    public static InPortCounterPersistence InMemory { get; } = new(null, null);

    /// <summary>The store of the counter service, loaded on initialization.</summary>
    internal IInPortCounterStore? Store { get; }

    /// <summary>Creates the store of the project selected on the counter service.</summary>
    internal Func<Guid, IInPortCounterStore>? ProjectStoreFactory { get; }

    /// <summary>Counts are loaded from and saved to one store (one counter service per project runtime).</summary>
    public static InPortCounterPersistence InStore(IInPortCounterStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return new InPortCounterPersistence(store, null);
    }

    /// <summary>Each selected project gets its own store (one counter service for all projects, MOBAsmart).</summary>
    public static InPortCounterPersistence PerProject(Func<Guid, IInPortCounterStore> projectStoreFactory)
    {
        ArgumentNullException.ThrowIfNull(projectStoreFactory);
        return new InPortCounterPersistence(null, projectStoreFactory);
    }
}
