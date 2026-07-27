using UnityEngine;
using UnityEngine.UI;

// The candle's live flame. Rides the burn line of the wax (a Filled Image) so the clipped fill
// edge always reads as the candle's molten top — never as a hard cut through the art.
// Same trick as PotSteam: generated soft-circle Images flickered by Perlin noise, because a real
// ParticleSystem would be invisible under the Screen Space - Overlay canvases.
public class CandleFlame : MonoBehaviour
{
    [Header("Wired")]
    [Tooltip("The Filled candle image — the flame follows its fillAmount")]
    [SerializeField] private Image wax;
    [Tooltip("Optional — last call makes the flame gutter, night end snuffs it")]
    [SerializeField] private NightClock clock;
    [Tooltip("Optional — the rising glow puffs on this GO; turns to smoke when snuffed")]
    [SerializeField] private PotSteam embers;

    [Header("Look")]
    [SerializeField] private Color glowColor  = new Color(1f, 0.72f, 0.35f, 0.4f);
    [SerializeField] private Color outerColor = new Color(1f, 0.58f, 0.16f, 0.9f);
    [SerializeField] private Color innerColor = new Color(1f, 0.94f, 0.66f, 1f);
    [SerializeField] private float glowSize    = 130f;
    [SerializeField] private float flameHeight = 30f;
    [Tooltip("Flame base sits this far above the burn line")]
    [SerializeField] private float wickOffset  = 2f;

    [Header("Flicker")]
    [SerializeField] private float flickerAmount = 0.12f;
    [SerializeField] private float flickerSpeed  = 1.2f;
    [Tooltip("Last call — the flame gutters this much faster and harder")]
    [SerializeField] private float nervousFactor = 2.5f;

    private RectTransform glow, outer, inner;
    private Image glowImg;
    private bool nervous, snuffed;
    private float seed;

    private void Awake()
    {
        seed = Random.Range(0f, 100f);
        var circle = PotSteam.MakeSoftCircle();
        glow  = MakeLayer("Glow",  circle, glowColor,  out glowImg);
        outer = MakeLayer("Outer", circle, outerColor, out _);
        inner = MakeLayer("Inner", circle, innerColor, out _);
        if (clock != null)
        {
            clock.OnLastCall += () => nervous = true;
            clock.OnNightEnd += Snuff;
        }
    }

    private void Update()
    {
        if (snuffed || wax == null) return;

        // Follow the burn line: fill origin is the bottom, so the molten top sits at fillAmount.
        var waxRect = wax.rectTransform;
        float lineY = (wax.fillAmount - waxRect.pivot.y) * waxRect.rect.height;
        ((RectTransform)transform).anchoredPosition = new Vector2(0f, lineY + wickOffset);

        float speed = flickerSpeed * (nervous ? nervousFactor : 1f);
        float amount = flickerAmount * (nervous ? nervousFactor : 1f);
        float t = Time.time * speed + seed;
        float sway = Mathf.PerlinNoise(t, 0.3f) - 0.5f;    // -0.5..0.5
        float breath = Mathf.PerlinNoise(0.7f, t * 1.7f) - 0.5f;

        float h = flameHeight * (1f + breath * amount * 2f);
        float w = flameHeight * 0.55f * (1f + sway * amount);
        outer.sizeDelta = new Vector2(w, h);
        outer.anchoredPosition = new Vector2(sway * 4f, h * 0.5f);
        inner.sizeDelta = new Vector2(w * 0.55f, h * 0.55f);
        inner.anchoredPosition = new Vector2(sway * 3f, h * 0.4f);
        glow.sizeDelta = Vector2.one * (glowSize * (1f + breath * amount));
        glow.anchoredPosition = new Vector2(0f, h * 0.4f);
        var c = glowColor;
        c.a = glowColor.a * Mathf.Clamp01(1f + breath * amount * 2f);
        glowImg.color = c;
    }

    // The night is over: flame out, glow puffs become a wisp of smoke.
    private void Snuff()
    {
        snuffed = true;
        glow.gameObject.SetActive(false);
        outer.gameObject.SetActive(false);
        inner.gameObject.SetActive(false);
        if (embers)
        {
            embers.tint = new Color(0.72f, 0.72f, 0.75f);
            embers.SetIntensity(0.25f);
        }
    }

    private RectTransform MakeLayer(string layerName, Sprite sprite, Color color, out Image image)
    {
        var go = new GameObject(layerName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return (RectTransform)go.transform;
    }
}
