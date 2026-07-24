using System.Collections;
using UnityEngine;

// The whole tween library. No packages. Every routine takes its timing from a FeelSO field.
public static class Tween
{
    public static IEnumerator Fade(CanvasGroup g, float from, float to, float seconds, AnimationCurve ease)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            g.alpha = Mathf.LerpUnclamped(from, to, ease.Evaluate(t / seconds));
            yield return null;
        }
        g.alpha = to;
    }

    public static IEnumerator Move(RectTransform r, Vector2 from, Vector2 to, float seconds, AnimationCurve ease)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            r.anchoredPosition = Vector2.LerpUnclamped(from, to, ease.Evaluate(t / seconds));
            yield return null;
        }
        r.anchoredPosition = to;
    }

    // Parabolic arc between two WORLD positions (jar -> pot across parents)
    public static IEnumerator ArcWorld(Transform tr, Vector3 from, Vector3 to, float height, float seconds, AnimationCurve ease)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float p = ease.Evaluate(t / seconds);
            Vector3 pos = Vector3.LerpUnclamped(from, to, p);
            pos.y += height * 4f * p * (1f - p);
            tr.position = pos;
            yield return null;
        }
        tr.position = to;
    }

    // Hop in place (arc that returns home), in anchored space
    public static IEnumerator Hop(RectTransform r, Vector2 home, float height, float seconds, AnimationCurve ease)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float p = ease.Evaluate(t / seconds);
            Vector2 pos = home;
            pos.y += height * 4f * p * (1f - p);
            r.anchoredPosition = pos;
            yield return null;
        }
        r.anchoredPosition = home;
    }

    // Scale punch and settle back
    public static IEnumerator Punch(Transform tr, float scale, float seconds)
    {
        Vector3 baseScale = tr.localScale;
        Vector3 peak = baseScale * scale;
        float half = Mathf.Max(0.01f, seconds * 0.5f);
        for (float t = 0f; t < half; t += Time.deltaTime) { tr.localScale = Vector3.Lerp(baseScale, peak, t / half); yield return null; }
        for (float t = 0f; t < half; t += Time.deltaTime) { tr.localScale = Vector3.Lerp(peak, baseScale, t / half); yield return null; }
        tr.localScale = baseScale;
    }
}
