using UnityEngine;

public class VoiceInput : MonoBehaviour
{
    public static float loudness;
    public static float pitch;

    [Header("Sensitivity")]
    [Tooltip("Minimum volume om te registreren")]
    public float minLoudness = 0.05f; // alles onder 0.05 wordt genegeerd

    private AudioClip micClip;
    private string micDevice;
    private int sampleWindow = 256;

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 10, 44100);
            Debug.Log("🎤 Microfoon gestart: " + micDevice);
        }
        else
        {
            Debug.LogError("❌ Geen microfoon gevonden!");
        }
    }

    void Update()
    {
        if (micClip == null) return;

        loudness = GetLoudness();
        pitch = GetPitch();

        // minimale input toepassen
        if (loudness < minLoudness)
        {
            loudness = 0f;
        }

        if (loudness > 0f)
            Debug.Log("🟢 Stem gedetecteerd! Loudness: " + loudness + " | Pitch: " + pitch);
        else
            Debug.Log("🔴 Stilte gedetecteerd. Loudness: " + loudness);
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

        float maxVal = 0f;
        int maxIndex = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > maxVal)
            {
                maxVal = Mathf.Abs(samples[i]);
                maxIndex = i;
            }
        }
        return maxIndex;
    }
}
