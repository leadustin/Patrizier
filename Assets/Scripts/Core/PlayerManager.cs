using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("Finanzen")]
    public int currentGold = 2000;

    [Header("Schiff")]
    public int maxCargo = 100;
    public int currentCargo = 0;
    public Dictionary<string, int> inventory = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int GetStock(string wareName)
    {
        if (inventory.ContainsKey(wareName)) return inventory[wareName];
        return 0;
    }

    // --- NEU: HANDEL MIT DER STADT (Economy Phase 7) ---

    // Kaufen: Stadt -> Schiff
    // Diese Methode wird vom MarketRow aufgerufen!
    public bool TryBuyFromCity(City city, string wareName, int amount, int unitPrice)
    {
        int totalCost = amount * unitPrice;

        // 1. Checks
        if (currentGold < totalCost) { Debug.Log("Zu wenig Gold"); return false; }
        if (currentCargo + amount > maxCargo) { Debug.Log("Schiff voll"); return false; }

        // Hat die Stadt genug? (Wichtig für die Wirtschaft)
        if (city.GetMarketStock(wareName) < amount) { Debug.Log("Stadt hat nicht genug"); return false; }

        // 2. Transaktion
        currentGold -= totalCost;

        // Spieler bekommt Ware
        AddCargo(wareName, amount);

        // Stadt verliert Ware (Preis steigt beim nächsten Mal!)
        city.RemoveMarketStock(wareName, amount);

        return true;
    }

    // Verkaufen: Schiff -> Stadt
    public bool TrySellToCity(City city, string wareName, int amount, int unitPrice)
    {
        if (GetStock(wareName) < amount) return false;

        // Transaktion
        currentGold += (amount * unitPrice);

        // Spieler gibt Ware ab
        RemoveCargo(wareName, amount);

        // Stadt bekommt Ware (Preis sinkt beim nächsten Mal!)
        city.AddMarketStock(wareName, amount);

        return true;
    }

    // --- INTERNE LOGISTIK (Hilfsfunktionen) ---

    void AddCargo(string ware, int amount)
    {
        currentCargo += amount;
        if (inventory.ContainsKey(ware)) inventory[ware] += amount;
        else inventory.Add(ware, amount);
    }

    void RemoveCargo(string ware, int amount)
    {
        currentCargo -= amount;
        inventory[ware] -= amount;
        if (inventory[ware] < 0) inventory[ware] = 0;
    }

    // --- VERSCHIEBEN (Schiff <-> Kontor) ---

    public bool TransferToKontor(City city, string wareName, int amount)
    {
        if (GetStock(wareName) < amount) return false;
        RemoveCargo(wareName, amount);
        city.AddToKontor(wareName, amount);
        return true;
    }

    public bool TransferToShip(City city, string wareName, int amount)
    {
        if (city.GetKontorStock(wareName) < amount) return false;
        if (currentCargo + amount > maxCargo) return false;

        city.AddToKontor(wareName, -amount);
        AddCargo(wareName, amount);
        return true;
    }

    public int GetGold() { return currentGold; }
}