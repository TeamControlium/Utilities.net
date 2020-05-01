Feature: General Tests
	In order to test General Class methods
	As a test automator
	I want to be able to hold my head high and say themethods work fine.

    Scenario: 1.0.0 - Write data to a file
	Given I have a unique filename
	When I write data "xyz" to the file, overwriting if file exists
	Then the file contains the text "xyz"

	Scenario: 1.0.1 - Overwrite overwrites last file
	Given I have a unique filename
	When I write data "xyz" to the file, overwriting if file exists
	And I write data "abc" to the file, overwriting if file exists
	Then the file contains the text "abc"

	Scenario: 1.0.2 - Append appends to last file
	Given I have a unique filename
	When I write data "xyz" to the file, overwriting if file exists
	And I write data "abc" to the file, appending if file exists
	Then the file contains the text "xyzabc"

	Scenario: 1.0.3 - AutoVersion versions file writes
	Given I have a unique filename
	When I write data "xyz" to the file, overwriting if file exists
	And I write data "abc" to the file, creating new file if file already exists
	Then the file contains the text "xyz"
	And the file (version 1) contains the text "abc"

	Scenario: 1.0.4 - Append to versioned file
	Given I have a unique filename
	When I write data "xyz" to the file, overwriting if file exists
	And I write data "abc" to the file, creating new file if file already exists
	And I write data "def" to the file, appending if file exists
	Then the file contains the text "xyz"
	And the file (version 1) contains the text "abcdef"

	Scenario: 2.0.0 - Web URL can be converted to valid Filename
	Given I have a URL "http:\\www.me.com\path?qery=hello#whatever=more"
	When I convert URL to a filename
	Then Filename is "http_www_me_com_path_qery_hello_whatever_more"

	Scenario: 2.0.1 - Empty string passed to ConvertURLToValidFilename returns empty string
	Given I have a URL ""
	When I convert URL to a filename
	Then Filename is ""