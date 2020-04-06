// <copyright file="testarguments.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Processes test command-line arguments and presents them to the test script as a string array
    /// </summary>
    /// <remarks>
    /// <code>Thanks to GriffonRL (https://www.codeproject.com/Articles/3111/C-NET-Command-Line-Arguments-Parser) for original</code>
    /// </remarks>
    public static class TestArguments
    {
        /// <summary>
        /// Working global holding processed parameters
        /// </summary>
        private static StringDictionary processedParameters;

        /// <summary>
        /// Process the test arguments and make available for the test to use.
        /// </summary>
        /// <remarks>
        /// Arguments are space delimited and handle various common parameter preambles.<br/>
        /// In the following example the test is called with the following parameters;<br/>
        /// param1  - string 'value1'<br/>
        /// param2  - an empty string (but does exist)<br/>
        /// param3  - string 'Test-:-work'<br/>
        /// param4  - string 'happy'<br/>
        /// param5  - string my param5 data'<br/>
        /// param6  - an empty string (but does exist)<br/>
        /// EG. <code>Test.exe -param1 value1 --param2 /param3:"Test-:-work" /param4=happy -param5 "my param5 data" -param6</code>
        /// </remarks>
        /// <param name="argumentsToProcess">String array of arguments for the test to use.</param>
        public static void Load(string[] argumentsToProcess)
        {
            string parameter = null;
            string[] parts;
            string oneOrMoreSpaces = @"[^\s]+";
            string ignoreTextInQuotes = @"(?x)""(\\.|[^\\\r\n""])*""";
            string regex = ignoreTextInQuotes + "|" + oneOrMoreSpaces;
            Regex spliter = new Regex(@"^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            MatchCollection normalizedArgs = (new Regex(regex)).Matches(string.Join(' ', argumentsToProcess));

            processedParameters = new StringDictionary();

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: 
            // -param1 value1 --param2 /param3:"Test-:-work" 
            //   /param4=happy -param5 '--=nice=--'
            foreach (Match normalizedArg in normalizedArgs)
            {
                // Look for new parameters (-,/ or --) and a
                // possible enclosed value (=,:)
                parts = spliter.Split(normalizedArg.Value, 3);

                switch (parts.Length)
                {
                    // Found a value (for the last parameter 
                    // found (space separator))
                    case 1:
                        if (parameter != null)
                        {
                            if (!processedParameters.ContainsKey(parameter))
                            {
                                parts[0] = remover.Replace(parts[0], "$1");
                                processedParameters.Add(parameter, parts[0]);
                            }

                            parameter = null;
                        }

                        // else Error: no parameter waiting for a value (skipped)
                        break;

                    // Found just a parameter
                    case 2:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (parameter != null)
                        {
                            if (!processedParameters.ContainsKey(parameter))
                            {
                                processedParameters.Add(parameter, string.Empty);
                            }
                        }

                        parameter = parts[1];
                        break;

                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (parameter != null)
                        {
                            if (!processedParameters.ContainsKey(parameter))
                            {
                                processedParameters.Add(parameter, string.Empty);
                            }
                        }

                        parameter = parts[1];

                        // Remove possible enclosing characters (",')
                        if (!processedParameters.ContainsKey(parameter))
                        {
                            parts[2] = remover.Replace(parts[2], "$1");
                            processedParameters.Add(parameter, parts[2]);
                        }

                        parameter = null;
                        break;
                }
            }

            // In case a parameter is still waiting
            if (parameter != null)
            {
                if (!processedParameters.ContainsKey(parameter))
                {
                    processedParameters.Add(parameter, string.Empty);
                }
            }
        }

        /// <summary>
        /// Return a named parameter value if it exists
        /// </summary>
        /// <param name="param">Parameter to obtain</param>
        /// <returns>Value of named parameter.  If named parameter does not exist null is returned</returns>
        public static string GetParam(string param)
        {
            try
            {
                return processedParameters[param];
            }
            catch
            {
                return null;
            }
        }
    }
}
