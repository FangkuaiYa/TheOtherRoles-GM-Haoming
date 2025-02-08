using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Data;
using InnerNet;
using PowerTools;
using TheOtherRoles.Modules;
using TheOtherRoles.Patches;
using UnityEngine;
using static TheOtherRoles.GameHistory;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.TheOtherRolesGM;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace TheOtherRoles;

public enum MurderAttemptResult
{
    PerformKill,
    SuppressKill,
    BlankKill
}

public static class Helpers
{
    public static Dictionary<string, Sprite> CachedSprites = new();

    public static Sprite loadSpriteFromResources(string path, float pixelsPerUnit)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
            Texture2D texture = loadTextureFromResources(path);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            System.Console.WriteLine("Error loading sprite from path: " + path);
        }
        return null;
    }
    public static Sprite loadSpriteFromResources(Texture2D texture, float pixelsPerUnit, Rect textureRect, Vector2 pivot)
    {
        return Sprite.Create(texture, textureRect, pivot, pixelsPerUnit);
    }
    public static unsafe Texture2D loadTextureFromResources(string path)
    {
        try
        {
            Texture2D texture = new(2, 2, TextureFormat.ARGB32, true);
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(path);
            var length = stream.Length;
            var byteTexture = new Il2CppStructArray<byte>(length);
            stream.Read(new Span<byte>(IntPtr.Add(byteTexture.Pointer, IntPtr.Size * 4).ToPointer(), (int)length));
            if (path.Contains("HorseHats"))
            {
                byteTexture = new Il2CppStructArray<byte>(byteTexture.Reverse().ToArray());
            }
            ImageConversion.LoadImage(texture, byteTexture, false);
            return texture;
        }
        catch
        {
            System.Console.WriteLine("Error loading texture from resources: " + path);
        }
        return null;
    }

    public static Texture2D loadTextureFromDisk(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Texture2D texture = new(2, 2, TextureFormat.ARGB32, true);
                var byteTexture = Il2CppSystem.IO.File.ReadAllBytes(path);
                ImageConversion.LoadImage(texture, byteTexture, false);
                return texture;
            }
        }
        catch
        {
            TheOtherRolesPlugin.Logger.LogError("Error loading texture from disk: " + path);
        }
        return null;
    }


    public static bool ShowButtons =>
        !(MapBehaviour.Instance && MapBehaviour.Instance.IsOpen) &&
        !MeetingHud.Instance &&
        !ExileController.Instance;

    public static bool ShowMeetingText =>
        MeetingHud.Instance != null &&
        (MeetingHud.Instance.state == MeetingHud.VoteStates.Voted ||
         MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted ||
         MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion);

    public static bool GameStarted => AmongUsClient.Instance?.GameState == InnerNetClient.GameStates.Started;

    public static bool RolesEnabled => /*CustomOptionHolder.activateRoles.getBool()*/ true;

    public static bool RefundVotes => CustomOptionHolder.refundVotesOnDeath.getBool();

    public static void destroyList<T>(List<T> items) where T : Object
    {
        if (items == null) return;
        foreach (T item in items) Object.Destroy(item);
    }

    public static void destroyList<T>(System.Collections.Generic.List<T> items) where T : Object
    {
        if (items == null) return;
        foreach (T item in items) Object.Destroy(item);
    }

    public static void log(string msg)
    {
        TheOtherRolesPlugin.Instance.Log.LogInfo(msg);
    }
    public static bool isChinese()
    {
        try
        {
            var name = CultureInfo.CurrentUICulture.Name;
            if (name.StartsWith("zh")) return true;
            return false;
        }
        catch
        {
            return false;
        }
    }
    public static System.Collections.Generic.List<byte> generateTasks(int numCommon, int numShort, int numLong)
    {
        if (numCommon + numShort + numLong <= 0)
        {
            numShort = 1;
        }

        var tasks = new List<byte>();
        var hashSet = new HashSet<TaskTypes>();

        var commonTasks = new List<NormalPlayerTask>();
        foreach (var task in MapUtilities.CachedShipStatus.CommonTasks.OrderBy(x => rnd.Next())) commonTasks.Add(task);

        var shortTasks = new List<NormalPlayerTask>();
        foreach (var task in MapUtilities.CachedShipStatus.ShortTasks.OrderBy(x => rnd.Next())) shortTasks.Add(task);

        var longTasks = new List<NormalPlayerTask>();
        foreach (var task in MapUtilities.CachedShipStatus.LongTasks.OrderBy(x => rnd.Next())) longTasks.Add(task);

        int start = 0;
        MapUtilities.CachedShipStatus.AddTasksFromList(ref start, numCommon, tasks, hashSet, commonTasks);

        start = 0;
        MapUtilities.CachedShipStatus.AddTasksFromList(ref start, numShort, tasks, hashSet, shortTasks);

        start = 0;
        MapUtilities.CachedShipStatus.AddTasksFromList(ref start, numLong, tasks, hashSet, longTasks);

        return tasks.ToArray().ToList();
    }

    public static void generateAndAssignTasks(this PlayerControl player, int numCommon, int numShort, int numLong)
    {
        if (player == null) return;

        System.Collections.Generic.List<byte> taskTypeIds = generateTasks(numCommon, numShort, numLong);

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedSetTasks, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.WriteBytesAndSize(taskTypeIds.ToArray());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCProcedure.uncheckedSetTasks(player.PlayerId, taskTypeIds.ToArray());
    }
    public static void setInvisible(PlayerControl player, Color color, float alpha)
    {
        if (player.cosmetics.currentBodySprite.BodySprite != null)
            player.cosmetics.currentBodySprite.BodySprite.color = color;

        if (player.cosmetics.skin?.layer != null)
            player.cosmetics.skin.layer.color = color;

        if (player.cosmetics.hat != null)
        {
            player.cosmetics.hat.FrontLayer.color = color;
            player.cosmetics.hat.BackLayer.color = color;
        }

        if (player.cosmetics.currentPet != null)
            player.cosmetics.currentPet.SetAlpha(alpha);

        if (player.cosmetics.visor != null)
            player.cosmetics.visor.Image.color = color;

        if (player.cosmetics.colorBlindText != null)
            player.cosmetics.colorBlindText.color = color;

        if (player.cosmetics.PettingHand != null)
            player.cosmetics.PettingHand.SetAlpha(alpha);
    }

    public static void setSkinWithAnim(PlayerPhysics playerPhysics, string SkinId)
    {
        SkinViewData nextSkin = null;
        try { nextSkin = ShipStatus.Instance.CosmeticsCache.GetSkin(SkinId); } catch { return; };
        AnimationClip clip = null;
        SpriteAnim spriteAnim = playerPhysics.myPlayer.cosmetics.skin.animator;
        Animator anim = spriteAnim.m_animator;
        SkinLayer skinLayer = playerPhysics.myPlayer.cosmetics.skin;

        AnimationClip currentPhysicsAnim = playerPhysics.Animations.Animator.GetCurrentAnimation();
        if (currentPhysicsAnim == playerPhysics.Animations.group.RunAnim)
            clip = nextSkin.RunAnim;
        else if (currentPhysicsAnim == playerPhysics.Animations.group.SpawnAnim)
            clip = nextSkin.SpawnAnim;
        else if (currentPhysicsAnim == playerPhysics.Animations.group.EnterVentAnim)
            clip = nextSkin.EnterVentAnim;
        else if (currentPhysicsAnim == playerPhysics.Animations.group.ExitVentAnim)
            clip = nextSkin.ExitVentAnim;
        else if (currentPhysicsAnim == playerPhysics.Animations.group.IdleAnim)
            clip = nextSkin.IdleAnim;
        else clip = nextSkin.IdleAnim;

        float progress = playerPhysics.Animations.Animator.m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        skinLayer.skin = nextSkin;

        spriteAnim.Play(clip);
        anim.Play("a", 0, progress % 1);
        anim.Update(0f);
    }

    public static PlayerControl playerById(byte id)
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            if (player.PlayerId == id)
                return player;
        return null;
    }

    public static System.Collections.Generic.Dictionary<byte, PlayerControl> allPlayersById()
    {
        System.Collections.Generic.Dictionary<byte, PlayerControl> res = new();
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            res.Add(player.PlayerId, player);
        return res;
    }

    public static void handleVampireBiteOnBodyReport()
    {
        // Murder the bitten player and reset bitten (regardless whether the kill was successful or not)
        checkMuderAttemptAndKill(Vampire.vampire, Vampire.bitten, true, false);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
            (byte)CustomRPC.VampireSetBitten, SendOption.Reliable, -1);
        writer.Write(byte.MaxValue);
        writer.Write(byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCProcedure.vampireSetBitten(byte.MaxValue, byte.MaxValue);
    }

    public static void refreshRoleDescription(PlayerControl player)
    {
        if (player == null) return;

        System.Collections.Generic.List<RoleInfo> infos = RoleInfo.getRoleInfoForPlayer(player);

        System.Collections.Generic.List<PlayerTask> toRemove = new();
        foreach (PlayerTask t in player.myTasks)
        {
            ImportantTextTask textTask = t.gameObject.GetComponent<ImportantTextTask>();
            if (textTask != null)
            {
                RoleInfo info = infos.FirstOrDefault(x => textTask.Text.StartsWith(x.name));
                if (info != null)
                    infos.Remove(info); // TextTask for this RoleInfo does not have to be added, as it already exists
                else
                    toRemove.Add(t); // TextTask does not have a corresponding RoleInfo and will hence be deleted
            }
        }

        foreach (PlayerTask t in toRemove)
        {
            t.OnRemove();
            player.myTasks.Remove(t);
            Object.Destroy(t.gameObject);
        }

        // Add TextTask for remaining RoleInfos
        foreach (RoleInfo roleInfo in infos)
        {
            ImportantTextTask task = new GameObject("RoleTask").AddComponent<ImportantTextTask>();
            task.transform.SetParent(player.transform, false);

            if (roleInfo.roleType == RoleType.Jackal)
            {
                if (Jackal.canCreateSidekick)
                    task.Text = cs(roleInfo.color,
                        $"{roleInfo.name}: " + ModTranslation.getString("jackalWithSidekick"));
                else
                    task.Text = cs(roleInfo.color, $"{roleInfo.name}: " + ModTranslation.getString("jackalShortDesc"));
            }
            else
                task.Text = cs(roleInfo.color, $"{roleInfo.name}: {roleInfo.shortDescription}");

            player.myTasks.Insert(0, task);
        }

        if (player.hasModifier(ModifierType.Madmate) || player.hasModifier(ModifierType.CreatedMadmate))
        {
            ImportantTextTask task = new GameObject("RoleTask").AddComponent<ImportantTextTask>();
            task.transform.SetParent(player.transform, false);
            task.Text = cs(Madmate.color, $"{Madmate.fullName}: " + ModTranslation.getString("madmateShortDesc"));
            player.myTasks.Insert(0, task);
        }
    }

    public static bool isLighterColor(int colorId)
    {
        return CustomColors.lighterColors.Contains(colorId);
    }

    public static bool isCustomServer()
    {
        if (FastDestroyableSingleton<ServerManager>.Instance == null) return false;
        StringNames n = FastDestroyableSingleton<ServerManager>.Instance.CurrentRegion.TranslateName;
        return n is not StringNames.ServerNA and not StringNames.ServerEU and not StringNames.ServerAS;
    }

    public static bool isDead(this PlayerControl player)
    {
        return player == null || player?.Data?.IsDead == true || player?.Data?.Disconnected == true ||
               (finalStatuses != null && finalStatuses.ContainsKey(player.PlayerId) &&
                finalStatuses[player.PlayerId] != FinalStatus.Alive);
    }

    public static bool isAlive(this PlayerControl player)
    {
        return !isDead(player);
    }

    public static bool isNeutral(this PlayerControl player)
    {
        return player != null &&
               (player.isRole(RoleType.Jackal) ||
                player.isRole(RoleType.Sidekick) ||
                Jackal.formerJackals.Contains(player) ||
                player.isRole(RoleType.Arsonist) ||
                player.isRole(RoleType.Jester) ||
                player.isRole(RoleType.Opportunist) ||
                player.isRole(RoleType.PlagueDoctor) ||
                player.isRole(RoleType.Fox) ||
                player.isRole(RoleType.Immoralist) ||
                player.isRole(RoleType.SchrodingersCat) ||
                player.isRole(RoleType.Puppeteer) ||
                (player.isRole(RoleType.JekyllAndHyde) && !JekyllAndHyde.isJekyll()) ||
                player.isRole(RoleType.Moriarty) ||
                player == Puppeteer.dummy ||
                player.isRole(RoleType.Vulture) ||
                player.isRole(RoleType.Lawyer) ||
                player.isRole(RoleType.Pursuer) ||
                player.isRole(RoleType.Akujo) ||
                player.isRole(RoleType.Cupid) ||
                (player.isRole(RoleType.Shifter) && Shifter.isNeutral));
    }

    public static bool isNeutral(this RoleType roleType)
    {
        return roleType == RoleType.Jackal ||
                roleType == RoleType.Sidekick ||
                roleType == RoleType.Jackal ||
                roleType == RoleType.Arsonist ||
                roleType == RoleType.Jester ||
                roleType == RoleType.Opportunist ||
                roleType == RoleType.PlagueDoctor ||
                roleType == RoleType.Fox ||
                roleType == RoleType.Immoralist ||
                roleType == RoleType.SchrodingersCat ||
                roleType == RoleType.Puppeteer ||
                (roleType == RoleType.JekyllAndHyde) && !JekyllAndHyde.isJekyll() ||
                roleType == RoleType.Moriarty ||
                //player == Puppeteer.dummy ||
                roleType == RoleType.Vulture ||
                roleType == RoleType.Lawyer ||
                roleType == RoleType.Pursuer ||
                roleType == RoleType.Akujo ||
                roleType == RoleType.Cupid ||
                (roleType == RoleType.Shifter) && Shifter.isNeutral;
    }

    public static bool isCrew(this PlayerControl player)
    {
        return player != null && !player.isImpostor() && !player.isNeutral() && !player.isGM();
    }
    public static void ModRevive(this PlayerControl player, bool resetRoleIfGhost = true)
    {
        player.Data.IsDead = false;
        player.gameObject.layer = LayerMask.NameToLayer("Players");
        player.MyPhysics.ResetMoveState();
        player.cosmetics.SetPetSource(player);
        player.cosmetics.SetNameMask(true);
        if (player.AmOwner)
        {
            DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(true);
            DestroyableSingleton<HudManager>.Instance.KillButton.ToggleVisible(player.Data.Role.IsImpostor);
            DestroyableSingleton<HudManager>.Instance.AdminButton.ToggleVisible(player.Data.Role.IsImpostor);
            DestroyableSingleton<HudManager>.Instance.SabotageButton.ToggleVisible(player.Data.Role.IsImpostor);
            DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(player.Data.Role.IsImpostor);
            DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
            DestroyableSingleton<HudManager>.Instance.Chat.SetVisible(false);
        }
        if (!resetRoleIfGhost || !AmongUsClient.Instance.AmHost || !RoleManager.IsGhostRole(player.Data.Role.Role))
            return;
        player.RpcSetRole(RoleTypes.Crewmate);
    }
    public static string camelString(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string firstLetter = input.Substring(0, 1).ToUpper();
        string remainingLetters = input.Substring(1).ToLower();
        return firstLetter + remainingLetters;
    }

    public static bool isImpostor(this PlayerControl player)
    {
        return player != null && player.Data.Role.IsImpostor;
    }

    public static bool hasFakeTasks(this PlayerControl player)
    {
        return (player.isNeutral() && !player.neutralHasTasks()) ||
               (player.hasModifier(ModifierType.CreatedMadmate) && !CreatedMadmate.hasTasks) ||
               (player.hasModifier(ModifierType.Madmate) && !Madmate.hasTasks) ||
               (player.isLovers() && Lovers.separateTeam && !Lovers.tasksCount);
    }

    public static bool neutralHasTasks(this PlayerControl player)
    {
        if (player.isRole(RoleType.SchrodingersCat) && SchrodingersCat.hideRole) return true;
        if (player.isRole(RoleType.JekyllAndHyde)) return true;
        return player.isNeutral() && (player.isRole(RoleType.Lawyer) || player.isRole(RoleType.Pursuer) ||
                                      player.isRole(RoleType.Shifter) || player.isRole(RoleType.Fox));
    }

    public static bool isGM(this PlayerControl player)
    {
        return GM.gm != null && player == GM.gm;
    }

    public static bool isLovers(this PlayerControl player)
    {
        return player != null && Lovers.isLovers(player);
    }

    public static bool isAkujoLover(this PlayerControl player)
    {
        return player != null &&
               (player.isRole(RoleType.Akujo) ||
                player.hasModifier(ModifierType.AkujoHonmei) ||
                player.hasModifier(ModifierType.AkujoKeep));
    }

    public static bool isAkujoPartners(this PlayerControl player, PlayerControl partner)
    {
        foreach (Akujo akujo in Akujo.players)
            if ((akujo.player == player && akujo.isPartner(partner)) ||
                (akujo.player == partner && akujo.isPartner(player)))
                return true;

        return false;
    }

    public static PlayerControl getPartner(this PlayerControl player)
    {
        return Lovers.getPartner(player);
    }

    public static bool canBeErased(this PlayerControl player)
    {
        return player != Jackal.jackal && player != Sidekick.sidekick && !Jackal.formerJackals.Contains(player);
    }

    public static void clearAllTasks(this PlayerControl player)
    {
        if (player == null) return;
        foreach (PlayerTask playerTask in player.myTasks)
        {
            playerTask.OnRemove();
            Object.Destroy(playerTask.gameObject);
        }

        player.myTasks.Clear();

        if (player.Data != null && player.Data.Tasks != null)
            player.Data.Tasks.Clear();
    }

    public static bool isMira() {
        return GameOptionsManager.Instance.CurrentGameOptions.MapId == 1;
    }
    public static bool isAirship() {
        return GameOptionsManager.Instance.CurrentGameOptions.MapId == 4;
    }
    public static bool isSkeld() {
        return GameOptionsManager.Instance.CurrentGameOptions.MapId == 0;
    }
    public static bool isPolus() {
        return GameOptionsManager.Instance.CurrentGameOptions.MapId == 2;
    }

    public static bool isFungle()
    {
        return GameOptionsManager.Instance.CurrentGameOptions.MapId == 5;
    }

    public static void Destroy(this Object obj)
    {
        Object.Destroy(obj);
    }

    public static void setSemiTransparent(this PoolablePlayer player, bool value)
    {
        float alpha = value ? 0.25f : 1f;
        foreach (SpriteRenderer r in player.gameObject.GetComponentsInChildren<SpriteRenderer>())
            r.color = new Color(r.color.r, r.color.g, r.color.b, alpha);
        player.cosmetics.nameText.color = new Color(player.cosmetics.nameText.color.r,
            player.cosmetics.nameText.color.g, player.cosmetics.nameText.color.b, alpha);
    }

    public static string GetString(this TranslationController t, StringNames key, params Il2CppSystem.Object[] parts)
    {
        return t.GetString(key, parts);
    }

    public static string cs(Color c, string s)
    {
        return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(c.r), ToByte(c.g), ToByte(c.b),
            ToByte(c.a), s);
    }

    private static byte ToByte(float f)
    {
        f = Mathf.Clamp01(f);
        return (byte)(f * 255);
    }
    public static void AddModSettingsChangeMessage(this NotificationPopper popper, StringNames key, string value, string option, bool playSound = true)
    {
        string str = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.LobbyChangeSettingNotification, "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + option + "</font>", "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + value + "</font>");
        popper.SettingsChangeMessageLogic(key, str, playSound);
    }
    public static int lineCount(string text)
    {
        return text.Count(c => c == '\n');
    }
    
    public static bool hidePlayerName(PlayerControl target)
    {
        return hidePlayerName(PlayerControl.LocalPlayer, target);
    }

    public static bool hidePlayerName(PlayerControl source, PlayerControl target)
    {
        if (source == target) return false;
        if (source == null || target == null) return true;
        if (source.isDead()) return false;
        if (target.isDead()) return true;
        if (Camouflager.camouflageTimer > 0f) return true; // No names are visible
        if (!source.isImpostor() && Ninja.isStealthed(target)) return true; // Hide ninja nametags from non-impostors
        if (!source.isRole(RoleType.Fox) && !source.Data.IsDead && Fox.isStealthed(target)) return true;
        if (!source.isRole(RoleType.Puppeteer) && !source.Data.IsDead && target.isRole(RoleType.Puppeteer) &&
            Puppeteer.stealthed) return true;
        if (!source.isRole(RoleType.Puppeteer) && !source.Data.IsDead && target == Puppeteer.dummy &&
            !Puppeteer.stealthed) return true;
        if (TORMapOptions.hideOutOfSightNametags && GameStarted && MapUtilities.CachedShipStatus != null &&
            source.transform != null && target.transform != null)
        {
            float distMod = 1.025f;
            float distance = Vector3.Distance(source.transform.position, target.transform.position);
            bool anythingBetween = PhysicsHelpers.AnythingBetween(source.GetTruePosition(), target.GetTruePosition(),
                Constants.ShadowMask, false);

            if (distance > MapUtilities.CachedShipStatus.CalculateLightRadius(source.Data) * distMod ||
                anythingBetween) return true;
        }

        if (!TORMapOptions.hidePlayerNames) return false; // All names are visible
        if (source.isImpostor() && (target.isImpostor() || target.isRole(RoleType.Spy) ||
                                    (target == Sidekick.sidekick && Sidekick.wasTeamRed) ||
                                    (target == Jackal.jackal && Jackal.wasTeamRed)))
            return false; // Members of team Impostors see the names of Impostors/Spies
        if (source.getPartner() == target) return false; // Members of team Lovers see the names of each other
        if ((source.isRole(RoleType.Jackal) || source.isRole(RoleType.Sidekick)) && (target.isRole(RoleType.Jackal) ||
                target.isRole(RoleType.Sidekick) ||
                target == Jackal.fakeSidekick)) return false; // Members of team Jackal see the names of each other
        if ((source.isRole(RoleType.Fox) || source.isRole(RoleType.Immoralist)) &&
            (target.isRole(RoleType.Fox) || target.isRole(RoleType.Immoralist)))
            return false; // Members of team Fox see the names of each other
        return true;
    }

    public static void setDefaultLook(this PlayerControl target)
    {
        target.setLook(target.Data.PlayerName, target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId,
            target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.PetId);
    }

    public static void setLook(this PlayerControl target, string playerName, int colorId, string hatId, string visorId,
        string skinId, string petId)
    {
        target.RawSetColor(colorId);
        target.RawSetVisor(visorId, colorId);
        target.RawSetHat(hatId, colorId);
        target.RawSetName(hidePlayerName(PlayerControl.LocalPlayer, target) ? "" : playerName);


        SkinViewData nextSkin = null;
        try { nextSkin = ShipStatus.Instance.CosmeticsCache.GetSkin(skinId); } catch { return; };

        PlayerPhysics playerPhysics = target.MyPhysics;
        AnimationClip clip = null;
        var spriteAnim = playerPhysics.myPlayer.cosmetics.skin.animator;
        var currentPhysicsAnim = playerPhysics.Animations.Animator.GetCurrentAnimation();


        if (currentPhysicsAnim == playerPhysics.Animations.group.RunAnim) clip = nextSkin.RunAnim;
        else if (currentPhysicsAnim == playerPhysics.Animations.group.SpawnAnim) clip = nextSkin.SpawnAnim;
        else if (currentPhysicsAnim == playerPhysics.Animations.group.EnterVentAnim) clip = nextSkin.EnterVentAnim;
        else if (currentPhysicsAnim == playerPhysics.Animations.group.ExitVentAnim) clip = nextSkin.ExitVentAnim;
        else if (currentPhysicsAnim == playerPhysics.Animations.group.IdleAnim) clip = nextSkin.IdleAnim;
        else clip = nextSkin.IdleAnim;
        float progress = playerPhysics.Animations.Animator.m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        playerPhysics.myPlayer.cosmetics.skin.skin = nextSkin;
        playerPhysics.myPlayer.cosmetics.skin.UpdateMaterial();

        spriteAnim.Play(clip, 1f);
        spriteAnim.m_animator.Play("a", 0, progress % 1);
        spriteAnim.m_animator.Update(0f);

        target.RawSetPet(petId, colorId);
    }

    public static bool roleCanUseVents(this PlayerControl player)
    {
        bool roleCouldUse = false;
        if (player.isRole(RoleType.Engineer))
            roleCouldUse = true;
        else if (Jackal.canUseVents && player.isRole(RoleType.Jackal))
            roleCouldUse = true;
        else if (Sidekick.canUseVents && player.isRole(RoleType.Sidekick))
            roleCouldUse = true;
        else if (Spy.canEnterVents && player.isRole(RoleType.Spy))
            roleCouldUse = true;
        else if (Madmate.canEnterVents && player.hasModifier(ModifierType.Madmate))
            roleCouldUse = true;
        else if (CreatedMadmate.canEnterVents && player.hasModifier(ModifierType.CreatedMadmate))
            roleCouldUse = true;
        else if (Vulture.canUseVents && player.isRole(RoleType.Vulture))
            roleCouldUse = true;
        else if (player.isRole(RoleType.JekyllAndHyde) && !JekyllAndHyde.isJekyll())
            roleCouldUse = true;
        else if (player.isRole(RoleType.Moriarty))
            roleCouldUse = true;
        else if (player.Data?.Role != null && player.Data.Role.CanVent)
        {
            if (!Janitor.canVent && player.isRole(RoleType.Janitor))
                roleCouldUse = false;
            else if (!Mafioso.canVent && player.isRole(RoleType.Mafioso))
                roleCouldUse = false;
            else if (!Ninja.canUseVents && player.isRole(RoleType.Ninja))
                roleCouldUse = false;
            else
                roleCouldUse = true;
        }

        return roleCouldUse;
    }

    public static bool roleCanSabotage(this PlayerControl player)
    {
        bool roleCouldUse = false;
        if (Madmate.canSabotage && player.hasModifier(ModifierType.Madmate))
            roleCouldUse = true;
        else if (CreatedMadmate.canSabotage && player.hasModifier(ModifierType.CreatedMadmate))
            roleCouldUse = true;
        else if (Jester.canSabotage && player.isRole(RoleType.Jester))
            roleCouldUse = true;
        else if (!Mafioso.canSabotage && player.isRole(RoleType.Mafioso))
            roleCouldUse = false;
        else if (!Janitor.canSabotage && player.isRole(RoleType.Janitor))
            roleCouldUse = false;
        else if (player.Data?.Role != null && player.Data.Role.IsImpostor)
            roleCouldUse = true;

        return roleCouldUse;
    }

    public static MurderAttemptResult checkMuderAttempt(PlayerControl killer, PlayerControl target,
        bool blockRewind = false)
    {
        // Modified vanilla checks
        if (AmongUsClient.Instance.IsGameOver) return MurderAttemptResult.SuppressKill;
        if (killer == null || killer.Data == null || killer.Data.IsDead || killer.Data.Disconnected)
            return MurderAttemptResult.SuppressKill; // Allow non Impostor kills compared to vanilla code
        if (target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected)
            return MurderAttemptResult.SuppressKill; // Allow killing players in vents compared to vanilla code

        // Handle blank shot
        if (Pursuer.blankedList.Any(x => x.PlayerId == killer.PlayerId))
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBlanked, SendOption.Reliable, -1);
            writer.Write(killer.PlayerId);
            writer.Write((byte)0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.setBlanked(killer.PlayerId, 0);

            return MurderAttemptResult.BlankKill;
        }

        // Block impostor shielded kill
        if (Medic.shielded != null && Medic.shielded == target)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId,
                (byte)CustomRPC.ShieldedMurderAttempt, SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.shieldedMurderAttempt();
            return MurderAttemptResult.SuppressKill;
        }

        // Block impostor not fully grown mini kill

        if (target.hasModifier(ModifierType.Mini) && !Mini.isGrownUp(target)) return MurderAttemptResult.SuppressKill;
        // Block Time Master with time shield kill
        if (TimeMaster.shieldActive && TimeMaster.timeMaster != null && TimeMaster.timeMaster == target)
        {
            if (!blockRewind)
            {
                // Only rewind the attempt was not called because a meeting startet
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId,
                    (byte)CustomRPC.TimeMasterRewindTime, SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.timeMasterRewindTime();
            }

            return MurderAttemptResult.SuppressKill;
        }

        // キューピッドのシールド

        if (Cupid.checkShieldActive(target))
        {
            Cupid.scapeGoat(target);
            return MurderAttemptResult.BlankKill;
        }

        return MurderAttemptResult.PerformKill;
    }

    public static MurderAttemptResult checkMuderAttemptAndKill(PlayerControl killer, PlayerControl target,
        bool isMeetingStart = false, bool showAnimation = true)
    {
        // The local player checks for the validity of the kill and performs it afterwards (different to vanilla, where the host performs all the checks)
        // The kill attempt will be shared using a custom RPC, hence combining modded and unmodded versions is impossible

        MurderAttemptResult murder = checkMuderAttempt(killer, target, isMeetingStart);
        if (murder == MurderAttemptResult.PerformKill)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UncheckedMurderPlayer,
                SendOption.Reliable, -1);
            writer.Write(killer.PlayerId);
            writer.Write(target.PlayerId);
            writer.Write(showAnimation ? byte.MaxValue : 0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.uncheckedMurderPlayer(killer.PlayerId, target.PlayerId,
                showAnimation ? byte.MaxValue : (byte)0);
        }

        return murder;
    }

    public static void shareGameVersion()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
            (byte)CustomRPC.VersionHandshake, SendOption.Reliable, -1);
        writer.WritePacked(TheOtherRolesPlugin.Version.Major);
        writer.WritePacked(TheOtherRolesPlugin.Version.Minor);
        writer.WritePacked(TheOtherRolesPlugin.Version.Build);
        writer.WritePacked(AmongUsClient.Instance.ClientId);
        writer.Write((byte)(TheOtherRolesPlugin.Version.Revision < 0 ? 0xFF : TheOtherRolesPlugin.Version.Revision));
        writer.Write(Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToByteArray());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        RPCProcedure.versionHandshake(TheOtherRolesPlugin.Version.Major, TheOtherRolesPlugin.Version.Minor,
            TheOtherRolesPlugin.Version.Build, TheOtherRolesPlugin.Version.Revision,
            Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId, AmongUsClient.Instance.ClientId);
    }

    public static System.Collections.Generic.List<PlayerControl> getKillerTeamMembers(PlayerControl player)
    {
        System.Collections.Generic.List<PlayerControl> team = new();
        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            if (player.Data.Role.IsImpostor && p.Data.Role.IsImpostor && player.PlayerId != p.PlayerId &&
                team.All(x => x.PlayerId != p.PlayerId)) team.Add(p);
            else if (player.isRole(RoleType.Jackal) && p.isRole(RoleType.Sidekick)) team.Add(p);
            else if (player.isRole(RoleType.Sidekick) && p.isRole(RoleType.Jackal)) team.Add(p);

        return team;
    }

    public static void showFlash(Color color, float duration = 1f)
    {
        if (FastDestroyableSingleton<HudManager>.Instance == null ||
            FastDestroyableSingleton<HudManager>.Instance.FullScreen == null) return;
        FastDestroyableSingleton<HudManager>.Instance.FullScreen.gameObject.SetActive(true);
        FastDestroyableSingleton<HudManager>.Instance.FullScreen.enabled = true;
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(duration, new Action<float>(p =>
        {
            SpriteRenderer renderer = FastDestroyableSingleton<HudManager>.Instance.FullScreen;

            if (p < 0.5)
            {
                if (renderer != null)
                    renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(p * 2 * 0.75f));
            }
            else
            {
                if (renderer != null)
                    renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01((1 - p) * 2 * 0.75f));
            }

            if (p == 1f && renderer != null)
            {
                bool reactorActive = false;
                foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
                    if (task.TaskType == TaskTypes.StopCharles)
                        reactorActive = true;

                if (!reactorActive && GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4) renderer.color = Color.black;
                renderer.gameObject.SetActive(false);
            }
        })));
    }

    public static void shuffle<T>(this System.Collections.Generic.IList<T> self, int startAt = 0)
    {
        for (int i = startAt; i < self.Count - 1; i++)
        {
            T value = self[i];
            int index = UnityEngine.Random.Range(i, self.Count);
            self[i] = self[index];
            self[index] = value;
        }
    }

    public static void shuffle<T>(this Random r, System.Collections.Generic.IList<T> self)
    {
        for (int i = 0; i < self.Count; i++)
        {
            T value = self[i];
            int index = r.Next(self.Count);
            self[i] = self[index];
            self[index] = value;
        }
    }

    public static object TryCast(this Il2CppObjectBase self, Type type)
    {
        return AccessTools.Method(self.GetType(), nameof(Il2CppObjectBase.TryCast)).MakeGenericMethod(type)
            .Invoke(self, Array.Empty<object>());
    }

    public static ClientData getClient(this PlayerControl player)
    {
        return AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId)
            .FirstOrDefault();
    }

    public static string getPlatform(this PlayerControl player)
    {
        return player?.getClient()?.PlatformData?.Platform.ToString();
    }

    public static PlayerControl getPlayerById(byte playerId)
    {
        return PlayerControl.AllPlayerControls.GetFastEnumerator().ToArray().Where(p => p.PlayerId == playerId)
            .FirstOrDefault();
    }

    public static IEnumerable FindObjectsOfType(Type type)
    {
        MethodInfo[] methods = typeof(Object).GetMethods(BindingFlags.Static | BindingFlags.Public);
        MethodInfo method = methods.First(m => m.Name == "FindObjectsOfType" && m.GetParameters().Length == 0)
            .MakeGenericMethod(type);
        object returned = method.Invoke(null, new object[0]);
        return returned as IEnumerable;
    }

    public static System.Collections.Generic.List<T> ToSystemList<T>(this List<T> iList)
    {
        System.Collections.Generic.List<T> systemList = new();
        foreach (T item in iList) systemList.Add(item);
        return systemList;
    }

    public static string removeHtml(this string text)
    {
        return Regex.Replace(text, "<[^>]*?>", "");
    }

    public static string getRoleName(this PlayerControl player)
    {
        return RoleInfo.GetRolesString(player, false, joinSeparator: " + ");
    }

    public static string getNameWithRole(this PlayerControl player)
    {
        return $"{player?.Data?.PlayerName}({player?.getRoleName()})";
    }

    public static string getNameWithRole(this NetworkedPlayerInfo player)
    {
        return $"{player?.PlayerName}({getPlayerById(player.PlayerId).getRoleName()})";
    }

    public static string getVoteName(byte num)
    {
        string name = "invalid";
        PlayerControl player = getPlayerById(num);
        if (num < 15 && player != null) name = player?.getNameWithRole();
        if (num == 253) name = "Skip";
        if (num == 254) name = "None";
        if (num == 255) name = "Dead";
        return name;
    }

    public static string PadRightV2(this object text, int num)
    {
        int bc = 0;
        string t = text.ToString();
        foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
        return t?.PadRight(num - (bc - t.Length));
    }

    public static System.Collections.Generic.List<T> ToList<T>(this Il2CppArrayBase<T> array)
    {
        System.Collections.Generic.List<T> list = new();
        foreach (T item in array) list.Add(item);
        return list;
    }

    public static void DestroyTranslator(this GameObject obj)
    {
        if (obj == null) return;
        obj.ForEachChild((Il2CppSystem.Action<GameObject>)DestroyTranslator);
        TextTranslatorTMP[] translator = obj.GetComponentsInChildren<TextTranslatorTMP>(true);
        translator?.Do(Object.Destroy);
    }
    public static void DestroyTranslator(this MonoBehaviour obj) => obj?.gameObject?.DestroyTranslator();

    public static bool hasImpostorVision(PlayerControl player)
    {
        if (player.isImpostor()
            || (Jackal.jackal != null && Jackal.jackal.PlayerId == player.PlayerId && Jackal.hasImpostorVision)
            || (Sidekick.sidekick != null && Sidekick.sidekick.PlayerId == player.PlayerId &&
                Sidekick.hasImpostorVision)
            || (Spy.spy != null && Spy.spy.PlayerId == player.PlayerId && Spy.hasImpostorVision)
            || (Jester.jester != null && Jester.jester.PlayerId == player.PlayerId && Jester.hasImpostorVision)
            || (player.hasModifier(ModifierType.Madmate) &&
                Madmate.hasImpostorVision) // Impostor, Jackal/Sidekick, Spy, or Madmate with Impostor vision
            || (player.hasModifier(ModifierType.CreatedMadmate) &&
                CreatedMadmate.hasImpostorVision) // Impostor, Jackal/Sidekick, Spy, or Madmate with Impostor vision
            || player.isRole(RoleType.Puppeteer)
            || (player.isRole(RoleType.JekyllAndHyde) && !JekyllAndHyde.isJekyll())
            || player.isRole(RoleType.Moriarty)
            || (Jester.jester != null && Jester.jester.PlayerId == player.PlayerId &&
                Jester.hasImpostorVision) // Jester with Impostor vision
            || player.isRole(RoleType.Fox))
            return true;

        return false;
    }

    public static PlainShipRoom getPlainShipRoom(PlayerControl p)
    {
        PlainShipRoom[] array = null;
        Il2CppReferenceArray<Collider2D> buffer = new Collider2D[10];
        ContactFilter2D filter = default;
        filter.layerMask = Constants.PlayersOnlyMask;
        filter.useLayerMask = true;
        filter.useTriggers = false;
        array = MapUtilities.CachedShipStatus?.AllRooms;
        if (array == null) return null;
        foreach (PlainShipRoom plainShipRoom in array)
            if (plainShipRoom.roomArea)
            {
                int hitCount = plainShipRoom.roomArea.OverlapCollider(filter, buffer);
                if (hitCount == 0) continue;
                for (int i = 0; i < hitCount; i++)
                    if (buffer[i]?.gameObject == p.gameObject)
                        return plainShipRoom;
            }

        return null;
    }

    public static bool isCrewmateAlive()
    {
        foreach (PlayerControl p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            if (p.isCrew() && !p.isRole(RoleType.JekyllAndHyde) && !p.hasModifier(ModifierType.Madmate) && p.isAlive())
                return true;
        return false;
    }

    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null)
        {
        }
        else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null)
        {
        }
        else rpcName = callId.ToString();

        return rpcName;
    }

    public static string SerializeObject(object value)
    {
        Type type = TheOtherRolesPlugin.JsonNet.GetType("Newtonsoft.Json.JsonConvert");
        MethodInfo method = type.GetMethods().FirstOrDefault(c =>
        {
            return c.Name == "SerializeObject" && c.GetParameters().Length == 1;
        });
        return method.Invoke(null, new[] { value }) as string;
    }
}
