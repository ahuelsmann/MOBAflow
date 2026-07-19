// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

[TestFixture]
internal sealed class TrackLayoutTests
{
    [Test]
    public void AddTrack_Should_AddTrackAndMakeItAvailableById()
    {
        // Arrange
        var layout = new Layout();
        var track = CreateTrack();

        // Act
        layout.AddTrack(track);
        bool found = layout.TryGetTrack(track.Id, out var resolvedTrack);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(resolvedTrack, Is.EqualTo(track));
            Assert.That(layout.Tracks, Is.EquivalentTo([track]));
        });
    }

    [Test]
    public void AddTrack_Should_RejectNullAndDuplicateTracks()
    {
        // Arrange
        var layout = new Layout();
        var track = CreateTrack();
        layout.AddTrack(track);

        // Act and assert
        Assert.Multiple(() =>
        {
            Assert.That(() => layout.AddTrack(null!), Throws.ArgumentNullException);
            Assert.That(
                () => layout.AddTrack(track),
                Throws.InvalidOperationException.With.Message.Contains(track.Id.ToString()));
        });
    }

    [Test]
    public void RemoveTrack_Should_RemoveTrackAndAllConnectedEdges()
    {
        // Arrange
        var layout = new Layout();
        var first = CreateTrack();
        var second = CreateTrack();
        var third = CreateTrack();
        layout.AddTrack(first);
        layout.AddTrack(second);
        layout.AddTrack(third);
        layout.Connect(new Connection(first.Id, "A", second.Id, "B"));
        layout.Connect(new Connection(third.Id, "A", first.Id, "B"));

        // Act
        bool removed = layout.RemoveTrack(first.Id);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(layout.TryGetTrack(first.Id, out _), Is.False);
            Assert.That(layout.Tracks, Is.EquivalentTo([second, third]));
            Assert.That(layout.Connections, Is.Empty);
        });
    }

    [Test]
    public void RemoveTrack_Should_ReturnFalseForUnknownTrack()
    {
        var layout = new Layout();

        bool removed = layout.RemoveTrack(Guid.NewGuid());

        Assert.That(removed, Is.False);
    }

    [Test]
    public void ReplaceTrack_Should_ReplaceExistingTrackOnly()
    {
        // Arrange
        var layout = new Layout();
        var original = CreateTrack();
        var replacement = original with { X = 42, TemplateId = "Replacement" };
        layout.AddTrack(original);

        // Act
        layout.ReplaceTrack(replacement);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(layout.TryGetTrack(original.Id, out var resolved), Is.True);
            Assert.That(resolved, Is.EqualTo(replacement));
            Assert.That(() => layout.ReplaceTrack(null!), Throws.ArgumentNullException);
            Assert.That(
                () => layout.ReplaceTrack(CreateTrack()),
                Throws.InstanceOf<KeyNotFoundException>());
        });
    }

    [Test]
    public void Connect_Should_RejectNullAndMissingTracks()
    {
        // Arrange
        var layout = new Layout();
        var existing = CreateTrack();
        layout.AddTrack(existing);

        // Act and assert
        Assert.Multiple(() =>
        {
            Assert.That(() => layout.Connect(null!), Throws.ArgumentNullException);
            Assert.That(
                () => layout.Connect(new Connection(existing.Id, "A", Guid.NewGuid(), "B")),
                Throws.InvalidOperationException);
            Assert.That(
                () => layout.Connect(new Connection(Guid.NewGuid(), "A", existing.Id, "B")),
                Throws.InvalidOperationException);
        });
    }

    [Test]
    public void Connect_Should_ReplaceConnectionsAlreadyUsingEitherConnector()
    {
        // Arrange
        var layout = new Layout();
        var first = CreateTrack();
        var second = CreateTrack();
        var third = CreateTrack();
        var fourth = CreateTrack();
        foreach (var track in new[] { first, second, third, fourth })
            layout.AddTrack(track);
        layout.Connect(new Connection(first.Id, "A", second.Id, "A"));
        layout.Connect(new Connection(third.Id, "A", fourth.Id, "A"));

        // Act
        var replacement = new Connection(first.Id, "A", fourth.Id, "A");
        layout.Connect(replacement);

        // Assert
        Assert.That(layout.Connections, Is.EqualTo([replacement]));
    }

    [Test]
    public void DisconnectConnector_Should_RemoveSourceAndTargetConnections()
    {
        // Arrange
        var layout = new Layout();
        var first = CreateTrack();
        var second = CreateTrack();
        var third = CreateTrack();
        layout.AddTrack(first);
        layout.AddTrack(second);
        layout.AddTrack(third);
        layout.Connect(new Connection(first.Id, "A", second.Id, "A"));
        layout.Connect(new Connection(second.Id, "B", third.Id, "A"));

        // Act
        layout.DisconnectConnector(second.Id, "A");
        layout.DisconnectConnector(second.Id, "B");

        // Assert
        Assert.That(layout.Connections, Is.Empty);
    }

    [Test]
    public void TrackLibraryRegistry_Should_ResolveCaseInsensitiveLibraryAndTemplate()
    {
        // Arrange
        var definition = new TrackDefinition("piko-a", "G231", "Straight", "Straight", []);
        var library = new StubTrackLibrary("piko-a", [definition]);
        var registry = new TrackLibraryRegistry([library]);

        // Act
        bool found = registry.TryGetLibrary("PIKO-A", out var resolvedLibrary);
        var resolvedDefinition = registry.ResolveDefinition("PIKO-A", "G231");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(resolvedLibrary, Is.SameAs(library));
            Assert.That(resolvedDefinition, Is.EqualTo(definition));
            Assert.That(registry.Libraries, Is.EquivalentTo([library]));
        });
    }

    [Test]
    public void TrackLibraryRegistry_Should_RejectInvalidRegistryAndLookups()
    {
        // Arrange
        var library = new StubTrackLibrary("piko-a", []);

        // Act and assert
        Assert.Multiple(() =>
        {
            Assert.That(() => new TrackLibraryRegistry(null!), Throws.ArgumentNullException);
            Assert.That(
                () => new TrackLibraryRegistry([library, library]),
                Throws.InstanceOf<ArgumentException>());

            var registry = new TrackLibraryRegistry([library]);
            Assert.That(
                () => registry.ResolveDefinition("missing", "G231"),
                Throws.InvalidOperationException.With.Message.Contains("missing"));
            Assert.That(
                () => registry.ResolveDefinition("piko-a", "missing"),
                Throws.InvalidOperationException.With.Message.Contains("missing"));
        });
    }

    [Test]
    public void RailroadState_Should_StoreAndClearOccupancyAndFeedback()
    {
        // Arrange
        var state = new RailroadState();
        var trackId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        // Act and assert
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.False);
            Assert.That(state.GetLastFeedback(trackId), Is.Null);
        });

        state.SetOccupied(trackId, true);
        Assert.That(state.IsOccupied(trackId), Is.True);

        state.MarkFeedback(trackId, timestamp);
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.True);
            Assert.That(state.GetLastFeedback(trackId), Is.EqualTo(timestamp));
        });

        state.ClearFeedback(trackId);
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.False);
            Assert.That(state.GetLastFeedback(trackId), Is.Null);
        });
    }

    [Test]
    public void ExpireFeedback_Should_ExpireAtTimeoutBoundaryOnly()
    {
        // Arrange
        var state = new RailroadState();
        var trackId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        state.MarkFeedback(trackId, timestamp);

        // Act
        state.ExpireFeedback(timestamp.AddMilliseconds(999), TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(state.IsOccupied(trackId), Is.True);

        state.ExpireFeedback(timestamp.AddSeconds(1), TimeSpan.FromSeconds(1));
        Assert.Multiple(() =>
        {
            Assert.That(state.IsOccupied(trackId), Is.False);
            Assert.That(state.GetLastFeedback(trackId), Is.Null);
            Assert.That(
                () => state.ExpireFeedback(timestamp, TimeSpan.FromMilliseconds(-1)),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void RailroadState_Should_StoreSwitchAndValidatedSignalState()
    {
        // Arrange
        var state = new RailroadState();

        // Act and assert
        Assert.Multiple(() =>
        {
            Assert.That(state.GetSwitchPosition(4), Is.Null);
            Assert.That(state.GetSignalAspect(5), Is.Null);
        });

        state.SetSwitchPosition(4, isLeft: false);
        state.SetSignalAspect(5, "Stop");

        Assert.Multiple(() =>
        {
            Assert.That(state.GetSwitchPosition(4), Is.False);
            Assert.That(state.GetSignalAspect(5), Is.EqualTo("Stop"));
            Assert.That(() => state.SetSignalAspect(5, ""), Throws.ArgumentException);
            Assert.That(() => state.SetSignalAspect(5, " "), Throws.ArgumentException);
            Assert.That(() => state.SetSignalAspect(5, null!), Throws.ArgumentNullException);
        });
    }

    private static TrackInstance CreateTrack()
        => new(Guid.NewGuid(), "piko-a", "G231", 0, 0, 0);

    private sealed class StubTrackLibrary(string libraryId, IReadOnlyList<TrackDefinition> definitions) : ITrackLibrary
    {
        public string LibraryId { get; } = libraryId;

        public string DisplayName => LibraryId;

        public IReadOnlyList<TrackDefinition> Definitions { get; } = definitions;

        public bool TryGetDefinition(string templateId, out TrackDefinition definition)
        {
            definition = Definitions.FirstOrDefault(
                candidate => string.Equals(candidate.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))!;
            return definition is not null;
        }
    }
}
