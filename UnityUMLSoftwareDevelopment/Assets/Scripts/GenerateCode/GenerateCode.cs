using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

public class GenerateCode
{
    public List<Class_object> class_Objects;
    public List<string> output = new List<string>();
    public GenerateCode(List<Class_object> class_Objects)
    {
        this.class_Objects = class_Objects;
    }

    public void generateCode()
    {
        //docasne pridanie namespacu
        output.Add("using System;");
        output.Add("namespace SampleInheritance");
        output.Add("{");
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
            foreach(var attribute in claz.attributes)
            {
                if (attribute.Contains("set;") || attribute.Contains("set;"))
                {
                    output.Add(attribute);
                } else
                {
                    output.Add(attribute + ";");
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
        output.Add("}");
        Debug.Log("Vysledok");
        string filePath = "Assets/SampleOutputs/SampleInheritance.txt";
        try
        {
            File.Delete(filePath);
            File.WriteAllLines(filePath, output);
        } catch (Exception ex)
        {
            Console.WriteLine("An error occurred while writting to the file");
        }
        compileCode(filePath);
    }

    public void generateMethodCode(Class_object class_Object,string method)
    {
        int CommandKey = 1;
        Dictionary<int, int> addEndOfIf = new Dictionary<int, int>();
        foreach (var command in class_Object.methodCommands[method])
        {
            CommandKey++;
            bool isFor = false;            
            foreach(var key in class_Object.commandKeys[method]){if (command.Equals(key.Value)) { CommandKey = key.Key;}}
            foreach(var edge in class_Object.commandEdges[method]) { if (edge.Value.ContainsKey(CommandKey) && edge.Key > CommandKey) { isFor = true; break; } }
            foreach(var endIfElse in class_Object.closeIfElse[method]){if (endIfElse.Value == CommandKey){ output.Add("}"); }}
            if (isFor) 
            { 
                output.Add("while (" + command + ") {");
            }
            else if (class_Object.commandEdges[method][CommandKey].Count > 1)
            {
                //toto moze byt pri prazdnych foroch zradne docela alebo prazdne ify tiez
                output.Add("if (" + command + ") {");
                foreach(var edge in class_Object.commandEdges[method][CommandKey])
                {
                    if (edge.Key > 1 && edge.Key - CommandKey > 1)
                    {
                        addEndOfIf.Add(edge.Key,CommandKey);
                    }
                }
            }
            else if (command.Equals("else"))
            {
                output.Add("} " + command + " {");
            }
            else
            {
                bool isEndOfLoop = false;                
                foreach (var edge in class_Object.commandEdges[method][CommandKey]) { if (CommandKey > edge.Key && edge.Key!=0) { isEndOfLoop = true; } }
                string commandForAdd = command;
                if (!command.Contains(';')) 
                {
                    commandForAdd = command + ";"; 
                }                
                output.Add(commandForAdd);             
                if (isEndOfLoop)
                {
                    output.Add("}");
                }                
            }
        }
        foreach(var claz in class_Objects)
        {
            foreach (var met in claz.commandEdges)
            {
                foreach(var from in class_Object.commandEdges[method])
                {
                    foreach(var to in class_Object.commandEdges[method][from.Key])
                    {
                        Debug.Log(claz.name + " " + met.Key + " " + from.Key + " " + to.Key + " " + to.Value);
                    }
                }
            }
        }
    }

    public void compileCode(string pathToFile)
    {
        string scriptCode = File.ReadAllText(pathToFile);
        var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            "Script Compilation",
            new[] { syntaxTree},
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Length == 0)
        {
            Debug.Log("Program is without beefstake");
        } else
        {
            foreach(var chyba in diagnostics)
            {
                Debug.Log(chyba.ToString());
            }
        }
    }
}
