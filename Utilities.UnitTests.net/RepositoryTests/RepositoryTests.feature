Feature: Repository Tests
	In order to test Repository
	As a test automator
	I want to be able to save and recall test data

    Scenario Outline: 1.0.0 - Save and recall a string to Repository
	Given I have saved string <Data Item> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name> 
	Then the recalled 1 value matches the saved 1 value
	Examples:
	| Repository | Data Item | Category Name | Item Name |
	| Local      | "My data" | "MyCategory"  | "MyItem"  |
	| Global     | "My data" | "MyCategory"  | "MyItem"  |

    Scenario Outline: 1.0.1 - Save and recall two strings from same Category
	Given I have saved string <Data Item1> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name1>
	And I have saved string <Data Item2> (Item 2) in Repository <Repository>, Category <Category Name>, Item Name <Item Name2> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name1> 
	And I recall (Item 2) from <Repository>, Category <Category Name>, Item Name <Item Name2> 
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 2 value
	Examples:
	| Repository | Data Item1    | Data Item2    | Category Name | Item Name1    | Item Name2    |
	| Local      | "My data one" | "My data two" | "MyCategory"  | "My Item one" | "My Item two" |
	| Global     | "My data one" | "My data two" | "MyCategory"  | "My Item one" | "My Item two" |



    Scenario Outline: 1.0.2 - Save and recall two strings from different Categories
	Given I have saved string <Data Item1> (Item 1) in Repository <Repository>, Category <Category Name1>, Item Name <Item Name1>
	And I have saved string <Data Item2> (Item 2) in Repository <Repository>, Category <Category Name2>, Item Name <Item Name2> 
	When I recall (Item 1) from <Repository>, Category <Category Name1>, Item Name <Item Name1> 
	And I recall (Item 2) from <Repository>, Category <Category Name2>, Item Name <Item Name2> 
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 2 value
	Examples:
	| Repository | Data Item1    | Data Item2    | Category Name1   | Category Name2   | Item Name1    | Item Name2    |
	| Local      | "My data one" | "My data two" | "MyCategory one" | "MyCategory two" | "My Item one" | "My Item two" |
	| Global     | "My data one" | "My data two" | "MyCategory one" | "MyCategory two" | "My Item one" | "My Item two" |

	Scenario Outline: 1.0.3 - Save and recall two strings using Local and Global repositories
	Given I have saved string <Data Item1> (Item 1) in Repository <Repository1>, Category <Category Name1>, Item Name <Item Name1>
	And I have saved string <Data Item2> (Item 2) in Repository <Repository2>, Category <Category Name2>, Item Name <Item Name2> 
	When I recall (Item 1) from <Repository1>, Category <Category Name1>, Item Name <Item Name1> 
	And I recall (Item 2) from <Repository2>, Category <Category Name2>, Item Name <Item Name2> 
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 2 value
	Examples:
	| Repository1 | Repository2 | Data Item1    | Data Item2    | Category Name1 | Category Name2 | Item Name1 | Item Name2 |
	| Local       | Global      | "My data one" | "My data two" | "MyCategory"   | "MyCategory"   | "My Item"  | "My Item"  |

	
	Scenario Outline: 1.0.4 - Save and recall an integer to Repository
	Given I have saved integer <Data Item> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name> 
	Then the recalled 1 value matches the saved 1 value
	Examples:
	| Repository | Data Item | Category Name | Item Name |
	| Local      | 23        | "MyCategory"  | "MyItem"  |
	| Global     | 23        | "MyCategory"  | "MyItem"  |


    Scenario Outline: 1.0.5 - Save and recall one string and one int from same Category
	Given I have saved string <Data Item1> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name1>
	And I have saved integer <Data Item2> (Item 2) in Repository <Repository>, Category <Category Name>, Item Name <Item Name2> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name1> 
	And I recall (Item 2) from <Repository>, Category <Category Name>, Item Name <Item Name2> 
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 2 value
	Examples:
	| Repository | Data Item1    | Data Item2 | Category Name | Item Name1    | Item Name2    |
	| Local      | "My data one" | 24         | "MyCategory"  | "My Item one" | "My Item two" |
	| Global     | "My data one" | 24         | "MyCategory"  | "My Item one" | "My Item two" |

    Scenario Outline: 1.0.6 - Change a string value in test data
	Given I have saved string <Data Item1> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name>
	And I have saved string <Data Item2> (Item 2) in Repository <Repository>, Category <Category Name>, Item Name <Item Name> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name> 
	Then the recalled 1 value matches the saved 2 value
	Examples:
	| Repository | Data Item1    | Data Item2    | Category Name | Item Name    | Item Name    |
	| Local      | "My data one" | "My data two" | "MyCategory"  | "MyItem one" | "MyItem two" |
	| Global     | "My data one" | "My data two" | "MyCategory"  | "MyItem one" | "MyItem two" |

	Scenario Outline: 1.0.7 - Change an integer value in test data
	Given I have saved integer <Data Item1> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name>
	And I have saved integer <Data Item2> (Item 2) in Repository <Repository>, Category <Category Name>, Item Name <Item Name> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name> 
	Then the recalled 1 value matches the saved 2 value
	Examples:
	| Repository | Data Item1 | Data Item2 | Category Name | Item Name    | Item Name    |
	| Local      | 23         | 65         | "MyCategory"  | "MyItem one" | "MyItem two" |
	| Global     | 55         | 93         | "MyCategory"  | "MyItem one" | "MyItem two" |


    Scenario Outline: 1.0.8 - Change an integer value to a string value in test data
	Given I have saved integer <Data Item1> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name>
	And I have saved string <Data Item2> (Item 2) in Repository <Repository>, Category <Category Name>, Item Name <Item Name> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name> 
	Then the recalled 1 value matches the saved 2 value
	Examples:
	| Repository | Data Item1 | Data Item2        | Category Name | Item Name    | Item Name    |
	| Local      | 23         | "Im a string now" | "MyCategory"  | "MyItem one" | "MyItem two" |
	| Global     | 55         | "Im a string now" | "MyCategory"  | "MyItem one" | "MyItem two" |

	Scenario Outline: 1.0.9 - Recall a strongly typed String
	Given I have saved string <Data Item> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name> as a ["System.String"]
	Then the recalled 1 value matches the saved 1 value
	Examples:
	| Repository | Data Item | Category Name | Item Name |
	| Local      | "My data" | "MyCategory"  | "MyItem"  |
	| Global     | "My data" | "MyCategory"  | "MyItem"  |


	Scenario Outline: 1.1.0 - Correct error if I try to get a string as an integer
	Given I have saved string <Data Item> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name> 
	When I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name> as a ["System.Int32"]
	Then the recalled 1 value is an exception with innermost exception message "Expected type [Int32] but got type System.String"
	Examples:
	| Repository | Data Item | Category Name | Item Name |
	| Local      | "My data" | "MyCategory"  | "MyItem"  |
	| Global     | "My data" | "MyCategory"  | "MyItem"  |


    Scenario Outline: 1.1.1 - Verify Local and Global repositories can be cleared
	Given I have saved string <Data Item> (Item 1) in Repository <Repository>, Category <Category Name>, Item Name <Item Name> 
	When I clear the <Repository> repository
	And I recall (Item 1) from <Repository>, Category <Category Name>, Item Name <Item Name>
	Then the recalled 1 value is an exception with innermost exception message <Exception Text>
	Examples:
	| Repository | Data Item | Category Name | Item Name | Exception Text                                                                                                      |
	| Local      | "My data" | "MyCategory"  | "MyItem"  | "Category name My Category does not exist in Local (ThreadID 4) repository. Category name must be valid and exist." |
	| Global     | "My data" | "MyCategory"  | "MyItem"  | "Category name MyCategory does not exist in Global repository. Category name must be valid and exist"               |

	
	Scenario: 1.2.0 - Clone Global test data to Local, overwriting any existing data
	Given I have saved string "Saved to Global" (Item 1) in Repository Global, Category "MyCat", Item Name "MyItem1"
	And I have saved string "Saved to Local" (Item 2) in Repository Local, Category "MyCat", Item Name "MyItem2" 
	And I clone Global test data to Local test data, overwriting any existing
	When I recall (Item 1) from Global, Category "MyCat", Item Name "MyItem1"
	And I recall (Item 2) from Local, Category "MyCat", Item Name "MyItem1"
	Then the recalled 1 value matches the saved 1 value
	And the recalled 2 value matches the saved 1 value

	Scenario: 1.2.1 - Verify data is not overwritten if requested
	Given I have saved string "Saved to Global" (Item 1) in Repository Global, Category "MyCat", Item Name "MyItem1"
	And I have saved string "Saved to Local" (Item 2) in Repository Local, Category "MyCat", Item Name "MyItem2" 
	When I clone Global test data to Local test data, not overwriting any existing
	Then an Exception is thrown with text "Wobble"

	Scenario: 1.2.2 - Verify values are cloned not references
	Given I have saved string "My cloned Data" (Item 1) in Repository Global, Category "MyCat", Item Name "MyItem1"
	And I clone Global test data to Local test data, overwriting any existing
	When I have saved string "My new Global Data" (Item 2) in Repository Global, Category "MyCat", Item Name "MyItem1"
	And I recall (Item 1) from Global, Category "MyCat", Item Name "MyItem1"
	And I recall (Item 2) from Local, Category "MyCat", Item Name "MyItem1"
	Then the recalled 1 value matches the saved 2 value
	And the recalled 2 value matches the saved 1 value