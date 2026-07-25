using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hearts pop + coin popup. Pure juice — all timing from TavernFeelSO.
public class ReactionFX : MonoBehaviour
{
    [SerializeField] private TavernFeelSO feel;

    [Header("Pieces")]
    [SerializeField] private GameObject[] hearts;       // 3 heart images above the customer, start hidden
    [SerializeField] private TMP_Text coinPopupLabel;   // "+30", starts hidden
    [SerializeField] private RectTransform coinPopupRect;

    private void Awake()
    {
        // Reaction visuals are transient — hide them until a reaction actually plays,
        // otherwise the scene's authored-active hearts/coin show from the intro on.
        foreach (var h in hearts) if (h) h.SetActive(false);
        if (coinPopupLabel) coinPopupLabel.gameObject.SetActive(false);
    }

    // Returns the running beat so the caller can wait for the FULL reveal (hearts + coin popup)
    // to finish before it fades the customer out — otherwise the reward animates over an empty stool.
    public Coroutine Play(int heartCount, int coins) => StartCoroutine(PlayRoutine(heartCount, coins));

    private IEnumerator PlayRoutine(int heartCount, int coins)
    {
        // Always show all 3 slots so a low score reads as "1/3", not "nothing happened":
        // earned hearts are full-colour, the rest sit dimmed as empty slots.
        for (int i = 0; i < hearts.Length; i++)
        {
            if (!hearts[i]) continue;
            hearts[i].SetActive(true);
            var img = hearts[i].GetComponent<Image>();
            if (img) img.color = i < heartCount ? feel.heartFullColor : feel.heartEmptyColor;
        }

        // Then pop only the earned ones, in sequence.
        for (int i = 0; i < heartCount && i < hearts.Length; i++)
        {
            if (hearts[i]) StartCoroutine(Tween.Punch(hearts[i].transform, feel.heartPopScale, feel.heartPopInterval));
            yield return new WaitForSeconds(feel.heartPopInterval);
        }

        if (coins > 0 && coinPopupLabel)
        {
            coinPopupLabel.text = "+" + coins;
            coinPopupLabel.gameObject.SetActive(true);
            Vector2 home = coinPopupRect.anchoredPosition;
            yield return Tween.Move(coinPopupRect, home, home + Vector2.up * feel.coinRisePixels,
                                    feel.coinFlySeconds, feel.easeCurve);
            coinPopupLabel.gameObject.SetActive(false);
            coinPopupRect.anchoredPosition = home;
        }

        yield return new WaitForSeconds(feel.reactionTotalSeconds * 0.3f);
        foreach (var h in hearts) if (h) h.SetActive(false);
    }
}
