using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SeaGrid : MonoBehaviour
{
    public static SeaGrid Instance;

    private Dictionary<SeaNode, List<SeaLane>> adjacencyList = new Dictionary<SeaNode, List<SeaLane>>();
    private List<SeaNode> allNodes = new List<SeaNode>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        BuildGraph();
    }

    public void BuildGraph()
    {
        adjacencyList.Clear();
        allNodes = FindObjectsOfType<SeaNode>().ToList();
        SeaLane[] allLanes = FindObjectsOfType<SeaLane>();

        foreach (var lane in allLanes)
        {
            if (lane.startNode == null || lane.endNode == null) continue;

            if (!adjacencyList.ContainsKey(lane.startNode)) adjacencyList[lane.startNode] = new List<SeaLane>();
            adjacencyList[lane.startNode].Add(lane);

            if (!adjacencyList.ContainsKey(lane.endNode)) adjacencyList[lane.endNode] = new List<SeaLane>();
            adjacencyList[lane.endNode].Add(lane);
        }
        Debug.Log($"SeaGrid: {allNodes.Count} Knoten und {allLanes.Length} Straßen vernetzt.");
    }

    public SeaNode GetClosestNode(Vector3 pos)
    {
        SeaNode best = null;
        float minDist = float.MaxValue;
        foreach (var node in allNodes)
        {
            float d = Vector3.Distance(pos, node.transform.position);
            if (d < minDist) { minDist = d; best = node; }
        }
        return best;
    }

    public Queue<Vector3> GetRoute(Vector3 startPos, Vector3 endPos)
    {
        SeaNode startNode = GetClosestNode(startPos);
        SeaNode targetNode = GetClosestNode(endPos);

        if (startNode == targetNode) return new Queue<Vector3>();

        // A* Algorithmus
        Dictionary<SeaNode, SeaNode> cameFrom = new Dictionary<SeaNode, SeaNode>();
        Dictionary<SeaNode, SeaLane> laneUsed = new Dictionary<SeaNode, SeaLane>();
        List<SeaNode> openSet = new List<SeaNode> { startNode };
        Dictionary<SeaNode, float> gScore = new Dictionary<SeaNode, float>();
        gScore[startNode] = 0;

        while (openSet.Count > 0)
        {
            SeaNode current = openSet.OrderBy(n => gScore.ContainsKey(n) ? gScore[n] : float.MaxValue).First();
            if (current == targetNode) break;

            openSet.Remove(current);

            if (!adjacencyList.ContainsKey(current)) continue;

            foreach (var lane in adjacencyList[current])
            {
                SeaNode neighbor = (lane.startNode == current) ? lane.endNode : lane.startNode;
                float dist = Vector3.Distance(current.transform.position, neighbor.transform.position); // Grobe Distanz
                float newG = gScore[current] + dist;

                if (!gScore.ContainsKey(neighbor) || newG < gScore[neighbor])
                {
                    gScore[neighbor] = newG;
                    cameFrom[neighbor] = current;
                    laneUsed[neighbor] = lane;
                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        if (!cameFrom.ContainsKey(targetNode)) return new Queue<Vector3>();

        // Pfad rückwärts rekonstruieren (Lanes sammeln)
        List<SeaLane> pathLanes = new List<SeaLane>();
        SeaNode trace = targetNode;
        while (trace != startNode)
        {
            SeaLane l = laneUsed[trace];
            pathLanes.Add(l);
            trace = (l.endNode == trace) ? l.startNode : l.endNode;
        }
        pathLanes.Reverse();

        // Punkte extrahieren (Hier holen wir die glatten Spline-Punkte!)
        Queue<Vector3> finalPoints = new Queue<Vector3>();
        SeaNode currNode = startNode;

        foreach (var lane in pathLanes)
        {
            bool reverse = (lane.endNode == currNode);
            // HIER RUFEN WIR DIE NEUE SPLINE-FUNKTION AUF:
            List<Vector3> smoothPoints = lane.GetSmoothPathPoints(reverse);

            // Punkte hinzufügen (Vermeide doppelte Punkte an den Nahtstellen)
            for (int i = 0; i < smoothPoints.Count; i++)
            {
                if (finalPoints.Count > 0 && i == 0) continue;
                finalPoints.Enqueue(smoothPoints[i]);
            }
            currNode = (lane.startNode == currNode) ? lane.endNode : lane.startNode;
        }

        finalPoints.Enqueue(endPos);
        return finalPoints;
    }
}