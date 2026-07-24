using System;
using UnityEngine;
using UnityEngine.UI;

// One of only two gameplay scripts with Update() (the other is NightClock).
// MUST live under Systems (always active) — a script under a disabled canvas stops ticking.
public class PatienceMeter : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GameConfigSO config;
    [SerializeField] private TavernFeelSO feel;

    [Header("Spine")]
    [SerializeField] private GameManager gameManager;   // read Paused only

    [Header("Display")]
    [SerializeField] private GameObject tavernBarRoot;  // whole bar, hidden when idle
    [SerializeField] private Image tavernFill;          // filled image on the tavern screen
    [Tooltip("Optional second fill next to the kitchen portrait — pressure follows you in")]
    [SerializeField] private Image kitchenFill;

    public event Action OnPatienceEmpty;
    public float Fraction { get; private set; } = 1f;

    private bool draining;

    public void StartDraining()
    {
        Fraction = 1f;
        draining = true;
        if (tavernBarRoot) tavernBarRoot.SetActive(true);
        Refresh();
    }

    public void StopDraining()
    {
        draining = false;
        if (tavernBarRoot) tavernBarRoot.SetActive(false);
    }

    private void Update()
    {
        if (!draining || gameManager.Paused) return;   // napkin open = patience frozen too
        Fraction -= Time.deltaTime / config.patienceSeconds;
        if (Fraction <= 0f)
        {
            Fraction = 0f;
            draining = false;
            Refresh();
            OnPatienceEmpty?.Invoke();
            return;
        }
        Refresh();
    }

    private void Refresh()
    {
        Color c = Fraction < feel.warningThreshold ? feel.barColorWarning : feel.barColorFull;
        if (tavernFill)  { tavernFill.fillAmount  = Fraction; tavernFill.color  = c; }
        if (kitchenFill) { kitchenFill.fillAmount = Fraction; kitchenFill.color = c; }
    }
}
