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
    private Ship pendingShip; // Für Reparatur ODER Verkauf
    private bool pendingIsBuy;
    private bool pendingIsRepair;
    private bool pendingIsSell; // NEU
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

    // --- REPAIR TAB ---
    [Header("--- UI REPAIR TAB (Struktur) ---")]
    public GameObject repairListView;
    public GameObject repairDetailView;

    [Header("--- UI REPAIR (Listenansicht) ---")]
    public Transform repairListContent;
    public GameObject repairListItemPrefab;
    public TextMeshProUGUI repairEmptyText;

    [Header("--- UI REPAIR (Detailansicht) ---")]
    public Image repairShipImage;
    public TextMeshProUGUI repairShipNameText;
    public TextMeshProUGUI repairStatHealth;
    public TextMeshProUGUI repairStatCrew;
    public List<GameObject> repairMaterialSlots;
    public TextMeshProUGUI repairMaterialCostText;
    public TextMeshProUGUI repairTotalCostText;
    public TextMeshProUGUI repairTimeText;
    public Toggle repairOwnMaterialsToggle;
    public Toggle repairLayoffCrewToggle;
    public TextMeshProUGUI repairWageWarningText;
    public Button repairActionBtn;
    public Button repairBackBtn;
    private Ship currentDetailShip;

    // ========================================================
    // --- SELL TAB (NEU) ---
    // ========================================================
    [Header("--- UI SELL TAB (Struktur) ---")]
    public GameObject sellListView;
    public GameObject sellDetailView;

    [Header("--- UI SELL (Listenansicht) ---")]
    public Transform sellListContent;
    public GameObject sellListItemPrefab; // Kann dasselbe sein wie Repair
    public TextMeshProUGUI sellEmptyText;

    [Header("--- UI SELL (Detailansicht) ---")]
    public Image sellShipImage;
    public TextMeshProUGUI sellShipNameText;
    public TextMeshProUGUI sellStatHealth;
    public TextMeshProUGUI sellStatCargo; // Anzeige Fracht
    public TextMeshProUGUI sellPriceText; // "Wert: 1500"

    // Buttons für "Zustand A: Leer"
    public Button sellActionBtn; // "Verkaufen"

    // Buttons für "Zustand B: Fracht"
    public Button sellBulkToKontorBtn; // "Alles ins Kontor"
    public Button sellBulkToMarketBtn; // "Alles verkaufen"
    public TextMeshProUGUI sellCargoWarningText; // "Laderaum muss leer sein!"

    public Button sellBackBtn;
    private Ship currentSellShip;


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

        // Listener
        if (buildOwnMaterialsToggle) buildOwnMaterialsToggle.onValueChanged.AddListener(delegate { UpdateBuildUI(false); });

        if (repairOwnMaterialsToggle) repairOwnMaterialsToggle.onValueChanged.AddListener(delegate { UpdateRepairDetailView(); });
        if (repairLayoffCrewToggle) repairLayoffCrewToggle.onValueChanged.AddListener(delegate { UpdateRepairDetailView(); });
        if (repairBackBtn) repairBackBtn.onClick.AddListener(SwitchToRepairList);

        // SELL Listener
        if (sellBackBtn) sellBackBtn.onClick.AddListener(SwitchToSellList);
        if (sellBulkToKontorBtn) sellBulkToKontorBtn.onClick.AddListener(OnBulkToKontorClicked);
        if (sellBulkToMarketBtn) sellBulkToMarketBtn.onClick.AddListener(OnBulkToMarketClicked);
    }

    private void Update()
    {
        if (queueArea != null && queueArea.activeSelf) UpdateQueueUI();
    }

    // --- NAVIGATION ---
    public void ShowArea_Build() { ActivateArea(buildArea); UpdateBuildUI(true); }
    public void ShowArea_Buy() { ActivateArea(buyArea); UpdateBuyUI(true); }
    public void ShowArea_Queue() { ActivateArea(queueArea); UpdateQueueUI(); }
    public void ShowArea_Repair() { ActivateArea(repairArea); SwitchToRepairList(); }
    public void ShowArea_Sell() { ActivateArea(sellArea); SwitchToSellList(); } // NEU

    // (Upgrade Area später...)
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
    // REPAIR UI
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
                candidates.Add(s);
        }

        if (candidates.Count == 0 && repairEmptyText) repairEmptyText.gameObject.SetActive(true);
        else
        {
            if (repairEmptyText) repairEmptyText.gameObject.SetActive(false);
            foreach (Ship s in candidates) CreateListItem(s, repairListContent, repairListItemPrefab, true);
        }
    }

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
        // (Hier steht dein vorhandener Repair-Code... Ich kürze es für die Übersicht nicht ab, sondern füge es ein)
        Ship ship = currentDetailShip;
        City city = UIManager.Instance?.currentCity;
        bool useOwn = repairOwnMaterialsToggle != null && repairOwnMaterialsToggle.isOn;
        bool layoff = repairLayoffCrewToggle != null && repairLayoffCrewToggle.isOn;

        if (repairShipNameText) repairShipNameText.text = ship.shipName;
        if (repairShipImage) repairShipImage.sprite = ship.type.icon;
        if (repairStatHealth) repairStatHealth.text = $"{Mathf.CeilToInt(ship.currentHealth)} / {ship.type.maxHealth}";
        if (repairStatCrew) repairStatCrew.text = $"{ship.currentCrew} / {ship.type.maxCrew}";

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
            else repairWageWarningText.text = "Heuer läuft weiter";
        }

        if (repairActionBtn)
        {
            bool canAfford = PlayerManager.Instance.currentGold >= totalGold;
            repairActionBtn.interactable = canAfford;
            repairActionBtn.onClick.RemoveAllListeners();
            repairActionBtn.onClick.AddListener(() => OpenConfirmPopupRepair(ship, totalGold, useOwn, layoff));
        }
    }

    // ========================================================
    // SELL UI (NEU)
    // ========================================================

    private void SwitchToSellList()
    {
        if (sellListView) sellListView.SetActive(true);
        if (sellDetailView) sellDetailView.SetActive(false);
        GenerateSellList();
    }

    private void GenerateSellList()
    {
        foreach (Transform child in sellListContent) Destroy(child.gameObject);
        City currentCity = UIManager.Instance?.currentCity;
        if (currentCity == null) return;

        Ship[] allShips = FindObjectsOfType<Ship>();
        List<Ship> candidates = new List<Ship>();

        foreach (Ship s in allShips)
        {
            // Alle meine Schiffe in dieser Stadt (egal ob kaputt oder heile)
            if (s.currentCityLocation == currentCity && !s.isUnderRepair)
                candidates.Add(s);
        }

        if (candidates.Count == 0 && sellEmptyText) sellEmptyText.gameObject.SetActive(true);
        else
        {
            if (sellEmptyText) sellEmptyText.gameObject.SetActive(false);
            // Nutze gleiches Prefab oder ein eigenes
            GameObject prefab = sellListItemPrefab != null ? sellListItemPrefab : repairListItemPrefab;
            foreach (Ship s in candidates) CreateListItem(s, sellListContent, prefab, false);
        }
    }

    // Helper für Liste (DRY)
    private void CreateListItem(Ship s, Transform parent, GameObject prefab, bool isRepair)
    {
        if (!prefab) return;
        GameObject itemObj = Instantiate(prefab, parent);
        TextMeshProUGUI[] texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length > 0) texts[0].text = s.shipName;
        // Für Verkauf zeigen wir den Wert an, für Reparatur die HP
        if (texts.Length > 1)
        {
            if (isRepair) texts[1].text = $"{Mathf.CeilToInt(s.currentHealth)} HP";
            else texts[1].text = $"{PlayerManager.Instance.CalculateSellValue(s)} Gold";
        }

        Button btn = itemObj.GetComponent<Button>() ?? itemObj.GetComponentInChildren<Button>();
        if (btn)
        {
            if (isRepair) btn.onClick.AddListener(() => SwitchToRepairDetail(s));
            else btn.onClick.AddListener(() => SwitchToSellDetail(s));
        }
    }

    private void SwitchToSellDetail(Ship ship)
    {
        currentSellShip = ship;
        if (sellListView) sellListView.SetActive(false);
        if (sellDetailView) sellDetailView.SetActive(true);
        UpdateSellDetailView();
    }

    private void UpdateSellDetailView()
    {
        if (currentSellShip == null) { SwitchToSellList(); return; }

        Ship ship = currentSellShip;
        int value = PlayerManager.Instance.CalculateSellValue(ship);
        bool hasCargo = ship.currentCargoLoad > 0;

        if (sellShipNameText) sellShipNameText.text = ship.shipName;
        if (sellShipImage) sellShipImage.sprite = ship.type.icon;

        if (sellStatHealth) sellStatHealth.text = $"Zustand: {Mathf.CeilToInt(ship.currentHealth)} / {ship.type.maxHealth}";
        if (sellStatCargo) sellStatCargo.text = $"Fracht: {ship.currentCargoLoad} / {ship.GetMaxCargo()}";
        if (sellPriceText) sellPriceText.text = $"Wert: {value} Gold";

        // --- ZUSTAND A: Fracht an Bord ---
        if (hasCargo)
        {
            if (sellActionBtn) sellActionBtn.gameObject.SetActive(false); // Verstecken

            if (sellBulkToKontorBtn) sellBulkToKontorBtn.gameObject.SetActive(true);
            if (sellBulkToMarketBtn) sellBulkToMarketBtn.gameObject.SetActive(true);
            if (sellCargoWarningText)
            {
                sellCargoWarningText.gameObject.SetActive(true);
                sellCargoWarningText.text = "Laderaum muss leer sein!";
            }
        }
        // --- ZUSTAND B: Schiff leer ---
        else
        {
            if (sellActionBtn)
            {
                sellActionBtn.gameObject.SetActive(true);
                sellActionBtn.interactable = true;
                sellActionBtn.onClick.RemoveAllListeners();
                sellActionBtn.onClick.AddListener(() => OpenConfirmPopupSell(ship, value));
            }

            if (sellBulkToKontorBtn) sellBulkToKontorBtn.gameObject.SetActive(false);
            if (sellBulkToMarketBtn) sellBulkToMarketBtn.gameObject.SetActive(false);
            if (sellCargoWarningText) sellCargoWarningText.gameObject.SetActive(false);
        }
    }

    private void OnBulkToKontorClicked()
    {
        if (currentSellShip != null)
        {
            PlayerManager.Instance.TransferAllCargoToKontor(currentSellShip, UIManager.Instance.currentCity);
            UpdateSellDetailView(); // UI aktualisieren (Buttons tauschen)
        }
    }

    private void OnBulkToMarketClicked()
    {
        if (currentSellShip != null)
        {
            PlayerManager.Instance.SellAllCargoToCity(currentSellShip, UIManager.Instance.currentCity);
            UIManager.Instance.UpdateGoldDisplay();
            UpdateSellDetailView();
        }
    }


    // ========================================================
    // POPUP LOGIK
    // ========================================================

    private void OpenConfirmPopup(ShipType type, int cost, bool isInstantBuy, bool useOwnMats, string shipName)
    {
        pendingType = type;
        pendingIsRepair = false;
        pendingIsSell = false;
        pendingCost = cost;
        pendingIsBuy = isInstantBuy;
        pendingUseOwnMats = useOwnMats;
        pendingName = shipName;
        SetupPopupUI(isInstantBuy ? "Sofortkauf" : "Bauauftrag", shipName, type.typeName, cost);
    }

    private void OpenConfirmPopupRepair(Ship ship, int cost, bool useOwnMats, bool layoffCrew)
    {
        pendingShip = ship; // Generic pending ship
        pendingIsRepair = true;
        pendingIsSell = false;
        pendingCost = cost;
        pendingUseOwnMats = useOwnMats;
        pendingLayoffCrew = layoffCrew;
        SetupPopupUI("Reparaturauftrag", ship.shipName, ship.type.typeName, cost);
    }

    private void OpenConfirmPopupSell(Ship ship, int value)
    {
        pendingShip = ship;
        pendingIsRepair = false;
        pendingIsSell = true;
        pendingCost = value; // Hier ist Cost = Erlös
        SetupPopupUI("Schiff verkaufen", ship.shipName, ship.type.typeName, value);
    }

    private void SetupPopupUI(string actionTitle, string name, string typeName, int cost)
    {
        string priceLabel = pendingIsSell ? "Erlös" : "Preis";
        if (popupInfoText)
            popupInfoText.text = $"<b>{actionTitle} bestätigen</b>\n\nSchiff: {name}\nTyp: {typeName}\n{priceLabel}: {cost} Gold";

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

        if (pendingIsSell)
        {
            success = PlayerManager.Instance.SellShip(pendingShip);
        }
        else if (pendingIsRepair)
        {
            success = PlayerManager.Instance.OrderRepair(pendingShip, UIManager.Instance.currentCity, pendingUseOwnMats, pendingLayoffCrew);
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

            if (pendingIsSell) SwitchToSellList();
            else if (pendingIsRepair) SwitchToRepairList();
            else if (pendingIsBuy) UpdateBuyUI(false);
            else ShowArea_Queue();
        }
    }

    // ========================================================
    // BUILD & BUY & QUEUE (Standard Logik)
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

    // Die alte Sell-Logik haben wir entfernt, da sie durch den neuen Tab ersetzt wurde
}