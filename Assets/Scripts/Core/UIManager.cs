using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Ansichten")]
    public GameObject mapCanvas;
    public GameObject cityViewCanvas;
    public Image cityBackgroundImage;

    [Header("Info Panel")]
    public GameObject cityPanel;
    public TextMeshProUGUI cityNameText;
    public Button btnEnterCity;

    [Header("Markt UI")]
    public GameObject marketPanel;
    public Transform goodsListContainer;
    public GameObject marketRowPrefab;
    public TextMeshProUGUI playerGoldText;

    // Das Datum oben im UI
    public TextMeshProUGUI dateText;

    // Handels-Variablen
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
        if (mapCanvas != null) mapCanvas.SetActive(true);
        if (cityViewCanvas != null) cityViewCanvas.SetActive(false);
        if (cityPanel != null) cityPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(false);

        // Zeit abonnieren
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged += UpdateDateDisplay;

            // Initial-Aufruf: Wir warten auf den ersten Tick oder holen die Startzeit
            // (Kann leer bleiben oder manuell aufgerufen werden)
        }
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged -= UpdateDateDisplay;
    }

    // --- ÄNDERUNG: Jahreszeit übersetzen ---
    void UpdateDateDisplay(System.DateTime date, Season season)
    {
        if (dateText != null)
        {
            string seasonName = season.ToString();
            string monthName = date.ToString("MMMM"); // Standard System-Sprache

            if (LocalizationManager.Instance != null)
            {
                seasonName = LocalizationManager.Instance.GetSeasonName(season);
                // NEU: Monat übersetzen
                monthName = LocalizationManager.Instance.GetMonthName(date.Month);
            }

            // Wir bauen das Datum selbst zusammen: "01. Mai 1300"
            dateText.text = $"{date.Day:00}. {monthName} {date.Year} ({seasonName})";

            if (marketPanel != null && marketPanel.activeSelf)
            {
                RefreshMarketList();
            }
        }
    }
    // ---------------------------------------

    // --- REST BLEIBT GLEICH (Navigation & Markt-Logik) ---

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
        cityBackgroundImage.sprite = currentCity.cityBackgroundSprite;
        mapCanvas.SetActive(false);
        cityViewCanvas.SetActive(true);
    }

    public void ReturnToMap()
    {
        CloseMarket();
        cityViewCanvas.SetActive(false);
        mapCanvas.SetActive(true);
    }

    public void OnClickMarketplace()
    {
        marketPanel.SetActive(true);
        UpdateGoldDisplay();
        SetMarketMode(0);
    }

    public void CloseMarket() { marketPanel.SetActive(false); }

    public void UpdateGoldDisplay()
    {
        if (playerGoldText != null && PlayerManager.Instance != null)
        {
            // ALT: playerGoldText.text = "Gold: " + ...

            // NEU: Wir holen das Wort "Gold" aus dem Manager
            string goldLabel = "Gold";
            if (LocalizationManager.Instance != null)
                goldLabel = LocalizationManager.Instance.Get("UI_GOLD");

            playerGoldText.text = $"{goldLabel}: {PlayerManager.Instance.GetGold()}";
        }
    }

    public void SetTradeAmount(int amount) { currentTradeAmount = amount; }

    public void SetMarketMode(int modeIndex)
    {
        currentMarketMode = (MarketMode)modeIndex;
        RefreshMarketList();
    }

    public void RefreshMarketList()
    {
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

            if (currentCity != null)
                currentPrice = currentCity.GetCurrentPrice(ware);
            else
                currentPrice = EconomySystem.GetBasePrice(ware);

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

            MarketRow rowScript = newRow.GetComponent<MarketRow>();
            if (rowScript != null)
            {
                rowScript.SetupRow(ware, leftAmount, rightAmount, currentPrice, isTransfer);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(goodsListContainer.GetComponent<RectTransform>());
    }
}