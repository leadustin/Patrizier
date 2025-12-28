using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

[System.Serializable]
public class ShipOrder
{
    public string shipName;
    public ShipType type;      // Für Neubau
    public Ship existingShip;  // Für Reparatur
    public int daysLeft;
    public City location;

    public bool isRepair;      // Unterscheidung Bau vs. Reparatur
    public bool isPlayerShip;  // Für KI-Konkurrenz
}

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("Finanzen")]
    public int currentGold = 10000;

    [Header("Schiff Verwaltung")]
    public GameObject shipPrefab;
    public Ship selectedShip;

    [Header("Werft Einstellungen")]
    public int defaultShipyardCapacity = 1;

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

    // ============================================================
    // 1. WERFT & QUEUE LOGIK
    // ============================================================

    void ProcessQueue(DateTime date, Season season)
    {
        Dictionary<City, int> activeJobsPerCity = new Dictionary<City, int>();

        for (int i = 0; i < buildQueue.Count; i++)
        {
            ShipOrder order = buildQueue[i];

            if (!activeJobsPerCity.ContainsKey(order.location)) activeJobsPerCity[order.location] = 0;

            if (activeJobsPerCity[order.location] < defaultShipyardCapacity)
            {
                order.daysLeft--;
                activeJobsPerCity[order.location]++;

                if (order.daysLeft <= 0)
                {
                    FinishOrder(order);
                    buildQueue.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    private void FinishOrder(ShipOrder order)
    {
        if (order.isRepair && order.existingShip != null)
        {
            order.existingShip.currentHealth = order.existingShip.type.maxHealth;
            order.existingShip.isUnderRepair = false;
            // Falls Mannschaft entlassen wurde, ist sie jetzt 0 (wurde beim OrderRepair schon gesetzt)
            Debug.Log($"Reparatur abgeschlossen: {order.shipName}");
        }
        else if (!order.isRepair)
        {
            SpawnShip(order.type, order.location, order.shipName);
        }
    }

    // --- Neubau Berechnungen ---
    public (int totalCost, bool canBuild) CalculateBuildCost(ShipType type, City city, bool useOwnMaterials)
    {
        int materialCost = 0;
        bool missingGoods = false;

        foreach (var req in type.requiredResources)
        {
            string wName = req.wareType.ToString();
            int needed = req.amount;
            if (useOwnMaterials)
            {
                int inKontor = city.GetKontorStock(wName);
                if (inKontor >= needed) needed = 0; else needed -= inKontor;
            }
            if (needed > 0)
            {
                if (city.GetMarketStock(wName) >= needed) materialCost += needed * city.GetPrice(wName, true);
                else missingGoods = true;
            }
        }
        int total = type.baseBuildPrice + materialCost;
        return (total, !missingGoods && currentGold >= total);
    }

    public bool OrderShip(ShipType type, City city, bool useOwnMaterials, string customName)
    {
        var calculation = CalculateBuildCost(type, city, useOwnMaterials);
        if (!calculation.canBuild) return false;

        currentGold -= calculation.totalCost;
        ConsumeMaterials(type.requiredResources, city, useOwnMaterials);

        ShipOrder newOrder = new ShipOrder();
        newOrder.shipName = string.IsNullOrEmpty(customName) ? type.typeName : customName;
        newOrder.type = type;
        newOrder.location = city;
        newOrder.daysLeft = type.buildTimeDays;
        newOrder.isRepair = false;
        newOrder.isPlayerShip = true;

        buildQueue.Add(newOrder);
        return true;
    }

    public bool BuyShipInstant(ShipType type, City city, int totalCost, string customName)
    {
        if (currentGold < totalCost) return false;
        currentGold -= totalCost;
        SpawnShip(type, city, customName);
        return true;
    }

    // --- Reparatur Berechnungen ---
    public (int goldCost, int days, List<ResourceRequirement> requiredMats) CalculateRepairRequirements(Ship ship)
    {
        List<ResourceRequirement> mats = new List<ResourceRequirement>();
        if (ship == null || ship.type == null) return (0, 0, mats);

        float damageRatio = 1.0f - (ship.currentHealth / (float)ship.type.maxHealth);
        if (damageRatio <= 0.001f) return (0, 0, mats);

        int days = Mathf.CeilToInt(ship.type.buildTimeDays * damageRatio);
        if (days < 1) days = 1;

        int goldCost = Mathf.CeilToInt(ship.type.baseBuildPrice * 0.2f * damageRatio);

        foreach (var req in ship.type.requiredResources)
        {
            ResourceRequirement repairReq = req;
            repairReq.amount = Mathf.CeilToInt(req.amount * damageRatio * 0.5f); // 50% Effizienz
            if (repairReq.amount > 0) mats.Add(repairReq);
        }

        return (goldCost, days, mats);
    }

    public bool OrderRepair(Ship ship, City city, bool useOwnMaterials, bool layoffCrew)
    {
        var reqs = CalculateRepairRequirements(ship);

        int matCostGold = 0;
        bool missingGoods = false;

        foreach (var mat in reqs.requiredMats)
        {
            string wName = mat.wareType.ToString();
            int needed = mat.amount;
            if (useOwnMaterials)
            {
                int inKontor = city.GetKontorStock(wName);
                if (inKontor >= needed) needed = 0; else needed -= inKontor;
            }
            if (needed > 0)
            {
                if (city.GetMarketStock(wName) >= needed) matCostGold += needed * city.GetPrice(wName, true);
                else missingGoods = true;
            }
        }

        int totalGoldNeeded = reqs.goldCost + matCostGold;
        if (missingGoods || currentGold < totalGoldNeeded) return false;

        currentGold -= totalGoldNeeded;
        ConsumeMaterials(reqs.requiredMats, city, useOwnMaterials);

        if (layoffCrew) ship.currentCrew = 0;
        ship.isUnderRepair = true;

        ShipOrder repairOrder = new ShipOrder();
        repairOrder.shipName = ship.shipName;
        repairOrder.existingShip = ship;
        repairOrder.location = city;
        repairOrder.daysLeft = reqs.days;
        repairOrder.isRepair = true;
        repairOrder.isPlayerShip = true;

        buildQueue.Add(repairOrder);
        Debug.Log($"Reparatur beauftragt: {ship.shipName}");
        return true;
    }

    private void ConsumeMaterials(List<ResourceRequirement> mats, City city, bool useOwn)
    {
        foreach (var req in mats)
        {
            string wName = req.wareType.ToString();
            int needed = req.amount;
            if (useOwn)
            {
                int inKontor = city.GetKontorStock(wName);
                int take = Mathf.Min(inKontor, needed);
                city.AddToKontor(wName, -take);
                needed -= take;
            }
            if (needed > 0) city.RemoveMarketStock(wName, needed);
        }
    }

    public void SpawnShip(ShipType type, City location, string nameOverride = "")
    {
        if (location == null) return;
        GameObject newShipObj = Instantiate(shipPrefab, location.transform.position, Quaternion.identity);
        Ship newShip = newShipObj.GetComponent<Ship>();

        newShip.type = type;
        newShip.shipName = string.IsNullOrEmpty(nameOverride) ? (type.typeName + " (Neu)") : nameOverride;
        newShip.currentHealth = type.maxHealth;
        newShip.currentCityLocation = location;
        newShip.currentCrew = 0;

        if (selectedShip == null) selectedShip = newShip;
    }

    // ============================================================
    // 2. HANDEL & LOGISTIK (FIX: JETZT MIT ÜBERLADUNGEN)
    // ============================================================

    public int GetStock(string ware)
    {
        if (selectedShip == null) return 0;
        return selectedShip.GetStock(ware);
    }

    // Markt -> Schiff (Kaufen) - HAUPTMETHODE
    public bool TryBuyFromCity(City city, string ware, int amount)
    {
        if (selectedShip == null || city == null) return false;

        int price = city.GetPrice(ware, true);
        int cost = price * amount;

        if (currentGold < cost) return false;
        if (city.GetMarketStock(ware) < amount) return false;
        if (selectedShip.currentCargoLoad + amount > selectedShip.GetMaxCargo()) return false;

        currentGold -= cost;
        city.RemoveMarketStock(ware, amount);
        selectedShip.AddCargo(ware, amount);

        return true;
    }

    // Schiff -> Markt (Verkaufen) - HAUPTMETHODE
    public bool TrySellToCity(City city, string ware, int amount)
    {
        if (selectedShip == null || city == null) return false;
        if (selectedShip.GetStock(ware) < amount) return false;

        int price = city.GetPrice(ware, false);
        int revenue = price * amount;

        selectedShip.RemoveCargo(ware, amount);
        city.RemoveMarketStock(ware, -amount); // Negativ entfernen = Hinzufügen

        currentGold += revenue;
        return true;
    }

    // --- NEU: OVERLOADS FÜR 4 PARAMETER (FIX FÜR MARKETROW) ---
    // Diese Methoden fangen den Aufruf mit dem Preis ab und leiten ihn weiter.
    public bool TryBuyFromCity(City city, string ware, int amount, int explicitPrice)
    {
        return TryBuyFromCity(city, ware, amount);
    }

    public bool TrySellToCity(City city, string ware, int amount, int explicitPrice)
    {
        return TrySellToCity(city, ware, amount);
    }

    // Kontor -> Schiff
    public bool TransferToShip(City city, string ware, int amount)
    {
        if (selectedShip == null || city == null) return false;
        if (city.GetKontorStock(ware) < amount) return false;
        if (selectedShip.currentCargoLoad + amount > selectedShip.GetMaxCargo()) return false;

        city.AddToKontor(ware, -amount);
        selectedShip.AddCargo(ware, amount);
        return true;
    }

    // Schiff -> Kontor
    public bool TransferToKontor(City city, string ware, int amount)
    {
        if (selectedShip == null || city == null) return false;
        if (selectedShip.GetStock(ware) < amount) return false;

        selectedShip.RemoveCargo(ware, amount);
        city.AddToKontor(ware, amount);
        return true;
    }

    // ============================================================
    // 3. LEGACY & HELPER
    // ============================================================

    public int CalculateSellPrice()
    {
        if (selectedShip == null) return 0;
        float healthPercent = (float)selectedShip.currentHealth / (float)selectedShip.type.maxHealth;
        float baseValue = (float)selectedShip.type.baseBuildPrice;
        return Mathf.FloorToInt(baseValue * 0.5f * healthPercent);
    }

    public bool SellShip()
    {
        if (selectedShip == null) return false;
        int price = CalculateSellPrice();
        currentGold += price;
        Destroy(selectedShip.gameObject);
        selectedShip = null;
        return true;
    }

    public void RenameShip(string n) { if (selectedShip) selectedShip.shipName = n; }
    public int GetGold() => currentGold;
}