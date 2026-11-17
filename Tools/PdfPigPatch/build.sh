#!/bin/sh
# Builds the PdfPig patch tool with Unity's bundled .NET SDK and Mono.Cecil.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$(cd "$HERE/../.." && pwd)"
DOTNET="$(cat "$PROJECT/Tools/DocumentTools/Build/dotnet-path")"
SDK="$(dirname "$DOTNET")"
CSC="$(ls "$SDK"/sdk/*/Roslyn/bincore/csc.dll | sort -V | tail -1)"
REFS="$(ls -d "$SDK"/packs/Microsoft.NETCore.App.Ref/*/ref/net*/ | sort -V | tail -1)"
CECIL="$(ls "$PROJECT"/Library/PackageCache/com.unity.nuget.mono-cecil@*/Mono.Cecil.dll | head -1)"
RUNTIME="$(ls "$SDK"/shared/Microsoft.NETCore.App/ | sort -V | tail -1)"
mkdir -p "$HERE/Build"
REFERENCES=""
for dll in "$REFS"*.dll; do REFERENCES="$REFERENCES -r:$dll"; done
"$DOTNET" "$CSC" -nologo -out:"$HERE/Build/PdfPigPatch.dll" $REFERENCES -r:"$CECIL" "$HERE/Program.cs"
cp "$CECIL" "$HERE/Build/"
printf '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"%s"}}}' "$RUNTIME" > "$HERE/Build/PdfPigPatch.runtimeconfig.json"
echo "$DOTNET" > "$HERE/Build/dotnet-path"
