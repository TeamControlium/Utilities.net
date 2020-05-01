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
    using System.Threading;
    using HtmlAgilityPack;
    using static TeamControlium.Utilities.Log;

    /// <summary>
    /// Collection of general Test Automation oriented standalone helper methods
    /// </summary>
    public static class General
    {
        /// <summary>
        /// Keeps note of all filenames being written to by threads.  Prevents overwriting between threads.
        /// </summary>
        private static Dictionary<int, string> writeFilesThreaded = new Dictionary<int, string>();

        /// <summary>
        /// Write mode when writing to a file
        /// </summary>
        public enum WriteMode
        {
            /// <summary>
            /// Number appended to filename to ensure filename unique.
            /// </summary>
            AutoVersion,

            /// <summary>
            /// If file exists it is overwritten
            /// </summary>
            Overwrite,

            /// <summary>
            /// If filename exists, data appended to latest versioned instance.
            /// </summary>
            Append
        }

        /// <summary>
        /// Writes given Text to a text file, optionally auto versioning (adding (n) to filename) OR
        /// overwriting.
        /// </summary>
        /// <remarks>
        /// No exception is raised if there is any problem, but details of error is written to Logger log
        /// </remarks>
        /// <param name="fileName">Full path and filename to use</param>
        /// <param name="writeMode">Write mode (See <see cref="WriteMode"/>) when writing to file</param>
        /// <param name="text">Text to write</param>
        public static void WriteTextToFile(string fileName, WriteMode writeMode, string text)
        {
            object lockObject = new object();
            lock (lockObject)
            {
                try
                {
                    string fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    string path = Path.GetDirectoryName(fileName);
                    string filenameToUse = fileName;
                    string lastFilename;
                    int count;

                    if (writeMode == WriteMode.AutoVersion)
                    {
                        count = 1;
                        while (File.Exists(filenameToUse))
                        {
                            filenameToUse = Path.Combine(path, string.Format("{0}({1})", fileNameOnly, count++) + extension);
                        }

                        File.AppendAllText(filenameToUse, text);
                        if (writeFilesThreaded.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                        {
                            writeFilesThreaded[Thread.CurrentThread.ManagedThreadId] = filenameToUse;
                        }
                        else
                        {
                            writeFilesThreaded.Add(Thread.CurrentThread.ManagedThreadId, filenameToUse);
                        }
                    }

                    if (writeMode == WriteMode.Append)
                    {
                        if (writeFilesThreaded.ContainsKey(Thread.CurrentThread.ManagedThreadId) && writeFilesThreaded[Thread.CurrentThread.ManagedThreadId].StartsWith(filenameToUse))
                        {
                            filenameToUse = writeFilesThreaded[Thread.CurrentThread.ManagedThreadId];
                        }
                        else
                        {
                            count = 1;
                            lastFilename = filenameToUse;
                            while (File.Exists(filenameToUse))
                            {
                                lastFilename = filenameToUse;
                                filenameToUse = Path.Combine(path, string.Format("{0}({1})", fileNameOnly, count++) + extension);
                            }

                            File.AppendAllText(lastFilename, text);
                            writeFilesThreaded.Add(Thread.CurrentThread.ManagedThreadId, lastFilename);
                        }
                    }

                    if (writeMode == WriteMode.Overwrite)
                    {
                        count = 1;
                        while (File.Exists(filenameToUse))
                        {
                            File.Delete(filenameToUse);
                            filenameToUse = Path.Combine(path, string.Format("{0}({1})", fileNameOnly, count++) + extension);
                        }

                        File.WriteAllText(fileName, text);
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex, $"Cannot write data to file [{fileName ?? "Null Filename!"}] (Mode={writeMode.ToString()})");
                }
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
        /// Converts a Web URL to a nix/Windows compatible filename
        /// </summary>
        /// <param name="url">URL to convert</param>
        /// <returns>URL represented as a valid filename</returns>
        /// <remarks>All non-alpha characters are converted to underscore</remarks>
        public static string ConvertURLToValidFilename(string url)
        {
            List<string> matchedChars = new List<string>();
            string rt = string.Empty;
            Regex r = new Regex(@"[a-z]+", RegexOptions.IgnoreCase);
            foreach (Match m in r.Matches(url))
            {
                matchedChars.Add(m.Value);
            }

            for (int i = 0; i < matchedChars.Count; i++)
            {
                rt += matchedChars[i];
                rt += "_";
            }

            rt = (matchedChars.Count > 0) ? rt.Substring(0, rt.Length - 1) : string.Empty;
            return rt;
        }

        /// <summary>
        /// Encodes a plain text string into Base64.
        /// </summary>
        /// <param name="plainText">Text to be converted</param>
        /// <returns>Equivalent string Base64 encoded</returns>
        public static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));
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
