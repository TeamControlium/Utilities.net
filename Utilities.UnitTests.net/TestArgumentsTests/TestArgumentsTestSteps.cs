// <copyright file="TestArgumentsTestSteps.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TeamControlium.Utilities;
    using TechTalk.SpecFlow;
    using static TeamControlium.Utilities.Detokeniser;
    using static TeamControlium.Utilities.Log;
    using static TeamControlium.Utilities.Repository;

    /// <summary>
    /// Test-step definitions for steps using/validating the Utilities Repository class.
    /// </summary>
    [Binding]
    public sealed class TestArgumentsTestSteps
    {
        /// <summary>
        /// Used to hold context information for current Scenario
        /// </summary>
        private readonly ScenarioContext scenarioContext;

        /// <summary>
        /// Initialises a new instance of the <see cref="TestArgumentsTestSteps" /> class.
        /// Stores console output when redirected by any test steps.
        /// </summary>
        /// <param name="scenarioContext">Scenario context</param>        
        public TestArgumentsTestSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        /// <summary>
        /// Called by Specflow at start of TestArgument feature.  Ensures framework debugging to allow testing of Repository logging
        /// </summary>
        [BeforeFeature]
        public static void BeforeFeature()
        {
            LoggingCurrentLevel = LogLevels.FrameworkDebug;
        }

        /// <summary>
        /// Apply the given command-line to the <code>TestArguments</code> processor
        /// </summary>
        /// <param name="sampleCommandLine">Command-line to use</param>
        [Given(@"I have the following command line ""(.*)""")]
        public void GivenIHaveTheFollowingCommandLine(string sampleCommandLine)
        {
            string[] args = sampleCommandLine.Split(' ');
            TestArguments.Load(args);
        }

        /// <summary>
        /// Verify <code>TestArguments</code> processor returns expected text for given argument name
        /// </summary>
        /// <param name="expectedData">Data expected to be returned</param>
        /// <param name="argName">Name of argument to get</param>
        [Then(@"TestArguments has the data ""(.*)"" for argument ""(.*)""")]
        public void ThenTestArgumentsHasTheDataForArgument(string expectedData, string argName)
        {
            string actualArgData = TestArguments.GetParam(argName);
            Assert.AreEqual(expectedData, actualArgData, $"Expect argument [{argName}] equal to [{expectedData}] - Actual [{actualArgData}]");
        }

        /// <summary>
        /// Verify <code>TestArguments</code> processor returns null for given argument name
        /// </summary>
        /// <param name="argName">Name of argument to get</param>
        [Then(@"TestArguments returns null for argument ""(.*)""")]
        public void ThenTestArgumentsRetrunsNullForArgument(string argName)
        {
            string actualArgData = TestArguments.GetParam(argName);
            Assert.AreEqual(null, actualArgData, $"Expect argument [{argName}] to be null - Actual [{actualArgData}]");
        }
    }
}