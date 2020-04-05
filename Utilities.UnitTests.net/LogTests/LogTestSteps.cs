// <copyright file="LogTestSteps.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TechTalk.SpecFlow;
    using static TeamControlium.Utilities.Log;

    /// <summary>
    /// Test-step definitions for steps using/validating the Utilities Log class.
    /// </summary>
    [Binding]
    public sealed class LogTestSteps
    {
        /// <summary>
        /// Used to hold context information for current Scenario
        /// </summary>
        private readonly ScenarioContext scenarioContext;

        /// <summary>
        /// Stores console output when redirected by any test steps.
        /// </summary>
        private StringWriter consoleOut = new StringWriter();

        /// <summary>
        /// Initialises a new instance of the <see cref="LogTestSteps" /> class.
        /// Used by Specflow supply Scenario context information.
        /// </summary>
        /// <param name="context">Scenario context data for use by class methods</param>
        public LogTestSteps(ScenarioContext context)
        {
            this.scenarioContext = context;
        }

        /// <summary>
        /// Specflow step to set Log LogToConsole flag as required by test.
        /// </summary>
        /// <param name="writeToConsole">Flag (true/false) setting Log LogToConsole. When true, log writes to Console stdout, when false it does not.</param>
        [Given(@"I have configured Log LogToConsole to (false|true)")]
        [When(@"I change Log LogToConsole to (false|true)")]
        public void GivenIHaveConfiguredLogToLogToConsole(bool writeToConsole)
        {
            LogToConsole = writeToConsole;
        }

        /// <summary>
        /// Sets Log LogOutputDelegate delegate to configured (Log writes output to configured receiver) or not configured (LogOutputDelegate set to null)
        /// </summary>
        /// <param name="configuredOrNotConfigured">"Configured" (Receiver setup) or "Not Configured" (no Receiver)</param>
        [Given(@"I have (.*) Log LogOutputDelegate delegate")]
        [When(@"I have (.*) Log LogOutputDelegate delegate")]
        public void GivenIHaveConfiguredLogLogOutputDelegateDelegate(string configuredOrNotConfigured)
        {
            if (configuredOrNotConfigured.ToLower().Trim('"').StartsWith("not"))
            {
                LogOutputDelegate = null;
            }
            else
            {
                LogOutputDelegate = (s) =>
                {
                    scenarioContext.Add("LogOutputReceiver", s);
                };
            }
        }

        /// <summary>
        /// Sets Log logging level to required setting,
        /// </summary>
        /// <param name="logLevel">Required level of logging (<see cref="Log.LogLevels"/>)</param>
        [Given(@"I set Log to level (.*)")]
        public void WhenISetLogToLevel(LogLevels logLevel)
        {
            LoggingCurrentLevel = logLevel;
        }

        /// <summary>
        /// Log.LogWriteLine is called with required logging level (<see cref="Log.LogLevels"/>) and text
        /// </summary>
        /// <remarks>Console STDOUT is set to consoleOut (scenario StringWriter type) for duration of LogWriteLine call.</remarks>
        /// <param name="logLevel">Logging level to call LogWriteLine with (<see cref="Log.LogLevels"/>)</param>
        /// <param name="stringToWrite">Text to write to log</param>
        [Given(@"I call Log.WriteLine with level (.*) and string ""(.*)""")]
        [When(@"I call Log.WriteLine with level (.*) and string ""(.*)""")]
        public void WhenICallLogWithLevelAndString(LogLevels logLevel, string stringToWrite)
        {
            Console.SetOut((StringWriter)this.scenarioContext["consoleOut"]);
            LogWriteLine(logLevel, stringToWrite);
            var sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);
        }

        /// <summary>
        /// Verifies Console Output contains a line of text starting and ending with required text. 
        /// If both empty expect Console Output to contain no lines of text.
        /// </summary>
        /// <param name="expectedToStartWith">Text matching Console stdout line must start with. Empty string matches any.</param>
        /// <param name="expectedToEndWith">Text matching Console stdout line must end with. Empty string matches any.</param>
        [Then(@"the console stdout contains a line starting withstring.Empty(.*)"" and ending withstring.Empty(.*)""")]
        public void ThenTheConsoleWrittenToByLogShouldEndWith(string expectedToStartWith, string expectedToEndWith)
        {
            var consoleOutput = new List<string>();
            var outputString = string.Empty;
            var expectedStartString = string.IsNullOrEmpty(expectedToStartWith) ? string.Empty : expectedToStartWith;
            var expectedEndString = string.IsNullOrEmpty(expectedToEndWith) ? string.Empty : expectedToEndWith;
            try
            {
                consoleOutput = ((StringWriter)this.scenarioContext["consoleOut"]).ToString().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                outputString = consoleOutput.Find(x => (x.StartsWith(expectedToStartWith) && x.EndsWith(expectedToEndWith))) ?? string.Empty;
            }
            catch
            {
                // We ignore any error as we are just interested in the Console output text.
            }

            string testMessage;
            if (expectedStartString == string.Empty && expectedEndString == string.Empty)
            {
                testMessage = $"Verify Console stdio ({consoleOutput.Count} lines received) contains Zero (0) lines of text";
            }
            else
            {
                testMessage = $"Verify Console stdio ({(consoleOutput.Count == 0 ? "0 lines received" : outputString)}) contains a line of text starting with [{(expectedStartString == string.Empty?"<Any text>":expectedStartString)}] and ending with [{(expectedEndString == string.Empty ? "<Any text>" : expectedEndString)}]";
            }

            Assert.IsTrue((outputString == string.Empty) == (expectedStartString == string.Empty && expectedEndString == string.Empty), testMessage);
        }

        /// <summary>
        /// Verifies Log delegated receiver contains a line of text starting and ending with required text. 
        /// If both empty expect delegated receiver to contain no lines of text.
        /// </summary>
        /// <param name="expectedToStartWith">Text matching Log Receiver line must start with. Empty string matches any.</param>
        /// <param name="expectedToEndWith">Text matching Log Receiver line must end with. Empty string matches any.</param>
        [Then(@"Log text receiver contains a line starting withstring.Empty(.*)"" and ending withstring.Empty(.*)""")]
        public void ThenTheStringWrittenToByLogShouldEndWith(string expectedToStartWith, string expectedToEndWith)
        {
            var receiverLines = new List<string>();
            var matchingLine = string.Empty;
            var expectedStartString = string.IsNullOrEmpty(expectedToStartWith) ? string.Empty : expectedToStartWith;
            var expectedEndString = string.IsNullOrEmpty(expectedToEndWith) ? string.Empty : expectedToEndWith;
            try
            {
                receiverLines = ((string)this.scenarioContext["LogOutputReceiver"]).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                matchingLine = receiverLines.Find(x => (x.StartsWith(expectedToStartWith) && x.EndsWith(expectedToEndWith))) ?? string.Empty;
            }
            catch
            {
                // We ignore any error as we are just interested in the Log Receiver text.
            }

            string testMessage;
            if (expectedStartString == string.Empty && expectedEndString == string.Empty)
            {
                testMessage = $"Verify Log receiver delegate (Actual: {receiverLines.Count} lines received) received Zero (0) lines of text";
            }
            else
            {
                testMessage = $"Verify Log receiver delegate (Actual: [{(receiverLines.Count == 0 ? "0 lines received" : matchingLine)}]) received a line of text starting with [{(expectedStartString == string.Empty ? "<Any text>" : expectedStartString)}] and ending with [{(expectedEndString == string.Empty ? "<Any text>" : expectedEndString)}]";
            }

            Assert.IsTrue((matchingLine == string.Empty) == (expectedStartString == string.Empty && expectedEndString == string.Empty), testMessage);
        }
    }
}