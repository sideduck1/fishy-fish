using UnityEngine;

public class VoiceInput : MonoBehaviour
{
    public static float loudness;
    public static float pitch; // in Hz

    [Header("Sensitivity")]
    public float minLoudness = 0.05f;

    [Header("Microphone Gain")]
    [Range(0.1f, 10f)]
    public float micGain = 1f;

    [Header("Pitch Settings")]
    public float minPitch = 80f;
    public float maxPitch = 1000f;

    private AudioClip micClip;
    private string micDevice;
    private int sampleWindow = 1024;

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 10, 44100);
        }
        else
        {
            Debug.LogError("❌ Geen microfoon gevonden!");
        }
    }

    void Update()
    {
        if (micClip == null) return;

        loudness = GetLoudness() * micGain;
        loudness = Mathf.Clamp01(loudness);

        if (loudness < minLoudness)
        {
            loudness = 0f;
            pitch = 0f;
            return;
        }

        pitch = GetPitch();
    }

    float GetLoudness()
    {
        float[] samples = new float[sampleWindow];
        int micPos = Microphone.GetPosition(micDevice) - sampleWindow;
        if (micPos < 0) return 0;

        micClip.GetData(samples, micPos);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }

        return sum / samples.Length;
    }

    float GetPitch()
    {
        float[] samples = new float[sampleWindow];
        int micPos = Microphone.GetPosition(micDevice) - sampleWindow;
        if (micPos < 0) return 0;

        micClip.GetData(samples, micPos);

        int bestOffset = 0;
        float bestCorrelation = 0f;

        for (int offset = 20; offset < 500; offset++)
        {
            float correlation = 0f;

            for (int i = 0; i < sampleWindow - offset; i++)
            {
                correlation += samples[i] * samples[i + offset];
            }

            if (correlation > bestCorrelation)
            {
                bestCorrelation = correlation;
                bestOffset = offset;
            }
        }

        if (bestOffset == 0) return 0;

        float frequency = 44100f / bestOffset;

        if (frequency < minPitch || frequency > maxPitch)
            return 0;

        return frequency;
    }
}