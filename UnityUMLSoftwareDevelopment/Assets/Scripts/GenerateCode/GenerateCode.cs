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
        foreach(Class_object claz in class_Objects)
        {
            output.Add("class " + claz.name + " {");
            foreach(var attribute in claz.attributes)
            {
                output.Add(attribute + ";");
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
        string filePath = "C:/Users/Admin/Desktop/output.txt";
        try
        {
            File.Delete(filePath);
            File.WriteAllLines(filePath, output);
        } catch (Exception ex)
        {
            Console.WriteLine("An error occurred while writting to the file");
        }
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
}
