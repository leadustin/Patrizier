using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ResourceRequirement
{
    public WareType wareType;
    public int amount;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "NewShipType", menuName = "Patrizier/ShipType")]
public class ShipType : ScriptableObject
{
    [Header("Basis Daten")]
    public string typeName = "Schnigge";
    [TextArea] public string description = "Ein kleines Handelsschiff.";
    public Sprite icon;

    [Header("Bau-Anforderungen")]
    public int baseBuildPrice = 1500;
    public int buildTimeDays = 14;
    public List<ResourceRequirement> requiredResources;

    [Header("Die 6 Status-Werte")]
    public int maxCargo = 150;
    public int maneuverability = 100;
    public int maxHealth = 100;
    public int dailyMaintenance = 5;
    public int upgradeSlots = 2;
    public int maxCrew = 20; // WICHTIG: Das hat gefehlt!

    public bool isRiverCapable = true;
    public float speed = 5.0f;
}