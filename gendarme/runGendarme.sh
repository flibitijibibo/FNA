#!/bin/bash

# Filename of the FNA assembly output
ASSEMBLYNAME="../MonoGame.Framework/bin/SDL2/Debug/MonoGame.Framework.dll"

# Move to script's directory
cd "`dirname "$0"`"

# Run Gendarme using the FNA ruleset
gendarme --ignore fna.ignore --html gendarme.html $ASSEMBLYNAME
