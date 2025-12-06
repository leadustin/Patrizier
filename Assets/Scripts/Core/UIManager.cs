using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Ansichten (Canvas)")]
    public GameObject mapCanvas;
    public GameObject cityViewCanvas;
    public Image cityBackgroundImage;

    [Header("Gebäude Hotspots (Buttons im CityView)")]
    public GameObject hotspotMarket;
    public GameObject hotspotTavern;
    public GameObject hotspotChurch;
    public GameObject hotspotShipyard;

    [Header("Gebäude Fenster (Panels)")]
    public GameObject marketPanel;     // Das Handelsfenster
    public GameObject tavernPanel;     // Das Tavernen-Fenster (NEU)
    public GameObject churchPanel;     // Das Kirchen-Fenster (NEU)
    public GameObject shipyardPanel;   // Das Werft-Fenster (NEU)

    [Header("Info Panel (Karte)")]
    public GameObject cityPanel;
    public TextMeshProUGUI cityNameText;
    public Button btnEnterCity;

    [Header("Markt UI Elemente")]
    public Transform goodsListContainer;
    public GameObject marketRowPrefab;
    public TextMeshProUGUI playerGoldText;
    public TextMeshProUGUI dateText;

    // --- HANDELS-LOGIK ---
    public enum MarketMode { CityToShip, CityToKontor, ShipToKontor }
    public MarketMode currentMarketMode = MarketMode.CityToShip;
    public int currentTradeAmount = 1;

    public City currentCity;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Startzustand: Nur Karte an, alle Fenster zu
        if (mapCanvas != null) mapCanvas.SetActive(true);
        if (cityViewCanvas != null) cityViewCanvas.SetActive(false);
        if (cityPanel != null) cityPanel.SetActive(false);

        CloseAllPanels(); // Sicherstellen, dass alle Popups zu sind

        // Zeit abonnieren
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged += UpdateDateDisplay;

            // Initiales Datum setzen
            UpdateDateDisplay(TimeManager.Instance.currentDate, TimeManager.Instance.GetSeason(TimeManager.Instance.currentDate.Month));
        }
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged -= UpdateDateDisplay;
    }

    void UpdateDateDisplay(System.DateTime date, Season season)
    {
        if (dateText != null)
        {
            string seasonName = season.ToString();
            string monthName = date.ToString("MMMM");

            if (LocalizationManager.Instance != null)
            {
                seasonName = LocalizationManager.Instance.GetSeasonName(season);
                monthName = LocalizationManager.Instance.GetMonthName(date.Month);
            }

            dateText.text = $"{date.Day:00}. {monthName} {date.Year} ({seasonName})";

            // Markt live aktualisieren bei Datumswechsel
            if (marketPanel != null && marketPanel.activeSelf) RefreshMarketList();
        }
    }

    // ---------------------------------------------------------
    // BEREICH: NAVIGATION (KARTE <-> STADT)
    // ---------------------------------------------------------

    public void OpenCityMenu(City city)
    {
        currentCity = city;
        cityPanel.SetActive(true);
        cityNameText.text = city.cityName;
        btnEnterCity.interactable = (city.cityBackgroundSprite != null);
    }

    public void CloseCityMenu() { cityPanel.SetActive(false); }

    public void EnterCityView()
    {
        if (currentCity == null || currentCity.cityBackgroundSprite == null) return;

        CloseCityMenu();

        // 1. Hintergrundbild laden
        cityBackgroundImage.sprite = currentCity.cityBackgroundSprite;

        // 2. Hotspots (Buttons) an/ausschalten je nach Gebäude-Existenz
        if (hotspotMarket != null) hotspotMarket.SetActive(currentCity.buildings.hasMarketplace);
        if (hotspotTavern != null) hotspotTavern.SetActive(currentCity.buildings.hasTavern);
        if (hotspotChurch != null) hotspotChurch.SetActive(currentCity.buildings.hasChurch);
        if (hotspotShipyard != null) hotspotShipyard.SetActive(currentCity.buildings.hasShipyard);

        // 3. Ansicht wechseln
        mapCanvas.SetActive(false);
        cityViewCanvas.SetActive(true);
    }

    public void ReturnToMap()
    {
        CloseAllPanels(); // Wichtig: Alle Fenster schließen
        cityViewCanvas.SetActive(false);
        mapCanvas.SetActive(true);
    }

    // ---------------------------------------------------------
    // BEREICH: GEBÄUDE FENSTER STEUERUNG
    // ---------------------------------------------------------

    // Hilfsfunktion: Macht erst mal alles zu
    public void CloseAllPanels()
    {
        if (marketPanel != null) marketPanel.SetActive(false);
        if (tavernPanel != null) tavernPanel.SetActive(false);
        if (churchPanel != null) churchPanel.SetActive(false);
        if (shipyardPanel != null) shipyardPanel.SetActive(false);
    }

    public void OnClickMarketplace()
    {
        CloseAllPanels();
        marketPanel.SetActive(true);
        UpdateGoldDisplay();
        SetMarketMode(0); // Reset auf Standard-Ansicht
    }

    public void OnClickTavern()
    {
        CloseAllPanels();
        if (tavernPanel != null) tavernPanel.SetActive(true);
        Debug.Log("Taverne geöffnet");
    }

    public void OnClickChurch()
    {
        CloseAllPanels();
        if (churchPanel != null) churchPanel.SetActive(true);
        Debug.Log("Kirche geöffnet");
    }

    public void OnClickShipyard()
    {
        CloseAllPanels();
        if (shipyardPanel != null) shipyardPanel.SetActive(true);
        Debug.Log("Werft geöffnet");
    }

    // ---------------------------------------------------------
    // BEREICH: MARKT LOGIK
    // ---------------------------------------------------------

    public void UpdateGoldDisplay()
    {
        string goldLabel = "Gold";
        if (LocalizationManager.Instance != null) goldLabel = LocalizationManager.Instance.Get("UI_GOLD");

        if (playerGoldText != null && PlayerManager.Instance != null)
            playerGoldText.text = $"{goldLabel}: {PlayerManager.Instance.GetGold()}";
    }

    public void SetTradeAmount(int amount)
    {
        currentTradeAmount = amount;
    }

    public void SetMarketMode(int modeIndex)
    {
        currentMarketMode = (MarketMode)modeIndex;
        RefreshMarketList();
    }

    public void RefreshMarketList()
    {
        // Aufräumen
        foreach (Transform child in goodsListContainer) Destroy(child.gameObject);
        goodsListContainer.DetachChildren();

        string[] displayedWares = { "Holz", "Ziegel", "Getreide", "Fisch", "Bier", "Tuch", "Eisen", "Salz", "Wein" };

        foreach (string ware in displayedWares)
        {
            GameObject newRow = Instantiate(marketRowPrefab, goodsListContainer);
            newRow.transform.localScale = Vector3.one;
            newRow.transform.localPosition = Vector3.zero;
            newRow.transform.localRotation = Quaternion.identity;

            int leftAmount = 0;
            int rightAmount = 0;
            int currentPrice = 0;
            bool isTransfer = false;

            // Preis holen
            if (currentCity != null)
                currentPrice = currentCity.GetCurrentPrice(ware);
            else
                currentPrice = EconomySystem.GetBasePrice(ware);

            // Bestände je nach Modus
            switch (currentMarketMode)
            {
                case MarketMode.CityToShip:
                    if (currentCity != null) leftAmount = currentCity.GetMarketStock(ware);
                    rightAmount = PlayerManager.Instance.GetStock(ware);
                    isTransfer = false;
                    break;

                case MarketMode.CityToKontor:
                    if (currentCity != null) leftAmount = currentCity.GetMarketStock(ware);
                    if (currentCity != null) rightAmount = currentCity.GetKontorStock(ware);
                    isTransfer = false;
                    break;

                case MarketMode.ShipToKontor:
                    if (currentCity != null) leftAmount = currentCity.GetKontorStock(ware);
                    rightAmount = PlayerManager.Instance.GetStock(ware);
                    currentPrice = 0;
                    isTransfer = true;
                    break;
            }

            // Zeile befüllen
            MarketRow rowScript = newRow.GetComponent<MarketRow>();
            if (rowScript != null)
            {
                rowScript.SetupRow(ware, leftAmount, rightAmount, currentPrice, isTransfer);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(goodsListContainer.GetComponent<RectTransform>());
    }
}