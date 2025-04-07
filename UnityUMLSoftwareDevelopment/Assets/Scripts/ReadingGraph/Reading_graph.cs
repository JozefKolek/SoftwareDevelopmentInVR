using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

public class Reading_graph
{
    public string directoryPath;
    public CompilationUnitSyntax root;
    public int highestKey = 0;
    public int uroven = 0;
    public Dictionary<int,int>  urovne = new Dictionary<int, int>();
    public List<int> last_if_else_bodyKeys = new List<int>();
    public Reading_graph(string directoryPath)
    {
        this.directoryPath = directoryPath;
    }

    public List<Class_object> read_from_code()
    {
        List<Class_object> classList = new List<Class_object>();
        List<Dictionary<string, object>> allData = new List<Dictionary<string, object>>();
        var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
        foreach(var file in files)
        {
            Dictionary<string, Class_object> classDictionary = new Dictionary<string, Class_object>();
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = syntaxTree.GetCompilationUnitRoot();

            // Extract all classes and interfaces
            var classNodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            var interfaceNodes = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            var usings = root.Usings;
            // Extracted data
            var result = new List<object>();
            foreach (var claz in classNodes)
            {
                result.Add(ExtractClassOrInterface(claz, "Class"));
            }

            foreach (var interfejz in interfaceNodes)
            {
                result.Add(ExtractClassOrInterface(interfejz, "Interface"));
            }

            // Convert data to JSON format
            string jsonFormat = JsonConvert.SerializeObject(result, Formatting.Indented);
            Debug.Log(jsonFormat.ToString());

            // Deserialize the JSON into a list of dictionaries
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonFormat);
            foreach(var i in data) { allData.Add(i);}
            int pocitadlo = 0;
            //iterate over clases
            foreach (var claz in data)
            {
                pocitadlo++;
                // Initialize the Class_object
                string className = claz["Name"].ToString();

                Class_object class_Object = new Class_object(className);
                if (pocitadlo == 1)
                {
                    foreach (var usin in usings)
                    {
                        class_Object.usings.Add(usin.ToString());
                    }
                }

                string typ = claz.ContainsKey("Type") ? class_Object.type = claz["Type"].ToString().ToLower() : class_Object.type = "Unknown";
                string visibil = claz.ContainsKey("Visibility") ? class_Object.visibility = claz["Visibility"].ToString() : class_Object.visibility = "Unknown";
                bool isAbstract = claz.ContainsKey("IsAbstract") ? class_Object.isAbstract = (bool)claz["IsAbstract"] : class_Object.isAbstract = false;
                bool isVirtual = claz.ContainsKey("IsVirtual") ? class_Object.isVirtual = (bool)claz["IsVirtual"] : class_Object.isVirtual = false;
                // Access Attributes if they exist
                if (claz.ContainsKey("Attributes"))
                {
                    var attributes = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(claz["Attributes"], Formatting.Indented));
                    if (attributes != null && attributes.Count > 0)
                    {
                        foreach (var attribute in attributes)
                        {
                            string name = attribute.ContainsKey("Name") ? attribute["Name"].ToString() : "Unknown";
                            string type = attribute.ContainsKey("Type") ? attribute["Type"].ToString() : "Unknown";
                            string visibility = attribute.ContainsKey("Visibility") ? attribute["Visibility"].ToString() : "Unknown";
                            string defaultValue = attribute.ContainsKey("DefaultValue") && attribute["DefaultValue"] != null ? attribute["DefaultValue"].ToString() : "None";
                            string forAdd = " ";
                            if (!visibility.Equals("Unknown")) { forAdd += visibility + " "; }
                            forAdd += type + " " + name;
                            if (!defaultValue.Equals("None")) { forAdd += "= " + defaultValue; }
                            class_Object.attributes.Add(forAdd);
                        }
                    }
                    else
                    {
                        Debug.Log($"No attributes found for class: {className}");
                    }
                }

                // Access Properties if they exist
                if (claz.ContainsKey("Properties"))
                {
                    var properties = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(claz["Properties"], Formatting.Indented));
                    if (properties != null && properties.Count > 0)
                    {
                        foreach (var property in properties)
                        {
                            string name = property.ContainsKey("Name") ? property["Name"].ToString() : "Unknown";
                            string type = property.ContainsKey("Type") ? property["Type"].ToString() : "Unknown";
                            string visibility = property.ContainsKey("Visibility") ? property["Visibility"].ToString() : "Unknown";
                            string defaultValue = property.ContainsKey("DefaultValue") && property["DefaultValue"] != null ? property["DefaultValue"].ToString() : "None";
                            bool hasGetter = property.ContainsKey("HasGetter") && (bool)property["HasGetter"];
                            bool hasSetter = property.ContainsKey("HasSetter") && (bool)property["HasSetter"];
                            string forAdd = "";
                            if (!visibility.Equals("Unknown")) { forAdd += visibility + " "; }
                            forAdd += type + " " + name;
                            //docasne riesenie ak sa bude setovat value dopredu nebude to dobre
                            if (hasGetter || hasSetter)
                            {
                                forAdd += "{";
                                if (hasGetter) { forAdd += "get; "; }
                                if (hasSetter) { forAdd += " set; "; }
                                forAdd += "}";
                            }


                            if (!defaultValue.Equals("None")) { forAdd += "= " + defaultValue; }
                            class_Object.attributes.Add(forAdd);
                        }
                    }
                    else
                    {
                        Debug.Log($"No properties found for class: {className}");
                    }
                }

                // Access Constructors if they exist
                if (claz.ContainsKey("Constructors"))
                {
                    var constructors = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(claz["Constructors"], Formatting.Indented));
                    if (constructors != null && constructors.Count > 0)
                    {
                        foreach (var constructor in constructors)
                        {
                            string constructorName = constructor.ContainsKey("Name") ? constructor["Name"].ToString() : "Unknown";
                            string visibility = constructor.ContainsKey("Visibility") ? constructor["Visibility"].ToString() : "Unknown";
                            bool abstrakt = constructor.ContainsKey("IsAbstract") ? (bool)constructor["IsAbstract"] : false;
                            bool virtualn = constructor.ContainsKey("IsVirtual") ? (bool)constructor["IsVirtual"] : false;
                            var baseVariables = JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(constructor["BaseCall"], Formatting.Indented));
                            string forAdd = "";
                            if (!visibility.Equals("Unknown")) { forAdd += visibility + " "; }
                            if (virtualn) { forAdd += "virtual "; }
                            if (abstrakt) { forAdd += "abstract "; }
                            forAdd += constructorName + " (";
                            //Accessibility to parameters which constructors have
                            if (constructor.ContainsKey("Parameters"))
                            {
                                var parameters = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(constructor["Parameters"], Formatting.Indented));
                                if (parameters != null && parameters.Count > 0)
                                {
                                    foreach (var parameter in parameters)
                                    {
                                        string name = parameter.ContainsKey("Text") ? parameter["Text"].ToString() : "Unknown";
                                        string type = parameter.ContainsKey("Type") ? parameter["Type"].ToString() : "Unknown";
                                        forAdd += type + " " + name + ",";
                                    }
                                    forAdd = forAdd.Substring(0, forAdd.Length - 1);
                                }
                            }
                            forAdd += " )";
                            if (baseVariables != null && baseVariables.Count > 0)
                            {
                                Debug.Log("dostal som sa sem");
                                int counter = 0;
                                forAdd += " : base (";
                                foreach (string variable in baseVariables)
                                {
                                    counter++;
                                    if (counter == 1) { forAdd += variable; }
                                    else { forAdd += ", " + variable; }
                                }
                                forAdd += ")";
                            }
                            class_Object.methods.Add(forAdd);
                            class_Object.methodCommands.Add(forAdd, new List<string>());
                            class_Object.commandKeys.Add(forAdd, new Dictionary<int, string>());
                            class_Object.commandEdges.Add(forAdd, new Dictionary<int, Dictionary<int, string>>());
                            Debug.Log("som konstr?");
                            if (constructor.ContainsKey("Commands"))
                            {
                                var commands = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(constructor["Commands"], Formatting.Indented));

                                class_Object.commandKeys[forAdd].Add(1, "start");
                                class_Object.commandKeys[forAdd].Add(0, "end");

                                class_Object.commandEdges[forAdd].Add(1, new Dictionary<int, string>());
                                class_Object.commandEdges[forAdd].Add(0, new Dictionary<int, string>());
                                if (commands != null && commands.Count > 0)
                                {
                                    highestKey = 1;
                                    uroven = 1;
                                    urovne.Clear();
                                    last_if_else_bodyKeys.Clear();
                                    class_Object.commandEdges[forAdd][1].Add(highestKey + 1, "normal");                                    
                                    parseCommands(commands, class_Object, forAdd);
                                    Debug.Log(string.Join(", ", urovne.Select(kvp => $"{class_Object.commandKeys[forAdd][kvp.Key]}: {kvp.Value}")));
                                    //doeriesenie false hran ak target neexistuje priradi sa mu end, popripade ak nema nic priradene prida sa mu end
                                    Dictionary<int, int> forChange = new Dictionary<int, int>();

                                    foreach (var command in class_Object.commandEdges[forAdd])
                                    {
                                        if (command.Key != 0)
                                        {
                                            if (command.Value.Count == 0)
                                            {
                                                class_Object.commandEdges[forAdd][command.Key].Add(0, "normal");
                                            }
                                            else
                                            {
                                                foreach (var commandEdge in command.Value)
                                                {
                                                    if (!class_Object.commandKeys[forAdd].ContainsKey(commandEdge.Key))
                                                    {
                                                        forChange.Add(command.Key, commandEdge.Key);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    foreach (var remove in forChange)
                                    {
                                        class_Object.commandEdges[forAdd][remove.Key].Remove(remove.Value);
                                        class_Object.commandEdges[forAdd][remove.Key].Add(0, "normal");
                                    }

                                    List<int> listKlucovUrovni = urovne.Keys.ToList();
                                    for(int i = 0;i< listKlucovUrovni.Count; i++)
                                    {
                                        if (last_if_else_bodyKeys.Contains(listKlucovUrovni[i]))
                                        {
                                            List<int> hrany = class_Object.commandEdges[forAdd][listKlucovUrovni[i]].Keys.ToList();
                                            Debug.Log("Dlzka ifelsebody key " + listKlucovUrovni[i] + " " + class_Object.commandKeys[forAdd][listKlucovUrovni[i]] + " " + hrany.Count);
                                            foreach(var hrana in hrany)
                                            {
                                                if (class_Object.commandKeys[forAdd][hrana].Equals("else") && urovne.ContainsKey(hrana))
                                                {
                                                    Debug.Log("Idem zmenit kluc");
                                                    int ReplaceKey = 0;
                                                    for(int j = i + 1; j < listKlucovUrovni.Count; j++)
                                                    {
                                                        if (urovne[hrana] - 1 == urovne[listKlucovUrovni[j]])
                                                        {
                                                            ReplaceKey = listKlucovUrovni[j];                                                            
                                                            break;
                                                        }
                                                    }
                                                    class_Object.commandEdges[forAdd][listKlucovUrovni[i]].Remove(hrana);
                                                    class_Object.commandEdges[forAdd][listKlucovUrovni[i]].Add(ReplaceKey, "normal");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"No constructors found for class: {className}");
                    }
                }

                //Add methods
                if (claz.ContainsKey("Methods"))
                {
                    var methods = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(claz["Methods"], Formatting.Indented));
                    if (methods != null && methods.Count > 0)
                    {
                        foreach (var method in methods)
                        {
                            string visibility = method.ContainsKey("Visibility") && method["Visibility"]?.ToString() != "Unknown" ? method["Visibility"].ToString() : "Unknown";
                            string returnType = method.ContainsKey("ReturnType") ? method["ReturnType"].ToString() : "Unknown";
                            string name = method.ContainsKey("Name") ? method["Name"].ToString() : "Unknown";
                            bool isOveride = method.ContainsKey("IsOverride") ? (bool)method["IsOverride"] : false;
                            bool abstrakt = method.ContainsKey("IsAbstract") ? (bool)method["IsAbstract"] : false;
                            bool virtualn = method.ContainsKey("IsVirtual") ? (bool)method["IsVirtual"] : false;
                            bool isStatic = method.ContainsKey("IsStatic") ? (bool)method["IsStatic"] : false;
                            string forAdd = "";
                            if (!visibility.Equals("Unknown")) { forAdd += visibility + " "; }
                            if (isStatic) { forAdd += "static "; }
                            if (virtualn) { forAdd += "virtual "; }
                            if (abstrakt) { forAdd += "abstract "; }
                            if (isOveride) { forAdd += "override "; }
                            forAdd += returnType + " " + name + "(";
                            //Include parameters
                            if (method.ContainsKey("Parameters"))
                            {
                                var parameters = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(method["Parameters"], Formatting.Indented));
                                if (parameters != null && parameters.Count > 0)
                                {
                                    foreach (var parameter in parameters)
                                    {
                                        string nameParameter = parameter.ContainsKey("Text") ? parameter["Text"].ToString() : "Unknown";
                                        string type = parameter.ContainsKey("Type") ? parameter["Type"].ToString() : "Unknown";
                                        forAdd += type + " " + nameParameter + ",";
                                    }
                                    forAdd = forAdd.Substring(0, forAdd.Length - 1);
                                }
                            }
                            forAdd += ")";
                            class_Object.methods.Add(forAdd);
                            class_Object.methodCommands.Add(forAdd, new List<string>());
                            class_Object.commandKeys.Add(forAdd, new Dictionary<int, string>());
                            class_Object.commandEdges.Add(forAdd, new Dictionary<int, Dictionary<int, string>>());
                            //include commands
                            if (method.ContainsKey("Commands"))
                            {
                                var commands = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(method["Commands"], Formatting.Indented));

                                class_Object.commandKeys[forAdd].Add(1, "start");
                                class_Object.commandKeys[forAdd].Add(0, "end");

                                class_Object.commandEdges[forAdd].Add(1, new Dictionary<int, string>());
                                class_Object.commandEdges[forAdd].Add(0, new Dictionary<int, string>());

                                if (commands != null && commands.Count > 0)
                                {
                                    highestKey = 1;
                                    uroven = 1;
                                    urovne.Clear();
                                    class_Object.commandEdges[forAdd][1].Add(highestKey + 1, "normal");
                                    last_if_else_bodyKeys.Clear();
                                    parseCommands(commands, class_Object, forAdd);
                                    Debug.Log(string.Join(", ", urovne.Select(kvp => $"{class_Object.commandKeys[forAdd][kvp.Key]}: {kvp.Value}")));
                                    //doeriesenie false hran ak target neexistuje priradi sa mu end, popripade ak nema nic priradene prida sa mu end
                                    Dictionary<int, int> forChange = new Dictionary<int, int>();

                                    foreach (var command in class_Object.commandEdges[forAdd])
                                    {
                                        if (command.Key != 0)
                                        {
                                            if (command.Value.Count == 0)
                                            {
                                                class_Object.commandEdges[forAdd][command.Key].Add(0, "normal");
                                            }
                                            else
                                            {
                                                foreach (var commandEdge in command.Value)
                                                {
                                                    if (!class_Object.commandKeys[forAdd].ContainsKey(commandEdge.Key))
                                                    {
                                                        forChange.Add(command.Key, commandEdge.Key);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    foreach (var remove in forChange)
                                    {
                                        class_Object.commandEdges[forAdd][remove.Key].Remove(remove.Value);
                                        class_Object.commandEdges[forAdd][remove.Key].Add(0, "normal");
                                    }
                                    List<int> listKlucovUrovni = urovne.Keys.ToList();
                                    for (int i = 0; i < listKlucovUrovni.Count; i++)
                                    {
                                        if (last_if_else_bodyKeys.Contains(listKlucovUrovni[i]))
                                        {
                                            List<int> hrany = class_Object.commandEdges[forAdd][listKlucovUrovni[i]].Keys.ToList();
                                            Debug.Log("Dlzka ifelsebody key " + listKlucovUrovni[i] + " " + class_Object.commandKeys[forAdd][listKlucovUrovni[i]] + " " + hrany.Count);
                                            foreach (var hrana in hrany)
                                            {
                                                if (class_Object.commandKeys[forAdd][hrana].Equals("else") && urovne.ContainsKey(hrana))
                                                {
                                                    Debug.Log("Idem zmenit kluc");
                                                    int ReplaceKey = 0;
                                                    for (int j = i + 1; j < listKlucovUrovni.Count; j++)
                                                    {
                                                        if (urovne[hrana] - 1 == urovne[listKlucovUrovni[j]])
                                                        {
                                                            ReplaceKey = listKlucovUrovni[j];
                                                            break;
                                                        }
                                                    }
                                                    class_Object.commandEdges[forAdd][listKlucovUrovni[i]].Remove(hrana);
                                                    class_Object.commandEdges[forAdd][listKlucovUrovni[i]].Add(ReplaceKey, "normal");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //AddComponentMenu generalisation or realisation
                if (claz.ContainsKey("Connections"))
                {
                    var connections = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(claz["Connections"], Formatting.Indented));
                    if (connections != null && connections.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> connect in connections)
                        {
                            class_Object.connections.Add(connect.Key, connect.Value);
                        }
                    }
                }
                classDictionary.Add(className, class_Object);
            }            
            foreach (Class_object claz in classDictionary.Values) { classList.Add(claz); }
        }
        Dictionary<string, Class_object> classNodesdict = new Dictionary<string, Class_object>();
        foreach(var claz in classList) { classNodesdict.Add(claz.name, claz);}
        foreach (var claz in classNodesdict)
        {
            foreach (var connect in claz.Value.connections)
            {
                if (classNodesdict.ContainsKey(connect.Key) && classNodesdict[connect.Key].type.Equals("interface"))
                {
                    classNodesdict[claz.Key].connections[connect.Key] = "Realisation";
                }
            }
        }
        //add Aggregation, Composition, Dependency, Association
        //iterate again over clases
        foreach (var claz in allData)
        {
            string className = claz["Name"].ToString();
            //add Aggregation, Composition, Association
            if (claz.ContainsKey("Attributes"))
            {
                var attributes = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(claz["Attributes"], Formatting.Indented));
                //iterate over attributes
                if (attributes != null && attributes.Count > 0)
                {
                    foreach (var attribute in attributes)
                    {
                        string name = attribute.ContainsKey("Name") ? attribute["Name"].ToString() : "Unknown";
                        string type = attribute.ContainsKey("Type") ? attribute["Type"].ToString() : "Unknown";
                        string defaultValue = attribute.ContainsKey("DefaultValue") && attribute["DefaultValue"] != null ? attribute["DefaultValue"].ToString() : "None";
                        bool isConnection = false;
                        string targetclassName = "";
                        //We found another class Declaration
                        foreach (string meno in classNodesdict.Keys) { if (type.Contains(meno)) { isConnection = true; targetclassName = meno; } }
                        if (isConnection && !classNodesdict[className].connections.ContainsKey(targetclassName))
                        {
                            if (!defaultValue.Equals("None"))
                            {
                                if (defaultValue.Contains("new"))
                                {
                                    classNodesdict[className].connections.Add(targetclassName, "Composition");
                                }
                                else
                                {
                                    classNodesdict[className].connections.Add(targetclassName, "Aggregation");
                                }
                            }
                            else
                            {
                                //We didn't find initialisation so must search in class method commands
                                bool hasConnection = false;
                                foreach (KeyValuePair<string, List<string>> method in classNodesdict[className].methodCommands)
                                {
                                    foreach (string command in method.Value)
                                    {
                                        string[] lines = command.Split("/n");
                                        foreach (string line in lines)
                                        {
                                            //nasiel som premennu typu targetClass ktorej chybala inicializacia
                                            if (line.Contains(name))
                                            {
                                                //Ma inicializaciu
                                                if (line.Contains("="))
                                                {
                                                    hasConnection = true;
                                                    if (line.Contains("new"))
                                                    {
                                                        classNodesdict[className].connections.Add(targetclassName, "Composition");
                                                    }
                                                    else
                                                    {
                                                        classNodesdict[className].connections.Add(targetclassName, "Aggregation");
                                                    }
                                                }
                                            }
                                            if (hasConnection) { break; }
                                        }
                                        if (hasConnection) { break; }
                                    }
                                    if (hasConnection) { break; }
                                }
                                if (!classNodesdict[className].connections.ContainsKey(targetclassName)) { classNodesdict[className].connections.Add(targetclassName, "Aggregation"); }
                            }
                        }
                    }
                }
            }
            //vbn ten isty scenar s properties akurat
            if (claz.ContainsKey("Properties"))
            {
                var properties = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(claz["Properties"], Formatting.Indented));
                if (properties != null && properties.Count > 0)
                {
                    foreach (var property in properties)
                    {
                        string name = property.ContainsKey("Name") ? property["Name"].ToString() : "Unknown";
                        string type = property.ContainsKey("Type") ? property["Type"].ToString() : "Unknown";
                        string defaultValue = property.ContainsKey("DefaultValue") && property["DefaultValue"] != null ? property["DefaultValue"].ToString() : "None";
                        bool isConnection = false;
                        string targetclassName = "";
                        foreach (string meno in classNodesdict.Keys) { if (type.Contains(meno)) { isConnection = true; targetclassName = meno; } }
                        if (isConnection && !classNodesdict[className].connections.ContainsKey(targetclassName))
                        {
                            if (!defaultValue.Equals("None"))
                            {
                                if (defaultValue.Contains("new"))
                                {
                                    classNodesdict[className].connections.Add(targetclassName, "Composition");
                                }
                                else
                                {
                                    classNodesdict[className].connections.Add(targetclassName, "Aggregation");
                                }
                            }
                            else
                            {
                                bool hasConnection = false;
                                foreach (KeyValuePair<string, List<string>> method in classNodesdict[className].methodCommands)
                                {
                                    foreach (string command in method.Value)
                                    {
                                        string[] lines = command.Split("/n");
                                        foreach (string line in lines)
                                        {
                                            if (line.Contains(name))
                                            {
                                                if (line.Contains("="))
                                                {
                                                    hasConnection = true;
                                                    if (line.Contains("new"))
                                                    {
                                                        classNodesdict[className].connections.Add(targetclassName, "Composition");
                                                    }
                                                    else
                                                    {
                                                        classNodesdict[className].connections.Add(targetclassName, "Aggregation");
                                                    }
                                                }
                                            }
                                            if (hasConnection) { break; }
                                        }
                                        if (hasConnection) { break; }
                                    }
                                    if (hasConnection) { break; }
                                }
                                if (!classNodesdict[className].connections.ContainsKey(targetclassName)) { classNodesdict[className].connections.Add(targetclassName, "Aggregation"); }
                            }
                        }
                    }
                }
            }
            //vbn add dependency, Association
            //iterate over just methods in class
            foreach (KeyValuePair<string, List<string>> method in classNodesdict[className].methodCommands)
            {
                foreach (string command in method.Value)
                {
                    string[] lines = command.Split("/n");
                    foreach (string line in lines)
                    {
                        string targetClasName = " ";
                        foreach (string meno in classNodesdict.Keys) { if (line.Contains(meno) && !line.Contains("if")) { targetClasName = meno; break; } }
                        //another clas declaration was found
                        if (!targetClasName.Equals(" ") && !classNodesdict[className].connections.ContainsKey(targetClasName))
                        {
                            if (line.Contains("="))
                            {
                                //Hash clas inicialisation
                                classNodesdict[className].connections.Add(targetClasName, "Dependency");
                            }
                            else
                            {
                                classNodesdict[className].connections.Add(targetClasName, "Asociation");
                            }
                        }
                    }
                }
            }
        }
        classList = new List<Class_object>();
        foreach (var node in classNodesdict) { classList.Add(node.Value);}
        return classList;
    }

    private void parseCommands(List<Dictionary<string, object>> commands, Class_object class_Object, string forAdd)
    {
        foreach (var command in commands)
        {
            string typeOfComand = command.ContainsKey("Type") ? command["Type"].ToString() : "Unknown";
            if (typeOfComand.Equals("ForLoop"))
            {
                int conditionKey = -1;
                string initialisation = command.ContainsKey("Initialization") ? command["Initialization"].ToString() : "Unknown";
                string condition = command.ContainsKey("Condition") ? command["Condition"].ToString() : "Unknown";
                var incrementors = command.ContainsKey("Incrementors") && command["Incrementors"] is IEnumerable<object> incr
                    ? incr.Select(x => x.ToString()).ToList()
                    : new List<string>();

                // Handle initialization
                if (!initialisation.Equals("Unknown") && initialisation.Length > 0 && !condition.Equals("Unknown") && condition.Length > 0 && incrementors.Count > 0)
                {
                    conditionKey = highestKey + 2;

                    class_Object.methodCommands[forAdd].Add(initialisation);
                    class_Object.methodCommands[forAdd].Add(condition);

                    highestKey += 2;
                    class_Object.commandKeys[forAdd].Add(highestKey - 1, initialisation);
                    class_Object.commandKeys[forAdd].Add(highestKey, condition);

                    class_Object.commandEdges[forAdd].Add(highestKey - 1, new Dictionary<int, string>());
                    class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());

                    class_Object.commandEdges[forAdd][highestKey - 1].Add(highestKey, "normal");
                    class_Object.commandEdges[forAdd][highestKey].Add(highestKey + 1, "normal");

                    urovne.Add(highestKey - 1, uroven);
                    urovne.Add(highestKey, uroven);

                    // Handle body
                    if (command.ContainsKey("Body"))
                    {
                        var body = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            JsonConvert.SerializeObject(command["Body"], Formatting.Indented));
                        if (body != null && body.Count > 0)
                        {
                            uroven += 1;
                            parseCommands(body, class_Object, forAdd);
                            uroven -= 1;
                        }
                    }
                    // Handle incrementors                    
                    string increase = string.Join(" && ", incrementors);

                    Debug.Log(increase);
                    class_Object.methodCommands[forAdd].Add(increase);

                    highestKey++;
                    class_Object.commandKeys[forAdd].Add(highestKey, increase);
                    class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());
                    class_Object.commandEdges[forAdd][highestKey].Add(conditionKey, "normal");

                    urovne.Add(highestKey, uroven);

                    //pridam spoj prec z foru ked podmienka nie je splnena
                    class_Object.commandEdges[forAdd][conditionKey].Add(highestKey + 1, "normal");
                }
            }
            else if (typeOfComand.Equals("WhileLoop"))
            {
                int conditionKey = -1;

                string condition = command.ContainsKey("Condition") ? command["Condition"].ToString() : "Unknown";

                // Handle condition
                if (!condition.Equals("Unknown") && condition.Length > 0)
                {
                    conditionKey = highestKey + 1;

                    class_Object.methodCommands[forAdd].Add(condition);

                    highestKey++;

                    class_Object.commandKeys[forAdd].Add(highestKey, condition);

                    class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());
                    class_Object.commandEdges[forAdd][highestKey].Add(highestKey + 1, "normal");

                    urovne.Add(highestKey, uroven);

                    // Handle body
                    if (command.ContainsKey("Body"))
                    {
                        var body = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            JsonConvert.SerializeObject(command["Body"], Formatting.Indented));

                        if (body != null && body.Count > 0)
                        {
                            uroven += 1;
                            parseCommands(body, class_Object, forAdd);
                            uroven -= 1;
                        }
                    }
                    if (highestKey != conditionKey)
                    {
                        int loopKey = conditionKey;

                        class_Object.commandEdges[forAdd][conditionKey].Add(highestKey + 1, "normal");

                        if(urovne[highestKey]-1 == urovne[conditionKey] && !class_Object.commandKeys[forAdd][highestKey].Equals("else"))
                        {
                            loopKey = highestKey;
                            Debug.Log(forAdd + " ma loop key");
                        }
                        class_Object.commandEdges[forAdd][highestKey].Remove(highestKey + 1);
                        class_Object.commandEdges[forAdd][highestKey].Add(conditionKey, "normal");
                        Dictionary<int,int> forEdit = new Dictionary<int, int>();
                        for(int i = conditionKey; i <= highestKey;i++)
                        {
                            if (urovne.ContainsKey(i) && last_if_else_bodyKeys.Contains(i))
                            {
                                foreach(var hrana in class_Object.commandEdges[forAdd][i])
                                {
                                    if (class_Object.commandKeys[forAdd].ContainsKey(hrana.Key) && class_Object.commandKeys[forAdd][hrana.Key].Equals("else"))
                                    {
                                        forEdit.Add(i,hrana.Key);                                        
                                    } else if (!class_Object.commandKeys[forAdd].ContainsKey(hrana.Key))
                                    {
                                        forEdit.Add(i, hrana.Key);                                        
                                    }
                                }
                            }
                        }
                        foreach(KeyValuePair<int,int> removed in forEdit)
                        {
                            last_if_else_bodyKeys.Remove(removed.Key);
                            class_Object.commandEdges[forAdd][removed.Key].Remove(removed.Value);
                            class_Object.commandEdges[forAdd][removed.Key].Add(loopKey, "normal");
                        }
                    }
                }
            }
            else if (typeOfComand.Equals("IfCondition"))
            {
                int ifConditionKey = -1;
                bool containIfBody = false;
                bool containElse = false;
                bool containElseBody = false;
                string condition = command.ContainsKey("Condition") ? command["Condition"].ToString() : "Unknown";
                if (!condition.Equals("Unknown") && condition.Length > 0)
                {
                    ifConditionKey = highestKey + 1;

                    class_Object.methodCommands[forAdd].Add(condition);

                    highestKey++;

                    class_Object.commandKeys[forAdd].Add(highestKey, condition);

                    class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());
                    class_Object.commandEdges[forAdd][highestKey].Add(highestKey + 1, "normal");

                    urovne.Add(highestKey, uroven);

                    //check construction of if condition what else ThreadStaticAttribute contains
                    if (command.ContainsKey("Body"))
                    {
                        var body = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                           JsonConvert.SerializeObject(command["Body"], Formatting.Indented));
                        if (body != null && body.Count > 0) { containIfBody = true; }
                    }
                    if (command.ContainsKey("ElseBody") && command["ElseBody"] is not null)
                    {
                        containElse = true;
                        var elseBody = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(command["ElseBody"], Formatting.Indented));
                        if (elseBody != null && elseBody.Count > 0) { containElseBody = true; }
                    }
                    //case classic construction if ifBody else containElseBody
                    if (containIfBody && containElse && containElseBody)
                    {
                        int ifBodyKey = -1;
                        int elseKey = -1;
                        int elseBodyKey = -1;

                        //workifbody
                        var body = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            JsonConvert.SerializeObject(command["Body"], Formatting.Indented));
                        uroven += 1;
                        parseCommands(body, class_Object, forAdd);
                        uroven -= 1;
                        ifBodyKey = highestKey;
                        last_if_else_bodyKeys.Add(ifBodyKey);

                        //workelseCondition
                        highestKey++;
                        elseKey = highestKey;
                        class_Object.methodCommands[forAdd].Add("else");

                        class_Object.commandKeys[forAdd].Add(highestKey, "else");

                        class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());
                        class_Object.commandEdges[forAdd][highestKey].Add(highestKey + 1, "normal");

                        urovne.Add(highestKey, uroven);

                        class_Object.commandEdges[forAdd][ifConditionKey].Add(elseKey, "normal");
                        if (class_Object.commandEdges[forAdd].ContainsKey(ifBodyKey) && class_Object.commandEdges[forAdd][ifBodyKey].ContainsKey(elseKey)) { class_Object.commandEdges[forAdd][ifBodyKey].Remove(elseKey); }

                        //workElseBody
                        var elseBody = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(command["ElseBody"], Formatting.Indented));
                        uroven += 1;
                        parseCommands(elseBody, class_Object, forAdd);
                        uroven -= 1;
                        elseBodyKey = highestKey;
                        last_if_else_bodyKeys.Add(elseBodyKey);
                        class_Object.commandEdges[forAdd][ifBodyKey].Add(elseBodyKey + 1, "normal");
                    }
                    //case classic construction if ifBody else !containElseBody
                    if (containIfBody && containElse && !containElseBody)
                    {
                        int ifBodyKey = -1;
                        int elseKey = -1;

                        //workifbody
                        var body = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            JsonConvert.SerializeObject(command["Body"], Formatting.Indented));
                        uroven += 1;
                        parseCommands(body, class_Object, forAdd);
                        uroven -= 1;
                        ifBodyKey = highestKey;
                        last_if_else_bodyKeys.Add(ifBodyKey);

                        //workelseCondition
                        highestKey++;
                        urovne.Add(highestKey, uroven);
                        elseKey = highestKey;
                        last_if_else_bodyKeys.Add(elseKey);
                        class_Object.methodCommands[forAdd].Add("else");

                        class_Object.commandKeys[forAdd].Add(highestKey, "else");

                        class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());
                        class_Object.commandEdges[forAdd][highestKey].Add(highestKey + 1, "normal");
                        class_Object.commandEdges[forAdd][ifConditionKey].Add(elseKey, "normal");
                        if (class_Object.commandEdges[forAdd].ContainsKey(ifBodyKey) && class_Object.commandEdges[forAdd][ifBodyKey].ContainsKey(elseKey)) { class_Object.commandEdges[forAdd][ifBodyKey].Remove(elseKey); }

                        class_Object.commandEdges[forAdd][ifBodyKey].Add(elseKey + 1, "normal");
                    }
                    //case classic construction if ifBody else containElseBody
                    if (containIfBody && !containElse && !containElseBody)
                    {
                        last_if_else_bodyKeys.Add(ifConditionKey);
                        int ifBodyKey = -1;
                        //workifbody
                        var body = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            JsonConvert.SerializeObject(command["Body"], Formatting.Indented));
                        uroven += 1;
                        parseCommands(body, class_Object, forAdd);
                        uroven -= 1;
                        ifBodyKey = highestKey;
                        last_if_else_bodyKeys.Add(ifBodyKey);
                        class_Object.commandEdges[forAdd][ifConditionKey].Add(ifBodyKey + 1, "normal");
                    }
                    //case classic construction if ifBody else containElseBody
                    if (!containIfBody && containElse && containElseBody)
                    {
                        int elseKey = -1;
                        int elseBodyKey = -1;

                        //workelseCondition
                        highestKey++;
                        urovne.Add(highestKey, uroven);
                        elseKey = highestKey;
                        class_Object.methodCommands[forAdd].Add("else");

                        class_Object.commandKeys[forAdd].Add(highestKey, "else");

                        class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());
                        class_Object.commandEdges[forAdd][highestKey].Add(highestKey + 1, "normal");

                        //workElseBody
                        var elseBody = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(command["ElseBody"], Formatting.Indented));
                        uroven -= 1;
                        parseCommands(elseBody, class_Object, forAdd);
                        uroven += 1;
                        elseBodyKey = highestKey;
                        last_if_else_bodyKeys.Add(elseBodyKey);

                        class_Object.commandEdges[forAdd][ifConditionKey].Add(elseBodyKey + 1, "normal");
                    }
                }
                //case classic construction if ifBody else containElseBody
                if (!containIfBody && containElse && !containElseBody)
                {
                    int elseKey = -1;
                    //workelseCondition
                    highestKey++;
                    urovne.Add(highestKey, uroven);
                    elseKey = highestKey;
                    last_if_else_bodyKeys.Add(elseKey);
                    class_Object.methodCommands[forAdd].Add("else");
                    class_Object.commandKeys[forAdd].Add(highestKey, "else");
                    class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());
                    class_Object.commandEdges[forAdd][highestKey].Add(highestKey + 1, "normal");

                    class_Object.commandEdges[forAdd][ifConditionKey].Add(elseKey + 1, "normal");
                }
            }
            else if (typeOfComand.Equals("Statement"))
            {
                string line = command.ContainsKey("Code") ? command["Code"].ToString() : "Unknown";
                if (!line.Equals("Unknown") && line.Length > 0)
                {
                    class_Object.methodCommands[forAdd].Add(line);

                    highestKey++;

                    urovne.Add(highestKey, uroven);

                    class_Object.commandKeys[forAdd].Add(highestKey, line);

                    class_Object.commandEdges[forAdd].Add(highestKey, new Dictionary<int, string>());
                    class_Object.commandEdges[forAdd][highestKey].Add(highestKey + 1, "normal");
                }
            }
        }
    }

    private static object ExtractClassOrInterface(TypeDeclarationSyntax declaration, string type)
    {
        return new
        {
            Name = declaration.Identifier.Text,
            Type = type,
            Visibility = GetVisibility(declaration.Modifiers), // Extract visibility for the class/interface
            IsAbstract = declaration.Modifiers.Any(SyntaxKind.AbstractKeyword), // Check if the class is abstract
            IsVirtual = false,  // Classes themselves can't be virtual, so this is false by default
            Attributes = declaration.Members.OfType<FieldDeclarationSyntax>().Select(f => ExtractField(f)).ToList(),
            Properties = declaration.Members.OfType<PropertyDeclarationSyntax>().Select(p => ExtractProperty(p)).ToList(),
            Constructors = declaration.Members.OfType<ConstructorDeclarationSyntax>().Select(c => ExtractConstructor(c)).ToList(),
            Methods = declaration.Members.OfType<MethodDeclarationSyntax>().Select(m => ExtractMethod(m)).ToList(),
            Connections = declaration.BaseList != null ? ExtractConnections(declaration.BaseList) : new Dictionary<string, string>()
        };
    }

    // Extract attributes (fields)
    private static object ExtractField(FieldDeclarationSyntax field)
    {
        var variable = field.Declaration.Variables.FirstOrDefault();
        return new
        {
            Name = variable?.Identifier.Text,
            Type = field.Declaration.Type.ToString(),
            Visibility = GetVisibility(field.Modifiers),
            DefaultValue = variable?.Initializer?.Value.ToString() // Extract default value if exists
        };
    }

    // Extract properties
    private static object ExtractProperty(PropertyDeclarationSyntax property)
    {
        return new
        {
            Name = property.Identifier.Text,
            Type = property.Type.ToString(),
            Visibility = GetVisibility(property.Modifiers),
            HasGetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false,
            HasSetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false,
            DefaultValue = property.Initializer?.Value.ToString() // Extract default value if exists
        };
    }

    private static object ExtractMethod(MethodDeclarationSyntax method)
    {
        return new
        {
            Name = method.Identifier.Text,
            Visibility = GetVisibility(method.Modifiers),
            ReturnType = method.ReturnType.ToString(),
            Parameters = method.ParameterList.Parameters.Select(p => new { p.Identifier.Text, Type = p.Type.ToString() }).ToList(),
            Commands = ExtractCommands(method.Body), // Extract detailed commands
            IsAbstract = method.Modifiers.Any(SyntaxKind.AbstractKeyword),  // Check if method is abstract
            IsVirtual = method.Modifiers.Any(SyntaxKind.VirtualKeyword),  // Check if method is virtual
            IsOverride = method.Modifiers.Any(SyntaxKind.OverrideKeyword), // Check if method is override
            IsStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword)       // Check if method is static
        };
    }


    private static List<object> ExtractCommands(SyntaxNode node)
    {
        var commands = new List<object>();

        if (node is BlockSyntax block)
        {
            // Iterate through all statements in a block
            foreach (var statement in block.Statements)
            {
                commands.AddRange(ExtractCommands(statement));
            }
        }
        else if (node is ForStatementSyntax forStatement)
        {
            // Handle a for-loop
            commands.Add(new
            {
                Type = "ForLoop",
                Initialization = forStatement.Declaration?.ToString(),
                Condition = forStatement.Condition?.ToString(),
                Incrementors = forStatement.Incrementors.Select(i => i.ToString()).ToList(),
                Body = ExtractCommands(forStatement.Statement)
            });
        }
        else if (node is IfStatementSyntax ifStatement)
        {
            // Handle an if-condition
            commands.Add(new
            {
                Type = "IfCondition",
                Condition = ifStatement.Condition.ToString(),
                Body = ExtractCommands(ifStatement.Statement),
                ElseBody = ifStatement.Else != null ? ExtractCommands(ifStatement.Else.Statement) : null
            });
        }
        else if (node is WhileStatementSyntax whileStatement)
        {
            // Handle a while-loop
            commands.Add(new
            {
                Type = "WhileLoop",
                Condition = whileStatement.Condition.ToString(),
                Body = ExtractCommands(whileStatement.Statement)
            });
        }
        else
        {
            // Default case: Treat as a simple statement
            commands.Add(new
            {
                Type = "Statement",
                Code = node.ToString()
            });
        }

        return commands;
    }

    private static object ExtractConstructor(ConstructorDeclarationSyntax constructor)
    {
        return new
        {
            Name = constructor.Identifier.Text,
            Visibility = GetVisibility(constructor.Modifiers),
            Parameters = constructor.ParameterList.Parameters.Select(p => new { p.Identifier.Text, Type = p.Type.ToString() }).ToList(),
            Commands = ExtractCommands(constructor.Body), // Extract detailed commands
            IsAbstract = constructor.Modifiers.Any(SyntaxKind.AbstractKeyword),  // Check if constructor is abstract
            IsVirtual = constructor.Modifiers.Any(SyntaxKind.VirtualKeyword),  // Check if constructor is virtual
            BaseCall = constructor.Initializer != null && constructor.Initializer.IsKind(SyntaxKind.BaseConstructorInitializer)
                ? constructor.Initializer.ArgumentList.Arguments.Select(arg => arg.ToString()).ToList()
                : null,
            ThisCall = constructor.Initializer != null && constructor.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer)
                ? constructor.Initializer.ArgumentList.Arguments.Select(arg => arg.ToString()).ToList()
                : null
        };
    }


    private static Dictionary<string, string> ExtractConnections(BaseListSyntax baseList)
    {
        var connections = new Dictionary<string, string>();

        foreach (var baseType in baseList.Types)
        {
            connections[baseType.Type.ToString()] = "Generalisation"; // MÙûeö pridaù rozliöovanie
        }

        return connections;
    }

    private static string GetVisibility(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword)) return "public";
        if (modifiers.Any(SyntaxKind.ProtectedKeyword)) return "protected";
        if (modifiers.Any(SyntaxKind.PrivateKeyword)) return "private";
        return "Unknown";
    }

}
