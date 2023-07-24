using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using System.IO;

namespace CriBindDir;

[BepInPlugin("SutandoTsukai181.CriBindDir", "CriBindDir", "1.0.0")]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    internal static AssetBundleManager ABManager;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo("CriBindDir loaded!");

        if (!Directory.Exists(Path.Combine(CriWare.Common.streamingAssetsPath, "BindDirectory")))
        {
            Log.LogInfo("Creating BindDirectory as it did not exist");
            Directory.CreateDirectory(Path.Combine(CriWare.Common.streamingAssetsPath, "BindDirectory"));
        }

        Harmony h = new Harmony("CriBindDirHarmony");
        h.PatchAll();
    }
}

[HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.Awake))]
internal class ABManagerAwake
{
    static bool Prefix(AssetBundleManager __instance)
    {
        Plugin.Log.LogDebug($"Hooked AssetBundleManager.Awake");
        Plugin.Log.LogDebug($"ABManager: {__instance.ToString()}, pointer: {__instance.Pointer}");
        Plugin.ABManager = __instance;

        return true;
    }
}

[HarmonyPatch(typeof(CriFsUtility), nameof(CriFsUtility.BindCpk))]
[HarmonyPatch(new Type[] { typeof(CriFsBinder), typeof(string) })]
internal class CriFsUtilityBindCpk
{
    static bool Prefix(CriFsBinder targetBinder, string srcPath)
    {
        Plugin.Log.LogDebug($"Hooked BindCpk");
        Plugin.Log.LogDebug($"targetBinder: {targetBinder.ToString()}, handle: {targetBinder.handle}, pointer: {targetBinder.Pointer}");
        Plugin.Log.LogDebug($"srcPath: {srcPath}");

        if (Plugin.ABManager == null)
        {
            Plugin.Log.LogError($"ABManager instance not found! Directory binding skipped");
            return true;
        }

        Plugin.Log.LogDebug($"ABManager: {Plugin.ABManager.ToString()}, pointer: {Plugin.ABManager.Pointer}");
        
        Plugin.Log.LogInfo($"Binding directory for {srcPath.Substring(1 + srcPath.LastIndexOf(System.IO.Path.DirectorySeparatorChar))}");
        var bindRequest = CriFsUtility.BindDirectory(targetBinder, System.IO.Path.Combine(CriWare.Common.streamingAssetsPath, "BindDirectory"));

        bindRequest.WaitForDone(Plugin.ABManager);
        Plugin.Log.LogInfo($"Done! BindId {bindRequest.bindId} is bound to directory \"{bindRequest.path}\"\n");
        CriFsBinder.SetPriority(bindRequest.bindId, 10000);

        return true;
    }
}
