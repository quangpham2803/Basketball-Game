using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class GameFeedback : MonoBehaviour
{
    [Header("Screen Shake")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float scoreShakeIntensity = 0.2f;
    [SerializeField] private float swishShakeIntensity = 0.35f;
    [SerializeField] private float shakeDuration = 0.25f;

    [Header("Slow Motion")]
    [SerializeField] private float slowMoTimeScale = 0.2f;
    [SerializeField] private float slowMoDuration = 0.5f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private Image screenFlash;

    private AudioSource audioSource;
    private AudioClip throwClip;
    private AudioClip bounceClip;
    private AudioClip scoreClip;
    private AudioClip swishClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        GenerateAudioClips();

        if (screenFlash != null)
            screenFlash.gameObject.SetActive(false);
    }

    public void PlayScoreEffect(bool isSwish)
    {
        float intensity = isSwish ? swishShakeIntensity : scoreShakeIntensity;
        if (cameraShake != null)
            cameraShake.Shake(intensity, shakeDuration);

        StartCoroutine(SlowMotionPulse());

        string text = isSwish ? "SWISH!" : "SCORE!";
        Color color = isSwish ? new Color(1f, 0.9f, 0.2f) : Color.white;
        StartCoroutine(ShowPopup(text, color));

        if (screenFlash != null)
            StartCoroutine(FlashScreen(isSwish ? new Color(1f, 0.9f, 0.3f, 0.3f) : new Color(1f, 1f, 1f, 0.2f)));

        PlaySound(isSwish ? swishClip : scoreClip, 0.6f);
    }

    public void PlayThrowEffect()
    {
        PlaySound(throwClip, 0.5f, Random.Range(0.9f, 1.1f));
    }

    public void PlayBounceEffect(float impact)
    {
        float volume = Mathf.Clamp01(impact / 8f) * 0.4f;
        PlaySound(bounceClip, volume, Random.Range(0.85f, 1.15f));
    }

    private void PlaySound(AudioClip clip, float volume, float pitch = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
    }

    private IEnumerator FlashScreen(Color color)
    {
        screenFlash.gameObject.SetActive(true);
        screenFlash.color = color;

        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(color.a, 0f, elapsed / duration);
            screenFlash.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        screenFlash.gameObject.SetActive(false);
    }

    private IEnumerator SlowMotionPulse()
    {
        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * slowMoTimeScale;

        float elapsed = 0f;
        while (elapsed < slowMoDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / slowMoDuration;
            Time.timeScale = Mathf.Lerp(slowMoTimeScale, 1f, t * t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private IEnumerator ShowPopup(string text, Color color)
    {
        if (popupText == null) yield break;

        popupText.text = text;
        popupText.color = color;
        popupText.gameObject.SetActive(true);
        popupText.rectTransform.anchoredPosition = Vector2.zero;

        float duration = 0.9f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float scale;
            if (t < 0.15f)
                scale = Mathf.Lerp(0f, 1.4f, t / 0.15f);
            else if (t < 0.3f)
                scale = Mathf.Lerp(1.4f, 1f, (t - 0.15f) / 0.15f);
            else
                scale = Mathf.Lerp(1f, 0.7f, (t - 0.3f) / 0.7f);

            popupText.transform.localScale = Vector3.one * scale;
            float alpha = t < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
            popupText.alpha = alpha;
            popupText.rectTransform.anchoredPosition = Vector2.up * t * 60f;

            yield return null;
        }

        popupText.gameObject.SetActive(false);
        popupText.rectTransform.anchoredPosition = Vector2.zero;
    }

    private void GenerateAudioClips()
    {
        throwClip = GenThrow();
        bounceClip = GenBounce();
        scoreClip = GenScoreChime();
        swishClip = GenSwish();
    }

    private AudioClip GenThrow()
    {
        int rate = 44100, len = (int)(rate * 0.18f);
        var d = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float env = Mathf.Sin(t * Mathf.PI) * 0.7f;
            float whoosh = (Mathf.PerlinNoise(i * 0.05f, 0f) - 0.5f) * 2f;
            float sweep = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 80f, t) * i / rate);
            d[i] = (whoosh * 0.6f + sweep * 0.4f) * env;
        }
        return MakeClip("Throw", d, rate);
    }

    private AudioClip GenBounce()
    {
        int rate = 44100, len = rate / 8;
        var d = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 18f);
            float f = Mathf.Lerp(280f, 80f, t);
            d[i] = Mathf.Sin(2f * Mathf.PI * f * i / rate) * env * 0.35f
                 + (Mathf.PerlinNoise(i * 0.08f, 1f) - 0.5f) * env * 0.25f;
        }
        return MakeClip("Bounce", d, rate);
    }

    private AudioClip GenScoreChime()
    {
        int rate = 44100, len = rate / 2;
        var d = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float env = Mathf.Exp(-t * 3f);
            float n1 = Mathf.Sin(2f * Mathf.PI * 523f * i / rate);
            float n2 = Mathf.Sin(2f * Mathf.PI * 659f * i / rate);
            d[i] = (t < 0.12f ? n1 : n1 * 0.4f + n2 * 0.6f) * env * 0.2f;
        }
        return MakeClip("Score", d, rate);
    }

    private AudioClip GenSwish()
    {
        int rate = 44100, len = rate / 3;
        var d = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            float noise = Mathf.PerlinNoise(i * 0.015f, 5f) - 0.5f;
            float env = Mathf.Sin(t * Mathf.PI) * 0.3f;
            float chime = Mathf.Sin(2f * Mathf.PI * 784f * i / rate) * Mathf.Exp(-t * 5f) * 0.15f;
            d[i] = noise * env + chime;
        }
        return MakeClip("Swish", d, rate);
    }

    private AudioClip MakeClip(string n, float[] d, int r)
    {
        var c = AudioClip.Create(n, d.Length, 1, r, false);
        c.SetData(d, 0);
        return c;
    }
}
