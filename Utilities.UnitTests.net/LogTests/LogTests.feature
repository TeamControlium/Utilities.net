Feature: Log
	In order to test log events in the Controlium solution
	As a test automator
	I want to be able to log events

Scenario Outline: Log only outputs levels equal to higher than the level selected
	Given I have configured Log LogToConsole to true
	Given I set Log to level <Log Level>
	When I call Log.WriteLine with level <Write Level> and string <Test String>
	Then the console stdout contains a line starting with <Type> and ending with <Output>
Examples:
| example description                                     | Log Level            | Write Level          | Test String             | Type    | Output                  |
| Level Framework Debug write Framework Debug             | FrameworkDebug       | FrameworkDebug       | "Test Framework Debug"  | "FKDBG" | "Test Framework Debug"  |
| Level Framework Debug write Framework Information       | FrameworkDebug       | FrameworkInformation | "Test Framework Info"   | "FKINF" | "Test Framework Info"   |
| Level Framework Debug write Test Debug                  | FrameworkDebug       | TestDebug            | "Test Test Debug"       | "TSDBG" | "Test Test Debug"       |
| Level Framework Debug write Test Information            | FrameworkDebug       | TestInformation      | "Test Test Information" | "TSINF" | "Test Test Information" |
| Level Framework Debug write Error                       | FrameworkDebug       | Error                | "Test Error"            | "ERROR" | "Test Error"            |
| Level Framework Information write Framework Debug       | FrameworkInformation | FrameworkDebug       | "Test Framework Debug"  |string.Empty      |string.Empty                      |
| Level Framework Information write Framework Information | FrameworkInformation | FrameworkInformation | "Test Framework Info"   | "FKINF" | "Test Framework Info"   |
| Level Framework Information write Test Debug            | FrameworkInformation | TestDebug            | "Test Test Debug"       | "TSDBG" | "Test Test Debug"       |
| Level Framework Information write Test Information      | FrameworkInformation | TestInformation      | "Test Test Information" | "TSINF" | "Test Test Information" |
| Level Framework Information write Error                 | FrameworkInformation | Error                | "Test Error"            | "ERROR" | "Test Error"            |
| Level Test Debug write Framework Debug                  | TestDebug            | FrameworkDebug       | "Test Framework Debug"  |string.Empty      |string.Empty                      |
| Level Test Debug write Framework Information            | TestDebug            | FrameworkInformation | "Test Framework Info"   |string.Empty      |string.Empty                      |
| Level Test Debug write Test Debug                       | TestDebug            | TestDebug            | "Test Test Debug"       | "TSDBG" | "Test Test Debug"       |
| Level Test Debug write Test Information                 | TestDebug            | TestInformation      | "Test Test Information" | "TSINF" | "Test Test Information" |
| Level Test Debug write Error                            | TestDebug            | Error                | "Test Error"            | "ERROR" | "Test Error"            |
| Level Test Information write Framework Debug            | TestInformation      | FrameworkDebug       | "Test Framework Debug"  |string.Empty      |string.Empty                      |
| Level Test Information write Framework Information      | TestInformation      | FrameworkInformation | "Test Framework Info"   |string.Empty      |string.Empty                      |
| Level Test Information write Test Debug                 | TestInformation      | TestDebug            | "Test Test Debug"       |string.Empty      |string.Empty                      |
| Level Test Information write Test Information           | TestInformation      | TestInformation      | "Test Test Information" | "TSINF" | "Test Test Information" |
| Level Test Information write                            | TestInformation      | Error                | "Test Error"            | "ERROR" | "Test Error"            |
| Level Error write Framework Debug                       | Error                | FrameworkDebug       | "Test Framework Debug"  |string.Empty      |string.Empty                      |
| Level Error write Framework Information                 | Error                | FrameworkInformation | "Test Framework Info"   |string.Empty      |string.Empty                      |
| Level Error write Test Debug                            | Error                | TestDebug            | "Test Test Debug"       |string.Empty      |string.Empty                      |
| Level Error write Test Information                      | Error                | TestInformation      | "Test Test Information" |string.Empty      |string.Empty                      |
| Level Error write Error                                 | Error                | Error                | "Test Error"            | "ERROR" | "Test Error"            |

Scenario Outline: Log output can be directed to Console or a custom output stream
	Given I have configured Log LogToConsole to <Write To Console>
    And I have <Test Tool Log> Log LogOutputDelegate delegate
	And I set Log to level FrameworkDebug
	When I call Log.WriteLine with level FrameworkDebug and string <Test String>
	Then Log text receiver contains a line starting with <Receiver Line Type> and ending with <TestLogOutput>
	And the console stdout contains a line starting with <Console Line Type> and ending with <ConsoleOutput>
	Examples:
| Description                                       | Write To Console | Test Tool Log  | Test String            | Console Line Type | Receiver Line Type | ConsoleOutput          | TestLogOutput          |
| LogToConsole True - LogOutputDelegate not configured  | true             | not configured | "Test Framework Debug" | "FKDBG"           |string.Empty                 | "Test Framework Debug" |string.Empty                     |
| LogToConsole False - LogOutputDelegate not configured | false            | not configured | "Test Framework Debug" | "FKDBG"           |string.Empty                 | "Test Framework Debug" |string.Empty                     |
| LogToConsole True - LogOutputDelegate configured      | true             | configured     | "Test Framework Debug" | "FKDBG"           | "FKDBG"            | "Test Framework Debug" | "Test Framework Debug" |
| LogToConsole False - LogOutputDelegate configured     | false            | configured     | "Test Framework Debug" |string.Empty                | "FKDBG"            |string.Empty                     | "Test Framework Debug" |

Scenario Outline: Log output can be changed in flight
	Given I set Log to level FrameworkDebug
	And I have configured Log LogToConsole to <Initial Write To Console>
    And I have <Test Tool Log Initially Configured> Log LogOutputDelegate delegate
	And I call Log.WriteLine with level FrameworkDebug and string <First Test String>
	When I change Log LogToConsole to <Second Write To Console>
	And I have <Test Tool Log Second Configured> Log LogOutputDelegate delegate
	And I call Log.WriteLine with level FrameworkDebug and string <Second Test String>
	Then Log text receiver contains a line starting with "FKDBG" and ending with <TestLogOutput>
	And the console stdout contains a line starting with "FKDBG" and ending with <ConsoleOutput>
	Examples:
| Description                                                                           | Initial Write To Console | Second Write To Console | Test Tool Log Initially Configured | Test Tool Log Second Configured | First Test String | Second Test String | TestLogOutput     | ConsoleOutput      |
| Test ToolLog configured then not configured                                           | false                    | false                   | configured                         | not configured                  | "My first string" | "My second string" | "My first string" | "My second string" |
| Test ToolLog configured then not configured while writing to console                  | true                     | true                    | configured                         | not configured                  | "My first string" | "My second string" | "My first string" | "My first string"  |
| Test ToolLog not configured and Console output then configured and no console output  | true                     | false                   | not configured                     | configured                      | "My first string" | "My second string" | "My second string" | "My first string"  |
| Test ToolLog not configured and not Console output then configured and console output | false                    | true                    | not configured                     | configured                      | "My first string" | "My second string" | "My second string" | "My first string"  |
