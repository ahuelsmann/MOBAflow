// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Sound;

using Moba.Sound;

[TestFixture]
internal sealed class PcmWavePostProcessorTests
{
    private string _temporaryDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"mobaflow-wave-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Test]
    public void Process_Should_TrimAndFade_WhenPcmWaveHasLongLowLevelTail()
    {
        // Arrange
        var path = Path.Combine(_temporaryDirectory, "noisy-tail.wav");
        var samples = Enumerable.Repeat((short)2_000, 160).Concat(Enumerable.Repeat((short)100, 1_600)).ToArray();
        WritePcmWave(path, samples);

        // Act
        var result = PcmWavePostProcessor.Process(path);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.WasProcessed, Is.True);
            Assert.That(result.TrimmedFrameCount, Is.GreaterThan(0));
            Assert.That(ReadSamples(path).Last(), Is.Zero);
        });
    }

    [Test]
    public void Process_Should_LeavePureSilenceUnchanged()
    {
        // Arrange
        var path = Path.Combine(_temporaryDirectory, "silence.wav");
        WritePcmWave(path, Enumerable.Repeat((short)0, 1_600).ToArray());
        var originalLength = new FileInfo(path).Length;

        // Act
        var result = PcmWavePostProcessor.Process(path);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.WasProcessed, Is.False);
            Assert.That(new FileInfo(path).Length, Is.EqualTo(originalLength));
        });
    }

    [Test]
    public void Process_Should_LeaveSpeechAboveNoiseThresholdUnchanged()
    {
        // Arrange
        var path = Path.Combine(_temporaryDirectory, "speech.wav");
        WritePcmWave(path, Enumerable.Repeat((short)2_000, 1_600).ToArray());
        var originalLength = new FileInfo(path).Length;

        // Act
        var result = PcmWavePostProcessor.Process(path);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.WasProcessed, Is.False);
            Assert.That(new FileInfo(path).Length, Is.EqualTo(originalLength));
        });
    }

    [Test]
    public void Process_Should_LeaveUnsupportedWaveUnchanged()
    {
        // Arrange
        var path = Path.Combine(_temporaryDirectory, "invalid.wav");
        File.WriteAllText(path, "not a wave file");

        // Act
        var result = PcmWavePostProcessor.Process(path);

        // Assert
        Assert.That(result, Is.EqualTo(PcmWavePostProcessResult.Unsupported));
    }

    private static void WritePcmWave(string path, short[] samples)
    {
        const int sampleRate = 16_000;
        const short channels = 1;
        const short bitsPerSample = 16;
        const short blockAlign = channels * (bitsPerSample / 8);
        var dataLength = samples.Length * blockAlign;

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * blockAlign);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data".ToCharArray());
        writer.Write(dataLength);
        foreach (var sample in samples)
        {
            writer.Write(sample);
        }
    }

    private static short[] ReadSamples(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        stream.Position = 40;
        var dataLength = reader.ReadInt32();
        return Enumerable.Range(0, dataLength / sizeof(short)).Select(_ => reader.ReadInt16()).ToArray();
    }
}
