using UnityEngine;

// The whole debug rig. Untick masterEnabled to kill everything for ship,
// or set the debug slot to None on any script — all access is null-safe.
[CreateAssetMenu(fileName = "DebugConfig", menuName = "Tavern Stew/Debug Config")]
public class DebugConfigSO : ScriptableObject
{
    [Header("MASTER SWITCH — off = zero debug anywhere in the game")]
    public bool masterEnabled = true;

    [Header("Channels")]
    public bool logStateTransitions = true;
    [Tooltip("Tiny on-screen text: state / hearts / coins / clock. Works in WebGL builds too")]
    public bool showOverlay = true;
    [Tooltip("TAB flips tavern/kitchen in Play Mode, ignoring all game locks (layout tool)")]
    public bool screenSwitchHotkey = true;
    [Tooltip("Design mode: night clock stands still; untick in Play Mode when you want time pressure back")]
    public bool freezeTimers = false;

    [Header("Test cheats")]
    [Tooltip("Skip the intro card, night starts immediately")]
    public bool skipIntro = false;
    [Tooltip("All 10 jars + full customer pool from second one — test unlock art fast")]
    public bool unlockAllJarsAtStart = false;
    [Tooltip("1 = normal. 4 = whole night in 45s — test LAST CALL & endings fast")]
    [Range(1f, 10f)] public float nightSpeedMultiplier = 1f;

    // Scripts read through these so the master switch gates everything
    public bool LogsOn      => masterEnabled && logStateTransitions;
    public bool OverlayOn   => masterEnabled && showOverlay;
    public bool HotkeyOn    => masterEnabled && screenSwitchHotkey;
    public bool FreezeOn    => masterEnabled && freezeTimers;
    public bool SkipIntroOn => masterEnabled && skipIntro;
    public bool AllJarsOn   => masterEnabled && unlockAllJarsAtStart;
    public float ClockSpeed => masterEnabled ? nightSpeedMultiplier : 1f;
}
