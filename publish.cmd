SET nugetversion=0.1.1
dotnet pack ./KeyedSemaphores/KeyedSemaphores.csproj --configuration Release
nuget push ./KeyedSemaphores/bin/Release/KeyedSemaphores.%nugetversion%.nupkg -source nuget.org