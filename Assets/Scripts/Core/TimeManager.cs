using UnityEngine;
using System; // Wichtig für DateTime und Action

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Zeit Einstellungen")]
    public float secondsPerDay = 1.0f; // Ein Tag pro Sekunde (zum Testen)
    public bool isPaused = false;

    // Startdatum
    public DateTime currentDate = new DateTime(1300, 1, 1);

    // --- WICHTIG: Das Event muss ZWEI Parameter haben (Datum, Jahreszeit) ---
    public event Action<DateTime, Season> OnDayChanged;

    private float timer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (isPaused) return;

        timer += Time.deltaTime;
        if (timer >= secondsPerDay)
        {
            AdvanceDay();
            timer = 0;
        }
    }

    void AdvanceDay()
    {
        // Tag hochzählen
        currentDate = currentDate.AddDays(1);

        // Jahreszeit berechnen
        Season currentSeason = GetSeason(currentDate.Month);

        // --- WICHTIG: Beides absenden ---
        OnDayChanged?.Invoke(currentDate, currentSeason);
    }

    // Hilfsfunktion: Monat -> Jahreszeit
    public Season GetSeason(int month)
    {
        if (month >= 3 && month <= 5) return Season.Frühling;
        if (month >= 6 && month <= 8) return Season.Sommer;
        if (month >= 9 && month <= 11) return Season.Herbst;
        return Season.Winter; // Dez, Jan, Feb
    }

    public string GetFormattedDate()
    {
        return currentDate.ToString("dd. MMMM yyyy");
    }
}