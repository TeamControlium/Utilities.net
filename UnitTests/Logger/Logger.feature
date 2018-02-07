Feature: Logger
	In order to test log events in the Controlium solution
	As a test automator
	I want to be able to log events

Scenario Outline: Logger only outputs levels equal to higher than the level selected
	Given I have configured Logger WriteToConsole to true
	Given I set Logger to level <Logger Level>
	When I call Logger with level <Write Level> and string <Test String>
	Then the console output contains a Logger line ending with <Output>
Examples:
| Logger Level         | Write Level          | Test String             | Output                  |
| FrameworkDebug       | FrameworkDebug       | "Test Framework Debug"  | "Test Framework Debug"  |
| FrameworkDebug       | FrameworkInformation | "Test Framework Debug"  | "Test Framework Debug"  |
| FrameworkDebug       | TestDebug            | "Test Test Debug"       | "Test Test Debug"       |
| FrameworkDebug       | TestInformation      | "Test Test Information" | "Test Test Information" |
| FrameworkDebug       | Error                | "Test Error"            | "Test Error"            |
| FrameworkInformation | FrameworkDebug       | "Test Framework Debug"  | ""                      |
| FrameworkInformation | FrameworkInformation | "Test Framework Debug"  | "Test Framework Debug"  |
| FrameworkInformation | TestDebug            | "Test Test Debug"       | "Test Test Debug"       |
| FrameworkInformation | TestInformation      | "Test Test Information" | "Test Test Information" |
| FrameworkInformation | Error                | "Test Error"            | "Test Error"            |
| TestDebug            | FrameworkDebug       | "Test Framework Debug"  | ""                      |
| TestDebug            | FrameworkInformation | "Test Framework Debug"  | ""                      |
| TestDebug            | TestDebug            | "Test Test Debug"       | "Test Test Debug"       |
| TestDebug            | TestInformation      | "Test Test Information" | "Test Test Information" |
| TestDebug            | Error                | "Test Error"            | "Test Error"            |
| TestInformation      | FrameworkDebug       | "Test Framework Debug"  | ""                      |
| TestInformation      | FrameworkInformation | "Test Framework Debug"  | ""                      |
| TestInformation      | TestDebug            | "Test Test Debug"       | ""                      |
| TestInformation      | TestInformation      | "Test Test Information" | "Test Test Information" |
| TestInformation      | Error                | "Test Error"            | "Test Error"            |
| Error                | FrameworkDebug       | "Test Framework Debug"  | ""                      |
| Error                | FrameworkInformation | "Test Framework Debug"  | ""                      |
| Error                | TestDebug            | "Test Test Debug"       | ""                      |
| Error                | TestInformation      | "Test Test Information" | ""                      |
| Error                | Error                | "Test Error"            | "Test Error"            |


Scenario Outline: Logger output can be directed to Console or a custom output stream
	Given I have configured Logger WriteToConsole to <Write To Console>
    And I have configured Logger TestToolLog to write to my string
	And I set Logger to level FrameworkDebug
	When I call Logger with level FrameworkDebug and string <Test String>
	Then my string contains a Logger line ending with <TestLogOutput>
	And the console output contains a Logger line ending with <ConsoleOutput>
	Examples:
| Write To Console | Test String            | TestLogOutput          | ConsoleOutput          |
| true             | "Test Framework Debug" | ""                     | "Test Framework Debug" |
| false            | "Test Framework Debug" | "Test Framework Debug" | ""                     |

Scenario Outline: Logger output can be changed in flight
	Given I have configured Logger WriteToConsole to <Initial Write To Console>
    And I have configured Logger TestToolLog to write to my string
	And I set Logger to level FrameworkDebug
	And I call Logger with level FrameworkDebug and string <First Test String>
	When I change Logger WriteToConsole to <Second Write To Console>
	And I call Logger with level FrameworkDebug and string <Second Test String>
	Then my string contains a Logger line ending with <TestLogOutput>
	And the console output contains a Logger line ending with <ConsoleOutput>
	Examples:
| Initial Write To Console | Second Write To Console | Logger Level   | Write Level    | First Test String | Second Test String | TestLogOutput      | ConsoleOutput      |
| false                    | true                    | FrameworkDebug | FrameworkDebug | "My First string" | "My Second string" | "My First string"  | "My Second string" |
| true                     | false                   | FrameworkDebug | FrameworkDebug | "My First string" | "My Second string" | "My Second string" | "My First string"  |