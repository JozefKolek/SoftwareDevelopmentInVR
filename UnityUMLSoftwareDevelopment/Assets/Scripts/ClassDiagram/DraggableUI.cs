using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    // Reference to UML_class_diagram to notify position change
    public UML_class_diagram umlDiagram;
    public UML_activity_diagram activity_Diagram;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>(); // Finds the Canvas this element is part of

        // Check if a CanvasGroup component exists, and add one if it doesn’t
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;  // Make it semi-transparent during drag
        canvasGroup.blocksRaycasts = false;  // Allow raycast to pass through to other UI elements
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move the UI element based on the drag position
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // Notify the UML diagram to update the lines
        if (umlDiagram != null)
        {
            //update nodes positions of class diagram and reroute edges
            umlDiagram.rerouteGraph();
        }
        if (activity_Diagram != null)
        {
            activity_Diagram.rerouteGraph();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;  // Restore full opacity
        canvasGroup.blocksRaycasts = true;  // Block raycast again        
    }
}
