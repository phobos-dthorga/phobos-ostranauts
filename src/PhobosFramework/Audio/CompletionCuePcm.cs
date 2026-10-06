using System.IO;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>Reads only our canonical mono 44.1 kHz PCM16 exports, embedded in the plugin; no runtime file or network
/// loader. Each caller names its own length bound: the completion cue at most half a second, the machine loops
/// (Framework 0.119.0) at most ten seconds.</summary>
internal static class CompletionCuePcm
{
    internal const int SampleRate = 44100;
    /// <summary>The completion cue: at most half a second.</summary>
    internal static float[] Read(Stream stream) => Read(stream, SampleRate / 2, "completion cue");
    internal static float[] Read(Stream stream, int maxSamples, string what)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        if (new string(reader.ReadChars(4)) != "RIFF" || reader.ReadInt32() != stream.Length - 8 ||
            new string(reader.ReadChars(4)) != "WAVE" || new string(reader.ReadChars(4)) != "fmt " ||
            reader.ReadInt32() != 16 || reader.ReadInt16() != 1 || reader.ReadInt16() != 1 ||
            reader.ReadInt32() != SampleRate || reader.ReadInt32() != SampleRate * 2 || reader.ReadInt16() != 2 ||
            reader.ReadInt16() != 16 || new string(reader.ReadChars(4)) != "data")
            throw new InvalidDataException("Unsupported " + what + " format.");
        int bytes = reader.ReadInt32();
        if (bytes <= 0 || bytes > maxSamples * 2 || bytes % 2 != 0 || bytes != stream.Length - stream.Position)
            throw new InvalidDataException("Invalid " + what + " length.");
        var samples = new float[bytes / 2];
        for (int i = 0; i < samples.Length; i++) samples[i] = reader.ReadInt16() / 32768f;
        return samples;
    }
}
