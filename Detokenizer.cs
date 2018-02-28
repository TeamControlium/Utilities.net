using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamControlium.Utilities
{
    public class Detokenizer
    {
        private static readonly char tokenStartChar = '{';
        private static readonly char tokenEndChar = '}';
        private static Random RandomGenerator { get; } = new Random();

        /// <summary>
        /// Delegate for processing custom tokens if required.  If a token cannot be resolved internally, the token (split based on the delimiter) is passed to
        /// the CustomTokenProcessor, if defined. CustomTokenProcessor returns a null string if token not resolved.
        /// </summary>
        static public Func<char, string[], string> CustomTokenProcessor { get; set; }

        //[DebuggerStepThrough]
        public static string ProcessTokensInString(string stringWithTokens)
        {
            var detokenizedString = new StringBuilder();
            var startIndex = 0;
            var foundTokenStart = false;

            //
            // Find the start of a token, ignoring doubles {{'s as they are litterals)
            //
            while (!foundTokenStart && startIndex < stringWithTokens.Length)
            {
                if (stringWithTokens[startIndex] == tokenStartChar)
                {
                    // We are looking at a token start char...
                    if ((startIndex < stringWithTokens.Length - 1) && (stringWithTokens[startIndex + 1] == tokenStartChar))
                    {
                        // Next char is also a start char, so ignore and skip past
                        startIndex += 1;
                    }
                    else
                    {
                        // Next char not a start char so we have found a token!
                        foundTokenStart = true;
                    }
                }
                startIndex += 1;
            }

            //
            // startIndex is now pointing to first char of a token
            //

            if (foundTokenStart)
            {
                var foundTokenEnd = false;
                var endIndex = startIndex; // We start searching for the end of the token from the first character of the 
                //
                // Find the end of the token.
                //
                while (!foundTokenEnd && endIndex < stringWithTokens.Length)
                {
                    if ((stringWithTokens[endIndex] == tokenStartChar) &&
                        !((startIndex < stringWithTokens.Length - 1) && (stringWithTokens[startIndex + 1] == tokenStartChar)))
                    {
                        //
                        // Another start token (and it is NOT a dounble!!!!)  We have nested tokens by golly.
                        // So, start the process again, but from the new start of the nested token. Hah, this
                        // is a quick easy way of dealing with nested tokens!
                        //
                        startIndex = endIndex + 1;
                    }
                    else if (stringWithTokens[endIndex] == tokenEndChar)
                    {
                        if ((endIndex < stringWithTokens.Length - 1) && (stringWithTokens[endIndex + 1] == tokenEndChar))
                        {
                            // Next char is also an end char, so ignore and skip past
                            endIndex += 1;
                        }
                        else
                        {
                            // Next char not a start char so we have found a token!
                            foundTokenEnd = true;
                        }
                    }
                    endIndex += 1;
                }
                if (foundTokenEnd)
                {
                    detokenizedString.Append(stringWithTokens.Substring(0, startIndex - 1));
                    string token = stringWithTokens.Substring(startIndex, endIndex - startIndex - 1);
                    try
                    {
                        detokenizedString.Append(ProcessToken(token));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error processing token {{{token}}} ({ex.Message})", ex);
                    }
                    detokenizedString.Append(stringWithTokens.Substring(endIndex, stringWithTokens.Length - endIndex));
                }
                else
                {
                    throw new Exception($"Found token start {{ found at index {startIndex} but no closing }} found: [{stringWithTokens}]");
                }
                // Now, we call ourself again to process any more tokens....
                detokenizedString = new StringBuilder(ProcessTokensInString(detokenizedString.ToString()));
            }
            else
            {
                // So no token found. We will convert all doubles back to singles and return the string...
                detokenizedString.Append(stringWithTokens).Replace("{{", "{").Replace("}}", "}");
            }
            return detokenizedString.ToString();
        }

        private static string ProcessToken(string token)
        {
            char delimiter = ';';
            string processedToken = "";
            if (string.IsNullOrEmpty(token)) throw new Exception("Empty token!");
            string[] splitToken = token.Split(new char[] { delimiter }, 2);
            switch (splitToken[0].ToLower().Trim())
            {
                case "random":
                    if (splitToken.Length < 2) throw new Exception($"Random token [{token}] needs 3 parts {{random;<type>;<length>}}");
                    processedToken = DoRandomToken(delimiter, splitToken[1]);
                    break;
                case "date":
                    if (splitToken.Length < 2) throw new Exception($"Date token [{token}] needs 3 parts {{date;<offset>;<format>}}");
                    processedToken = DoDateToken(delimiter, splitToken[1]);
                    break;
                case "financialyearstart":
                    if (splitToken.Length < 2) throw new Exception($"FinancialYearStart token [{token}] needs 3 parts {{FinancialYearStart;<date>;<format>}}");
                    processedToken = DoFinancialYearToken(delimiter, splitToken[1], true);
                    break;
                case "financialyearend":
                    if (splitToken.Length < 2) throw new Exception($"FinancialYearEnd token [{token}] needs 3 parts {{FinancialYearEnd;<date>;<format>}}");
                    processedToken = DoFinancialYearToken(delimiter, splitToken[1], false);
                    break;
//                case "seleniumkeys":
//                case "seleniumkey":
//                    if (splitToken.Length < 2) throw new Exception($"SeleniumKey token [{token}] needs 2 parts {{SeleniumKey;<Name>}}");
//                    processedToken = DoSeleniumKey(splitToken[1]);
//                    break;
                default:
                    if (CustomTokenProcessor!=null)
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
                        throw new Exception($"Unsupported token [{splitToken[0]}] in {token}");
                    }
                    break;
            }
            return processedToken;
        }


        private static string DoRandomToken(char delimiter, string TypeAndLength)
        {
            string[] typeAndLengthOrFormat = TypeAndLength.Split(new char[] { delimiter }, 2);
            string result;
            string select = "";
            string verb = typeAndLengthOrFormat[0].ToLower().Trim();
            if (verb.StartsWith("date("))
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
                            {
                                string acn = ProcessTokensInString("{random;digits;9}");
                                return acn;
                                break;
                            }
                        case "abn":
                            {
                                string acn = ProcessTokensInString("{random;acn}");
                                result = ProcessTokensInString($"{{ABNFromACN;{acn}}}");
                                return result;
                                break;
                            }
                        default:
                            throw new Exception($"Unrecognised random Type [{typeAndLengthOrFormat[0]}] - Expect letters, lowercaseletters, uppercaseletters digits or alphanumerics");
                    }
                }
                int number;
                if (!int.TryParse(typeAndLengthOrFormat[1], out number)) throw new Exception($"Invalid number of characters in Random token {{random;<type>;<length>}}");
                result = new string(Enumerable.Repeat(select, number).Select(s => s[RandomGenerator.Next(s.Length)]).ToArray());
            }
            return result;
        }

        private static string DoDateToken(char delimiter, string OffsetAndFormat)
        {
            string[] offsetAndFormat = OffsetAndFormat.Split(new char[] { delimiter }, 2);

            if (offsetAndFormat.Length != 2)
            {
                throw new Exception("Date token does not have a format parameter; example: {date" + delimiter + "today" + delimiter + "dd-MM-yyyy}");
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

        //private static string DoSeleniumKey(string KeyName)
        //{
        //    try
        //    {
        //        //
        //        // Selenium keys are static fields in the WebDriver Keys class.
        //        //
        //        return (string)typeof(Keys).GetField(KeyName).GetValue(null);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new ArgumentException($"[{KeyName ?? "null!!"}] not found as a field in Selenium Keys class", "KeyName");
        //    }
        //}

        private static string DoFinancialYearToken(char delimiter, string DateToWorkFromAndFormat, bool Start)
        {
            string[] dateToWorkFromAndFormat = DateToWorkFromAndFormat.Split(new char[] { delimiter }, 2);


            DateTime dateToWorkFrom;
            if (!DateTime.TryParseExact(dateToWorkFromAndFormat[0], new string[] { "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy", "dd/MM/yy", "d/MM/yy", "dd/M/yy", "d/M/yy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateToWorkFrom))
            {
                throw new ArgumentException("Cannot parse date.  Must be in format d/M/y", "DateToWorkFromAndFormat (first element)");
            }

            string year;
            if (dateToWorkFrom.Month >= 7)
                year = Start ? dateToWorkFrom.Year.ToString() : (dateToWorkFrom.Year + 1).ToString();
            else
                year = Start ? (dateToWorkFrom.Year - 1).ToString() : dateToWorkFrom.Year.ToString();

            DateTime returnDate = DateTime.ParseExact((Start ? "01/07/" : "30/06/") + year, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return returnDate.ToString(dateToWorkFromAndFormat[1]);
        }

        static private float DoRandomFloat(string MaxAndMinFloats)
        {
            char delimiter = ',';
            string[] MaxAndMin = MaxAndMinFloats.Split(delimiter);
            if (MaxAndMin.Length != 2)
                throw new Exception($"Invalid Maximum and Minimum floats. Expect {{random.float(min;max),<format>}}. Max/min was: [{MaxAndMinFloats}]");
            float Min;
            float Max;

            if (!float.TryParse(MaxAndMin[0], out Min))
                throw new Exception($"Invalid Minimum float. Expect {{random.float(min;max),<format>}}. Max/min was: [{MaxAndMinFloats}]");
            if (!float.TryParse(MaxAndMin[1], out Max))
                throw new Exception($"Invalid Maximum float. Expect {{random.float(min;max),<format>}}. Max/min was: [{MaxAndMinFloats}]");

            return DoRandomFloat(Min, Max);
        }
        static public float DoRandomFloat(float MinFloat, float MaxFloat)
        {
            if (MinFloat >= MaxFloat)
                throw new Exception($"Maximum float less than Minimum float! Expect {{random.float(min,max),<format>}} Min = {MinFloat.ToString()}, Max = {MaxFloat.ToString()}");
            return (float)RandomGenerator.NextDouble() * (MaxFloat - MinFloat) + MinFloat;
        }

        static private DateTime DoRandomDate(string MaxAndMinDates)
        {
            char delimiter = ',';
            string[] MaxAndMin = MaxAndMinDates.Split(delimiter);
            if (MaxAndMin.Length != 2)
                throw new Exception($"Invalid Maximum and Minimum dates. Expect {{random;date(dd-MM-yyyy,dd-MM-yyyy);<format>}}. Max/min was: [{MaxAndMinDates}]");
            DateTime Min;
            DateTime Max;

            if (!DateTime.TryParseExact(MaxAndMin[0], "d-M-yyyy", CultureInfo.InstalledUICulture, DateTimeStyles.None, out Min))
                throw new Exception($"Invalid Minimum date. Expect {{random;date(dd-MM-yyyy,dd-MM-yyyy);<format>}}. Max/min was: [{MaxAndMinDates}]");
            if (!DateTime.TryParseExact(MaxAndMin[1], "d-M-yyyy", CultureInfo.InstalledUICulture, DateTimeStyles.None, out Max))
                throw new Exception($"Invalid Maximum date. Expect {{random;date(dd-MM-yyyy,dd-MM-yyyy);<format>}}. Max/min was: [{MaxAndMinDates}]");

            return DoRandomDate(Min, Max);
        }
        static public DateTime DoRandomDate(DateTime MinDate, DateTime MaxDate)
        {
            if (MinDate > MaxDate)
                throw new Exception($"Maximum date earlier than Maximum date! Expect {{random;date(dd-MM-yyyy,dd-MM-yyyy);<format>}} Mindate = {MinDate.ToString("dd/MM/yyyy")}, Maxdate = {MaxDate.ToString("dd/MM/yyyy")}");
            return MinDate.AddDays(RandomGenerator.Next((MaxDate - MinDate).Days));
        }
    }
}
