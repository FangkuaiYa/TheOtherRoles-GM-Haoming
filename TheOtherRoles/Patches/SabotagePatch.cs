using HarmonyLib;

namespace TheOtherRoles.Patches;

[HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.UpdateSystem))]
internal class HeliSabotageSystemRepairDamagePatch
{
    private static void Postfix(HeliSabotageSystem __instance, PlayerControl player/*, byte amount*/)
    {
        HeliSabotageSystem.Tags tags = (HeliSabotageSystem.Tags)(/*amount & */240);
        if (tags != HeliSabotageSystem.Tags.ActiveBit)
            if (tags == HeliSabotageSystem.Tags.DamageBit)
                __instance.Countdown = CustomOptionHolder.airshipReactorDuration.getFloat();
    }
}
