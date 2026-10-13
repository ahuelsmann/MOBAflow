// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Speech.Base;

using System.Buffers.Binary;

/// <summary>RIFF PCM metadata used for playback completion and local volume control.</summary>
public static class WaveAudio
{
    /// <summary>Returns the duration of a valid PCM WAV, including files with ancillary chunks.</summary>
    public static TimeSpan GetDuration(ReadOnlySpan<byte> audio)
    {
        var info = Read(audio);
        return TimeSpan.FromSeconds((double)info.DataLength / info.BytesPerSecond);
    }

    /// <summary>Scales 16-bit PCM samples in place without changing RIFF metadata.</summary>
    public static void ApplyVolume(Span<byte> audio, uint volume)
    {
        var info = Read(audio);
        if (info.BitsPerSample != 16)
        {
            throw new InvalidDataException("Speech audio must use 16-bit PCM for volume control.");
        }

        var gain = Math.Clamp(volume, 0, 100) / 100.0;
        for (var offset = info.DataOffset; offset < info.DataOffset + info.DataLength; offset += sizeof(short))
        {
            var sample = BinaryPrimitives.ReadInt16LittleEndian(audio.Slice(offset, sizeof(short)));
            BinaryPrimitives.WriteInt16LittleEndian(audio.Slice(offset, sizeof(short)), (short)Math.Round(sample * gain));
        }
    }

    private static WaveInfo Read(ReadOnlySpan<byte> audio)
    {
        if (audio.Length < 12 || !audio[..4].SequenceEqual("RIFF"u8) || !audio.Slice(8, 4).SequenceEqual("WAVE"u8))
        {
            throw new InvalidDataException("Invalid speech WAV header.");
        }

        var bytesPerSecond = 0;
        var blockAlign = 0;
        var bitsPerSample = 0;
        for (var offset = 12; offset <= audio.Length - 8;)
        {
            var length = BinaryPrimitives.ReadUInt32LittleEndian(audio.Slice(offset + 4, 4));
            var dataOffset = offset + 8;
            if (length > (uint)(audio.Length - dataOffset))
            {
                throw new InvalidDataException("Invalid speech WAV chunk.");
            }

            var chunk = audio.Slice(offset, 4);
            if (chunk.SequenceEqual("fmt "u8) && length >= 16)
            {
                (bytesPerSecond, blockAlign, bitsPerSample) = ReadFormat(audio.Slice(dataOffset, 16));
            }
            else if (chunk.SequenceEqual("data"u8) && bytesPerSecond > 0 && length % blockAlign == 0)
            {
                return new WaveInfo(dataOffset, (int)length, bytesPerSecond, bitsPerSample);
            }

            var next = (long)dataOffset + length + (length & 1);
            if (next > audio.Length) break;
            offset = (int)next;
        }

        throw new InvalidDataException("Speech WAV contains no PCM audio.");
    }

    private static (int BytesPerSecond, int BlockAlign, int BitsPerSample) ReadFormat(ReadOnlySpan<byte> audio)
    {
        var format = BinaryPrimitives.ReadUInt16LittleEndian(audio[..2]);
        var bytesPerSecond = BinaryPrimitives.ReadInt32LittleEndian(audio.Slice(8, 4));
        var blockAlign = BinaryPrimitives.ReadUInt16LittleEndian(audio.Slice(12, 2));
        var bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(audio.Slice(14, 2));
        if (format != 1 || bytesPerSecond <= 0 || blockAlign <= 0)
        {
            throw new InvalidDataException("Unsupported speech WAV format.");
        }
        return (bytesPerSecond, blockAlign, bitsPerSample);
    }

    private readonly record struct WaveInfo(int DataOffset, int DataLength, int BytesPerSecond, int BitsPerSample);
}
