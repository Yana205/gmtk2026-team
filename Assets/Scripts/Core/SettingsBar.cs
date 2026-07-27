using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tiny always-on settings strip in the overlay corner. One switch for now: the night countdown.
// The timer fights the cozy read-the-guest fantasy, so players can turn the pressure off —
// the night then simply ends when the last scripted guest leaves. Choice persists between runs.
public class SettingsBar : MonoBehaviour
{
    private const string TimerPrefKey = "TavernStew.TimerOff";

    [SerializeField] private NightClock nightClock;
    [SerializeField] private Button timerButton;
    [SerializeField] private TMP_Text timerLabel;

    private void Start()
    {
        timerButton.onClick.AddListener(() => Apply(!nightClock.TimerOff));
        Apply(PlayerPrefs.GetInt(TimerPrefKey, 0) == 1);
    }

    private void Apply(bool off)
    {
        nightClock.SetTimerOff(off);
        PlayerPrefs.SetInt(TimerPrefKey, off ? 1 : 0);
        PlayerPrefs.Save();
        if (timerLabel) timerLabel.text = off ? "Night timer: off" : "Night timer: on";
    }
}
