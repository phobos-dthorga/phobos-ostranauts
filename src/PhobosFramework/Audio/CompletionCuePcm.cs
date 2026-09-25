using System.IO;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>Reads only our short canonical mono PCM16 export; no runtime file/network loader.</summary>
internal static class CompletionCuePcm
{
    internal const int SampleRate = 44100;
    internal static float[] Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        if (new string(reader.ReadChars(4)) != "RIFF" || reader.ReadInt32() != stream.Length - 8 ||
            new string(reader.ReadChars(4)) != "WAVE" || new string(reader.ReadChars(4)) != "fmt " ||
            reader.ReadInt32() != 16 || reader.ReadInt16() != 1 || reader.ReadInt16() != 1 ||
            reader.ReadInt32() != SampleRate || reader.ReadInt32() != SampleRate * 2 || reader.ReadInt16() != 2 ||
            reader.ReadInt16() != 16 || new string(reader.ReadChars(4)) != "data")
            throw new InvalidDataException("Unsupported completion cue format.");
        int bytes = reader.ReadInt32();
        if (bytes <= 0 || bytes > SampleRate || bytes % 2 != 0 || bytes != stream.Length - stream.Position)
            throw new InvalidDataException("Invalid completion cue length.");
        var samples = new float[bytes / 2];
        for (int i = 0; i < samples.Length; i++) samples[i] = reader.ReadInt16() / 32768f;
        return samples;
    }
}
