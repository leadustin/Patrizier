using UnityEngine;

public class MapNavigation : MonoBehaviour
{
    [Header("Map Settings")]
    public SpriteRenderer mapRenderer;

    [Header("Zoom Settings")]
    public float minZoom = 2f;
    public float maxZoom = 8f;
    public float zoomSpeedTouch = 0.5f;
    public float zoomSpeedMouse = 2f;

    private Vector3 dragOrigin;
    private Camera cam;
    private float mapMinX, mapMaxX, mapMinY, mapMaxY;
    private float mapWidth, mapHeight;

    void Start()
    {
        cam = Camera.main;

        if (mapRenderer != null)
        {
            mapWidth = mapRenderer.bounds.size.x;
            mapHeight = mapRenderer.bounds.size.y;

            mapMinX = mapRenderer.transform.position.x - mapWidth / 2f;
            mapMaxX = mapRenderer.transform.position.x + mapWidth / 2f;
            mapMinY = mapRenderer.transform.position.y - mapHeight / 2f;
            mapMaxY = mapRenderer.transform.position.y + mapHeight / 2f;
        }
    }

    void LateUpdate()
    {
        if (mapRenderer == null) return;

        HandleZoom();
        HandlePan();
        ClampCamera(); // Hält die Kamera immer innerhalb der Grenzen
    }

    void HandleZoom()
    {
        // --- PC / Editor (Mausrad) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0.0f)
        {
            // 1. Wo ist die Maus in der Welt VOR dem Zoom?
            Vector3 mouseWorldPosBefore = cam.ScreenToWorldPoint(Input.mousePosition);

            // 2. Zoom durchführen
            cam.orthographicSize -= scroll * zoomSpeedMouse;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

            // 3. Wo wäre die Maus in der Welt NACH dem Zoom (ohne Verschiebung)?
            Vector3 mouseWorldPosAfter = cam.ScreenToWorldPoint(Input.mousePosition);

            // 4. Differenz berechnen und Kamera korrigieren
            // Wir schieben die Kamera um die Differenz, damit der Punkt unter der Maus gleich bleibt
            transform.position += (mouseWorldPosBefore - mouseWorldPosAfter);
        }

        // --- Mobile (2 Finger Pinch) ---
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            cam.orthographicSize += deltaMagnitudeDiff * zoomSpeedTouch * 0.01f;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    void HandlePan()
    {
        if (Input.touchCount >= 2) return;

        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 currentPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 difference = dragOrigin - currentPos;

            transform.position += difference;
        }
    }

    void ClampCamera()
    {
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float newX = transform.position.x;
        float newY = transform.position.y;

        // X Klemmen
        if (mapWidth > camWidth)
        {
            float minX = mapMinX + camWidth / 2f;
            float maxX = mapMaxX - camWidth / 2f;
            newX = Mathf.Clamp(newX, minX, maxX);
        }
        else
        {
            newX = mapRenderer.transform.position.x;
        }

        // Y Klemmen
        if (mapHeight > camHeight)
        {
            float minY = mapMinY + camHeight / 2f;
            float maxY = mapMaxY - camHeight / 2f;
            newY = Mathf.Clamp(newY, minY, maxY);
        }
        else
        {
            newY = mapRenderer.transform.position.y;
        }

        transform.position = new Vector3(newX, newY, -10f);
    }
}