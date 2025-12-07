using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SeaGrid : MonoBehaviour
{
    public static SeaGrid Instance;

    // Alle Knoten im Spiel
    private List<SeaNode> allNodes = new List<SeaNode>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Sammelt alle Knoten ein
        allNodes.AddRange(FindObjectsOfType<SeaNode>());
    }

    // Sucht den nächstgelegenen Knoten zu einer Position (z.B. einer Stadt)
    public SeaNode GetClosestNode(Vector3 position)
    {
        SeaNode bestNode = null;
        float minDst = float.MaxValue;

        foreach (var node in allNodes)
        {
            float dst = Vector3.Distance(node.transform.position, position);
            if (dst < minDst)
            {
                minDst = dst;
                bestNode = node;
            }
        }
        return bestNode;
    }

    // A* Pathfinding: Findet die Liste der Straßen (Lanes) von Start nach Ziel
    public List<SeaLane> FindPath(SeaNode startNode, SeaNode targetNode)
    {
        if (startNode == targetNode) return new List<SeaLane>();

        // Setup für A*
        Dictionary<SeaNode, SeaNode> cameFrom = new Dictionary<SeaNode, SeaNode>();
        Dictionary<SeaNode, SeaLane> laneUsed = new Dictionary<SeaNode, SeaLane>();

        Dictionary<SeaNode, float> gScore = new Dictionary<SeaNode, float>();
        foreach (var n in allNodes) gScore[n] = float.MaxValue;
        gScore[startNode] = 0;

        List<SeaNode> openSet = new List<SeaNode> { startNode };

        while (openSet.Count > 0)
        {
            // Knoten mit geringstem Score finden
            SeaNode current = openSet.OrderBy(n => gScore[n] + Vector3.Distance(n.transform.position, targetNode.transform.position)).First();

            if (current == targetNode)
            {
                return ReconstructPath(laneUsed, current);
            }

            openSet.Remove(current);

            // Nachbarn prüfen
            foreach (var lane in current.outgoingLanes)
            {
                SeaNode neighbor = (lane.endNode == current) ? lane.startNode : lane.endNode; // Funktioniert in beide Richtungen? 
                                                                                              // ACHTUNG: Wir machen Straßen bidirektional (beidseitig befahrbar)
                                                                                              // Falls lane.startNode == current -> neighbor ist endNode

                float dist = Vector3.Distance(lane.startNode.transform.position, lane.endNode.transform.position); // Länge der Straße
                float tentativeG = gScore[current] + dist;

                if (tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    laneUsed[neighbor] = lane;
                    gScore[neighbor] = tentativeG;

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        Debug.LogError("Kein Weg gefunden!");
        return null;
    }

    List<SeaLane> ReconstructPath(Dictionary<SeaNode, SeaLane> laneUsed, SeaNode current)
    {
        List<SeaLane> totalPath = new List<SeaLane>();
        while (laneUsed.ContainsKey(current))
        {
            totalPath.Add(laneUsed[current]);
            // Gehe rückwärts: Wer war der Knoten VOR current?
            // Wir müssen laneUsed nutzen um den Knoten zu finden
            SeaLane lane = laneUsed[current];
            current = (lane.endNode == current) ? lane.startNode : lane.endNode;
        }
        totalPath.Reverse();
        return totalPath;
    }
}