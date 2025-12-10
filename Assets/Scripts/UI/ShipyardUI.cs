using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShipyardUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject repairPanel; // Das Panel für Reparatur
    public GameObject buyPanel;    // Das Panel für den Kauf

    [Header("Kauf UI")]
    public ShipType starterShipType; // Die Schnigge (Daten)
    public TextMeshProUGUI buyInfoText; // "Kauf Schnigge: 1500 Gold"
    public Button buyButton;

    [Header("Reparatur UI")]
    public TMP_InputField nameInput;
    public Slider healthSlider;
    public TextMeshProUGUI costText;
    public Button repairButton;

    void OnEnable()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (PlayerManager.Instance == null) return;

        // ENTSCHEIDUNG: Haben wir ein Schiff?
        if (PlayerManager.Instance.selectedShip == null)
        {
            // --- MODUS: KAUFEN ---
            if (repairPanel) repairPanel.SetActive(false);
            if (buyPanel) buyPanel.SetActive(true);

            if (starterShipType != null)
            {
                int cost = starterShipType.basePrice;
                buyInfoText.text = $"{starterShipType.typeName} kaufen\nKosten: {cost} Gold\nKapazität: {starterShipType.maxCargo}";

                // Button nur aktiv wenn genug Geld
                buyButton.interactable = (PlayerManager.Instance.currentGold >= cost);
            }
        }
        else
        {
            // --- MODUS: REPARIEREN ---
            if (repairPanel) repairPanel.SetActive(true);
            if (buyPanel) buyPanel.SetActive(false);

            Ship ship = PlayerManager.Instance.selectedShip;

            if (nameInput != null) nameInput.text = ship.shipName;

            // Zustand anzeigen
            float hpPercent = (ship.currentHealth / ship.type.maxHealth);
            if (healthSlider != null) healthSlider.value = hpPercent;

            int cost = ship.CalculateRepairCost();
            if (costText != null) costText.text = cost > 0 ? $"Reparatur: {cost} Gold" : "Zustand: Perfekt";

            if (repairButton != null)
                repairButton.interactable = (cost > 0 && PlayerManager.Instance.currentGold >= cost);
        }
    }

    // --- BUTTON EVENTS ---

    public void OnBuyClicked()
    {
        // Wir kaufen in der Stadt, wo wir gerade sind
        City currentCity = UIManager.Instance.currentCity;

        bool success = PlayerManager.Instance.BuyShip(starterShipType, currentCity);

        if (success)
        {
            UIManager.Instance.UpdateGoldDisplay();
            UpdateUI(); // Umschalten auf Reparatur-Ansicht
        }
    }

    public void OnRepairClicked()
    {
        if (PlayerManager.Instance.TryRepairShip())
        {
            UIManager.Instance.UpdateGoldDisplay();
            UpdateUI();
        }
    }

    public void OnRename(string newName)
    {
        PlayerManager.Instance.RenameShip(newName);
    }
}