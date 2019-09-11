using System;
using System.Linq;
using TechTalk.SpecFlow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace TeamControlium.Utilities
{
    [Binding]
    public sealed class LoggerTestSteps
    {
        private StringWriter consoleOut = new StringWriter();

        [Given(@"I have configured Logger WriteToConsole to (false|true)")]
        [When(@"I change Logger WriteToConsole to (false|true)")]
        public void GivenIHaveConfiguredLoggerToWriteToConsole(bool writeToConsole)
        {
            Console.SetOut(consoleOut);
            Logger.WriteToConsole = writeToConsole;
        }

        [Given(@"I have configured Logger TestToolLog to write to my string")]
        public void GivenIHaveConfiguredLoggerToWriteToAString()
        {
            Logger.TestToolLog = (s) => { ScenarioContext.Current.Add("LoggerOutputString", s); };
        }

        [Given(@"I set Logger to level (.*)")]
        public void WhenISetLoggerToLevel(Logger.LogLevels logLevel)
        {
            Logger.LoggingLevel = logLevel;
        }

        [Given(@"I call Logger with level (.*) and string ""(.*)""")]
        [When(@"I call Logger with level (.*) and string ""(.*)""")]
        public void WhenICallLoggerWithLevelAndString(Logger.LogLevels logLevel, string stringToWrite)
        {
            Logger.WriteLine(logLevel, stringToWrite);
        }

        [Then(@"the console output contains a Logger line ending with ""(.*)""")]
        public void ThenTheConsoleWrittenToByLoggerShouldEndWith(string expectedToEndWith)
        {
            var outputString = default(string);
            var expectedString = string.IsNullOrEmpty(expectedToEndWith) ? default(string) : expectedToEndWith;
            try
            {
                var consoleOutput = consoleOut.ToString().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                outputString = consoleOutput.Find(x => x.StartsWith("LOG-"));
            }
            catch
            {
                //
            }
            Assert.AreEqual(expectedString != null, outputString?.EndsWith(expectedString) ?? false, $"Console output had [{expectedString ?? "No logger output written"}] written to it: {outputString ?? "No logger output written"} ");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expectedToEndWith"></param>
        [Then(@"my string contains a Logger line ending with ""(.*)""")]
        public void ThenTheStringWrittenToByLoggerShouldEndWith(string expectedToEndWith)
        {
            var outputString = default(string);
            var expectedString = string.IsNullOrEmpty(expectedToEndWith) ? default(string) : expectedToEndWith;
            try
            {
                outputString = (string)ScenarioContext.Current["LoggerOutputString"];
            }
            catch
            {
                //
            }
            Assert.AreEqual(expectedString != null, outputString?.EndsWith(expectedString) ?? false, $"Console output had [{expectedString ?? "No logger output written"}] written to it: {outputString ?? "No logger output written"} ");
        }
    }
}