using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Hidden panel on the OverlayCanvas, filled by GameManager on NightEnd. Retry = reload the scene (full reset).
public class EndScreen : MonoBehaviour
{
    [SerializeField] private GameConfigSO config;

    [Header("Pieces")]
    [SerializeField] private GameObject panelRoot;      // hidden by default
    [SerializeField] private TMP_Text servedLabel;
    [SerializeField] private TMP_Text heartsLabel;
    [SerializeField] private TMP_Text coinsLabel;
    [SerializeField] private TMP_Text rankLabel;        // the C/B/A/S stamp
    [SerializeField] private TMP_Text storyLabel;       // rank-matched Yasika line

    public void Show(int served, int hearts, int coins, int rankIndex, string endingLine)
    {
        panelRoot.SetActive(true);
        servedLabel.text = "Customers served: " + served;
        heartsLabel.text = "Hearts: " + hearts;
        coinsLabel.text  = "Coins: " + coins;
        rankLabel.text   = config.rankLetters[Mathf.Clamp(rankIndex, 0, config.rankLetters.Length - 1)];
        storyLabel.text  = endingLine;
    }

    public void Retry()   // wired to the Retry button
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
