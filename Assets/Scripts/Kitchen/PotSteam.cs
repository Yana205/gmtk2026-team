using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Steam over the cooking pot. All our canvases are Screen Space - Overlay, and overlay UI is
// always drawn on top of anything a camera renders — a real world-space ParticleSystem would be
// invisible behind the kitchen art. So this is a tiny UI "particle system": pooled soft-circle
// Images that spawn at the pot mouth, rise, wobble, swell and fade. Intensity 0..1 drives spawn
// rate + opacity; StewBuilder ramps it with the pick count so a fuller pot steams harder.
public class PotSteam : MonoBehaviour
{
    [Range(0f, 1f)]      public float intensity      = 0.3f;
    [Header("Puff shape")]
    [Range(10f, 200f)]   public float startSize      = 46f;
    [Range(1f, 4f)]      public float endSizeFactor  = 2.1f;
    [Range(0f, 1f)]      public float maxAlpha       = 0.35f;
    [Header("Motion")]
    [Range(20f, 400f)]   public float riseHeight     = 130f;
    [Range(0f, 120f)]    public float spawnXJitter   = 55f;
    [Range(0f, 40f)]     public float wobble         = 14f;
    [Range(0.4f, 4f)]    public float lifeSeconds    = 1.7f;
    [Tooltip("Seconds between puffs at full intensity")]
    [Range(0.05f, 1f)]   public float spawnInterval  = 0.28f;

    private class Puff
    {
        public RectTransform rect;
        public Image image;
        public float age, x0, phase;
    }

    private readonly List<Puff> pool = new List<Puff>();
    private Sprite softCircle;
    private float spawnTimer;

    private void Awake()
    {
        softCircle = MakeSoftCircle();
    }

    public void SetIntensity(float value) => intensity = Mathf.Clamp01(value);

    private void Update()
    {
        // Spawn: interval stretches as intensity drops; near-zero intensity stops emitting.
        if (intensity > 0.02f)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = spawnInterval / intensity;
                Spawn();
            }
        }

        // Animate every live puff: rise (eased out), sideways wobble, swell, sine fade in/out.
        foreach (var p in pool)
        {
            if (!p.rect.gameObject.activeSelf) continue;
            p.age += Time.deltaTime;
            float t = p.age / lifeSeconds;
            if (t >= 1f) { p.rect.gameObject.SetActive(false); continue; }

            float rise = 1f - (1f - t) * (1f - t);
            float x = p.x0 + Mathf.Sin(p.age * 3f + p.phase) * wobble * t;
            p.rect.anchoredPosition = new Vector2(x, rise * riseHeight);
            float size = startSize * Mathf.Lerp(1f, endSizeFactor, t);
            p.rect.sizeDelta = new Vector2(size, size);
            var c = p.image.color;
            c.a = maxAlpha * Mathf.Lerp(0.5f, 1f, intensity) * Mathf.Sin(t * Mathf.PI);
            p.image.color = c;
        }
    }

    private void Spawn()
    {
        Puff puff = null;
        foreach (var p in pool)
            if (!p.rect.gameObject.activeSelf) { puff = p; break; }
        if (puff == null)
        {
            if (pool.Count >= 12) return;              // plenty for the longest life/interval combo
            var go = new GameObject("Puff", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            puff = new Puff { rect = (RectTransform)go.transform, image = go.GetComponent<Image>() };
            puff.image.sprite = softCircle;
            puff.image.raycastTarget = false;
            pool.Add(puff);
        }
        puff.age   = 0f;
        puff.x0    = Random.Range(-spawnXJitter, spawnXJitter);
        puff.phase = Random.Range(0f, Mathf.PI * 2f);
        puff.rect.anchoredPosition = new Vector2(puff.x0, 0f);
        puff.rect.sizeDelta = new Vector2(startSize, startSize);
        puff.image.color = new Color(1f, 1f, 1f, 0f);
        puff.rect.gameObject.SetActive(true);
    }

    // Radial-falloff white circle, generated so we don't need a texture asset.
    private static Sprite MakeSoftCircle(int size = 64)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                float a = Mathf.Clamp01(1f - d);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
