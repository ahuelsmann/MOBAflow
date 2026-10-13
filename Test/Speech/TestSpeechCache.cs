// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Speech;

using Moba.Speech.Base;
using Moq;

internal static class TestSpeechCache
{
    internal static IAudioCache Empty()
    {
        var cache = new Mock<IAudioCache>();
        cache.Setup(value => value.TryGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AudioCacheLookup((ReadOnlyMemory<byte>?)null, 0));
        return cache.Object;
    }
}
