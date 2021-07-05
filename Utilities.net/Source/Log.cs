// <copyright file="Log.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// Manages logging information.  Created from scratch rather than using an OffTheShelf (NLog, Log4Net etc) to keep footprint as small as possible while ensuring
    /// logging is formatted, along with functionality, exactly as we want.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Used to keep track of time since first call to Logger class made.
        /// </summary>
        private static Stopwatch testTimer;

        /// <summary>
        /// Different threads will be sending logging information to Log.  So, when building a Log string (IE. Calls to .DoWrite to build a Log
        /// string) the string relating to the correct thread is appended to.
        /// </summary>
        private static Dictionary<int, string> testToolStrings;

        /// <summary>
        /// Indicates whether an error has been written to the event log.  Allows suppressing of further errors to prevent log blow-out.
        /// </summary>
        private static bool errorWrittenToEventLog;

        /// <summary>
        /// Used for locking during a DoWriteLine.  To ensure thread safety, only a single thread at a time is
        /// permitted to call DoWriteLine at any one time.
        /// </summary>
        private static object lockWriteLine = new object();

        /// <summary>
        /// Used for locking during a DoWriteLine.  To ensure thread safety, only a single thread at a time is
        /// permitted to call DoWrite at any one time.
        /// </summary>
        private static object lockWrite = new object();

        /// <summary>
        /// Initialises static members of the <see cref="Log" /> class.
        /// Instantiates an instant of the Log static class.  Starts the main Stopwatch running for timing information in log data.
        /// </summary>
        static Log()
        {
            testToolStrings = new Dictionary<int, string>();
            LogResetTimer();
        }

        /// <summary>
        /// Levels of logging - Verbose (Maximum) to Exception (Minimum).  If level of text being written to
        /// logging is equal to, or higher than the current LoggingLevel the text is written.<br/>
        /// This is used to filter logging so that only entries to log are made if the level of the write is equal
        /// or greater than the logging level set by <see cref="LoggingCurrentLevel">LoggingLevel</see>.
        /// </summary>
        public enum LogLevels
        {
            /// <summary>
            /// Data written to log if LoggingLevel is FrameworkDebug and LogException is FrameworkDebug or higher
            /// </summary>
            FrameworkDebug = 0,

            /// <summary>
            /// Data written to log if LoggingLevel is FrameworkInformation and LogException is FrameworkInformation or higher
            /// </summary>
            FrameworkInformation = 1,

            /// <summary>
            /// Data written to log if LoggingLevel is TestDebug and LogException is TestDebug or higher
            /// </summary>
            TestDebug = 2,

            /// <summary>
            /// Data written to log if LoggingLevel is TestInformation and LogException is TestInformation or Error
            /// </summary>
            TestInformation = 3,

            /// <summary>
            /// Data always written to results
            /// </summary>
            Error = 4
        }

        /// <summary>
        /// Gets or sets Logging level. Lowest is Error (least amount of log data written - only writes at
        /// level <see cref="LogLevels.Error">Error</see> are written to the log). Most data is written to
        /// the log if level set is <see cref="LogLevels.FrameworkDebug">FrameworkDebug</see>.
        /// </summary>
        /// <remarks>Default is FrameworkDebug</remarks>
        public static LogLevels LoggingCurrentLevel { get; set; } = LogLevels.FrameworkDebug;

        /// <summary>
        /// Gets or sets a value indicating whether log data is written to Console.<br/>
        /// If true log data is written to the Console (STDOUT)<br/>
        /// If false and <see cref="Log.LogOutputDelegate"/> has been defined, no log data is written to Console. If <see cref="Log.LogOutputDelegate"/> has
        /// NOT been defined, LogToConsole false is ignored and output is STILL written to Console.<br/>
        /// </summary>
        /// <remarks>
        /// The default is true (log data to be written to the console)
        /// </remarks>
        public static bool LogToConsole { get; set; } = true;

        /// <summary>
        /// Gets or sets delegate to write debug data to if LogToConsole is false.
        /// </summary>
        /// <remarks>
        /// Note.  If the delegate throws an exception, allow the exception to ripple up.  Log class will handle the exception and write details to
        /// the OS event logging system.
        /// </remarks>
        /// <seealso cref="LogToConsole"/>
        public static Action<string> LogOutputDelegate { get; set; }

        /// <summary>
        /// Resets the logger elapsed timer to zero
        /// </summary>
        public static void LogResetTimer()
        {
            testTimer = new Stopwatch();
            testTimer.Start();
        }

        /// <summary>
        /// Writes details of a caught exception to the active debug log at level <see cref="LogLevels.Error">Error</see>
        /// </summary>
        /// <remarks>
        /// If current error logging level is <see cref="LogLevels.FrameworkDebug">FrameworkDebug</see> the full
        /// exception is written, including stack-trace etc.<br/>
        /// With any other <see cref="LogLevels">Log Level</see> only the exception message is written if an exception is thrown during write, Logger
        /// attempts to write the error details if able.
        /// </remarks>
        /// <param name="ex">Exception being logged</param>
        /// <example>
        /// <code language="cs">
        /// catch (InvalidHostURI ex)
        /// {
        ///   // Log exception and abort the test - we cant talk to the remote Selenium server
        ///   Logger.LogException(ex,"Connecting to Selenium host");
        ///   toolWrapper.AbortTest("Cannot connect to remote Selenium host");
        /// }
        /// </code>
        /// </example>
        public static void LogException(Exception ex)
        {
            StackFrame stackFrame = new StackFrame(1, true);

            LogException(stackFrame, ex, null);
        }

        /// <summary>
        /// Writes details of a caught exception to the active debug log at level <see cref="LogLevels.Error">Error</see>
        /// </summary>
        /// <remarks>
        /// If current error logging level is <see cref="LogLevels.FrameworkDebug">FrameworkDebug</see> the full
        /// exception is written, including stack trace details etc.<br/>
        /// With any other <see cref="LogLevels">Log Level</see> only the exception message is written if an exception is thrown during write, Logger
        /// attempts to write the error details if able.
        /// </remarks>
        /// <param name="ex">Exception being logged</param>
        /// <param name="text">Additional string format text to show when logging exception</param>
        /// <param name="args">Arguments shown in string format text</param>
        /// <example>
        ///  <code lang="C#">
        /// catch (InvalidHostURI ex)
        /// {
        ///   // Log exception and abort the test - we cant talk to the remote Selenium server
        ///   Logger.LogException(ex,"Given up trying to connect to [{0}]",Wherever);
        ///   toolWrapper.AbortTest("Cannot connect to remote Selenium host");
        /// }
        ///  </code>
        /// </example>
        public static void LogException(Exception ex, string text, params object[] args)
        {
            StackFrame stackFrame = new StackFrame(1, true);

            LogException(stackFrame, ex, text, args);
        }

        /// <summary>
        /// Writes a line of data to the active debug log with no line termination
        /// </summary>
        /// <param name="logLevel">Level of text being written (See <see cref="Log.LogLevels"/> for usage of the Log Level)</param>
        /// <param name="textString">Text to be written</param>
        /// <param name="args">String formatting arguments (if any)</param>
        /// <example>LogWrite a line of data from our test:
        /// <code lang="C#">
        /// Logger.WriteLn(LogLevels.TestDebug, "Select member using key (Member: {0})","90986754332");
        /// </code>code></example>
        public static void LogWrite(LogLevels logLevel, string textString, params object[] args)
        {
            StackFrame stackFrame = new StackFrame(1, true);
            if (args.Length == 0)
            {
                DoWrite(stackFrame?.GetMethod(), logLevel, textString);
            }
            else
            {
                try
                {
                    DoWrite(stackFrame?.GetMethod(), logLevel, string.Format(textString, args));
                }
                catch
                {
                    DoWrite(stackFrame?.GetMethod(), logLevel, "(Log Args ignored!) " + textString);
                }
            }
        }

        /// <summary>
        /// Writes a line of data to the active debug log. 
        /// Data can be formatted in the standard string.format syntax.  If an exception is thrown during write, Logger
        /// attempts to write the error details if able.
        /// </summary>
        /// <param name="logLevel">Level of text being written (See <see cref="Log.LogLevels"/> for usage of the Log Level)</param>
        /// <param name="textString">Text to be written</param>
        /// <param name="args">String formatting arguments (if any)</param>
        /// <example>LogException a line of data from our test:
        /// <code lang="C#">
        /// Logger.LogWriteLine(LogLevels.TestDebug, "Select member using key (Member: {0})","90986754332");
        /// </code></example>
        /// <remarks>If arguments are passed in but there is an error formatting the text line during resolution of the arguments they will be ignored and the text line written out without arguments.</remarks>
        public static void LogWriteLine(LogLevels logLevel, string textString, params object[] args)
        {
            StackFrame stackFrame = new StackFrame(1, true);
            if (args.Length == 0)
            {
                DoWriteLine(stackFrame?.GetMethod(), logLevel, textString);
            }
            else
            {
                try
                {
                    DoWriteLine(stackFrame?.GetMethod(), logLevel, string.Format(textString, args));
                }
                catch (Exception ex)
                {
                    DoWriteLine(stackFrame?.GetMethod(), LogLevels.Error, string.Format("[Note. Unable to write line due to arguments error [{0}]] (Arguments will be ignored)", ex.Message));
                    DoWriteLine(stackFrame?.GetMethod(), logLevel, textString);
                }
            }
        }

        /// <summary>
        /// Does writing of the logged exception
        /// </summary>
        /// <param name="stackFrame">Stack frame passed in by caller.  Used to get method base details</param>
        /// <param name="ex">Exception being reported</param>
        /// <param name="text">Optional text</param>
        /// <param name="args">Optional text arguments</param>
        private static void LogException(StackFrame stackFrame, Exception ex, string text, params object[] args)
        {
            if (text != null)
            {
                if (args.Length == 0)
                {
                    DoWrite(stackFrame?.GetMethod(), LogLevels.Error, text + "; " + ex.Message);
                }
                else
                {
                    try
                    {
                        DoWrite(stackFrame?.GetMethod(), LogLevels.Error, string.Format(text + "; ", args) + ex.Message);
                    }
                    catch (Exception iex)
                    {
                        DoWrite(stackFrame?.GetMethod(), LogLevels.Error, string.Format("[Note. Unable to report error cause because [{0}]]", iex.Message));
                    }
                }
            }

            if (LoggingCurrentLevel == LogLevels.FrameworkDebug)
            {
                DoWriteLine(stackFrame?.GetMethod(), LogLevels.Error, string.Format("Exception thrown: {0}", ex.ToString()));
            }
            else
            {
                DoWriteLine(stackFrame?.GetMethod(), LogLevels.Error, string.Format("Exception thrown: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Gets class-type and Method name of passed MethodBase class.
        /// </summary>
        /// <param name="methodBase">MethodBase of class</param>
        /// <returns>Formatted string containing Type.Method</returns>
        private static string CallingMethodDetails(MethodBase methodBase)
        {
            string methodName = "<Unknown>";
            string typeName = "<Unknown>";
            if (methodBase != null)
            {
                methodName = methodBase.Name ?? methodName;
                if (methodBase.DeclaringType != null)
                {
                    typeName = methodBase.DeclaringType.Name ?? typeName;
                }
            }

            return string.Format("{0}.{1}", typeName, methodName);
        }

        /// <summary>
        /// Appends text to currently active line.  If the start of line, text is pre-pended with Line header information
        /// </summary>
        /// <param name="methodBase">MethodBase of class calling Logger class</param>
        /// <param name="typeOfWrite">Level of debug text to be written</param>
        /// <param name="textString">Text string to be written</param>
        /// <remarks>Text is written if TypeOfWrite is equal to, or higher the current Logging Level</remarks>
        private static void DoWrite(MethodBase methodBase, LogLevels typeOfWrite, string textString)
        {
            // Only do write if level of this write is equal to or greater than the current logging level
            if (typeOfWrite >= LoggingCurrentLevel)
            {
                // Ensure thread safety by locking code around the write
                lock (lockWrite)
                {
                    // Get the id of the current thread and append text to end of the dictionary item for that
                    // thread (create new item if doesnt already exist).  If this is
                    // first time this thread is doing a write, prepend the PreAmble text first.
                    int threadID = Thread.CurrentThread.ManagedThreadId;
                    bool writeStart = !testToolStrings.ContainsKey(threadID);
                    if (writeStart)
                    {
                        testToolStrings[threadID] = GetPreAmble(methodBase, typeOfWrite);
                    }

                    testToolStrings[threadID] += textString ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// Appends text to currently active line and writes line to active log.  If new line, text is pre-pended with Line header information
        /// </summary>
        /// <param name="methodBase">MethodBase of class calling Logger class</param>
        /// <param name="typeOfWrite">Level of debug text to be written</param>
        /// <param name="textString">Text string to be written</param>
        /// <remarks>Text is written if TypeOfWrite is equal to, or higher the current Logging Level</remarks> 
        private static void DoWriteLine(MethodBase methodBase, LogLevels typeOfWrite, string textString)
        {
            if (typeOfWrite >= LoggingCurrentLevel)
            {
                var textToWrite = textString;
                lock (lockWriteLine)
                {
                    int threadID = Thread.CurrentThread.ManagedThreadId;
                    if (testToolStrings.ContainsKey(threadID))
                    {
                        try
                        {
                            textToWrite = testToolStrings[threadID] += (testToolStrings[threadID].EndsWith(" ") ? string.Empty : " ") + textToWrite;
                        }
                        finally
                        {
                            testToolStrings.Remove(threadID);
                        }
                    }
                    else
                    {
                        textToWrite = GetPreAmble(methodBase, typeOfWrite) + textString ?? string.Empty;
                    }

                    try
                    {
                        // If LogToConsole is true or LogOutputDelegate is not set (IE. No logging is being consumed by custom TestTool log) write the line to the console
                        if (LogToConsole || LogOutputDelegate == null)
                        {
                            Console.WriteLine(textToWrite);
                        }

                        LogOutputDelegate?.Invoke(textToWrite);
                    }
                    catch (Exception ex)
                    {
                        string details;
                        string eventLogString;
                        if (!errorWrittenToEventLog)
                        {
                            using (EventLog appLog = new EventLog("Application"))
                            {
                                if (LogToConsole)
                                {
                                    details = "console (STDOUT)";
                                }
                                else
                                {
                                    details = string.Format("delegate provide by tool{0}.", (LogOutputDelegate == null) ? " (Is null! - Has not been implemented!)" : string.Empty);
                                }

                                eventLogString = string.Format(
                                                 "AppServiceInterfaceMock - Logger error writing to {0}.\r\n\r\n" +
                                                 "Attempt to write line;\r\n" +
                                                 "{1}\r\n\r\n" +
                                                 "No further log writes to event log will happen in this session",
                                                 details,
                                                 textToWrite,
                                                 ex);

                                appLog.Source = "Application";
                                appLog.WriteEntry(eventLogString, EventLogEntryType.Warning, 12791, 1);
                            }

                            errorWrittenToEventLog = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Constructs and returns a log-file pre-amble.  Preamble is {Log Type} - [{Time}][{Elapsed}] [Calling Type.Method]:
        /// </summary>
        /// <param name="methodBase">Reference to calling method</param>
        /// <param name="typeOfWrite">Type of write</param>
        /// <example>
        /// GetPreAmple(methodBase, LogLevel.TestDebug) returns "TSDBG - [15:34:45][00012.33] [MyTestClass.MyMethod]:"
        /// </example>
        /// <returns>Line pre-amble text</returns>
        private static string GetPreAmble(MethodBase methodBase, LogLevels typeOfWrite)
        {
            return string.Format("{0} - [{1:HH:mm:ss.ff}][{2:00000.00}] [{3}]: ", GetWriteTypeString(typeOfWrite), DateTime.Now, testTimer.Elapsed.TotalSeconds, CallingMethodDetails(methodBase));
        }

        /// <summary>
        /// Returns debug line initial token based on LogLevel of text being written
        /// </summary>
        /// <param name="typeOfWrite">Log Level to obtain text for</param>
        /// <returns>Textual representation for Debug log line pre-amble</returns>
        private static string GetWriteTypeString(LogLevels typeOfWrite)
        {
            switch (typeOfWrite)
            {
                case LogLevels.Error:
                    return "ERROR";
                case LogLevels.FrameworkDebug:
                    return "FKDBG";
                case LogLevels.FrameworkInformation:
                    return "FKINF";
                case LogLevels.TestDebug:
                    return "TSDBG";
                case LogLevels.TestInformation:
                    return "TSINF";
                default:
                    return "?????";
            }
        }
    }
}