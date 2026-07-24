using UnityEngine;

// Three one-line hints, each gone forever after the first successful serve.
// Fully self-owned: subscribes to GameManager.OnStateChanged, GameManager never references this.
public class HintSystem : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("Hint labels (TMP texts, start hidden)")]
    [SerializeField] private GameObject tavernHint;    // "Read their outfit... then click the menu book!"
    [SerializeField] private GameObject kitchenHint;   // "Match the stew to what they're wearing."
    [SerializeField] private GameObject serveHint;     // "Click the customer to hand it over!"

    private bool firstServeDone;

    private void Awake() => gameManager.OnStateChanged += Handle;

    private void Handle(GameState s)
    {
        if (firstServeDone) return;
        tavernHint.SetActive(s == GameState.Ordering);
        kitchenHint.SetActive(s == GameState.Cooking);
        serveHint.SetActive(s == GameState.Delivering);
        if (s == GameState.Reacting)
        {
            firstServeDone = true;
            tavernHint.SetActive(false);
            kitchenHint.SetActive(false);
            serveHint.SetActive(false);
        }
    }
}
