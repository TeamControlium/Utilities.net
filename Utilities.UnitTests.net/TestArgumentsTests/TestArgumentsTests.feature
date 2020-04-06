Feature: TestArguments Tests
	In order to test TestArguments
	As a test automator
	I want to be able to pass parameters to tests/suite on commandline

    Scenario: Pass in a basic argument
	Given I have the following command line "test.exe -param1:hello"
	Then TestArguments has the data "hello" for argument "param1"

	Scenario: Quoted param with spaces
	Given I have the following command line "Test.exe --param1 "hello I have spaces""
	Then TestArguments has the data "hello I have spaces" for argument "param1"

	Scenario: Use documentation example commandline
	Given I have the following command line "Test.exe -param1 value1 --param2 /param3:"Test-:-work" /param4=happy -param5 "my param5 data" -param6"
	Then TestArguments has the data "value1" for argument "param1"
	And TestArguments has the data "" for argument "param2"
	And TestArguments has the data "Test-:-work" for argument "param3"
	And TestArguments has the data "happy" for argument "param4"
	And TestArguments has the data "my param5 data" for argument "param5"
	And TestArguments has the data "" for argument "param6"

	Scenario: Non-existant param returns null
	Given I have the following command line "Test.exe --param1 "hello I have spaces""
	Then TestArguments returns null for argument "param2"
