using UnityEngine;

// Deliberately dumb. Never decides WHEN — only GameManager (or the debug hotkey) calls it.
public class ScreenManager : MonoBehaviour
{
    [SerializeField] private GameObject tavernScreen;   // root canvas 1
    [SerializeField] private GameObject cookingScreen;  // root canvas 2

    public void ShowTavern()  { tavernScreen.SetActive(true);  cookingScreen.SetActive(false); }
    public void ShowKitchen() { cookingScreen.SetActive(true); tavernScreen.SetActive(false); }

    // Debug layout tool (TAB) — moves the view, not the game state
    public void Toggle()
    {
        if (tavernScreen.activeSelf) ShowKitchen(); else ShowTavern();
    }
}
