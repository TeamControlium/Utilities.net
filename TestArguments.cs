using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace TeamControlium.Utilities
{
    /// <summary>
    /// Processes test command-line arguments and presents them to the test script as a string array
    /// </summary>
    public class TestArguments
    {
        // Variables
        private StringDictionary processedParameters;

        /// <summary>
        /// Process the test arguments and make available for the test to use.
        /// </summary>
        /// <remarks>
        /// Arguments are space delimited and handle various common parameter preambles<br/><br/>
        /// EG. Test.exe -param1 value1 --param2 /param3:"Test-:-work /param4=happy -param5 '--=nice=--'
        /// </remarks>
        /// <param name="argumentsToProcess">String array of arguments for the test to use.</param>
        public TestArguments(string[] argumentsToProcess)
        {
            processedParameters = new StringDictionary();
            Regex Spliter = new Regex(@"^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Regex Remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                argumentParts = Spliter.Split(currentArgument, 3);

                switch (argumentParts.Length)
                {
                    // Found a value (for the last parameter 
                    // found (space separator))
                    case 1:
                        if (currentParameterBeingBuilt != null)
                        {
                            if (!processedParameters.ContainsKey(currentParameterBeingBuilt))
                            {
                                argumentParts[0] =
                                    Remover.Replace(argumentParts[0], "$1");

                                processedParameters.Add(currentParameterBeingBuilt, argumentParts[0]);
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
                            if (!processedParameters.ContainsKey(currentParameterBeingBuilt))
                                processedParameters.Add(currentParameterBeingBuilt, "true");
                        }
                        currentParameterBeingBuilt = argumentParts[1];
                        break;

                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. 
                        // With no value, set it to true.
                        if (currentParameterBeingBuilt != null)
                        {
                            if (!processedParameters.ContainsKey(currentParameterBeingBuilt))
                                processedParameters.Add(currentParameterBeingBuilt, "true");
                        }

                        currentParameterBeingBuilt = argumentParts[1];

                        // Remove possible enclosing characters (",')
                        if (!processedParameters.ContainsKey(currentParameterBeingBuilt))
                        {
                            argumentParts[2] = Remover.Replace(argumentParts[2], "$1");
                            processedParameters.Add(currentParameterBeingBuilt, argumentParts[2]);
                        }

                        currentParameterBeingBuilt = null;
                        break;
                }
            }
            // In case a parameter is still waiting
            if (currentParameterBeingBuilt != null)
            {
                if (!processedParameters.ContainsKey(currentParameterBeingBuilt))
                    processedParameters.Add(currentParameterBeingBuilt, "true");
            }
        }

        /// <summary>
        /// Return a named parameter value if it exists
        /// </summary>
        /// <param name="Param">Parameter to obtain</param>
        /// <returns>Value of named parameter.  If named parameter does not exist null is returned</returns>
        public string this[string Param]
        {
            get
            {
                try
                {
                    return (processedParameters[Param]);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}