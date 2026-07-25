using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Sits on the front desk. Holds ONLY authored story napkins. Appears with the first one.
// A new napkin lights a pulsing glow behind the pile (no text) until the player opens it.
[RequireComponent(typeof(Button))]
public class NapkinPile : MonoBehaviour
{
    [SerializeField] private NotePanel notePanel;
    [SerializeField] private GameObject pileVisual;   // hidden until first napkin
    [Tooltip("Soft halo behind the pile — pulses while there's an unread napkin")]
    [SerializeField] private Image unreadGlow;

    [Header("Unread pulse")]
    [SerializeField] private float pulseSeconds = 1.2f;
    [SerializeField] private float glowAlphaMin = 0.25f;
    [SerializeField] private float glowAlphaMax = 0.7f;
    [SerializeField] private float pulseScale   = 1.06f;

    private readonly List<StoryDataSO.UnlockBeat> napkins = new List<StoryDataSO.UnlockBeat>();
    private bool hasUnread;
    private Coroutine pulse;

    private void Awake()
    {
        pileVisual.SetActive(false);
        if (unreadGlow) unreadGlow.gameObject.SetActive(false);
        GetComponent<Button>().onClick.AddListener(OpenPile);
    }

    public void Add(StoryDataSO.UnlockBeat beat)
    {
        napkins.Add(beat);
        pileVisual.SetActive(true);
        hasUnread = true;
        StartPulse();
    }

    private void OpenPile()
    {
        hasUnread = false;
        StopPulse();
        notePanel.Open(napkins);
    }

    // The tavern screen toggles off while cooking, which kills coroutines — resume on the way back.
    private void OnEnable()
    {
        if (hasUnread) StartPulse();
    }

    private void StartPulse()
    {
        if (!isActiveAndEnabled) return;
        StopPulse();
        pulse = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (pulse != null) { StopCoroutine(pulse); pulse = null; }
        if (unreadGlow) unreadGlow.gameObject.SetActive(false);
        pileVisual.transform.localScale = Vector3.one;
    }

    private IEnumerator PulseRoutine()
    {
        if (unreadGlow) unreadGlow.gameObject.SetActive(true);
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float p = (Mathf.Sin(t / Mathf.Max(0.1f, pulseSeconds) * Mathf.PI * 2f) + 1f) * 0.5f;
            if (unreadGlow)
            {
                var c = unreadGlow.color;
                c.a = Mathf.Lerp(glowAlphaMin, glowAlphaMax, p);
                unreadGlow.color = c;
            }
            pileVisual.transform.localScale = Vector3.one * Mathf.Lerp(1f, pulseScale, p);
            yield return null;
        }
    }
}
