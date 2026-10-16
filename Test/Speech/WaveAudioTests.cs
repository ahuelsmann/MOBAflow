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

    [TestCase(0u, 0)]
    [TestCase(100u, 12000)]
    [TestCase(101u, 12000)]
    [TestCase(uint.MaxValue, 12000)]
    public void Volume_ClampsWithoutAmplifyingOrChangingTheHeader(uint volume, short expected)
    {
        var audio = TestWaveAudio.Create([12000, -12000]);
        var header = audio.AsSpan(0, 44).ToArray();
        WaveAudio.ApplyVolume(audio, volume);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(BinaryPrimitives.ReadInt16LittleEndian(audio.AsSpan(44, 2)), Is.EqualTo(expected));
            Assert.That(BinaryPrimitives.ReadInt16LittleEndian(audio.AsSpan(46, 2)), Is.EqualTo(-expected));
            Assert.That(audio.AsSpan(0, 44).ToArray(), Is.EqualTo(header));
        }
    }

    [TestCase(0)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(36)]
    public void TruncatedAudio_IsRejected(int length)
    {
        var audio = TestWaveAudio.Create([1])[..length];
        Assert.Throws<InvalidDataException>(() => WaveAudio.GetDuration(audio));
    }

    [TestCase(0)]
    [TestCase(8)]
    public void InvalidContainerSignature_IsRejected(int offset)
    {
        var audio = TestWaveAudio.Create([1]);
        audio[offset] = 0;
        Assert.Throws<InvalidDataException>(() => WaveAudio.GetDuration(audio));
    }

    [TestCase(20, 3)]
    [TestCase(28, 0)]
    [TestCase(28, -1)]
    [TestCase(32, 0)]
    public void UnsupportedPcmFormat_IsRejected(int offset, int value)
    {
        var audio = TestWaveAudio.Create([1]);
        if (offset == 28) BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(offset, 4), value);
        else BinaryPrimitives.WriteInt16LittleEndian(audio.AsSpan(offset, 2), (short)value);
        Assert.Throws<InvalidDataException>(() => WaveAudio.GetDuration(audio));
    }

    [Test]
    public void PartialSample_IsRejected()
    {
        var audio = TestWaveAudio.Create([1]);
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(40, 4), 1);
        Assert.Throws<InvalidDataException>(() => WaveAudio.GetDuration(audio));
    }

    [Test]
    public void EightBitPcm_HasDurationButCannotBeModifiedAsSixteenBitSamples()
    {
        var audio = TestWaveAudio.Create([1]);
        BinaryPrimitives.WriteInt32LittleEndian(audio.AsSpan(28, 4), 1000);
        BinaryPrimitives.WriteInt16LittleEndian(audio.AsSpan(32, 2), 1);
        BinaryPrimitives.WriteInt16LittleEndian(audio.AsSpan(34, 2), 8);
        Assert.That(WaveAudio.GetDuration(audio), Is.EqualTo(TimeSpan.FromMilliseconds(2)));
        var original = audio.ToArray();
        Assert.Throws<InvalidDataException>(() => WaveAudio.ApplyVolume(audio, 50));
        Assert.That(audio, Is.EqualTo(original));
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
