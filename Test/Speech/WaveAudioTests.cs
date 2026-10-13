// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Speech;

using Moba.Speech.Base;
using System.Buffers.Binary;

[TestFixture]
internal sealed class WaveAudioTests
{
    [Test]
    public void Volume_ScalesPositiveAndNegativeSamplesWithoutChangingDuration()
    {
        var audio = TestWaveAudio.Create([12000, -12000, 0], 1000);
        WaveAudio.ApplyVolume(audio, 25);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(BinaryPrimitives.ReadInt16LittleEndian(audio.AsSpan(44, 2)), Is.EqualTo(3000));
            Assert.That(BinaryPrimitives.ReadInt16LittleEndian(audio.AsSpan(46, 2)), Is.EqualTo(-3000));
            Assert.That(WaveAudio.GetDuration(audio), Is.EqualTo(TimeSpan.FromMilliseconds(3)));
        }
    }

    [Test]
    public void InvalidChunkLength_IsRejected()
    {
        var audio = TestWaveAudio.Create([1], 1000);
        BinaryPrimitives.WriteUInt32LittleEndian(audio.AsSpan(40, 4), uint.MaxValue);
        Assert.Throws<InvalidDataException>(() => WaveAudio.GetDuration(audio));
    }

    [Test]
    public void Duration_HandlesAncillaryChunksWithOddLengthPadding()
    {
        var original = TestWaveAudio.Create([1000, -1000], 1000);
        var audio = new byte[original.Length + 10];
        original.AsSpan(0, 36).CopyTo(audio);
        "JUNK"u8.CopyTo(audio.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(40, 4), 1);
        audio[44] = 42;
        original.AsSpan(36).CopyTo(audio.AsSpan(46));
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(4, 4), audio.Length - 8);
        Assert.That(WaveAudio.GetDuration(audio), Is.EqualTo(TimeSpan.FromMilliseconds(2)));
    }
}

internal static class TestWaveAudio
{
    internal static byte[] Create(short[] samples, int sampleRate = 1000)
    {
        var audio = new byte[44 + (samples.Length * sizeof(short))];
        "RIFF"u8.CopyTo(audio);
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(4, 4), audio.Length - 8);
        "WAVEfmt "u8.CopyTo(audio.AsSpan(8));
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(16, 4), 16);
        BinaryPrimitives.WriteInt16LittleEndian(audio.AsSpan(20, 2), 1);
        BinaryPrimitives.WriteInt16LittleEndian(audio.AsSpan(22, 2), 1);
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(24, 4), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(28, 4), sampleRate * sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(audio.AsSpan(32, 2), sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(audio.AsSpan(34, 2), 16);
        "data"u8.CopyTo(audio.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(40, 4), samples.Length * sizeof(short));
        for (var index = 0; index < samples.Length; index++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(audio.AsSpan(44 + (index * sizeof(short)), sizeof(short)), samples[index]);
        }
        return audio;
    }
}
