// <copyright file="general.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.Utilities
{
    using System;
    using System.IO;

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
                Log.LogException(ex, $"Cannot write data to file [{fileName ?? "Null Filename!"}] (AutoVersion={(autoVersion ? "Yes" : "No")})");
            }
        }
    }
}
