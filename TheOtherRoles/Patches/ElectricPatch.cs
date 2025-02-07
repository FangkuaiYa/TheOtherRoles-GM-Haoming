using HarmonyLib;
using UnityEngine;

namespace TheOtherRoles.Patches;

[Harmony]
public class ElectricPatch
{
    public static bool isOntask()
    {
        return Camera.main.gameObject.GetComponentInChildren<SwitchMinigame>() != null;
    }
}
