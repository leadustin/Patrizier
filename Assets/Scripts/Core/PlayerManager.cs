using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class ShipOrder
{
    public string shipName;
    public ShipType type;
    public int daysLeft;
    public City location;
}

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("Finanzen")]
    public int currentGold = 10000;

    [Header("Schiff Verwaltung")]
    public GameObject shipPrefab;
    public Ship selectedShip;

    [Header("Warteschlange")]
    public List<ShipOrder> buildQueue = new List<ShipOrder>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged += ProcessQueue;
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged -= ProcessQueue;
    }

    // ------------------------------------------------------------
    // 1. BAU-LOGIK
    // ------------------------------------------------------------

    public (int totalCost, bool canBuild) CalculateBuildCost(ShipType type, City city, bool useOwnMaterials)
    {
        int materialCost = 0;
        bool missingGoods = false;

        foreach (var req in type.requiredResources)
        {
            int needed = req.amount;

            // A) Eigene Waren nutzen?
            if (useOwnMaterials)
            {
                int inKontor = city.GetKontorStock(req.wareName);
                if (inKontor >= needed)
                {
                    needed = 0; // Haben wir komplett selbst
                }
                else
                {
                    needed -= inKontor; // Rest muss gekauft werden
                }
            }

            // B) Rest vom Markt kaufen
            if (needed > 0)
            {
                if (city.GetMarketStock(req.wareName) >= needed)
                {
                    // Preis mit Aufschlag bei Kauf von Stadt
                    int pricePerUnit = city.GetPrice(req.wareName, true);
                    materialCost += needed * pricePerUnit;
                }
                else
                {
                    missingGoods = true; // Stadt hat es auch nicht!
                }
            }
        }

        int total = type.baseBuildPrice + materialCost;
        bool possible = !missingGoods && currentGold >= total;

        return (total, possible);
    }

    public bool OrderShip(ShipType type, City city, bool useOwnMaterials)
    {
        var calculation = CalculateBuildCost(type, city, useOwnMaterials);

        if (!calculation.canBuild) return false;

        // 1. Gold abziehen
        currentGold -= calculation.totalCost;

        // 2. Waren entfernen
        foreach (var req in type.requiredResources)
        {
            int needed = req.amount;

            // Erst aus Kontor
            if (useOwnMaterials)
            {
                int inKontor = city.GetKontorStock(req.wareName);
                int takeFromKontor = Mathf.Min(inKontor, needed);

                city.AddToKontor(req.wareName, -takeFromKontor);
                needed -= takeFromKontor;
            }

            // Rest vom Markt
            if (needed > 0)
            {
                city.RemoveMarketStock(req.wareName, needed);
            }
        }

        // 3. Auftrag erstellen
        ShipOrder newOrder = new ShipOrder();
        newOrder.shipName = type.typeName;
        newOrder.type = type;
        newOrder.location = city;
        newOrder.daysLeft = type.buildTimeDays;

        buildQueue.Add(newOrder);
        Debug.Log($"Bau gestartet: {type.typeName} für {calculation.totalCost} Gold.");

        return true;
    }

    void ProcessQueue(DateTime date, Season season)
    {
        for (int i = buildQueue.Count - 1; i >= 0; i--)
        {
            ShipOrder order = buildQueue[i];
            order.daysLeft--;

            if (order.daysLeft <= 0)
            {
                SpawnShip(order.type, order.location);
                buildQueue.RemoveAt(i);
            }
        }
    }

    private void SpawnShip(ShipType type, City location)
    {
        GameObject newShipObj = Instantiate(shipPrefab, location.transform.position, Quaternion.identity);
        Ship newShip = newShipObj.GetComponent<Ship>();

        newShip.type = type;
        newShip.shipName = type.typeName + " (Neu)";
        newShip.currentHealth = type.maxHealth;
        newShip.currentCityLocation = location;

        // HIER WICHTIG: Das Schiff startet mit 0 verbrauchten Slots. 
        // Die "extraCargoSpace" Variable im Schiff müsste später durch ein komplexeres 
        // "Upgrade"-System ersetzt werden, wenn du Waffen/Segel einbaust.
        newShip.extraCargoSpace = 0;

        if (selectedShip == null) selectedShip = newShip;
    }

    // ------------------------------------------------------------
    // 2. EXISTIERENDE HANDELS-FUNKTIONEN
    // ------------------------------------------------------------
    public int GetGold() { return currentGold; }
    public int GetStock(string ware) { return selectedShip != null ? selectedShip.GetStock(ware) : 0; }

    public bool TryBuyFromCity(City city, string wareName, int amount, int unitPrice)
    {
        if (selectedShip == null) return false;
        if (currentGold < amount * unitPrice) return false;
        if (selectedShip.currentCargoLoad + amount > selectedShip.GetMaxCargo()) return false;
        if (city.GetMarketStock(wareName) < amount) return false;

        currentGold -= amount * unitPrice;
        selectedShip.AddCargo(wareName, amount);
        city.RemoveMarketStock(wareName, amount);
        return true;
    }

    public bool TrySellToCity(City city, string wareName, int amount, int unitPrice)
    {
        if (selectedShip == null || selectedShip.GetStock(wareName) < amount) return false;
        currentGold += amount * unitPrice;
        selectedShip.RemoveCargo(wareName, amount);
        city.AddMarketStock(wareName, amount);
        return true;
    }

    public bool TransferToKontor(City city, string wareName, int amount)
    {
        if (selectedShip == null || selectedShip.GetStock(wareName) < amount) return false;
        selectedShip.RemoveCargo(wareName, amount);
        city.AddToKontor(wareName, amount);
        return true;
    }

    public bool TransferToShip(City city, string wareName, int amount)
    {
        if (selectedShip == null || city.GetKontorStock(wareName) < amount) return false;
        if (selectedShip.currentCargoLoad + amount > selectedShip.GetMaxCargo()) return false;
        city.AddToKontor(wareName, -amount);
        selectedShip.AddCargo(wareName, amount);
        return true;
    }

    // Platzhalter / Legacy Support
    public bool BuyShipInstant(ShipType type, City location) { return false; }
    public bool TryRepairShip()
    {
        if (selectedShip == null) return false;
        int cost = selectedShip.CalculateRepairCost();
        if (cost <= 0 || currentGold < cost) return false;
        currentGold -= cost;
        selectedShip.Repair();
        return true;
    }
    public void RenameShip(string newName) { if (selectedShip != null) selectedShip.shipName = newName; }
}