using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    public string key;

    private TextMeshProUGUI textComp;

    // Startet beim Spielstart
    void Start()
    {
        textComp = GetComponent<TextMeshProUGUI>();
        UpdateText();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (textComp == null) textComp = GetComponent<TextMeshProUGUI>();

        // Sicherheitscheck, falls Manager noch nicht da ist (passiert im Editor oft)
        if (LocalizationManager.Instance != null)
        {
            textComp.text = LocalizationManager.Instance.Get(key);
        }
    }

    // --- DAS IST NEU: Editor-Vorschau ---
    // Diese Methode wird aufgerufen, sobald du im Inspector etwas änderst
    void OnValidate()
    {
        // Wir setzen den Text im Editor nur temporär auf den Key, 
        // damit du siehst, welchen Key du eingetragen hast.
        // Echte Übersetzung geht im Editor nur schwer ohne laufendes Spiel.
        if (!Application.isPlaying)
        {
            textComp = GetComponent<TextMeshProUGUI>();
            if (textComp != null)
                textComp.text = "[" + key + "]";
        }
    }
}