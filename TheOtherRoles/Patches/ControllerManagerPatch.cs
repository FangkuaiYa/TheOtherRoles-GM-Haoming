using System;
using System.Diagnostics;
using System.IO;
using HarmonyLib;
using TheOtherRoles;
using UnityEngine;

namespace TownOfHost;

[HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
internal class ControllerManagerUpdatePatch
{
    private static readonly (int, int)[] resolutions = { (480, 270), (640, 360), (800, 450), (1280, 720), (1600, 900) };
    private static int resolutionIndex;

    public static void Postfix(ControllerManager __instance)
    {
        //解像度変更
        if (Input.GetKeyDown(KeyCode.F11))
        {
            resolutionIndex++;
            if (resolutionIndex >= resolutions.Length) resolutionIndex = 0;
            ResolutionManager.SetResolution(resolutions[resolutionIndex].Item1, resolutions[resolutionIndex].Item2,
                false);
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F1))
        {
            string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string filename =
                $"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TheOtherRoles_GM_H-v{TheOtherRolesPlugin.VersionString}-{t}.log";
            FileInfo file = new(@$"{Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            file.CopyTo(filename);
            Process.Start(@$"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
        }
    }
}
