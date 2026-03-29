#!/bin/bash

# To add a new migration, run this script with the name of the migration as an argument.
# This script is just to avoid having to specify the output directory for the migration every time.
# Without it, the Migrations folder would be created in the root of the project instead of in the Infrastructure/Persistence folder.
dotnet ef migrations add "$1" -o Infrastructure/Persistence/Migrations