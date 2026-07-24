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
    [SerializeField] private RectTransform rect;         // hop / droop
    [SerializeField] private GameObject thoughtBubble;   // hungry icon while ordering

    // Grey tint shown behind the bust until the real portrait PNG is assigned on the CharacterSO.
    private static readonly Color PortraitPlaceholderTint = new Color(0.6f, 0.6f, 0.65f, 1f);

    private Vector2 homePos;

    private void Awake()
    {
        homePos = rect.anchoredPosition;
        group.alpha = 0f;
        thoughtBubble.SetActive(false);
    }

    public IEnumerator EnterRoutine(Dictionary<SlotType, IngredientSO> order, CharacterSO character)
    {
        if (character != null)
            bust.ShowPortrait(character.portrait, PortraitPlaceholderTint);  // real visitor
        else
            bust.Dress(order);                                                    // grey-box dressing
        rect.anchoredPosition = homePos;
        yield return Tween.Fade(group, 0f, 1f, feel.fadeInSeconds, feel.easeCurve);
        thoughtBubble.SetActive(true);
    }

    public void ShowReaction(int hearts)
    {
        thoughtBubble.SetActive(false);
        bust.SetFace(hearts, feel.happyFaceMinHearts);
        if (hearts >= feel.happyFaceMinHearts)
            StartCoroutine(Tween.Hop(rect, homePos, feel.happyHopHeight, feel.happyHopSeconds, feel.easeCurve));
        else if (hearts == 0)
            rect.anchoredPosition = homePos + Vector2.down * feel.sadDroopPixels;
    }

    public IEnumerator ExitRoutine(bool happy)
    {
        yield return Tween.Fade(group, 1f, 0f, feel.fadeOutSeconds, feel.easeCurve);
        rect.anchoredPosition = homePos;
    }
}
