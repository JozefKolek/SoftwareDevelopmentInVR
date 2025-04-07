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
using UnityEditor;
using UnityEngine.SceneManagement;

public class GenerateCode : MonoBehaviour
{
    public GameObject canvas;
    public GameObject CompilationCanvas;
    public GameObject EditCanvas;
    public GameObject Result;
    public GameObject ErrorOutput;
    public GameObject CloseButton;
    public GameObject SaveButton;
    public List<Class_object> class_Objects;
    public List<string> output = new List<string>();
    private List<Class_object> classObjects;
    private Canvas canvasObj;
    public List<int> usedKeys;
    public Dictionary<int, int> loopKeys;
    public Dictionary<int, string> commandTypes;
    public Dictionary<int, string> mergeTypes;
    public Dictionary<string, List<string>> outputClassFiles = new Dictionary<string, List<string>>();
    string preoutputPath;
    public string dllPath; // Cesta, kam uloûÌö DLL
    public List<string> OutputError = new List<string>();
    public string uroven;
    private void Start()
    {
        preoutputPath = Application.persistentDataPath + "/Preoutput/";
        if (!Directory.Exists(preoutputPath))
        {
            Directory.CreateDirectory(preoutputPath);
        }
    }
    public void initialise(List<Class_object> classObjects, Canvas canvasObj,string level)
    {
        this.classObjects = classObjects;
        this.canvasObj = canvasObj;
        this.uroven = level;

        CloseButton.GetComponent<Button>().onClick.AddListener(() => closeCanvas());
        SaveButton.GetComponent<Button>().onClick.AddListener(() => saveFile());
    }

    private void closeCanvas()
    {
        CompilationCanvas.SetActive(false);
        canvas.SetActive(true);
        EditCanvas.SetActive(true);
        OutputError = new List<string>();
        ErrorOutput.SetActive(false);
        ErrorOutput.GetComponentInChildren<TextMeshProUGUI>().text = "";


        foreach(var component in Result.GetComponents<Component>())
        {
            if(!(component is RectTransform)){Destroy(component);}
        }
        List<string> forStay = new List<string>(){ "Directional Light", "MRTK XR Rig", "MRTKInputSimulator", "ActivityCanvas", "Canvas", "EventSystem", "CompilationCanvas", "EditableCanvas","Sun"};
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            if (!forStay.Contains(obj.name))
            {
                Destroy(obj);
            }                            
        }

        string[] files = Directory.GetFiles(preoutputPath);
        foreach (string file in files)
        {
            File.Delete(file);
            Debug.Log("Deleted: " + file);
        }

        EditCanvas.SetActive(true);
    }

    public void saveFile()
    {        
        string[] files = Directory.GetFiles(Application.persistentDataPath + "/SampleCode/" + uroven + "/");
        foreach (string file in files)
        {
            File.Delete(file);
            Debug.Log("Deleted: " + file);
        }
        foreach (KeyValuePair<string, List<string>> file in outputClassFiles)
        {
            string filePath = Application.persistentDataPath + "/SampleCode/" + uroven + "/" + file.Key + ".cs";
            try
            {
                File.WriteAllLines(filePath, file.Value);
            }
            catch (IOException)
            {
                Console.WriteLine("An error occurred while writting to the file");
            }
        }
    }
    public void generateCode()
    {               
        this.class_Objects = classObjects;
        outputClassFiles = new Dictionary<string, List<string>>();
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
            if (!outputClassFiles.ContainsKey(claz.name)) { outputClassFiles.Add(claz.name, output); }
        }
        string folder = preoutputPath;
        foreach(var file in Directory.GetFiles(folder, "*.cs")){File.Delete(file);}
        Debug.Log("Vysledok");
        foreach (KeyValuePair<string, List<string>> file in outputClassFiles)
        {
            string filePath = preoutputPath + file.Key + ".cs";
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
        if (CompileUnityScriptsInFolder(preoutputPath))
        {
            EditCanvas.SetActive(false);
            canvas.SetActive(false);
            CompilationCanvas.SetActive(true);
            ErrorOutput.SetActive(false);
            AttachScriptToGameObject(Result);            
        } else
        {
            EditCanvas.SetActive(false);
            canvas.SetActive(false);
            CompilationCanvas.SetActive(true);
            ErrorOutput.SetActive(true);
            ErrorOutput.GetComponentInChildren<TextMeshProUGUI>().text = string.Join('\n', OutputError);
        }

    }    
    public void generateMethodCode(Class_object class_Object,string method)
    {
        int start = 1;
        usedKeys = new List<int>();
        loopKeys = new Dictionary<int, int>();
        commandTypes = new Dictionary<int, string>();
        mergeTypes = new Dictionary<int, string>();
        
        generateCommandTypes(start, class_Object, method, 0);
        mergeNodes(class_Object, method);
        if (!commandTypes.ContainsKey(0)) { commandTypes.Add(0, "end"); }
        if (!commandTypes.ContainsKey(1)) { commandTypes.Add(1, "command"); }
        string vypluj = "Metoda " + method;
        foreach(var j in commandTypes)
        {
            vypluj += " " + j.Key + " "  + class_Object.commandKeys[method][j.Key] + " " + j.Value + " " + mergeTypes[j.Key];
        }
        Debug.Log(string.Join('\n',vypluj));
        
        usedKeys = new List<int>();
        if (class_Object.commandKeys[method].ContainsKey(1))
        {
            workCommand(start, class_Object, method, 0);
        }        
    }
    private void mergeNodes(Class_object class_Object, string method)
    {        
        foreach(var i in class_Object.commandEdges[method])
        {
            int counter = 0;
            foreach(var j in class_Object.commandEdges[method])
            {
                if (j.Key!= i.Key)
                {
                    foreach(var k in class_Object.commandEdges[method][j.Key])
                    {
                        if (i.Key == k.Key) { counter += 1; }
                    }
                }
            }
            if(counter == 1) { mergeTypes.Add(i.Key, "no"); }
            else if (counter > 1) { mergeTypes.Add(i.Key, "yes"); }
            else if (i.Key == 1){ mergeTypes.Add(1, "start"); }
            else { mergeTypes.Add(i.Key, "nezname"); }
        }
    }
    private void generateCommandTypes(int fromKey, Class_object class_Object, string method, int lastKey)
    {
        if (usedKeys.Contains(fromKey)) { return; }
        usedKeys.Add(fromKey);
        if(class_Object.commandEdges[method][fromKey].Count == 1)
        {
            if (class_Object.commandKeys[method][fromKey].Equals("else")){
                commandTypes.Add(fromKey, "elseCondition");
            } else
            {
                commandTypes.Add(fromKey, "command");
            }
        } else if (class_Object.commandEdges[method][fromKey].Count == 2)
        {
            bool isLoop = false;
            foreach (var node in class_Object.commandEdges[method])
            {
                if (node.Key != fromKey)
                {
                    foreach (var loopKey in class_Object.commandEdges[method][node.Key])
                    {
                        if (loopKey.Key == fromKey && lastKey != node.Key && !usedKeys.Contains(node.Key)) { isLoop = true; break; }
                    }
                }
            }            
            if (isLoop)
            {
                commandTypes.Add(fromKey, "while");
            }
            else
            {
                commandTypes.Add(fromKey, "if");
            }
        } else
        {
            commandTypes.Add(fromKey,"command");
        }
        foreach(var node in class_Object.commandEdges[method][fromKey])
        {
            generateCommandTypes(node.Key, class_Object, method, fromKey);
        }
    }

    private void workCommand(int fromKey, Class_object class_Object, string method, int lastKey)
    {
        if (usedKeys.Contains(fromKey)) return; // Uû spracovan˝ uzol
        usedKeys.Add(fromKey);
        Debug.Log("Skacem na prikaz " + method + " " + class_Object.commandKeys[method][fromKey]);
        if (commandTypes[fromKey].Equals("command"))
        {
            if (fromKey != 0 && fromKey != 1) // Start a End vynech·me
            {
                string doplnok = class_Object.commandKeys[method][fromKey].EndsWith(";") ? "" : ";";
                output.Add(class_Object.commandKeys[method][fromKey] + doplnok);
            }
            List<int> edges = class_Object.commandEdges[method][fromKey].Keys.ToList();

            if (edges.Count == 1)
            {
                if(edges[0] == 0) { return; }
                else if (loopKeys.Keys.Contains(edges[0]) || loopKeys.Values.Contains(edges[0]))
                {
                    return;
                }
                else if (mergeTypes[edges[0]].Equals("yes") && commandTypes[edges[0]].Equals("command"))
                {
                    return;
                }
                workCommand(edges[0], class_Object, method, fromKey);
            }                        
        }
        else if (commandTypes[fromKey].Equals("while")){
            output.Add("while (" + class_Object.commandKeys[method][fromKey] + ")");
            output.Add("{");
            List<int> loopKey = new List<int>();
            foreach (KeyValuePair<int, Dictionary<int, string>> i in class_Object.commandEdges[method])
            {
                if (i.Key != fromKey && i.Key != lastKey && class_Object.commandEdges[method][i.Key].ContainsKey(fromKey))
                {
                    loopKey.Add(i.Key);
                }
            }
            if (loopKey.Count == 1)
            {
                loopKeys.Add(fromKey, loopKey[0]);
                Debug.Log( "While v " + method + " Klasicky while s poslednym prikazom ktory returnuje");                
            } else if (loopKey.Count > 1)
            {
                loopKeys.Add(fromKey, fromKey);
                Debug.Log("Vela Whiles if v " + method + " Klasicky while s poslednym prikazom ktory returnuje");
            }
            List<int> noKeys = new List<int>();
            List<int> bodyKeys = new List<int>();
            //Decide which node i should continue first go to body of while than other
            foreach (var toKey in class_Object.commandEdges[method][fromKey])
            {
                List<int> route = FindRoute(class_Object, method, toKey.Key, loopKeys[fromKey]);
                bool existConnection = true;
                //check if exist from next node route to loop key by only unused nodes
                if (route.Count == 0) { existConnection = false; }
                Debug.Log("Pre while " + method + " " + toKey.Key + " je cesta " + string.Join(' ', route));
                int counter = 0;
                foreach (int i in route)
                {
                    if (usedKeys.Contains(i))
                    {
                        if(counter == route.Count-1 && loopKeys[fromKey] == fromKey && route[counter] == fromKey)
                        {

                        } else
                        {
                            existConnection = false;
                            break;
                        }                        
                    }
                    counter++;
                }                
                if (existConnection)
                {
                    bodyKeys.Add(toKey.Key);
                }
                else
                {
                    noKeys.Add(toKey.Key);
                }
            }
            Debug.Log("Pre while cyklus " + method + " su noKeys " + string.Join(' ', noKeys) + " a bodyKeys " + string.Join(' ',bodyKeys));
            if (noKeys.Count == 1 && bodyKeys.Count == 1)
            {
                workCommand(bodyKeys[0], class_Object, method, fromKey);
                if (!loopKeys[fromKey].Equals(fromKey))
                {
                    usedKeys.Add(loopKeys[fromKey]);
                    string doplnok = class_Object.commandKeys[method][loopKeys[fromKey]].EndsWith(";") ? "" : ";";
                    Debug.Log("Pridal som loop key " + class_Object.commandKeys[method][loopKeys[fromKey]]);
                    output.Add(class_Object.commandKeys[method][loopKeys[fromKey]] + doplnok);
                }
                output.Add("}");                
                if (mergeTypes[noKeys[0]].Equals("no") && !loopKeys.Keys.Contains(noKeys[0]) && !loopKeys.Values.Contains(noKeys[0]))
                {
                    workCommand(noKeys[0], class_Object, method, fromKey);
                } else if (mergeTypes[noKeys[0]].Equals("yes") && !loopKeys.Keys.Contains(noKeys[0]) && !loopKeys.Values.Contains(noKeys[0]))
                {
                    Debug.Log("Isiel som na " + class_Object.commandKeys[method][noKeys[0]]);
                    workCommand(noKeys[0], class_Object, method, fromKey);
                }                
            }
        } else if (commandTypes[fromKey].Equals("if"))
        {
            output.Add("if (" + class_Object.commandKeys[method][fromKey] + ")");
            output.Add("{");
            int yesKey = -1;
            int noKey = -1;
            List<int> toKeys = class_Object.commandEdges[method][fromKey].Keys.ToList();
            if (commandTypes[toKeys[0]].Equals("elseCondition") && !commandTypes[toKeys[1]].Equals("elseCondition"))
            {
                yesKey = toKeys[1];
                noKey = toKeys[0];
            }
            else if (commandTypes[toKeys[1]].Equals("elseCondition") && !commandTypes[toKeys[0]].Equals("elseCondition"))
            {
                yesKey = toKeys[0];
                noKey = toKeys[1];
            }
            else if (class_Object.commandKeys[method][toKeys[0]].Equals("else") && class_Object.commandKeys[method][toKeys[1]].Equals("else"))
            {
                Debug.Log("Bad activity diagram");
            } else
            {
                //Ak za ifom nenasleduje else cast
                List<int> from_0_to_1 = FindRoute(class_Object, method, toKeys[0], toKeys[1]);
                List<int> from_1_to_0 = FindRoute(class_Object, method, toKeys[1], toKeys[0]);
                Debug.Log(method + " " + toKeys[0] + " " + toKeys[1] +  " " + string.Join(' ', from_0_to_1) + " " + string.Join(' ',usedKeys));
                Debug.Log(method + " " + toKeys[1] + " " + toKeys[0] + " " + string.Join(' ', from_1_to_0));
                //route must come over only unused nodes
                bool existConnection_from_0_to_1 = true;
                bool existConnection_from_1_to_0 = true;
                if (from_0_to_1.Count > 0)
                {
                    int counter = 0;
                    foreach (int i in from_0_to_1)
                    {
                        if (usedKeys.Contains(i))
                        {
                            if (counter == from_0_to_1.Count-1 && loopKeys.ContainsKey(toKeys[1]) && loopKeys[toKeys[1]] == toKeys[1] && from_0_to_1[counter] == toKeys[1])
                            {
                                existConnection_from_0_to_1 = true;
                            } else
                            {
                                existConnection_from_0_to_1 = false;
                                break;
                            }                            
                        }
                        counter++;
                    }
                } else
                {
                    existConnection_from_0_to_1 = false;
                }
                if (from_1_to_0.Count > 0)
                {
                    int counter = 0;
                    foreach (int i in from_1_to_0)
                    {
                        if (usedKeys.Contains(i))
                        {
                            if(counter == from_1_to_0.Count - 1 && loopKeys.ContainsKey(toKeys[0]) && loopKeys[toKeys[0]] == toKeys[0] && from_1_to_0[counter] == toKeys[0])
                            {
                                existConnection_from_1_to_0 = true;
                            } else
                            {
                                existConnection_from_1_to_0 = false;
                                break;
                            }                            
                        }
                        counter++;
                    }
                } else
                {
                    existConnection_from_1_to_0 = false;
                }
                if (!existConnection_from_0_to_1 && existConnection_from_1_to_0)
                {
                    yesKey = toKeys[1];
                    noKey = toKeys[0];
                    Debug.Log("tentok 1");
                }
                else if (existConnection_from_0_to_1 && !existConnection_from_1_to_0)
                {
                    yesKey = toKeys[0];
                    noKey = toKeys[1];
                    Debug.Log("tentok 2");
                }
                else
                {
                    Debug.Log("Bad activity diagram");
                }
            }
            if (noKey != -1 && yesKey != -1)
            {
                Debug.Log("Idem spracovat if");
                workCommand(yesKey, class_Object, method, fromKey);
                output.Add("}");
                if (mergeTypes[noKey].Equals("no") && !loopKeys.Keys.Contains(noKey) && !loopKeys.Values.Contains(noKey))
                {
                    workCommand(noKey, class_Object, method, fromKey);
                }
                else if (mergeTypes[noKey].Equals("yes") && !loopKeys.Keys.Contains(noKey) && !loopKeys.Values.Contains(noKey))
                {
                    Debug.Log("Isiel som na " + class_Object.commandKeys[method][noKey]);
                    workCommand(noKey, class_Object, method, fromKey);
                }
            }

        }
        else if (commandTypes[fromKey].Equals("elseCondition"))
        {
            output.Add(class_Object.commandKeys[method][fromKey]);
            output.Add("{");
            foreach (KeyValuePair<int, string> toKey in class_Object.commandEdges[method][fromKey])
            {
                if (mergeTypes[toKey.Key].Equals("no") && !loopKeys.Keys.Contains(toKey.Key) && !loopKeys.Values.Contains(toKey.Key))
                {
                    workCommand(toKey.Key, class_Object, method, fromKey);
                }
                else if (mergeTypes[toKey.Key].Equals("yes") && !loopKeys.Keys.Contains(toKey.Key) && !loopKeys.Values.Contains(toKey.Key))
                {
                    Debug.Log("Isiel som na " + class_Object.commandKeys[method][toKey.Key]);
                    workCommand(toKey.Key, class_Object, method, fromKey);
                }
            }
            output.Add("}");
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

        // Kompilujeme VäETKY s˙bory naraz, nie jednotlivo!
        List<SyntaxTree> syntaxTrees = scriptFiles
            .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), new CSharpParseOptions(preprocessorSymbols: new[] { "UNITY_EDITOR", "UNITY_2021" })))
            .ToList();

        // ZÌskame vöetky potrebnÈ kniûnice Unity (UnityEngine, UnityEngine.UI, UnityEditor)
        var references = GetUnityReferences();

        // Kompilujeme vöetko do jednej assembly
        var compilation = CSharpCompilation.Create(
            "UnityProjectCompilation",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        dllPath = preoutputPath + "compiledScripts_" + DateTime.Now.Ticks.ToString() + ".dll";
        Debug.Log("DllPatdh " + dllPath);
        using (var fileStream = new FileStream(dllPath, FileMode.Create))
        {
            var result = compilation.Emit(fileStream); // Emit the compilation to the FileStream

            if (!result.Success)
            {
                OutputError.Add("Chyby pri kompil·cii Unity skriptov:");
                foreach (var chyba in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    OutputError.Add(chyba.ToString());
                }
                return false;
            }
        }

        Debug.Log($"Kompil·cia ˙speön·! DLL uloûen· na {dllPath}");
        return true;
    }

    private IEnumerable<MetadataReference> GetUnityReferences()
    {
        // Cesta k prieËinku Unity, ktor˝ obsahuje vöetky kniûnice
        string unityEditorPath = @"C:\Program Files\Unity\Hub\Editor\2021.3.23f1\Editor\Data\Managed\";

        // ZÌskame vöetky .dll s˙bory v prieËinku Managed
        var unityAssemblies = Directory.GetFiles(unityEditorPath, "*.dll", SearchOption.AllDirectories)                                        
                                        .Select(assembly => MetadataReference.CreateFromFile(assembly))
                                        .ToList();
        foreach (var projectAssembliesDll in Directory.GetFiles(@"..\UnityUMLSoftwareDevelopment\Library\ScriptAssemblies","*.dll",SearchOption.AllDirectories)
            .Select(assembly => MetadataReference.CreateFromFile(assembly)).ToList())
        {
            unityAssemblies.Add(projectAssembliesDll);
        }
        // Prid·me .NET kniûnice ako referencie
        unityAssemblies.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        unityAssemblies.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
        unityAssemblies.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));

        return unityAssemblies;
    }

    public void LoadAssemblyAndAttachToGameObject(GameObject targetObject)
    {
        try
        {
            // NaËÌtanie DLL s˙boru
            Debug.Log("Idem nacitat z assembly " + dllPath);
            Assembly assembly = Assembly.LoadFrom(dllPath);

            // ZÌskaj typ triedy, ktor˙ chceö pridaù (musÌö vedieù meno triedy)
            Type[] types = assembly.GetTypes();

            // Pre kaûd˝ typ v DLL
            foreach (Type type in types)
            {
                // Skontrolujeme, Ëi typ je trieda a implementuje poûadovanÈ rozhranie alebo dedÌ od MonoBehaviour
                if (type.IsClass && type.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    Debug.Log($"NaËÌtan· trieda: {type.FullName}");

                    // Dynamicky vytvorÌme inötanciu triedy (pokiaæ m· pr·zdny konötruktor)
                    var scriptInstance = targetObject.AddComponent(type);
                    Debug.Log($"Trieda {type.Name} bola ˙speöne pripojen· k GameObjectu!");
                }                
            }                        
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chyba pri naËÌtavanÌ DLL alebo prid·vanÌ komponentu: {ex.Message}");
        }
    }

    public void AttachScriptToGameObject(GameObject targetObject)
    {
        LoadAssemblyAndAttachToGameObject(targetObject);
    }
}
