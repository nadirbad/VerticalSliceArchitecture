# NDepend Instructions

## Download and Install

Visit the official [NDepend](https://www.ndepend.com/) website and download the macOS version. Your download will be a ZIP file containing everything you need.

Unzip the downloaded file to a location of your choice. Let's say we've extracted it to `~/NDepend` for this guide. All further CLI commands will work as if you extracted NDepend to this path.

## Register Licence

```shell
dotnet ~/Ndepend/net8.0/NDepend.Console.MultiOS.dll --RegEval
```

After you should see a message like this:

```shell
Evaluation registered on this machine
Evaluation 14 days left
```

## Create NDepend project

```shell
dotnet ~/NDepend/net8.0/NDepend.Console.MultiOS.dll --cp ./VerticalSliceArchitecture.ndproj ~/code/github/VerticalSliceArchitecture/VerticalSliceArchitecture.sln
```

## Run Analysis

```shell
dotnet ~/NDepend/net8.0/NDepend.Console.MultiOS.dll VerticalSliceArchitecture.ndproj
```

## Improvements: shell script

The cross-platform executable is a .dll so running it is a bit long command line:

`dotnet ~/path/to/net8.0/NDepend.Console.MultiOS.dll`

So instead let's create a shell script to make it easier to run NDepend from the command line for our project

- Create a new file, e.g., run-ndepend.sh, in your project directory.
- Make the script executable.
- In the script, include the command to run your .NET project with NDepend and pass any additional arguments.

```bash
#!/bin/bash

# Check if the NDepend executable exists
NDEPEND_PATH=~/NDepend/net8.0/NDepend.Console.MultiOS.dll

if [ ! -f "$NDEPEND_PATH" ]; then
  echo "Error: NDepend executable not found at $NDEPEND_PATH"
  exit 1
fi

# Execute the dotnet command with NDepend and pass all arguments
dotnet "$NDEPEND_PATH" "$@"

```

### Steps to Use the run-ndepend.sh script

- Save the script as `run-ndepend.sh`.
- Run `chmod +x run-ndepend.sh` to make it executable.
- Use ./run-ndepend.sh <arguments> to run the script with any arguments for NDepend. For now to run analysis you can use the following command:

```shell
./run-ndepend.sh VerticalSliceArchitecture.ndproj
```
