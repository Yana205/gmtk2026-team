using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameState { Intro, CustomerEntering, Ordering, Cooking, Delivering, Reacting, NightEnd }

// THE single brain. Everyone reports up; only this file changes state.
public class GameManager : MonoBehaviour
{
    [Header("Data assets")]
    [SerializeField] private GameConfigSO config;
    [SerializeField] private TavernFeelSO tavernFeel;
    [SerializeField] private StoryDataSO  story;

    [Header("Debug (optional — empty slot = no debug anywhere)")]
    [SerializeField] private DebugConfigSO debug;

    [Header("Spine")]
    [SerializeField] private ScreenManager screens;
    [SerializeField] private NightClock nightClock;

    [Header("Tavern pieces")]
    [SerializeField] private CustomerGenerator generator;
    [SerializeField] private CustomerView customerView;
    [SerializeField] private GameObject dishOnCounter;   // dish + mead mug parent
    [SerializeField] private ReactionFX reactionFX;
    [SerializeField] private PatienceMeter patience;
    [SerializeField] private ToastBanner toast;
    [SerializeField] private NapkinPile napkins;
    [SerializeField] private EndScreen endScreen;
    [SerializeField] private GameObject introPanel;

    [Header("Kitchen pieces")]
    [SerializeField] private StewBuilder stewBuilder;

    [Header("Buttons gated by state (Disabled Color on each Button = the dim look)")]
    [SerializeField] private Button menuBookButton;
    [SerializeField] private Button customerButton;

    [Header("Live debug view — read-only while playing, never edit")]
    [SerializeField] private GameState state;
    [SerializeField] private int totalHearts;
    [SerializeField] private int totalCoins;
    [SerializeField] private int customersServed;

    public event Action<GameState> OnStateChanged;

    public GameState State    => state;
    public bool Paused        { get; private set; }   // napkin open
    public bool IsLastCall    { get; private set; }
    public int TotalHearts     => totalHearts;
    public int TotalCoins      => totalCoins;
    public int CustomersServed => customersServed;

    private Dictionary<SlotType, IngredientSO> currentOrder;    // the secret craving
    private Dictionary<SlotType, IngredientSO> submittedPicks;  // what you cooked
    private readonly List<int> pendingUnlocks = new List<int>();
    private bool nightOver;

    // ---------- lifecycle ----------
    private void Awake()
    {
        nightClock.OnLastCall    += HandleLastCall;
        nightClock.OnNightEnd    += HandleNightEnd;
        nightClock.OnUnlock      += HandleUnlock;
        patience.OnPatienceEmpty += HandlePatienceEmpty;
    }

    private void Start()
    {
        screens.ShowTavern();
        SetState(GameState.Intro);   // intro panel active in scene by default
        if (debug && debug.SkipIntroOn) StartNight();
    }

    // Wired to the intro card's Start button — also the WebGL audio unlock click
    public void StartNight()
    {
        introPanel.SetActive(false);
        nightClock.Begin();
        NextCustomer();
    }

    // ---------- reports from the world (the ONLY entry points) ----------
    public void OnMenuBookClicked()
    {
        if (state != GameState.Ordering) return;
        SetState(GameState.Cooking);
        screens.ShowKitchen();
        stewBuilder.BeginCooking(currentOrder);   // dresses the corner portrait
    }

    public void SubmitDish(Dictionary<SlotType, IngredientSO> picks)
    {
        if (state != GameState.Cooking) return;
        submittedPicks = new Dictionary<SlotType, IngredientSO>(picks); // copy BEFORE clear
        stewBuilder.Clear();
        SetState(GameState.Delivering);
        screens.ShowTavern();
        dishOnCounter.SetActive(true);            // dish + mead mug together
    }

    public void OnCustomerClicked()
    {
        if (state != GameState.Delivering) return;
        StartCoroutine(ReactionRoutine());
    }

    public void SetPaused(bool value) => Paused = value;   // NotePanel open/close

    // ---------- clock announcements ----------
    private void HandleLastCall()
    {
        IsLastCall = true;
        StartCoroutine(toast.ShowRoutine(story.lastCallBanner));
    }

    private void HandleUnlock(int index) => pendingUnlocks.Add(index);

    private void HandleNightEnd()
    {
        nightOver = true;
        // Mid-dish grace: cooking/delivering/reacting beats finish first. Waiting customers end now.
        if (state == GameState.CustomerEntering || state == GameState.Ordering)
            EndNight();
    }

    private void HandlePatienceEmpty()
    {
        if (state != GameState.Ordering && state != GameState.Cooking
            && state != GameState.Delivering) return;
        StartCoroutine(AngryLeaveRoutine());
    }

    // ---------- the beats (all timing in coroutines + TavernFeelSO) ----------
    private void NextCustomer() => StartCoroutine(NextCustomerRoutine());

    private IEnumerator NextCustomerRoutine()
    {
        SetState(GameState.CustomerEntering);
        yield return ConsumeUnlockBeats();          // unlocks fire ONLY between customers
        if (nightOver) { EndNight(); yield break; }
        yield return new WaitForSeconds(config.delayBetweenCustomers);
        currentOrder = generator.Draw();
        yield return customerView.EnterRoutine(currentOrder);
        patience.StartDraining();
        SetState(GameState.Ordering);
    }

    private IEnumerator ReactionRoutine()
    {
        SetState(GameState.Reacting);
        patience.StopDraining();
        var result = ScoringService.Score(currentOrder, submittedPicks,
                                          patience.Fraction, IsLastCall, config);
        totalHearts += result.hearts;
        totalCoins  += result.coins;
        customersServed++;
        dishOnCounter.SetActive(false);
        customerView.ShowReaction(result.hearts);
        reactionFX.Play(result.hearts, result.coins);
        yield return new WaitForSeconds(tavernFeel.reactionTotalSeconds);
        yield return customerView.ExitRoutine(result.hearts > 0);
        if (nightOver) EndNight(); else NextCustomer();
    }

    private IEnumerator AngryLeaveRoutine()
    {
        bool wasCooking = state == GameState.Cooking;
        SetState(GameState.Reacting);
        patience.StopDraining();
        if (wasCooking) { screens.ShowTavern(); stewBuilder.Clear(); }
        dishOnCounter.SetActive(false);
        customerView.ShowReaction(0);               // annoyed face, zero hearts
        yield return new WaitForSeconds(tavernFeel.reactionTotalSeconds);
        yield return customerView.ExitRoutine(false);
        if (nightOver) EndNight(); else NextCustomer();
    }

    private IEnumerator ConsumeUnlockBeats()
    {
        foreach (int i in pendingUnlocks)
        {
            if (i >= story.unlockBeats.Length) continue;   // index-alignment safety clamp
            var beat = story.unlockBeats[i];
            generator.Unlock(beat.ingredientToUnlock);     // enters the customer pool
            stewBuilder.UnlockJar(beat.ingredientToUnlock);// jar appears on the shelf
            if (beat.hasNapkin) napkins.Add(beat);         // napkin lands on the desk
            yield return toast.ShowRoutine(beat.toastLine);
        }
        pendingUnlocks.Clear();
    }

    private void EndNight()
    {
        SetState(GameState.NightEnd);
        patience.StopDraining();
        int rankIndex = ScoringService.RankIndex(totalHearts, config);
        endScreen.Show(customersServed, totalHearts, totalCoins,
                       rankIndex, story.endingLinesByRank[rankIndex]);
    }

    private void SetState(GameState next)
    {
        state = next;
        if (menuBookButton) menuBookButton.interactable = state == GameState.Ordering;
        if (customerButton) customerButton.interactable = state == GameState.Delivering;
        OnStateChanged?.Invoke(state);
        if (debug && debug.LogsOn) Debug.Log("[GameManager] -> " + state);
    }
}
