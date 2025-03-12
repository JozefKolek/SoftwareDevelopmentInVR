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
    public Dictionary<string, List<string>> outputClassFiles = new Dictionary<string, List<string>>();
    Dictionary<int, int> urovne = new Dictionary<int, int>();
    int uroven = 1;
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
        foreach (Class_object claz in classObjects)
        {
            output = new List<string>();
            //libaries
            foreach (string usen in claz.usings)
            {
                output.Add(usen);
            }
            //initialise class name
            List<string> clazName = new List<string>();
            if (!claz.visibility.Equals("Unknown")) { clazName.Add(claz.visibility); }
            if (claz.isAbstract) { clazName.Add("abstract"); }
            if (claz.isVirtual) { clazName.Add("virtual"); }
            clazName.Add(claz.type);
            clazName.Add(claz.name);
            int counter = 0;
            foreach (var connect in claz.connections)
            {
                if (connect.Value.Equals("Generalisation") || connect.Value.Equals("Realisation"))
                {
                    counter++;
                    if (counter == 1)
                    {
                        clazName.Add(":");
                        clazName.Add(connect.Key);
                    }
                    else
                    {
                        clazName.Add(", " + connect.Key);
                    }
                }
            }
            clazName.Add("{");
            output.Add(String.Join(" ", clazName));
            string vypis = "";
            foreach (string i in output)
            {
                vypis += i + "\n";
            }
            //adding attributes
            foreach (var attribute in claz.attributes)
            {
                if (attribute.Contains("set;") || attribute.Contains("set;"))
                {
                    output.Add(attribute.Trim(' '));
                }
                else
                {
                    output.Add(attribute.Trim(' ') + ";");
                }

            }
            //adding method and their body code
            foreach (var method in claz.methods)
            {
                output.Add(method + " {");
                generateMethodCode(claz, method);
                output.Add("}");
            }
            output.Add("}");
            outputClassFiles.Add(claz.name, output);
        }

        Debug.Log("Vysledok");
        foreach (KeyValuePair<string, List<string>> file in outputClassFiles)
        {
            string filePath = "C:/Users/Admin/Desktop/Preoutput/" + file.Key + ".cs";
            try
            {
                File.Delete(filePath);
                File.WriteAllLines(filePath, file.Value);                
            }
            catch (IOException)
            {
                Console.WriteLine("An error occurred while writting to the file");
            }            
        }
        CompileUnityScriptsInFolder("C:/Users/Admin/Desktop/Preoutput/");
        foreach (KeyValuePair<string, List<string>> file in outputClassFiles)
        {
            string filePath = "Assets/SampleOutputs/" + file.Key + ".cs";
            try
            {
                File.Delete(filePath);
                File.WriteAllLines(filePath, file.Value);                
            }
            catch (IOException)
            {
                Console.WriteLine("An error occurred while writting to the file");
            }            
        }
    }    
    public void generateMethodCode(Class_object class_Object,string method)
    {
        int start = 1;
        usedKeys = new List<int>();
        loopKeys = new Dictionary<int, int>();
        commandTypes = new Dictionary<int, string>();
        urovne.Clear();
        uroven = 1;
        generateCommandTypes(start, class_Object, method, 0);
        string vypluj = "Metoda " + method;
        foreach(var j in commandTypes)
        {
            vypluj += " " + j.Key + " "  + class_Object.commandKeys[method][j.Key] + " " + j.Value;
        }
        Debug.Log(vypluj);
        usedKeys = new List<int>();

        workCommand(start,class_Object,method,0);        
    }

    private void generateCommandTypes(int fromKey, Class_object class_Object, string method, int lastKey)
    {
        if (usedKeys.Contains(fromKey)) return;

        if (class_Object.commandEdges[method][fromKey].Count == 1 && !class_Object.commandKeys[method][fromKey].Equals("else"))
        {
            commandTypes[fromKey] = "command";
        }
        else if (class_Object.commandEdges[method][fromKey].Count == 2)
        {
            bool isLoop = class_Object.commandEdges[method].Any(edge =>
                edge.Key != fromKey && edge.Key != lastKey && edge.Value.ContainsKey(fromKey));

            if (isLoop)
            {
                commandTypes[fromKey] = "loop";
            }
            else
            {
                commandTypes[fromKey] = "if_condition";
            }
        }
        else if (class_Object.commandEdges[method][fromKey].Count == 1 && class_Object.commandKeys[method][fromKey].Equals("else"))
        {
            commandTypes[fromKey] = "else_condition";
        }

        usedKeys.Add(fromKey);
        foreach (var toKey in class_Object.commandEdges[method][fromKey])
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
                if (commandTypes.ContainsKey(edges[0]) && (commandTypes[edges[0]].Equals("command") || commandTypes[edges[0]].Equals("loopback")))
                {
                    return;
                }
            }
            workCommand(edges[0], class_Object, method, fromKey);
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
                Debug.Log("Som v ife");
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
                    Debug.Log("Idem spracovat if");
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
    public bool CompileUnityScriptsInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.Log($"PrieËinok neexistuje: {folderPath}");
            return false;
        }

        string[] scriptFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);

        if (scriptFiles.Length == 0)
        {
            Debug.LogWarning($"V prieËinku {folderPath} neboli n·jdenÈ ûiadne C# skripty.");
            return false;
        }

        List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
        foreach (var file in scriptFiles)
        {
            string scriptCode = File.ReadAllText(file);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(scriptCode));
        }

        // Cesty k dÙleûit˝m Unity DLL s˙borom (prispÙsob podæa verzie Unity)
        string unityEnginePath = @"C:/Program Files/Unity/Hub/Editor/2021.3.23f1/Editor/Data/Managed/UnityEngine.dll";
        string unityEditorPath = @"C:/Program Files/Unity/Hub/Editor/2021.3.23f1/Editor/Data/Managed/UnityEditor.dll";

        var references = new[]
        {
        MetadataReference.CreateFromFile(unityEnginePath),
        MetadataReference.CreateFromFile(unityEditorPath),
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "UnityProjectCompilation",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var result = compilation.Emit(new MemoryStream());

        if (!result.Success)
        {
            Debug.Log("Chyby pri kompil·cii Unity skriptov:");
            foreach (var chyba in result.Diagnostics)
            {
                Debug.Log(chyba.ToString());
            }
            return false;
        }

        Debug.Log($"Kompil·cia ˙speön·! {scriptFiles.Length} skriptov skontrolovan˝ch.");
        return true;
    }


}
