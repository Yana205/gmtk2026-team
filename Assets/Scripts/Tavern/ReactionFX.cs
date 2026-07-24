using System.Collections;
using TMPro;
using UnityEngine;

// Hearts pop + coin popup. Pure juice — all timing from TavernFeelSO.
public class ReactionFX : MonoBehaviour
{
    [SerializeField] private TavernFeelSO feel;

    [Header("Pieces")]
    [SerializeField] private GameObject[] hearts;       // 3 heart images above the customer, start hidden
    [SerializeField] private TMP_Text coinPopupLabel;   // "+30", starts hidden
    [SerializeField] private RectTransform coinPopupRect;

    public void Play(int heartCount, int coins) => StartCoroutine(PlayRoutine(heartCount, coins));

    private IEnumerator PlayRoutine(int heartCount, int coins)
    {
        foreach (var h in hearts) h.SetActive(false);

        for (int i = 0; i < heartCount && i < hearts.Length; i++)
        {
            hearts[i].SetActive(true);
            StartCoroutine(Tween.Punch(hearts[i].transform, feel.heartPopScale, feel.heartPopInterval));
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
        foreach (var h in hearts) h.SetActive(false);
    }
}
