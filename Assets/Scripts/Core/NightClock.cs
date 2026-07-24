using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The theme carrier. A dumb timer that announces and displays — never decides.
// Lives under Systems (always active) so it keeps ticking while the kitchen is shown.
public class NightClock : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GameConfigSO config;
    [SerializeField] private DebugConfigSO debug;      // optional — speed cheat

    [Header("Spine")]
    [SerializeField] private GameManager gameManager;  // read Paused only

    [Header("Display (safe to leave empty in early grey-box)")]
    [SerializeField] private TMP_Text timeLabel;       // "2:47"
    [Tooltip("Image with a Fill Method set — drains 1 -> 0 over the night (the candle)")]
    [SerializeField] private Image candleFill;

    public event Action      OnLastCall;
    public event Action      OnNightEnd;
    public event Action<int> OnUnlock;                 // index into config.unlockTimes

    public float SecondsLeft { get; private set; }
    public bool  Running     { get; private set; }

    private bool lastCallFired;
    private int  nextUnlockIndex;

    public void Begin()
    {
        SecondsLeft     = config.nightSeconds;
        nextUnlockIndex = 0;
        lastCallFired   = false;
        Running         = true;
    }

    private void Update()
    {
        if (!Running) return;
        if (gameManager.Paused) return;   // napkin open — the world holds its breath

        SecondsLeft -= Time.deltaTime * (debug ? debug.ClockSpeed : 1f);

        // while-loop so a fast-forwarded frame can't skip an unlock
        float elapsed = config.nightSeconds - SecondsLeft;
        while (nextUnlockIndex < config.unlockTimes.Length
               && elapsed >= config.unlockTimes[nextUnlockIndex])
        {
            OnUnlock?.Invoke(nextUnlockIndex);
            nextUnlockIndex++;
        }

        if (!lastCallFired && SecondsLeft <= config.lastCallSeconds)
        {
            lastCallFired = true;
            OnLastCall?.Invoke();
        }

        if (SecondsLeft <= 0f)
        {
            SecondsLeft = 0f;
            Running = false;
            RefreshDisplay();
            OnNightEnd?.Invoke();
            return;
        }

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (timeLabel)
        {
            int m = Mathf.FloorToInt(SecondsLeft / 60f);
            int s = Mathf.FloorToInt(SecondsLeft % 60f);
            timeLabel.text = m + ":" + s.ToString("00");
        }
        if (candleFill) candleFill.fillAmount = SecondsLeft / config.nightSeconds;
    }
}
