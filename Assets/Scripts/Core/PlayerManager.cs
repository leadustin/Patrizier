using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("Finanzen")]
    public int currentGold = 5000; // Genug Startkapital für ein Schiff

    [Header("Schiff Verwaltung")]
    public GameObject shipPrefab; // Das Aussehen (Prefab)
    public Ship selectedShip;     // Das aktive Schiff (am Anfang LEER/NULL)

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- SCHIFF KAUFEN (Der wichtigste Teil) ---
    public bool BuyShip(ShipType type, City location)
    {
        // 1. Haben wir genug Geld?
        if (currentGold < type.basePrice)
        {
            Debug.Log("Nicht genug Gold!");
            return false;
        }

        // 2. Geld abziehen
        currentGold -= type.basePrice;

        // 3. Schiff in der Welt erstellen (am Ort der Stadt)
        // Wir nutzen das generische Prefab, aber füllen es mit den Daten des Typs
        GameObject newShipObj = Instantiate(shipPrefab, location.transform.position, Quaternion.identity);

        Ship newShip = newShipObj.GetComponent<Ship>();
        newShip.type = type; // WICHTIG: Daten zuweisen
        newShip.shipName = type.typeName + " 1";
        newShip.currentHealth = type.maxHealth;
        newShip.currentCityLocation = location;

        // 4. Dem Spieler zuweisen
        selectedShip = newShip;

        Debug.Log("Schiff gekauft: " + newShip.shipName);
        return true;
    }

    // --- REPARIEREN ---
    public bool TryRepairShip()
    {
        if (selectedShip == null) return false;

        int cost = selectedShip.CalculateRepairCost();
        if (cost <= 0 || currentGold < cost) return false;

        currentGold -= cost;
        selectedShip.Repair();
        return true;
    }

    // --- UMBENENNEN ---
    public void RenameShip(string newName)
    {
        if (selectedShip != null) selectedShip.shipName = newName;
    }

    // --- HELPER FÜR HANDEL (Damit der Markt funktioniert) ---
    public int GetGold() { return currentGold; }

    public int GetStock(string ware)
    {
        return selectedShip != null ? selectedShip.GetStock(ware) : 0;
    }

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
}