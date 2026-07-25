using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Owns the bust's visuals ONLY. Reports nothing, decides nothing — GameManager drives it.
public class CustomerView : MonoBehaviour
{
    [Header("Feel")]
    [SerializeField] private TavernFeelSO feel;

    [Header("Pieces")]
    [SerializeField] private BustDresser bust;
    [SerializeField] private CanvasGroup group;          // fade in/out
    [SerializeField] private RectTransform rect;         // slide / hop / droop / idle bob
    [SerializeField] private GameObject thoughtBubble;   // hungry icon while ordering

    // Grey tint shown behind the bust until the real portrait PNG is assigned on the CharacterSO.
    private static readonly Color PortraitPlaceholderTint = new Color(0.6f, 0.6f, 0.65f, 1f);

    private Vector2 homePos;
    private Coroutine idle;

    private void Awake()
    {
        homePos = rect.anchoredPosition;
        group.alpha = 0f;
        thoughtBubble.SetActive(false);
    }

    public IEnumerator EnterRoutine(Dictionary<SlotType, IngredientSO> order, CharacterSO character)
    {
        StopIdle();
        if (character != null)
            bust.ShowPortrait(character.portrait, PortraitPlaceholderTint);  // real visitor
        else
            bust.Dress(order);                                                    // grey-box dressing

        // slide up from below WHILE fading in — no more pop-in-place
        rect.anchoredPosition = homePos + Vector2.down * feel.enterRisePixels;
        StartCoroutine(Tween.Move(rect, rect.anchoredPosition, homePos, feel.fadeInSeconds, feel.easeCurve));
        yield return Tween.Fade(group, 0f, 1f, feel.fadeInSeconds, feel.easeCurve);
        rect.anchoredPosition = homePos;

        thoughtBubble.SetActive(true);
        StartCoroutine(Tween.Punch(thoughtBubble.transform, 1.25f, 0.25f));   // little pop instead of a hard snap
        idle = StartCoroutine(IdleBob());
    }

    public void ShowReaction(int hearts)
    {
        StopIdle();
        thoughtBubble.SetActive(false);
        bust.SetFace(hearts, feel.happyFaceMinHearts);
        rect.anchoredPosition = homePos;
        if (hearts >= feel.happyFaceMinHearts)
            StartCoroutine(Tween.Hop(rect, homePos, feel.happyHopHeight, feel.happyHopSeconds, feel.easeCurve));
        else if (hearts == 0)
            // slump down — now eased instead of teleporting
            StartCoroutine(Tween.Move(rect, homePos, homePos + Vector2.down * feel.sadDroopPixels, feel.sadDroopSeconds, feel.easeCurve));
    }

    public IEnumerator ExitRoutine(bool happy)
    {
        StopIdle();
        // happy guests bounce up-and-out; unhappy ones slump down as they fade
        Vector2 from = rect.anchoredPosition;
        Vector2 to   = homePos + (happy ? Vector2.up : Vector2.down) * feel.exitDriftPixels;
        StartCoroutine(Tween.Move(rect, from, to, feel.fadeOutSeconds, feel.easeCurve));
        yield return Tween.Fade(group, 1f, 0f, feel.fadeOutSeconds, feel.easeCurve);
        rect.anchoredPosition = homePos;
    }

    // Gentle sine bob so the waiting bust isn't dead-still. Runs from entrance until the reaction beat.
    private IEnumerator IdleBob()
    {
        if (feel.idleBobPixels <= 0f) yield break;
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float y = Mathf.Sin(t / feel.idleBobSeconds * Mathf.PI * 2f) * feel.idleBobPixels;
            rect.anchoredPosition = homePos + Vector2.up * y;
            yield return null;
        }
    }

    private void StopIdle()
    {
        if (idle != null) { StopCoroutine(idle); idle = null; }
        rect.anchoredPosition = homePos;
    }
}
