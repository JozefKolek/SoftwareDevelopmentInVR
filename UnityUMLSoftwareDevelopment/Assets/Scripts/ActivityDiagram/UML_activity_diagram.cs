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
    
    public Dictionary<string, List<string>> gruf = new Dictionary<string, List<string>>();
    public Dictionary<string, GameObject> actionNodes = new Dictionary<string, GameObject>();
    public Dictionary<string, Node> MsaglActionNodes = new Dictionary<string, Node>();
    public Dictionary<string, GameObject> actionEdges = new Dictionary<string, GameObject>();
    public Dictionary<string, Edge> MsaglActionEdges = new Dictionary<string, Edge>();
    
    public float factor = 0.2f;

    void Start()
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

    public void drawDiagram(string method)
    {
        canvasObj.SetActive(true);        
        gruf.Add("start", new List<string> { "Adam"});
        gruf.Add("Adam", new List<string> { "Zuza","Dona"});
        gruf.Add("Zuza", new List<string> {"end"});
        gruf.Add("Dona", new List<string> {"Micka"});
        gruf.Add("Micka", new List<string> {"Adam","Muska"});
        gruf.Add("Muska", new List<string> {"MikiMouse"});
        gruf.Add("MikiMouse", new List<string> {"end"});

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
        
        foreach (string name in gruf.Keys)
        {
            GameObject node = actionNode.DrawAction(name);
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
            Debug.Log(width + " " + height + " " + anchoredPosition.x + " " + anchoredPosition.y + " " + name);            
            //Node msaglNode = new Node(CurveFactory.CreateRectangle(width, height, initialCenter));
            //msaglNode.UserData = node;

            //if (msaglNode != null)
            //{
            //    graph.Nodes.Add(msaglNode);
            //}
            //MsaglActionNodes.Add(name, msaglNode);

        }
        // Adjust panel layout (spacing out panels)
        PositionPanels();

        foreach (KeyValuePair<string,List<string>> actionNode in gruf)
        {
            // Draw connections to related classes
            foreach (string target in actionNode.Value)
            {                
                if (actionNodes.ContainsKey(target) && actionNodes[target]!=null)
                {
                    DrawConnectionLine(actionNodes[actionNode.Key].transform, actionNodes[target].transform, actionNode.Key,target);
                    //Edge msaglHrana = new Edge(MsaglActionNodes[actionNode.Key],MsaglActionNodes[target]);
                    //MsaglActionEdges.Add(actionNode.Key + " " + target, msaglHrana);
                }
            }
        }

        foreach(var spojenia in actionEdges.Keys)
        {
            Debug.Log("spoje " + spojenia);
        }
        //LayoutHelpers.CalculateLayout(graph, settings, null);
        //float maxX = canvasObj.GetComponent<RectTransform>().rect.width / 2;
        //float maxY = canvasObj.GetComponent<RectTransform>().rect.height / 2;

        //foreach (KeyValuePair<string,Node> kvp in MsaglActionNodes)
        //{
        //    Node msaglNode = kvp.Value;

        //    Vector2 newPosition = new Vector2((float)msaglNode.Center.X * factor, (float)msaglNode.Center.Y * factor);

        //    RectTransform rectTransform = actionNodes[kvp.Key].GetComponent<RectTransform>();
        //    rectTransform.anchoredPosition = newPosition;
        //}
    }    

    internal void removeActionClass(string name)
    {
        List<string> to_remove = new List<string>();
        foreach(KeyValuePair<string,GameObject> hrana in actionEdges)
        {
            string[] spoj = hrana.Key.Split(" ");
            if (spoj[0].Equals(name) || spoj[1].Equals(name))
            {
                Destroy(hrana.Value);
                to_remove.Add(hrana.Key);
            }
        }
        foreach(string hrana in to_remove) { actionEdges.Remove(hrana); }
        if (gruf.ContainsKey(name)) { gruf.Remove(name); }
        if (actionNodes.ContainsKey(name)) { actionNodes.Remove(name); }
    }

    private void DrawConnectionLine(Transform startTransform, Transform endTransform, string from, string to)
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

        foreach (KeyValuePair<string,GameObject> node in actionNodes)
        {
            if (node.Value != null)
            {
                RectTransform rectTransform = node.Value.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(offsetX * (index % 5), offsetY * (index / 5));
                Debug.Log($"{node.Key} position: {rectTransform.anchoredPosition}");
                index++;
            }
        }
    }

    public void UpdateLines()
    {
        foreach (KeyValuePair<string,GameObject> hrana in actionEdges)
        {            
            var lineUpdater = hrana.Value.GetComponent<LineUpdater>();
            if (lineUpdater != null)
            {
                lineUpdater.UpdateLinePositions();
            }         
        }
    }

    public void redrawGraph()
    {
        foreach (var vrchol in actionNodes.Keys){Destroy(actionNodes[vrchol]);}
        foreach (var hrana in actionEdges.Keys) { Destroy(actionEdges[hrana]);}
        actionNodes.Clear();
        actionEdges.Clear();
        gruf.Clear();
    }

    private void CloseActivityDiagram()
    {
        canvasObj.SetActive(false);
        gruf.Clear();
        foreach(KeyValuePair<string,GameObject> vrchol in actionNodes) { Destroy(vrchol.Value); }
        foreach(KeyValuePair<string,GameObject> hrana in actionEdges) { Destroy(hrana.Value); }
        actionEdges.Clear();
        actionNodes.Clear();
        MsaglActionEdges.Clear();
        MsaglActionNodes.Clear();
        ClassCanvasObj.SetActive(true);        
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
        actionNodes.Add(claz_name, actionNode.DrawAction(claz_name));
        gruf.Add(claz_name, new List<string>());
        DraggableUI draggableUI = actionNodes[claz_name].GetComponent<DraggableUI>();
        if (draggableUI != null)
        {
            draggableUI.activity_Diagram = this; // Set the UML diagram reference
        }
    }

    internal void addEdge(string name)
    {
        addEdgePopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Add edge to action node";

        List<string> connections = new List<string> { "generalization", "aggergation" };
        List<string> classes = new List<string>();
        foreach(var vrchol in gruf.Keys) { classes.Add(vrchol); }
        foreach (var vrchol in gruf[name]) { classes.Remove(vrchol); }
        classes.Remove(name);                       

        TMP_Dropdown[] dropdowns = addEdgePopUp.GetComponentsInChildren<TMP_Dropdown>();
        dropdowns[0].ClearOptions();
        dropdowns[0].AddOptions(classes);
        dropdowns[1].ClearOptions();
        dropdowns[1].AddOptions(connections);

        addEdgePopUp.SetActive(true);
        addEdgePopUp.GetComponentInChildren<Button>().onClick.AddListener(() => AddEdgeToGraph(name));
    }

    private void AddEdgeToGraph(string name)
    {
        addEdgePopUp.SetActive(false);        
        if (actionNodes.ContainsKey(name))
        {
            TMP_Dropdown[] dropdowns = addEdgePopUp.GetComponentsInChildren<TMP_Dropdown>();
            string targetClass = dropdowns[0].options[dropdowns[0].value].text;
            string connection = dropdowns[1].options[dropdowns[1].value].text;
            if (actionNodes.ContainsKey(targetClass)){
                DrawConnectionLine(actionNodes[name].transform, actionNodes[targetClass].transform, name, targetClass);
            }
        }
    }

    internal void removeEdge(string name)
    {        
        if (gruf.ContainsKey(name))
        {
            List<string> target_classes = new List<string>();
            foreach (var target in gruf[name]) { target_classes.Add(target); }
            TMP_Dropdown targetClassesMenu = removeEdgePopUp.GetComponentInChildren<TMP_Dropdown>();
            targetClassesMenu.ClearOptions();
            targetClassesMenu.AddOptions(target_classes);
            removeEdgePopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Remove edge";
            removeEdgePopUp.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveEdgeFromGraph(name));
            removeEdgePopUp.SetActive(true);
        }
        removeEdgePopUp.SetActive(true);
    }

    private void RemoveEdgeFromGraph(string name)
    {
        removeEdgePopUp.SetActive(false);        
        if (gruf.ContainsKey(name))
        {
            TMP_Dropdown targetClassesMenu = removeEdgePopUp.GetComponentInChildren<TMP_Dropdown>();
            string target_class = targetClassesMenu.options[targetClassesMenu.value].text;
            if (gruf.ContainsKey(name))
            {                
                gruf[name].Remove(target_class);
            }
            if (actionEdges.ContainsKey(name + " " + target_class))
            {
                Destroy(actionEdges[name + " " + target_class]);
                actionEdges.Remove(name + " " + target_class);
            }
            Debug.Log("Edge from " + name + " to " + target_class + " removed");
        }
    }
}
