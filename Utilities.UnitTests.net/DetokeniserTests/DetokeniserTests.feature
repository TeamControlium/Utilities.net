Feature: Detokeniser
	In order to process tokens
	As a test automator
	I want to be able to convert tokens to real texts

	#########################################################################################################
	##
	##  DATES
	##
	##

	#
	# Basic dates
	#

    Scenario: Convert today's date token to the date we run this test
	Given I have a string "{date;today;dd/MM/yyyy}"
	When I process the token to a string
	Then the string is today's date in the format "dd/MM/yyyy"

	Scenario: Convert yesterday's date token to the day before we run this test
	Given I have a string "{date;yesterday;dd/MM/yyyy}"
	When I process the token to a string
	Then the string is yesterday's date in the format "dd/MM/yyyy"

	Scenario: Convert tomorrows's date token to the day after we run this test
	Given I have a string "{date;tomorrow;dd/MM/yyyy}"
	When I process the token to a string
	Then the string is tomorrows's date in the format "dd/MM/yyyy"

	Scenario: Date command and verb not case sensitive
	Given I have a string "{Date;Today;dd/MM/yyyy}"
	When I process the token to a string
	Then the string is today's date in the format "dd/MM/yyyy"

	#
	# Output formatting
	#

	Scenario: Get the day of the week when test is run
	Given I have a string "{date;today;dddd}"
	When I process the token to a string
	Then the string is today's date in the format "dddd"

	#
	# Complex offsets
	#
    Scenario: Convert date for a days offset in the past
	Given I have a string "{date;AddDays(-10);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is the date -10 "days" in the format "dd/MM/yyyy"

	Scenario: Convert date for a days offset in the future
	Given I have a string "{date;AddDays(10);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is the date 10 "days" in the format "dd/MM/yyyy"

    Scenario: Convert date for a days offset in the future (with a plus sign)
	Given I have a string "{date;AddDays(+10);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is the date 10 "days" in the format "dd/MM/yyyy"

    Scenario: Convert date for a months offset in the past
	Given I have a string "{date;AddMonths(-19);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is the date -19 "months" in the format "dd/MM/yyyy"

	Scenario: Convert date for a months offset in the future
	Given I have a string "{date;AddMonths(17);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is the date 17 "months" in the format "dd/MM/yyyy"

	Scenario: Convert date for a years offset in the past
	Given I have a string "{date;AddYears(-25);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is the date -25 "years" in the format "dd/MM/yyyy"

	Scenario: Convert date for a years offset in the future
	Given I have a string "{date;AddYears(25);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is the date 25 "years" in the format "dd/MM/yyyy"

	#############################################################
	##
	##  Random
	##
	##

	#
	# Date
	#

	Scenario:I can get a random date from a single date
	Given I have a string "{random;date(12-07-2001,12-07-2001);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is a date between "12/07/2001" and "12/07/2001"

	Scenario:I can get a random date from one of two consequtive dates
	Given I have a string "{random;date(11-07-2001,12-07-2001);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is a date between "11/07/2001" and "12/07/2001"

	Scenario:I get correct error if min date after max date
	Given I have a string "{random;date(12-07-2001,11-07-2001);dd/MM/yyyy}"
	When I process the token to a string
	Then the string is "Error processing token {random;date(12-07-2001,11-07-2001);dd/MM/yyyy} (Maximum date earlier than Minimum date! Expect {random;date(dd-MM-yyyy,dd-MM-yyyy);<format>} Mindate = 12/07/2001, Maxdate = 11/07/2001)"

	#
	# Float
	#
	Scenario:I can get a random positive floating point number with no decimal places
	Given I have a string "{random;float(0,1);0}"
	When I process the token to a string
	Then the string matches regular expression "^[0-9]{1}$"
	And the string is a number between 0 and 1

	Scenario:I can get a random positive floating point number with 1 decimal places
	Given I have a string "{random;float(0,1);0.0}"
	When I process the token to a string
	Then the string matches regular expression "^[0-1]{1}\.[0-9]{1}$"
	And the string is a number between 0 and 1

	Scenario:I can get a random negative floating point number with 1 decimal places
	Given I have a string "{random;float(-1,0);0.0}"
	When I process the token to a string
	Then the string matches regular expression "^-[0-9]{1}\.[0-9]{1}$"
	And the string is a number between -1 and 0

	Scenario:I can get a random positive floating point number between tiny numbers
	Given I have a string "{random;float(0.0003,0.0004);0.000000}"
	When I process the token to a string
	Then the string matches regular expression "^0{1}\.0{3}[0-9]{3}$"
	And the string is a number between 0.0003 and 0.0004

	Scenario:I can get a random positive floating point number with leading zeros
	Given I have a string "{random;float(3,4);000}"
	When I process the token to a string
	Then the string matches regular expression "^0{2}[3-4]{1}$"
	And the string is a number between 3 and 4

	#
	# TFN
	#
	Scenario:I can get a random valid TFN
	Given I have a string "{Random;AustralianTFN}"
	When I process the token to a string
	Then the string is a valid Australian TFN

	#
	# Set of characters
	#
	Scenario:I can get a single random character from a given set
	Given I have a string "{random;from(abc);1}"
	When I process the token to a string
	Then the string matches regular expression "^.{1}$"
	And the string is 1 characters from "abc"

	Scenario:I can get a multiple random characters from a given set
	Given I have a string "{random;from(abcdef);10}"
	When I process the token to a string
	Then the string matches regular expression "^.{10}$"
	And the string is 10 characters from "abcdef"


	Scenario: Nested tokens
	Given I have a string "{random;digits;{random;from(368);1}}"
	When I process the token to a string
	Then the string length is one of [368]

	Scenario: Escaped opening token at start of string
	Given I have a string "\{random;from(abc);1}"
	When I process the token to a string
	Then the string is "{random;from(abc);1}"

	Scenario: Escaped opening token in string
	Given I have a string "some chars \{random;from(abc);1}"
	When I process the token to a string
	Then the string is "some chars {random;from(abc);1}"

	Scenario: Non-Escaped opening token at start of string
	Given I have a string "\\{random;from(aaa);1}"
	When I process the token to a string
	Then the string is "\a"

	Scenario: Non-Escaped opening token in string
	Given I have a string "some chars \\{random;from(bbb);1}"
	When I process the token to a string
	Then the string is "some chars \b"

	Scenario: Mixed Escaped opening token at start of string
	Given I have a string "\\\\\{random;from(abc);1}"
	When I process the token to a string
	Then the string is "\\{random;from(abc);1}"

	Scenario: Mixed Escaped opening token in string
	Given I have a string "some chars \\\\\\\{random;from(abc);1}"
	When I process the token to a string
	Then the string is "some chars \\\{random;from(abc);1}"

	Scenario: Mixed Non-Escaped opening token at start of string
	Given I have a string "\\\\\\{random;from(ccc);1}"
	When I process the token to a string
	Then the string is "\\\c"

	Scenario: Mixed Non-Escaped opening token in string
	Given I have a string "some chars \\\\\\\\{random;from(ddd);1}"
	When I process the token to a string
	Then the string is "some chars \\\\d"