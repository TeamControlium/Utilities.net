using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;

namespace TeamControlium.Utilities
{
    [Binding]
    public sealed class DetokenizerTestSteps
    {
        [Given(@"I have a string ""(.*)""")]
        public void GivenIHaveAStringWithToken(string stringToBeProcessed)
        {
            ScenarioContext.Current["StringToBeProcessed"] = stringToBeProcessed;
        }

        [When(@"I process the token to a string")]
        public void WhenIProcessTheToken()
        {
            string toBeProcessed = (string)ScenarioContext.Current["StringToBeProcessed"];
            string processed;
            try
            {
                processed = Detokenizer.ProcessTokensInString(toBeProcessed);
            }
            catch (Exception ex)
            {
                processed = ex.Message;
            }
            ScenarioContext.Current["ProcessedString"] = processed;
        }

        [Then(@"the string is ""(.*)""")]
        public void ThenTheStringIs(string expectedString)
        {
            Assert.AreEqual(expectedString, (string)ScenarioContext.Current["ProcessedString"]);
        }

        [Then(@"the string is today's date in the format ""(.*)""")]
        public void ThenTheStringIsTodaySDateInTheFormat(string requiredFormatOfDate)
        {
            string requiredDate = DateTime.Now.ToString(requiredFormatOfDate);
            string actualDate = (string)ScenarioContext.Current["ProcessedString"];
            Assert.AreEqual(requiredDate, actualDate, "Dates and formats match");
        }

        [Then(@"the string is yesterday's date in the format ""(.*)""")]
        public void ThenTheStringIsYesterdaySDateInTheFormat(string requiredFormatOfDate)
        {
            string requiredDate = DateTime.Now.AddDays(-1).ToString(requiredFormatOfDate);
            string actualDate = (string)ScenarioContext.Current["ProcessedString"];
            Assert.AreEqual(requiredDate, actualDate, "Dates and formats match");
        }

        [Then(@"the string is tomorrows's date in the format ""(.*)""")]
        public void ThenTheStringIsTomorrowsDateInTheFormat(string requiredFormatOfDate)
        {
            string requiredDate = DateTime.Now.AddDays(1).ToString(requiredFormatOfDate);
            string actualDate = (string)ScenarioContext.Current["ProcessedString"];
            Assert.AreEqual(requiredDate, actualDate, "Dates and formats match");
        }

        [Then(@"the string is the date (.*) ""(.*)"" in the format ""(.*)""")]
        public void ThenTheStringIsTheDateInTheFormat(int offset, string offsetType, string requiredFormatOfDate)
        {
            DateTime requiredDate;
            switch (offsetType.ToLower().Trim())
            {
                case "days":
                    requiredDate = DateTime.Now.AddDays(offset); break;
                case "months":
                    requiredDate = DateTime.Now.AddMonths(offset); break;
                case "years":
                    requiredDate = DateTime.Now.AddYears(offset); break;
                default:
                    throw new ArgumentException("Unknown Offset Type. Expect days, months or years.", "offsetType");
            }
            string actualDate = (string)ScenarioContext.Current["ProcessedString"];
            Assert.AreEqual(requiredDate.ToString(requiredFormatOfDate), actualDate, "Dates and formats match");

        }

        [Then(@"the string is a date between ""(.*)"" and ""(.*)""")]
        public void ThenTheStringIsADateBetweenAnd(string minDate, string maxDate)
        {
            var processedString = (string)ScenarioContext.Current["ProcessedString"];
            var actualDate = DateTime.ParseExact(processedString, new string[] { "d/M/yy", "dd/M/yy", "d/MM/yy", "dd/MM/yy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
            var min = DateTime.ParseExact(minDate, "d/M/yyyy", CultureInfo.InvariantCulture);
            var max = DateTime.ParseExact(maxDate, "d/M/yyyy", CultureInfo.InvariantCulture);

            if (min > max) throw new Exception($"Minimum date [{minDate}] is later than Maximum date [{maxDate}]!");

            Assert.IsTrue((actualDate >= min) && (actualDate <= max));



        }

        [Then(@"the string is a ""(.*)"" number between ""(.*)"" and ""(.*)""")]
        public void ThenTheStringIsANumberBetweenAnd(string format, int minNumber, int maxNumber)
        {
            var processedString = (string)ScenarioContext.Current["ProcessedString"];

        }
        [Then(@"the string matches regular expression ""(.*)""")]
        public void ThenTheStringIsAFormattedNumber(string format)
        {
            var processedString = (string)ScenarioContext.Current["ProcessedString"];
            bool result = Regex.IsMatch(processedString, format);
            Assert.IsTrue(result, string.Format("Processed string [{0}] matches regular expression [{1}]", processedString, format));
        }

        [Then(@"the string is a number between (.*) and (.*)")]
        public void ThenTheStringIsANumberBetweenAnd(float minNumber, float maxNumber)
        {
            var processedString = (string)ScenarioContext.Current["ProcessedString"];
            float num = float.Parse(processedString);
            Assert.IsTrue((num >= minNumber) && (num <= maxNumber));
        }

        [Then(@"the string is (.*) characters from ""(.*)""")]
        public void ThenTheStringIsCharacterFrom(int numberOfCharacters, string possibleCharacters)
        {
            var processedString = (string)ScenarioContext.Current["ProcessedString"];

            Assert.AreEqual(numberOfCharacters, processedString.Length);

            foreach (char character in processedString)
            {
                // Need to populate this up....
            }

        }


    }
}
