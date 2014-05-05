# Toggl Mobile

This repository contains the source code for building the native Toggl Android and iOS applications.
These applications are build using [Xamarin](http://xamarin.com/) products to allow for maximum
code reuse and faster development times.

## Repository structure

The repository consists of the following projects:

- Phoebe - shared data models and business logic across platforms
- Joey - Android application (UI & Android specific code)
- Ross - iOS application (UI & iOS specific code)
- Tests - unit tests for testing code in Phoebe

Phoebe has several project files ([Phoebe.Android.csproj](https://github.com/toggl/mobile/blob/master/Phoebe/Phoebe.Android.csproj), [Phoebe.iOS.csproj](https://github.com/toggl/mobile/blob/master/Phoebe/Phoebe.iOS.csproj) and [Phoebe.Desktop.csproj](https://github.com/toggl/mobile/blob/master/Phoebe/Phoebe.Desktop.csproj)) which compile the same code for different target platforms
(Android, iOS and desktop).

## Setting up

You should begin by initializing git submodules we reference:

	$ git submodule init
	$ git submodule update

We use some NuGet packages, so you need to install an addin for Xamarin Studio:
[installation instructions](https://github.com/mrward/monodevelop-nuget-addin#installation).
After successfully installing the addin, you need to right click on the solution and select
"Restore NuGet Packages".

Before compiling any of the projects, there is one last file you need to edit.
[Phoebe/Build.cs](https://github.com/toggl/mobile/blob/master/Phoebe/Build.cs) contains various
configuration parameters for different components, which you need to fill in for the app to run.

## Contributing

Want to contribute? Great! Just [fork](https://github.com/toggl/mobile/fork) the project, make your
changes and submit a [pull request](https://github.com/toggl/mobile/pulls).

### Code style

We're lazy, so instead of having official coding style, we have a Xamarin Studio code formatting
settings. And our IDEs configured to automatically format the file when saving.

If you plan on contributing it's best to have the same settings. You can copy the
[Default.mdpolicy.xml](https://github.com/toggl/mobile/blob/master/Default.mdpolicy.xml) to
`~/Library/XamarinStudio-4.0/Policies/` directory (on OSX).

Then make sure that you have "Custom" policy selected under Xamarin Studio settings > Source Code >
Code Formatting > C# source code. And don't forget to turn on "Format document on save" under
Xamarin Studio settings > Text Editor > Behavior.

## License

The code in this repository is licensed under the [BSD 3-clause license](https://github.com/toggl/mobile/blob/master/LICENSE).
