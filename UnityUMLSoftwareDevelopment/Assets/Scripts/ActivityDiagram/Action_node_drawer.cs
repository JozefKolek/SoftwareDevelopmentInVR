using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Action_node_drawer : MonoBehaviour
{
    public GameObject actionPanelPrefab;
    public GameObject canvasObj;
    public GameObject buttonPrefab;
    public GameObject namePrefab;
    public UML_activity_diagram activityManager;
    public GameObject content;

    internal GameObject DrawAction(string name,int index)
    {
        int key = index;
        GameObject node = Instantiate(actionPanelPrefab, Vector3.zero, Quaternion.identity, content.transform);
        node.name = name;
        //set Action name
        GameObject action = Instantiate(namePrefab, Vector3.zero, Quaternion.identity, node.transform);
        action.GetComponent<TMP_Text>().text = name;

        GameObject addEdgeButton = Instantiate(buttonPrefab, node.transform);
        addEdgeButton.GetComponent<Image>().color = Color.magenta;
        TextMeshProUGUI addEdgeText = addEdgeButton.GetComponentInChildren<TextMeshProUGUI>();
        addEdgeText.GetComponent<TMP_Text>().text = "Add Edge";
        addEdgeButton.GetComponent<Button>().onClick.AddListener(() => AddEdge(key));

        GameObject removeButton = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity, node.transform);
        TextMeshProUGUI removeButtonText = removeButton.GetComponentInChildren<TextMeshProUGUI>();        
        removeButtonText.GetComponent<TMP_Text>().fontSize = 20;
        removeButtonText.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        removeButtonText.GetComponent<TMP_Text>().text = "Remove Action Node";
        removeButton.GetComponent<Button>().onClick.AddListener(() => reomoveActionNode(key,node));
        
        GameObject removeEdgeButton = Instantiate(buttonPrefab, node.transform);
        TextMeshProUGUI removeEdgeText = removeEdgeButton.GetComponentInChildren<TextMeshProUGUI>();
        removeEdgeText.GetComponent<TMP_Text>().fontSize = 20;
        removeEdgeText.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        removeEdgeText.GetComponent<TMP_Text>().text = "Remove Edge";
        removeEdgeButton.GetComponent<Button>().onClick.AddListener(() => RemoveEdge(key));

        // Set position of the class panel to avoid overlap
        RectTransform rectTransform = node.GetComponent<RectTransform>();
        // You can adjust this position to suit your layout
        rectTransform.anchoredPosition = new Vector2(0, 0); // Set this to a calculated position as needed

        return node;
    }
   
    private void AddEdge(int key)
    {
        activityManager.addEdge(key);
    }

    private void RemoveEdge(int key)
    {
        activityManager.removeEdge(key);
    }
    private void reomoveActionNode(int key, GameObject node)
    {
        Destroy(node);
        activityManager.removeActionClass(key);
    }

}
