# PdfPig patch

PdfPig 0.1.16 throws `Unsupported operating system` on the Quest. Its `SystemFontFinder` only knows Windows, macOS and
Linux, and under IL2CPP on Android `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)` is false. Every PDF then fails
to open with a `TypeInitializationException`.

`Assets/Plugins/PdfPig/UglyToad.PdfPig.Fonts.dll` is patched so that check is always true: any system that is not
Windows or macOS uses the Linux font lister, which only reads font folders that exist.

- `./build.sh` builds the tool with Unity's .NET SDK and the Mono.Cecil package.
- `dotnet Build/PdfPigPatch.dll ../../Assets/Plugins/PdfPig/UglyToad.PdfPig.Fonts.dll` applies the patch. Running it
  again does nothing. Add `--dry` to only show the check it would change.
- `original/` keeps the unpatched DLL.

Run it again after upgrading PdfPig, unless the new version supports Android.
