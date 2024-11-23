using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System;
using System.Linq;

public class ActivityReader
{
    List<string> commands = new List<string>();
    List<string> parsedCommands = new List<string>(); // This list stores the processed commands
    public ActivityReader(List<string> commands)
    {
        this.commands = commands;
    }

    public List<string> parseCommandsToActions()
    {        

        foreach (string command in commands)
        {
            if (command.Split("\n").Length > 1)
            {
                Debug.Log("som tu");
                // Parse multi-line command using Roslyn
                SyntaxTree tree = CSharpSyntaxTree.ParseText(command);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
                var forParse = root.DescendantNodes().FirstOrDefault();
                if(forParse!= null)
                {
                    Debug.Log("Spracuvam prikaz: " + command);
                    Debug.Log("dostal som sem");
                    identify(forParse);
                }
            }
            else
            {
                // Single-line commands are added directly
                parsedCommands.Add(command);
            }
        }

        return parsedCommands; // Correctly return the processed list
    }    

    public void identify(SyntaxNode node)
    {
        if (node is ForStatementSyntax)
        {
            handleFor((ForStatementSyntax)node);
            Debug.Log("som for cyklus");
        }
        else if (node is IfStatementSyntax)
        {
            handleIf((IfStatementSyntax)node);
            Debug.Log("som if podmienka");
        }
        else if (node is WhileStatementSyntax)
        {
            handleWhile((WhileStatementSyntax)node);
            Debug.Log("som while cyklus");
        }
        else
        {
            // Default case: add the node as a command
            parsedCommands.Add(node.ToString().Trim());
            Debug.Log("som prikaz");
        }
    }

    private void handleFor(ForStatementSyntax node)
    {
        // Add the for-loop declaration to parsed commands
        parsedCommands.Add("for (" + node.Declaration?.ToString() + "; " +
                           node.Condition?.ToString() + "; " +
                           string.Join(", ", node.Incrementors) + ")");
        parsedCommands.Add(node.Declaration?.ToString());
        parsedCommands.Add(node.Condition?.ToString());
        // Process the body of the for-loop
        if (node.Statement is BlockSyntax block)
        {
            foreach (var statement in block.Statements)
            {
                // Recursively identify and handle each statement inside the block
                identify(statement);
            }
            parsedCommands.Add(node.Incrementors.ToString());
        }
        else
        {
            // If the body is a single statement, process it
            identify(node.Statement);
            parsedCommands.Add(node.Incrementors.ToString());
        }
    }

    private void handleIf(IfStatementSyntax node)
    {
        // Add the if condition to parsed commands
        parsedCommands.Add(node.Condition.ToString());

        // Process the true branch
        if (node.Statement is BlockSyntax block)
        {
            foreach (var statement in block.Statements)
            {
                identify(statement);
            }
        }
        else
        {
            identify(node.Statement);
        }

        // Handle the else branch if it exists
        if (node.Else != null)
        {
            parsedCommands.Add("else");
            if (node.Else.Statement is BlockSyntax elseBlock)
            {
                foreach (var statement in elseBlock.Statements)
                {
                    identify(statement);
                }
            }
            else
            {
                identify(node.Else.Statement);
            }
        }
    }

    private void handleWhile(WhileStatementSyntax node)
    {
        // Add the while-loop condition to parsed commands
        parsedCommands.Add(node.Condition.ToString());

        // Process the body of the while-loop
        if (node.Statement is BlockSyntax block)
        {
            foreach (var statement in block.Statements)
            {
                identify(statement);
            }
        }
        else
        {
            identify(node.Statement);
        }
    }
}
