using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The open napkin. Opening freezes the world (one paused flag on GameManager); closing resumes it.
// Lives on the always-active OverlayCanvas.
public class NotePanel : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private StoryDataSO story;

    [Header("Pieces")]
    [SerializeField] private GameObject panelRoot;      // hidden by default
    [SerializeField] private TMP_Text headerLabel;      // "Tell us what you thought of the food!"
    [SerializeField] private TMP_Text guestLabel;
    [SerializeField] private TMP_Text bodyLabel;        // writer's text VERBATIM
    [SerializeField] private GameObject prevArrow;      // Buttons wired to Prev()/Next() in inspector
    [SerializeField] private GameObject nextArrow;

    private List<StoryDataSO.UnlockBeat> napkins;
    private int index;

    public void Open(List<StoryDataSO.UnlockBeat> list)
    {
        if (list == null || list.Count == 0) return;
        napkins = list;
        index = list.Count - 1;          // newest first
        panelRoot.SetActive(true);
        gameManager.SetPaused(true);     // the world holds its breath
        Refresh();
    }

    public void Close()                  // wired to close button
    {
        panelRoot.SetActive(false);
        gameManager.SetPaused(false);
    }

    public void Next() { if (index < napkins.Count - 1) { index++; Refresh(); } }
    public void Prev() { if (index > 0)                 { index--; Refresh(); } }

    private void Refresh()
    {
        var n = napkins[index];
        headerLabel.text = story.napkinHeader;
        guestLabel.text  = n.guestName;
        bodyLabel.text   = n.napkinText;
        prevArrow.SetActive(index > 0);
        nextArrow.SetActive(index < napkins.Count - 1);
    }
}
