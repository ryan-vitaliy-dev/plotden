#!/bin/bash
rm src/Application/Resources/SharedResource.Designer.cs
dotnet build && dotnet run --project src/API/API.csproj