using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Hidden panel on the OverlayCanvas, filled by GameManager on NightEnd. Retry = reload the scene (full reset).
// No rank stamp — the night is summed up as hearts + coins, big and centred.
public class EndScreen : MonoBehaviour
{
    [Header("Pieces")]
    [SerializeField] private GameObject panelRoot;       // hidden by default
    [SerializeField] private TMP_Text heartsLabel;       // big number next to the heart icon
    [SerializeField] private TMP_Text coinsLabel;        // big gold "$ N"
    [SerializeField] private TMP_Text storyLabel;        // rank-matched Yasika line — hidden until authored

    public void Show(int hearts, int coins, string endingLine)
    {
        panelRoot.SetActive(true);
        heartsLabel.text = hearts.ToString();
        coinsLabel.text  = "$ " + coins;
        // No authored ending line yet — hide the label instead of showing a placeholder/empty gap.
        storyLabel.text  = endingLine;
        storyLabel.gameObject.SetActive(!string.IsNullOrEmpty(endingLine));
        StartCoroutine(Pop(heartsLabel.transform.parent));   // the stats row shares one parent
    }

    private IEnumerator Pop(Transform row)
    {
        yield return null;                                   // let layout settle for one frame
        yield return Tween.Punch(row, 1.12f, 0.3f);
    }

    public void Retry()   // wired to the Retry button
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
