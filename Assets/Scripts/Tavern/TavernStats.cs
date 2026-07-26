using TMPro;
using UnityEngine;

// The night's running tally in the tavern corner: hearts earned + coins collected.
// Lives under TavernScreen so it hides with it in the kitchen. Polls two ints per frame
// (totals change mid-coroutine in GameManager, so events would fire before the numbers move).
public class TavernStats : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text heartsLabel;
    [SerializeField] private TMP_Text coinsLabel;
    [Range(1f, 1.5f)]
    [SerializeField] private float changePunchScale = 1.25f;

    private int shownHearts = -1, shownCoins = -1;

    private void Update()
    {
        if (!gameManager) return;
        Refresh(heartsLabel, ref shownHearts, gameManager.TotalHearts);
        Refresh(coinsLabel,  ref shownCoins,  gameManager.TotalCoins);
    }

    private void Refresh(TMP_Text label, ref int shown, int value)
    {
        if (!label || shown == value) return;
        bool punch = shown >= 0;                   // no pop on the initial fill-in
        shown = value;
        label.text = value.ToString();
        if (punch) StartCoroutine(Tween.Punch(label.transform, changePunchScale, 0.25f));
    }
}
