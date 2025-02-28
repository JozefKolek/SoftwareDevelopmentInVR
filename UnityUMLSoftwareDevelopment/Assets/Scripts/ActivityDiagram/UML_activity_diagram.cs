using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using Radishmouse;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UML_activity_diagram : MonoBehaviour
{
    public GameObject canvasObj;
    public GameObject ClassCanvasObj;
    public GameObject linePrefab;
    public GameObject buttonPrefab;
    public Action_node_drawer actionNode;
    public GameObject addClassPrefab;
    public GameObject addEdgePrefab;
    public GameObject removeEdgePrefab;
    private GameObject addClassPopUP;
    private GameObject removeEdgePopUp;
    private GameObject addEdgePopUp;

    private GeometryGraph graph;
    private LayoutAlgorithmSettings settings;
    private Transform units;

    public Dictionary<int, Dictionary<int, string>> gruf = new Dictionary<int, Dictionary<int, string>>();
    public Dictionary<int, GameObject> actionNodes = new Dictionary<int, GameObject>();
    public Dictionary<int, Node> MsaglActionNodes = new Dictionary<int, Node>();
    public Dictionary<string,  GameObject> actionEdges = new Dictionary<string, GameObject>();
    public Dictionary<string, Edge> MsaglActionEdges = new Dictionary<string, Edge>();
    string method = "";
    Class_object clasa = new Class_object("");
    public UML_class_diagram class_Diagram;

    public float factor = 0.2f;

    public void initialise(string method, Class_object classObj)
    {
        this.clasa = classObj;
        this.method = method;
    }

    public IEnumerator drawDiagram()
    {
        gruf = new Dictionary<int, Dictionary<int, string>>();
        foreach (var comand in clasa.commandEdges) { Debug.Log(comand.Key); }
        foreach (var comand in clasa.commandEdges[method])
        {
            gruf.Add(comand.Key, new Dictionary<int, string>());
            foreach (KeyValuePair<int,string> to in comand.Value)
            {
                gruf[comand.Key].Add(to.Key, to.Value);
            }
        }

        addClassPopUP = Instantiate(addClassPrefab, canvasObj.transform);
        addClassPopUP.SetActive(false);
        addEdgePopUp = Instantiate(addEdgePrefab, canvasObj.transform);
        addEdgePopUp.SetActive(false);

        removeEdgePopUp = Instantiate(removeEdgePrefab, canvasObj.transform);
        removeEdgePopUp.SetActive(false);

        GameObject AddClassButton = Instantiate(buttonPrefab, canvasObj.transform);
        AddClassButton.GetComponent<Button>().onClick.AddListener(() => AddClass());
        AddClassButton.GetComponent<Image>().color = Color.yellow;

        RectTransform addClassForm = AddClassButton.transform.GetComponent<RectTransform>();
        addClassForm.localPosition = new Vector3(-275, 210, 0);
        addClassForm.sizeDelta = new Vector2(150, 60);
        AddClassButton.GetComponentInChildren<TextMeshProUGUI>().text = "Add class";

        GameObject CloseButton = Instantiate(buttonPrefab, canvasObj.transform);
        CloseButton.GetComponent<Button>().onClick.AddListener(() => CloseActivityDiagram());
        CloseButton.GetComponent<Image>().color = Color.yellow;

        RectTransform closeClassForm = CloseButton.transform.GetComponent<RectTransform>();
        closeClassForm.localPosition = new Vector3(-275, 140, 0);
        closeClassForm.sizeDelta = new Vector2(150, 60);
        CloseButton.GetComponentInChildren<TextMeshProUGUI>().text = "Close activity";

        foreach (int name in gruf.Keys)
        {
            GameObject node = actionNode.DrawAction(clasa.commandKeys[method][name], name);
            actionNodes.Add(name, node);

            DraggableUI draggableUI = node.GetComponent<DraggableUI>();
            if (draggableUI != null)
            {
                draggableUI.activity_Diagram = this; // Set the UML diagram reference
            }

            //msagl part
            RectTransform rectTransform = node.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            
            // Set the initial center based on Unity's anchored position
            Node msaglNode = new Node
            {
                BoundaryCurve = CurveFactory.CreateRectangle(width / factor, height / factor, new Point()),
                UserData = node
            };
            msaglNode.UserData = node.name;

            MsaglActionNodes.Add(name, msaglNode);
            graph.Nodes.Add(msaglNode);
        }

        //add edges to msagl part
        foreach (var from in gruf)
        {
            foreach (var to in from.Value)
            {                
                Edge msaglHrana = new Edge(MsaglActionNodes[from.Key], MsaglActionNodes[to.Key]);
                MsaglActionEdges.Add(from.Key + " " + to.Key, msaglHrana);
                graph.Edges.Add(msaglHrana);
            }
        }

        LayoutHelpers.CalculateLayout(graph, settings, null);

        foreach (KeyValuePair<int, Node> kvp in MsaglActionNodes)
        {            
            Node msaglNode = kvp.Value;
            Vector2 newPosition = new Vector2((float)msaglNode.Center.X * factor - 425, (float)msaglNode.Center.Y * factor - 540);
            RectTransform rectTransform = actionNodes[kvp.Key].GetComponent<RectTransform>();
            rectTransform.anchoredPosition = newPosition;
        }

        foreach(KeyValuePair<string,Edge> edge in MsaglActionEdges)
        {
            DrawConnectionLine(edge.Key);

        }

        yield return null;
        yield return null;
        yield return null;
    }

    private void DrawConnectionLine(string edgeFromTo)
    {
        // Instantiate the line prefab
        GameObject lineInstance = Instantiate(linePrefab, canvasObj.transform);
        lineInstance.GetComponent<Transform>().position = new Vector3(0, 0, 0);
        lineInstance.name = "Edge_" + edgeFromTo;
        
        // Optionally, store the line in the startClass's UIedges dictionary
        actionEdges.Add(edgeFromTo,lineInstance);

        UILineRenderer lineRenderer = lineInstance.GetComponent<UILineRenderer>();

        Edge msaglEdge = MsaglActionEdges[edgeFromTo];
        List<Vector3> points = new List<Vector3>();
        Curve curve = msaglEdge.Curve as Curve;

        if (curve != null)
        {
            // Add the start point
            Point p = curve[curve.ParStart];
            points.Add(new Vector3((float)p.X * factor - 377, (float)p.Y * factor - 490, 0));

            // Add the intermediate points
            foreach (ICurve seg in curve.Segments)
            {
                p = seg[seg.ParEnd];
                points.Add(new Vector3((float)p.X * factor - 377, (float)p.Y * factor - 490, 0));
            }
        }
        else
        {
            // Handle the case where the curve is a line segment
            LineSegment ls = msaglEdge.Curve as LineSegment;
            if (ls != null)
            {
                Point p = ls.Start;
                points.Add(new Vector3((float)p.X * factor - 377, (float)p.Y * factor - 490, 0));
                p = ls.End;
                points.Add(new Vector3((float)p.X * factor - 377, (float)p.Y * factor - 490, 0));
            }
        }
        foreach (var i in points) { Debug.Log("Suradnice pre hranu z " + edgeFromTo + " ma parametre: " + i.x + " " + i.y); }
        lineRenderer.points = points.ToArray();
    }

    public void RedrawDiagram()
    {
        canvasObj.SetActive(true);

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

        //Destroy activity nodes and edges on canvas
        foreach (KeyValuePair<int, GameObject> vrchol in actionNodes) { Destroy(vrchol.Value); }
        foreach (KeyValuePair<string, GameObject> hrana in actionEdges) { Destroy(hrana.Value); }
        actionEdges.Clear();
        actionNodes.Clear();
        MsaglActionEdges.Clear();
        MsaglActionNodes.Clear();
        StartCoroutine(drawDiagram());
    }

    private void CloseActivityDiagram()
    {
        canvasObj.SetActive(false);
        gruf.Clear();
        foreach (KeyValuePair<int, GameObject> vrchol in actionNodes) { Destroy(vrchol.Value); }
        foreach (KeyValuePair<string, GameObject> hrana in actionEdges) { Destroy(hrana.Value); }
        actionEdges.Clear();
        actionNodes.Clear();
        MsaglActionEdges.Clear();
        MsaglActionNodes.Clear();
        ClassCanvasObj.SetActive(true);
    }

    //Add Node
    private void AddClass()
    {
        addClassPopUP.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Add Action to Activity";
        addClassPopUP.SetActive(true);
        addClassPopUP.GetComponentInChildren<Button>().onClick.AddListener(() => AddClassToGraph());
    }

    private void AddClassToGraph()
    {
        addClassPopUP.SetActive(false);
        string claz_name = addClassPopUP.GetComponentInChildren<TMP_InputField>().text;
        int index = class_Diagram.AddActivityNode(claz_name, method, clasa.name);
        RedrawDiagram();
    }

    //Add Edge
    internal void addEdge(int key)
    {
        addEdgePopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Add edge to action node";
        List<string> connections = new List<string> { "generalization", "aggergation" };
        List<string> classes = new List<string>();
        foreach (var vrchol in clasa.commandKeys[method])
        {
            if (key != vrchol.Key && !gruf[key].ContainsKey(vrchol.Key))
            {
                classes.Add(vrchol.Key + " " + vrchol.Value);
            }
        }
        foreach (var vrchol in gruf)
        {
            if (gruf[vrchol.Key].ContainsKey(key))
            {
                classes.Remove(vrchol.Key + " " + clasa.commandKeys[method][vrchol.Key]);
            }
        }

        TMP_Dropdown[] dropdowns = addEdgePopUp.GetComponentsInChildren<TMP_Dropdown>();
        dropdowns[0].ClearOptions();
        dropdowns[0].AddOptions(classes);
        dropdowns[1].ClearOptions();
        dropdowns[1].AddOptions(connections);

        addEdgePopUp.SetActive(true);
        addEdgePopUp.GetComponentInChildren<Button>().onClick.AddListener(() => AddEdgeToGraph(key));

    }

    private void AddEdgeToGraph(int key)
    {
        addEdgePopUp.SetActive(false);
        if (actionNodes.ContainsKey(key))
        {
            TMP_Dropdown[] dropdowns = addEdgePopUp.GetComponentsInChildren<TMP_Dropdown>();
            string targetClass = dropdowns[0].options[dropdowns[0].value].text;
            string connection = dropdowns[1].options[dropdowns[1].value].text;
            int targetIndex = Int32.Parse(targetClass.Split(" ")[0]);
            if (actionNodes.ContainsKey(targetIndex))
            {
                class_Diagram.AddActionEdge(key, targetIndex, method, connection, clasa.name);
                RedrawDiagram();
            }
        }
    }

    //remove Node
    internal void removeActionClass(int key)
    {
        List<string> to_remove = new List<string>();
        foreach (KeyValuePair<string, GameObject> hrana in actionEdges)
        {
            string[] spoj = hrana.Key.Split(" ");
            if (Int32.Parse(spoj[0]) == key || Int32.Parse(spoj[1]) == key)
            {
                Destroy(hrana.Value);
                to_remove.Add(hrana.Key);
            }
        }
        foreach (string hrana in to_remove) { actionEdges.Remove(hrana); }
        if (gruf.ContainsKey(key)) { gruf.Remove(key); }
        if (actionNodes.ContainsKey(key)) { actionNodes.Remove(key); }
        class_Diagram.removeActionClass(method, key, clasa.name);
        RedrawDiagram();
    }
    //remove Edge
    internal void removeEdge(int key)
    {
        if (gruf.ContainsKey(key))
        {
            List<string> target_classes = new List<string>();
            foreach (var target in gruf[key]) { target_classes.Add(target.Key + " " + clasa.commandKeys[method][target.Key]); }
            TMP_Dropdown targetClassesMenu = removeEdgePopUp.GetComponentInChildren<TMP_Dropdown>();
            targetClassesMenu.ClearOptions();
            targetClassesMenu.AddOptions(target_classes);
            removeEdgePopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Remove edge";
            removeEdgePopUp.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveEdgeFromGraph(key));
            removeEdgePopUp.SetActive(true);
        }
        removeEdgePopUp.SetActive(true);
    }

    private void RemoveEdgeFromGraph(int key)
    {
        removeEdgePopUp.SetActive(false);
        if (gruf.ContainsKey(key))
        {
            TMP_Dropdown targetClassesMenu = removeEdgePopUp.GetComponentInChildren<TMP_Dropdown>();
            string target_class = targetClassesMenu.options[targetClassesMenu.value].text;
            int targetIndex = Int32.Parse(target_class.Split(" ")[0]);
            if (gruf.ContainsKey(key))
            {
                gruf[key].Remove(targetIndex);
            }
            if (actionEdges.ContainsKey(key + " " + targetIndex))
            {
                Destroy(actionEdges[key + " " + targetIndex]);
                actionEdges.Remove(key + " " + targetIndex);
                class_Diagram.removeActionEdge(key, targetIndex, method, clasa.name);
                RedrawDiagram();
            }
        }
    }

    //after drag and drop reroute edges and nodes in msagl
    public void rerouteGraph()
    {
        foreach (KeyValuePair<int,GameObject> node in actionNodes)
        {
            Vector2 anchoredPosition = node.Value.GetComponent<RectTransform>().anchoredPosition;
            Node msaglNode = MsaglActionNodes[node.Key];
            msaglNode.Center = new Point((anchoredPosition.x + 425) / factor, (anchoredPosition.y + 540) / factor);
        }
        LayoutHelpers.RouteAndLabelEdges(graph, settings, graph.Edges, 0, new CancelToken());
        
        foreach (KeyValuePair<string, Edge> hrana in MsaglActionEdges)
        {
            GameObject line = actionEdges[hrana.Key];

            UILineRenderer lineRenderer = line.GetComponent<UILineRenderer>();

            Edge msaglEdge = hrana.Value;
            List<Vector3> points = new List<Vector3>();
            Curve curve = msaglEdge.Curve as Curve;

            if (curve != null)
            {
                // Add the start point
                Point p = curve[curve.ParStart];
                points.Add(new Vector3((float)p.X * factor - 377, (float)p.Y * factor - 490, 0));

                // Add the intermediate points
                foreach (ICurve seg in curve.Segments)
                {
                    p = seg[seg.ParEnd];
                    points.Add(new Vector3((float)p.X * factor - 377, (float)p.Y * factor - 490, 0));
                }
            }
            else
            {
                // Handle the case where the curve is a line segment
                LineSegment ls = msaglEdge.Curve as LineSegment;
                if (ls != null)
                {
                    Point p = ls.Start;
                    points.Add(new Vector3((float)p.X * factor - 377, (float)p.Y * factor - 490, 0));
                    p = ls.End;
                    points.Add(new Vector3((float)p.X * factor - 377, (float)p.Y * factor - 490, 0));
                }
            }
            foreach (var i in points) { Debug.Log("Suradnice pre hranu z " + hrana.Key + " ma parametre: " + i.x + " " + i.y); }
            lineRenderer.points = points.ToArray();
            lineRenderer.SetVerticesDirty();
            lineRenderer.Rebuild(CanvasUpdate.PreRender);
        }        
    }
}
