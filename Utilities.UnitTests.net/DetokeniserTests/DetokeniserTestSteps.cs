// <copyright file="DetokeniserTestSteps.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TechTalk.SpecFlow;
    using static TeamControlium.Utilities.Detokeniser;

    /// <summary>
    /// Test-step definitions for steps using/validating the Utilities Detokeniser class.
    /// </summary>
    [Binding]
    public sealed class DetokeniserTestSteps
    {
        /// <summary>
        /// Used to hold context information for current Scenario
        /// </summary>
        private readonly ScenarioContext scenarioContext;

        /// <summary>
        /// Initialises a new instance of the <see cref="DetokeniserTestSteps" /> class.
        /// Stores console output when redirected by any test steps.
        /// </summary>
        /// <param name="scenarioContext">Scenario context</param>
        public DetokeniserTestSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        /// <summary>
        /// Store String to be processed into the Scenario context to enable use and validation
        /// </summary>
        /// <param name="stringToBeProcessed">String to be processed to Detokeniser and then used in validation/s.</param>
        [Given(@"I have a string ""(.*)""")]
        public void GivenIHaveAStringWithToken(string stringToBeProcessed)
        {
            this.scenarioContext["StringToBeProcessed"] = stringToBeProcessed;
        }

        /// <summary>
        /// Call Detokeniser to process string placing result in Scenario context to enable later validation.
        /// Catch any exception and store in result for possible later validation
        /// </summary>
        [When(@"I process the token to a string")]
        public void WhenIProcessTheToken()
        {
            string stringToProcess = (string)this.scenarioContext["StringToBeProcessed"];
            string processed;

            try
            {
                processed = Detokenize(stringToProcess);
            }
            catch (Exception ex)
            {
                processed = ex.Message;
            }

            this.scenarioContext["ProcessedString"] = processed;
        }

        /// <summary>
        /// Validate that Detokeniser returned string matches the expected
        /// </summary>
        /// <param name="expectedString">Expected string</param>
        [Then(@"the string is ""(.*)""")]
        public void ThenTheStringIs(string expectedString)
        {
            Assert.AreEqual(expectedString, (string)this.scenarioContext["ProcessedString"], $"Verify Detokeniser processed string [{(string)this.scenarioContext["ProcessedString"]}] matches expected [{expectedString}]");
        }

        /// <summary>
        /// Verify Detokeniser returned string is todays date in the required format
        /// </summary>
        /// <param name="requiredFormatOfDate">Required format of date</param>
        [Then(@"the string is today's date in the format ""(.*)""")]
        public void ThenTheStringIsTodaySDateInTheFormat(string requiredFormatOfDate)
        {
            string requiredDate = DateTime.Now.ToString(requiredFormatOfDate);
            string actualDate = (string)this.scenarioContext["ProcessedString"];
            Assert.AreEqual(requiredDate, actualDate, "Dates and formats match");
        }

        /// <summary>
        /// Verify Detokeniser returned string is yesterdays date in the required format
        /// </summary>
        /// <param name="requiredFormatOfDate">Required format of date</param>
        [Then(@"the string is yesterday's date in the format ""(.*)""")]
        public void ThenTheStringIsYesterdaySDateInTheFormat(string requiredFormatOfDate)
        {
            string requiredDate = DateTime.Now.AddDays(-1).ToString(requiredFormatOfDate);
            string actualDate = (string)this.scenarioContext["ProcessedString"];
            Assert.AreEqual(requiredDate, actualDate, "Dates and formats match");
        }

        /// <summary>
        /// Verify Detokeniser returned string is tomorrows date in the required format
        /// </summary>
        /// <param name="requiredFormatOfDate">Required format of date</param>
        [Then(@"the string is tomorrows's date in the format ""(.*)""")]
        public void ThenTheStringIsTomorrowsDateInTheFormat(string requiredFormatOfDate)
        {
            string requiredDate = DateTime.Now.AddDays(1).ToString(requiredFormatOfDate);
            string actualDate = (string)this.scenarioContext["ProcessedString"];
            Assert.AreEqual(requiredDate, actualDate, "Dates and formats match");
        }

        /// <summary>
        /// Verify Detokeniser returned string is required date offset in the required format
        /// </summary>
        /// <param name="offset">Number of units of offset required</param>
        /// <param name="offsetType">Unit type of offset (IE. days, months or years)</param>
        /// <param name="requiredFormatOfDate">Required format of date</param>
        [Then(@"the string is the date (.*) ""(.*)"" in the format ""(.*)""")]
        public void ThenTheStringIsTheDateInTheFormat(int offset, string offsetType, string requiredFormatOfDate)
        {
            DateTime requiredDate;
            string actualDate = (string)this.scenarioContext["ProcessedString"];

            switch (offsetType.ToLower().Trim())
            {
                case "days":
                    requiredDate = DateTime.Now.AddDays(offset);
                    break;
                case "months":
                    requiredDate = DateTime.Now.AddMonths(offset);
                    break;
                case "years":
                    requiredDate = DateTime.Now.AddYears(offset);
                    break;
                default:
                    throw new ArgumentException("Unknown Offset Type. Expect days, months or years.", "offsetType");
            }

            Assert.AreEqual(requiredDate.ToString(requiredFormatOfDate), actualDate, "Dates and formats match");
        }

        /// <summary>
        /// Verify date is within the required maximum and minimum
        /// </summary>
        /// <param name="minDate">Minimum date Detokeniser date must be</param>
        /// <param name="maxDate">Maximum date Detokeniser date must be</param>
        [Then(@"the string is a date between ""(.*)"" and ""(.*)""")]
        public void ThenTheStringIsADateBetweenAnd(string minDate, string maxDate)
        {
            var processedString = (string)this.scenarioContext["ProcessedString"];
            var actualDate = DateTime.ParseExact(processedString, new string[] { "d/M/yy", "dd/M/yy", "d/MM/yy", "dd/MM/yy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
            var min = DateTime.ParseExact(minDate, "d/M/yyyy", CultureInfo.InvariantCulture);
            var max = DateTime.ParseExact(maxDate, "d/M/yyyy", CultureInfo.InvariantCulture);

            if (min > max)
            {
                throw new Exception($"Test error: Minimum date [{minDate}] is later than Maximum date [{maxDate}]!");
            }

            Assert.IsTrue((actualDate >= min) && (actualDate <= max));
        }

        /// <summary>
        /// Verify Detokeniser result matches required regular expression
        /// </summary>
        /// <param name="regExpPattern">Regular Expression pattern to match</param>
        [Then(@"the string matches regular expression ""(.*)""")]
        public void ThenTheStringIsAFormattedNumber(string regExpPattern)
        {
            var processedString = (string)this.scenarioContext["ProcessedString"];
            bool result = Regex.IsMatch(processedString, regExpPattern);
            Assert.IsTrue(result, string.Format("Processed string [{0}] matches regular expression [{1}]", processedString, regExpPattern));
        }

        /// <summary>
        /// Verify Detokeniser result is a number within the given limits
        /// </summary>
        /// <param name="minNumber">Minimum number result can be</param>
        /// <param name="maxNumber">Maximum number result can be</param>
        [Then(@"the string is a number between (.*) and (.*)")]
        public void ThenTheStringIsANumberBetweenAnd(float minNumber, float maxNumber)
        {
            var processedString = (string)this.scenarioContext["ProcessedString"];
            float num = float.Parse(processedString);
            Assert.IsTrue((num >= minNumber) && (num <= maxNumber));
        }

        /// <summary>
        /// Verify the Detokeniser result is correct number of characters and only selected from the required set
        /// </summary>
        /// <param name="numberOfCharacters">Number of characters Detokeniser result must be</param>
        /// <param name="possibleCharacters">Characters Detokeniser result must be selected from</param>
        [Then(@"the string is (.*) characters from ""(.*)""")]
        public void ThenTheStringIsCharacterFrom(int numberOfCharacters, string possibleCharacters)
        {
            var processedString = (string)this.scenarioContext["ProcessedString"];
            var seenInPossibleChars = true;

            Assert.AreEqual(numberOfCharacters, processedString.Length);

            foreach (char character in processedString)
            {
                if (possibleCharacters.IndexOf(character) == -1)
                {
                    Assert.IsTrue(false, $"Verify character [{character}] (in detokenizer result [{processedString}]) is from possible characters [{possibleCharacters}]");
                    seenInPossibleChars = false;
                    break;
                }
            }

            if (seenInPossibleChars)
            {
                Assert.IsTrue(true, $"All characters in detokenizer result [{processedString}] are from possible characters [{possibleCharacters}]");
            }
        }

        /// <summary>
        /// Verify Detokeniser result is a valid Australian TFN (Tax File Number)
        /// </summary>
        [Then(@"the string is a valid Australian TFN")]
        public void ThenTheStringIsAValidAustralianTFN()
        {
            int[] weights = { 1, 4, 3, 7, 5, 8, 6, 9 };
            int index;
            int sum = 0;
            int product;
            var processedString = (string)this.scenarioContext["ProcessedString"];

            for (index = 0; index < weights.Length; index++)
            {
                if (!int.TryParse(processedString[index].ToString(), out product))
                {
                    throw new Exception($"Processed string contain invalid TFN (at position {(index + 1)}) - expected only digits.  Got {processedString}");
                }

                sum += product * int.Parse(weights[index].ToString());
            }

            product = sum % 11;

            if (!int.TryParse(processedString[8].ToString(), out sum))
            {
                throw new Exception($"Processed string contain invalid TFN (at position 8) - expected only digits.  Got {processedString}");
            }

            Assert.AreEqual(product, sum, $"Verify TFN ({processedString}) check digit ({processedString[8]}) matches expected ({product})");
        }

        /// <summary>
        /// Verify the length of the Detokeniser result is one of the single digit lengths given
        /// </summary>
        /// <param name="possibleSingleDigitLengths">Possible lengths the ProcessedString context variable can be.  All single digit lengths.</param>
        [Then(@"the string length is one of \[(\d*)]")]
        public void ThenTheStringLengthIsOneOf(string possibleSingleDigitLengths)
        {
            int dummy;
            var processedString = (string)this.scenarioContext["ProcessedString"];
            var processedStringLength = processedString.Length;
            if (!int.TryParse(possibleSingleDigitLengths, out dummy))
            {
                throw new Exception($"Possible Lengths ({possibleSingleDigitLengths}) contains non-numerics! Check test step");
            }

            Assert.IsTrue(possibleSingleDigitLengths.ToList().Any(len => processedStringLength == int.Parse(len.ToString())), $"Processed string [{processedString}] length is one of [{possibleSingleDigitLengths}]");
        }
    }
}