using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShipyardUI : MonoBehaviour
{
    [Header("--- AREAS ---")]
    public GameObject buildArea;
    public GameObject queueArea;
    public GameObject repairArea;
    public GameObject sellArea;
    public GameObject upgradeArea;

    [Header("--- DATEN ---")]
    public List<ShipType> availableShipTypes;
    private int currentSelectionIndex = 0;

    // ========================================================
    // LINKE SEITE: SCHIFF & WERTE
    // ========================================================
    [Header("--- UI LINKS (Schiff) ---")]
    public Image shipImage;
    public TextMeshProUGUI shipNameText;
    public Button nextButton;
    public Button prevButton;

    [Header("--- UI LINKS (Die 6 Icons) ---")]
    public TextMeshProUGUI statCargoText;      // Laderaum
    public TextMeshProUGUI statManeuverText;   // Wendigkeit
    public TextMeshProUGUI statHealthText;     // Trefferpunkte
    public TextMeshProUGUI statCostText;       // Betriebskosten
    public TextMeshProUGUI statSlotsText;      // Upgrade Slots
    public TextMeshProUGUI statRiverText;      // Meer/Fluss

    // ========================================================
    // RECHTE SEITE: MATERIAL & KOSTEN
    // ========================================================
    [Header("--- UI RECHTS (Baustelle) ---")]
    public Transform materialListParent;
    public GameObject materialItemPrefab;

    // HIER GEÄNDERT: Kein "Arbeitslohn" Text mehr
    public TextMeshProUGUI materialCostText;   // "Material: 400"
    public TextMeshProUGUI totalCostText;      // "Gesamt: 1900"
    public TextMeshProUGUI timeText;           // "Dauer: 14 Tage"

    public Toggle useOwnMaterialsToggle;
    public Button buildButton;

    // ========================================================
    // ANDERE TABS
    // ========================================================
    [Header("--- ANDERE TABS ---")]
    public TextMeshProUGUI queueListText;

    public TextMeshProUGUI genericInfoText;
    public TextMeshProUGUI genericCostText;
    public Button genericActionButton;

    private void Start()
    {
        ShowArea_Build();
        if (useOwnMaterialsToggle != null)
            useOwnMaterialsToggle.onValueChanged.AddListener(delegate { UpdateBuildUI(); });
    }

    private void Update()
    {
        if (queueArea != null && queueArea.activeSelf) UpdateQueueUI();
    }

    public void ShowArea_Build() { ActivateArea(buildArea); UpdateBuildUI(); }
    public void ShowArea_Queue() { ActivateArea(queueArea); UpdateQueueUI(); }
    public void ShowArea_Repair() { ActivateArea(repairArea); UpdateRepairUI(); }
    public void ShowArea_Sell() { ActivateArea(sellArea); UpdateSellUI(); }
    public void ShowArea_Upgrade() { ActivateArea(upgradeArea);/* UpdateUpgradeUI(); */ }

    private void ActivateArea(GameObject areaToActive)
    {
        if (buildArea) buildArea.SetActive(false);
        if (queueArea) queueArea.SetActive(false);
        if (repairArea) repairArea.SetActive(false);
        if (sellArea) sellArea.SetActive(false);
        if (upgradeArea) upgradeArea.SetActive(false);

        if (areaToActive) areaToActive.SetActive(true);
    }

    // ========================================================
    // HAUPTFUNKTION: BUILD UI UPDATE
    // ========================================================
    private void UpdateBuildUI()
    {
        if (availableShipTypes == null || availableShipTypes.Count == 0) return;
        currentSelectionIndex = Mathf.Clamp(currentSelectionIndex, 0, availableShipTypes.Count - 1);
        ShipType type = availableShipTypes[currentSelectionIndex];

        City city = UIManager.Instance.currentCity;
        bool useOwn = useOwnMaterialsToggle != null && useOwnMaterialsToggle.isOn;

        // --- LINKS: SCHIFF ---
        if (shipNameText) shipNameText.text = type.typeName;
        if (shipImage) shipImage.sprite = type.icon;

        // Die 6 Status Werte setzen
        if (statCargoText) statCargoText.text = $"{type.maxCargo}";
        if (statManeuverText) statManeuverText.text = $"{type.maneuverability}%";
        if (statHealthText) statHealthText.text = $"{type.maxHealth}";
        if (statCostText) statCostText.text = $"{type.dailyMaintenance}";
        if (statSlotsText) statSlotsText.text = $"{type.upgradeSlots}";
        if (statRiverText) statRiverText.text = type.isRiverCapable ? "Meer/Fluss" : "Nur Meer";

        if (nextButton)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => { currentSelectionIndex++; UpdateBuildUI(); });
            nextButton.interactable = currentSelectionIndex < availableShipTypes.Count - 1;
        }
        if (prevButton)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(() => { currentSelectionIndex--; UpdateBuildUI(); });
            prevButton.interactable = currentSelectionIndex > 0;
        }

        // --- RECHTS: MATERIAL & KOSTEN ---
        if (materialListParent != null && materialItemPrefab != null)
        {
            foreach (Transform child in materialListParent) Destroy(child.gameObject);

            foreach (var req in type.requiredResources)
            {
                GameObject item = Instantiate(materialItemPrefab, materialListParent);

                Image iconImg = item.transform.Find("Icon")?.GetComponent<Image>();
                TextMeshProUGUI amountTxt = item.transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();

                if (iconImg) iconImg.sprite = req.icon;

                int inKontor = city.GetKontorStock(req.wareName);
                int inMarket = city.GetMarketStock(req.wareName);

                bool hasEnough = false;
                if (useOwn) hasEnough = (inKontor + inMarket) >= req.amount;
                else hasEnough = inMarket >= req.amount;

                if (amountTxt)
                {
                    amountTxt.text = $"{req.amount}";
                    amountTxt.color = hasEnough ? Color.white : Color.red;
                }
            }
        }

        var calculation = PlayerManager.Instance.CalculateBuildCost(type, city, useOwn);

        // Berechnung: Gesamt - Basispreis = Materialkosten
        int materialCosts = calculation.totalCost - type.baseBuildPrice;

        // HIER GEÄNDERT: Wir zeigen nur noch Material, Gesamt und Zeit an
        if (materialCostText) materialCostText.text = $"Material: {materialCosts}";
        if (totalCostText) totalCostText.text = $"Gesamt: {calculation.totalCost}";
        if (timeText) timeText.text = $"Bauzeit: {type.buildTimeDays} Tage";

        if (buildButton)
        {
            buildButton.interactable = calculation.canBuild;
            buildButton.onClick.RemoveAllListeners();
            buildButton.onClick.AddListener(() => {
                if (PlayerManager.Instance.OrderShip(type, city, useOwn))
                {
                    UIManager.Instance.UpdateGoldDisplay();
                    ShowArea_Queue();
                }
            });
        }
    }

    // --- QUEUE ---
    private void UpdateQueueUI()
    {
        if (PlayerManager.Instance.buildQueue.Count == 0)
        {
            if (queueListText) queueListText.text = "Keine laufenden Aufträge.";
        }
        else
        {
            string content = "<b>Laufende Aufträge:</b>\n\n";
            foreach (var order in PlayerManager.Instance.buildQueue)
            {
                content += $"- {order.type.typeName}: {order.daysLeft} Tage\n";
            }
            if (queueListText) queueListText.text = content;
        }
    }

    // --- REPAIR ---
    private void UpdateRepairUI()
    {
        Ship ship = PlayerManager.Instance.selectedShip;
        if (ship == null) { if (genericInfoText) genericInfoText.text = "Kein Schiff"; return; }

        int cost = ship.CalculateRepairCost();
        if (genericInfoText) genericInfoText.text = $"Reparatur: {ship.shipName}";
        if (genericCostText) genericCostText.text = $"Kosten: {cost}";

        if (genericActionButton)
        {
            genericActionButton.interactable = (cost > 0 && PlayerManager.Instance.currentGold >= cost);
            genericActionButton.onClick.RemoveAllListeners();
            genericActionButton.onClick.AddListener(() => {
                PlayerManager.Instance.TryRepairShip();
                UIManager.Instance.UpdateGoldDisplay();
                UpdateRepairUI();
            });
        }
    }

    // --- SELL ---
    private void UpdateSellUI()
    {
        Ship ship = PlayerManager.Instance.selectedShip;
        if (ship == null) return;
        int val = Mathf.RoundToInt(ship.type.baseBuildPrice * 0.5f);
        if (genericInfoText) genericInfoText.text = $"Verkaufen: {ship.shipName}?";
        if (genericCostText) genericCostText.text = $"Erlös: {val}";
        if (genericActionButton)
        {
            genericActionButton.interactable = true;
            genericActionButton.onClick.RemoveAllListeners();
            genericActionButton.onClick.AddListener(() => {
                PlayerManager.Instance.currentGold += val;
                Destroy(ship.gameObject);
                PlayerManager.Instance.selectedShip = null;
                UIManager.Instance.UpdateGoldDisplay();
                ShowArea_Build();
            });
        }
    }
}