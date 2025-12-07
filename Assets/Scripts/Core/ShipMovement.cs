using UnityEngine;
using System.Collections.Generic;

public class ShipMovement : MonoBehaviour
{
    public float travelSpeed = 2.0f;
    public float rotationSpeed = 2.0f;

    private Ship myShipData;

    // Die aktuelle Route
    private List<SeaLane> pathLanes = new List<SeaLane>();
    private int currentLaneIndex = 0;

    // Fortschritt auf der aktuellen Straße (0.0 bis 1.0)
    private float laneProgress = 0f;
    private bool isMovingForwardOnLane = true; // Fahren wir Start->Ende oder Ende->Start?

    private bool isSailing = false;
    private City finalDestination;

    void Start()
    {
        myShipData = GetComponent<Ship>();
    }

    void Update()
    {
        if (isSailing)
        {
            MoveAlongPath();
        }
    }

    public void SetDestination(City start, City end)
    {
        if (SeaGrid.Instance == null) return;

        finalDestination = end;

        // 1. Nächstgelegene Knoten finden (Einstieg ins Netz)
        SeaNode startNode = SeaGrid.Instance.GetClosestNode(transform.position); // Wo bin ich?
        SeaNode endNode = SeaGrid.Instance.GetClosestNode(end.transform.position); // Wo will ich hin?

        // 2. Pfad berechnen
        pathLanes = SeaGrid.Instance.FindPath(startNode, endNode);

        if (pathLanes != null && pathLanes.Count > 0)
        {
            currentLaneIndex = 0;
            SetupNextLane();
            isSailing = true;

            // UI schließen
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseCityMenu();
                UIManager.Instance.CloseShipStatus();
            }
        }
        else
        {
            // Spezialfall: Start == Ziel oder kein Weg
            if (startNode == endNode) Arrive(); // Schon da
        }
    }

    void SetupNextLane()
    {
        if (currentLaneIndex >= pathLanes.Count)
        {
            Arrive();
            return;
        }

        SeaLane lane = pathLanes[currentLaneIndex];

        // Prüfen: Wo sind wir? Am Start oder Ende der Lane?
        float distToStart = Vector3.Distance(transform.position, lane.startNode.transform.position);
        float distToEnd = Vector3.Distance(transform.position, lane.endNode.transform.position);

        if (distToStart < distToEnd)
        {
            isMovingForwardOnLane = true; // Start -> Ende
            laneProgress = 0f;
        }
        else
        {
            isMovingForwardOnLane = false; // Ende -> Start
            laneProgress = 1f;
        }
    }

    void MoveAlongPath()
    {
        SeaLane currentLane = pathLanes[currentLaneIndex];

        // Geschwindigkeit (in t pro Sekunde)
        // t muss basierend auf der Länge der Kurve skaliert werden, sonst fahren wir bei langen Kurven langsam und kurzen schnell
        // Vereinfacht: Distanz Start-Ende
        float laneLength = Vector3.Distance(currentLane.startNode.transform.position, currentLane.endNode.transform.position);
        float speedUnits = (myShipData.type != null && myShipData.type.speed > 0) ? myShipData.type.speed * 0.5f : 2.0f;

        float speedT = (speedUnits / laneLength) * Time.deltaTime;

        if (isMovingForwardOnLane)
        {
            laneProgress += speedT;
            if (laneProgress >= 1f)
            {
                currentLaneIndex++;
                SetupNextLane();
                return;
            }
        }
        else
        {
            laneProgress -= speedT;
            if (laneProgress <= 0f)
            {
                currentLaneIndex++;
                SetupNextLane();
                return;
            }
        }

        // Position & Rotation setzen
        Vector3 targetPos = currentLane.GetPointAt(laneProgress);

        Vector3 dir = targetPos - transform.position;
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.AngleAxis(angle, Vector3.forward), rotationSpeed * Time.deltaTime);
        }
        transform.position = targetPos;
    }

    void Arrive()
    {
        isSailing = false;
        if (myShipData != null) myShipData.currentCityLocation = finalDestination;
        if (UIManager.Instance != null) UIManager.Instance.OpenCityMenu(finalDestination);
    }
}