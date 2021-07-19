// <copyright file="detokeniser.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using static TeamControlium.Utilities.Log;

    /// <summary>
    /// Processes given strings to process and resolve all tokens in passed strings.
    /// </summary>
    public static class Detokeniser
    {
        /// <summary>
        /// Character to use as an escape char.
        /// </summary>
        /// <remarks>Defined as a string in-case it itself has to be escaped in the Regular Expression pattern</remarks>
        private static readonly char EscapeChar = '\\';

        /// <summary>
        /// Character defining the start of a Token to be processed.
        /// </summary>
        /// <remarks>Must NOT be the same as the escape character.
        /// Is a string in-case it needs to be escaped for Regular Expression pattern.</remarks>
        private static readonly char StartTokenChar = '{';

        /// <summary>
        /// Character defining the end of a Token to be processed.
        /// </summary>
        /// <remarks>Must NOT be the same as the escape character or the start token character.
        /// Is a string in-case it needs to be escaped for Regular Expression pattern.</remarks>
        private static readonly char EndTokenChar = '}';

        /// <summary>
        /// Regular Expression pattern defining the start of a Token to be processed.
        /// </summary>
        /// <remarks>Should contain a negative look-behind to enable escaping.</remarks>
        private static readonly string StartTokenPattern = StartTokenChar + @"(?<!" + EscapeChar + ".)";

        /// <summary>
        /// Regular Expression pattern defining the end of a Token to be processed.
        /// </summary>
        /// <remarks>Should contain a negative look-behind to enable escaping.</remarks>
        private static readonly string EndTokenPattern = EndTokenChar + @"(?<!" + EscapeChar + ".)";

        /// <summary>
        /// Default token delimiter.  Delimiter is used to partition token verb and body and is the default for field partitions within a token body.
        /// </summary>
        private static readonly char DefaultCommonDelimiter = ';';

        /// <summary>
        /// Collection of delimiters split by thread.  Allows different threads to use different delimiters if required.
        /// </summary>
        private static Dictionary<int, char> threadDelimiters = new Dictionary<int, char>();

        /// <summary>
        /// Gets or sets delegate for processing custom tokens if required. If set, delegate is called before internal token processing to allow overriding if required.
        /// </summary>
        /// <remarks> Assigned delegate must take char (delimiter being used) and the token (split into an array using delimiter as separator) and return the resolved token text.
        /// If the token cannot be resolved, delegate should return a null.  Any exception should be allowed to ripple up to enable the Utilities Detokeniser to handle
        /// the error.</remarks>
        public static Func<char, string[], string> CustomTokenProcessor { get; set; }

        /// <summary>
        /// Gets or sets the default delimiter within tokens.
        /// </summary>
        /// <remarks>This is used as the delimiter when separating token verb from body and is the default token when token verb processors split their required fields.
        /// Is thread-safe; if a Thread changes the token delimiter the change only affects Detokeniser calls for that thread.</remarks>
        public static Char DefaultTokenDelimiterCurrentThread
        {
            get
            {
                if (!threadDelimiters.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                {
                    DefaultTokenDelimiterCurrentThread = DefaultCommonDelimiter;
                }

                return threadDelimiters[Thread.CurrentThread.ManagedThreadId];
            }

            set
            {
                threadDelimiters.Add(Thread.CurrentThread.ManagedThreadId, value);
            }
        }

        /// <summary>
        /// Gets initialized instance of the .NET Random generator.  Used when resolving {random tokens.
        /// </summary>
        private static Random RandomGenerator { get; } = new Random();

        /// <summary>
        /// Process passed string and return after resolving all tokens.
        /// </summary>
        /// <param name="tokenisedString">String possibly containing tokens.</param>
        /// <returns>Passed string with all valid tokens resolved.</returns>
        public static string Detokenize(string tokenisedString)
        {
            bool deEscape = true;
            string outputString = string.Empty;
            string doubleEscapes = "" + EscapeChar + EscapeChar;
            string singleEscapes = "" + EscapeChar;
            string escapedStart = "" + EscapeChar + StartTokenChar;
            string escapedEnd = "" + EscapeChar + EndTokenChar;

            try
            {
                // Drill down to the left-most deepest (if nested) token
                InnermostToken token = new InnermostToken(tokenisedString);

                // Get all text to the left of any token found in the string passed in.  If no token was found Preamble will contain all text from passed string
                outputString = token.Preamble;

                // Loop until last token find found no tokens
                while (token.HasToken)
                {
                    // Process the last found token, prepend it to the text after the last found token then find any token in the resulting string
                    token = new InnermostToken(ProcessToken(token.Token.Substring(1, token.Token.Length - 2)) + token.Postamble);

                    // Concatinate the last found tokens preable (or full text if none found) to the built string and recursivley call self to ensure full token resolution 
                    outputString = Detokenize(outputString + token.Preamble);
                    deEscape = false;
                }

                // Return string may have escaped start/end token characters.  So remove escaping


                if (deEscape)
                {
                    outputString = outputString.Replace(doubleEscapes, singleEscapes);
                    outputString = outputString.Replace(escapedStart, "" + StartTokenChar);
                    outputString = outputString.Replace(escapedEnd, "" + EndTokenChar);
                }
                LogWriteLine(LogLevels.FrameworkDebug, $"Processed [{tokenisedString}]. Result [{outputString}]");
            }
            catch (Exception ex)
            {
                LogException(ex, $"Processing string [{tokenisedString}]]");
#pragma warning disable CA2200 // Rethrow to preserve stack details. Ignoring as catch/rethrow is for logging purposes....
                throw ex;
#pragma warning restore CA2200
            }

            // Finally return the fully processed string
            return outputString;
        }

        /// <summary>
        /// Processes parses and processes passed in token and returns the resolution result. Token must be trimmed of its start/end indicators.  
        /// </summary>
        /// <param name="token">Token to be processed</param>
        /// <returns>Result of processing passed in token.</returns>
        /// <remarks>A token must at least contain a single element, the verb.  This tells the processor what processing is required to resolve the token. The total number
        /// of elements in the token depends on the verb.  As an example the random token requires three fields (examples assume delimiter is a ;): verb;type;format or length.  IE. "random;digits;5" returns
        /// 5 random digits.  <code>"random;date(01-01-1980,31-12-1980);dd MMM yy"</code> returns a random 1980 date such as 09 JUN 80.</remarks>
        private static string ProcessToken(string token)
        {
            char delimiter = DefaultTokenDelimiterCurrentThread;

            string processedToken = null;

            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("Empty token!");
                }

                string[] splitToken = token.Split(new char[] { delimiter }, 2);

                if (CustomTokenProcessor != null)
                {
                    try
                    {
                        processedToken = CustomTokenProcessor(delimiter, splitToken);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error thrown by Custom Token Processor for [{splitToken[0]}] in {token}", ex);
                    }
                }

                if (processedToken == null)
                {
                    switch (splitToken[0].ToLower().Trim())
                    {
                        case "random":
                            if (splitToken.Length < 2)
                            {
                                throw new Exception($"Random token [{token}] needs at least 2 parts (IE. {{random;type[;<length>]}} etc.)");
                            }

                            processedToken = DoRandomToken(splitToken[1]);
                            break;
                        case "date":
                            if (splitToken.Length < 2)
                            {
                                throw new Exception($"Date token [{token}] needs 3 parts {{date;<offset>;<format>}}");
                            }

                            processedToken = DoDateToken(splitToken[1]);
                            break;
                        case "financialyearstart":
                            if (splitToken.Length < 2)
                            {
                                throw new Exception($"FinancialYearStart token [{token}] needs 3 parts {{FinancialYearStart;<date>;<format>}}");
                            }

                            processedToken = DoFinancialYearToken(splitToken[1], true);
                            break;
                        case "financialyearend":
                            if (splitToken.Length < 2)
                            {
                                throw new Exception($"FinancialYearEnd token [{token}] needs 3 parts {{FinancialYearEnd;<date>;<format>}}");
                            }

                            processedToken = DoFinancialYearToken(splitToken[1], false);
                            break;
                        default:
                            throw new Exception($"Unsupported token [{splitToken[0]}] in {token}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing token {{{token}}} ({ex.Message})");
            }

            return processedToken;
        }

        /// <summary>
        /// Process a 'random' token and return result
        /// </summary>
        /// <param name="tokenBody">Body part of token. Format depends on type of random being executed.</param>
        /// <returns>Token resolution result</returns>
        private static string DoRandomToken(string tokenBody)
        {
            string[] typeAndLengthOrFormat = tokenBody.Split(new char[] { DefaultTokenDelimiterCurrentThread }, 2);
            string result;
            string select = string.Empty;
            string verb = typeAndLengthOrFormat[0].ToLower().Trim();
            if (verb == "australiantfn")
            {
                result = DoRandomTFN();
            }
            else if (verb.StartsWith("date("))
            {
                result = DoRandomDate(verb.Substring(verb.IndexOf('(') + 1, verb.Length - 2 - verb.IndexOf('('))).ToString(typeAndLengthOrFormat[1]);
            }
            else if (verb.StartsWith("float("))
            {
                result = DoRandomFloat(verb.Substring(verb.IndexOf('(') + 1, verb.Length - 2 - verb.IndexOf('('))).ToString(typeAndLengthOrFormat[1]);
            }
            else
            {
                // {random,from(ASDF),5} - 5 characters selected from ASDF
                if (verb.StartsWith("from("))
                {
                    select = typeAndLengthOrFormat[0].Trim().Substring(verb.IndexOf('(') + 1, verb.Length - 2 - verb.IndexOf('('));
                }
                else
                {
                    switch (verb)
                    {
                        case "letters":
                            select = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                            break;
                        case "lowercaseletters":
                            select = "abcdefghijklmnopqrstuvwxyz";
                            break;
                        case "uppercaseletters":
                            select = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                            break;
                        case "digits":
                            select = "01234567890";
                            break;
                        case "alphanumerics":
                            select = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ01234567890";
                            break;
                        case "acn":
                            return Detokenize("{random;digits;9}");
                        case "abn":
                            {
                                string acn = Detokenize("{random;acn}");
                                return Detokenize($"{{ABNFromACN;{acn}}}");
                            }

                        default:
                            throw new Exception($"Unrecognised random Type [{typeAndLengthOrFormat[0]}] - Expect letters, lowercaseletters, uppercaseletters digits or alphanumerics");
                    }
                }

                if (!int.TryParse(typeAndLengthOrFormat[1], out int number))
                {
                    throw new Exception($"Invalid length part in Random token {{random;<type>;<length>}}");
                }

                result = new string(Enumerable.Repeat(select, number).Select(s => s[RandomGenerator.Next(s.Length)]).ToArray());
            }

            return result;
        }

        /// <summary>
        /// Process a 'date' token and return result.
        /// </summary>
        /// <param name="tokenBody">Consists of date offset process and required output format.</param>
        /// <returns>Required date in specified format.</returns>
        private static string DoDateToken(string tokenBody)
        {
            string[] offsetAndFormat = tokenBody.Split(new char[] { DefaultTokenDelimiterCurrentThread }, 2);

            if (offsetAndFormat.Length != 2)
            {
                throw new Exception("Date token does not have a format parameter; example: {date" + DefaultTokenDelimiterCurrentThread + "today" + DefaultTokenDelimiterCurrentThread + "dd-MM-yyyy}");
            }

            DateTime dt;
            string verb = offsetAndFormat[0].ToLower().Trim();
            if (verb.StartsWith("random("))
            {
                dt = DoRandomDate(verb.Substring(verb.IndexOf('(') + 1, verb.Length - 2 - verb.IndexOf('(')));
            }
            else
            {
                switch (verb)
                {
                    case "today":
                        dt = DateTime.Now;
                        break;
                    case "yesterday":
                        dt = DateTime.Now.AddDays(-1);
                        break;
                    case "tomorrow":
                        dt = DateTime.Now.AddDays(1);
                        break;
                    default:
                        {
                            if (offsetAndFormat[0].Contains('(') && offsetAndFormat[0].EndsWith(")"))
                            {
                                string[] activeOffset = verb.Substring(0, verb.Length - 1).Split(new char[] { '(' }, 2);
                                switch (activeOffset[0].Trim())
                                {
                                    case "addyears":
                                        dt = DateTime.Now.AddYears(int.Parse(activeOffset[1]));
                                        break;
                                    case "addmonths":
                                        dt = DateTime.Now.AddMonths(int.Parse(activeOffset[1]));
                                        break;
                                    case "adddays":
                                        dt = DateTime.Now.AddDays(int.Parse(activeOffset[1]));
                                        break;
                                    default:
                                        throw new Exception($"Invalid Active Date offset.  Expect AddYears(n) AddMonths(n) or AddDays(n). Got [{activeOffset[0].Trim()}]");
                                }
                            }
                            else
                            {
                                throw new Exception($"Invalid Active Date offset.  Open or Closing paranthesis missing.  Expect example {{date;AddYears(-30);dd-MM-yyyy}}");
                            }

                            break;
                        }
                }
            }

            return dt.ToString(offsetAndFormat[1]);
        }

        /// <summary>
        /// Returns formatted start or end date of financial year date passed in is within.
        /// </summary>
        /// <param name="tokenBody">Date within required financial year and required return format.</param>
        /// <param name="startOfFinancialYear">True - returns date start of financial year, False - returns date end of financial year.</param>
        /// <returns>Start or end date of financial year date is within.</returns>
        private static string DoFinancialYearToken(string tokenBody, bool startOfFinancialYear)
        {
            string year;
            string[] dateAndFormat = tokenBody.Split(new char[] { DefaultTokenDelimiterCurrentThread }, 2);

            if (!DateTime.TryParseExact(dateAndFormat[0], new string[] { "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy", "dd/MM/yy", "d/MM/yy", "dd/M/yy", "d/M/yy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateToWorkFrom))
            {
                throw new ArgumentException("Cannot parse date.  Must be in format d/M/y", "DateToWorkFromAndFormat (first element)");
            }

            if (dateToWorkFrom.Month >= 7)
            {
                year = startOfFinancialYear ? dateToWorkFrom.Year.ToString() : (dateToWorkFrom.Year + 1).ToString();
            }
            else
            {
                year = startOfFinancialYear ? (dateToWorkFrom.Year - 1).ToString() : dateToWorkFrom.Year.ToString();
            }

            DateTime returnDate = DateTime.ParseExact((startOfFinancialYear ? "01/07/" : "30/06/") + year, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return returnDate.ToString(dateAndFormat[1]);
        }

        /// <summary>
        /// Returns random floating point number on or between the minimum and maximum limits 
        /// </summary>
        /// <param name="limits">Maximum and minimum limits of random selection.</param>
        /// <returns>Random floating point number</returns>
        private static float DoRandomFloat(string limits)
        {
            string[] minimumAndMaximum = limits.Split(',');

            if (minimumAndMaximum.Length != 2)
            {
                throw new Exception($"Invalid Maximum and Minimum floats. Expect {{random.float(min;max),<format>}}. Max/min was: [{limits}]");
            }

            if (!float.TryParse(minimumAndMaximum[0], out float Min))
            {
                throw new Exception($"Invalid Minimum float. Expect {{random.float(min;max),<format>}}. Max/min was: [{limits}]");
            }

            if (!float.TryParse(minimumAndMaximum[1], out float Max))
            {
                throw new Exception($"Invalid Maximum float. Expect {{random.float(min;max),<format>}}. Max/min was: [{limits}]");
            }

            return DoRandomFloat(Min, Max);
        }

        /// <summary>
        /// Returns random floating point number on or between the minimum and maximum limits 
        /// </summary>
        /// <param name="minFloat">Minimum limit</param>
        /// <param name="maxFloat">Maximum limit</param>
        /// <returns>Random floating point number</returns>
        private static float DoRandomFloat(float minFloat, float maxFloat)
        {
            if (minFloat >= maxFloat)
            {
                throw new Exception($"Maximum float less than Minimum float! Expect {{random.float(min,max),<format>}} Min = {minFloat.ToString()}, Max = {maxFloat.ToString()}");
            }

            return ((float)RandomGenerator.NextDouble() * (maxFloat - minFloat)) + minFloat;
        }

        /// <summary>
        /// Returns random date on or between the minimum and maximum limits
        /// </summary>
        /// <param name="maxAndMinDates">String containing minimum and maximum dates in required format</param>
        /// <returns>Random dates between the two given dates, inclusive.</returns>
        private static DateTime DoRandomDate(string maxAndMinDates)
        {
            string[] maxAndMin = maxAndMinDates.Split(',');
            if (maxAndMin.Length != 2)
            {
                throw new Exception($"Invalid Maximum and Minimum dates. Expect {{random;date(dd-MM-yyyy,dd-MM-yyyy);<format>}}. Max/min was: [{maxAndMinDates}]");
            }

            if (!DateTime.TryParseExact(maxAndMin[0], "d-M-yyyy", CultureInfo.InstalledUICulture, DateTimeStyles.None, out DateTime Min))
            {
                throw new Exception($"Invalid Minimum date. Expect {{random;date(dd-MM-yyyy,dd-MM-yyyy);<format>}}. Max/min was: [{maxAndMinDates}]");
            }

            if (!DateTime.TryParseExact(maxAndMin[1], "d-M-yyyy", CultureInfo.InstalledUICulture, DateTimeStyles.None, out DateTime Max))
            {
                throw new Exception($"Invalid Maximum date. Expect {{random;date(dd-MM-yyyy,dd-MM-yyyy);<format>}}. Max/min was: [{maxAndMinDates}]");
            }

            return DoRandomDate(Min, Max);
        }

        /// <summary>
        /// Returns random date on or between the minimum and maximum limits
        /// </summary>
        /// <param name="minDate">Minimum date of random selection</param>
        /// <param name="maxDate">Maximum date of random selection</param>
        /// <returns>Random dates between the two given dates, inclusive.</returns>
        private static DateTime DoRandomDate(DateTime minDate, DateTime maxDate)
        {
            if (minDate > maxDate)
            {
                throw new Exception($"Maximum date earlier than Minimum date! Expect {{random;date(dd-MM-yyyy,dd-MM-yyyy);<format>}} Mindate = {minDate.ToString("dd/MM/yyyy")}, Maxdate = {maxDate.ToString("dd/MM/yyyy")}");
            }

            return minDate.AddDays(RandomGenerator.Next((maxDate - minDate).Days));
        }

        /// <summary>
        /// Returns a valid random Australian TFN
        /// </summary>
        /// <returns>Random valid TFN</returns>
        private static string DoRandomTFN()
        {
            // Formula for the Australian TFN got from https://en.wikipedia.org/wiki/Tax_file_number and
            // verified on https://www.clearwater.com.au/code/tfn
            // TFN is created here by getting 8 random digits (deliberately NO zeros as first digit as I'm not sure an initial zero is valid
            // in a TFN)
            // Each digit is then multiplied by its positional weighting and summed.  The Sum is then divided by 11 and the modulus obtained. 
            // A modulus of Zero is invalid but can happen.  In this case we simply try again. In modulus is valid it is appended to the 8 TFN digits
            string tfnNumber = ProcessToken("random;from(123456789);1") + ProcessToken("random;digits;7");
            int[] weights = new int[] { 1, 4, 3, 7, 5, 8, 6, 9 };
            int sum = 0;
            int checkDigit;
            for (int i = 0; i <= 7; i++)
            {
                int mid = int.Parse(tfnNumber.Substring(i, 1));
                sum += mid * weights[i];
            }

            checkDigit = sum % 11;

            if (checkDigit == 10)
            {
                return ProcessToken("random;australiantfn");
            }
            else
            {
                return tfnNumber + checkDigit;
            }
        }

        /// <summary>
        /// Represents result of a Token search for a given string
        /// </summary>
        private class InnermostToken
        {
            /// <summary>
            /// Holds all characters preceding any found token in given string.  Holds ALL characters from given string if no token found.
            /// </summary>
            private string tokenPreamble = default(string);

            /// <summary>
            /// Holds all characters following found token in given string.
            /// </summary>
            private string tokenPostamble = default(string);

            /// <summary>
            /// Innermost token found in given string
            /// </summary>
            private string token = default(string);

            /// <summary>
            /// Indicates if a token was found or not
            /// </summary>
            private bool foundToken = default(bool);

            /// <summary>
            /// Initialises a new instance of the <see cref="InnermostToken" /> class.
            /// Takes a string that may or may not contain token/s.  If string contains token/s, innermost (from left) token is identified  
            /// </summary>
            /// <param name="inputString">String to be processed</param>
            public InnermostToken(string inputString)
            {
                int startIndex = -1;
                int endIndex = -1;

                //Regex startRegex = new Regex(StartTokenPattern, RegexOptions.IgnoreCase);
                //Regex startRegexReverse = new Regex(StartTokenPattern, RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                //Regex endRegex = new Regex(EndTokenPattern, RegexOptions.IgnoreCase);

                //Match startMatch = startRegex.Match(inputString);
                //if (startMatch.Success)
                //{
                //    startIndex = startMatch.Index;
                //    Match endMatch = endRegex.Match(inputString, startIndex);
                //    if (endMatch.Success)
                //    {
                //        endIndex = endMatch.Index;
                //        startIndex = startRegexReverse.Match(inputString, endIndex).Index;
                //        matchMade = true;
                //    }
                //}

                // We want the innermost LEFT token, so we find the first (left to right) occurance of a non-escaped closing token
                // that has a non-escaped opening token to its left.  Sounds easy.  :-)

                for (int index = 0; index < inputString.Length; index++)
                {
                    if (inputString[index] == EndTokenChar && !IsEscaped(inputString, index))
                    {
                        endIndex = index;
                        break;
                    }
                }

                // Now see if that closing token has a corresponding non-escaped opening token...

                for (int index = endIndex; index >= 0; index--)
                {
                    if (inputString[index] == StartTokenChar && !IsEscaped(inputString,index))
                    {
                        startIndex = index;
                        break;
                    }
                }


                if ((startIndex != -1) && (endIndex != -1))
                {
                    this.tokenPreamble = inputString.Substring(0, startIndex);
                    this.tokenPostamble = inputString.Substring(endIndex + 1, inputString.Length - endIndex - 1);
                    this.token = inputString.Substring(startIndex, endIndex - startIndex + 1);
                }
                else
                {
                    this.tokenPreamble = inputString;
                    this.tokenPostamble = string.Empty;
                }


                this.foundToken = (startIndex != -1) && (endIndex != -1);
            }


            /// <summary>
            /// Gets all characters preceding any found token in given string.  Returns ALL characters from given string if no token found.
            /// </summary>
            public string Preamble
            {
                get
                {
                    return this.tokenPreamble;
                }
            }

            /// <summary>
            /// Gets all characters following found token in given string.
            /// </summary>
            public string Postamble
            {
                get
                {
                    return this.tokenPostamble;
                }
            }

            /// <summary>
            /// Gets Innermost token found in given string
            /// </summary>            
            public string Token
            {
                get
                {
                    return this.token;
                }
            }

            /// <summary>
            /// Gets a value indicating whether a token was found
            /// </summary>
            public bool HasToken
            {
                get
                {
                    return this.foundToken;
                }
            }

            private bool IsEscaped(string fullString, int positionToTest)
            {
                int index;
                int escapeCharCount = 0;
                bool isEscaped = false;  // We default to not escaped....

                // Is escaped if there are an odd number of escape chars to the left of the position...
                if (positionToTest > 0)
                {
                    index = positionToTest;
                    while (!(--index < 0))
                    {
                        if (fullString[index] == EscapeChar)
                        {
                            escapeCharCount++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (escapeCharCount % 2 == 1)
                    {
                        // Odd number of escapes, yes it has been escaped (IE. has odd number of escape chars before it)
                        isEscaped = true;
                    }
                }
                return isEscaped;
            }
        }
    }
}
