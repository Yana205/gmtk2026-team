using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Drop-on "this is clickable" juice for any UI button: grows a touch on hover, dips on press.
// Jars have their own hover pop in IngredientJar — don't add this to them.
public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Scale while hovered — 1.06 = 6% bigger")]
    [Range(1f, 1.5f)]     public float hoverScale = 1.06f;
    [Tooltip("Scale while held down — slight dip reads as a physical press")]
    [Range(0.8f, 1f)]     public float pressScale = 0.96f;
    [Range(0.01f, 0.3f)]  public float seconds    = 0.08f;

    private Vector3 baseScale;
    private float target = 1f;
    private float current = 1f;
    private bool hovered, pressed;
    private Selectable selectable;                 // optional — no hover pop while disabled

    private void Awake()
    {
        baseScale = transform.localScale;          // respect hand-authored scales
        selectable = GetComponent<Selectable>();
    }

    private void OnDisable()
    {
        current = target = 1f;
        transform.localScale = baseScale;
        hovered = pressed = false;
    }

    public void OnPointerEnter(PointerEventData e) { hovered = true;  Retarget(); }
    public void OnPointerExit(PointerEventData e)  { hovered = false; pressed = false; Retarget(); }
    public void OnPointerDown(PointerEventData e)  { pressed = true;  Retarget(); }
    public void OnPointerUp(PointerEventData e)    { pressed = false; Retarget(); }

    private void Retarget()
    {
        bool usable = !selectable || selectable.interactable;
        target = !usable ? 1f : pressed ? pressScale : hovered ? hoverScale : 1f;
    }

    private void Update()
    {
        if (Mathf.Approximately(current, target)) return;
        float speed = (hoverScale - pressScale) / seconds;   // full swing takes `seconds`
        current = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        transform.localScale = baseScale * current;
    }
}
