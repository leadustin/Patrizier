using UnityEngine;
using System.Collections.Generic;

public class Ship : MonoBehaviour
{
    [Header("Bauplan")]
    public ShipType type;

    [Header("Aktueller Zustand")]
    public string shipName;
    public float currentHealth;

    [Header("Upgrades")]
    public int extraCargoSpace = 0; // Das ist neu: Hier speichern wir den Ausbau

    public City currentCityLocation;
    public int currentCargoLoad = 0;
    public Dictionary<string, int> cargo = new Dictionary<string, int>();

    void Start()
    {
        if (type != null && currentHealth <= 0) currentHealth = type.maxHealth;
    }

    // WICHTIG: Wir rechnen den Extra-Platz dazu
    public int GetMaxCargo()
    {
        return (type != null ? type.maxCargo : 0) + extraCargoSpace;
    }

    public int GetStock(string ware) { return cargo.ContainsKey(ware) ? cargo[ware] : 0; }

    public void AddCargo(string ware, int amount)
    {
        if (cargo.ContainsKey(ware)) cargo[ware] += amount; else cargo.Add(ware, amount);
        currentCargoLoad += amount;
    }

    public void RemoveCargo(string ware, int amount)
    {
        if (cargo.ContainsKey(ware)) { cargo[ware] -= amount; if (cargo[ware] < 0) cargo[ware] = 0; }
        currentCargoLoad -= amount; if (currentCargoLoad < 0) currentCargoLoad = 0;
    }

    public int CalculateRepairCost()
    {
        if (type == null) return 0;
        return Mathf.CeilToInt((type.maxHealth - currentHealth) * 10);
    }

    public void Repair() { if (type != null) currentHealth = type.maxHealth; }
}