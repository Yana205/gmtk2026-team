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

    [Header("Shelf — all 10 jars")]
    [SerializeField] private List<IngredientJar> jars;

    [Header("Flying ingredient — spare Image under the kitchen canvas, disabled by default")]
    [SerializeField] private Image flyer;
    [SerializeField] private Canvas rootCanvas;        // for arc height scaling

    [Header("Serve")]
    [SerializeField] private Button serveButton;
    [Tooltip("Label inside the serve button — grayed together with the frame while the pot is empty")]
    [SerializeField] private TMP_Text serveLabel;
    [SerializeField] private Color serveBgDisabled    = new Color(0.42f, 0.38f, 0.35f, 1f);
    [SerializeField] private Color serveLabelDisabled = new Color(0.72f, 0.69f, 0.65f, 0.55f);

    [Header("Pick feedback — shelf headers count 0/1 per slot, pot caption lists the contents")]
    [SerializeField] private TMP_Text mainHeader;
    [SerializeField] private TMP_Text sideHeader;
    [SerializeField] private TMP_Text sauceHeader;
    [SerializeField] private TMP_Text potCaption;
    [SerializeField] private Color headerDoneColor = new Color(1f, 0.82f, 0.35f, 1f);

    [Header("Portrait — second BustDresser instance, small, top corner")]
    [SerializeField] private BustDresser portrait;

    [Header("Steam — optional, simmers when empty and ramps with each pick")]
    [SerializeField] private PotSteam steam;

    private readonly Dictionary<SlotType, IngredientSO> picks = new Dictionary<SlotType, IngredientSO>();
    private Vector2 potHome;
    private bool serveWasOn;
    private Image serveImage;
    private Color serveBgColor, serveLabelColor;   // authored "ready" colors, captured in Awake
    private string headerBaseMain, headerBaseSide, headerBaseSauce;
    private Color headerRestColor;

    private void Awake()
    {
        potHome = potRect.anchoredPosition;
        serveButton.onClick.AddListener(() => gameManager.SubmitDish(picks));
        // Serve button gray-out: we drive the colors ourselves, so neutralize the ColorTint's
        // own disabled fade (it only touches the frame and would double-dim it).
        serveImage = serveButton.GetComponent<Image>();
        if (serveImage) serveBgColor = serveImage.color;
        if (serveLabel) serveLabelColor = serveLabel.color;
        var tint = serveButton.colors;
        tint.disabledColor = Color.white;
        serveButton.colors = tint;
        // Remember the authored header/caption text so the counters append to it instead of replacing it.
        if (mainHeader)  { headerBaseMain  = mainHeader.text;  headerRestColor = mainHeader.color; }
        if (sideHeader)    headerBaseSide  = sideHeader.text;
        if (sauceHeader)   headerBaseSauce = sauceHeader.text;
        Clear();
    }

    // Locking waits until Start so every IngredientJar.Awake (which paints the unlocked look) has run.
    // Catch-of-the-day jars stay on the shelf as black silhouettes until their visitor drops them off.
    private void Start()
    {
        foreach (var jar in jars)
            jar.SetLocked(jar.Ingredient.startsLocked && !(debug && debug.AllJarsOn));
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
            if (jar.Ingredient == ing) jar.SetLocked(false);
    }

    public void Select(IngredientSO ing, RectTransform fromJar)
    {
        picks[ing.slot] = ing;                          // 1. state first — same slot overwrites
        RedrawPot();                                    // 2. pot shows the truth
        RefreshPickFeedback();                          //    ...and so do the jars + headers
        StartCoroutine(FlyRoutine(ing, fromJar));       // 3. fire-and-forget juice
        RefreshServeGate();                             // 4. gate
    }

    public void Clear()
    {
        picks.Clear();
        RedrawPot();
        RefreshPickFeedback();
        serveWasOn = false;
        serveButton.interactable = config.minSlotsToServe == 0;
        ApplyServeVisuals(serveButton.interactable);
    }

    // One frame per slot: the picked jar wears the gold frame, its header flips to "1/1",
    // and the pot caption spells out exactly what's inside.
    private void RefreshPickFeedback()
    {
        foreach (var jar in jars)
        {
            var ing = jar.Ingredient;
            jar.SetSelected(ing && picks.TryGetValue(ing.slot, out var picked) && picked == ing);
        }
        RefreshHeader(mainHeader,  headerBaseMain,  SlotType.Main);
        RefreshHeader(sideHeader,  headerBaseSide,  SlotType.Side);
        RefreshHeader(sauceHeader, headerBaseSauce, SlotType.Sauce);
        if (steam) steam.SetIntensity(Mathf.Lerp(0.25f, 1f, picks.Count / 3f));   // simmer -> boil
        if (potCaption)
        {
            // Always spell out the 3-slot recipe shape: picked slots show their ingredient,
            // empty slots show a dim "Main?" placeholder — that's how players learn "one of each".
            var sb = new StringBuilder();
            foreach (var slot in new[] { SlotType.Main, SlotType.Side, SlotType.Sauce })
            {
                if (sb.Length > 0) sb.Append("  ·  ");
                if (picks.TryGetValue(slot, out var p) && p)
                    sb.Append(p.displayName);
                else                                       // TMP alpha tag has no closing form — reset by hand
                    sb.Append("<alpha=#55>").Append(slot).Append("?<alpha=#FF>");
            }
            potCaption.text = sb.ToString();
        }
    }

    private void RefreshHeader(TMP_Text header, string baseText, SlotType slot)
    {
        if (!header) return;
        bool done = picks.ContainsKey(slot);
        header.text  = baseText + (done ? "  1/1" : "  0/1");
        header.color = done ? headerDoneColor : headerRestColor;
    }

    private void RefreshServeGate()
    {
        serveButton.interactable = picks.Count >= config.minSlotsToServe;
        ApplyServeVisuals(serveButton.interactable);
        if (serveButton.interactable && !serveWasOn)
            StartCoroutine(Tween.Punch(serveButton.transform, feel.servePopScale, feel.servePopSeconds));
        serveWasOn = serveButton.interactable;
    }

    // Empty pot = ashen, unclickable-looking; first pick brings the ember colors back.
    private void ApplyServeVisuals(bool ready)
    {
        if (serveImage) serveImage.color = ready ? serveBgColor : serveBgDisabled;
        if (serveLabel) serveLabel.color = ready ? serveLabelColor : serveLabelDisabled;
    }

    private void RedrawPot()
    {
        SetLayer(mainLayer,  SlotType.Main);
        SetLayer(sideLayer,  SlotType.Side);
        SetLayer(sauceLayer, SlotType.Sauce);
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
        if (steam) steam.Burst();   // the splash — food hits the stew, the boil answers
        yield return Tween.Hop(potRect, potHome, feel.potBouncePixels, feel.potBounceSeconds, feel.easeCurve);
    }
}
