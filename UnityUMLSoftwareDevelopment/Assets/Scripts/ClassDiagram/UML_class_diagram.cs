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

    void Start()
    {
        activityCanvasObj.SetActive(false);
        classObjects = read.read_from_code();
        
        // Initialize geometry graph and layout settings
        graph = new GeometryGraph();
        settings = new SugiyamaLayoutSettings
        {
            LayerSeparation = 50,
            NodeSeparation = 50            
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

        GameObject GenerateCodeButton = Instantiate(buttonPrefab, canvasObj.transform);
        GenerateCode generator = new GenerateCode(classObjects,canvasObj);
        GenerateCodeButton.GetComponent<Button>().onClick.AddListener(() => generator.generateCode());
        GenerateCodeButton.GetComponent<Image>().color = Color.yellow;

        RectTransform GenerateCodeForm = GenerateCodeButton.GetComponent<RectTransform>();
        GenerateCodeForm.localPosition = new Vector3(-275, 150, 0);
        addClassForm.sizeDelta = new Vector2(150, 60);
        GenerateCodeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Generate code";

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
                    graph.Edges.Add(msaglHrana);
                }
            }
        }

        LayoutHelpers.CalculateLayout(graph, settings, null);

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width / 2;  // Half-width for clamping in anchored space
        float canvasHeight = canvasRect.rect.height / 2; // Half-height for clamping in anchored space

        foreach (var kvp in classObjects)
        {
            Node msaglNode = kvp.vrchol;

            // Calculate the new position based on MSAGL layout
            Vector2 newPosition = new Vector2((float)msaglNode.Center.X * factor, (float)msaglNode.Center.Y * factor);

            // Clamp the position to keep within canvas bounds
            newPosition.x = Mathf.Clamp(newPosition.x, -canvasWidth+150, canvasWidth-150);
            newPosition.y = Mathf.Clamp(newPosition.y, -canvasHeight+150, canvasHeight-150);

            // Apply the clamped position to the UI node
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

    internal int AddActivityNode(string text, string method, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            int newKey = 2;
            foreach(int key in classObj.commandKeys[method].Keys) { if (key > newKey) { newKey = key; } }
            classObj.commandKeys[method].Add(newKey+1, text);
            classObj.methodCommands[method].Add(text);
            classObj.commandEdges[method].Add(newKey+1, new Dictionary<int, string>());
            return newKey+1;
        }
        return -1;
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

    internal void AddAttribute(string attribute, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            classObj.attributes.Add(attribute);
            redrawGraph();
        }
    }

    public void AddActionEdge(int key, int targetIndex, string method, string connection, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {            
            classObj.commandEdges[method][key].Add(targetIndex, connection);
        }
    }

    public void RemoveAttributeFromClass(string className, string attribute)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == className);
        if (classObj != null)
        {
            classObj.attributes.Remove(attribute);
        }
    }

    // Function to remove a method from a specific class object
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
        }        
    }

    internal void RemoveEdge(string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            List<string> target_classes = new List<string>();
            foreach(var target in classObj.connections.Keys) { target_classes.Add(target); }
            TMP_Dropdown targetClassesMenu = removeEdgePopUp.GetComponentInChildren<TMP_Dropdown>();
            targetClassesMenu.ClearOptions();
            targetClassesMenu.AddOptions(target_classes);
            removeEdgePopUp.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Remove edge from class";
            removeEdgePopUp.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveEdgeFromGraph(name));
            removeEdgePopUp.SetActive(true);            
        }
    }

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
            foreach(var command in classObj.commandEdges[method])
            {
                if (classObj.commandEdges[method][command.Key].ContainsKey(key))
                {
                    classObj.commandEdges[method][command.Key].Remove(key);
                }
            }
        }     
    }

    internal void removeActionEdge(int key, int targetIndex, string method, string name)
    {
        Class_object classObj = classObjects.Find(obj => obj.name == name);
        if (classObj != null)
        {
            classObj.commandEdges[method][key].Remove(targetIndex);
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
        }
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
