using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
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

    public Dictionary<int, Dictionary<int,string>> gruf = new Dictionary<int, Dictionary<int, string>>();
    public Dictionary<int, GameObject> actionNodes = new Dictionary<int, GameObject>();
    public Dictionary<int, Node> MsaglActionNodes = new Dictionary<int, Node>();
    public Dictionary<string,  GameObject> actionEdges = new Dictionary<string, GameObject>();
    public Dictionary<string, Edge> MsaglActionEdges = new Dictionary<string, Edge>();
    string method = "";
    Class_object clasa = new Class_object("");
    public UML_class_diagram class_Diagram;

    public float factor = 0.2f;

    public void initialise()
    {
        // Initialize geometry graph and layout settings
        graph = new GeometryGraph();
        settings = new SugiyamaLayoutSettings
        {
            LayerSeparation = 30,
            NodeSeparation = 20
        };
        settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.Spline;

        // Initialize the Unity transform for the units (node container)
        units = transform.Find("Units");
        if (units == null)
        {
            units = new GameObject("Units").transform;
            units.SetParent(transform);
        }
        canvasObj.SetActive(true);
    }

    public void drawDiagram(string method, Class_object clasa)
    {
        this.clasa = clasa;
        this.method = method;
        canvasObj.SetActive(true);
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
            GameObject node = actionNode.DrawAction(clasa.commandKeys[method][name],name);
            actionNodes.Add(name, node);

            DraggableUI draggableUI = node.GetComponent<DraggableUI>();
            if (draggableUI != null)
            {
                draggableUI.activity_Diagram = this; // Set the UML diagram reference
            }

            //msagl part
            RectTransform rectTransform = node.GetComponent<RectTransform>();
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            // Set the initial center based on Unity's anchored position
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            var initialCenter = new Microsoft.Msagl.Core.Geometry.Point(anchoredPosition.x, anchoredPosition.y);
            Node msaglNode = new Node(CurveFactory.CreateRectangle(width, height, initialCenter));
            msaglNode.UserData = node;

            if (msaglNode != null)
            {
                graph.Nodes.Add(msaglNode);
            }
            MsaglActionNodes.Add(name, msaglNode);
            graph.Nodes.Add(msaglNode);

        }
        // Adjust panel layout (spacing out panels)
        PositionPanels();

        foreach(var from in gruf)
        {
            foreach (var to in from.Value)
            {                
                DrawConnectionLine(actionNodes[from.Key].transform, actionNodes[to.Key].transform, from.Key, to.Key);
                Edge msaglHrana = new Edge(MsaglActionNodes[from.Key], MsaglActionNodes[to.Key]);
                MsaglActionEdges.Add(from.Key + " " + to.Key, msaglHrana);
                graph.Edges.Add(msaglHrana);
            }
        }

        LayoutHelpers.CalculateLayout(graph, settings, null);
        float canvasWidth = canvasObj.GetComponent<RectTransform>().rect.width / 2;
        float canvasHeight = canvasObj.GetComponent<RectTransform>().rect.height / 2;

        foreach (KeyValuePair<int, Node> kvp in MsaglActionNodes)
        {
            Node msaglNode = kvp.Value;

            Vector2 newPosition = new Vector2((float)msaglNode.Center.X * factor, (float)msaglNode.Center.Y * factor);

            // Clamp the position to keep within canvas bounds
            newPosition.x = Mathf.Clamp(newPosition.x, -canvasWidth + 150, canvasWidth - 150);
            newPosition.y = Mathf.Clamp(newPosition.y, -canvasHeight + 150, canvasHeight - 150);

            RectTransform rectTransform = actionNodes[kvp.Key].GetComponent<RectTransform>();
            rectTransform.anchoredPosition = newPosition;
        }
    }

    private void DrawConnectionLine(Transform startTransform, Transform endTransform, int from, int to)
    {
        // Instantiate the line prefab
        GameObject lineInstance = Instantiate(linePrefab, canvasObj.transform);

        // Initialize the line between start and end transforms
        LineUpdater lineUpdater = lineInstance.GetComponent<LineUpdater>();
        lineUpdater.Initialize(actionNodes[from], actionNodes[to]);

        // Optionally, store the line in the startClass's UIedges dictionary
        actionEdges[from + " " + to] = lineInstance;

        Vector3 start = lineUpdater.GetClosestEdgePosition(startTransform, endTransform.position);
        Vector3 end = lineUpdater.GetClosestEdgePosition(endTransform, endTransform.position);
        lineUpdater.SetLinePositions(start, end);
    }

    private void PositionPanels()
    {
        float offsetX = 250f;  // Adjust spacing between panels horizontally
        float offsetY = -150f; // Adjust spacing between panels vertically
        int index = 0;

        foreach (KeyValuePair<int, GameObject> node in actionNodes)
        {
            if (node.Value != null)
            {
                RectTransform rectTransform = node.Value.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(offsetX * (index % 5), offsetY * (index / 5));
                index++;
            }
        }
    }

    public void UpdateLines()
    {
        foreach (KeyValuePair<string, GameObject> hrana in actionEdges)
        {
            var lineUpdater = hrana.Value.GetComponent<LineUpdater>();
            if (lineUpdater != null)
            {
                lineUpdater.UpdateLinePositions();
            }
        }
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

    private void RedrawDiagram()
    {
        gruf.Clear();
        foreach (KeyValuePair<int, GameObject> vrchol in actionNodes) { Destroy(vrchol.Value); }
        foreach (KeyValuePair<string, GameObject> hrana in actionEdges) { Destroy(hrana.Value); }
        actionEdges.Clear();
        actionNodes.Clear();
        MsaglActionEdges.Clear();
        MsaglActionNodes.Clear();
        drawDiagram(method, clasa);
    }

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
        int index= class_Diagram.AddActivityNode(claz_name,method,clasa.name);
        RedrawDiagram();        
    }

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
                class_Diagram.AddActionEdge(key,targetIndex,method,connection,clasa.name);
                RedrawDiagram();
            }
        }
    }
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
        //RedrawDiagram();
    }

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
                class_Diagram.removeActionEdge(key, targetIndex, method,clasa.name);
            }
        }
    }
}
