using System.Collections.Generic;
using UnityEngine;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Core.Routing;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Microsoft.Msagl.Core.Geometry;
using Radishmouse;

public class UML_class_diagram : MonoBehaviour
{

    Reading_graph read = new Reading_graph("Assets/SampleCode/Sample_code_C_pre_znaz_UML.cs");
    //Reading_graph read = new Reading_graph("Assets/SampleCode/RotatingSphere.cs");
    //Reading_graph read = new Reading_graph("C:/Users/Admin/Desktop/RotatingSphere.cs");
    public List<Class_object> classObjects;
    public ClassDrawer classDrawer;     // Reference to ClassDrawer script for drawing classes
    public Canvas canvasObj;            // Canvas to hold the panels
    public GameObject linePrefab;       // Line prefab with LineUpdater script attached
    public GameObject buttonPrefab;
    public GameObject addClassPrefab;
    public GameObject addEdgePrefab;
    public GameObject removeEdgePrefab;
    public GameObject activityCanvasObj;
    private GameObject addEdgePopUp;
    private GameObject removeEdgePopUp;

    public float factor = 0.2f;

    private GeometryGraph graph;
    private LayoutAlgorithmSettings settings;
    private Transform units;

    private void Start()
    {
        activityCanvasObj.SetActive(false);
        classObjects = read.read_from_code();
        redrawGraph();
    }

    public IEnumerator GenerateUMLDiagram()
    {
        GameObject AddClassPopUp = Instantiate(addClassPrefab, canvasObj.transform);
        addEdgePopUp = Instantiate(addEdgePrefab, canvasObj.transform);
        removeEdgePopUp = Instantiate(removeEdgePrefab, canvasObj.transform);

        AddClassPopUp.SetActive(false);
        addEdgePopUp.SetActive(false);
        removeEdgePopUp.SetActive(false);
        TMP_Dropdown[] dropdowns = addEdgePopUp.GetComponentsInChildren<TMP_Dropdown>();
        dropdowns[1].ClearOptions();
        dropdowns[1].AddOptions(new List<string> { "generalization", "agregation", "composition", "dependency", "realisation" });

        GameObject AddClassButton = Instantiate(buttonPrefab, canvasObj.transform);
        AddClassButton.GetComponent<Button>().onClick.AddListener(() => AddClass(AddClassPopUp));
        AddClassButton.GetComponent<Image>().color = Color.yellow;

        RectTransform addClassForm = AddClassButton.transform.GetComponent<RectTransform>();
        addClassForm.localPosition = new Vector3(-275, 210, 0);
        addClassForm.sizeDelta = new Vector2(150, 60);
        AddClassButton.GetComponentInChildren<TextMeshProUGUI>().text = "Add class";

        GameObject GenerateCodeButton = Instantiate(buttonPrefab, canvasObj.transform);
        GenerateCode generator = new GenerateCode(classObjects, canvasObj);
        GenerateCodeButton.GetComponent<Button>().onClick.AddListener(() => generator.generateCode());
        GenerateCodeButton.GetComponent<Image>().color = Color.yellow;

        RectTransform GenerateCodeForm = GenerateCodeButton.GetComponent<RectTransform>();
        GenerateCodeForm.localPosition = new Vector3(-275, 150, 0);
        addClassForm.sizeDelta = new Vector2(150, 60);
        GenerateCodeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Generate code";

        //prvotne vykreslenie vrcholov na platno kvoli ziskaniu paramtrov potrebnych pre Node MSAGL
        foreach (Class_object classObj in classObjects)
        {
            // Create and position each class panel using ClassDrawer
            GameObject classPanel = classDrawer.CreateClassPanel(classObj);
            classObj.UInode = classPanel; // Store the panel in the Class_object's UInode for reference         

            // Get the DraggableUI component and set the UML diagram reference
            DraggableUI draggableUI = classPanel.GetComponent<DraggableUI>();
            if (draggableUI != null)
            {
                draggableUI.umlDiagram = this; // Set the UML diagram reference
            }

            //msagl part
            RectTransform rectTransform = classPanel.GetComponent<RectTransform>();
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            // Set the initial center based on Unity's anchored position
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            var initialCenter = new Microsoft.Msagl.Core.Geometry.Point(anchoredPosition.x, anchoredPosition.y);

            Node msaglNode = new Node
            {
                UserData = classObj,
                BoundaryCurve = CurveFactory.CreateRectangle(width, height, initialCenter)
            };
            msaglNode.UserData = classObj.name;
            msaglNode.Center = initialCenter;
            graph.Nodes.Add(msaglNode);
            classObj.vrchol = msaglNode;
        }

        //pridanie hran do msagl
        foreach (Class_object classObj in classObjects)
        {
            // Msagl edges part
            foreach (var connection in classObj.connections)
            {
                Class_object targetClass = classObjects.Find(obj => obj.name == connection.Key);
                if (targetClass != null && targetClass.UInode != null)
                {
                    Edge msaglHrana = new Edge(classObj.vrchol, targetClass.vrchol);
                    classObj.hrany.Add(targetClass.name, msaglHrana);
                    graph.Edges.Add(msaglHrana);
                }
            }
        }

        LayoutHelpers.CalculateLayout(graph, settings, null);
                
        //redrawGraph node positions based on msagl results
        foreach (var kvp in classObjects)
        {
            Node msaglNode = kvp.vrchol;

            // Calculate the new position based on MSAGL layout
            Vector2 newPosition = new Vector2((float)msaglNode.Center.X * factor - 425, (float)msaglNode.Center.Y * factor - 540);

            // Apply the clamped position to the UI node
            RectTransform rectTransform = kvp.UInode.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = newPosition;
        }

        //DrawConnectionLine edges based on msagl results
        foreach (Class_object classObj in classObjects)
        {
            // Msagl edges part
            foreach (var connection in classObj.hrany)
            {
                Class_object targetClass = classObjects.Find(obj => obj.name == connection.Key);
                if (targetClass != null && targetClass.UInode != null)
                {
                    DrawConnectionLine(classObj.UInode.transform, targetClass.UInode.transform, classObj, targetClass);
                }
            }
        }

        yield return null;
        yield return null;
        yield return null;
    }

    private void DrawConnectionLine(Transform startTransform, Transform endTransform, Class_object startClass, Class_object endClass)
    {
        // Instantiate the line prefab
        GameObject lineInstance = Instantiate(linePrefab, canvasObj.transform);
        lineInstance.GetComponent<Transform>().position = new Vector3(0,0,0);
        lineInstance.name = "Edge_" + startClass.name + "_" + endClass.name;
        startClass.UIedges.Add(endClass.name, lineInstance);

        UILineRenderer lineRenderer = lineInstance.GetComponent<UILineRenderer>();

        Edge msaglEdge = startClass.hrany[endClass.name];
        List<Vector3> points = new List<Vector3>();
        Curve curve = msaglEdge.Curve as Curve;

        if (curve != null)
        {
            // Add the start point
            Point p = curve[curve.ParStart];
            points.Add(new Vector3((float)p.X * factor - 425, (float)p.Y * factor - 540, 0));

            // Add the intermediate points
            foreach (ICurve seg in curve.Segments)
            {
                p = seg[seg.ParEnd];
                points.Add(new Vector3((float)p.X * factor - 425, (float)p.Y * factor - 540, 0));
            }
        }
        else
        {
            // Handle the case where the curve is a line segment
            LineSegment ls = msaglEdge.Curve as LineSegment;
            if (ls != null)
            {
                Point p = ls.Start;
                points.Add(new Vector3((float)p.X * factor - 425, (float)p.Y * factor - 540, 0));
                p = ls.End;
                points.Add(new Vector3((float)p.X * factor - 425, (float)p.Y * factor - 540, 0));
            }
        }
        lineRenderer.points = points.ToArray();
    }

    public void redrawGraph()
    {
        //intialize msagl graph part
        // Initialize geometry graph and layout settings
        graph = new GeometryGraph();
        settings = new SugiyamaLayoutSettings
        {
            LayerSeparation = 50,
            NodeSeparation = 50,
            EdgeRoutingSettings = new EdgeRoutingSettings
            {
                EdgeRoutingMode = EdgeRoutingMode.RectilinearToCenter, // Change to Rectilinear for orthogonal edges
                PolylinePadding = 5, // Add padding to ensure space around edges
                CornerRadius = 10 // Add corner radius for smoother bends
            }
        };

        
        //remove all nodes and edges from canvas 
        foreach (var claz in classObjects)
        {
            Destroy(claz.UInode);
            claz.UInode = null;
            foreach (KeyValuePair<string, GameObject> edge in claz.UIedges) { Destroy(edge.Value); }
            claz.UIedges.Clear();
            claz.hrany.Clear();
            claz.vrchol = null;
        }

        StartCoroutine(GenerateUMLDiagram());
    }

    private void AddClass(GameObject addClassPopUp)
    {
        addClassPopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Add class to graph";
        addClassPopUp.SetActive(true);
        addClassPopUp.GetComponentInChildren<Button>().onClick.AddListener(() => AddClassToGraph(addClassPopUp));
    }

    private void AddClassToGraph(GameObject addClassPopUp)
    {
        addClassPopUp.SetActive(false);
        string claz_name = addClassPopUp.GetComponentInChildren<TMP_InputField>().text;
        Class_object classObj = new Class_object(claz_name);
        //docasne
        classObj.visibility = "public";
        classObjects.Add(classObj);
        redrawGraph();
    }

}
