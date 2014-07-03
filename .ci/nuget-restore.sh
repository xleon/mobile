#!/bin/sh -x

mkdir .nuget
wget http://nuget.org/nuget.exe -O .nuget/NuGet.exe

mono --runtime=v4.0 .nuget/NuGet.exe restore $1
exit $?
