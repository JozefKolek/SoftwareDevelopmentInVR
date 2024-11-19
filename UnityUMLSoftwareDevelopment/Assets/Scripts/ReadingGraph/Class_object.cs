using Microsoft.Msagl.Core.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Class_object
{
    public string name;
    public Dictionary<string, string> connections = new Dictionary<string, string>();
    public List<string> attributes = new List<string>();
    public List<string> methods = new List<string>();
    public Dictionary<string, List<string>> methodCommands = new Dictionary<string, List<string>>();

    public GameObject UInode;
    public Node vrchol;

    public Dictionary<string, GameObject> UIedges = new Dictionary<string, GameObject>();
    public Dictionary<string, Edge> hrany = new Dictionary<string, Edge>();

    public Dictionary<string, Dictionary<string, GameObject>> method_Activity_nodes;

    public Class_object(string name)
    {
        this.name = name;

    }

}
