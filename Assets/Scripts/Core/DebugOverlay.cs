using TMPro;
using UnityEngine;

// Self-hiding on-screen debug panel + TAB screen-switch hotkey.
// Lives on the always-active OverlayCanvas. Null-safe: no DebugConfig assigned = fully inert.
public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private DebugConfigSO debug;
    [SerializeField] private GameManager gm;
    [SerializeField] private NightClock clock;
    [SerializeField] private ScreenManager screens;
    [SerializeField] private TMP_Text label;         // tiny text, corner of screen

    private void Update()
    {
        if (debug && debug.HotkeyOn && Input.GetKeyDown(KeyCode.Tab))
            screens.Toggle();

        bool on = debug && debug.OverlayOn;
        if (label.enabled != on) label.enabled = on;
        if (!on) return;
        label.text = gm.State + " | H" + gm.TotalHearts + " | $" + gm.TotalCoins
                   + " | served " + gm.CustomersServed + " | " + Mathf.CeilToInt(clock.SecondsLeft) + "s";
    }
}
