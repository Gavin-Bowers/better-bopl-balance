# Bopl Battle Mod Template
This repository contains a template that can be used to develop functional mods for Bopl Battle.

## Contents
This repository contains the following items:

- `README.md` (this file)
- `TemplateMod.csproj` (the project file for the mod)
- `.gitignore` (a list of files and directories to exclude from Git, use this to remove IDE configurations and other non-copyable files)
- `dependencies.txt` (a list of .dll files from Bopl Battle's `BoplBattle_Data/Managed` needed as assembly references)
- `NuGet.Config` (a list of NuGet packages used in the project, most IDEs give you a GUI to manage NuGet packages so avoid manually editing it)
- `ModInfo.cs` (a C# file that contains basic mod info, such as the version, GUID, and name)
- `Plugin.cs` (a C# file containing the core of your mod, ideally use this for general loading tasks and create other files to handle more aspects of your mod)

## Setup
1. Load the `TemplateMod.csproj` into your IDE
2. Add the assembly references (typically a menu in your IDE, VS Code may require manual references)
3. Customize `ModInfo.cs` with your mod's info
4. Start modding!

## License
You are free to use this repository as you like without restriction. **No credit is required and all forms of reproduction are allowed**.