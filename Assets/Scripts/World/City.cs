using UnityEngine;
using System.Collections.Generic;

public class City : MonoBehaviour
{
    // ------------------------------------------------------------
    // 1. ALLGEMEINE DATEN
    // ------------------------------------------------------------
    [Header("Allgemeine Info")]
    public string cityName = "Unbenannt";
    public CityType type = CityType.Hansestadt; // Hansestadt oder Kontor
    public Sprite cityBackgroundSprite;         // Das Bild für die Stadtansicht

    [Header("Wirtschaft & Bevölkerung")]
    public int population = 2000;

    // ------------------------------------------------------------
    // 2. GEBÄUDE (INFRASTRUKTUR)
    // Hier stellst du ein, welche Gebäude die Stadt hat (fürs UI)
    // ------------------------------------------------------------
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

    // ------------------------------------------------------------
    // 3. PRODUKTION (WAS WIRD HERGESTELLT?)
    // Konfigurierbar pro Jahreszeit
    // ------------------------------------------------------------
    [Header("Produktion (Konfigurierbar)")]
    public List<ProductionBuilding> productionLines = new List<ProductionBuilding>();

    [System.Serializable]
    public class ProductionBuilding
    {
        public string name = "Betrieb"; // Nur für Übersicht im Editor
        public WareType ware;           // Dropdown-Auswahl (Enum)

        [Header("Produktion pro Tag (Basis)")]
        public int baseAmount = 10;

        [Header("Jahreszeiten-Faktor (1.0 = 100%)")]
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
        public bool isUnderSiege; // Belagerung (Preise hoch, Prod stoppt)
        public bool hasPlague;    // Pest (Verbrauch runter, Prod runter)
        public bool hasFire;      // Feuer (Baumaterial teuer)
        public bool isHardWinter; // Eis (Hafen zu? - noch nicht implementiert)
    }

    // Die Warenlager (String = Warenname, Int = Menge)
    public Dictionary<string, int> kontorInventory = new Dictionary<string, int>();
    public Dictionary<string, int> marketInventory = new Dictionary<string, int>();

    [Header("Dein Besitz")]
    [Range(0, 3)] public int kontorLevel = 0; // 0=Keins, 1=Kontor...

    // ------------------------------------------------------------
    // 5. INITIALISIERUNG & ZEIT-SYSTEM
    // ------------------------------------------------------------

    void OnEnable()
    {
        // Wir melden uns beim TimeManager an, um jeden Tag benachrichtigt zu werden
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

    // Füllt den Markt beim Spielstart, damit er nicht leer ist
    void InitializeMarket()
    {
        // Wir gehen durch die Produktionslinien und füllen Startbestände auf
        foreach (var prod in productionLines)
        {
            string wareName = prod.ware.ToString();
            // Startvorrat für ca. 10 Tage
            AddMarketStock(wareName, prod.baseAmount * 10);
        }

        // Optional: Auch Waren, die wir NICHT produzieren, sollten minimal da sein (Importe)
        // Das kann man später verfeinern.
    }

    // ------------------------------------------------------------
    // 6. TÄGLICHE LOGIK (VERBRAUCH & PRODUKTION)
    // ------------------------------------------------------------
    void HandleNewDay(System.DateTime date, Season season)
    {
        // A) VERBRAUCH (Bevölkerung isst/nutzt Waren)
        // Wir iterieren über eine Kopie der Keys, da wir das Dictionary verändern könnten
        List<string> marketWares = new List<string>(marketInventory.Keys);
        foreach (string wareName in marketWares)
        {
            int consumption = EconomySystem.CalculateDailyConsumption(wareName, population);

            // Ereignisse: Bei Pest sterben Leute -> weniger Verbrauch
            if (activeEvents.hasPlague) consumption = Mathf.FloorToInt(consumption * 0.7f);

            RemoveMarketStock(wareName, consumption);
        }

        // B) PRODUKTION (Betriebe stellen her)
        foreach (var factory in productionLines)
        {
            string wareString = factory.ware.ToString();

            // Basis Produktion
            float amount = factory.baseAmount;

            // Jahreszeit anwenden
            switch (season)
            {
                case Season.Frühling: amount *= factory.springMult; break;
                case Season.Sommer: amount *= factory.summerMult; break;
                case Season.Herbst: amount *= factory.autumnMult; break;
                case Season.Winter: amount *= factory.winterMult; break;
            }

            // Ereignisse anwenden
            if (activeEvents.hasPlague) amount *= 0.5f;     // Weniger Arbeiter
            if (activeEvents.isUnderSiege) amount *= 0.1f;  // Belagerung = fast nix geht

            int finalAmount = Mathf.FloorToInt(amount);
            if (finalAmount > 0)
            {
                AddMarketStock(wareString, finalAmount);
            }
        }
    }

    // ------------------------------------------------------------
    // 7. INTERAKTION (MAUS KLICK)
    // ------------------------------------------------------------
    void OnMouseDown()
    {
        // Verhindern, dass man durch UI hindurchklickt
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        // Prüfen: Haben wir ein Schiff ausgewählt?
        if (PlayerManager.Instance != null && PlayerManager.Instance.selectedShip != null)
        {
            Ship selectedShip = PlayerManager.Instance.selectedShip;
            float distance = Vector3.Distance(transform.position, selectedShip.transform.position);

            // Ist das Schiff nah genug (im Hafen)?
            if (distance < 0.5f)
            {
                // Schiff ist da -> Stadt-Menü öffnen
                if (UIManager.Instance != null) UIManager.Instance.OpenCityMenu(this);

                // Schiff weiß jetzt: "Ich bin in dieser Stadt"
                selectedShip.currentCityLocation = this;
            }
            else
            {
                // Schiff ist woanders -> REISE STARTEN (Wegfindung)
                ShipMovement movement = selectedShip.GetComponent<ShipMovement>();

                // Startpunkt ermitteln (Wo war das Schiff zuletzt?)
                City startCity = selectedShip.currentCityLocation;

                if (startCity != null && movement != null)
                {
                    // Befehl an Schiff: Fahr von Start nach Hier
                    movement.SetDestination(startCity, this);
                }
                else
                {
                    Debug.LogWarning("Schiff hat keinen bekannten Start-Hafen! (Evtl. manuell setzen)");
                }
            }
        }
        else
        {
            // Kein Schiff ausgewählt -> Nur Info-Menü öffnen (Cheat/Debug/Ansicht)
            if (UIManager.Instance != null) UIManager.Instance.OpenCityMenu(this);
        }
    }

    // ------------------------------------------------------------
    // 8. HILFSMETHODEN (API)
    // ------------------------------------------------------------

    // Prüft, ob diese Stadt eine Ware herstellt (für Preis-Bonus)
    public bool DoesProduce(string wareName)
    {
        foreach (var p in productionLines)
        {
            if (p.ware.ToString() == wareName) return true;
        }
        return false;
    }

    // Preis abfragen (delegiert an EconomySystem)
    public int GetCurrentPrice(string ware)
    {
        return EconomySystem.CalculatePrice(ware, GetMarketStock(ware), this);
    }

    // --- MARKT INVENTAR ---
    public int GetMarketStock(string ware)
    {
        return marketInventory.ContainsKey(ware) ? marketInventory[ware] : 0;
    }

    public void RemoveMarketStock(string ware, int amount)
    {
        if (marketInventory.ContainsKey(ware))
            marketInventory[ware] -= amount;

        if (marketInventory[ware] < 0) marketInventory[ware] = 0;
    }

    public void AddMarketStock(string ware, int amount)
    {
        if (marketInventory.ContainsKey(ware))
            marketInventory[ware] += amount;
        else
            marketInventory.Add(ware, amount);
    }

    // --- KONTOR INVENTAR ---
    public int GetKontorStock(string ware)
    {
        return kontorInventory.ContainsKey(ware) ? kontorInventory[ware] : 0;
    }

    public void AddToKontor(string ware, int amount)
    {
        if (kontorInventory.ContainsKey(ware))
            kontorInventory[ware] += amount;
        else
            kontorInventory.Add(ware, amount);

        if (kontorInventory[ware] < 0) kontorInventory[ware] = 0;
    }
}