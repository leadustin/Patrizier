// Diese Datei hat KEIN "class GameEnums : MonoBehaviour" drumherum.
// Die Enums stehen "frei", damit jeder (UIManager, City, TimeManager) sie kennt.

public enum Season
{
    Frühling,
    Sommer,
    Herbst,
    Winter
}

public enum WareType
{
    // --- 1. GRUNDWAREN (Nahrungsmittel) ---
    Getreide,
    Mehl,
    Brot,
    Fisch,
    Fleisch,
    Honig,
    Bier,
    Salz,

    // --- 2. ROHSTOFFE ---
    Holz,   // Baumstämme (für Produktion & Bau)
    Erz,    // Eisenerz
    Wolle,  // Schafswolle
    Hanf,   // Für Taue/Segel
    Ton,    // Für Ziegel

    // --- 3. WEITERVERARBEITET (Handwerk / Industrie) ---
    Bretter,    // Aus Holz (Sägewerk)
    Eisenwaren, // Aus Erz (Schmiede) - Werkzeuge/Waffen
    Stoff,      // Aus Wolle (Weberei)
    Leder,      // Aus Häuten/Fleischproduktion? (Gerberei)
    Pech,       // Aus Holz/Harz (Pechsiederei)
    Ziegel,     // Aus Ton (Ziegelei)

    // --- 4. LUXUSWAREN (Reiche Bürger) ---
    Gewuerze,
    Wein,
    Schmuck,
    Samt,
    Pelze
}

public enum WeaponSize
{
    Small,  // z.B. kleine Steinbüchse
    Medium, // z.B. Balliste
    Large   // z.B. Große Bombarde
}

public enum CityType
{
    Hansestadt, // Vollwertiges Mitglied (Lübeck, Hamburg...)
    Kontor,     // Große Auslandsniederlassung (London, Brügge...)
    Faktorei    // Kleiner Handelsposten (Optional für später)
}