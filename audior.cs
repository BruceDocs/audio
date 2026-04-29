using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

class Program
{
    static void Main()
    {
        using var capture = new WasapiLoopbackCapture();

        var targetFormat = new WaveFormat(16000, 16, 1);
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

                Console.OpenStandardOutput().Write(byteBuffer, 0, bytes);
                Console.OpenStandardOutput().Flush();
            }
            else
            {
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
