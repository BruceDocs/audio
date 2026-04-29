using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

class Program
{
    static void Main()
    {
        var stdout = Console.OpenStandardOutput();
        Console.SetOut(TextWriter.Null);

        using var capture = new WasapiLoopbackCapture();

        // Print actual capture format to stderr
        Console.Error.WriteLine($"Capture format: {capture.WaveFormat}");
        Console.Error.WriteLine($"Sample Rate: {capture.WaveFormat.SampleRate}");
        Console.Error.WriteLine($"Channels: {capture.WaveFormat.Channels}");
        Console.Error.WriteLine($"Bits per sample: {capture.WaveFormat.BitsPerSample}");
        Console.Error.WriteLine($"Encoding: {capture.WaveFormat.Encoding}");

        var buffered = new BufferedWaveProvider(capture.WaveFormat)
        {
            DiscardOnBufferOverflow = true
        };

        capture.DataAvailable += (s, e) =>
        {
            buffered.AddSamples(e.Buffer, 0, e.BytesRecorded);
        };

        capture.StartRecording();

        var sampleProvider = buffered.ToSampleProvider();
        var resampler = new WdlResamplingSampleProvider(sampleProvider, 16000);
        var floatBuffer = new float[1024];
        var byteBuffer = new byte[1024 * 2];

        Console.Error.WriteLine("Recording system audio to stdout...");

        while (true)
        {
            int read = resampler.Read(floatBuffer, 0, floatBuffer.Length);
            if (read > 0)
            {
                int bytes = 0;
                for (int i = 0; i < read; i++)
                {
                    var sample = Math.Clamp(floatBuffer[i], -1f, 1f);
                    short s16 = (short)(sample * short.MaxValue);
                    byteBuffer[bytes++] = (byte)(s16 & 0xff);
                    byteBuffer[bytes++] = (byte)((s16 >> 8) & 0xff);
                }
                stdout.Write(byteBuffer, 0, bytes);
                stdout.Flush();
            }
            else
            {
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
