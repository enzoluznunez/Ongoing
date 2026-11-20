// Patches PdfPig so it opens PDFs on Android (Quest).
//
// PdfPig's SystemFontFinder picks a font lister for Windows, macOS or Linux and otherwise throws
// "Unsupported operating system". Under IL2CPP on Android, RuntimeInformation.IsOSPlatform(OSPlatform.Linux) is false,
// so every PDF fails to open. This replaces that one Linux check with "true", so any other system is treated as Linux.
// The Linux lister only looks for font folders that exist; text extraction does not need system fonts.
//
// Usage: dotnet Build/PdfPigPatch.dll <path to UglyToad.PdfPig.Fonts.dll> [--dry]
using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 1) { Console.Error.WriteLine("Give the path to UglyToad.PdfPig.Fonts.dll."); return 2; }
        string path = Path.GetFullPath(args[0]);
        bool dry = args.Contains("--dry");

        var module = ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(path)));
        TypeDefinition finder = module.GetTypes().FirstOrDefault(t => t.FullName == "UglyToad.PdfPig.Fonts.SystemFonts.SystemFontFinder");
        if (finder == null) { Console.Error.WriteLine("SystemFontFinder was not found; this is not the PdfPig version this patch knows."); return 1; }

        int patched = 0;
        foreach (TypeDefinition type in new[] { finder }.Concat(finder.NestedTypes))
        foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
        {
            var il = method.Body.Instructions;
            for (int i = 0; i + 1 < il.Count; i++)
            {
                if (!IsCall(il[i], "System.Runtime.InteropServices.OSPlatform", "get_Linux")) continue;
                if (!IsCall(il[i + 1], "System.Runtime.InteropServices.RuntimeInformation", "IsOSPlatform")) continue;
                Console.WriteLine($"{type.Name}.{method.Name}: IL_{il[i].Offset:X4} {il[i]} / {il[i + 1]}");
                if (dry) continue;
                ILProcessor p = method.Body.GetILProcessor();
                Instruction check = il[i + 1];
                p.Replace(check, Instruction.Create(OpCodes.Pop));
                p.InsertAfter(il[i + 1], Instruction.Create(OpCodes.Ldc_I4_1));
                patched++;
            }
        }

        if (dry) return 0;
        if (patched == 0)
        {
            Console.WriteLine("Nothing to patch: the Linux check is already replaced.");
            return 0;
        }

        string backup = Path.Combine(AppContext.BaseDirectory, "..", "original", Path.GetFileName(path));
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        if (!File.Exists(backup)) File.Copy(path, backup);
        using (var output = new MemoryStream())
        {
            module.Write(output);
            File.WriteAllBytes(path, output.ToArray());
        }
        Console.WriteLine($"Patched {patched} check(s) in {path}. The original is in {Path.GetFullPath(backup)}.");
        return 0;
    }

    private static bool IsCall(Instruction instruction, string type, string name) =>
        (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
        instruction.Operand is MethodReference m && m.DeclaringType.FullName == type && m.Name == name;
}
