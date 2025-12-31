using UnityEngine;
using System.Collections.Generic;

public class City : MonoBehaviour
{
    // ------------------------------------------------------------
    // 1. ALLGEMEINE DATEN
    // ------------------------------------------------------------
    [Header("Allgemeine Info")]
    public string cityName = "Unbenannt";
    public CityType type = CityType.Hansestadt;
    public Sprite cityBackgroundSprite;

    [Header("Wirtschaft & Bevölkerung")]
    public int population = 2000;

    // ------------------------------------------------------------
    // 2. GEBÄUDE (INFRASTRUKTUR)
    // ------------------------------------------------------------
    [Header("Gebäude & Infrastruktur")]
    public CityInfrastructure buildings;

    [System.Serializable]
    public class CityInfrastructure
    {
        [Header("Öffentliche Gebäude")]
        public bool hasMarketplace = true;
        public bool hasTavern = false;
        public bool hasChurch = false;
        public bool hasShipyard = false;
        public bool hasHealer = false;

        [Header("Verwaltung")]
        public bool hasCityHall = false;
        public bool hasGuild = false;
    }

    // ------------------------------------------------------------
    // 3. PRODUKTION
    // ------------------------------------------------------------
    [Header("Produktion (Konfigurierbar)")]
    public List<ProductionBuilding> productionLines = new List<ProductionBuilding>();

    [System.Serializable]
    public class ProductionBuilding
    {
        public string name = "Betrieb";
        public WareType ware;
        public int baseAmount = 10;

        [Range(0f, 2f)] public float springMult = 1.0f;
        [Range(0f, 2f)] public float summerMult = 1.0f;
        [Range(0f, 2f)] public float autumnMult = 1.0f;
        [Range(0f, 2f)] public float winterMult = 0.5f;
    }

    // ------------------------------------------------------------
    // 4. EREIGNISSE & INVENTARE
    // ------------------------------------------------------------
    [Header("Aktuelle Ereignisse")]
    public CityEvents activeEvents;

    [System.Serializable]
    public struct CityEvents
    {
        public bool isUnderSiege;
        public bool hasPlague;
        public bool hasFire;
        public bool isHardWinter;
    }

    public Dictionary<string, int> kontorInventory = new Dictionary<string, int>();
    public Dictionary<string, int> marketInventory = new Dictionary<string, int>();

    [Header("Dein Besitz")]
    [Range(0, 3)] public int kontorLevel = 0;

    // ------------------------------------------------------------
    // 5. INITIALISIERUNG & ZEIT-SYSTEM
    // ------------------------------------------------------------

    void OnEnable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged += HandleNewDay;
    }

    void OnDisable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged -= HandleNewDay;
    }

    void Start()
    {
        InitializeMarket();
    }

    void InitializeMarket()
    {
        foreach (var prod in productionLines)
        {
            string wareName = prod.ware.ToString();
            AddMarketStock(wareName, prod.baseAmount * 10);
        }
    }

    // ------------------------------------------------------------
    // 6. TÄGLICHE LOGIK
    // ------------------------------------------------------------
    void HandleNewDay(System.DateTime date, Season season)
    {
        // A) VERBRAUCH
        List<string> marketWares = new List<string>(marketInventory.Keys);
        foreach (string wareName in marketWares)
        {
            int consumption = EconomySystem.CalculateDailyConsumption(wareName, population);
            if (activeEvents.hasPlague) consumption = Mathf.FloorToInt(consumption * 0.7f);
            RemoveMarketStock(wareName, consumption);
        }

        // B) PRODUKTION
        foreach (var factory in productionLines)
        {
            string wareString = factory.ware.ToString();
            float amount = factory.baseAmount;

            switch (season)
            {
                case Season.Frühling: amount *= factory.springMult; break;
                case Season.Sommer: amount *= factory.summerMult; break;
                case Season.Herbst: amount *= factory.autumnMult; break;
                case Season.Winter: amount *= factory.winterMult; break;
            }

            if (activeEvents.hasPlague) amount *= 0.5f;
            if (activeEvents.isUnderSiege) amount *= 0.1f;

            int finalAmount = Mathf.FloorToInt(amount);
            if (finalAmount > 0)
            {
                AddMarketStock(wareString, finalAmount);
            }
        }
    }

    // ------------------------------------------------------------
    // 7. INTERAKTION
    // ------------------------------------------------------------
    void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (PlayerManager.Instance != null && PlayerManager.Instance.selectedShip != null)
        {
            Ship selectedShip = PlayerManager.Instance.selectedShip;
            float distance = Vector3.Distance(transform.position, selectedShip.transform.position);

            if (distance < 0.5f)
            {
                if (UIManager.Instance != null) UIManager.Instance.OpenCityMenu(this);
                selectedShip.currentCityLocation = this;
            }
            else
            {
                ShipMovement movement = selectedShip.GetComponent<ShipMovement>();
                City startCity = selectedShip.currentCityLocation;

                if (startCity != null && movement != null)
                    movement.SetDestination(startCity, this);
            }
        }
        else
        {
            if (UIManager.Instance != null) UIManager.Instance.OpenCityMenu(this);
        }
    }

    // ------------------------------------------------------------
    // 8. HILFSMETHODEN
    // ------------------------------------------------------------

    public bool DoesProduce(string wareName)
    {
        foreach (var p in productionLines) if (p.ware.ToString() == wareName) return true;
        return false;
    }

    public int GetCurrentPrice(string ware)
    {
        return EconomySystem.CalculatePrice(ware, GetMarketStock(ware), this);
    }

    public int GetPrice(string ware, bool isBuyingFromCity)
    {
        int basePrice = GetCurrentPrice(ware);
        if (isBuyingFromCity) return Mathf.CeilToInt(basePrice * 1.1f);
        return basePrice;
    }

    public int GetMarketStock(string ware) { return marketInventory.ContainsKey(ware) ? marketInventory[ware] : 0; }

    public void RemoveMarketStock(string ware, int amount)
    {
        if (marketInventory.ContainsKey(ware)) marketInventory[ware] -= amount;
        if (marketInventory[ware] < 0) marketInventory[ware] = 0;
    }

    public void AddMarketStock(string ware, int amount)
    {
        if (marketInventory.ContainsKey(ware)) marketInventory[ware] += amount;
        else marketInventory.Add(ware, amount);
    }

    public int GetKontorStock(string ware) { return kontorInventory.ContainsKey(ware) ? kontorInventory[ware] : 0; }

    public void AddToKontor(string ware, int amount)
    {
        if (kontorInventory.ContainsKey(ware)) kontorInventory[ware] += amount;
        else kontorInventory.Add(ware, amount);
        if (kontorInventory[ware] < 0) kontorInventory[ware] = 0;
    }
}