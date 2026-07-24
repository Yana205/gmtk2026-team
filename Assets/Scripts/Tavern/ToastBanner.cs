using System.Collections;
using TMPro;
using UnityEngine;

// One banner for everything: unlock toasts AND the LAST CALL banner (text comes from StoryDataSO).
// Lives on the always-active OverlayCanvas so it can fire over either screen.
public class ToastBanner : MonoBehaviour
{
    [SerializeField] private TavernFeelSO feel;

    [Header("Pieces")]
    [SerializeField] private RectTransform banner;
    [SerializeField] private TMP_Text label;

    [Header("Positions (anchored) — banner sits at hiddenPos in the scene")]
    [SerializeField] private Vector2 shownPos  = new Vector2(0f, -80f);
    [SerializeField] private Vector2 hiddenPos = new Vector2(0f, 120f);

    public IEnumerator ShowRoutine(string text)
    {
        label.text = text;
        yield return Tween.Move(banner, hiddenPos, shownPos, feel.bannerSlideSeconds, feel.easeCurve);
        yield return new WaitForSeconds(feel.toastHoldSeconds);
        yield return Tween.Move(banner, shownPos, hiddenPos, feel.bannerSlideSeconds, feel.easeCurve);
    }
}
