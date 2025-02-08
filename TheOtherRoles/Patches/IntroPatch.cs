using System;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime;
using Il2CppSystem.Collections;
using Il2CppSystem.Collections.Generic;
using TheOtherRoles.Objects;
using TMPro;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.TheOtherRolesGM;
using Object = UnityEngine.Object;


namespace TheOtherRoles.Patches;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
internal class IntroCutsceneOnDestroyPatch
{
    public static PoolablePlayer playerPrefab;
    public static Vector3 bottomLeft;
    public static void Prefix(IntroCutscene __instance)
    {
        int playerCounter = 0;
        // Generate and initialize player icons
        if (PlayerControl.LocalPlayer != null && FastDestroyableSingleton<HudManager>.Instance != null)
        {
            float aspect = Camera.main.aspect;
            float safeOrthographicSize = CameraSafeArea.GetSafeOrthographicSize(Camera.main);
            float xpos = 1.75f - safeOrthographicSize * aspect * 1.70f;
            float ypos = 0.15f - safeOrthographicSize * 1.7f;
            bottomLeft = new Vector3(xpos / 2, ypos/2, -61f);

            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                NetworkedPlayerInfo data = p.Data;
                PoolablePlayer player = UnityEngine.Object.Instantiate<PoolablePlayer>(__instance.PlayerPrefab, FastDestroyableSingleton<HudManager>.Instance.transform);
                playerPrefab = __instance.PlayerPrefab;
                p.SetPlayerMaterialColors(player.cosmetics.currentBodySprite.BodySprite);
                player.SetSkin(data.DefaultOutfit.SkinId, data.DefaultOutfit.ColorId);
                player.cosmetics.SetHat(data.DefaultOutfit.HatId, data.DefaultOutfit.ColorId);
                // PlayerControl.SetPetImage(data.DefaultOutfit.PetId, data.DefaultOutfit.ColorId, player.PetSlot);
                player.cosmetics.nameText.text = data.PlayerName;
                player.SetFlipX(true);
                TORMapOptions.playerIcons[p.PlayerId] = player;

                player.gameObject.SetActive(false);

                if (PlayerControl.LocalPlayer == Arsonist.arsonist && p != Arsonist.arsonist)
                {
                    player.transform.localPosition = bottomLeft + new Vector3(-0.25f, -0.25f, 0) + Vector3.right * playerCounter++ * 0.35f;
                    player.transform.localScale = Vector3.one * 0.2f;
                    player.setSemiTransparent(true);
                    player.gameObject.SetActive(true);
                }
                else
                {   //  This can be done for all players not just for the bounty hunter as it was before. Allows the thief to have the correct position and scaling
                    player.transform.localPosition = bottomLeft;
                    player.transform.localScale = Vector3.one * 0.4f;
                    player.gameObject.SetActive(false);
                }
            }
        }

        // Force Bounty Hunter to load a new Bounty when the Intro is over
        if (BountyHunter.bounty != null && PlayerControl.LocalPlayer == BountyHunter.bountyHunter)
        {
            BountyHunter.bountyUpdateTimer = 0f;
            if (FastDestroyableSingleton<HudManager>.Instance != null)
            {
                BountyHunter.cooldownText =
                    Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.KillButton.cooldownTimerText,
                        FastDestroyableSingleton<HudManager>.Instance.transform);
                BountyHunter.cooldownText.alignment = TextAlignmentOptions.Center;
                BountyHunter.cooldownText.transform.localPosition = bottomLeft + new Vector3(0f, -0.35f, -62f);
                BountyHunter.cooldownText.transform.localScale = Vector3.one * 0.4f;
                BountyHunter.cooldownText.gameObject.SetActive(true);
            }
        }

        Arsonist.updateIcons();
        Morphling.resetMorph();
        Camouflager.resetCamouflage();

        if (PlayerControl.LocalPlayer == GM.gm && !GM.hasTasks)
            PlayerControl.LocalPlayer.clearAllTasks();

        if (PlayerControl.LocalPlayer.isGM())
        {
            FastDestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);
            FastDestroyableSingleton<HudManager>.Instance.ReportButton.gameObject.SetActiveRecursively(false);
            FastDestroyableSingleton<HudManager>.Instance.ReportButton.SetActive(false);
            FastDestroyableSingleton<HudManager>.Instance.ReportButton.graphic.enabled = false;
            FastDestroyableSingleton<HudManager>.Instance.ReportButton.enabled = false;
            FastDestroyableSingleton<HudManager>.Instance.ReportButton.graphic.sprite = null;
            FastDestroyableSingleton<HudManager>.Instance.ReportButton.buttonLabelText.enabled = false;
            FastDestroyableSingleton<HudManager>.Instance.ReportButton.buttonLabelText.SetText("");

            FastDestroyableSingleton<HudManager>.Instance.roomTracker.gameObject.SetActiveRecursively(false);
            FastDestroyableSingleton<HudManager>.Instance.roomTracker.text.enabled = false;
            FastDestroyableSingleton<HudManager>.Instance.roomTracker.text.SetText("");
            FastDestroyableSingleton<HudManager>.Instance.roomTracker.enabled = false;
        }

        // インポスター視界の場合に昇降機右の影を無効化
        if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4 && CustomOptionHolder.airshipOptimizeMap.getBool() &&
            Helpers.hasImpostorVision(PlayerControl.LocalPlayer))
        {
            GameObject obj = ShipStatus.Instance.FastRooms[SystemTypes.GapRoom].gameObject;
            OneWayShadows oneWayShadow = obj.transform.FindChild("Shadow").FindChild("LedgeShadow")
                .GetComponent<OneWayShadows>();
            oneWayShadow.gameObject.SetActive(false);
        }

        // ベントを追加する
        AdditionalVents.AddAdditionalVents();

        // スペシメンにバイタルを移動する
        SpecimenVital.moveVital();

        // アーカイブのアドミンを消す
        if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4 && CustomOptionHolder.airshipOldAdmin.getBool())
        {
            GameObject records = ShipStatus.Instance.FastRooms[SystemTypes.Records].gameObject;
            records.GetComponentsInChildren<MapConsole>().Where(x => x.name == "records_admin_map").FirstOrDefault()
                ?.gameObject.SetActive(false);
        }

        if (ShipStatus.Instance.FastRooms.ContainsKey(SystemTypes.GapRoom))
        {
            GameObject gapRoom = ShipStatus.Instance.FastRooms[SystemTypes.GapRoom].gameObject;
            // GapRoomの配電盤を消す
            if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4 && CustomOptionHolder.airshipDisableGapSwitchBoard.getBool())
            {
                GameObject sabo = gapRoom.GetComponentsInChildren<Console>()
                    .Where(x => x.name == "task_lightssabotage (gap)").FirstOrDefault()?.gameObject;
                sabo.SetActive(false);
                MapUtilities.CachedShipStatus.AllConsoles = MapUtilities.CachedShipStatus.AllConsoles
                    .Where(x => x != sabo.GetComponent<Console>()).ToArray();
            }

            // ぬ～んを消す
            if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4 && CustomOptionHolder.airshipDisableMovingPlatform.getBool())
            {
                gapRoom.GetComponentInChildren<MovingPlatformBehaviour>().gameObject.SetActive(false);
                gapRoom.GetComponentsInChildren<PlatformConsole>().ForEach(x => x.gameObject.SetActive(false));
            }
        }

        //タスクバグ修正
        if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4 && CustomOptionHolder.airshipEnableWallCheck.getBool())
        {
            System.Collections.Generic.List<Console> objects = GameObject.FindObjectsOfType<Console>().ToList();
            objects.Find(x => x.name == "task_garbage1").checkWalls = true;
            objects.Find(x => x.name == "task_garbage2").checkWalls = true;
            objects.Find(x => x.name == "task_garbage3").checkWalls = true;
            objects.Find(x => x.name == "task_garbage4").checkWalls = true;
            objects.Find(x => x.name == "task_garbage5").checkWalls = true;
            objects.Find(x => x.name == "task_shower").checkWalls = true;
            objects.Find(x => x.name == "task_developphotos").checkWalls = true;
            objects.Find(x => x.name == "DivertRecieve" && x.Room == SystemTypes.Armory).checkWalls = true;
            objects.Find(x => x.name == "DivertRecieve" && x.Room == SystemTypes.MainHall).checkWalls = true;
        }

        // 最初から一人の場合はLast Impostorになる
        if (AmongUsClient.Instance.AmHost) LastImpostor.promoteToLastImpostor();

        // タスクパネルの表示優先度を上げる
        GameObject taskPanel = FastDestroyableSingleton<HudManager>.Instance.TaskStuff;
        Vector3 pos = taskPanel.transform.position;
        taskPanel.transform.position = new Vector3(pos.x, pos.y, -20);

        // ダミー人形をスポーンさせておく
        if (PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer) && SubmergedCompatibility.isSubmerged())
        {
            byte playerId = (byte)GameData.Instance.GetAvailableId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SpawnDummy, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(PlayerControl.LocalPlayer.transform.position.x);
            writer.Write(PlayerControl.LocalPlayer.transform.position.y);
            writer.Write(PlayerControl.LocalPlayer.transform.position.z);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.spawnDummy(playerId, PlayerControl.LocalPlayer.transform.position);
        }

        HudManager.Instance.ShowVanillaKeyGuide();

        // Cornucopiaのバナーを表示する
        Cornucopia.showBanner();

        // マップデータのコピーを読み込み
        if (CustomOptionHolder.airshipReplaceSafeTask.getBool()) MapData.LoadAssets(AmongUsClient.Instance);

        // お参りタスク
        if (CustomOptionHolder.foxSpawnRate.getSelection() > 0)
        {
            Shrine.activateShrines(GameOptionsManager.Instance.currentNormalGameOptions.MapId);
            System.Collections.Generic.List<byte> taskIdList = new();
            Shrine.allShrine.ForEach(shrine => taskIdList.Add((byte)shrine.console.ConsoleId));
            taskIdList.Shuffle();
            CustomNormalPlayerTask cpt = new("foxTaskStay", Il2CppType.Of<FoxTask>(), Fox.numTasks,
                taskIdList.ToArray(),
                Shrine.allShrine.Find(x => x.console.ConsoleId == taskIdList.ToArray()[0]).console.Room, true);
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                if (p.isRole(RoleType.Fox))
                {
                    p.clearAllTasks();
                    cpt.addTaskToPlayer(p.PlayerId);
                }
        }

        // シュレディンガーの猫変身メニュー用テンプレート
        SchrodingersCat.playerTemplate = Object.Instantiate(__instance.PlayerPrefab,
            FastDestroyableSingleton<HudManager>.Instance.transform);
        SchrodingersCat.playerTemplate.UpdateFromPlayerOutfit(PlayerControl.LocalPlayer.Data.DefaultOutfit,
            PlayerMaterial.MaskType.ComplexUI, false, true);
        SchrodingersCat.playerTemplate.SetFlipX(true);
        SchrodingersCat.playerTemplate.gameObject.SetActive(false);
        SchrodingersCat.playerTemplate.cosmetics.currentPet?.gameObject.SetActive(false);
        SchrodingersCat.playerTemplate.cosmetics.nameText.text = "";
        SchrodingersCat.playerTemplate.gameObject.SetActive(false);
    }
}

[HarmonyPatch]
internal class IntroPatch
{
    public static void setupIntroTeamIcons(IntroCutscene __instance, ref List<PlayerControl> yourTeam)
    {
        // Intro solo teams
        if (PlayerControl.LocalPlayer.isNeutral() || PlayerControl.LocalPlayer == GM.gm)
            if (!(PlayerControl.LocalPlayer.isRole(RoleType.SchrodingersCat) && SchrodingersCat.hideRole))
            {
                List<PlayerControl> soloTeam = new();
                soloTeam.Add(PlayerControl.LocalPlayer);
                yourTeam = soloTeam;
            }

        // Don't show the GM
        if (!PlayerControl.LocalPlayer.isGM())
        {
            List<PlayerControl> newTeam = new();
            foreach (PlayerControl p in yourTeam)
                if (p != GM.gm)
                    newTeam.Add(p);
            yourTeam = newTeam;
        }

        // Add the Spy to the Impostor team (for the Impostors)
        if (Spy.spy != null && PlayerControl.LocalPlayer.Data.Role.IsImpostor)
        {
            System.Collections.Generic.List<PlayerControl> players = PlayerControl.AllPlayerControls.GetFastEnumerator()
                .ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            List<PlayerControl>
                fakeImpostorTeam =
                    new(); // The local player always has to be the first one in the list (to be displayed in the center)
            fakeImpostorTeam.Add(PlayerControl.LocalPlayer);
            foreach (PlayerControl p in players)
                if (PlayerControl.LocalPlayer != p && (p == Spy.spy || p.Data.Role.IsImpostor))
                    fakeImpostorTeam.Add(p);
            yourTeam = fakeImpostorTeam;
        }
    }

    public static void setupIntroTeam(IntroCutscene __instance, ref List<PlayerControl> yourTeam)
    {
        System.Collections.Generic.List<RoleInfo> infos =
            RoleInfo.getRoleInfoForPlayer(PlayerControl.LocalPlayer);
        RoleInfo roleInfo = infos.Where(info => info.roleType != RoleType.Lovers).FirstOrDefault();
        if (roleInfo == null) return;
        if (PlayerControl.LocalPlayer.isNeutral() || PlayerControl.LocalPlayer.isGM())
            if (!(PlayerControl.LocalPlayer.isRole(RoleType.SchrodingersCat) && SchrodingersCat.hideRole))
            {
                __instance.BackgroundBar.material.color = roleInfo.color;
                __instance.TeamTitle.text = roleInfo.name;
                __instance.TeamTitle.color = roleInfo.color;
                __instance.ImpostorText.text = "";
            }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    private class IntroCutsceneCoBeginPatch
    {
        private static bool Prefix(IntroCutscene __instance, ref IEnumerator __result)
        {
            if (RoleAssignmentPatch.isAssigned) return true;
            __result = CoBegin(__instance).WrapToIl2Cpp();
            return false;
        }

        private static System.Collections.IEnumerator CoBegin(IntroCutscene __instance)
        {
            yield return waitRoleAssign().WrapToIl2Cpp();
            yield return __instance.CoBegin();
            yield break;
        }

        private static System.Collections.IEnumerator waitRoleAssign()
        {
            while (!RoleAssignmentPatch.isAssigned) yield return null;
            yield break;
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
    private class SetUpRoleTextPatch
    {
        public static bool Prefix(IntroCutscene __instance, ref IEnumerator __result)
        {
            __result = setupRole(__instance).WrapToIl2Cpp();
            return false;
        }

        private static System.Collections.IEnumerator setupRole(IntroCutscene __instance)
        {
            System.Collections.Generic.List<RoleInfo> infos =
                RoleInfo.getRoleInfoForPlayer(PlayerControl.LocalPlayer, new[] { RoleType.Lovers });
            RoleInfo roleInfo = infos.FirstOrDefault();
            if (roleInfo == RoleInfo.fortuneTeller && FortuneTeller.numTasks > 0) roleInfo = RoleInfo.crewmate;

#if DEBUG
            LogHelper.Info("----------Role Assign-----------", "Settings");
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
                LogHelper.Info(
                    string.Format("{0,-3}{1,-2}:{2}:{3}", pc.AmOwner ? "[*]" : "", pc.PlayerId,
                        pc.Data.PlayerName.PadRightV2(20), RoleInfo.GetRolesString(pc, false, joinSeparator: " + ")),
                    "Settings");
            LogHelper.Info("-----------Platforms------------", "Settings");
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
                LogHelper.Info(
                    string.Format("{0,-3}{1,-2}:{2}:{3}", pc.AmOwner ? "[*]" : "", pc.PlayerId,
                        pc.Data.PlayerName.PadRightV2(20), pc.getPlatform().Replace("Standalone", "")), "Settings");
            LogHelper.Info("---------Game Settings----------", "Settings");
            TheOtherRolesPlugin.optionsPage = 0;
            string[] tmp = GameOptionsManager.Instance.currentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10)
                .Split("\r\n");
            foreach (string t in tmp[1..(tmp.Length - 2)])
                LogHelper.Info(t, "Settings");
            LogHelper.Info("--------Advance Settings--------", "Settings");
            foreach (CustomOption o in CustomOption.options)
                if (o.parent == null ? !o.getString().Equals("0%") : o.parent.enabled)
                    LogHelper.Info(
                        string.Format("{0}:{1}",
                            o.parent == null
                                ? o.name.removeHtml().PadRightV2(43)
                                : $"┗ {o.name.removeHtml().PadRightV2(41)}", o.getString().removeHtml()), "Settings");
            LogHelper.Info("--------------------------------", "Settings");
#endif

            __instance.YouAreText.color = roleInfo.color;
            __instance.RoleText.text = roleInfo.name;
            __instance.RoleText.color = roleInfo.color;
            __instance.RoleBlurbText.text = roleInfo.introDescription;
            __instance.RoleBlurbText.color = roleInfo.color;

            //bool isSiuneInGame =
            //    PlayerControl.AllPlayerControls.ToArray().Count(player => player.Data.PlayerName == "卯ノ花しうね") != 0;
            //if (roleInfo == RoleInfo.crewmate && isSiuneInGame && rnd.Next(1, 101) > 90)
            //{
            //    __instance.RoleText.text = "素村";
            //    __instance.RoleBlurbText.text = "大当たりー";
            //}

            if (PlayerControl.LocalPlayer.hasModifier(ModifierType.Madmate))
            {
                if (roleInfo == RoleInfo.crewmate)
                    __instance.RoleText.text = ModTranslation.getString("madmate");
                else
                    __instance.RoleText.text = ModTranslation.getString("madmatePrefix") + __instance.RoleText.text;
                __instance.YouAreText.color = Madmate.color;
                __instance.RoleText.color = Madmate.color;
                __instance.RoleBlurbText.text = ModTranslation.getString("madmateIntroDesc");
                __instance.RoleBlurbText.color = Madmate.color;
            }

            if (infos.Any(info => info.roleType == RoleType.Lovers))
            {
                PlayerControl otherLover = PlayerControl.LocalPlayer.getPartner();
                __instance.RoleBlurbText.text += "\n" + Helpers.cs(Lovers.color,
                    string.Format(ModTranslation.getString("loversFlavor"), otherLover?.Data?.PlayerName ?? ""));
            }

            // 従来処理
            SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.Data.Role.IntroSound, false);
            __instance.YouAreText.gameObject.SetActive(true);
            __instance.RoleText.gameObject.SetActive(true);
            __instance.RoleBlurbText.gameObject.SetActive(true);

            if (__instance.ourCrewmate == null)
            {
                __instance.ourCrewmate =
                    __instance.CreatePlayer(0, 1, PlayerControl.LocalPlayer.Data, false);
                __instance.ourCrewmate.gameObject.SetActive(false);
            }

            __instance.ourCrewmate.gameObject.SetActive(true);
            __instance.ourCrewmate.transform.localPosition = new Vector3(0f, -1.05f, -18f);
            __instance.ourCrewmate.transform.localScale = new Vector3(1f, 1f, 1f);
            __instance.ourCrewmate.ToggleName(false);
            yield return new WaitForSeconds(2.5f);
            __instance.YouAreText.gameObject.SetActive(false);
            __instance.RoleText.gameObject.SetActive(false);
            __instance.RoleBlurbText.gameObject.SetActive(false);
            __instance.ourCrewmate.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    private class BeginCrewmatePatch
    {
        public static void Prefix(IntroCutscene __instance, ref List<PlayerControl> teamToDisplay)
        {
            setupIntroTeamIcons(__instance, ref teamToDisplay);
        }

        public static void Postfix(IntroCutscene __instance, ref List<PlayerControl> teamToDisplay)
        {
            setupIntroTeam(__instance, ref teamToDisplay);
        }
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    private class BeginImpostorPatch
    {
        public static void Prefix(IntroCutscene __instance, ref List<PlayerControl> yourTeam)
        {
            setupIntroTeamIcons(__instance, ref yourTeam);
        }

        public static void Postfix(IntroCutscene __instance, ref List<PlayerControl> yourTeam)
        {
            setupIntroTeam(__instance, ref yourTeam);
        }
    }
}
