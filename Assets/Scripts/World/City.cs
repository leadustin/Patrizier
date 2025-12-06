using UnityEngine;
using System.Collections.Generic;

public class City : MonoBehaviour
{
    [Header("Allgemeine Info")]
    public string cityName = "Unbenannt";
    public Sprite cityBackgroundSprite;

    [Header("Wirtschaft & Bevölkerung")]
    public int population = 2000;

    // --- NEU: GEBÄUDE INFRASTRUKTUR (Der Baukasten) ---
    [Header("Gebäude & Infrastruktur")]
    public CityInfrastructure buildings;

    [System.Serializable]
    public class CityInfrastructure
    {
        [Header("Öffentliche Gebäude")]
        public bool hasMarketplace = true; // Fast immer da
        public bool hasTavern = false;     // Kneipe
        public bool hasChurch = false;     // Kirche
        public bool hasShipyard = false;   // Werft
        public bool hasHealer = false;     // Arzt/Badehaus

        [Header("Verwaltung")]
        public bool hasCityHall = false;   // Rathaus
        public bool hasGuild = false;      // Gilde
    }
    // ---------------------------------------------------

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

    [Header("Ereignisse")]
    public CityEvents activeEvents;
    [System.Serializable]
    public struct CityEvents { public bool isUnderSiege; public bool hasPlague; public bool hasFire; public bool isHardWinter; }

    // Inventare
    public Dictionary<string, int> kontorInventory = new Dictionary<string, int>();
    public Dictionary<string, int> marketInventory = new Dictionary<string, int>();

    [Header("Dein Besitz")]
    [Range(0, 3)] public int kontorLevel = 0;

    // --- ANMELDUNG BEIM ZEIT-SYSTEM ---
    void OnEnable()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += HandleNewDay;
    }

    void OnDisable()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= HandleNewDay;
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

    // --- TÄGLICHE ROUTINE ---
    void HandleNewDay(System.DateTime date, Season season)
    {
        // 1. VERBRAUCH
        List<string> marketWares = new List<string>(marketInventory.Keys);
        foreach (string wareName in marketWares)
        {
            int consumption = EconomySystem.CalculateDailyConsumption(wareName, population);
            RemoveMarketStock(wareName, consumption);
        }

        // 2. PRODUKTION
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
            if (finalAmount > 0) AddMarketStock(wareString, finalAmount);
        }
    }

    // --- UI HELPER ---
    public bool DoesProduce(string wareName)
    {
        foreach (var p in productionLines)
        {
            if (p.ware.ToString() == wareName) return true;
        }
        return false;
    }

    // --- STANDARD METHODEN ---
    public int GetCurrentPrice(string ware) { return EconomySystem.CalculatePrice(ware, GetMarketStock(ware), this); }
    public int GetMarketStock(string ware) { return marketInventory.ContainsKey(ware) ? marketInventory[ware] : 0; }
    public void RemoveMarketStock(string ware, int amount) { if (marketInventory.ContainsKey(ware)) marketInventory[ware] -= amount; if (marketInventory[ware] < 0) marketInventory[ware] = 0; }
    public void AddMarketStock(string ware, int amount) { if (marketInventory.ContainsKey(ware)) marketInventory[ware] += amount; else marketInventory.Add(ware, amount); }
    public int GetKontorStock(string ware) { return kontorInventory.ContainsKey(ware) ? kontorInventory[ware] : 0; }
    public void AddToKontor(string ware, int amount) { if (kontorInventory.ContainsKey(ware)) kontorInventory[ware] += amount; else kontorInventory.Add(ware, amount); if (kontorInventory[ware] < 0) kontorInventory[ware] = 0; }

    void OnMouseDown()
    {
        if (UIManager.Instance != null) UIManager.Instance.OpenCityMenu(this);
    }
}