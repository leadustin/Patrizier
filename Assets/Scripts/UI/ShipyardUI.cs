using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ShipyardUI : MonoBehaviour
{
    [Header("--- AREAS (Fenster) ---")]
    public GameObject buildArea;
    public GameObject buyArea;
    public GameObject queueArea;
    public GameObject repairArea;
    public GameObject sellArea;
    public GameObject upgradeArea;

    [Header("--- DATEN ---")]
    public List<ShipType> availableShipTypes;
    private int currentSelectionIndex = 0;

    // --- POPUP ---
    [Header("--- POPUP (Confirm Only) ---")]
    public GameObject confirmPopup;
    public TextMeshProUGUI popupInfoText;
    public Button popupConfirmBtn;
    public Button popupCancelBtn;

    // Speicher für Transaktionen
    private ShipType pendingType;
    private Ship pendingRepairShip;
    private bool pendingIsBuy;
    private bool pendingIsRepair;
    private int pendingCost;
    private bool pendingUseOwnMats;
    private bool pendingLayoffCrew;
    private string pendingName;

    // --- BUILD TAB ---
    [Header("--- UI BUILD TAB ---")]
    public Image buildShipImage;
    public TextMeshProUGUI buildShipNameText;
    public TMP_InputField buildNameInput;
    public TextMeshProUGUI buildStatCargo, buildStatManeuver, buildStatHealth, buildStatMaint, buildStatSlots, buildStatRiver;
    public Button buildNextBtn, buildPrevBtn;
    public List<GameObject> buildMaterialSlots;
    public TextMeshProUGUI buildMaterialCostText, buildTotalCostText, buildTimeText;
    public Toggle buildOwnMaterialsToggle;
    public Button buildActionBtn;

    // --- BUY TAB ---
    [Header("--- UI BUY TAB ---")]
    public Image buyShipImage;
    public TextMeshProUGUI buyShipNameText;
    public TMP_InputField buyNameInput;
    public TextMeshProUGUI buyStatCargo, buyStatManeuver, buyStatHealth, buyStatMaint, buyStatSlots, buyStatRiver;
    public Button buyNextBtn, buyPrevBtn;
    public TextMeshProUGUI buyPriceText;
    public Button buyActionBtn;

    // ========================================================
    // --- REPAIR TAB (STRUKTUR & LISTE & DETAILS) ---
    // ========================================================

    [Header("--- UI REPAIR TAB (Struktur) ---")]
    public GameObject repairListView;    // Container Liste
    public GameObject repairDetailView;  // Container Details

    [Header("--- UI REPAIR (Listenansicht) ---")]
    public Transform repairListContent;     // ScrollView Content
    public GameObject repairListItemPrefab; // Button Prefab
    public TextMeshProUGUI repairEmptyText; // "Keine Schiffe..."

    [Header("--- UI REPAIR (Detailansicht) ---")]
    public Image repairShipImage;
    public TextMeshProUGUI repairShipNameText;

    // Stats
    public TextMeshProUGUI repairStatHealth;
    public TextMeshProUGUI repairStatCrew;

    // Rechts: Kosten & Material
    public List<GameObject> repairMaterialSlots;
    public TextMeshProUGUI repairMaterialCostText;
    public TextMeshProUGUI repairTotalCostText;
    public TextMeshProUGUI repairTimeText;

    // Toggles
    public Toggle repairOwnMaterialsToggle;
    public Toggle repairLayoffCrewToggle;
    public TextMeshProUGUI repairWageWarningText;

    public Button repairActionBtn;   // "Reparatur beauftragen"
    public Button repairBackBtn;     // "Zurück"

    private Ship currentDetailShip;


    // --- QUEUE & GENERIC ---
    [Header("--- QUEUE & GENERIC ---")]
    public TextMeshProUGUI queueListText;
    public TextMeshProUGUI genericInfoText, genericCostText;
    public Button genericActionButton;

    private void Start()
    {
        ShowArea_Build();
        if (confirmPopup) confirmPopup.SetActive(false);
        if (popupCancelBtn) popupCancelBtn.onClick.AddListener(ClosePopup);

        if (buildOwnMaterialsToggle) buildOwnMaterialsToggle.onValueChanged.AddListener(delegate { UpdateBuildUI(false); });

        if (repairOwnMaterialsToggle) repairOwnMaterialsToggle.onValueChanged.AddListener(delegate { UpdateRepairDetailView(); });
        if (repairLayoffCrewToggle) repairLayoffCrewToggle.onValueChanged.AddListener(delegate { UpdateRepairDetailView(); });

        if (repairBackBtn) repairBackBtn.onClick.AddListener(SwitchToRepairList);
    }

    private void Update()
    {
        if (queueArea != null && queueArea.activeSelf) UpdateQueueUI();
    }

    // --- NAVIGATION ---
    public void ShowArea_Build() { ActivateArea(buildArea); UpdateBuildUI(true); }
    public void ShowArea_Buy() { ActivateArea(buyArea); UpdateBuyUI(true); }
    public void ShowArea_Queue() { ActivateArea(queueArea); UpdateQueueUI(); }

    public void ShowArea_Repair()
    {
        ActivateArea(repairArea);
        SwitchToRepairList();
    }

    public void ShowArea_Sell() { ActivateArea(sellArea); UpdateSellUI(); }
    public void ShowArea_Upgrade() { ActivateArea(upgradeArea); }

    private void ActivateArea(GameObject areaToActive)
    {
        if (buildArea) buildArea.SetActive(false);
        if (buyArea) buyArea.SetActive(false);
        if (queueArea) queueArea.SetActive(false);
        if (repairArea) repairArea.SetActive(false);
        if (sellArea) sellArea.SetActive(false);
        if (upgradeArea) upgradeArea.SetActive(false);

        if (areaToActive) areaToActive.SetActive(true);
        ClosePopup();
    }

    // ========================================================
    // REPAIR UI: LIST VIEW LOGIK
    // ========================================================

    private void SwitchToRepairList()
    {
        if (repairListView) repairListView.SetActive(true);
        if (repairDetailView) repairDetailView.SetActive(false);

        GenerateRepairList();
    }

    private void GenerateRepairList()
    {
        foreach (Transform child in repairListContent) Destroy(child.gameObject);

        City currentCity = UIManager.Instance?.currentCity;
        if (currentCity == null) return;

        Ship[] allShips = FindObjectsOfType<Ship>();
        List<Ship> candidates = new List<Ship>();

        foreach (Ship s in allShips)
        {
            if (s.currentCityLocation == currentCity && s.currentHealth < s.type.maxHealth && !s.isUnderRepair)
            {
                candidates.Add(s);
            }
        }

        if (candidates.Count == 0)
        {
            if (repairEmptyText) repairEmptyText.gameObject.SetActive(true);
        }
        else
        {
            if (repairEmptyText) repairEmptyText.gameObject.SetActive(false);

            foreach (Ship s in candidates)
            {
                if (repairListItemPrefab)
                {
                    GameObject itemObj = Instantiate(repairListItemPrefab, repairListContent);

                    TextMeshProUGUI[] texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length > 0) texts[0].text = s.shipName;
                    if (texts.Length > 1) texts[1].text = $"{Mathf.CeilToInt(s.currentHealth)}/{s.type.maxHealth} HP";

                    Button btn = itemObj.GetComponent<Button>();
                    if (btn == null) btn = itemObj.GetComponentInChildren<Button>();

                    if (btn)
                    {
                        btn.onClick.AddListener(() => SwitchToRepairDetail(s));
                    }
                }
            }
        }
    }

    // ========================================================
    // REPAIR UI: DETAIL VIEW LOGIK
    // ========================================================

    private void SwitchToRepairDetail(Ship ship)
    {
        currentDetailShip = ship;

        if (repairListView) repairListView.SetActive(false);
        if (repairDetailView) repairDetailView.SetActive(true);

        if (repairOwnMaterialsToggle) repairOwnMaterialsToggle.isOn = false;
        if (repairLayoffCrewToggle) repairLayoffCrewToggle.isOn = false;

        UpdateRepairDetailView();
    }

    private void UpdateRepairDetailView()
    {
        if (currentDetailShip == null) { SwitchToRepairList(); return; }

        Ship ship = currentDetailShip;
        City city = UIManager.Instance?.currentCity;
        bool useOwn = repairOwnMaterialsToggle != null && repairOwnMaterialsToggle.isOn;
        bool layoff = repairLayoffCrewToggle != null && repairLayoffCrewToggle.isOn;

        if (repairShipNameText) repairShipNameText.text = ship.shipName;
        if (repairShipImage) repairShipImage.sprite = ship.type.icon;

        if (repairStatHealth) repairStatHealth.text = $"Zustand: {Mathf.CeilToInt(ship.currentHealth)} / {ship.type.maxHealth}";
        if (repairStatCrew) repairStatCrew.text = $"Crew: {ship.currentCrew} / {ship.type.maxCrew}";

        var reqs = PlayerManager.Instance.CalculateRepairRequirements(ship);

        foreach (var slot in repairMaterialSlots) if (slot != null) slot.SetActive(false);
        int matCostGold = 0;

        for (int i = 0; i < reqs.requiredMats.Count; i++)
        {
            if (i >= repairMaterialSlots.Count) break;
            GameObject slotObj = repairMaterialSlots[i];
            var req = reqs.requiredMats[i];
            string wName = req.wareType.ToString();

            slotObj.SetActive(true);
            Image iconImg = slotObj.transform.Find("Icon")?.GetComponent<Image>();
            TextMeshProUGUI amountTxt = slotObj.transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();

            if (iconImg) iconImg.sprite = req.icon;

            int inKontor = city.GetKontorStock(wName);
            int inMarket = city.GetMarketStock(wName);
            int stock = useOwn ? (inKontor + inMarket) : inMarket;

            if (amountTxt)
            {
                amountTxt.text = $"{req.amount}";
                amountTxt.color = stock >= req.amount ? Color.white : Color.red;
            }

            int neededToBuy = req.amount;
            if (useOwn) neededToBuy = Mathf.Max(0, neededToBuy - inKontor);
            matCostGold += neededToBuy * city.GetPrice(wName, true);
        }

        int totalGold = reqs.goldCost + matCostGold;

        if (repairMaterialCostText) repairMaterialCostText.text = $"Materialzukauf: {matCostGold}";
        if (repairTotalCostText) repairTotalCostText.text = $"Gesamt: {totalGold}";
        if (repairTimeText) repairTimeText.text = $"Dauer: {reqs.days} Tage";

        if (repairWageWarningText)
        {
            if (layoff) repairWageWarningText.text = "Crew wird entlassen (0 G/Tag)";
            else
            {
                int dailyWage = ship.currentCrew * 1;
                repairWageWarningText.text = $"Heuer läuft weiter (ca. {dailyWage} G/Tag)";
            }
        }

        if (repairActionBtn)
        {
            bool canAfford = PlayerManager.Instance.currentGold >= totalGold;
            repairActionBtn.interactable = canAfford;

            repairActionBtn.onClick.RemoveAllListeners();
            repairActionBtn.onClick.AddListener(() => {
                OpenConfirmPopupRepair(ship, totalGold, useOwn, layoff);
            });
        }
    }

    // ========================================================
    // POPUP LOGIK
    // ========================================================

    private void OpenConfirmPopup(ShipType type, int cost, bool isInstantBuy, bool useOwnMats, string shipName)
    {
        pendingType = type;
        pendingIsRepair = false;
        pendingCost = cost;
        pendingIsBuy = isInstantBuy;
        pendingUseOwnMats = useOwnMats;
        pendingName = shipName;
        SetupPopupUI(isInstantBuy ? "Sofortkauf" : "Bauauftrag", shipName, type.typeName, cost);
    }

    private void OpenConfirmPopupRepair(Ship ship, int cost, bool useOwnMats, bool layoffCrew)
    {
        pendingRepairShip = ship;
        pendingIsRepair = true;
        pendingCost = cost;
        pendingUseOwnMats = useOwnMats;
        pendingLayoffCrew = layoffCrew;
        SetupPopupUI("Reparaturauftrag", ship.shipName, ship.type.typeName, cost);
    }

    private void SetupPopupUI(string actionTitle, string name, string typeName, int cost)
    {
        if (popupInfoText)
            popupInfoText.text = $"<b>{actionTitle} bestätigen</b>\n\nSchiff: {name}\nTyp: {typeName}\nPreis: {cost} Gold";

        if (popupConfirmBtn)
        {
            popupConfirmBtn.onClick.RemoveAllListeners();
            popupConfirmBtn.onClick.AddListener(ExecuteTransaction);
        }
        if (confirmPopup) confirmPopup.SetActive(true);
    }

    private void ClosePopup() { if (confirmPopup) confirmPopup.SetActive(false); }

    private void ExecuteTransaction()
    {
        bool success = false;

        if (pendingIsRepair)
        {
            success = PlayerManager.Instance.OrderRepair(pendingRepairShip, UIManager.Instance.currentCity, pendingUseOwnMats, pendingLayoffCrew);
        }
        else if (pendingIsBuy)
        {
            string finalName = string.IsNullOrWhiteSpace(pendingName) ? pendingType.typeName : pendingName;
            success = PlayerManager.Instance.BuyShipInstant(pendingType, UIManager.Instance.currentCity, pendingCost, finalName);
        }
        else
        {
            string finalName = string.IsNullOrWhiteSpace(pendingName) ? pendingType.typeName : pendingName;
            success = PlayerManager.Instance.OrderShip(pendingType, UIManager.Instance.currentCity, pendingUseOwnMats, finalName);
        }

        if (success)
        {
            UIManager.Instance.UpdateGoldDisplay();
            ClosePopup();

            if (pendingIsRepair)
            {
                SwitchToRepairList();
            }
            else if (pendingIsBuy) UpdateBuyUI(false);
            else ShowArea_Queue();
        }
    }

    // ========================================================
    // BUILD & BUY & QUEUE
    // ========================================================

    private void UpdateBuildUI(bool resetName = false)
    {
        if (availableShipTypes == null) return;
        City city = UIManager.Instance?.currentCity ?? FindObjectOfType<City>();
        ShipType type = availableShipTypes[Mathf.Clamp(currentSelectionIndex, 0, availableShipTypes.Count - 1)];
        bool useOwn = buildOwnMaterialsToggle != null && buildOwnMaterialsToggle.isOn;

        if (buildShipNameText) buildShipNameText.text = type.typeName;
        if (buildShipImage) buildShipImage.sprite = type.icon;
        if (buildNameInput && resetName) buildNameInput.text = type.typeName;

        if (buildStatCargo) buildStatCargo.text = $"{type.maxCargo}";
        if (buildStatManeuver) buildStatManeuver.text = $"{type.maneuverability}%";
        if (buildStatHealth) buildStatHealth.text = $"{type.maxHealth}";
        if (buildStatMaint) buildStatMaint.text = $"{type.dailyMaintenance}";
        if (buildStatSlots) buildStatSlots.text = $"{type.upgradeSlots}";
        if (buildStatRiver) buildStatRiver.text = type.isRiverCapable ? "Meer/Fluss" : "Nur Meer";

        if (buildNextBtn)
        {
            buildNextBtn.onClick.RemoveAllListeners();
            buildNextBtn.onClick.AddListener(() => { currentSelectionIndex++; UpdateBuildUI(true); });
            buildNextBtn.interactable = currentSelectionIndex < availableShipTypes.Count - 1;
        }
        if (buildPrevBtn)
        {
            buildPrevBtn.onClick.RemoveAllListeners();
            buildPrevBtn.onClick.AddListener(() => { currentSelectionIndex--; UpdateBuildUI(true); });
            buildPrevBtn.interactable = currentSelectionIndex > 0;
        }

        foreach (var slot in buildMaterialSlots) if (slot != null) slot.SetActive(false);
        if (type.requiredResources != null)
        {
            for (int i = 0; i < type.requiredResources.Count; i++)
            {
                if (i >= buildMaterialSlots.Count) break;
                GameObject slotObj = buildMaterialSlots[i];
                var req = type.requiredResources[i];
                slotObj.SetActive(true);
                Image iconImg = slotObj.transform.Find("Icon")?.GetComponent<Image>();
                TextMeshProUGUI amountTxt = slotObj.transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();

                if (iconImg) iconImg.sprite = req.icon;
                int stock = useOwn ? (city.GetKontorStock(req.wareType.ToString()) + city.GetMarketStock(req.wareType.ToString())) : city.GetMarketStock(req.wareType.ToString());
                if (amountTxt)
                {
                    amountTxt.text = $"{req.amount}";
                    amountTxt.color = stock >= req.amount ? Color.white : Color.red;
                }
            }
        }

        var calc = PlayerManager.Instance.CalculateBuildCost(type, city, useOwn);
        int matCost = calc.totalCost - type.baseBuildPrice;
        if (matCost < 0) matCost = 0;

        if (buildMaterialCostText) buildMaterialCostText.text = $"Material: {matCost}";
        if (buildTotalCostText) buildTotalCostText.text = $"Gesamt: {calc.totalCost}";
        if (buildTimeText) buildTimeText.text = $"Bauzeit: {type.buildTimeDays} Tage";

        if (buildActionBtn)
        {
            buildActionBtn.interactable = calc.canBuild;
            buildActionBtn.onClick.RemoveAllListeners();
            buildActionBtn.onClick.AddListener(() => {
                string wName = buildNameInput ? buildNameInput.text : "";
                OpenConfirmPopup(type, calc.totalCost, false, useOwn, wName);
            });
        }
    }

    private void UpdateBuyUI(bool resetName = false)
    {
        if (availableShipTypes == null) return;
        City city = UIManager.Instance?.currentCity;
        ShipType type = availableShipTypes[Mathf.Clamp(currentSelectionIndex, 0, availableShipTypes.Count - 1)];

        if (buyShipNameText) buyShipNameText.text = type.typeName;
        if (buyShipImage) buyShipImage.sprite = type.icon;
        if (buyNameInput && resetName) buyNameInput.text = type.typeName;

        if (buyStatCargo) buyStatCargo.text = $"{type.maxCargo}";
        if (buyStatManeuver) buyStatManeuver.text = $"{type.maneuverability}%";
        if (buyStatHealth) buyStatHealth.text = $"{type.maxHealth}";
        if (buyStatMaint) buyStatMaint.text = $"{type.dailyMaintenance}";
        if (buyStatSlots) buyStatSlots.text = $"{type.upgradeSlots}";
        if (buyStatRiver) buyStatRiver.text = type.isRiverCapable ? "Ja" : "Nein";

        if (buyNextBtn)
        {
            buyNextBtn.onClick.RemoveAllListeners();
            buyNextBtn.onClick.AddListener(() => { currentSelectionIndex++; UpdateBuyUI(true); });
            buyNextBtn.interactable = currentSelectionIndex < availableShipTypes.Count - 1;
        }
        if (buyPrevBtn)
        {
            buyPrevBtn.onClick.RemoveAllListeners();
            buyPrevBtn.onClick.AddListener(() => { currentSelectionIndex--; UpdateBuyUI(true); });
            buyPrevBtn.interactable = currentSelectionIndex > 0;
        }

        var calc = PlayerManager.Instance.CalculateBuildCost(type, city, false);
        int price = Mathf.RoundToInt(calc.totalCost * 2f);

        if (buyPriceText) buyPriceText.text = $"Kaufpreis: {price} Gold";

        if (buyActionBtn)
        {
            bool canAfford = PlayerManager.Instance.currentGold >= price;
            buyActionBtn.interactable = canAfford;
            buyActionBtn.onClick.RemoveAllListeners();
            buyActionBtn.onClick.AddListener(() => {
                string wName = buyNameInput ? buyNameInput.text : "";
                OpenConfirmPopup(type, price, true, false, wName);
            });
        }
    }

    private void UpdateQueueUI()
    {
        if (PlayerManager.Instance.buildQueue.Count == 0)
        {
            if (queueListText) queueListText.text = "Keine laufenden Aufträge.";
        }
        else
        {
            string content = "<b>Laufende Aufträge:</b>\n";
            foreach (var order in PlayerManager.Instance.buildQueue)
            {
                string status = order.isRepair ? "Reparatur" : "Neubau";
                content += $"- {order.shipName}: {order.daysLeft} Tage ({status})\n";
            }
            if (queueListText) queueListText.text = content;
        }
    }

    private void UpdateSellUI()
    {
        Ship ship = PlayerManager.Instance.selectedShip;
        if (ship == null)
        {
            if (genericInfoText) genericInfoText.text = "Kein Schiff ausgewählt.";
            if (genericCostText) genericCostText.text = "";
            if (genericActionButton) genericActionButton.interactable = false;
            return;
        }

        float rawPrice = (float)PlayerManager.Instance.CalculateSellPrice();
        int sellPrice = Mathf.FloorToInt(rawPrice);

        if (genericInfoText) genericInfoText.text = $"Möchten Sie die '{ship.shipName}' wirklich verkaufen?";
        if (genericCostText) genericCostText.text = $"Erlös: {sellPrice} Gold";

        if (genericActionButton)
        {
            genericActionButton.interactable = true;
            TextMeshProUGUI btnText = genericActionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = "Verkaufen";

            genericActionButton.onClick.RemoveAllListeners();
            genericActionButton.onClick.AddListener(() => {
                if (PlayerManager.Instance.SellShip())
                {
                    UIManager.Instance.UpdateGoldDisplay();
                    ShowArea_Build();
                }
            });
        }
    }
}