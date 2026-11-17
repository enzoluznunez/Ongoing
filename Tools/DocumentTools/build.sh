#!/bin/bash
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$(cd "$HERE/../.." && pwd)"
UNITY="$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app | sort -V | tail -1)"
SDK="$UNITY/Contents/Resources/Scripting/DotNetSdk"
CSC="$(ls "$SDK"/sdk/*/Roslyn/bincore/csc.dll | sort -V | tail -1)"
REFS="$(ls -d "$SDK"/packs/Microsoft.NETCore.App.Ref/*/ref/net* | sort -V | tail -1)"
mkdir -p "$HERE/Build"
REFERENCES=()
for dll in "$REFS"/*.dll "$PROJECT"/Assets/Plugins/PdfPig/*.dll; do REFERENCES+=("-r:$dll"); done
"$SDK/dotnet" "$CSC" -nologo -langversion:latest -nowarn:CS8632 "${REFERENCES[@]}" -out:"$HERE/Build/Tools.dll" \
  "$HERE"/Program.cs "$HERE"/Samples.cs "$HERE"/SampleWriter.cs "$HERE"/UnityStubs.cs \
  "$PROJECT/Assets/AccountingImmersiveAnalytics/Documents/PdfDocumentReader.cs" "$PROJECT/Assets/AccountingImmersiveAnalytics/Documents/DocumentFile.cs"
cp "$HERE/Tools.runtimeconfig.json" "$HERE/Build/Tools.runtimeconfig.json"
cp "$PROJECT"/Assets/Plugins/PdfPig/*.dll "$HERE/Build/"
echo "$SDK/dotnet" > "$HERE/Build/dotnet-path"
