using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalLocalPos;
    private float shakeDuration;
    private float shakeIntensity;
    private float elapsed;

    private void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    public void Shake(float intensity, float duration)
    {
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration - elapsed, duration);
        elapsed = 0f;
    }

    private void LateUpdate()
    {
        if (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - (elapsed / shakeDuration);
            float strength = shakeIntensity * t;

            float seed = Time.unscaledTime * 25f;
            float x = (Mathf.PerlinNoise(seed, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, seed) - 0.5f) * 2f;

            transform.localPosition = originalLocalPos + new Vector3(x, y, 0f) * strength;
        }
        else
        {
            transform.localPosition = originalLocalPos;
            shakeIntensity = 0f;
        }
    }
}
