using System;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace TeamControlium.Utilities
{
    /// <summary>
    /// Enables test scripts to log data that assists in debugging of test scripts and/or framework.  Library and
    /// helper classes write to the debug log using log levels <see cref="LogLevels.FrameworkInformation">Framework
    /// Information</see> and <see cref="LogLevels.FrameworkDebug">Framework Debug</see> to ensure detailed analysis
    /// is possible.<br/>
    /// Debug text redirection is possible if underlying tool supplies its own logging and/or debug output, wired up
    /// to <see cref="Logger.TestToolLog"/> and <see cref="Logger.WriteToConsole"/> is set to false.<br/>
    /// The timestamp, shown on every line of the log output, is reset on the first call to the Logger.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    static public class Logger
    {
        static private bool errorWrittenToEventLog;
        static private Stopwatch testTimer;         // Used to keep track of time since first call to Logger class made.
        static private Dictionary<int, string> testToolStrings;       // Used to build string-per-thread as logger Write calls made.
        static private object lockWriteLine = new object(); // Used for locking during a DoWriteLine to ensure thread safety
        static private object lockWrite = new object(); // Used for locking during a DoWrite to ensure thread safety

        /// <summary>
        /// Instantiates an instant of the Logger static class.  Starts the Stopwatch running for timing information in debug data.
        /// </summary>
        static Logger()
        {
            errorWrittenToEventLog = false;
            testToolStrings = new Dictionary<int, string>();
            LoggingLevel = LogLevels.TestInformation; // Default logging level
            ResetTimer();
        }

        /// <summary>
        /// Levels of logging - Verbose (Maximum) to Exception (Minimum).  If level of text being written to
        /// logging is equal to, or higher than the current LoggingLevel the text is written.<br/>
        /// This is used to filter logging so that only entries to log are made if the level of the write is equal
        /// or greater than the logging level set by <see cref="LoggingLevel">LoggingLevel</see>.
        /// </summary>
        public enum LogLevels
        {
            /// <summary>
            /// Data written to log if LoggingLevel is FrameworkDebug and Write is FrameworkDebug or higher
            /// </summary>
            FrameworkDebug = 0,
            /// <summary>
            /// Data written to log if LoggingLevel is FrameworkInformation and Write is FrameworkInformation or higher
            /// </summary>
            FrameworkInformation = 1,
            /// <summary>
            /// Data written to log if LoggingLevel is TestDebug and Write is TestDebug or higher
            /// </summary>
            TestDebug = 2,
            /// <summary>
            /// Data written to log if LoggingLevel is TestInformation and Write is TestInformation or Error
            /// </summary>
            TestInformation = 3,
            /// <summary>
            /// Data always written to results
            /// </summary>
            Error = 4
        };

        /// <summary>
        /// Logging level. Lowest is Error (least amount of log data written - only writes at
        /// level <see cref="LogLevels.Error">Error</see> are written to the log). Most data is written to
        /// the log if level set is <see cref="LogLevels.FrameworkDebug">FrameworkDebug</see>.
        /// </summary>
        static public LogLevels LoggingLevel { get; set; } = LogLevels.FrameworkDebug;

        /// <summary>
        /// Defines where log lines are written to.<br/>
        /// If true (or <see cref="Logger.TestToolLog"/> has not been defined), debug data is written to the Console (stdout)<br/>
        /// If false, debug data logging is written to the <see cref="Logger.TestToolLog"/> delegate (if wired up)<br/>
        /// </summary>
        /// <remarks>
        /// The default is for log data to be written to the console
        /// </remarks>
        static public bool WriteToConsole { get; set; }

        /// <summary>
        /// System delegate to write debug data to if WriteToConsole is false.
        /// </summary>
        /// <seealso cref="WriteToConsole"/>
        static public Action<string> TestToolLog { get; set; }


        /// <summary>
        /// Resets the logger elapsed timer to zero
        /// </summary>
        static public void ResetTimer()
        {
            testTimer = new Stopwatch();
            testTimer.Start();
        }

        /// <summary>
        /// Writes details of a caught exception to the active debug log at level <see cref="LogLevels.Error">Error</see>
        /// </summary>
        /// <remarks>
        /// If current error logging level is <see cref="LogLevels.FrameworkDebug">FrameworkDebug</see> the full
        /// exception is written, including stacktrace etc.<br/>
        /// With any other <see cref="LogLevels">Log Level</see> only the exception message is writteIf an exception is thrown during write, Logger
        /// attempts to write the error details if able.
        /// </remarks>
        /// <param name="ex">Exception being logged</param>
        /// <example>
        /// <code language="cs">
        /// catch (InvalidHostURI ex)
        /// {
        ///   // Log exception and abort the test - we cant talk to the remote Selenium server
        ///   Logger.LogException(ex);
        ///   toolWrapper.AbortTest("Cannot connect to remote Selenium host");
        /// }
        /// </code>
        /// </example>
        static public void LogException(Exception ex)
        {
            StackFrame stackFrame = new StackFrame(1, true);
            if (LoggingLevel == LogLevels.FrameworkDebug)
            {
                DoWriteLine((stackFrame == null) ? null : stackFrame.GetMethod(), LogLevels.Error,
                string.Format("Exception thrown: {0}", ex.ToString()));
            }
            else
            {
                DoWriteLine((stackFrame == null) ? null : stackFrame.GetMethod(), LogLevels.Error,
                string.Format("Exception thrown: {0}", ex.Message));
            }
        }
        /// <summary>
        /// Writes details of a caught exception to the active debug log at level <see cref="LogLevels.Error">Error</see>
        /// </summary>
        /// <remarks>
        /// If current error logging level is <see cref="LogLevels.FrameworkDebug">FrameworkDebug</see> the full
        /// exception is written, including stacktrace etc.<br/>
        /// With any other <see cref="LogLevels">Log Level</see> only the exception message is writteIf an exception is thrown during write, Logger
        /// attempts to write the error details if able.
        /// </remarks>
        /// <param name="ex">Exception being logged</param>
        /// <param name="text">Additional string format text to show when logging exception</param>
        /// <param name="args">Arguments shown in string format text</param>
        /// <example>
        /// <code lang="C#">
        /// catch (InvalidHostURI ex)
        /// {
        ///   // Log exception and abort the test - we cant talk to the remote Selenium server
        ///   Logger.LogException(ex,"Given up trying to connect to [{0}]",Wherever);
        ///   toolWrapper.AbortTest("Cannot connect to remote Selenium host");
        /// }
        /// </code>
        ///</example>
        static public void LogException(Exception ex, string text, params Object[] args)
        {
            StackFrame stackFrame = new StackFrame(1, true);
            DoWrite((stackFrame == null) ? null : stackFrame.GetMethod(), LogLevels.Error, string.Format(text, args));
            if (LoggingLevel == LogLevels.FrameworkDebug)
            {
                DoWriteLine((stackFrame == null) ? null : stackFrame.GetMethod(), LogLevels.Error,
                string.Format("Exception thrown: {0}", ex.ToString()));
            }
            else
            {
                DoWriteLine((stackFrame == null) ? null : stackFrame.GetMethod(), LogLevels.Error,
                string.Format("Exception thrown: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Writes a line of data to the active debug log with no line termination
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="textString">Text to be written</param>
        /// <param name="args">String formatting arguments (if any)</param>
        /// <param name="logLevel">Level of text being written (See <see cref="Logger.LogLevels"/> for usage of the Log Level)</param>
        /// <example>Write a line of data from our test:
        /// <code lang="C#">
        /// Logger.WriteLn(LogLevels.TestDebug, "Select member using key (Member: {0})","90986754332");
        /// </code>code></example>
        static public void Write(LogLevels logLevel, string textString, params Object[] args)
        {
            StackFrame stackFrame = new StackFrame(1, true);
            DoWrite((stackFrame == null) ? null : stackFrame.GetMethod(), logLevel, string.Format(textString, args));
        }

        /// <summary>
        /// Writes a line of data to the active debug log. 
        /// Data can be formatted in the standard string.format syntax.  If an exception is thrown during write, Logger
        /// attempts to write the error deatils if able.
        /// </summary>
        /// <param name="logLevel">Level of text being written (See <see cref="Logger.LogLevels"/> for usage of the Log Level)</param>
        /// <param name="textString">Text to be written</param>
        /// <param name="args">String formatting arguments (if any)</param>
        /// <example>Write a line of data from our test:
        /// <code lang="C#">
        /// Logger.WriteLine(LogLevels.TestDebug, "Select member using key (Member: {0})","90986754332");
        /// </code></example>
        static public void WriteLine(LogLevels logLevel, string textString, params Object[] args)
        {
            StackFrame stackFrame = new StackFrame(1, true);
            DoWriteLine((stackFrame == null) ? null : stackFrame.GetMethod(), logLevel,
                    string.Format(textString, args));
        }

        /// <summary>
        /// Writes given Text to a text file, optionally auto versioning (adding (n) to filename) OR
        /// overwriting.
        /// </summary>
        /// <remarks>
        /// No exception is raised if there is any problem, but details of error is written to Logger log
        /// </remarks>
        /// <param name="Filename">Full path and filename to use</param>
        /// <param name="AutoVersion">If true and file exists. (n) is added to auto-version.  If false and file exists, it is overwritten if able</param>
        /// <param name="Text">Text to write</param>
        static public void WriteTextToFile(string Filename, bool AutoVersion, string Text)
        {
            try
            {
                string FilenameToUse = Filename;
                if (AutoVersion)
                {
                    int count = 1;
                    string fileNameOnly = Path.GetFileNameWithoutExtension(Filename);
                    string extension = Path.GetExtension(Filename);
                    string path = Path.GetDirectoryName(Filename);
                    FilenameToUse = Filename;

                    while (File.Exists(FilenameToUse))
                    {
                        string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                        FilenameToUse = Path.Combine(path, tempFileName + extension);
                    }
                }

                File.WriteAllText(FilenameToUse, Text);
            }
            catch (Exception ex)
            {
                LogException(ex, $"Cannot write data to file [{Filename ?? "Null Filename!"}] (AutoVersion={(AutoVersion ? "Yes" : "No")})");
            }
        }

        /// <summary>
        /// Gets class-type and Method name of passed MethodBase class.
        /// </summary>
        /// <param name="methodBase">MethodBase of class</param>
        /// <returns>Formatted string containing Type.Method</returns>
        static private string CallingMethodDetails(MethodBase methodBase)
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
        /// <param name="TypeOfWrite">Level of debug text to be written</param>
        /// <param name="textString">Text string to be written</param>
        /// <remarks>Text is written if TypeOfWrite is equal to, or higher the current Logging Level</remarks>
        static private void DoWrite(MethodBase methodBase, LogLevels TypeOfWrite, string textString)
        {
            // Only do write if level of this write is equal to or greater than the current logging level
            if (TypeOfWrite >= LoggingLevel)
            {
                // Ensure thread safety by locking code around the write
                lock (lockWrite)
                {
                    //
                    // Get the id of the current thread and append text to end of the dictionary item for that
                    // thread (create new item if doesnt already exist).  If this is
                    // first time this thread is doing a write, prepend the PreAmble text first.
                    //
                    int threadID = Thread.CurrentThread.ManagedThreadId;
                    bool writeStart = !testToolStrings.ContainsKey(threadID);
                    if (writeStart) testToolStrings[threadID] = GetPreAmble(methodBase, TypeOfWrite);
                    testToolStrings[threadID] += textString ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// Appends text to currently active line and writes line to active log.  If new line, text is pre-pended with Line header information
        /// </summary>
        /// <param name="methodBase">MethodBase of class calling Logger class</param>
        /// <param name="TypeOfWrite">Level of debug text to be written</param>
        /// <param name="textString">Text string to be written</param>
        /// <remarks>Text is written if TypeOfWrite is equal to, or higher the current Logging Level</remarks> 
        static private void DoWriteLine(MethodBase methodBase, LogLevels TypeOfWrite, string textString)
        {
            if (TypeOfWrite >= LoggingLevel)
            {
                var textToWrite = textString;
                lock (lockWriteLine)
                {
                    int threadID = Thread.CurrentThread.ManagedThreadId;
                    if (testToolStrings.ContainsKey(threadID))
                    {
                        try
                        {
                            textToWrite = testToolStrings[threadID] += testToolStrings[threadID].EndsWith(" ") ? "" : " " + textToWrite;
                        }
                        finally
                        {
                            testToolStrings.Remove(threadID);
                        }
                    }
                    else
                    {
                        textToWrite = GetPreAmble(methodBase, TypeOfWrite) + textString ?? string.Empty;
                    }

                    try
                    {
                        if (WriteToConsole || TestToolLog == null)
                            Console.WriteLine(textToWrite);
                        else
                            TestToolLog(textToWrite);
                    }
                    catch (Exception ex)
                    {
                        string details;
                        if (!errorWrittenToEventLog)
                        {
                            using (EventLog appLog = new EventLog("Application"))
                            {
                                if (WriteToConsole)
                                {
                                    details = "console (STDOUT)";
                                }
                                else
                                {
                                    details = string.Format("delegate provide by tool{0}.", (TestToolLog == null) ?
                                                                                                 " (Is null! - Has not been implemented!)" :
                                                                                                 "");
                                }
                                appLog.Source = "Application";
                                appLog.WriteEntry(string.Format("AppServiceInterfaceMock - Logger error writing to {0}.\r\n\r\n" +
                                                                "Attempt to write line;\r\n" +
                                                                "{1}\r\n\r\n" +
                                                                "No further log writes to event log will happen in this session", details, textToWrite, ex), EventLogEntryType.Warning, 12791, 1);
                            }
                            errorWrittenToEventLog = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Constructs and returns a log-file pre-amble.  Preamble is {Log Type} {Time} [Calling Type.Method]:
        /// </summary>
        /// <param name="methodBase">Reference to calling method</param>
        /// <param name="TypeOfWrite">Type of write</param>
        /// <returns>Line pre-amble text</returns>
        static private string GetPreAmble(MethodBase methodBase, LogLevels TypeOfWrite)
        {
            string time = String.Format("[{0:HH:mm:ss.ff}][{1:00000.00}]", DateTime.Now, testTimer.Elapsed.TotalSeconds);
            int totalTimeLength = time.Length + (8 - (int)TypeOfWrite);
            if ((time.Length + 1) <= totalTimeLength) time = time.PadRight(totalTimeLength);
            string preAmble = String.Format("{0} {1} [{2}]: ", WriteTypeString(TypeOfWrite),
                time,
                CallingMethodDetails(methodBase));
            return preAmble;
        }

        /// <summary>
        /// Returns debug line inital token based on LogLevel of text being written
        /// </summary>
        /// <param name="TypeOfWrite">Log Level to obtain text for</param>
        /// <returns>Textual representation for Debug log line pre-amble</returns>
        static private string WriteTypeString(LogLevels TypeOfWrite)
        {
            switch (TypeOfWrite)
            {
                case LogLevels.Error:
                    return "LOG-E";
                case LogLevels.FrameworkDebug:
                    return "LOG-F";
                case LogLevels.FrameworkInformation:
                    return "LOG-R";
                case LogLevels.TestDebug:
                    return "LOG-D";
                case LogLevels.TestInformation:
                    return "LOG-I";
                default:
                    return "LOG-?";
            }
        }
    }
}