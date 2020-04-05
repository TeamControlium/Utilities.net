// <copyright file="RepositoryTestSteps.cs" company="TeamControlium Contributors">
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
    public sealed class RepositoryTestSteps
    {
        /// <summary>
        /// Used to hold context information for current Scenario
        /// </summary>
        private readonly ScenarioContext scenarioContext;

        /// <summary>
        /// Initialises a new instance of the <see cref="RepositoryTestSteps" /> class.
        /// Stores console output when redirected by any test steps.
        /// </summary>
        /// <param name="scenarioContext">Scenario context</param>        
        public RepositoryTestSteps(ScenarioContext scenarioContext)
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
        /// Called by Specflow at start of each scenario.  Ensure Repository is cleared and wires up <see cref="ProcessCustomDetokeniser(char, string[])"/> to enable tests
        /// to obtain correct thread ID's when executing in parallel.
        /// </summary>
        [BeforeScenario]
        [Scope(Feature = "Repository Tests")]
        public static void BeforeScenario()
        {
            ClearRepositoryAll();
            CustomTokenProcessor = ProcessCustomDetokeniser;
        }

        /// <summary>
        /// Processes 'repository' tokens to enable Specflow tests to address local repository threadID without knowing the current thread's ThreadID..  IE.  Test may state 'Given i am in thread ID {repository;threadID}'
        /// which will get processed to 'Given i am in thread ID 6' (if thread id is 6).
        /// </summary>
        /// <param name="delimiter">Detokeniser indicates what character is used at token delimited</param>
        /// <param name="token">Array of full token, split using the delimiter character</param>
        /// <returns>Processed token</returns>
        public static string ProcessCustomDetokeniser(char delimiter, string[] token)
        {
            string returnString = null;

            if (token[0] == "repository")
            {
                switch (token[1].ToLower().Trim())
                {
                    case "threadid":
                        {
                            returnString = Thread.CurrentThread.ManagedThreadId.ToString();
                        }

                        break;
                    default:
                        throw new Exception($"Unknown repository token expression [{token[1]}].  Expected 'ThreadID'");
                }
            }

            return returnString;
        }

        /// <summary>
        /// Clones Global to Local repository.  No exception can be thrown.
        /// </summary>
        /// <param name="blankOrNot">Indicates if items can be overwritten (Empty string allows overwrite, otherwise not)</param>
        [Given(@"I clone Global test data to Local test data, ((?:not ){0,1})overwriting any existing")]
        [When(@"I clone Global test data to Local test data, ((?:not ){0,1})overwriting any existing")]
        public void GivenICloneGlobalTestDataToLocalTestDataOverwritingAnyExisting(string blankOrNot)
        {
            TryCloneGlobalToLocal(blankOrNot.Trim().ToLower() == string.Empty);
        }

        /// <summary>
        /// Saves string to Global or Local repository with the given Category and Item name/s. String value also saved to Scenario context (with given index) to enable validation
        /// in subsequent steps.
        /// </summary>
        /// <param name="item">String to save</param>
        /// <param name="savedItemIndex">Index of item when saved in Specflow repository. Saves with the label Saved-n - where 'n' is the index given</param>
        /// <param name="localOrGlobal">Local or Global repository</param>
        /// <param name="categoryName">Name of category to save in repository</param>
        /// <param name="itemName">Name of item in category</param>
        [Given(@"I have saved string ""(.*)"" \(Item (.*)\) in Repository (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        [When(@"I have saved string ""(.*)"" \(Item (.*)\) in Repository (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        public void GivenIHaveSavedStringInRepositoryLocalCategoryItemName(string item, int savedItemIndex, string localOrGlobal, string categoryName, string itemName)
        {
            this.scenarioContext[$"Saved-{savedItemIndex}"] = item;
            if (localOrGlobal.ToLower().Trim() == "local")
            {
                ItemLocal[categoryName, itemName] = item;
            }
            else if (localOrGlobal.ToLower().Trim() == "global")
            {
                ItemGlobal[categoryName, itemName] = item;
            }
            else
            {
                Assert.Inconclusive($"Unrecognised GlobalDataItem ({ItemGlobal}).  Should be Thread or Global to denote test data repo");
            }
        }

        /// <summary>
        /// Saves integer to Global or Local repository with the given Category and Item name/s. Integer value also saved to Scenario context (with given index) to enable validation
        /// in subsequent steps.
        /// </summary>
        /// <param name="item">Integer to save</param>
        /// <param name="savedItemIndex">Index of item when saved in Specflow repository. Saves with the label Saved-n - where 'n' is the index given</param>
        /// <param name="localOrGlobal">Local or Global repository</param>
        /// <param name="categoryName">Name of category to save in repository</param>
        /// <param name="itemName">Name of item in category</param>
        [Given(@"I have saved integer (.*) \(Item (.*)\) in Repository (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        [When(@"I have saved integer (.*) \(Item (.*)\) in Repository (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        public void GivenIHaveSavedIntegerInRepositoryLocalCategoryItemName(int item, int savedItemIndex, string localOrGlobal, string categoryName, string itemName)
        {
            this.scenarioContext[$"Saved-{savedItemIndex}"] = item;
            if (localOrGlobal.ToLower().Trim() == "local")
            {
                ItemLocal[categoryName, itemName] = item;
            }
            else if (localOrGlobal.ToLower().Trim() == "global")
            {
                ItemGlobal[categoryName, itemName] = item;
            }
            else
            {
                Assert.Inconclusive($"Unrecognised GlobalDataItem ({ItemGlobal}).  Should be Thread or Global to denote test data repo");
            }
        }

        /// <summary>
        /// Recalls named data item from Global or Local repository's Category and Item name/s and saves to Specflow repository with given index.
        /// </summary>
        /// <param name="recalledItemIndex">Index of item when saved in Specflow repository. Saves with the label Recalled-n - where 'n' is the index given</param>
        /// <param name="localOrGlobal">Local or Global repository</param>
        /// <param name="categoryName">Name of category to recall from in repository</param>
        /// <param name="itemName">>Name of item in category</param>
        [When(@"I recall \(Item (.*)\) from (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        public void WhenIRecallStringItemFromCategoryItemName(int recalledItemIndex, string localOrGlobal, string categoryName, string itemName)
        {
            bool isLocal = localOrGlobal.ToLower().Trim() == "local";
            bool success = false;
            dynamic value = null;
            if (isLocal)
            {
                success = TryGetItemLocal(categoryName, itemName, out value);
            }
            else if (localOrGlobal.ToLower().Trim() == "global")
            {
                success = TryGetItemGlobal(categoryName, itemName, out value);
            }

            if (success)
            {
                this.scenarioContext[$"Recalled-{recalledItemIndex}"] = value;
            }
            else
            {
                this.scenarioContext[$"Recalled-{recalledItemIndex}"] = RepositoryLastTryException();
            }
        }

        /// <summary>
        /// Recalls named data item from Global or Local repository's Category and Item name/s, ensuring type is as required, and saves to Specflow repository with given index.  If Type is different
        /// (or any other exception is thrown during recall) value saved to Specflow repository with given index is the exception text.
        /// </summary>
        /// <param name="recalledItemIndex">Index of item when saved in Specflow repository. Saves with the label Recalled-n - where 'n' is the index given</param>
        /// <param name="localOrGlobal">Local or Global repository</param>
        /// <param name="categoryName">Name of category to recall from in repository</param>
        /// <param name="itemName">>Name of item in category</param>
        /// <param name="requiredType">Required type of data being recalled</param>
        [When(@"I recall \(Item (.*)\) from (?i)(local|global), Category ""(.*)"", Item Name ""(.*)"" as a \[""(.*)""]")]
        public void WhenIRecallStringItemFromLocalCategoryItemName(int recalledItemIndex, string localOrGlobal, string categoryName, string itemName, string requiredType)
        {
            bool isLocal = localOrGlobal.ToLower().Trim() == "local";
            bool success = false;
            dynamic value = null;
            object[] parameters = new object[] { categoryName, itemName, null };
            var mi = typeof(Repository).GetMethods().Where(method => (method.Name == (isLocal ? "TryGetItemLocal" : "TryGetItemGlobal") && method.IsGenericMethod == true && method.GetParameters().Length == 3));
            var fooRef = mi.First().MakeGenericMethod(Type.GetType(requiredType));
            success = (bool)fooRef.Invoke(null, parameters);
            
            if (success)
            {
                value = (dynamic)parameters[2];
            }

            if (success)
            {
                this.scenarioContext[$"Recalled-{recalledItemIndex}"] = value;
            }
            else
            {
                this.scenarioContext[$"Recalled-{recalledItemIndex}"] = RepositoryLastTryException();
            }
        }

        /// <summary>
        /// Clears Global or Local repository using ClearRepositoryLocal()/ClearRepositoryGlobal() methods
        /// </summary>
        /// <param name="localOrGlobal">Global or Local repository</param>
        [When(@"I clear the (?i)(local|global) repository")]
        public void WhenIClearTheLocalTestData(string localOrGlobal)
        {
            if (localOrGlobal.ToLower().Trim() == "local")
            {
                ClearRepositoryLocal();
            }
            else
            {
                ClearRepositoryGlobal();
            }
        }

        /// <summary>
        /// Validates the Specflow 'Recalled-n' value matches the Specflow 'Saved-m' - where n and m are the Recalled and Saved indexes respectively.
        /// </summary>
        /// <param name="recalledIndex">Specflow data recalled index.  Retrieves data from 'Recalled-n' where n is the index</param>
        /// <param name="savedIndex">Specflow data saved index.  Retrieves data from 'Saved-n' where n is the index</param>
        /// <remarks>Performs Assert that Saved-n data equals Recalled-n data</remarks>
        [Then(@"the recalled (.*) value matches the saved (.*) value")]
        public void ThenTheRecalledValueMatchesTheSavedValue(int recalledIndex, int savedIndex)
        {
            if (!this.scenarioContext.ContainsKey($"Saved-{savedIndex}"))
            {
                Assert.Inconclusive($"No [Saved-{savedIndex}] scenario context key");
            }

            if (!this.scenarioContext.ContainsKey($"Recalled-{recalledIndex}"))
            {
                Assert.Inconclusive($"No [Recalled-{recalledIndex}] scenario context key");
            }

            Assert.AreEqual(this.scenarioContext[$"Saved-{savedIndex}"], this.scenarioContext[$"Recalled-{recalledIndex}"], "Verify recalled [{0}] (Actual) value ({1}) matches saved [{2}] (Expected) value ({3})", recalledIndex, this.scenarioContext[$"Recalled-{recalledIndex}"], savedIndex, this.scenarioContext[$"Saved-{savedIndex}"]);
        }

        /// <summary>
        /// Validates that Specflow 'Recalled-n' data is an exception with required text
        /// </summary>
        /// <param name="recalledIndex">Specflow data recalled index.  Retrieves data from 'Recalled-n' where n is the index</param>
        /// <param name="rawErrorText">Expected exception message text - Only alphanumerics are compared; Spaces and no alphanumerics are ignored.</param>
        [Then(@"the recalled (.*) value is an exception with innermost exception message ""(.*)""")]
        public void ThenTheRecalledValueIsError(int recalledIndex, string rawErrorText)
        {
            object dynError = this.scenarioContext[$"Recalled-{recalledIndex}"];
            string errorText = Detokenize(rawErrorText);

            Assert.IsInstanceOfType(dynError, typeof(Exception));

            Exception exception = (Exception)this.scenarioContext[$"Recalled-{recalledIndex}"];
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            string expectedText = (new string(errorText.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray())).Replace(" ", string.Empty).Trim();
            string actualText = (new string(exception.Message.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray())).Replace(" ", string.Empty).Trim();

            Assert.AreEqual(expectedText, actualText);
        }

        /// <summary>
        ///  Verifies last exception thrown by a Repository TryXXXXXX method is as expected.
        /// </summary>
        /// <param name="exceptionText">Expected exception message text - Only alphanumerics are compared; Spaces and no alphanumerics are ignored.</param>
        [Then(@"an Exception is thrown with text ""(.*)""")]
        public void ThenAnExceptionIsThrownWithText(string exceptionText)
        {
            string actualText = "No exception has been thrown!";
            string expectedText = string.IsNullOrEmpty(exceptionText) ? actualText : (new string(exceptionText.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray())).Replace(" ", string.Empty).Trim();

            if (RepositoryLastTryException() != null)
            {
                actualText = (new string(RepositoryLastTryException().Message.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray())).Replace(" ", string.Empty).Trim();
            }

            Assert.AreEqual(expectedText, actualText);
        }

        /// <summary>
        ///  Verifies no exception has been thrown by a Repository TryXXXXXX method.
        /// </summary>
        [Then(@"no exception is thrown")]
        public void ThenNoExceptionIsThrown()
        {
            Assert.IsNull(RepositoryLastTryException(), $"Verify Repository has not thrown an Exception (was [{(RepositoryLastTryException()==null?"Sure hasn't": RepositoryLastTryException().Message)}])");
        }
    }
}