// <copyright file="Program.cs" company="http://rikker.ru">
// Copyright (c) Rikker Serg 2012 All Right Reserved
// </copyright>
// <author>Rikker Serg</author>
// <email>serg@rikker.ru</email>
// <summary>Simple console app to combine multiple XAML resource dictionaries in one.</summary>
namespace XamlCombine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// Represents simple console app to combine multiple XAML resource dictionaries in one.
    /// Command line syntaxis:
    /// XamlCombine.exe [list-of-xamls.txt] [result-xaml.xaml]
    /// All paths must be relative to XamlCombine.exe location.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Represents XAML resource.
        /// </summary>
        private struct ResourceElement
        {
            /// <summary>
            /// Resource name.
            /// </summary>
            public string Key;

            /// <summary>
            /// Resource XML node.
            /// </summary>
            public XmlElement Element;

            /// <summary>
            /// XAML keys used in this resource.
            /// </summary>
            public string[] UsedKeys;
        }

        /// <summary>
        /// Main function.
        /// </summary>
        /// <param name="args">Command line args.</param>
        private static void Main(string[] args)
        {
            // TODO: Add flags for some parameters.
            if (args.Length == 2)
            {
                Combine(args[0], args[1]);
            }

            // TODO: Add help output.
        }

        /// <summary>
        /// Combines multiple XAML resource dictionaries in one.
        /// </summary>
        /// <param name="sourceFile">Filename of list of XAML's.</param>
        /// <param name="resultFile">Result XAML filename.</param>
        private static void Combine(string sourceFile, string resultFile)
        {
            // Current application path
            // TODO: Need to support all types of paths not only relative.
            string appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // Write to console
            Console.WriteLine("Loading resources list from \"{0}\"", sourceFile);

            if(!File.Exists(sourceFile))
            {
                sourceFile = Path.Combine(appPath, sourceFile);
                if(!File.Exists(sourceFile))
                {
                    Console.WriteLine("Error: File not found.");
                    return;
                }
            }

            // Load resources lists
            string[] resources = File.ReadAllLines(sourceFile);

            
            // Create result XML document
            XmlDocument finalDocument = new XmlDocument();
            XmlElement rootNode = finalDocument.CreateElement("ResourceDictionary", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            finalDocument.AppendChild(rootNode);

            // List of existing keys, to avoid duplicates
            List<string> keys = new List<string>();

            // Associate key with ResourceElement
            Dictionary<string, ResourceElement> resourceElements = new Dictionary<string, ResourceElement>();

            // List of readed resources
            List<ResourceElement> resourcesList = new List<ResourceElement>();

            // For each resource file
            for (int i = 0; i < resources.Length; i++)
            {
                XmlDocument current = new XmlDocument();
                string file = resources[i];
                if(!File.Exists(file))
                {
                    file = Path.Combine(appPath, resources[i]);
                    if(!File.Exists(file))
                    {
                        Console.WriteLine("Error: Resource not found \"{0}\"", resources[0]);
                        return;
                    }
                }
                current.Load(file);

                // Write to console
                Console.WriteLine("Loading resource \"{0}\"", resources[i]);

                // Set and fix resource dictionary attributes
                XmlElement root = current.DocumentElement;
                if (root == null) continue;
                for (int j = 0; j < root.Attributes.Count; j++)
                {
                    XmlAttribute attr = root.Attributes[j];
                    if (rootNode.HasAttribute(attr.Name))
                    {
                        // If namespace with this name exists and not equal
                        if ((attr.Value != rootNode.Attributes[attr.Name].Value) && (attr.Prefix == "xmlns"))
                        {
                            // Create new namespace name
                            int index = 0;
                            string name;
                            do
                            {
                                name = attr.LocalName + "_" + index.ToString(CultureInfo.InvariantCulture);
                            }
                            while (rootNode.HasAttribute("xmlns:" + name));

                            root.SetAttribute("xmlns:" + name, attr.Value);

                            // Change namespace prefixes in resource dictionary
                            ChangeNamespacePrefix(root, attr.LocalName, name);

                            // Add renamed namespace
                            XmlAttribute a = finalDocument.CreateAttribute("xmlns", name, attr.NamespaceURI);
                            a.Value = attr.Value;
                            rootNode.Attributes.Append(a);
                        }
                    }
                    else
                    {
                        bool isExist = false;
                        if (attr.Prefix == "xmlns")
                        {
                            // Try to find equal namespace with different name
                            foreach (XmlAttribute attribute in rootNode.Attributes)
                            {
                                if (attr.Value == attribute.Value)
                                {
                                    root.SetAttribute(attr.Name, attr.Value);
                                    ChangeNamespacePrefix(root, attr.LocalName, attribute.LocalName);
                                    isExist = true;
                                    break;
                                }
                            }
                        }

                        if (!isExist)
                        {
                            // Add namespace to result resource dictionarty
                            XmlAttribute a = finalDocument.CreateAttribute(attr.Prefix, attr.LocalName,attr.NamespaceURI);
                            a.Value = attr.Value;
                            rootNode.Attributes.Append(a);
                        }
                    }
                }

                // Extract resources
                foreach (XmlNode node in root.ChildNodes)
                {
                    if ((node is XmlElement) && (node.Name != "ResourceDictionary.MergedDictionaries"))
                    {
                        // Import XML node from one XML document to result XML document                        
                        XmlElement importedElement = finalDocument.ImportNode(node, true) as XmlElement;                        

                        // Find resource key
                        // TODO: Is any other variants???
                        string key = string.Empty;
                        if (importedElement.HasAttribute("Key")) key = importedElement.Attributes["Key"].Value;
                        else if (importedElement.HasAttribute("x:Key")) key = importedElement.Attributes["x:Key"].Value;
                        else if (importedElement.HasAttribute("TargetType")) key = importedElement.Attributes["TargetType"].Value;

                        if (!string.IsNullOrEmpty(key))
                        {
                            // Check key unique
                            if (keys.Contains(key)) continue;
                            keys.Add(key);

                            // Create ResourceElement for key and XML  node
                            ResourceElement res = new ResourceElement();
                            res.Key = key;
                            res.Element = importedElement;
                            res.UsedKeys = FillKeys(importedElement);
                            resourceElements.Add(key, res);
                            resourcesList.Add(res);
                        }
                    }

                    // TODO: Add output information.
                }
            }

            // Result list 
            List<ResourceElement> finalOrderList = new List<ResourceElement>();

            // Add all items with empty UsedKeys
            for (int i = 0; i < resourcesList.Count; i++)
            {
                if (resourcesList[i].UsedKeys.Length == 0)
                {
                    finalOrderList.Add(resourcesList[i]);

                    // Write to console
                    Console.WriteLine("Adding resource \"{0}\"", resourcesList[i].Key);

                    resourcesList.RemoveAt(i);
                    i--;
                }
            }

            // Add other resources in correct order
            while (resourcesList.Count > 0)
            {
                for (int i = 0; i < resourcesList.Count; i++)
                {
                    // Check used keys is in result list
                    bool containsAll = true;
                    for (int j = 0; j < resourcesList[i].UsedKeys.Length; j++)
                    {
                        if (resourceElements.ContainsKey(resourcesList[i].UsedKeys[j]) && (!finalOrderList.Contains(resourceElements[resourcesList[i].UsedKeys[j]])))
                        {
                            containsAll = false;
                            break;
                        }
                    }

                    // If all used keys is in result list ad this resource to result list
                    if (containsAll)
                    {
                        finalOrderList.Add(resourcesList[i]);
                        
                        // Write to console
                        Console.WriteLine("Adding resource \"{0}\"", resourcesList[i].Key);

                        resourcesList.RemoveAt(i);
                        i--;
                    }
                }

                // TODO: Limit iterations count.
            }

            // Add nodes to XML document
            for (int i = 0; i < finalOrderList.Count; i++)
            {
                rootNode.AppendChild(finalOrderList[i].Element);
            }

            // Write to console
            Console.WriteLine("Resource Dictionary generation completed.");

            // Save result file
            resultFile = Path.GetFullPath(resultFile);
            try
            {
                finalDocument.Save(resultFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error during Resource Dictionary saving: {0}", e.Message));
                return;
            }
            Console.WriteLine(string.Format("Resource Dictionary saved to \"{0}\".", resultFile));
        }

        /// <summary>
        /// Changes namespace prefix for XML node.
        /// </summary>
        /// <param name="element">XML node.</param>
        /// <param name="oldPrefix">Old namespace prefix.</param>
        /// <param name="newPrefix">New namespace prefix.</param>
        private static void ChangeNamespacePrefix(XmlElement element, string oldPrefix, string newPrefix)
        {
            // String for search
            string oldString = oldPrefix + ":";
            string newString = newPrefix + ":";
            string oldStringSpaced = " " + oldString;
            string newStringSpaced = " " + newString;

            foreach (XmlNode child in element.ChildNodes)
            {
                if (child is XmlElement)
                {
                    XmlElement childElement = child as XmlElement;
                    if (child.Prefix == oldPrefix) child.Prefix = newPrefix;
                    foreach (XmlAttribute attr in childElement.Attributes)
                    {
                        // Check all attributes prefix
                        if (attr.Prefix == oldPrefix) attr.Prefix = newPrefix;

                        // Check {x:Type {x:Static in attributes values
                        // TODO: Is any other???
                        if ((attr.Value.Contains("{x:Type") || attr.Value.Contains("{x:Static")) && attr.Value.Contains(oldStringSpaced))
                        {
                            attr.Value = attr.Value.Replace(oldStringSpaced, newStringSpaced);
                        }

                        // Check Property attribute
                        // TODO: Is any other???
                        if ((attr.Name == "Property") && attr.Value.StartsWith(oldString))
                        {
                            attr.Value = attr.Value.Replace(oldString, newString);
                        }
                    }

                    // Chenge namespaces for child node
                    ChangeNamespacePrefix(childElement, oldPrefix, newPrefix);
                }
            }
        }

        /// <summary>
        /// Dynamic resource string.
        /// </summary>
        private const string DynamicResourceString = "{DynamicResource ";

        /// <summary>
        /// Static resource string.
        /// </summary>
        private const string StaticResourceString = "{StaticResource ";

        /// <summary>
        /// Find all used keys for resource.
        /// </summary>
        /// <param name="element">Xml element which contains resource.</param>
        /// <returns>Array of keys used by resource.</returns>
        private static string[] FillKeys(XmlElement element)
        {
            // Result list
            List<string> result = new List<string>();

            // Check all attributes
            foreach (XmlAttribute attr in element.Attributes)
            {
                if (attr.Value.StartsWith(DynamicResourceString))
                {
                    // Find key
                    string key = attr.Value.Substring(DynamicResourceString.Length, attr.Value.Length - DynamicResourceString.Length - 1).Trim();

                    // Replace dynamic resource with static resource
                    attr.Value = StaticResourceString + key + "}";

                    // Add key to result
                    if (!result.Contains(key)) result.Add(key);
                }
                else if (attr.Value.StartsWith(StaticResourceString))
                {
                    // Find key
                    string key = attr.Value.Substring(StaticResourceString.Length, attr.Value.Length - StaticResourceString.Length - 1).Trim();

                    // Add key to result
                    if (!result.Contains(key)) result.Add(key);
                }
            }

            // Check child nodes
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node is XmlElement) result.AddRange(FillKeys(node as XmlElement));
            }

            return result.ToArray();
        }
    }
}
