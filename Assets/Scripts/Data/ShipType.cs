using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ResourceRequirement
{
    public string wareName; // z.B. "Holz"
    public int amount;      // z.B. 20
    public Sprite icon;     // Bild der Ware für die UI
}

[CreateAssetMenu(fileName = "NewShipType", menuName = "Patrizier/ShipType")]
public class ShipType : ScriptableObject
{
    [Header("Basis Daten")]
    public string typeName = "Schnigge";
    [TextArea] public string description = "Ein kleines Handelsschiff.";
    public Sprite icon; // Das große Bild oben

    [Header("Bau-Anforderungen")]
    public int baseBuildPrice = 1500; // Arbeitslohn der Werft
    public int buildTimeDays = 14;    // Bauzeit
    public List<ResourceRequirement> requiredResources; // Material-Liste

    [Header("Die 6 Status-Werte")]
    public int maxCargo = 150;           // Basis-Laderaum
    public int maneuverability = 100;    // Wendigkeit in %
    public int maxHealth = 100;          // Trefferpunkte
    public int dailyMaintenance = 5;     // Tägliche Kosten

    [Tooltip("Anzahl der freien Plätze für Waffen ODER andere Upgrades")]
    public int upgradeSlots = 2;         // HIER GEÄNDERT: "Upgrade Slots" statt "Weapon Slots"

    public bool isRiverCapable = true;   // Kann auf Flüssen fahren?

    // Alte Variable für Kompatibilität
    public float speed = 5.0f;
}