using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace TeamControlium.Utilities
{
    /// <summary>
    /// Test stuff
    /// </summary>
    [Binding]
    public sealed class LoggerTestSteps
    {
        private StringWriter consoleOut = new StringWriter();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writeToConsole"></param>
        [Given(@"I have configured Logger WriteToConsole to (false|true)")]
        [When(@"I change Logger WriteToConsole to (false|true)")]
        public void GivenIHaveConfiguredLoggerToWriteToConsole(bool writeToConsole)
        {
            Console.SetOut(consoleOut);
            Logger.WriteToConsole = writeToConsole;
        }

        /// <summary>
        /// 
        /// </summary>
        [Given(@"I have configured Logger TestToolLog to write to my string")]
        public void GivenIHaveConfiguredLoggerToWriteToAString()
        {
            Logger.TestToolLog = (s) => { ScenarioContext.Current.Add("LoggerOutputString", s); };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logLevel"></param>
        [Given(@"I set Logger to level (.*)")]
        public void WhenISetLoggerToLevel(Logger.LogLevels logLevel)
        {
            Logger.LoggingLevel = logLevel;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="stringToWrite"></param>
        [Given(@"I call Logger with level (.*) and string ""(.*)""")]
        [When(@"I call Logger with level (.*) and string ""(.*)""")]
        public void WhenICallLoggerWithLevelAndString(Logger.LogLevels logLevel, string stringToWrite)
        {
            Logger.WriteLine(logLevel, stringToWrite);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expectedToEndWith"></param>
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
            { }
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
            { }
            Assert.AreEqual(expectedString != null, outputString?.EndsWith(expectedString) ?? false, $"Console output had [{expectedString ?? "No logger output written"}] written to it: {outputString ?? "No logger output written"} ");
        }

    }
}
