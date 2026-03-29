#!/bin/bash
rm src/Resources/SharedResource.Designer.cs
dotnet build && dotnet run --project src/backend.csproj