// <copyright file="SpecflowHooks.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.Utilities.UnitTests
{
    using System.IO;
    using System.Threading;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Specflow Hooks file containing before/after event handlers.
    /// For additional details on SpecFlow hooks see http://go.specflow.org/doc-hooks
    /// </summary>
    [Binding]
    public sealed class SpecflowHooks
    {
        /// <summary>
        /// Called by Specflow before each Scenario is executed
        /// </summary>
        /// <param name="scenarioContext">Scenario Contextual data</param>
        [BeforeScenario]
        public void BeforeScenario(ScenarioContext scenarioContext)
        {
            // Set logging level to maximum incase debug is needed.
            Log.CurrentLoggingLevel = Log.LogLevels.FrameworkDebug;
            scenarioContext.Add("consoleOut", new StringWriter());
            scenarioContext.Add("currentThreadID", Thread.CurrentThread.ManagedThreadId);
        }

    }


}
