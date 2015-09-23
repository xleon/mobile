#!/bin/bash
DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

# Download Nuget binary
NUGET="$DIR/.ci/teamcity/tools/NuGet/NuGet.exe"
if [[ ! -e ${NUGET} ]]; then
    curl -o ${NUGET} https://az320820.vo.msecnd.net/downloads/nuget.exe
fi

mono --runtime=v4.0 ${NUGET} install FAKE -Version 4.4.2 -OutputDirectory packages
mono --runtime=v4.0 ${NUGET} install NUnit.Runners -Version 2.6.4 -OutputDirectory packages
mono --runtime=v4.0 packages/FAKE.4.1.4/tools/FAKE.exe .build.fsx $@
