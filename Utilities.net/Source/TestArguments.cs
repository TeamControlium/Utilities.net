// <copyright file="testarguments.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.Utilities
{
    using System.Collections.Specialized;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Processes test command-line arguments and presents them to the test script as a string array
    /// </summary>
    /// <remarks>
    /// Thanks to Mike Burns (https://www.linkedin.com/in/maddogmikeb) for original
    /// </remarks>
    public class TestArguments
    {
        /// <summary>
        /// Working global holding processed parameters
        /// </summary>
        private StringDictionary processedParameters;

        /// <summary>
        /// Initialises a new instance of the <see cref="TestArguments" /> class.
        /// Process the test arguments and make available for the test to use.
        /// </summary>
        /// <remarks>
        /// Arguments are space delimited and handle various common parameter preambles<br/><br/>
        /// EG. <code>Test.exe -param1 value1 --param2 /param3:"Test-:-work /param4=happy -param5 '--=nice=--'</code>
        /// </remarks>
        /// <param name="argumentsToProcess">String array of arguments for the test to use.</param>
        public TestArguments(string[] argumentsToProcess)
        {
            this.processedParameters = new StringDictionary();
            Regex spliter = new Regex(@"^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            string currentParameterBeingBuilt = null;
            string[] argumentParts;

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: 
            // -param1 value1 --param2 /param3:"Test-:-work" 
            //   /param4=happy -param5 '--=nice=--'
            foreach (string currentArgument in argumentsToProcess)
            {
                // Look for new parameters (-,/ or --) and a
                // possible enclosed value (=,:)
                argumentParts = spliter.Split(currentArgument, 3);

                switch (argumentParts.Length)
                {
                    // Found a value (for the last parameter 
                    // found (space separator))
                    case 1:
                        if (currentParameterBeingBuilt != null)
                        {
                            if (!this.processedParameters.ContainsKey(currentParameterBeingBuilt))
                            {
                                argumentParts[0] =
                                    remover.Replace(argumentParts[0], "$1");

                                this.processedParameters.Add(currentParameterBeingBuilt, argumentParts[0]);
                            }

                            currentParameterBeingBuilt = null;
                        }

                        // else Error: no parameter waiting for a value (skipped)
                        break;

                    // Found just a parameter
                    case 2:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (currentParameterBeingBuilt != null)
                        {
                            if (!this.processedParameters.ContainsKey(currentParameterBeingBuilt))
                            {
                                this.processedParameters.Add(currentParameterBeingBuilt, "true");
                            }
                        }

                        currentParameterBeingBuilt = argumentParts[1];
                        break;

                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (currentParameterBeingBuilt != null)
                        {
                            if (!this.processedParameters.ContainsKey(currentParameterBeingBuilt))
                            {
                                this.processedParameters.Add(currentParameterBeingBuilt, "true");
                            }
                        }

                        currentParameterBeingBuilt = argumentParts[1];

                        // Remove possible enclosing characters (",')
                        if (!this.processedParameters.ContainsKey(currentParameterBeingBuilt))
                        {
                            argumentParts[2] = remover.Replace(argumentParts[2], "$1");
                            this.processedParameters.Add(currentParameterBeingBuilt, argumentParts[2]);
                        }

                        currentParameterBeingBuilt = null;
                        break;
                }
            }

            // In case a parameter is still waiting
            if (currentParameterBeingBuilt != null)
            {
                if (!this.processedParameters.ContainsKey(currentParameterBeingBuilt))
                {
                    this.processedParameters.Add(currentParameterBeingBuilt, "true");
                }
            }
        }

        /// <summary>
        /// Return a named parameter value if it exists
        /// </summary>
        /// <param name="param">Parameter to obtain</param>
        /// <returns>Value of named parameter.  If named parameter does not exist null is returned</returns>
        public string this[string param]
        {
            get
            {
                try
                {
                    return this.processedParameters[param];
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
