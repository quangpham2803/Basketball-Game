using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.15f;
    [SerializeField] private float bpm = 95f;

    private AudioSource source;
    private AudioClip loopClip;

    private const int SampleRate = 44100;
    private const int Channels = 1;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.volume = volume;
        source.playOnAwake = false;

        loopClip = GenerateLoop();
        source.clip = loopClip;
        source.Play();
    }

    private void Update()
    {
        int streak = scoreManager != null ? scoreManager.Streak : 0;
        float targetVol = volume + Mathf.Clamp01(streak * 0.03f) * 0.15f;
        source.volume = Mathf.Lerp(source.volume, targetVol, Time.deltaTime * 3f);

        float targetPitch = 1f + Mathf.Clamp01(streak - 2) * 0.04f;
        source.pitch = Mathf.Lerp(source.pitch, targetPitch, Time.deltaTime * 2f);
    }

    private AudioClip GenerateLoop()
    {
        float beatDur = 60f / bpm;
        int bars = 4;
        int beatsPerBar = 4;
        int totalBeats = bars * beatsPerBar;
        int totalSamples = (int)(totalBeats * beatDur * SampleRate);
        float[] data = new float[totalSamples];

        float[] bassPattern = { 65f, 0f, 65f, 0f, 82f, 0f, 73f, 0f,
                                 65f, 0f, 65f, 0f, 82f, 0f, 98f, 0f };

        float[] melodyPattern = { 330f, 0f, 392f, 330f, 294f, 0f, 262f, 0f,
                                   330f, 0f, 392f, 440f, 392f, 0f, 330f, 0f };

        for (int beat = 0; beat < totalBeats; beat++)
        {
            int beatStart = (int)(beat * beatDur * SampleRate);
            int beatSamples = (int)(beatDur * SampleRate);

            if (beat % 4 == 0 || beat % 4 == 2)
                AddKick(data, beatStart, beatSamples);

            if (beat % 4 == 1 || beat % 4 == 3)
                AddSnare(data, beatStart, beatSamples);

            AddHiHat(data, beatStart, beatSamples, 0.08f);
            AddHiHat(data, beatStart + beatSamples / 2, beatSamples / 2, 0.04f);

            float bassFreq = bassPattern[beat % bassPattern.Length];
            if (bassFreq > 0f)
                AddBass(data, beatStart, beatSamples, bassFreq);

            float melodyFreq = melodyPattern[beat % melodyPattern.Length];
            if (melodyFreq > 0f)
                AddMelody(data, beatStart, beatSamples, melodyFreq);
        }

        Normalize(data, 0.85f);

        var clip = AudioClip.Create("BGM", totalSamples, Channels, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void AddKick(float[] data, int start, int len)
    {
        int samples = Mathf.Min(len, (int)(0.2f * SampleRate));
        for (int i = 0; i < samples && start + i < data.Length; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(150f, 40f, t);
            float env = Mathf.Exp(-t * 8f);
            data[start + i] += Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate) * env * 0.4f;
        }
    }

    private void AddSnare(float[] data, int start, int len)
    {
        int samples = Mathf.Min(len, (int)(0.12f * SampleRate));
        for (int i = 0; i < samples && start + i < data.Length; i++)
        {
            float t = (float)i / samples;
            float env = Mathf.Exp(-t * 12f);
            float noise = (Mathf.PerlinNoise(i * 0.1f, 3f) - 0.5f) * 2f;
            float tone = Mathf.Sin(2f * Mathf.PI * 200f * i / SampleRate);
            data[start + i] += (noise * 0.6f + tone * 0.3f) * env * 0.25f;
        }
    }

    private void AddHiHat(float[] data, int start, int len, float vol)
    {
        int samples = Mathf.Min(len, (int)(0.05f * SampleRate));
        for (int i = 0; i < samples && start + i < data.Length; i++)
        {
            float t = (float)i / samples;
            float env = Mathf.Exp(-t * 30f);
            float noise = (Mathf.PerlinNoise(i * 0.3f, 7f) - 0.5f) * 2f;
            data[start + i] += noise * env * vol;
        }
    }

    private void AddBass(float[] data, int start, int len, float freq)
    {
        int samples = Mathf.Min(len, (int)(0.3f * SampleRate));
        for (int i = 0; i < samples && start + i < data.Length; i++)
        {
            float t = (float)i / samples;
            float env = t < 0.05f ? t / 0.05f : Mathf.Exp(-(t - 0.05f) * 4f);
            float wave = Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate)
                       + Mathf.Sin(2f * Mathf.PI * freq * 0.5f * i / SampleRate) * 0.5f;
            data[start + i] += wave * env * 0.2f;
        }
    }

    private void AddMelody(float[] data, int start, int len, float freq)
    {
        int samples = Mathf.Min(len, (int)(0.25f * SampleRate));
        for (int i = 0; i < samples && start + i < data.Length; i++)
        {
            float t = (float)i / samples;
            float env = t < 0.03f ? t / 0.03f : Mathf.Exp(-(t - 0.03f) * 5f);
            float phase = 2f * Mathf.PI * freq * i / SampleRate;
            float wave = Mathf.Sin(phase) * 0.7f
                       + Mathf.Sin(phase * 2f) * 0.2f
                       + Mathf.Sin(phase * 3f) * 0.1f;
            data[start + i] += wave * env * 0.08f;
        }
    }

    private void Normalize(float[] data, float peak)
    {
        float max = 0f;
        for (int i = 0; i < data.Length; i++)
            max = Mathf.Max(max, Mathf.Abs(data[i]));

        if (max > 0.001f)
        {
            float scale = peak / max;
            for (int i = 0; i < data.Length; i++)
                data[i] *= scale;
        }
    }
}
