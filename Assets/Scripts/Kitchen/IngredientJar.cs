using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One jar = one serialized IngredientSO + one click. Prefab x10; catch-of-the-day jars start
// LOCKED (a black silhouette, not clickable) and unlock = SetLocked(false) when their visitor leaves.
// The ingredient name is a hover tooltip (hidden until the pointer is over the jar; "???" while locked).
[RequireComponent(typeof(Button))]
public class IngredientJar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    float HoverScale => feel ? feel.jarHoverScale : 1.1f;

    [SerializeField] private IngredientSO ingredient;
    [SerializeField] private StewBuilder stewBuilder;
    [SerializeField] private KitchenFeelSO feel;

    [Header("Pieces")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLabel;   // shown on hover only
    [Tooltip("Gold frame shown while this jar is the current pick for its slot")]
    [SerializeField] private GameObject selectedFrame;

    [Header("Locked look")]
    [Tooltip("Silhouette tint for a not-yet-unlocked catch of the day")]
    [SerializeField] private Color lockedTint = new Color(0.05f, 0.05f, 0.07f, 1f);

    private bool locked;
    private bool hovered;
    private bool selected;

    public IngredientSO Ingredient => ingredient;

    private void Awake()
    {
        ApplyIcon();                               // unlocked look by default
        if (nameLabel)
        {
            nameLabel.text = ingredient ? ingredient.displayName : "";
            nameLabel.enabled = false;             // hidden until hover
        }
        GetComponent<Button>().onClick.AddListener(OnClick);
        if (selectedFrame) selectedFrame.SetActive(false);
    }

    // The "it's in the pot" marker — StewBuilder turns exactly one on per slot.
    // A selected jar also keeps its name label pinned on, so you can read what you picked.
    public void SetSelected(bool value)
    {
        selected = value;
        if (selectedFrame) selectedFrame.SetActive(value);
        RefreshLabel();
    }

    // Locked = black silhouette of the jar art + not clickable. Unlocked = full colour + clickable.
    public void SetLocked(bool value)
    {
        locked = value;
        GetComponent<Button>().interactable = !value;
        ApplyIcon();
        if (locked) transform.localScale = Vector3.one;   // cancel any lingering hover pop
    }

    private void ApplyIcon()
    {
        if (!icon) return;
        icon.sprite = ingredient ? ingredient.jarSprite : null;
        if (locked)
            icon.color = lockedTint;               // shape only, blacked out
        else if (ingredient)
            icon.color = ingredient.jarSprite ? Color.white : ingredient.placeholderColor;
    }

    // Label shows while hovered OR while this jar is the current pick for its slot.
    private void RefreshLabel()
    {
        if (!nameLabel) return;
        bool show = hovered || (selected && !locked);
        if (show) nameLabel.text = locked ? "???" : (ingredient ? ingredient.displayName : "");
        nameLabel.enabled = show;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hovered = true;
        RefreshLabel();
        if (!locked) transform.localScale = Vector3.one * HoverScale;   // no pop on a locked jar
    }

    public void OnPointerExit(PointerEventData e)
    {
        hovered = false;
        RefreshLabel();
        transform.localScale = Vector3.one;
    }

    private void OnClick()
    {
        if (locked) return;                        // belt-and-braces; Button is already non-interactable
        StartCoroutine(Tween.Punch(transform, feel.jarPunchScale, feel.jarPunchSeconds));
        stewBuilder.Select(ingredient, (RectTransform)transform);
    }
}
