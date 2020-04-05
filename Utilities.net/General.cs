// <copyright file="general.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using HtmlAgilityPack;
    using static TeamControlium.Utilities.Log;

    /// <summary>
    /// Collection of general Test Automation oriented standalone helper methods
    /// </summary>
    public static class General
    {
        /// <summary>
        /// Writes given Text to a text file, optionally auto versioning (adding (n) to filename) OR
        /// overwriting.
        /// </summary>
        /// <remarks>
        /// No exception is raised if there is any problem, but details of error is written to Logger log
        /// </remarks>
        /// <param name="fileName">Full path and filename to use</param>
        /// <param name="autoVersion">If true and file exists. (n) is added to auto-version.  If false and file exists, it is overwritten if able</param>
        /// <param name="text">Text to write</param>
        public static void WriteTextToFile(string fileName, bool autoVersion, string text)
        {
            try
            {
                string filenameToUse = fileName;
                if (autoVersion)
                {
                    int count = 1;
                    string fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    string path = Path.GetDirectoryName(fileName);
                    filenameToUse = fileName;

                    while (File.Exists(filenameToUse))
                    {
                        string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                        filenameToUse = Path.Combine(path, tempFileName + extension);
                    }
                }

                File.WriteAllText(filenameToUse, text);
            }
            catch (Exception ex)
            {
                LogException(ex, $"Cannot write data to file [{fileName ?? "Null Filename!"}] (AutoVersion={(autoVersion ? "Yes" : "No")})");
            }
        }

        /// <summary>
        /// Returns true if string does not start with 0 or starts with t, y or o (IE. true, yes or on)
        /// </summary>
        /// <param name="value">value to check</param>
        /// <returns>true if string first digit is not 0 or is true, yes or on</returns>
        public static bool IsValueTrue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (int.TryParse(value, out int i))
            {
                return i > 0;
            }

            return value.ToLower().StartsWith("t") || value.ToLower().StartsWith("y") || value.ToLower().StartsWith("on");
        }

        /// <summary>
        /// Normalises single and double quotes for XPath use
        /// </summary>
        /// <param name="original">String containing single and double quotes</param>
        /// <returns>String for XPath use</returns>
        public static string CleanStringForXPath(string original)
        {
            if (!original.Contains("'"))
            {
                return '\'' + original + '\'';
            }
            else if (!original.Contains("\""))
            {
                return '"' + original + '"';
            }
            else
            {
                return "concat('" + original.Replace("'", "',\"'\",'") + "')";
            }
        }

        /// <summary>
        /// Makes string filename friendly
        /// </summary>
        /// <param name="original">Possible unfriendly filename string</param>
        /// <returns>String that can be used in a filename</returns>
        public static string CleanStringForFilename(string original)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(original, invalidRegStr, "_");
        }

        /// <summary>
        /// Extracts displayed text from an HTML node and descendants
        /// </summary>
        /// <param name="htmlData">HTML containing text</param>
        /// <returns>Text with HTML stripped out</returns>
        public static string GetTextFromHTML(string htmlData)
        {
            if (string.IsNullOrEmpty(htmlData))
            {
                return string.Empty;
            }

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlData);

            string[] acceptableTags = new string[] { "strong", "em", "u" };

            Queue<HtmlNode> nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()"));
            while (nodes.Count > 0)
            {
                HtmlNode node = nodes.Dequeue();
                HtmlNode parentNode = node.ParentNode;

                if (!acceptableTags.Contains(node.Name) && node.Name != "#text")
                {
                    HtmlNodeCollection childNodes = node.SelectNodes("./*|./text()");

                    if (childNodes != null)
                    {
                        foreach (var child in childNodes)
                        {
                            nodes.Enqueue(child);
                            parentNode.InsertBefore(child, node);
                        }
                    }

                    parentNode.RemoveChild(node);
                }
            }

            return document.DocumentNode.InnerHtml;
        }
    }
}
