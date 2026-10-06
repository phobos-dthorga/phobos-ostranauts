using System;
using System.IO;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>One decoded sound: mono samples (-1 to 1) and their rate.</summary>
public readonly struct PcmClip
{
    public PcmClip(float[] samples, int sampleRate) { Samples = samples; SampleRate = sampleRate; }
    public float[] Samples { get; }
    public int SampleRate { get; }
    public double Seconds => SampleRate > 0 ? Samples.Length / (double)SampleRate : 0;
}

/// <summary>Reads uncompressed 16-bit PCM WAV, the format every audio editor exports (Framework 0.120.0; the shipped
/// sounds and a player's replacements alike). Mono or stereo, 8 to 96 kHz; stereo is mixed to mono, since every sound
/// we play is mono. Chunks other than the format and the samples (editors add LIST and similar notes) are skipped,
/// but every chunk must be whole and named in plain characters, and the file must end where its header says. Each
/// caller names its own length bound in seconds. Anything else is refused with InvalidDataException.</summary>
internal static class PcmWav
{
    /// <summary>The rate of our own exports.</summary>
    internal const int SampleRate = 44100;
    internal const int MinSampleRate = 8000, MaxSampleRate = 96000;
    /// <summary>Room for an editor's notes beside the samples.</summary>
    private const int MetadataBytes = 65536;

    /// <summary>The largest file worth opening for a sound of at most this length.</summary>
    internal static long MaxFileBytes(double maxSeconds) => 44 + MetadataBytes + (long)Math.Ceiling(maxSeconds * MaxSampleRate) * 4;

    internal static PcmClip Read(Stream stream, double maxSeconds, string what)
    {
        try { return ReadChecked(stream, maxSeconds, what); }
        catch (EndOfStreamException) { throw new InvalidDataException("The " + what + " file ends early."); }
    }

    private static PcmClip ReadChecked(Stream stream, double maxSeconds, string what)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        if (Id(reader) != "RIFF" || reader.ReadInt32() != stream.Length - 8 || Id(reader) != "WAVE")
            throw new InvalidDataException("The " + what + " file is not a WAV file.");
        int channels = 0, rate = 0;
        float[]? samples = null;
        while (stream.Position < stream.Length)
        {
            string id = Id(reader);
            int size = reader.ReadInt32();
            if (size < 0 || size > stream.Length - stream.Position) throw new InvalidDataException("The " + what + " file has a damaged section.");
            long next = stream.Position + size + (size & 1);
            if (id == "fmt ")
            {
                if (channels != 0 || size < 16) throw new InvalidDataException("The " + what + " file has a damaged format section.");
                short format = reader.ReadInt16();
                channels = reader.ReadInt16();
                rate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                short blockAlign = reader.ReadInt16(), bits = reader.ReadInt16();
                if (format != 1 || bits != 16 || channels < 1 || channels > 2)
                    throw new InvalidDataException("The " + what + " file must be uncompressed 16-bit PCM, mono or stereo.");
                if (rate < MinSampleRate || rate > MaxSampleRate || byteRate != rate * channels * 2 || blockAlign != channels * 2)
                    throw new InvalidDataException("The " + what + " file has an unsupported sample rate or a damaged format section.");
            }
            else if (id == "data")
            {
                if (channels == 0 || samples != null) throw new InvalidDataException("The " + what + " file has its samples before its format.");
                int frameBytes = channels * 2;
                if (size == 0 || size % frameBytes != 0) throw new InvalidDataException("The " + what + " file has an invalid length.");
                int frames = size / frameBytes;
                if (frames > maxSeconds * rate) throw new InvalidDataException("The " + what + " file is longer than " + maxSeconds + " seconds.");
                samples = new float[frames];
                for (int i = 0; i < frames; i++)
                {
                    int sum = 0;
                    for (int c = 0; c < channels; c++) sum += reader.ReadInt16();
                    samples[i] = sum / (32768f * channels);
                }
            }
            if (next > stream.Length) { if (next - 1 != stream.Length) throw new InvalidDataException("The " + what + " file has a damaged section."); next = stream.Length; }
            stream.Position = next;
        }
        if (samples == null) throw new InvalidDataException("The " + what + " file holds no samples.");
        return new PcmClip(samples, rate);
    }

    /// <summary>A four-character section name: plain printable characters only, so stray sample bytes never pass.</summary>
    private static string Id(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        if (bytes.Length < 4) throw new EndOfStreamException();
        foreach (byte b in bytes) if (b < 0x20 || b > 0x7e) throw new InvalidDataException("The file has a damaged section name.");
        return System.Text.Encoding.ASCII.GetString(bytes);
    }
}
