@Utilities_TestData
Feature: Utilities_TestData
	In order to test Test Data repository
	As a test automator
	I want to be able to save and recall test data

    Scenario: Save and recall a string to test data
	Given I have saved Test Data item 1 "My Data" in item named "MyName" in category "MyCategory"
	When I recall the Test Data item 1 named "MyName" in category "MyCategory"
	Then the recalled 1 value matches the saved 1 value

    Scenario: Save and recall two strings from same Category
	Given I have saved Test Data item 1 "My Data one" in item named "MyName1" in category "MyCategory"
	And I have saved Test Data item 2 "My Data two" in item named "MyName2" in category "MyCategory"
	When I recall the Test Data item 1 named "MyName1" in category "MyCategory"
	And I recall the Test Data item 2 named "MyName2" in category "MyCategory"
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 2 value

	Scenario: Save two strings to a category then recall
	Given I have a Test data item "My Data one" in item named "MyName1"
	And I have a Test data item "My Data two" in item named "MyName2"
	And I have saved items in category "MyCategory"
	When I recall the Test Data item 1 named "MyName1" in category "MyCategory"
	And I recall the Test Data item 2 named "MyName2" in category "MyCategory"
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 2 value

    Scenario: Save and recall two strings from different Categories
	Given I have saved Test Data item 1 "My Data one" in item named "MyName" in category "MyCategory1"
	And I have saved Test Data item 2 "My Data two" in item named "MyName" in category "MyCategory2"
	When I recall the Test Data item 1 named "MyName" in category "MyCategory1"
	And I recall the Test Data item 2 named "MyName" in category "MyCategory2"
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 2 value

	Scenario: Save and recall an integer to test data
	Given I have saved Test Data item 1 69 in item named "MyName" in category "MyCategory"
	When I recall the Test Data item 1 named "MyName" in category "MyCategory"
	Then the recalled 1 value matches the saved 1 value

    Scenario: Save and recall one string and one int from same Category
	Given I have saved Test Data item 1 "My Data one" in item named "MyName1" in category "MyCategory"
	And I have saved Test Data item 2 222 in item named "MyName2" in category "MyCategory"
	When I recall the Test Data item 1 named "MyName1" in category "MyCategory"
	And I recall the Test Data item 2 named "MyName2" in category "MyCategory"
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 2 value

    Scenario: Change a string value in test data
	Given I have saved Test Data item 1 "My Data" in item named "MyName" in category "MyCategory"
	When I change Test Data item 1 named "MyName" in category "MyCategory" to "New Value"
	And I recall the Test Data item 1 named "MyName" in category "MyCategory"
	Then the recalled 1 value matches the saved 1 value

    Scenario: Change an integer value to a string value in test data
	Given I have saved Test Data item 1 69 in item named "MyName" in category "MyCategory"
	When I change Test Data item 1 named "MyName" in category "MyCategory" to "New Value"
	And I recall the Test Data item 1 named "MyName" in category "MyCategory"
	Then the recalled 1 value matches the saved 1 value

    Scenario: Correct error if I try to get a string as an integer
	Given I have saved Test Data item 1 "My Data" in item named "MyName" in category "MyCategory"
	When I recall the Test Data item 1 named "MyName" in category "MyCategory" as an integer
    Then the recalled 1 value is an exception with innermost exception message "Expected type [Int32] but got type [System.String]."

    Scenario: Save one item of test data then clear
	Given I have saved Test Data item 1 "My Data" in item named "MyName" in category "MyCategory"
	When I clear the test data
	And I recall the Test Data item 1 named "MyName" in category "MyCategory"
    Then the recalled 1 value is an exception with innermost exception message "Category does not exist Parameter name: category Actual value was MyCategory."

	Scenario: Save two items of test data in different categories then clear
	Given I have saved Test Data item 1 "My Data" in item named "MyName" in category "MyCategory1"
	Given I have saved Test Data item 2 66 in item named "MyName" in category "MyCategory2"
	When I clear the test data
	And I recall the Test Data item 1 named "MyName" in category "MyCategory1"
    Then the recalled 1 value is an exception with innermost exception message "Category does not exist Parameter name: category Actual value was MyCategory1."
	When I recall the Test Data item 2 named "MyName" in category "MyCategory2"
    Then the recalled 2 value is an exception with innermost exception message "Category does not exist Parameter name: category Actual value was MyCategory2."
