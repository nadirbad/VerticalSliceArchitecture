#!/bin/bash

# Check if the NDepend executable exists
NDEPEND_PATH=~/NDepend/net8.0/NDepend.Console.MultiOS.dll

if [ ! -f "$NDEPEND_PATH" ]; then
  echo "Error: NDepend executable not found at $NDEPEND_PATH"
  exit 1
fi

# Execute the dotnet command with NDepend and pass all arguments
dotnet "$NDEPEND_PATH" "$@"
