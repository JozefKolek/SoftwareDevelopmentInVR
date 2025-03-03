using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

public class GenerateCode : MonoBehaviour
{
    public GameObject canvas;
    public GameObject CompilationCanvas;
    public GameObject outputText;
    public GameObject CloseButton;
    public List<Class_object> class_Objects;
    public List<string> output = new List<string>();
    private List<Class_object> classObjects;
    private Canvas canvasObj;
    public List<int> usedKeys;
    public Dictionary<int, int> loopKeys;
    public Dictionary<int, string> commandTypes;

    public void initialise(List<Class_object> classObjects, Canvas canvasObj)
    {
        this.classObjects = classObjects;
        this.canvasObj = canvasObj;
        
        CloseButton.GetComponent<Button>().onClick.AddListener(() => closeCanvas());        
    }

    private void closeCanvas()
    {
        CompilationCanvas.SetActive(false);
        canvas.SetActive(true);
    }

    public void generateCode()
    {
        this.class_Objects = classObjects;
        foreach(Class_object claz in class_Objects)
        {
            foreach(string usen in claz.usings)
            {
                output.Add(usen);
            }
        }
        foreach(Class_object claz in class_Objects)
        {
            List<string> clazName = new List<string>();
            if (!claz.visibility.Equals("Unknown")) { clazName.Add(claz.visibility); }
            if (claz.isAbstract) { clazName.Add("abstract"); }
            if (claz.isVirtual) { clazName.Add("virtual"); }
            clazName.Add(claz.type);
            clazName.Add(claz.name);
            int counter = 0;
            foreach(var connect in claz.connections)
            {
                if (connect.Value.Equals("Generalisation") || connect.Value.Equals("Realisation"))
                {
                    counter++;
                    if (counter == 1)
                    {
                        clazName.Add(":");
                        clazName.Add(connect.Key);
                    } else
                    {
                        clazName.Add(", " + connect.Key);
                    }
                }
            }
            clazName.Add("{");
            output.Add(String.Join(" ",clazName));
            string vypis = "";
            foreach (string i in output)
            {
                vypis += i + "\n";
            }
            foreach(var attribute in claz.attributes)
            {
                if (attribute.Contains("set;") || attribute.Contains("set;"))
                {
                    output.Add(attribute.Trim(' '));
                } else
                {
                    output.Add(attribute.Trim(' ') + ";");
                }
                
            }
            foreach(var method in claz.methods)
            {
                output.Add(method + " {");
                generateMethodCode(claz, method);
                output.Add( "}");
            }
            output.Add("}");
        }
        Debug.Log("Vysledok");
        string filePath = "C:/Users/Admin/Desktop/SampleInheritance.cs";
        try
        {
            File.Delete(filePath);
            File.WriteAllLines(filePath, output);
        } catch (IOException)
        {
            Console.WriteLine("An error occurred while writting to the file");
        }
        compileCode(filePath);
    }

    public void generateMethodCode(Class_object class_Object,string method)
    {
        int start = 1;
        usedKeys = new List<int>();
        loopKeys = new Dictionary<int, int>();
        commandTypes = new Dictionary<int, string>();                
        generateCommandTypes(start, class_Object, method, 0);        
        usedKeys = new List<int>();
        workCommand(start,class_Object,method,0);        
    }

    private void generateCommandTypes(int fromKey, Class_object class_Object, string method, int lastKey)
    {
        if (usedKeys.Contains(fromKey)) { return; }
        if (class_Object.commandEdges[method][fromKey].Count == 1 && !class_Object.commandKeys[method][fromKey].Equals("else"))
        {
            if (!commandTypes.ContainsKey(fromKey)){commandTypes.Add(fromKey, "command");}
        } //while if else part
        else if (class_Object.commandEdges[method][fromKey].Count == 2)
        {
            int loopKey = -1;
            bool isLoop = false;
            // Prech·dzame vöetky uzly a zisùujeme, Ëi sa nejak˝ odkazuje sp‰ù na `while`
            foreach (KeyValuePair<int, Dictionary<int, string>> i in class_Object.commandEdges[method])
            {
                if (i.Key != fromKey && i.Key != lastKey && class_Object.commandEdges[method][i.Key].ContainsKey(fromKey))
                {
                    isLoop = true;
                    loopKey = i.Key;                    
                    break;
                }
            }
            //spracovanie while cyklus commandu
            if (isLoop){
                commandTypes.Add(fromKey, "loop");
                commandTypes.Add(loopKey, "loopback");
            } else { commandTypes.Add(fromKey, "if_condition");}
        } else if (class_Object.commandEdges[method][fromKey].Count == 1 && class_Object.commandKeys[method][fromKey].Equals("else"))
        {
            commandTypes.Add(fromKey, "else_condition");
        }
        usedKeys.Add(fromKey);
        foreach (KeyValuePair<int, string> toKey in class_Object.commandEdges[method][fromKey])
        {
            generateCommandTypes(toKey.Key, class_Object, method, fromKey);
        }
    }
    private void workCommand(int fromKey, Class_object class_Object, string method, int lastKey)
    {
        if (usedKeys.Contains(fromKey)) return; // Uû spracovan˝ uzol
        usedKeys.Add(fromKey);

        // Ak je iba jeden odkaz alebo else part, pokraËujeme priamo
        if (commandTypes.ContainsKey(fromKey) && (commandTypes[fromKey].Equals("command") || commandTypes[fromKey].Equals("loopback")))
        {
            if (fromKey != 0 && fromKey != 1) // Start a End vynech·me
            {
                string doplnok = class_Object.commandKeys[method][fromKey].EndsWith(";") ? "" : ";";
                output.Add(class_Object.commandKeys[method][fromKey] + doplnok);
            }
            //check end of if or else body
            List<int> edges = class_Object.commandEdges[method][fromKey].Keys.ToList();
            int counter = 0;
            foreach (KeyValuePair<int, Dictionary<int, string>> i in class_Object.commandEdges[method])
            {
                if (i.Value.ContainsKey(edges[0])) { counter++; }
            }
            if (loopKeys.ContainsKey(fromKey))
            {
                loopKeys.Remove(fromKey);
                return;
            }
            if (counter > 1)
            {
                Debug.Log("Som v prikaze " + class_Object.commandKeys[method][fromKey]);
                if(commandTypes.ContainsKey(edges[0]) && (commandTypes[edges[0]].Equals("command") || commandTypes[edges[0]].Equals("loopback")))
                {
                    return;
                }
            }
            foreach (var toKey in class_Object.commandEdges[method][fromKey])
            {
                workCommand(toKey.Key, class_Object, method, fromKey);
            }
        }
        //while if else part
        else if (class_Object.commandEdges[method][fromKey].Count == 2)
        {
            int loopKey = -1;
            bool isLoop = false;
            // Prech·dzame vöetky uzly a zisùujeme, Ëi sa nejak˝ odkazuje sp‰ù na `while`
            foreach (KeyValuePair<int, Dictionary<int, string>> i in class_Object.commandEdges[method])
            {
                if (i.Key != fromKey && i.Key != lastKey && class_Object.commandEdges[method][i.Key].ContainsKey(fromKey))
                {
                    isLoop = true;
                    loopKey = i.Key;
                    loopKeys.Add(loopKey, fromKey);
                    break;
                }
            }
            //spracovanie while cyklus commandu
            if (isLoop)
            {
                output.Add("while (" + class_Object.commandKeys[method][fromKey] + ")");
                output.Add("{");
                List<int> noKeys = new List<int>();
                List<int> bodyKeys = new List<int>();
                foreach (var toKey in class_Object.commandEdges[method][fromKey])
                {
                    List<int> route = FindRoute(class_Object, method, toKey.Key, loopKey);
                    bool existConnection = true;
                    foreach (int i in route)
                    {
                        if (usedKeys.Contains(i))
                        {
                            existConnection = false;
                            break;
                        }
                    }
                    if (route.Count == 0) { existConnection = false; }
                    if (existConnection)
                    {
                        bodyKeys.Add(toKey.Key);
                    }
                    else
                    {
                        noKeys.Add(toKey.Key);
                    }
                }
                if (noKeys.Count == 1 && bodyKeys.Count == 1)
                {
                    workCommand(bodyKeys[0], class_Object, method, fromKey);
                    if (loopKeys.ContainsKey(loopKey) && !usedKeys.Contains(loopKey))
                    {
                        loopKeys.Remove(loopKey);
                        usedKeys.Add(loopKey);
                        string doplnok = class_Object.commandKeys[method][loopKey].EndsWith(";") ? "" : ";";
                        output.Add(class_Object.commandKeys[method][loopKey] + doplnok);
                    }
                    output.Add("}");
                    workCommand(noKeys[0], class_Object, method, fromKey);
                }
                else { Debug.Log("Activity diagram je zly kvoli while"); }
            }
            //spracovanie if podmienky
            else
            {
                output.Add("if (" + class_Object.commandKeys[method][fromKey] + ")");
                output.Add("{");
                int yesKey = -1;
                int noKey = -1;
                List<int> toKeys = class_Object.commandEdges[method][fromKey].Keys.ToList();
                // no vetva ifu je else
                if (class_Object.commandKeys[method][toKeys[0]].Equals("else") && !class_Object.commandKeys[method][toKeys[1]].Equals("else"))
                {
                    yesKey = toKeys[1];
                    noKey = toKeys[0];
                }
                else if (class_Object.commandKeys[method][toKeys[1]].Equals("else") && !class_Object.commandKeys[method][toKeys[0]].Equals("else"))
                {
                    yesKey = toKeys[0];
                    noKey = toKeys[1];
                }
                else if (class_Object.commandKeys[method][toKeys[0]].Equals("else") && class_Object.commandKeys[method][toKeys[1]].Equals("else"))
                {
                    Debug.Log("Bad activity diagram");
                }
                else
                {
                    //Ak za ifom nenasleduje else cast
                    List<int> from_0_to_1 = FindRoute(class_Object, method, toKeys[0], toKeys[1]);
                    List<int> from_1_to_0 = FindRoute(class_Object, method, toKeys[1], toKeys[0]);
                    //route must come over only unused nodes
                    bool existConnection_from_0_to_1 = true;
                    bool existConnection_from_1_to_0 = true;
                    if (from_0_to_1.Count > 0)
                    {
                        foreach (int i in from_0_to_1)
                        {
                            if (usedKeys.Contains(i))
                            {
                                existConnection_from_0_to_1 = false;
                                break;
                            }
                        }
                    }
                    if (from_1_to_0.Count > 0)
                    {
                        foreach (int i in from_1_to_0)
                        {
                            if (usedKeys.Contains(i))
                            {
                                existConnection_from_1_to_0 = false;
                                break;
                            }
                        }
                    }
                    if (existConnection_from_0_to_1 && existConnection_from_1_to_0)
                    {
                        Debug.Log("Bad activity diagram");
                    }
                    else if (existConnection_from_0_to_1 && !existConnection_from_1_to_0)
                    {
                        yesKey = toKeys[0];
                        noKey = toKeys[1];
                    }
                    else
                    {
                        yesKey = toKeys[1];
                        noKey = toKeys[0];
                    }
                }

                if (noKey != -1 && yesKey != -1)
                {
                    workCommand(yesKey, class_Object, method, fromKey);
                    output.Add("}");
                    workCommand(noKey, class_Object, method, fromKey);
                }
            }
        }
        // spracovanie else
        else if (class_Object.commandEdges[method][fromKey].Count == 1 && class_Object.commandKeys[method][fromKey].Equals("else"))
        {
            output.Add(class_Object.commandKeys[method][fromKey]);
            output.Add("{");
            foreach (KeyValuePair<int, string> toKey in class_Object.commandEdges[method][fromKey])
            {
                workCommand(toKey.Key, class_Object, method, fromKey);
            }
            output.Add("}");
        }
        else
        {
            Debug.Log("Zly activity diagram " + class_Object.commandKeys[method][fromKey]);
        }
    }

    public List<int> FindRoute(Class_object class_Object, string method, int fromKey, int toKey)
    {
        if (fromKey == toKey) { return new List<int>(); }
        Queue<int> queue = new Queue<int>();
        Dictionary<int, int> parent = new Dictionary<int, int>();
        queue.Enqueue(fromKey);
        parent[fromKey] = -1; // OznaËenie zaËiatku

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == toKey)
            {
                // Rekonötrukcia cesty
                List<int> path = new List<int>();
                while (current != -1)
                {
                    path.Add(current);
                    current = parent[current];
                }
                path.Reverse();
                return path;
            }

            foreach (KeyValuePair<int, string> edge in class_Object.commandEdges[method][current])
            {
                if (!parent.ContainsKey(edge.Key)) // Ak eöte nebol navötÌven˝
                {
                    queue.Enqueue(edge.Key);
                    parent[edge.Key] = current;
                }
            }
        }

        return new List<int>(); // Ak cesta neexistuje
    }



    //spracuje command

    public void compileCode(string pathToFile)
    {
        canvas.SetActive(false);
        CompilationCanvas.SetActive(true);
        string scriptCode = File.ReadAllText(pathToFile);
        var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);

        // Unity assembly paths (update these paths according to your Unity installation)
        string unityEnginePath = @"C:/Program Files/Unity/Hub/Editor/2021.3.23f1/Editor/Data/Managed/UnityEngine.dll";
        string unityEditorPath = @"C:/Program Files/Unity/Hub/Editor/2021.3.23f1/Editor/Data/Managed/UnityEditor.dll";
        string mscorlibPath = @"C:/Program Files/Unity/Hub/Editor/2021.3.23f1/Editor/Data/Tools/netcorerun/mscorlib.dll";

        var references = new[]
        {
            MetadataReference.CreateFromFile(unityEnginePath),
            MetadataReference.CreateFromFile(unityEditorPath),
            MetadataReference.CreateFromFile(mscorlibPath),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            "Script Compilation",
            new[] { syntaxTree},
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        var diagnostics = compilation.GetDiagnostics();        
        using (var ms = new MemoryStream())
        {
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                outputText.GetComponent<TextMeshProUGUI>().text = "Chyby pri kompil·cii:\n";
                foreach (var chyba in result.Diagnostics)
                {
                    outputText.GetComponent<TextMeshProUGUI>().text += chyba.ToString() + "\n";
                }
                return;
            }

            //Ak kompil·cia ˙speön·, spustÌme kÛd a zachytÌme v˝stup
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var entryPoint = assembly.EntryPoint;

            if (entryPoint != null)
            {
                var stringWriter = new StringWriter();
                Console.SetOut(stringWriter); // ZachytÌme Console.WriteLine

                entryPoint.Invoke(null, new object[] { new string[0] });

                outputText.GetComponent<TextMeshProUGUI>().text = "Kompil·cia a spustenie ˙speönÈ:\n" + stringWriter.ToString();
            }
        }
    }
}
