using UnityEngine;

[CreateAssetMenu(fileName = "NewShipType", menuName = "Patrizier/ShipType")]
public class ShipType : ScriptableObject
{
    [Header("Basis Daten")]
    public string typeName = "Schnigge";
    [TextArea] public string description = "Ein kleines, wendiges Handelsschiff.";

    [Header("Werte")]
    public int maxCargo = 150;      // Laderaum
    public float maxHealth = 100f;  // Struktur
    public int basePrice = 1500;    // Kaufpreis in der Werft
    public float speed = 5.0f;

    [Header("Kampfwerte")]
    public int weaponSlots = 2;
    // Hier kannst du später WeaponSize Enum nutzen, wenn du willst
}