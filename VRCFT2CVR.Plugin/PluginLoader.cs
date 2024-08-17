using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MelonLoader;
using VRCFT2CVR.Plugin;

[assembly: MelonInfo(typeof(PluginLoader), "VRCFT2CVR.Plugin", "1.2.0", "200Tigersbloxed")]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonColor(255, 144, 242, 35)]
[assembly: MelonAuthorColor(255, 252, 100, 0)]

namespace VRCFT2CVR.Plugin;

public class PluginLoader : MelonPlugin
{
    private const string BEGIN_TOOLNAMESPACE = "VRCFT2CVR.Plugin";
    private const string MOD_RESOURCE = BEGIN_TOOLNAMESPACE + ".VRCFT2CVR.dll";

    private static readonly Dictionary<string, Assembly> LoadedAssemblies = new();
    private static readonly IReadOnlyList<string> NativeAssemblies = new[]
    {
        BEGIN_TOOLNAMESPACE + ".fti_osc.dll"
    };
    
    private Assembly? CurrentAssembly;
    
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);
    
    public static string CalculateMD5(byte[] data)
    {
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(data);
        StringBuilder sb = new StringBuilder();
        foreach (var t in hash)
            sb.Append(t.ToString("x2"));
        return sb.ToString();
    }

    private byte[]? GetDllData(Assembly assembly, string dll)
    {
        using Stream? stream = assembly.GetManifestResourceStream(dll);
        if (stream == null)
        {
            LoggerInstance.Error("Cannot find dll from reference " + dll);
            return null;
        }
        using MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private void HandleManaged(string dll, byte[] data)
    {
        Assembly a = AppDomain.CurrentDomain.Load(data);
        LoadedAssemblies.Add(dll, a);
    }

    private void HandleNative(string dll, byte[] data)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory , "UserData", "VRCFTData");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        string fileName = dll.Remove(0, BEGIN_TOOLNAMESPACE.Length + 1);
        string file = Path.Combine(path, fileName);
        if (File.Exists(file))
        {
            string fileMD5 = CalculateMD5(data);
            string compareMD5 = CalculateMD5(File.ReadAllBytes(file));
            if(fileMD5 == compareMD5)
            {
                LoadLibrary(file);
                return;
            }
        }
        File.WriteAllBytes(file, data);
        LoadLibrary(file);
    }
    
    public override void OnPreInitialization()
    {
        CurrentAssembly = Assembly.GetExecutingAssembly();
        string[] dlls = CurrentAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll") && x != MOD_RESOURCE)
            .ToArray();
        foreach (string dll in dlls)
        {
            byte[]? data = GetDllData(CurrentAssembly, dll);
            if(data == null) continue;
            if (NativeAssemblies.Contains(dll))
            {
                HandleNative(dll, data);
                continue;
            }
            HandleManaged(dll, data);
        }
        LoggerInstance.Msg("Loaded all dependencies!");
        MelonEvents.OnPreModsLoaded.Subscribe(() =>
        {
            byte[]? modData = GetDllData(CurrentAssembly, MOD_RESOURCE);
            if(modData == null) return;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory , "Mods", "VRCFT2CVR.dll");
            MelonAssembly melonAssembly = MelonAssembly.LoadRawMelonAssembly(path, modData);
            var melons = new List<MelonMod>();
            melonAssembly.LoadMelons();
            foreach (MelonBase melonBase in melonAssembly.LoadedMelons)
            {
                if (melonBase is MelonMod t)
                {
                    melons.Add(t);
                }
            }
            RegisterSorted(melons);
            LoggerInstance.Msg("Loaded mod!");
        });
        // This is in case something else complains i.e. GetAllTypes
        AppDomain.CurrentDomain.AssemblyResolve += (_, resolve) =>
        {
            // Attempt to return a cached value
            string assemblyName = resolve.Name.Split(',')[0];
            string namespaceFile = BEGIN_TOOLNAMESPACE + "." + assemblyName + ".dll";
            if (LoadedAssemblies.TryGetValue(namespaceFile, out Assembly a)) return a;
            // Find the searched one and cache it
            foreach (string dll in dlls)
            {
                if(!dll.Contains(namespaceFile)) continue;
                byte[]? data = GetDllData(CurrentAssembly, dll);
                if(data == null) continue;
                Assembly dependency = Assembly.Load(data);
                LoadedAssemblies.Add(namespaceFile, dependency);
                return dependency;
            }
            return null;
        };
    }
}