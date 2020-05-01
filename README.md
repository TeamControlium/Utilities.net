# TeamControlium - Utilities .NET Library

Library providing a suite of test automation oriented utilities.

## Main Areas

### Detokeniser
Resolves tokens (enclosed in {}) within strings.  Tokens can be for random data, dates, date-offests, financial year dates etc...

### General
General set of assistance methods; such as Cleaning strings for use in filenames, extracting text from HTML, determining if a string value is resolved to boolean true or false tc...

### Log
Provides full logging capabilities.  Current open-source Logging projects (Log4Net, nLog etc) are either too specific, too heavyweight or too complex.  So TeamControlium include a simple, lightweight and threadsafe logging system tailored for automated testing projects.

### Repository
Thread safe and aware test data and settings storage system, used by TeamControlium components, for test-wide storage/recall of data used within tests.  Data can be made available to all threads of a multi-threaded test suite or to just the local thread etc...

### TestArguments
For use where tests or test suites have data passed from command-line.  Stores and makes easily available arguments passed using the commandline.

## Getting Started

Library is available on NuGet (TeamControlium.Utilities). 

[Full API documentation](https://teamcontrolium.github.io/Utilities.net)

### Examples

To be written

### Dependencies

.Net Core 3.1

## Unit tests

Library uses Specflow/MSTest for it's unit tests and must always run to pass before merging to develop branch.

## Coding Style

Vanilla Stylecop is used for policing of coding style with zero violations allowed.

## Built With

* Visual Studio Community 2019 with Sandcastle for online usage documentation

## Contributing

Contact TeamControlium contributors for possible contributions

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## Authors

* **Mat Walker** - *Initial work and maintenance* - [v-mwalk](https://github.com/v-mwalk)
* **Mike Burns** - *Maintenance and work on original Utilities project* - [maddogmikeb](https://github.com/maddogmikeb)

See also the list of [people](https://github.com/TeamControlium/NonGUI.net/people) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details

## Acknowledgments

* Selenium Contributors
