using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Version = SemanticVersioning.Version;


namespace TheOtherRoles.Patches;

[HarmonyPatch]
public class SubmergedPatch
{
    public static Type SubmarineElevatorType;
    public static Type FloorHandlerType;
    public static Type SubmarineSpawnInSystemType;
    public static Type SubmarinePlayerFloorSystemType;
    public static Type SpawnInStateType;
    public static MethodInfo GetFloorHandlerMethod;
    public static MethodInfo RpcRequestChangeFloorMethod;

    public static void Patch()
    {
        bool loaded =
            IL2CPPChainloader.Instance.Plugins.TryGetValue(SubmergedCompatibility.SUBMERGED_GUID,
                out PluginInfo pluginInfo);
        if (!loaded) return;
        BasePlugin plugin = pluginInfo!.Instance as BasePlugin;
        Version version = pluginInfo.Metadata.Version;
        Assembly assembly = plugin!.GetType().Assembly;
        Type[] types = AccessTools.GetTypesFromAssembly(assembly);
        SubmarineElevatorType = types.First(t => t.Name == "SubmarineElevator");
        SubmarinePlayerFloorSystemType = types.First(t => t.Name == "SubmarinePlayerFloorSystem");
        SpawnInStateType = types.First(t => t.Name == "SpawnInState");
        FloorHandlerType = types.First(t => t.Name == "FloorHandler");
        GetFloorHandlerMethod =
            AccessTools.Method(FloorHandlerType, "GetFloorHandler", new[] { typeof(PlayerControl) });
        RpcRequestChangeFloorMethod = AccessTools.Method(FloorHandlerType, "RpcRequestChangeFloor");

        // OnDestroyパッチ
        Type SubmarineSelectSpawnType = types.First(t => t.Name == "SubmarineSelectSpawn");
        MethodInfo SubmarineSelectSpawnOnDestroyOriginal = AccessTools.Method(SubmarineSelectSpawnType, "OnDestroy");
        MethodInfo SubmarineSelectSpawnOnDestroyPostfix =
            SymbolExtensions.GetMethodInfo(() => SubmarineSelectSpawnOnDestroyPatch.Postfix());
        MethodInfo SubmarineSelectSpawnOnDestroyPrefix =
            SymbolExtensions.GetMethodInfo(() => SubmarineSelectSpawnOnDestroyPatch.Prefix());

        // GetTotalPlayerAmountパッチ
        int aInt = 0;
        SubmarineSpawnInSystemType = types.First(t => t.Name == "SubmarineSpawnInSystem");
        MethodInfo GetTotalPlayerAmountOriginal =
            AccessTools.Method(SubmarineSpawnInSystemType, "GetTotalPlayerAmount");
        MethodInfo GetTotalPlayerAmountPostfix =
            SymbolExtensions.GetMethodInfo(() => SubmarineSpawnInSystemGetTotalPlayerAmountPatch.Postfix());
        MethodInfo GetTotalPlayerAmountPrefix =
            SymbolExtensions.GetMethodInfo(() => SubmarineSpawnInSystemGetTotalPlayerAmountPatch.Prefix(ref aInt));
        // Detoriorateパッチ
        object aObject = null;
        float aFloat = 0f;
        MethodInfo DetoriorateOriginal = AccessTools.Method(SubmarineSpawnInSystemType, "Detoriorate");
        MethodInfo DetorioratePostfix =
            SymbolExtensions.GetMethodInfo(() => SubmarineSpawnInSystemDetorioratePatch.Postfix());
        MethodInfo DetorioratePrefix =
            SymbolExtensions.GetMethodInfo(() => SubmarineSpawnInSystemDetorioratePatch.Prefix(aObject, aFloat));


        // パッチ適応
        Harmony harmony = new("Submerged");
        harmony.Patch(SubmarineSelectSpawnOnDestroyOriginal, new HarmonyMethod(SubmarineSelectSpawnOnDestroyPrefix),
            new HarmonyMethod(SubmarineSelectSpawnOnDestroyPostfix));
        harmony.Patch(GetTotalPlayerAmountOriginal, new HarmonyMethod(GetTotalPlayerAmountPrefix),
            new HarmonyMethod(GetTotalPlayerAmountPostfix));
        harmony.Patch(DetoriorateOriginal, new HarmonyMethod(DetorioratePrefix), new HarmonyMethod(DetorioratePostfix));
    }

    public static void ChangePlayerFloorState(byte playerId, bool toUpper)
    {
        PropertyInfo[] SubMarinePlayerFloorSystemProperties =
            SubmarinePlayerFloorSystemType.GetProperties(BindingFlags.Static | BindingFlags.Public);
        object Instance = SubMarinePlayerFloorSystemProperties.First(f => f.Name == "Instance").GetValue(null);
        MethodInfo ChangePlayerFloorStateMethod = SubmarinePlayerFloorSystemType.GetMethod("ChangePlayerFloorState");
        ChangePlayerFloorStateMethod.Invoke(Instance, new object[] { playerId, toUpper });
    }

    public class SubmarineSelectSpawnOnDestroyPatch
    {
        public static void Prefix()
        {
        }

        public static void Postfix()
        {
            PlayerControl.LocalPlayer.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
            MapUtilities.CachedShipStatus.EmergencyCooldown = GameManager.Instance.LogicOptions.GetEmergencyCooldown();
            ExileControllerReEnableGameplayPatch.ReEnableGameplay();
        }
    }

    public class SubmarineSpawnInSystemGetTotalPlayerAmountPatch
    {
        public static bool Prefix(ref int __result)
        {
            __result = GameData.Instance.AllPlayers.ToSystemList().Count(delegate(NetworkedPlayerInfo p)
            {
                if (p != null && !p.IsDead && !p.Disconnected && Helpers.playerById(p.PlayerId) != Puppeteer.dummy)
                {
                    PlayerControl @object = p.Object;
                    if (@object != null) return !@object.isDummy;
                }

                return false;
            });
            return false;
        }

        public static void Postfix()
        {
        }
    }

    public class SubmarineSpawnInSystemDetorioratePatch
    {
        public static void Postfix()
        {
        }

        public static bool Prefix(object __instance, float deltaTime)
        {
            MethodInfo GetTotalPlayerAmount = AccessTools.Method(SubmarineSpawnInSystemType, "GetTotalPlayerAmount");
            int totalPlayerAmount = (GetTotalPlayerAmount.Invoke(__instance, new object[0]) as int?).Value;
            MethodInfo GetReadyPlayerAmount = AccessTools.Method(SubmarineSpawnInSystemType, "GetReadyPlayerAmount");
            int ReadyPlayerAmount = (GetReadyPlayerAmount.Invoke(__instance, new object[0]) as int?).Value;
            FieldInfo[] SubmarineSpawnInSystemFields = SubmarineSpawnInSystemType.GetFields(BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo CurrentState = SubmarineSpawnInSystemFields.First(f => f.Name == "CurrentState");
            CurrentState = SubmarineSpawnInSystemType.GetField("CurrentState");
            object currentState = CurrentState.GetValue(__instance);
            Type enumUnderlyingType = Enum.GetUnderlyingType(SpawnInStateType);
            object state = Convert.ChangeType(currentState, enumUnderlyingType);

            FieldInfo Timer = SubmarineSpawnInSystemFields.First(f => f.Name == "Timer");
            if ((byte)state == 1)
            {
                float timer = MathF.Max(0f, (Timer.GetValue(__instance) as float?).Value - deltaTime);
                Timer.SetValue(__instance, timer);
            }

            if (totalPlayerAmount == ReadyPlayerAmount)
            {
                FieldInfo Players = SubmarineSpawnInSystemFields.First(f => f.Name == "Players");
                PropertyInfo[] SubmarineSpawnInSystemProperties = SubmarineSpawnInSystemType.GetProperties(
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                PropertyInfo IsDirty = SubmarineSpawnInSystemProperties.First(f => f.Name == "IsDirty");
                CurrentState.SetValueDirect(__makeref(__instance), (byte)state + 1);
                //CurrentState.SetValue(__instance, Done);
                Players.SetValue(__instance, new HashSet<byte>());
                Timer.SetValue(__instance, 10f);
                IsDirty.SetValue(__instance, true);
            }

            return false;
        }
    }
}
