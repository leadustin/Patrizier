using UnityEngine;
using System.Collections.Generic;

public class Ship : MonoBehaviour
{
    [Header("Bauplan")]
    public ShipType type; // Verweis auf die Daten (Kogge/Schnigge)

    [Header("Aktueller Zustand")]
    public string shipName;
    public float currentHealth;

    // Ladung
    public int currentCargoLoad = 0;
    public Dictionary<string, int> cargo = new Dictionary<string, int>();

    void Start()
    {
        // Falls das Schiff frisch gespawnt wurde und leer ist: Werte setzen
        if (type != null && currentHealth <= 0)
        {
            currentHealth = type.maxHealth;
        }
    }

    // --- LOGIK ---

    public int GetMaxCargo()
    {
        return type != null ? type.maxCargo : 0;
    }

    public int GetStock(string ware)
    {
        return cargo.ContainsKey(ware) ? cargo[ware] : 0;
    }

    public void AddCargo(string ware, int amount)
    {
        if (cargo.ContainsKey(ware)) cargo[ware] += amount;
        else cargo.Add(ware, amount);
        currentCargoLoad += amount;
    }

    public void RemoveCargo(string ware, int amount)
    {
        if (cargo.ContainsKey(ware))
        {
            cargo[ware] -= amount;
            if (cargo[ware] < 0) cargo[ware] = 0;
        }
        currentCargoLoad -= amount;
        if (currentCargoLoad < 0) currentCargoLoad = 0;
    }

    // --- REPARATUR ---
    public int CalculateRepairCost()
    {
        if (type == null) return 0;
        float damage = type.maxHealth - currentHealth;
        // 1 Schadenspunkt = 10 Gold (Beispiel)
        return Mathf.CeilToInt(damage * 10);
    }

    public void Repair()
    {
        if (type != null) currentHealth = type.maxHealth;
    }

    void OnMouseDown()
    {
        // Verhindern, dass man durch UI hindurchklickt (falls Schiff unter einem Button liegt)
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        Debug.Log("Schiff angeklickt: " + shipName);

        if (UIManager.Instance != null)
        {
            // Wir sagen dem UI: "Zeig MICH an!"
            UIManager.Instance.OpenShipStatus(this);

            // Optional: Dieses Schiff auch gleich als "selectedShip" im Manager setzen?
            // PlayerManager.Instance.selectedShip = this; 
        }
    }
}