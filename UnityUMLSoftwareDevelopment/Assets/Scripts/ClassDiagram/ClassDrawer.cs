using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class ClassDrawer : MonoBehaviour
{
    public GameObject mainPanelPrefab;      // Prefab for the main class panel
    public GameObject namePrefab;           // Prefab for displaying the class name
    public GameObject attributePrefab;      // Prefab for displaying each attribute
    public GameObject methodPrefab;         // Prefab for displaying each method
    public GameObject canvasObj;
    public GameObject buttonPrefab;
    public GameObject addMetOrAt;
    public GameObject atOrMetContainerPrefab;

    public UML_class_diagram umlManager;
    public UML_activity_diagram activity_Diagram;
    // Canvas to hold the panels

    // Creates a panel for a single Class_object instance
    public GameObject CreateClassPanel(Class_object classObj)
    {
        GameObject mainPanel = Instantiate(mainPanelPrefab, Vector3.zero, Quaternion.identity, canvasObj.transform);
        GameObject addMethodOrClass = Instantiate(addMetOrAt, canvasObj.transform);

        addMethodOrClass.SetActive(false);

        mainPanel.name = classObj.name;

        // Set class name
        GameObject nameObj = Instantiate(namePrefab, Vector3.zero, Quaternion.identity, mainPanel.transform);
        nameObj.GetComponent<TMP_Text>().text = classObj.name;

        // Attributes section
        foreach (string attribute in classObj.attributes)
        {
            GameObject container = Instantiate(atOrMetContainerPrefab, Vector3.zero, Quaternion.identity, mainPanel.transform);
            GameObject attributeObj = Instantiate(attributePrefab, Vector3.zero, Quaternion.identity, container.transform);
            TextMeshProUGUI textComponent = attributeObj.GetComponentInChildren<TextMeshProUGUI>();
            textComponent.GetComponent<TMP_Text>().text = attribute;
            GameObject removeAttributeButton = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity, container.transform);
            removeAttributeButton.GetComponent<Image>().color = Color.green;
            TextMeshProUGUI removeAttributeButtonText = removeAttributeButton.GetComponentInChildren<TextMeshProUGUI>();
            removeAttributeButtonText.GetComponent<TMP_Text>().text = "-";
            removeAttributeButtonText.fontSize = 32;
            removeAttributeButton.GetComponent<Button>().onClick.AddListener(() => RemoveAttribute(container, attribute, classObj.name));
        }

        // Methods section
        foreach (string method in classObj.methods)
        {
            GameObject container = Instantiate(atOrMetContainerPrefab, Vector3.zero, Quaternion.identity, mainPanel.transform);
            GameObject methodObj = Instantiate(methodPrefab, Vector3.zero, Quaternion.identity, container.transform);
            TextMeshProUGUI textComponent = methodObj.GetComponentInChildren<TextMeshProUGUI>();
            textComponent.GetComponent<TMP_Text>().text = method;
            textComponent.GetComponent<Button>().onClick.AddListener(() => displayActivityDiagram(method, classObj));
            GameObject removemethodButton = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity, container.transform);
            removemethodButton.GetComponent<Image>().color = Color.cyan;
            TextMeshProUGUI removemethodButtonText = removemethodButton.GetComponentInChildren<TextMeshProUGUI>();
            removemethodButtonText.GetComponent<TMP_Text>().text = "-";
            removemethodButtonText.fontSize = 32;
            removemethodButton.GetComponent<Button>().onClick.AddListener(() => RemoveMethod(container, method, classObj.name));
        }

        GameObject addContainer = Instantiate(atOrMetContainerPrefab, mainPanel.transform);

        //Buttons for adding methods, attributes and removing class
        GameObject addAttribute = Instantiate(buttonPrefab, addContainer.transform);
        addAttribute.GetComponent<Image>().color = Color.magenta;
        TextMeshProUGUI addAttributeText = addAttribute.GetComponentInChildren<TextMeshProUGUI>();
        addAttributeText.GetComponent<TMP_Text>().text = "Add Attribute";
        addAttributeText.fontSize = 20;
        addAttribute.GetComponent<Button>().onClick.AddListener(() => AddAttribute(classObj.name, addMethodOrClass));

        GameObject addMethod = Instantiate(buttonPrefab, addContainer.transform);
        addMethod.GetComponent<Image>().color = Color.magenta;
        TextMeshProUGUI addMethodText = addMethod.GetComponentInChildren<TextMeshProUGUI>();
        addMethodText.GetComponent<TMP_Text>().text = "Add Method";
        addMethodText.fontSize = 20;
        addMethod.GetComponent<Button>().onClick.AddListener(() => AddMethod(classObj.name, addMethodOrClass));

        GameObject addEdgeButton = Instantiate(buttonPrefab, addContainer.transform);
        addEdgeButton.GetComponent<Image>().color = Color.magenta;
        TextMeshProUGUI addEdgeText = addEdgeButton.GetComponentInChildren<TextMeshProUGUI>();
        addEdgeText.GetComponent<TMP_Text>().text = "Add Edge";
        addEdgeText.fontSize = 20;
        addEdgeButton.GetComponent<Button>().onClick.AddListener(() => AddEdge(classObj.name));

        GameObject removeContainer = Instantiate(atOrMetContainerPrefab, mainPanel.transform);
        GameObject removeEdgeButton = Instantiate(buttonPrefab, removeContainer.transform);
        TextMeshProUGUI removeEdgeText = removeEdgeButton.GetComponentInChildren<TextMeshProUGUI>();
        removeEdgeText.GetComponent<TMP_Text>().fontSize = 20;
        removeEdgeText.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        removeEdgeText.GetComponent<TMP_Text>().text = "Remove Edge";
        removeEdgeButton.GetComponent<Button>().onClick.AddListener(() => RemoveEdge(classObj.name));

        GameObject removeClassButton = Instantiate(buttonPrefab, removeContainer.transform);
        TextMeshProUGUI removeClassText = removeClassButton.GetComponentInChildren<TextMeshProUGUI>();
        removeClassText.GetComponent<TMP_Text>().fontSize = 20;
        removeClassText.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        removeClassText.GetComponent<TMP_Text>().text = "Remove Class";
        removeClassButton.GetComponent<Button>().onClick.AddListener(() => RemoveClass(mainPanel, classObj.name));

        // Set position of the class panel to avoid overlap
        RectTransform rectTransform = mainPanel.GetComponent<RectTransform>();
        // You can adjust this position to suit your layout
        rectTransform.anchoredPosition = new Vector2(0, 0); // Set this to a calculated position as needed

        return mainPanel;
    }

    private void AddEdge(string name)
    {
        //umlManager.AddEdge(name);
    }

    private void AddMethod(string name, GameObject addMetOrAt)
    {
        addMetOrAt.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Add method to class";
        addMetOrAt.SetActive(true);
        addMetOrAt.GetComponentInChildren<Button>().onClick.AddListener(() => AddMethodToClass(name, addMetOrAt));
    }

    private void AddMethodToClass(string name, GameObject addMetOrAt)
    {
        addMetOrAt.SetActive(false);
        string method = addMetOrAt.GetComponentInChildren<TMP_InputField>().text;
        //umlManager.AddMethod(method.Trim(' '), name);
    }

    private void AddAttribute(string name, GameObject addMetOrAt)
    {
        addMetOrAt.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = "Add attribute to class";
        addMetOrAt.SetActive(true);
        addMetOrAt.GetComponentInChildren<Button>().onClick.AddListener(() => AddAttributeToClass(name, addMetOrAt));
    }

    private void AddAttributeToClass(string name, GameObject addMetOrAt)
    {
        addMetOrAt.SetActive(false);
        string attribute = addMetOrAt.GetComponentInChildren<TMP_InputField>().text;
        //umlManager.AddAttribute(attribute.Trim(' '), name);
    }

    private void RemoveAttribute(GameObject attributeObj, string attribute, string name)
    {
        Destroy(attributeObj);
        //umlManager.RemoveAttributeFromClass(name, attribute);
    }

    private void RemoveMethod(GameObject methodObj, string method, string name)
    {
        Destroy(methodObj);
        //umlManager.RemoveMethodFromClass(name, method);
    }

    private void RemoveEdge(string name)
    {
        //umlManager.RemoveEdge(name);
    }

    private void RemoveClass(GameObject Klas, string className)
    {
        Destroy(Klas);
        //umlManager.RemoveClass(className);
    }

    private void displayActivityDiagram(string method, Class_object classObj)
    {
        canvasObj.SetActive(false);
        activity_Diagram.initialise();
        activity_Diagram.drawDiagram(method, classObj);
    }
}