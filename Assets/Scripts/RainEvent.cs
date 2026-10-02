using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Lluvia una vez por sector: avisa, oscurece y la escorrentia arrastra al rio la basura que quedo cerca de la
// orilla (salvo la protegida por queñuas). Las bolsas arrastradas flotan y todavia se pueden rescatar.
public class RainEvent : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private float warning = 6f, duration = 14f, washInterval = 3.5f, washReach = 2f, purityLoss = 2f;
    [SerializeField] private Vector2 startRange = new Vector2(0.35f, 0.6f);   // fraccion del sector en que empieza

    private int scheduledSector = -1;
    private float startAt = -1f;
    private ParticleSystem rain;
    private AudioSource sound;

    void Start()
    {
        rain = BuildRain();
        sound = BuildSound();
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm.State != GamePhase.Playing) return;
        if (scheduledSector != gm.SectorIndex)
        {
            scheduledSector = gm.SectorIndex;
            startAt = Random.Range(startRange.x, startRange.y) * gm.Sector.duration;
        }
        else if (startAt >= 0f && gm.SectorTime >= startAt - warning)
        {
            startAt = -1f;
            StartCoroutine(Storm());
        }
    }

    IEnumerator Storm()
    {
        GameManager.I.Alert("Se acerca la lluvia: recoge la basura de las orillas antes de que el agua la arrastre", warning);
        yield return new WaitForSeconds(warning);
        RiverVisuals.Raining = true;
        rain.Play();
        sound.Play();
        float baseLight = globalLight.intensity, nextWash = 1f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.Clamp01(Mathf.Min(t, duration - t) / 1.5f);   // entra y sale suave
            globalLight.intensity = baseLight * (1f - 0.3f * k);
            sound.volume = 0.35f * k;
            if (t >= nextWash) { nextWash = t + washInterval; WashOne(); }
            yield return null;
        }
        globalLight.intensity = baseLight;
        rain.Stop();
        sound.Stop();
        RiverVisuals.Raining = false;
    }

    // la bolsa mas cercana al agua (dentro del alcance) se desliza al rio
    void WashOne()
    {
        TrashItem best = null;
        Vector2 bestWater = default;
        float bestDist = washReach;
        foreach (var t in FindObjectsByType<TrashItem>(FindObjectsSortMode.None))
        {
            Vector2 p = t.transform.position;
            if (Reforestation.Protects(p) || DumpSite.IsInside(p) || !RiverVisuals.NearestWater(p, washReach, out var w)) continue;
            float d = Vector2.Distance(p, w);
            if (d < bestDist) { bestDist = d; best = t; bestWater = w; }
        }
        if (best != null) StartCoroutine(Wash(best, bestWater));
    }

    IEnumerator Wash(TrashItem trash, Vector2 water)
    {
        trash.GetComponent<Collider2D>().enabled = false;   // ya no se recoge mientras se desliza
        Vector2 from = trash.transform.position;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            trash.transform.position = Vector2.Lerp(from, water, t / 0.6f);
            yield return null;
        }
        FloatingTrash.Spawn(trash.GetComponent<SpriteRenderer>().sprite, water);
        Destroy(trash.gameObject);
        RiverVisuals.RippleAt(water);
        GameManager.I.Purity.Remove(purityLoss);
        GameManager.I.RegisterPollution(false);
        GameManager.I.Alert("La lluvia arrastró basura de la orilla al río", 2f);
    }

    ParticleSystem BuildRain()
    {
        var go = new GameObject("Lluvia");
        go.transform.SetParent(Camera.main.transform, false);
        go.transform.localPosition = new Vector3(0f, 9f, 10f);   // sobre el borde superior de la vista, en z = 0
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false; main.loop = true; main.startLifetime = 1.3f; main.startSpeed = 0f; main.startSize = 0.07f;
        main.startColor = new Color(0.75f, 0.85f, 1f, 0.6f); main.maxParticles = 600; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 220f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(34f, 1f, 1f);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-3f); vel.y = new ParticleSystem.MinMaxCurve(-15f); vel.z = new ParticleSystem.MinMaxCurve(0f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = particleMaterial; r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.035f;
        r.sortingLayerName = "Decoration"; r.sortingOrder = 30;
        return ps;
    }

    // ruido suavizado en bucle: sonido de lluvia sin necesitar un archivo de audio
    AudioSource BuildSound()
    {
        int rate = 22050, n = rate * 2;
        var data = new float[n];
        float y = 0f;
        for (int i = 0; i < n; i++) { y += 0.35f * (Random.Range(-1f, 1f) - y); data[i] = y * 0.6f; }
        var clip = AudioClip.Create("lluvia", n, 1, rate, false);
        clip.SetData(data, 0);
        var src = gameObject.AddComponent<AudioSource>();
        src.clip = clip; src.loop = true; src.playOnAwake = false; src.volume = 0f;
        return src;
    }
}
