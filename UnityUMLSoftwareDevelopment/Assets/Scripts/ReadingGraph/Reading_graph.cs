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
    public string path = "";
    public CompilationUnitSyntax root;

    public Reading_graph(string path)
    {
        this.path = path;
    }

    public List<Class_object> read_from_code()
    {
        List<Class_object> classList = new List<Class_object>();
        Dictionary<string, Class_object>  classDictionary = new Dictionary<string, Class_object>();

        // Parsovanie C# kÛdu
        var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
        var root = syntaxTree.GetCompilationUnitRoot();

        // Extract all classes and interfaces
        var classNodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        var interfaceNodes = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();

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

        // Deserialize the JSON into a list of dictionaries
        var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonFormat);                

        //iterate over clases
        foreach (var claz in data)
        {
            // Initialize the Class_object
            string className = claz["Name"].ToString();
            Class_object class_Object = new Class_object(className);
            Debug.Log(claz);
            // Access Attributes if they exist
            if (claz.ContainsKey("Attributes"))
            {
                var attributes = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(claz["Attributes"],Formatting.Indented));
                if (attributes != null && attributes.Count > 0)
                {
                    Debug.Log($"Attributes found for class: {className}");
                    foreach (var attribute in attributes)
                    {
                        string name = attribute.ContainsKey("Name") ? attribute["Name"].ToString() : "Unknown";
                        string type = attribute.ContainsKey("Type") ? attribute["Type"].ToString() : "Unknown";
                        string visibility = attribute.ContainsKey("Visibility") ? attribute["Visibility"].ToString() : "Unknown";
                        string defaultValue = attribute.ContainsKey("DefaultValue") && attribute["DefaultValue"]!=null ? attribute["DefaultValue"].ToString() : "None";
                        string forAdd = " ";
                        if (!visibility.Equals("Unknown")) { forAdd += visibility + " "; }
                        forAdd += type + " " + name;
                        if (!defaultValue.Equals("None")) { forAdd += "= " + defaultValue;}                        
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
                    Debug.Log($"Properties found for class: {className}");
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
                    Debug.Log($"Constructors found for class: {className}");
                    foreach (var constructor in constructors)
                    {
                        string constructorName = constructor.ContainsKey("Name") ? constructor["Name"].ToString() : "Unknown";
                        string visibility = constructor.ContainsKey("Visibility") ? constructor["Visibility"].ToString() : "Unknown";
                        string forAdd = "";
                        if (!visibility.Equals("Unknown")) { forAdd += visibility + " "; }
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
                        forAdd += constructorName + " )";
                        class_Object.methods.Add(forAdd);
                        if (constructor.ContainsKey("Commands")){
                            var commands = JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(constructor["Commands"], Formatting.Indented));
                            if (commands != null && commands.Count > 0)
                            {
                                List<string> commandsList = new List<string>();
                                foreach(var command in commands) { commandsList.Add(command); }
                                class_Object.methodCommands.Add(forAdd, commandsList);
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
                if (methods!=null && methods.Count > 0)
                {
                    Debug.Log($"Methods found for class: {className}");
                    foreach(var method in methods)
                    {
                        string visibility = method.ContainsKey("Visibility") ? method["Visibility"].ToString() : "Unknown";
                        string returnType = method.ContainsKey("ReturnType") ? method["ReturnType"].ToString() : "Unknown";
                        string name = method.ContainsKey("Name") ? method["Name"].ToString() : "Unknown";
                        bool isOveride = method.ContainsKey("IsOverride") ? (bool) method["IsOverride"] : false;
                        string forAdd = "";
                        if (isOveride) { forAdd += "override "; }
                        if (!visibility.Equals("Unknown")) { forAdd += visibility + " "; }
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
                        //include commands
                        if (method.ContainsKey("Commands"))
                        {
                            var commands = JsonConvert.DeserializeObject<List<string>>(JsonConvert.SerializeObject(method["Commands"], Formatting.Indented));
                            if (commands != null && commands.Count > 0)
                            {
                                List<string> commandsList = new List<string>();
                                foreach (var command in commands) { commandsList.Add(command); }
                                class_Object.methodCommands.Add(forAdd, commandsList);
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
                    foreach (KeyValuePair<string,string> connect in connections)
                    {
                        class_Object.connections.Add(connect.Key, connect.Value);
                    }
                }
            }
            classDictionary.Add(className,class_Object);
        }

        //add Aggregation, Composition, Dependency, Association
        //iterate again over clases
        foreach (var claz in data)
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
                        foreach (string meno in classDictionary.Keys) { if (type.Contains(meno)) { isConnection = true; targetclassName = meno; } }                            
                        if (isConnection && !classDictionary[className].connections.ContainsKey(targetclassName))
                        {
                            Debug.Log("Odhalil som " + className + " " + targetclassName);
                            if (!defaultValue.Equals("None")){
                                Debug.Log("Mam set value juhu " + className + " " + targetclassName);
                                if (defaultValue.Contains("new"))
                                {
                                    classDictionary[className].connections.Add(targetclassName, "Composition");
                                } else
                                {
                                    classDictionary[className].connections.Add(targetclassName, "Aggregation");
                                }
                            } else
                            {
                                //We didn't find initialisation so must search in class method commands
                                bool hasConnection = false;
                                Debug.Log("Nemam set value ojoj " + className + " " + targetclassName);
                                foreach (KeyValuePair<string, List<string>> method in classDictionary[className].methodCommands)
                                {
                                    foreach (string command in method.Value)
                                    {
                                        string[] lines = command.Split("\n");                                        
                                        foreach (string line in lines)
                                        {
                                            Debug.Log("riesim comand " + line); 
                                            //nasiel som premennu typu targetClass ktorej chybala inicializacia
                                            if (line.Contains(name))
                                            {
                                                Debug.Log("Bude dodatocne spojenie " + className + " s " + targetclassName);
                                                //Ma inicializaciu
                                                if (line.Contains("="))
                                                {
                                                    hasConnection = true;
                                                    if (line.Contains("new"))
                                                    {
                                                        classDictionary[className].connections.Add(targetclassName, "Composition");
                                                    } else
                                                    {
                                                        classDictionary[className].connections.Add(targetclassName, "Aggregation");
                                                    }                                                    
                                                }
                                            }
                                            if (hasConnection) { break; }
                                        }
                                        if (hasConnection) { break; }
                                    }
                                    if (hasConnection) { break; }
                                }
                                if (!classDictionary[className].connections.ContainsKey(targetclassName)) { classDictionary[className].connections.Add(targetclassName, "Aggregation");}
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
                        foreach (string meno in classDictionary.Keys) { if (type.Contains(meno)) { isConnection = true; targetclassName = meno; } }
                        if (isConnection && !classDictionary[className].connections.ContainsKey(targetclassName))
                        {
                            Debug.Log("Odhalil som " + className + " " + targetclassName);
                            if (!defaultValue.Equals("None"))
                            {
                                Debug.Log("Mam set value juhu " + className + " " + targetclassName);
                                if (defaultValue.Contains("new"))
                                {
                                    classDictionary[className].connections.Add(targetclassName, "Composition");
                                }
                                else
                                {
                                    classDictionary[className].connections.Add(targetclassName, "Aggregation");
                                }
                            }
                            else
                            {
                                bool hasConnection = false;
                                Debug.Log("Nemam set value ojoj " + className + " " + targetclassName);
                                foreach (KeyValuePair<string, List<string>> method in classDictionary[className].methodCommands)
                                {
                                    foreach (string command in method.Value)
                                    {
                                        string[] lines = command.Split("\n");
                                        foreach (string line in lines)
                                        {
                                            Debug.Log("riesim comand " + line);
                                            if (line.Contains(name))
                                            {
                                                Debug.Log("Bude dodatocne spojenie " + className + " s " + targetclassName);
                                                if (line.Contains("="))
                                                {
                                                    hasConnection = true;
                                                    if (line.Contains("new"))
                                                    {
                                                        classDictionary[className].connections.Add(targetclassName, "Composition");
                                                    }
                                                    else
                                                    {
                                                        classDictionary[className].connections.Add(targetclassName, "Aggregation");
                                                    }
                                                }
                                            }
                                            if (hasConnection) { break; }
                                        }
                                        if (hasConnection) { break; }
                                    }
                                    if (hasConnection) { break; }
                                }
                                if (!classDictionary[className].connections.ContainsKey(targetclassName)) { classDictionary[className].connections.Add(targetclassName, "Aggregation"); }
                            }
                        }
                    }
                }                
            }
            //vbn add dependency, Association
            //iterate over just methods in class
            foreach (KeyValuePair<string, List<string>> method in classDictionary[className].methodCommands)
            {
                foreach (string command in method.Value)
                {
                    string[] lines = command.Split("\n");
                    foreach (string line in lines)
                    {
                        string targetClasName = " ";
                        foreach(string meno in classDictionary.Keys) { if (line.Contains(meno) && !line.Contains("if")){ targetClasName = meno;break; }}
                        //another clas declaration was found
                        if(!targetClasName.Equals(" ") && !classDictionary[className].connections.ContainsKey(targetClasName)){
                            if (line.Contains("="))
                            {
                                //Hash clas inicialisation
                                classDictionary[className].connections.Add(targetClasName, "Dependency");
                            } else
                            {
                                classDictionary[className].connections.Add(targetClasName, "Asociation");
                            }
                        }
                    }                    
                }
            }
        }
        //fill list for return. Dictionary was better for ClassObject editation 
        foreach(Class_object claz in classDictionary.Values) { classList.Add(claz); }
        return classList;
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
            Commands = method.Body != null ? method.Body.Statements.Select(s => s.ToString()).ToList() : new List<string>(),
            IsAbstract = method.Modifiers.Any(SyntaxKind.AbstractKeyword),  // Check if method is abstract
            IsVirtual = method.Modifiers.Any(SyntaxKind.VirtualKeyword),  // Check if method is virtual
            IsOverride = method.Modifiers.Any(SyntaxKind.OverrideKeyword)  // Check if method is override
        };
    }

    private static object ExtractConstructor(ConstructorDeclarationSyntax constructor)
    {
        return new
        {
            Name = constructor.Identifier.Text,
            Visibility = GetVisibility(constructor.Modifiers),
            Parameters = constructor.ParameterList.Parameters.Select(p => new { p.Identifier.Text, Type = p.Type.ToString() }).ToList(),
            Commands = constructor.Body != null ? constructor.Body.Statements.Select(s => s.ToString()).ToList() : new List<string>(),
            IsAbstract = constructor.Modifiers.Any(SyntaxKind.AbstractKeyword),  // Check if constructor is abstract
            IsVirtual = constructor.Modifiers.Any(SyntaxKind.VirtualKeyword)  // Check if constructor is virtual
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
        return "internal";
    }
}
