SET nugetversion=0.1.1
dotnet pack ./KeyedSemaphores/KeyedSemaphores.csproj --configuration Release
nuget push ./KeyedSemaphores/bin/Release/KeyedSemaphores.%nugetversion%.nupkg -skipduplicate -source nuget.org
nuget push ./KeyedSemaphores/bin/Release/KeyedSemaphores.%nugetversion%.nupkg -skipduplicate -source Github