using UnityEngine;

public class ParticleEffects : MonoBehaviour
{
    [SerializeField] private Transform ball;
    [SerializeField] private Transform hoopTransform;

    private ParticleSystem scoreConfetti;
    private ParticleSystem swishSparkles;
    private ParticleSystem ballFire;
    private ParticleSystem impactDust;
    private ParticleSystem starBurst;

    private Material particleMat;

    private void Awake()
    {
        particleMat = new Material(Shader.Find("Sprites/Default"));

        scoreConfetti = BuildConfetti();
        swishSparkles = BuildSwishSparkles();
        impactDust = BuildImpactDust();
        starBurst = BuildStarBurst();

        if (ball != null)
            ballFire = BuildBallFire(ball);
    }

    public void PlayScore(bool isSwish)
    {
        if (hoopTransform == null) return;

        scoreConfetti.transform.position = hoopTransform.position;
        scoreConfetti.Play();

        starBurst.transform.position = hoopTransform.position;
        starBurst.Play();

        if (isSwish)
        {
            swishSparkles.transform.position = hoopTransform.position;
            swishSparkles.Play();
        }
    }

    public void PlayImpact(Vector3 position, float intensity)
    {
        impactDust.transform.position = position;
        var burst = impactDust.emission;
        int count = Mathf.Clamp((int)(intensity * 5f), 3, 20);
        burst.SetBurst(0, new ParticleSystem.Burst(0f, (short)count));
        impactDust.Play();
    }

    public void SetBallFire(bool active)
    {
        if (ballFire == null) return;
        if (active && !ballFire.isPlaying)
            ballFire.Play();
        else if (!active && ballFire.isPlaying)
            ballFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private ParticleSystem BuildConfetti()
    {
        var ps = CreateSystem("ScoreConfetti");
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 1f));
        main.gravityModifier = 0.6f;
        main.maxParticles = 80;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 50));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ApplyFadeOverLifetime(ps);
        return ps;
    }

    private ParticleSystem BuildSwishSparkles()
    {
        var ps = CreateSystem("SwishSparkles");
        var main = ps.main;
        main.duration = 0.8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.3f), new Color(1f, 0.7f, 0.1f));
        main.gravityModifier = -0.3f;
        main.maxParticles = 60;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 40));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ApplyFadeOverLifetime(ps);
        return ps;
    }

    private ParticleSystem BuildImpactDust()
    {
        var ps = CreateSystem("ImpactDust");
        var main = ps.main;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new Color(0.8f, 0.7f, 0.6f, 0.6f);
        main.gravityModifier = 0.3f;
        main.maxParticles = 20;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 10));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.1f;

        ApplyFadeOverLifetime(ps);
        return ps;
    }

    private ParticleSystem BuildStarBurst()
    {
        var ps = CreateSystem("StarBurst");
        var main = ps.main;
        main.duration = 0.8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.2f, 1f), new Color(1f, 0.7f, 0f, 1f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.3f;
        main.maxParticles = 50;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 35));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.15f, 1.3f),
                new Keyframe(0.5f, 0.8f),
                new Keyframe(1f, 0f)));

        ApplyFadeOverLifetime(ps);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateStarMaterial();

        return ps;
    }

    private Material CreateStarMaterial()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        int points = 5;
        float outerR = size * 0.48f;
        float innerR = size * 0.2f;

        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x - center;
                float py = y - center;
                float dist = Mathf.Sqrt(px * px + py * py);
                float angle = Mathf.Atan2(py, px);

                float sector = Mathf.PI * 2f / points;
                float halfSector = sector * 0.5f;
                float localAngle = Mathf.Repeat(angle + Mathf.PI * 0.5f, sector);
                float t = Mathf.Abs(localAngle - halfSector) / halfSector;
                float starR = Mathf.Lerp(outerR, innerR, t);

                if (dist < starR)
                {
                    float bright = 1f - (dist / starR) * 0.3f;
                    tex.SetPixel(x, y, new Color(1f, 1f, bright, 1f));
                }
                else if (dist < starR + 2f)
                {
                    float alpha = 1f - (dist - starR) / 2f;
                    tex.SetPixel(x, y, new Color(1f, 0.9f, 0.5f, alpha * 0.6f));
                }
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = tex;
        return mat;
    }

    private ParticleSystem BuildBallFire(Transform ballTransform)
    {
        var go = new GameObject("BallFire");
        go.transform.SetParent(ballTransform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0f), new Color(1f, 0.15f, 0f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;
        main.loop = true;
        main.playOnAwake = false;
        main.gravityModifier = -0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 15;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ApplyFadeOverLifetime(ps);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;

        ps.Stop();
        return ps;
    }

    private ParticleSystem CreateSystem(string systemName)
    {
        var go = new GameObject(systemName);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMat;

        return ps;
    }

    private void ApplyFadeOverLifetime(ParticleSystem ps)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = gradient;
    }
}
