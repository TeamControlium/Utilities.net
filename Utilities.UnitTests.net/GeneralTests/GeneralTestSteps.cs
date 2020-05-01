// <copyright file="GeneralTestSteps.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.UnitTests
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TechTalk.SpecFlow;
    using static TeamControlium.Utilities.Detokeniser;
    using static TeamControlium.Utilities.General;
    using static TeamControlium.Utilities.Log;

    /// <summary>
    /// Test-step definitions for steps using/validating the Utilities Repository class.
    /// </summary>
    [Binding]
    public sealed class GeneralTestSteps
    {
        /// <summary>
        /// Filename prefix we will use.  Means we can clear Temp folder of test files before each test.  Low risk of killing someone else...
        /// </summary>
        private static readonly string TestFileTempate = "GTS_{0}.TEST";

        /// <summary>
        /// Used to hold context information for current Scenario
        /// </summary>
        private readonly ScenarioContext scenarioContext;

        /// <summary>
        /// Initialises a new instance of the <see cref="GeneralTestSteps" /> class.
        /// Stores console output when redirected by any test steps.
        /// </summary>
        /// <param name="scenarioContext">Scenario context</param>        
        public GeneralTestSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        /// <summary>
        /// Called by Specflow at start of Repository feature.  Ensures framework debugging to allow testing of Repository logging
        /// </summary>
        [BeforeFeature]
        public static void BeforeFeature()
        {
            LoggingCurrentLevel = LogLevels.FrameworkDebug;
        }

        /// <summary>
        /// Called by Specflow at start of each scenario.  Ensure all Test files have been removed prior to execution.  Saves a build-up...
        /// </summary>
        [BeforeScenario]
        [Scope(Feature = "General Tests")]
        public static void BeforeScenario()
        {
            string path = Path.GetTempPath();

            foreach (string filename in Directory.EnumerateFiles(Path.GetTempPath(), string.Format(TestFileTempate, "*")))
            {
                File.Delete(filename);
            }
        }

        /// <summary>
        /// Creates a unique filename and puts it in the Specflow data repository
        /// </summary>
        [Given(@"I have a unique filename")]
        public void GivenIHaveAUniqueFilename()
        {
            this.scenarioContext["Filename"] = Path.Combine(Path.GetTempPath(), string.Format(TestFileTempate, Detokenize("{random;digits;5}")));
        }

        /// <summary>
        /// Store URL in context data
        /// </summary>
        /// <param name="url">URL to store in Specflow context</param>
        [Given(@"I have a URL ""(.*)""")]
        public void GivenIHaveAURL(string url)
        {
            this.scenarioContext["URL"] = url;
        }

        /// <summary>
        /// Write data to required file.  If file exists overwrite
        /// </summary>
        /// <param name="data">Data to write to file</param>
        [When(@"I write data ""(.*)"" to the file, overwriting if file exists")]
        public void WhenIWriteDataToTheFile(string data)
        {
            WriteTextToFile((string)this.scenarioContext["Filename"], WriteMode.Overwrite, data);
        }

        /// <summary>
        /// Write data to required file.  If file exists append the data
        /// </summary>
        /// <param name="data">Data to write to file</param>
        [When(@"I write data ""(.*)"" to the file, appending if file exists")]
        public void WhenIWriteDataToTheFileAppendingIfFileExists(string data)
        {
            WriteTextToFile((string)this.scenarioContext["Filename"], WriteMode.Append, data);
        }

        /// <summary>
        /// Write data to required file.  If file exists create a new versioned file and write to that
        /// </summary>
        /// <param name="data">Data to write to file</param>
        [When(@"I write data ""(.*)"" to the file, creating new file if file already exists")]
        public void WhenIWriteDataToTheFileCreatingNewFileIfFileAlreadyExists(string data)
        {
            WriteTextToFile((string)this.scenarioContext["Filename"], WriteMode.AutoVersion, data);
        }

        /// <summary>
        /// Call ConvertURLToValidFilename to convert a URL to a filename
        /// </summary>
        [When(@"I convert URL to a filename")]
        public void WhenIConvertURLToAFilename()
        {
            this.scenarioContext["Filename"] = ConvertURLToValidFilename((string)this.scenarioContext["URL"]);
        }

        /// <summary>
        /// Verify the file contains the expected text
        /// </summary>
        /// <param name="expectedData">Expected file data</param>
        [Then(@"the file contains the text ""(.*)""")]
        public void ThenTheFileContainsTheText(string expectedData)
        {
            Assert.IsTrue(File.Exists((string)this.scenarioContext["Filename"]), $"Verify file [{(string)this.scenarioContext["Filename"]}] exists");

            string fileContents = File.ReadAllText((string)this.scenarioContext["Filename"]);
            Assert.AreEqual(expectedData, fileContents, $"Verify file [{(string)this.scenarioContext["Filename"]}] contains text [{expectedData}].  Actual [{fileContents}]");
        }

        /// <summary>
        /// Verify the versioned file contains expected data
        /// </summary>
        /// <param name="version">Version index</param>
        /// <param name="expectedData">Expected file data</param>
        [Then(@"the file \(version (\d*)\) contains the text ""(.*)""")]
        public void ThenTheFileVersionContainsTheText(int version, string expectedData)
        {
            string fileNameOnly = Path.GetFileNameWithoutExtension((string)this.scenarioContext["Filename"]);
            string extension = Path.GetExtension((string)this.scenarioContext["Filename"]);
            string path = Path.GetDirectoryName((string)this.scenarioContext["Filename"]);

            string fileNameWithVersion = Path.Combine(path, fileNameOnly + "(" + version.ToString() + ")" + extension);

            Assert.IsTrue(File.Exists(fileNameWithVersion), $"Verify file [{fileNameWithVersion}] exists");

            string fileContents = File.ReadAllText(fileNameWithVersion);
            Assert.AreEqual(expectedData, fileContents, $"Verify file [{fileNameWithVersion}] contains text [{expectedData}].  Actual [{fileContents}]");
        }

        /// <summary>
        /// Verifies scenario Filename matches expected text
        /// </summary>
        /// <param name="expectedFilename">Expected filename</param>
        [Then(@"Filename is ""(.*)""")]
        public void ThenFilenameIs(string expectedFilename)
        {
            string actualFilename = (string)this.scenarioContext["Filename"];
            Assert.AreEqual(expectedFilename, actualFilename, $"Expected filename [{expectedFilename}] matches actual [{actualFilename}]");
        }
    }
}