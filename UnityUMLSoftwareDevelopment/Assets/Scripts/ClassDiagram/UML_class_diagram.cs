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
using Microsoft.Msagl.Core;

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
    public GameObject compileCanvasObj;
    public GenerateCode generateCode;
    private GameObject addEdgePopUp;
    private GameObject removeEdgePopUp;
    private GameObject arrowHead;

    public float factor = 0.2f;

    private GeometryGraph graph;
    private LayoutAlgorithmSettings settings;
    private Transform units;

    private void Start()
    {
        activityCanvasObj.SetActive(false);
        compileCanvasObj.SetActive(false);
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
        addClassForm.localPosition = new Vector3(-600, 400, 0);
        addClassForm.sizeDelta = new Vector2(150, 60);
        AddClassButton.GetComponentInChildren<TextMeshProUGUI>().text = "Add class";

        GameObject GenerateCodeButton = Instantiate(buttonPrefab, canvasObj.transform);
        generateCode.initialise(classObjects, canvasObj);
        GenerateCodeButton.GetComponent<Button>().onClick.AddListener(() => generateCode.generateCode());
        GenerateCodeButton.GetComponent<Image>().color = Color.yellow;

        RectTransform GenerateCodeForm = GenerateCodeButton.GetComponent<RectTransform>();
        GenerateCodeForm.localPosition = new Vector3(-600, 340, 0);
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
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;            
            // Set the initial center based on Unity's anchored position
            
            Node msaglNode = new Node
            {
                UserData = classObj,
                BoundaryCurve = CurveFactory.CreateRectangle(width/factor, height/factor, new Point())
            };
            msaglNode.UserData = classObj.name;
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
        lineRenderer.points = points.ToArray();

        // Vytvorenie trojuholníkovej šípky
        if(startClass.connections.ContainsKey(endClass.name))
        {
            if (startClass.connections[endClass.name].Equals("Generalisation"))
            {
                lineRenderer.color = Color.black;
            } else if (startClass.connections[endClass.name].Equals("Realisation"))
            {
                lineRenderer.color = Color.red;
            } else if (startClass.connections[endClass.name].Equals("Composition"))
            {
                lineRenderer.color = Color.green;
            } else if (startClass.connections[endClass.name].Equals("Aggregation"))
            {
                lineRenderer.color = Color.blue;
            }
            else if (startClass.connections[endClass.name].Equals("Dependency"))
            {
                lineRenderer.color = Color.yellow;
            }            
        }        
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

    //add class to class diagram
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

    //add Edge to graph
    internal void AddEdge(string name)
    {
        addEdgePopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Add edge to class";
        List<string> classes = new List<string>();
        foreach (var claz in classObjects) { if (!claz.name.Equals(name)) { classes.Add(claz.name); } }
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            foreach (var targetClass in classObj.connections.Keys) { classes.Remove(targetClass); }
        }
        TMP_Dropdown[] dropdowns = addEdgePopUp.GetComponentsInChildren<TMP_Dropdown>();
        dropdowns[0].ClearOptions();
        dropdowns[0].AddOptions(classes);

        addEdgePopUp.SetActive(true);
        addEdgePopUp.GetComponentInChildren<Button>().onClick.AddListener(() => AddEdgeToGraph(name));
    }

    private void AddEdgeToGraph(string name)
    {
        addEdgePopUp.SetActive(false);
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            TMP_Dropdown[] dropdowns = addEdgePopUp.GetComponentsInChildren<TMP_Dropdown>();
            string targetClass = dropdowns[0].options[dropdowns[0].value].text;
            string connection = dropdowns[1].options[dropdowns[1].value].text;
            if (!classObj.connections.ContainsKey(targetClass)) { classObj.connections.Add(targetClass, connection); }
            redrawGraph();
        }
    }

    //add method to graph
    internal void AddMethod(string method, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            classObj.methods.Add(method);
            classObj.methodCommands.Add(method, new List<string>());
            classObj.commandKeys.Add(method, new Dictionary<int, string>());
            classObj.commandEdges.Add(method, new Dictionary<int, Dictionary<int, string>>());
            classObj.closeIfElse.Add(method, new Dictionary<int, int>());

            classObj.commandKeys[method].Add(1, "start");
            classObj.commandKeys[method].Add(0, "end");

            classObj.commandEdges[method].Add(1, new Dictionary<int, string>());
            classObj.commandEdges[method].Add(0, new Dictionary<int, string>());
            redrawGraph();
        }
    }

    //add atribute to graph
    internal void AddAttribute(string attribute, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            classObj.attributes.Add(attribute);
            redrawGraph();
        }
    }

    //remove method from class
    public void RemoveMethodFromClass(string className, string method)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == className);
        if (classObj != null)
        {
            classObj.methods.Remove(method);
            classObj.methodCommands.Remove(method);
            classObj.commandKeys.Remove(method);
            classObj.commandEdges.Remove(method);
            classObj.closeIfElse.Remove(method);
            redrawGraph();
        }        
    }

    //remove atribute  from class
    public void RemoveAttributeFromClass(string className, string attribute)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == className);
        if (classObj != null)
        {
            classObj.attributes.Remove(attribute);
            redrawGraph();
        }        
    }

    //remove edge from graph
    internal void RemoveEdge(string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            List<string> target_classes = new List<string>();
            foreach (var target in classObj.connections.Keys) { target_classes.Add(target); }
            TMP_Dropdown targetClassesMenu = removeEdgePopUp.GetComponentInChildren<TMP_Dropdown>();
            targetClassesMenu.ClearOptions();
            targetClassesMenu.AddOptions(target_classes);
            removeEdgePopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Remove edge from class";
            removeEdgePopUp.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveEdgeFromGraph(name));
            removeEdgePopUp.SetActive(true);
        }
    }

    private void RemoveEdgeFromGraph(string name)
    {
        removeEdgePopUp.SetActive(false);
        Class_object fromClass = classObjects.Find(obj => obj.name == name);
        if (fromClass != null)
        {
            TMP_Dropdown targetClassesMenu = removeEdgePopUp.GetComponentInChildren<TMP_Dropdown>();
            string target_class = targetClassesMenu.options[targetClassesMenu.value].text;
            if (fromClass.connections.ContainsKey(target_class))
            {
                fromClass.connections.Remove(target_class);
            }
            //moze vrzat bo nezda sa mi content slovnikov je to divno
            if (fromClass.hrany.ContainsKey(target_class))
            {
                fromClass.hrany.Remove(target_class);
            }
            if (fromClass.UIedges.ContainsKey(target_class))
            {
                Destroy(fromClass.UIedges[target_class]);
                fromClass.UIedges.Remove(target_class);
            }
            redrawGraph();
        }
    }

    //remove class from graph
    public void RemoveClass(string className)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == className);
        if (classObj != null)
        {
            // remove edges
            foreach (KeyValuePair<string, GameObject> edges in classObj.UIedges) { Destroy(edges.Value); }
            classObjects.Remove(classObj);
            foreach (var claz in classObjects)
            {
                if (claz.UIedges.ContainsKey(className))
                {
                    Destroy(claz.UIedges[className]);
                    claz.UIedges.Remove(className);
                    claz.connections.Remove(className);
                }
            }
            redrawGraph();
        }
    }

    //add activity node
    internal int AddActivityNode(string text, string method, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            int newKey = 2;
            foreach (int key in classObj.commandKeys[method].Keys) { if (key > newKey) { newKey = key; } }
            classObj.commandKeys[method].Add(newKey + 1, text);
            classObj.methodCommands[method].Add(text);
            classObj.commandEdges[method].Add(newKey + 1, new Dictionary<int, string>());
            return newKey + 1;
        }
        return -1;
    }

    //add activity edge
    public void AddActionEdge(int key, int targetIndex, string method, string connection, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            classObj.commandEdges[method][key].Add(targetIndex, connection);
        }
    }

    //remove activity node
    internal void removeActionClass(string method, int key, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            if (!classObj.commandKeys[method][key].Equals("start") || !classObj.commandKeys[method][key].Equals("end"))
            {
                //!!!!!pozor na repetetivnost nemusi byt ideal do buducna
                classObj.methodCommands[method].Remove(classObj.commandKeys[method][key]);
            }
            classObj.commandEdges[method].Remove(key);
            classObj.commandKeys[method].Remove(key);
            foreach (var command in classObj.commandEdges[method])
            {
                if (classObj.commandEdges[method][command.Key].ContainsKey(key))
                {
                    classObj.commandEdges[method][command.Key].Remove(key);
                }
            }
        }
    }

    //remove activity edge
    internal void removeActionEdge(int key, int targetIndex, string method, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            classObj.commandEdges[method][key].Remove(targetIndex);
        }
    }

    //reroute graph dragableUI part
    internal void rerouteGraph()
    {
        foreach(Class_object claz in classObjects)
        {            
            Vector2 anchoredPosition = claz.UInode.GetComponent<RectTransform>().anchoredPosition;
            Node msaglNode = claz.vrchol;
            msaglNode.Center = new Point((anchoredPosition.x + 425) / factor, (anchoredPosition.y+ 540)/ factor);
        }
        LayoutHelpers.RouteAndLabelEdges(graph, settings, graph.Edges,0, new CancelToken());
        foreach(Class_object claz in classObjects)
        {
            foreach(KeyValuePair<string,Edge> hrana in claz.hrany)
            {
                GameObject line = claz.UIedges[hrana.Key];

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
                lineRenderer.points = points.ToArray();
                lineRenderer.SetVerticesDirty();
                lineRenderer.Rebuild(CanvasUpdate.PreRender);
            }
        }
    }
}
