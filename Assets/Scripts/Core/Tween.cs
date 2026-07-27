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

    // Unhappy buzz: fast horizontal shake that decays to rest, in anchored space
    public static IEnumerator Shake(RectTransform r, Vector2 home, float pixels, float cycles, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float p = t / seconds;
            float x = Mathf.Sin(p * cycles * Mathf.PI * 2f) * pixels * (1f - p);
            r.anchoredPosition = home + Vector2.right * x;
            yield return null;
        }
        r.anchoredPosition = home;
    }

    // Scale punch: fast attack to the peak, then a damped overshoot settle back (snappier than a linear triangle).
    public static IEnumerator Punch(Transform tr, float scale, float seconds)
    {
        Vector3 baseScale = tr.localScale;
        float amp = scale - 1f;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            tr.localScale = baseScale * (1f + amp * Punch01(t / seconds));
            yield return null;
        }
        tr.localScale = baseScale;
    }

    // 0 -> peak(1) in the first 30%, then a decaying overshoot (dips slightly past base) back to 0.
    private static float Punch01(float p)
    {
        if (p < 0.3f) return Mathf.SmoothStep(0f, 1f, p / 0.3f);
        float q = (p - 0.3f) / 0.7f;
        return Mathf.Cos(q * Mathf.PI * 1.5f) * (1f - q);
    }
}
