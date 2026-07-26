using UnityEngine;
using UnityEngine.UI;

// Makes the menu book read as clickable: a gentle breathing pulse whenever its Button is
// interactable (Ordering, or peeking back at the guest mid-cook). Still while disabled.
[RequireComponent(typeof(Button))]
public class MenuBookAttention : MonoBehaviour
{
    [SerializeField] private float pulseScale = 1.07f;
    [SerializeField] private float pulseSeconds = 1.1f;

    private Button button;
    private Vector3 baseScale;
    private float t;

    private void Awake()
    {
        button = GetComponent<Button>();
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        t = 0f;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        if (button.interactable)
        {
            t += Time.deltaTime;
            float p = (Mathf.Sin(t / pulseSeconds * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.localScale = baseScale * Mathf.Lerp(1f, pulseScale, p);
        }
        else if (t != 0f)
        {
            t = 0f;
            transform.localScale = baseScale;
        }
    }
}
