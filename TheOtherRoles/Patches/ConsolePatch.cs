using System.Linq;
using HarmonyLib;
using TheOtherRoles.Objects;

namespace TheOtherRoles.Patches;

[HarmonyPatch(typeof(Console), nameof(Console.Use))]
internal class ConsoleUsePatch
{
    public static void Prefix(Console __instance)
    {
        if (CustomOptionHolder.airshipReplaceSafeTask.getBool())
        {
            PlayerTask playerTask = __instance.FindTask(PlayerControl.LocalPlayer);
            Minigame alignTelescopeMinigame = MapData.PolusShip.ShortTasks
                .FirstOrDefault(x => x.name == "AlignTelescope").MinigamePrefab;
            if (playerTask.MinigamePrefab.name == "SafeGame") playerTask.MinigamePrefab = alignTelescopeMinigame;
        }
    }
}
