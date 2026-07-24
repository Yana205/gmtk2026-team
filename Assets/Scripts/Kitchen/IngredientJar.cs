using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One jar = one serialized IngredientSO + one click. Prefab x10; unlock = SetActive(true).
[RequireComponent(typeof(Button))]
public class IngredientJar : MonoBehaviour
{
    [SerializeField] private IngredientSO ingredient;
    [SerializeField] private StewBuilder stewBuilder;
    [SerializeField] private KitchenFeelSO feel;

    [Header("Pieces")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLabel;   // jars are labeled (readability rule)

    public IngredientSO Ingredient => ingredient;

    private void Awake()
    {
        if (icon && ingredient)
        {
            icon.sprite = ingredient.jarSprite;
            icon.color  = ingredient.jarSprite ? Color.white : ingredient.placeholderColor;
        }
        if (nameLabel && ingredient) nameLabel.text = ingredient.displayName;
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        StartCoroutine(Tween.Punch(transform, feel.jarPunchScale, feel.jarPunchSeconds));
        stewBuilder.Select(ingredient, (RectTransform)transform);
    }
}
