namespace XamlCombine
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    public class Combiner
    {
        /// <summary>
        /// Dynamic resource string.
        /// </summary>
        private const string DynamicResourceString = "{DynamicResource ";

        /// <summary>
        /// Static resource string.
        /// </summary>
        private const string StaticResourceString = "{StaticResource ";

        private Lazy<string> appPath;

        public Combiner()
        {
            this.appPath = new Lazy<string>(this.GetAppPath);
        }

        /// <summary>
        /// Combines multiple XAML resource dictionaries in one.
        /// </summary>
        /// <param name="sourceFile">Filename of list of XAML's.</param>
        /// <param name="resultFile">Result XAML filename.</param>
        public void Combine(string sourceFile, string resultFile)
        {
            Trace.WriteLine(string.Format("Loading resources list from \"{0}\"", sourceFile));

            sourceFile = this.GetFilePath(sourceFile);

            // Load resources lists
            var resources = File.ReadAllLines(sourceFile);

            // Create result XML document
            var finalDocument = new XmlDocument();
            var rootNode = finalDocument.CreateElement("ResourceDictionary", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            finalDocument.AppendChild(rootNode);

            // List of existing keys, to avoid duplicates
            var keys = new List<string>();

            // Associate key with ResourceElement
            var resourceElements = new Dictionary<string, ResourceElement>();

            // List of read resources
            var resourcesList = new List<ResourceElement>();

            // For each resource file
            for (var i = 0; i < resources.Length; i++)
            {
                var current = new XmlDocument();
                current.Load(this.GetFilePath(resources[i]));

                Debug.WriteLine(string.Format("Loading resource \"{0}\"", resources[i]));

                // Set and fix resource dictionary attributes
                var root = current.DocumentElement;
                if (root == null)
                {
                    continue;
                }

                for (var j = 0; j < root.Attributes.Count; j++)
                {
                    XmlAttribute attr = root.Attributes[j];
                    if (rootNode.HasAttribute(attr.Name))
                    {
                        // If namespace with this name exists and not equal
                        if (attr.Value != rootNode.Attributes[attr.Name].Value
                            && attr.Prefix == "xmlns")
                        {
                            // Create new namespace name
                            var index = 0;
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
                        var exists = false;
                        if (attr.Prefix == "xmlns")
                        {
                            // Try to find equal namespace with different name
                            foreach (XmlAttribute attribute in rootNode.Attributes)
                            {
                                if (attr.Value == attribute.Value)
                                {
                                    root.SetAttribute(attr.Name, attr.Value);
                                    ChangeNamespacePrefix(root, attr.LocalName, attribute.LocalName);
                                    exists = true;
                                    break;
                                }
                            }
                        }

                        if (exists == false)
                        {
                            // Add namespace to result resource dictionarty
                            XmlAttribute a = finalDocument.CreateAttribute(attr.Prefix, attr.LocalName, attr.NamespaceURI);
                            a.Value = attr.Value;
                            rootNode.Attributes.Append(a);
                        }
                    }
                }

                // Extract resources
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node is XmlElement
                        && node.Name != "ResourceDictionary.MergedDictionaries")
                    {
                        // Import XML node from one XML document to result XML document                        
                        XmlElement importedElement = finalDocument.ImportNode(node, true) as XmlElement;

                        // Find resource key
                        // TODO: Is any other variants???
                        var key = string.Empty;
                        if (importedElement.HasAttribute("Key"))
                        {
                            key = importedElement.Attributes["Key"].Value;
                        }
                        else if (importedElement.HasAttribute("x:Key"))
                        {
                            key = importedElement.Attributes["x:Key"].Value;
                        }
                        else if (importedElement.HasAttribute("TargetType"))
                        {
                            key = importedElement.Attributes["TargetType"].Value;
                        }

                        if (string.IsNullOrEmpty(key) == false)
                        {
                            // Check key unique
                            if (keys.Contains(key))
                            {
                                continue;
                            }

                            keys.Add(key);

                            // Create ResourceElement for key and XML  node
                            var res = new ResourceElement(key, importedElement, FillKeys(importedElement));
                            resourceElements.Add(key, res);
                            resourcesList.Add(res);
                        }
                    }

                    // TODO: Add output information.
                }
            }

            // Result list 
            var finalOrderList = new List<ResourceElement>();

            // Add all items with empty UsedKeys
            for (var i = 0; i < resourcesList.Count; i++)
            {
                if (resourcesList[i].UsedKeys.Length == 0)
                {
                    finalOrderList.Add(resourcesList[i]);

                    Debug.WriteLine(string.Format("Adding resource \"{0}\"", resourcesList[i].Key));

                    resourcesList.RemoveAt(i);
                    i--;
                }
            }

            // Add other resources in correct order
            while (resourcesList.Count > 0)
            {
                for (var i = 0; i < resourcesList.Count; i++)
                {
                    // Check used keys is in result list
                    var containsAll = true;
                    for (var j = 0; j < resourcesList[i].UsedKeys.Length; j++)
                    {
                        if (resourceElements.ContainsKey(resourcesList[i].UsedKeys[j])
                            && finalOrderList.Contains(resourceElements[resourcesList[i].UsedKeys[j]]) == false)
                        {
                            containsAll = false;
                            break;
                        }
                    }

                    // If all used keys is in result list ad this resource to result list
                    if (containsAll)
                    {
                        finalOrderList.Add(resourcesList[i]);

                        Debug.WriteLine(string.Format("Adding resource \"{0}\"", resourcesList[i].Key));

                        resourcesList.RemoveAt(i);
                        i--;
                    }
                }

                // TODO: Limit iterations count.
            }

            // Add nodes to XML document
            for (var i = 0; i < finalOrderList.Count; i++)
            {
                rootNode.AppendChild(finalOrderList[i].Element);
            }

            // Save result file
            WriteResultFile(Path.Combine(appPath.Value, resultFile), finalDocument);
        }

        private string GetAppPath()
        {
            // Current application path
            // TODO: Need to support all types of paths not only relative.
            var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            return appPath;
        }

        private string GetFilePath(string file)
        {
            var filePath = file;

            if (File.Exists(filePath) == false)
            {
                filePath = Path.Combine(appPath.Value, filePath);

                if (File.Exists(filePath) == false)
                {
                    throw new FileNotFoundException("Unable to find file.", file);
                }
            }

            return filePath;
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
            var oldString = oldPrefix + ":";
            var newString = newPrefix + ":";
            var oldStringSpaced = " " + oldString;
            var newStringSpaced = " " + newString;

            foreach (XmlNode child in element.ChildNodes)
            {
                var childElement = child as XmlElement;

                if (childElement != null)
                {
                    if (child.Prefix == oldPrefix)
                    {
                        child.Prefix = newPrefix;
                    }

                    foreach (XmlAttribute attr in childElement.Attributes)
                    {
                        // Check all attributes prefix
                        if (attr.Prefix == oldPrefix) attr.Prefix = newPrefix;

                        // Check {x:Type {x:Static in attributes values
                        // TODO: Is any other???
                        if ((attr.Value.Contains("{x:Type") || attr.Value.Contains("{x:Static"))
                            && attr.Value.Contains(oldStringSpaced))
                        {
                            attr.Value = attr.Value.Replace(oldStringSpaced, newStringSpaced);
                        }

                        // Check Property attribute
                        // TODO: Is any other???
                        if (attr.Name == "Property"
                            && attr.Value.StartsWith(oldString))
                        {
                            attr.Value = attr.Value.Replace(oldString, newString);
                        }
                    }

                    // Change namespaces for child node
                    ChangeNamespacePrefix(childElement, oldPrefix, newPrefix);
                }
            }
        }

        /// <summary>
        /// Find all used keys for resource.
        /// </summary>
        /// <param name="element">Xml element which contains resource.</param>
        /// <returns>Array of keys used by resource.</returns>
        private static string[] FillKeys(XmlElement element)
        {
            // Result list
            var result = new List<string>();

            // Check all attributes
            foreach (XmlAttribute attr in element.Attributes)
            {
                if (attr.Value.StartsWith(DynamicResourceString))
                {
                    // Find key
                    var key = attr.Value.Substring(DynamicResourceString.Length, attr.Value.Length - DynamicResourceString.Length - 1).Trim();

                    // Add key to result
                    if (result.Contains(key) == false)
                    {
                        result.Add(key);
                    }
                }
                else if (attr.Value.StartsWith(StaticResourceString))
                {
                    // Find key
                    var key = attr.Value.Substring(StaticResourceString.Length, attr.Value.Length - StaticResourceString.Length - 1).Trim();

                    // Add key to result
                    if (result.Contains(key) == false)
                    {
                        result.Add(key);
                    }
                }
            }

            // Check child nodes
            foreach (XmlNode node in element.ChildNodes)
            {
                var nodeElement = node as XmlElement;
                if (nodeElement != null)
                {
                    result.AddRange(FillKeys(nodeElement));
                }
            }

            return result.ToArray();
        }

        private static void WriteResultFile(string resultFile, XmlDocument finalDocument)
        {
            try
            {
                var tempFile = resultFile + ".tmp";
                finalDocument.Save(tempFile);

                Trace.WriteLine(string.Format("Comparing temp file \"{0}\" to \"{1}\"", tempFile, resultFile));

                if (File.Exists(resultFile) == false
                    || File.ReadAllText(resultFile) != File.ReadAllText(tempFile))
                {
                    File.Copy(tempFile, resultFile, true);

                    Trace.WriteLine(string.Format("Resource Dictionary saved to \"{0}\".", resultFile));
                }
                else
                {
                    Trace.WriteLine("New Resource Dictionary did not differ from existing file. No new file written.");
                }

                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error during Resource Dictionary saving: {0}", e);
            }
        }
    }
}