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
    [SerializeField] private Image dishImage;            // the bowl — swapped to the Main's whole-dish art
    [SerializeField] private ReactionFX reactionFX;
    [SerializeField] private ToastBanner toast;
    [SerializeField] private NapkinPile napkins;
    [SerializeField] private CatchOfTheDaySign catchSign;   // wooden wall sign, updates on each drop-off
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
    private bool nightOver;

    // ---------- lifecycle ----------
    private void Awake()
    {
        nightClock.OnLastCall    += HandleLastCall;
        nightClock.OnNightEnd    += HandleNightEnd;
    }

    private void Start()
    {
        screens.ShowTavern();
        dishOnCounter.SetActive(false);   // no bowl on the counter until the first dish is served
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
        stewBuilder.BeginCooking(currentOrder, generator.CurrentCharacter);   // portrait follows you in
    }

    public void SubmitDish(Dictionary<SlotType, IngredientSO> picks)
    {
        if (state != GameState.Cooking) return;
        submittedPicks = new Dictionary<SlotType, IngredientSO>(picks); // copy BEFORE clear
        stewBuilder.Clear();
        SetState(GameState.Delivering);
        screens.ShowTavern();
        // The served bowl is the Main's whole-dish art (KRAKEN/RAT/FOX/HAM/ELK FINAL STEW).
        submittedPicks.TryGetValue(SlotType.Main, out var mainPick);
        bool haveDish = dishImage && mainPick && mainPick.dishSprite;
        if (haveDish)
        {
            dishImage.sprite = mainPick.dishSprite;
            dishImage.color  = Color.white;
        }
        if (dishImage) dishImage.enabled = haveDish;   // never leave a blank/stale bowl on the counter
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

    private void HandleNightEnd()
    {
        nightOver = true;
        // Mid-dish grace: cooking/delivering/reacting beats finish first. Waiting customers end now.
        if (state == GameState.CustomerEntering || state == GameState.Ordering)
            EndNight();
    }

    // ---------- the beats (all timing in coroutines + TavernFeelSO) ----------
    private void NextCustomer() => StartCoroutine(NextCustomerRoutine());

    private IEnumerator NextCustomerRoutine()
    {
        SetState(GameState.CustomerEntering);
        if (nightOver) { EndNight(); yield break; }
        // Fixed-visitor night ends when the scripted guest list runs out (Letitia → Caledon → Milog).
        if (generator.FixedMode && !generator.HasNext) { EndNight(); yield break; }
        yield return new WaitForSeconds(config.delayBetweenCustomers);
        currentOrder = generator.Draw();
        yield return customerView.EnterRoutine(currentOrder, generator.CurrentCharacter);
        SetState(GameState.Ordering);   // no per-customer timer — the night clock is the only pressure
    }

    private IEnumerator ReactionRoutine()
    {
        SetState(GameState.Reacting);
        var result = ScoringService.Score(currentOrder, submittedPicks, IsLastCall, config);
        totalHearts += result.hearts;
        totalCoins  += result.coins;
        customersServed++;
        // Named visitor leaves THEIR verbatim napkin; grey-box customers fall back to the pooled lore.
        var ch = generator.CurrentCharacter;
        if (ch != null && !string.IsNullOrEmpty(ch.napkinText))
            napkins.Add(new StoryDataSO.UnlockBeat { hasNapkin = true, guestName = ch.displayName, napkinText = ch.napkinText });
        else
        {
            var lore = story.MakeReactionNapkin(result.hearts, result.coins, customersServed);
            if (lore != null) napkins.Add(lore);
        }
        dishOnCounter.SetActive(false);
        customerView.ShowReaction(result.hearts);
        Coroutine reactionBeat = reactionFX.Play(result.hearts, result.coins);
        yield return new WaitForSeconds(tavernFeel.reactionTotalSeconds); // minimum beat (also covers the 0-heart case, which has no FX)
        yield return reactionBeat;                                        // then guarantee hearts + coin popup fully finished
        yield return customerView.ExitRoutine(result.hearts > 0);
        yield return FireUnlockBeat(customersServed - 1);                 // the visitor drops off today's catch as they leave
        if (nightOver) EndNight(); else NextCustomer();
    }

    // The served visitor leaves their kill behind: reveal that catch of the day (shelf silhouette -> live jar),
    // update the wooden wall sign, and announce it. Beat index is aligned with serve order (0,1,2).
    private IEnumerator FireUnlockBeat(int i)
    {
        if (story.unlockBeats == null || i < 0 || i >= story.unlockBeats.Length) yield break;
        var beat = story.unlockBeats[i];
        if (beat.ingredientToUnlock == null) yield break;
        generator.Unlock(beat.ingredientToUnlock);      // enters the customer pool
        stewBuilder.UnlockJar(beat.ingredientToUnlock); // silhouette becomes a live, clickable jar
        if (catchSign && beat.signArt) catchSign.Show(beat.signArt);   // wall sign shows today's catch
        if (beat.hasNapkin) napkins.Add(beat);          // (off for the fixed visitors — their napkin comes from CharacterSO)
        yield return toast.ShowRoutine(beat.toastLine);
    }

    private void EndNight()
    {
        SetState(GameState.NightEnd);
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
