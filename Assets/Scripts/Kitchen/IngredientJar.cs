using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One jar = one serialized IngredientSO + one click. Prefab x10; unlock = SetActive(true).
// The ingredient name is a hover tooltip (hidden until the pointer is over the jar).
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

    public IngredientSO Ingredient => ingredient;

    private void Awake()
    {
        if (icon && ingredient)
        {
            icon.sprite = ingredient.jarSprite;
            icon.color  = ingredient.jarSprite ? Color.white : ingredient.placeholderColor;
        }
        if (nameLabel)
        {
            nameLabel.text = ingredient ? ingredient.displayName : "";
            nameLabel.enabled = false;             // hidden until hover
        }
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (nameLabel) nameLabel.enabled = true;
        transform.localScale = Vector3.one * HoverScale;
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (nameLabel) nameLabel.enabled = false;
        transform.localScale = Vector3.one;
    }

    private void OnClick()
    {
        StartCoroutine(Tween.Punch(transform, feel.jarPunchScale, feel.jarPunchSeconds));
        stewBuilder.Select(ingredient, (RectTransform)transform);
    }
}
