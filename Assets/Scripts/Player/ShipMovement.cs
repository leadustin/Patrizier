using UnityEngine;
using System.Collections.Generic;

public class ShipMovement : MonoBehaviour
{
    public float travelSpeed = 2.0f;
    public float rotationSpeed = 5.0f; // Etwas höher drehen für Splines

    private Ship myShipData;
    private Queue<Vector3> waypointQueue = new Queue<Vector3>();
    private Vector3 currentTarget;
    private bool isSailing = false;
    private City finalDestination;

    void Start()
    {
        myShipData = GetComponent<Ship>();
    }

    void Update()
    {
        if (isSailing) MoveShip();
    }

    public void SetDestination(City start, City end)
    {
        if (SeaGrid.Instance == null) return;
        finalDestination = end;

        // Route holen (Das sind jetzt SEHR VIELE kleine Punkte für die Kurve)
        waypointQueue = SeaGrid.Instance.GetRoute(transform.position, end.transform.position);

        if (waypointQueue.Count > 0)
        {
            currentTarget = waypointQueue.Dequeue();
            isSailing = true;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseCityMenu();
                UIManager.Instance.CloseShipStatus();
            }
        }
    }

    void MoveShip()
    {
        float speed = travelSpeed;
        if (myShipData != null && myShipData.type != null && myShipData.type.speed > 0)
            speed = myShipData.type.speed * 0.5f;

        // Bewegung
        transform.position = Vector3.MoveTowards(transform.position, currentTarget, speed * Time.deltaTime);

        // Rotation
        Vector3 dir = currentTarget - transform.position;
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Punkt erreicht? (Kleinerer Radius, da Punkte dicht beieinander liegen)
        if (Vector3.Distance(transform.position, currentTarget) < 0.05f)
        {
            if (waypointQueue.Count > 0) currentTarget = waypointQueue.Dequeue();
            else Arrive();
        }
    }

    void Arrive()
    {
        isSailing = false;
        if (myShipData != null) myShipData.currentCityLocation = finalDestination;
        if (UIManager.Instance != null) UIManager.Instance.OpenCityMenu(finalDestination);
    }
}