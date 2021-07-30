# NugetTree
Project which reads nuget dependencies and has options for output

## To Run
`dotnet NugetTree.dll "C:\path\to\solutionfile.sln" [output type(s)]`

Available outputs:
```
-c   "Conflicts" => lists version mismatches by dependency name
-f   "Flat" => lists all dependencies by project in a flattened view
-t   "Tree" => lists full nuget dependency tree
-fv  "Framework Version" => lists projects by targeted framework
-nr  "Non-Recursive" => lists only top level dependencies, by project
```

*Example*
`dotnet NugetTree.dll "NugetTree.sln" -c`

#### Multiple outputs can be passed at the same time and will be displayed in the same order they appear in the argument list
*Example*
`dotnet NugetTree.dll "NugetTree.sln" -t -c`


## How to create your own Nuget tree output:

1. Clone this repository.
1. Make sure that you have dotnet installed on your machine (Core 3.1).
1. Navigate to the NugetTree directory and run dotnet build. This will build the solution and place an executable .dll inside the ``src/NugetTree/bin/Debug/netcoreapp3.1/`` directory.
1. Execute the dll using the command ```dotnet src/NugetTree/bin/Debug/netcoreapp3.1/NugetTree.dll "../octokit.net/Octokit.sln" -t``` Replace ``../octokit.net/Octokit.sln`` with the path to the solution file.
