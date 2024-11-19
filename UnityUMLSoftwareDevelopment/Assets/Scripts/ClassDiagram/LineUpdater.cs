using UnityEngine;

public class LineUpdater : MonoBehaviour
{
    private GameObject startNode;
    private GameObject endNode;
    private LineRenderer lineRenderer;

    // Method to initialize the line with its start and end nodes.
    public void Initialize(GameObject startNode, GameObject endNode)
    {
        this.startNode = startNode;
        this.endNode = endNode;
        this.lineRenderer = GetComponent<LineRenderer>();
    }

    public void UpdateLinePositions()
    {
        if (startNode != null && endNode != null && lineRenderer != null)
        {
            // Use GetClosestEdgePosition from UML_class_diagram to dynamically adjust positions
            Vector3 start = GetClosestEdgePosition(startNode.transform, endNode.transform.position);
            Vector3 end = GetClosestEdgePosition(endNode.transform, startNode.transform.position);

            SetLinePositions(start, end);
        }
    }

    public void SetLinePositions(Vector3 start, Vector3 end)
    {
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }
    }

    public Vector3 GetClosestEdgePosition(Transform rectTransform, Vector3 targetPosition)
{
        RectTransform rect = rectTransform.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners); // 0: bottom-left, 1: top-left, 2: top-right, 3: bottom-right

        // Calculate center of each edge
        Vector3 top = (corners[1] + corners[2]) / 2;
        Vector3 bottom = (corners[0] + corners[3]) / 2;
        Vector3 left = (corners[0] + corners[1]) / 2;
        Vector3 right = (corners[2] + corners[3]) / 2;

        // Find the edge closest to the target position
        Vector3 closestEdge = top;
        float minDistance = Vector3.Distance(top, targetPosition);

        if (Vector3.Distance(bottom, targetPosition) < minDistance)
        {
            closestEdge = bottom;
            minDistance = Vector3.Distance(bottom, targetPosition);
        }
        if (Vector3.Distance(left, targetPosition) < minDistance)
        {
            closestEdge = left;
            minDistance = Vector3.Distance(left, targetPosition);
        }
        if (Vector3.Distance(right, targetPosition) < minDistance)
        {
            closestEdge = right;
            minDistance = Vector3.Distance(right, targetPosition);
        }

        return closestEdge;
    }
}
