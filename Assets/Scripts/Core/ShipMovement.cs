using UnityEngine;

public class ShipMovement : MonoBehaviour
{
    [Header("Reise Einstellungen")]
    public float travelSpeed = 2.0f; // Geschwindigkeit auf der Karte

    private Ship myShipData; // Zugriff auf die Schiffsdaten (z.B. Speed-Bonus)
    private City targetCity;
    private bool isSailing = false;

    void Start()
    {
        myShipData = GetComponent<Ship>();
    }

    void Update()
    {
        if (isSailing && targetCity != null)
        {
            MoveTowardsTarget();
        }
    }

    public void SetDestination(City city)
    {
        targetCity = city;
        isSailing = true;

        // Schiff drehen (Optional, sieht aber gut aus)
        Vector3 dir = city.transform.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Debug.Log($"Kurs gesetzt auf {city.cityName}");
    }

    void MoveTowardsTarget()
    {
        // Geschwindigkeit berechnen (Basis * Typ-Faktor)
        float speed = travelSpeed;
        if (myShipData != null && myShipData.type != null)
        {
            speed = myShipData.type.speed * 0.5f; // Faktor zum Ausbalancieren
        }

        // Bewegen
        transform.position = Vector3.MoveTowards(transform.position, targetCity.transform.position, speed * Time.deltaTime);

        // Ankunft prüfen
        if (Vector3.Distance(transform.position, targetCity.transform.position) < 0.1f)
        {
            ArriveAtDestination();
        }
    }

    void ArriveAtDestination()
    {
        isSailing = false;
        Debug.Log("Angekommen in " + targetCity.cityName);

        // Automatisch Stadt-Menü öffnen
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenCityMenu(targetCity);
        }
    }
}