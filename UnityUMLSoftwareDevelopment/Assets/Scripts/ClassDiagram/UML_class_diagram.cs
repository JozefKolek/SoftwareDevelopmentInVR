using System.Collections.Generic;
using UnityEngine;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Core.Routing;
using TMPro;
using UnityEngine.UI;

public class UML_class_diagram : MonoBehaviour
{
    Reading_graph read = new Reading_graph("C:/Users/Admin/Documents/6. rocnik FMFI/Diploma2/Sample_code_C_pre_znaz_UML.cs");
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

    void Start()
    {
        activityCanvasObj.SetActive(false);
        classObjects = read.read_from_code();
        
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

        GenerateUMLDiagram();
        foreach (var claz in classObjects)
        {
            Debug.Log("zacinam trieda " + claz.name);
            foreach (var connect in claz.connections)
            {
                Debug.Log("from " + claz.name + " to " + connect.Key + " connect " + connect.Value);
            }
            foreach (var method in claz.methodCommands)
            {
                Debug.Log("metodak: " + method.Key);
                foreach (var command in method.Value)
                {
                    Debug.Log(method.Key + " " + command);
                }
            }

        }
    }

    public void GenerateUMLDiagram()
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

        // Create each class panel and set up initial positions
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

        // Adjust panel layout (spacing out panels)
        PositionPanels();

        foreach (Class_object classObj in classObjects)
        {
            // Draw connections to related classes
            foreach (var connection in classObj.connections)
            {
                Class_object targetClass = classObjects.Find(obj => obj.name == connection.Key);
                if (targetClass != null && targetClass.UInode != null)
                {
                    DrawConnectionLine(classObj.UInode.transform, targetClass.UInode.transform, classObj, targetClass);
                    Edge msaglHrana = new Edge(classObj.vrchol, targetClass.vrchol);
                    classObj.hrany.Add(targetClass.name, msaglHrana);
                }
            }
        }

        LayoutHelpers.CalculateLayout(graph, settings, null);
        float maxX = canvasObj.GetComponent<RectTransform>().rect.width / 2;
        float maxY = canvasObj.GetComponent<RectTransform>().rect.height / 2;

        foreach (var kvp in classObjects)
        {
            Node msaglNode = kvp.vrchol;

            Vector2 newPosition = new Vector2((float)msaglNode.Center.X * factor, (float)msaglNode.Center.Y * factor);

            RectTransform rectTransform = kvp.UInode.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = newPosition;                      
        }
    }    

    // Draw the line between the start and end class panels
    private void DrawConnectionLine(Transform startTransform, Transform endTransform, Class_object startClass, Class_object endClass)
    {
        // Instantiate the line prefab
        GameObject lineInstance = Instantiate(linePrefab, canvasObj.transform);

        // Initialize the line between start and end transforms
        LineUpdater lineUpdater = lineInstance.GetComponent<LineUpdater>();
        lineUpdater.Initialize(startClass.UInode, endClass.UInode);

        // Optionally, store the line in the startClass's UIedges dictionary
        startClass.UIedges[endClass.name] = lineInstance;

        Vector3 start =  lineUpdater.GetClosestEdgePosition(startTransform, endTransform.position);
        Vector3 end =  lineUpdater.GetClosestEdgePosition(endTransform,endTransform.position);
        lineUpdater.SetLinePositions(start, end);
    }
    
    // Basic layout adjustment (grid pattern, customize as needed)
    private void PositionPanels()
    {
        float offsetX = 250f;  // Adjust spacing between panels horizontally
        float offsetY = -150f; // Adjust spacing between panels vertically
        int index = 0;

        foreach (var classObj in classObjects)
        {
            if (classObj.UInode != null)
            {
                RectTransform rectTransform = classObj.UInode.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(offsetX * (index % 5), offsetY * (index / 5));
                Debug.Log($"{classObj.name} position: {rectTransform.anchoredPosition}");
                index++;
            }
        }
    }

    // Update lines when class panels are moved
    public void UpdateLines()
    {
        foreach(var claz in classObjects)
        {
            foreach (var line in claz.UIedges.Values)
            {
                var lineUpdater = line.GetComponent<LineUpdater>();
                if (lineUpdater != null)
                {
                    lineUpdater.UpdateLinePositions();
                }
            }
        }        
    }    

    internal void AddEdge(string name)
    {
        addEdgePopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Add edge to class";
        List<string> classes = new List<string>();
        foreach (var claz in classObjects) { if (!claz.name.Equals(name)) { classes.Add(claz.name); } }
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            foreach (var targetClass in classObj.connections.Keys){classes.Remove(targetClass);}            
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
        classObjects.Add(classObj);
        redrawGraph();
    }

    internal void AddMethod(string method, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            classObj.methods.Add(method);
            Debug.Log("Method " + method + " added");
            redrawGraph();
        }
    }

    internal void AddAttribute(string attribute, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            classObj.attributes.Add(attribute);
            Debug.Log("Attribute " + attribute + " added");
            redrawGraph();
        }
    }

    public void RemoveAttributeFromClass(string className, string attribute)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == className);
        if (classObj != null)
        {
            classObj.attributes.Remove(attribute);
            Debug.Log($"Attribute '{attribute}' removed from class '{className}' in data structure.");
        }
    }

    // Function to remove a method from a specific class object
    public void RemoveMethodFromClass(string className, string method)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == className);
        if (classObj != null)
        {
            classObj.methods.Remove(method);
            Debug.Log($"Method '{method}' removed from class '{className}' in data structure.");            
        }        
    }

    internal void RemoveEdge(string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            List<string> target_classes = new List<string>();
            foreach(var target in classObj.connections.Keys) { target_classes.Add(target); }
            foreach(var target in classObj.connections.Keys) { Debug.Log("On select class " + target); }
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
            Debug.Log("From to " + name  + " ffg "+ target_class);
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
            Debug.Log("Edge from " + name + " to " + target_class + " removed");
        }
    }

    public void RemoveClass(string className)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == className);
        if(classObj != null)
        {
            // remove edges
            foreach (KeyValuePair<string,GameObject> edges in classObj.UIedges) { Destroy(edges.Value);}
            classObjects.Remove(classObj);
            foreach (var claz in classObjects)
            {
                if (claz.UIedges.ContainsKey(className)) {
                    Destroy(claz.UIedges[className]);
                    claz.UIedges.Remove(className);
                    claz.connections.Remove(className);
                }
            }
            Debug.Log("Class " + className + " removed");                        
        }
    }

    internal void BackToClassDiagram()
    {
        
    }

    public void redrawGraph()
    {
        foreach (var claz in classObjects)
        {
            Destroy(claz.UInode);
            claz.UInode = null;
            foreach (KeyValuePair<string, GameObject> edge in claz.UIedges) { Destroy(edge.Value); }
            claz.UIedges.Clear();
            claz.hrany.Clear();
            claz.vrchol = null;
        }

        GenerateUMLDiagram();
    }
}