using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The kitchen's state + visuals. Order of operations in Select(): state -> pot truth -> juice -> gate.
// No Update() loops — everything is click-driven.
public class StewBuilder : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GameConfigSO config;
    [SerializeField] private KitchenFeelSO feel;
    [SerializeField] private DebugConfigSO debug;      // optional — AllJars cheat

    [Header("Spine")]
    [SerializeField] private GameManager gameManager;

    [Header("Pot — one overlay Image per slot (only shown when real stew art exists)")]
    [SerializeField] private Image mainLayer;
    [SerializeField] private Image sideLayer;
    [SerializeField] private Image sauceLayer;
    [SerializeField] private RectTransform potRect;
    [Tooltip("Text list of what's been added — replaces the greybox colored rectangles")]
    [SerializeField] private TMP_Text potContents;

    [Header("Shelf — all 10 jars")]
    [SerializeField] private List<IngredientJar> jars;

    [Header("Flying ingredient — spare Image under the kitchen canvas, disabled by default")]
    [SerializeField] private Image flyer;
    [SerializeField] private Canvas rootCanvas;        // for arc height scaling

    [Header("Serve")]
    [SerializeField] private Button serveButton;

    [Header("Portrait — second BustDresser instance, small, top corner")]
    [SerializeField] private BustDresser portrait;

    private readonly Dictionary<SlotType, IngredientSO> picks = new Dictionary<SlotType, IngredientSO>();
    private Vector2 potHome;
    private bool serveWasOn;

    private void Awake()
    {
        potHome = potRect.anchoredPosition;
        serveButton.onClick.AddListener(() => gameManager.SubmitDish(picks));
        foreach (var jar in jars)
            jar.gameObject.SetActive(!jar.Ingredient.startsLocked || (debug && debug.AllJarsOn));
        Clear();
    }

    public void BeginCooking(Dictionary<SlotType, IngredientSO> order, CharacterSO character)
    {
        // The customer follows you into the kitchen. Real visitors show their portrait (their look is
        // the only clue — the dressed answer would give the recipe away); grey-box falls back to dressing.
        if (character != null) portrait.ShowPortrait(character.portrait, new Color(0.6f, 0.6f, 0.65f, 1f));
        else                   portrait.Dress(order);
    }

    public void UnlockJar(IngredientSO ing)
    {
        foreach (var jar in jars)
            if (jar.Ingredient == ing) jar.gameObject.SetActive(true);
    }

    public void Select(IngredientSO ing, RectTransform fromJar)
    {
        picks[ing.slot] = ing;                          // 1. state first — same slot overwrites
        RedrawPot();                                    // 2. pot shows the truth
        StartCoroutine(FlyRoutine(ing, fromJar));       // 3. fire-and-forget juice
        RefreshServeGate();                             // 4. gate
    }

    public void Clear()
    {
        picks.Clear();
        RedrawPot();
        serveWasOn = false;
        serveButton.interactable = config.minSlotsToServe == 0;
    }

    private void RefreshServeGate()
    {
        serveButton.interactable = picks.Count >= config.minSlotsToServe;
        if (serveButton.interactable && !serveWasOn)
            StartCoroutine(Tween.Punch(serveButton.transform, feel.servePopScale, feel.servePopSeconds));
        serveWasOn = serveButton.interactable;
    }

    private void RedrawPot()
    {
        SetLayer(mainLayer,  SlotType.Main);
        SetLayer(sideLayer,  SlotType.Side);
        SetLayer(sauceLayer, SlotType.Sauce);
        RedrawContents();
    }

    // Simple text of what's in the pot — the greybox rectangles are gone.
    private void RedrawContents()
    {
        if (!potContents) return;
        var sb = new StringBuilder();
        AppendPick(sb, SlotType.Main);
        AppendPick(sb, SlotType.Side);
        AppendPick(sb, SlotType.Sauce);
        potContents.text = sb.Length == 0 ? "<i>empty pot</i>" : sb.ToString().TrimEnd();
    }

    private void AppendPick(StringBuilder sb, SlotType slot)
    {
        if (picks.TryGetValue(slot, out var ing) && ing) sb.AppendLine("+ " + ing.displayName);
    }

    // Only ever show a pot overlay when there is REAL stew art — never a placeholder rectangle.
    private void SetLayer(Image img, SlotType slot)
    {
        bool hasArt = picks.TryGetValue(slot, out var ing) && ing && ing.stewSprite;
        if (img) img.enabled = hasArt;
        if (!hasArt) return;
        img.sprite = ing.stewSprite;
        img.color  = Color.white;
    }

    private IEnumerator FlyRoutine(IngredientSO ing, RectTransform fromJar)
    {
        // Known simplification: rapid clicks share one flyer — last click wins the visual. Jam-fine.
        flyer.sprite = ing.jarSprite;
        flyer.color  = ing.jarSprite ? Color.white : ing.placeholderColor;
        flyer.gameObject.SetActive(true);
        Vector3 from = fromJar.position;
        Vector3 to   = potRect.position;
        float height = feel.arcHeight * (rootCanvas ? rootCanvas.scaleFactor : 1f);
        yield return Tween.ArcWorld(flyer.rectTransform, from, to, height, feel.arcSeconds, feel.easeCurve);
        flyer.gameObject.SetActive(false);
        yield return Tween.Hop(potRect, potHome, feel.potBouncePixels, feel.potBounceSeconds, feel.easeCurve);
    }
}
