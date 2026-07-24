using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Sits on the front desk. Holds ONLY authored story napkins. Appears with the first one.
[RequireComponent(typeof(Button))]
public class NapkinPile : MonoBehaviour
{
    [SerializeField] private NotePanel notePanel;
    [SerializeField] private GameObject pileVisual;   // hidden until first napkin

    private readonly List<StoryDataSO.UnlockBeat> napkins = new List<StoryDataSO.UnlockBeat>();

    private void Awake()
    {
        pileVisual.SetActive(false);
        GetComponent<Button>().onClick.AddListener(() => notePanel.Open(napkins));
    }

    public void Add(StoryDataSO.UnlockBeat beat)
    {
        napkins.Add(beat);
        pileVisual.SetActive(true);
    }
}
