namespace TeamControlium.Utilities.UnitTests
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
    using TechTalk.SpecFlow;
    using static TeamControlium.Utilities.Detokenizer;
    using static TeamControlium.Utilities.Log;
    using static TeamControlium.Utilities.Repository;

    [Binding]
    public sealed class RepositoryTestSteps
    {
        private readonly ScenarioContext _scenarioContext;
        public RepositoryTestSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeFeature]
        public static void BeforeFeature()
        {
            CurrentLoggingLevel = LogLevels.FrameworkDebug;
            TestToolLog = (s) => { Debug.WriteLine(s); };
            WriteToConsole = false;
        }

        [BeforeScenario()]
        [Scope(Feature = "Repository Tests")]
        public static void BeforeScenario()
        {
            ClearRepositoryAll();
            Detokenizer.CustomTokenProcessor = ProcessCustomDetokenizer;
        }

        public static string ProcessCustomDetokenizer(char delimiter, string[] token)
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

        [Given(@"I clone Global test data to Local test data, ((?:not ){0,1})overwriting any existing")]
        [When(@"I clone Global test data to Local test data, ((?:not ){0,1})overwriting any existing")]
        public void GivenICloneGlobalTestDataToLocalTestDataOverwritingAnyExisting(string blankOrNot)
        {
            TryCloneGlobalToLocal(blankOrNot.Trim().ToLower() == "");
        }




        [Given(@"I have saved string ""(.*)"" \(Item (.*)\) in Repository (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        [When(@"I have saved string ""(.*)"" \(Item (.*)\) in Repository (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        public void GivenIHaveSavedStringInRepositoryLocalCategoryItemName(string item, int savedItemIndex, string localOrGlobal, string categoryName, string itemName)
        {
            _scenarioContext[$"Saved-{savedItemIndex}"] = item;
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

        [Given(@"I have saved integer (.*) \(Item (.*)\) in Repository (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        [When(@"I have saved integer (.*) \(Item (.*)\) in Repository (?i)(local|global), Category ""(.*)"", Item Name ""(.*)""")]
        public void GivenIHaveSavedIntegerInRepositoryLocalCategoryItemName(int item, int savedItemIndex, string localOrGlobal, string categoryName, string itemName)
        {
            _scenarioContext[$"Saved-{savedItemIndex}"] = item;
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
                _scenarioContext[$"Recalled-{recalledItemIndex}"] = value;
            else
                _scenarioContext[$"Recalled-{recalledItemIndex}"] = RepositoryLastTryException();
        }


        [When(@"I recall \(Item (.*)\) from (?i)(local|global), Category ""(.*)"", Item Name ""(.*)"" as a \[""(.*)""]")]
        public void WhenIRecallStringItemFromLocalCategoryItemName(int recalledItemIndex, string localOrGlobal, string categoryName, string itemName, string requiredType)
        {
            bool isLocal = localOrGlobal.ToLower().Trim() == "local";
            bool success = false;
            dynamic value = null;
            object[] parameters = new object[] { categoryName, itemName, null };
            var gash = typeof(Repository).GetMethods();
            var mi = typeof(Repository).GetMethods().Where(method => (method.Name == (isLocal ? "TryGetItemLocal" : "TryGetItemGlobal") && method.IsGenericMethod == true && method.GetParameters().Length==3));
            var fooRef = mi.First().MakeGenericMethod(Type.GetType(requiredType));
            success = (bool)fooRef.Invoke(null, parameters);
            if (success)
            {
                value = (dynamic)parameters[2];
            }
            if (success)
                _scenarioContext[$"Recalled-{recalledItemIndex}"] = value;
            else
                _scenarioContext[$"Recalled-{recalledItemIndex}"] = RepositoryLastTryException();
        }


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



        [Then(@"the recalled (.*) value matches the saved (.*) value")]
        public void ThenTheRecalledValueMatchesTheSavedValue(int recalledIndex, int savedIndex)
        {
            if (!_scenarioContext.ContainsKey($"Saved-{savedIndex}"))
                Assert.Inconclusive($"No [Saved-{savedIndex}] scenario context key");
            if (!_scenarioContext.ContainsKey($"Recalled-{recalledIndex}"))
                Assert.Inconclusive($"No [Recalled-{recalledIndex}] scenario context key");

            Assert.AreEqual(_scenarioContext[$"Saved-{savedIndex}"], _scenarioContext[$"Recalled-{recalledIndex}"], "Verify recalled [{0}] (Actual) value ({1}) matches saved [{2}] (Expected) value ({3})", recalledIndex, _scenarioContext[$"Recalled-{recalledIndex}"], savedIndex, _scenarioContext[$"Saved-{savedIndex}"]);
        }

        [Then(@"the recalled (.*) value is an exception with innermost exception message ""(.*)""")]
        public void ThenTheRecalledValueIsError(int recalledIndex, string rawErrorText)
        {
            object dynError = _scenarioContext[$"Recalled-{recalledIndex}"];
            string errorText = Detokenize(rawErrorText);

            Assert.IsInstanceOfType(dynError, typeof(Exception));

            Exception exception = (Exception)_scenarioContext[$"Recalled-{recalledIndex}"];
            while (exception.InnerException != null) exception = exception.InnerException;

            string expectedText = (new string(errorText.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray())).Replace(" ", "").Trim();
            string actualText = (new string(exception.Message.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray())).Replace(" ", "").Trim();


            Assert.AreEqual(expectedText, actualText);
        }


        [Then(@"an Exception is thrown with text ""(.*)""")]
        public void ThenAnExceptionIsThrownWithText(string exceptionText)
        {
            if (RepositoryLastTryException()!=null)
            {
           //     Assert.AreEqual(exceptionText, RepositoryLastTryException(), )
            }
        }



    }
}