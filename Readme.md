# Toggl Mobile

## Setting up

You should begin by initializing git submodules we reference:

	$ git submodule init
	$ git submodule update

Phoebe projects are configured to automatically add all of their files. The added files shouldn't be
committed back to the repository. To make life easier we instruct git to assume they're unchanged:

	$ git update-index --assume-unchanged Phoebe/Phoebe.*.csproj

_(To undo the previous command you need to replace `--assume-unchanged` with `--no-assume-unchanged`
flag.)_

We use some NuGet packages, so you need to install an addin for Xamarin Studio:
[installation instructions](https://github.com/mrward/monodevelop-nuget-addin#installation).
After successfully installing the addin, you need to right click on the solution and select
"Restore NuGet Packages".