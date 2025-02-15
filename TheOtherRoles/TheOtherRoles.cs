using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace TheOtherRoles;

[HarmonyPatch]
public static class TheOtherRoles
{
    public enum Teams
    {
        Crew,
        Impostor,
        Jackal,
        Jester,
        Arsonist,
        Lovers,
        Opportunist,
        Vulture,

        None = int.MaxValue
    }

    // public static System.Random rnd = new System.Random((int)DateTime.Now.Ticks);
    // public static PermutedCongruentialGenerator rnd { get { return RandomGenerator.Instance; } }
    public static Random rnd = new MersenneTwister((int)DateTime.Now.Ticks);

    public static void clearAndReloadRoles()
    {
        Jester.clearAndReload();
        Mayor.clearAndReload();
        Engineer.clearAndReload();
        Sheriff.Clear();
        Lighter.Clear();
        Godfather.clearAndReload();
        Mafioso.clearAndReload();
        Janitor.clearAndReload();
        Detective.clearAndReload();
        TimeMaster.clearAndReload();
        Medic.clearAndReload();
        Seer.clearAndReload();
        EvilHacker.clearAndReload();
        Hacker.clearAndReload();
        Tracker.clearAndReload();
        Vampire.clearAndReload();
        Snitch.clearAndReload();
        Jackal.clearAndReload();
        Sidekick.clearAndReload();
        Eraser.clearAndReload();
        Spy.clearAndReload();
        Trickster.clearAndReload();
        Cleaner.clearAndReload();
        Warlock.clearAndReload();
        SecurityGuard.clearAndReload();
        Arsonist.clearAndReload();
        Guesser.clearAndReload();
        BountyHunter.clearAndReload();
        Bait.clearAndReload();
        Vulture.clearAndReload();
        Medium.clearAndReload();
        Lawyer.clearAndReload();
        Pursuer.clearAndReload();
        Witch.clearAndReload();
        Assassin.clearAndReload();
        TheOtherRolesGM.clearAndReloadRoles();
    }

    public static class Jester
    {
        public static PlayerControl jester;
        public static Color color = new Color32(236, 98, 165, byte.MaxValue);

        public static bool triggerJesterWin;
        public static bool canCallEmergency = true;
        public static bool canSabotage = true;
        public static bool canUseVent = true;
        public static bool hasImpostorVision;

        public static void clearAndReload()
        {
            jester = null;
            triggerJesterWin = false;
            canCallEmergency = CustomOptionHolder.jesterCanCallEmergency.getBool();
            canUseVent = CustomOptionHolder.jesterCanUseVent.getBool();
            canSabotage = CustomOptionHolder.jesterCanSabotage.getBool();
            hasImpostorVision = CustomOptionHolder.jesterHasImpostorVision.getBool();
        }
    }

    public static class Mayor
    {
        public static PlayerControl mayor;
        public static Color color = new Color32(32, 77, 66, byte.MaxValue);
        public static int numVotes = 2;

        public static void clearAndReload()
        {
            mayor = null;
            numVotes = (int)CustomOptionHolder.mayorNumVotes.getFloat();
        }
    }

    public static class Engineer
    {
        public static PlayerControl engineer;
        public static Color color = new Color32(0, 40, 245, byte.MaxValue);
        private static Sprite buttonSprite;

        public static int remainingFixes = 1;
        public static bool highlightForImpostors = true;
        public static bool highlightForTeamJackal = true;

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.RepairButton.png", 115f);
            return buttonSprite;
        }

        public static void clearAndReload()
        {
            engineer = null;
            remainingFixes = Mathf.RoundToInt(CustomOptionHolder.engineerNumberOfFixes.getFloat());
            highlightForImpostors = CustomOptionHolder.engineerHighlightForImpostors.getBool();
            highlightForTeamJackal = CustomOptionHolder.engineerHighlightForTeamJackal.getBool();
        }
    }

    public static class Godfather
    {
        public static PlayerControl godfather;
        public static Color color = Palette.ImpostorRed;

        public static void clearAndReload()
        {
            godfather = null;
        }
    }

    public static class Mafioso
    {
        public static PlayerControl mafioso;
        public static Color color = Palette.ImpostorRed;
        public static bool canSabotage => canKill || CustomOptionHolder.mafiosoCanSabotage.getBool();
        public static bool canRepair => canKill || CustomOptionHolder.mafiosoCanRepair.getBool();
        public static bool canVent => canKill || CustomOptionHolder.mafiosoCanVent.getBool();
        public static bool canKill => Godfather.godfather == null || Godfather.godfather.isDead();

        public static void clearAndReload()
        {
            mafioso = null;
        }
    }


    public static class Janitor
    {
        public static PlayerControl janitor;
        public static Color color = Palette.ImpostorRed;

        private static Sprite buttonSprite;

        public static float cooldown => CustomOptionHolder.janitorCooldown.getFloat();
        public static bool canSabotage => CustomOptionHolder.janitorCanSabotage.getBool();
        public static bool canRepair => CustomOptionHolder.janitorCanRepair.getBool();
        public static bool canVent => CustomOptionHolder.janitorCanVent.getBool();

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CleanButton.png", 115f);
            return buttonSprite;
        }

        public static void clearAndReload()
        {
            janitor = null;
        }
    }

    public static class Detective
    {
        public static PlayerControl detective;
        public static Color color = new Color32(45, 106, 165, byte.MaxValue);

        public static float footprintInterval = 1f;
        public static float footprintDuration = 1f;
        public static bool anonymousFootprints;
        public static float reportNameDuration;
        public static float reportColorDuration = 20f;
        public static float timer = 6.2f;

        public static void clearAndReload()
        {
            detective = null;
            anonymousFootprints = CustomOptionHolder.detectiveAnonymousFootprints.getBool();
            footprintInterval = CustomOptionHolder.detectiveFootprintInterval.getFloat();
            footprintDuration = CustomOptionHolder.detectiveFootprintDuration.getFloat();
            reportNameDuration = CustomOptionHolder.detectiveReportNameDuration.getFloat();
            reportColorDuration = CustomOptionHolder.detectiveReportColorDuration.getFloat();
            timer = 6.2f;
        }
    }

    public static class TimeMaster
    {
        public static PlayerControl timeMaster;
        public static Color color = new Color32(112, 142, 239, byte.MaxValue);

        public static bool reviveDuringRewind = false;
        public static float rewindTime = 3f;
        public static float shieldDuration = 3f;
        public static float cooldown = 30f;

        public static bool shieldActive;
        public static bool isRewinding;

        private static Sprite buttonSprite;

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TimeShieldButton.png", 115f);
            return buttonSprite;
        }

        public static void clearAndReload()
        {
            timeMaster = null;
            isRewinding = false;
            shieldActive = false;
            rewindTime = CustomOptionHolder.timeMasterRewindTime.getFloat();
            shieldDuration = CustomOptionHolder.timeMasterShieldDuration.getFloat();
            cooldown = CustomOptionHolder.timeMasterCooldown.getFloat();
        }
    }

    public static class Medic
    {
        public static PlayerControl medic;
        public static PlayerControl shielded;
        public static PlayerControl futureShielded;

        public static Color color = new Color32(126, 251, 194, byte.MaxValue);
        public static bool usedShield;

        public static int showShielded;
        public static bool showAttemptToShielded;
        public static bool showAttemptToMedic;
        public static bool setShieldAfterMeeting;

        public static Color shieldedColor = new Color32(0, 221, 255, byte.MaxValue);
        public static PlayerControl currentTarget;

        private static Sprite buttonSprite;

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.ShieldButton.png", 115f);
            return buttonSprite;
        }

        public static void clearAndReload()
        {
            medic = null;
            shielded = null;
            futureShielded = null;
            currentTarget = null;
            usedShield = false;
            showShielded = CustomOptionHolder.medicShowShielded.getSelection();
            showAttemptToShielded = CustomOptionHolder.medicShowAttemptToShielded.getBool();
            showAttemptToMedic = CustomOptionHolder.medicShowAttemptToMedic.getBool();
            setShieldAfterMeeting = CustomOptionHolder.medicSetShieldAfterMeeting.getBool();
        }
    }

    public static class Seer
    {
        public static PlayerControl seer;
        public static Color color = new Color32(97, 178, 108, byte.MaxValue);
        public static List<Vector3> deadBodyPositions = new();

        public static float soulDuration = 15f;
        public static bool limitSoulDuration;
        public static int mode;

        private static Sprite soulSprite;

        public static Sprite getSoulSprite()
        {
            if (soulSprite) return soulSprite;
            soulSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Soul.png", 500f);
            return soulSprite;
        }

        public static void clearAndReload()
        {
            seer = null;
            deadBodyPositions = new List<Vector3>();
            limitSoulDuration = CustomOptionHolder.seerLimitSoulDuration.getBool();
            soulDuration = CustomOptionHolder.seerSoulDuration.getFloat();
            mode = CustomOptionHolder.seerMode.getSelection();
        }
    }

    public static class EvilHacker
    {
        public static PlayerControl evilHacker;
        public static Color color = Palette.ImpostorRed;
        public static bool canCreateMadmate;
        public static PlayerControl fakeMadmate;
        public static PlayerControl currentTarget;

        private static Sprite buttonSprite;
        private static Sprite madmateButtonSprite;

        public static bool canHasBetterAdmin => CustomOptionHolder.evilHackerCanHasBetterAdmin.getBool();
        public static bool canCreateMadmateFromFox => CustomOptionHolder.evilHackerCanCreateMadmateFromFox.getBool();

        public static bool canCreateMadmateFromJackal =>
            CustomOptionHolder.evilHackerCanCreateMadmateFromJackal.getBool();

        public static bool canInheritAbility => CustomOptionHolder.evilHackerCanInheritAbility.getBool();
        public static bool canSeeDoorStatus => CustomOptionHolder.evilHackerCanSeeDoorStatus.getBool();

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            byte mapId = GameOptionsManager.Instance.currentNormalGameOptions.MapId;
            UseButtonSettings button =
                FastDestroyableSingleton<HudManager>.Instance.UseButton
                    .fastUseSettings[ImageNames.PolusAdminButton]; // Polus
            if (Helpers.isSkeld() || mapId == 3)
                button =
                    FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings
                        [ImageNames.AdminMapButton]; // Skeld || Dleks
            else if (Helpers.isMira())
                button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[
                    ImageNames.MIRAAdminButton]; // Mira HQ
            else if (Helpers.isAirship())
                button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[
                    ImageNames.AirshipAdminButton]; // Airship
            else if (Helpers.isFungle())
                button =
                    FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.AdminMapButton];
            buttonSprite = button.Image;
            return buttonSprite;
        }

        public static Sprite getMadmateButtonSprite()
        {
            if (madmateButtonSprite) return madmateButtonSprite;
            madmateButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.SidekickButton.png", 115f);
            return madmateButtonSprite;
        }

        public static bool isInherited()
        {
            return canInheritAbility && evilHacker != null && evilHacker.isDead() &&
                   PlayerControl.LocalPlayer.isImpostor();
        }

        public static void clearAndReload()
        {
            evilHacker = null;
            currentTarget = null;
            fakeMadmate = null;
            canCreateMadmate = CustomOptionHolder.evilHackerCanCreateMadmate.getBool();
        }
    }

    public static class Hacker
    {
        public static PlayerControl hacker;
        public static Minigame vitals;
        public static Minigame doorLog;
        public static Color color = new Color32(117, 250, 76, byte.MaxValue);

        public static float cooldown = 30f;
        public static float duration = 10f;
        public static float toolsNumber = 5f;
        public static bool onlyColorType;
        public static float hackerTimer;
        public static int rechargeTasksNumber = 2;
        public static int rechargedTasks = 2;
        public static int chargesVitals = 1;
        public static int chargesAdminTable = 1;
        public static bool cantMove = true;

        private static Sprite buttonSprite;
        private static Sprite vitalsSprite;
        private static Sprite logSprite;
        private static Sprite adminSprite;

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.HackerButton.png", 115f);
            return buttonSprite;
        }

        public static Sprite getVitalsSprite()
        {
            if (vitalsSprite) return vitalsSprite;
            vitalsSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton
                .fastUseSettings[ImageNames.VitalsButton].Image;
            return vitalsSprite;
        }

        public static Sprite getLogSprite()
        {
            if (logSprite) return logSprite;
            logSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton
                .fastUseSettings[ImageNames.DoorLogsButton].Image;
            return logSprite;
        }

        public static Sprite getAdminSprite()
        {
            byte mapId = GameOptionsManager.Instance.currentNormalGameOptions.MapId;
            UseButtonSettings button =
                FastDestroyableSingleton<HudManager>.Instance.UseButton
                    .fastUseSettings[ImageNames.PolusAdminButton]; // Polus
            if (mapId is 0 or 3)
                button =
                    FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings
                        [ImageNames.AdminMapButton]; // Skeld || Dleks
            else if (mapId == 1)
                button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[
                    ImageNames.MIRAAdminButton]; // Mira HQ
            else if (mapId == 4)
                button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[
                    ImageNames.AirshipAdminButton]; // Airship
            adminSprite = button.Image;
            return adminSprite;
        }

        public static void clearAndReload()
        {
            hacker = null;
            vitals = null;
            doorLog = null;
            hackerTimer = 0f;
            adminSprite = null;
            cooldown = CustomOptionHolder.hackerCooldown.getFloat();
            duration = CustomOptionHolder.hackerHackeringDuration.getFloat();
            onlyColorType = CustomOptionHolder.hackerOnlyColorType.getBool();
            toolsNumber = CustomOptionHolder.hackerToolsNumber.getFloat();
            rechargeTasksNumber = Mathf.RoundToInt(CustomOptionHolder.hackerRechargeTasksNumber.getFloat());
            rechargedTasks = Mathf.RoundToInt(CustomOptionHolder.hackerRechargeTasksNumber.getFloat());
            chargesVitals = Mathf.RoundToInt(CustomOptionHolder.hackerToolsNumber.getFloat()) / 2;
            chargesAdminTable = Mathf.RoundToInt(CustomOptionHolder.hackerToolsNumber.getFloat()) / 2;
            cantMove = CustomOptionHolder.hackerNoMove.getBool();
        }
    }

    public static class Tracker
    {
        public static PlayerControl tracker;
        public static Color color = new Color32(100, 58, 220, byte.MaxValue);
        public static List<Arrow> localArrows = new();

        public static float updateInterval = 5f;
        public static bool resetTargetAfterMeeting;
        public static bool canTrackCorpses;
        public static float corpsesTrackingCooldown = 30f;
        public static float corpsesTrackingDuration = 5f;
        public static float corpsesTrackingTimer;
        public static List<Vector3> deadBodyPositions = new();

        public static PlayerControl currentTarget;
        public static PlayerControl tracked;
        public static bool usedTracker;
        public static float timeUntilUpdate;
        public static Arrow arrow = new(Color.blue);

        private static Sprite trackCorpsesButtonSprite;

        private static Sprite buttonSprite;

        public static Sprite getTrackCorpsesButtonSprite()
        {
            if (trackCorpsesButtonSprite) return trackCorpsesButtonSprite;
            trackCorpsesButtonSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.PathfindButton.png", 115f);
            return trackCorpsesButtonSprite;
        }

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TrackerButton.png", 115f);
            return buttonSprite;
        }

        public static void resetTracked()
        {
            currentTarget = tracked = null;
            usedTracker = false;
            if (arrow?.arrow != null) Object.Destroy(arrow.arrow);
            arrow = new Arrow(Color.blue);
            if (arrow.arrow != null) arrow.arrow.SetActive(false);
        }

        public static void clearAndReload()
        {
            tracker = null;
            resetTracked();
            timeUntilUpdate = 0f;
            updateInterval = CustomOptionHolder.trackerUpdateInterval.getFloat();
            resetTargetAfterMeeting = CustomOptionHolder.trackerResetTargetAfterMeeting.getBool();
            if (localArrows != null)
                foreach (Arrow arrow in localArrows)
                    if (arrow?.arrow != null)
                        Object.Destroy(arrow.arrow);
            deadBodyPositions = new List<Vector3>();
            corpsesTrackingTimer = 0f;
            corpsesTrackingCooldown = CustomOptionHolder.trackerCorpsesTrackingCooldown.getFloat();
            corpsesTrackingDuration = CustomOptionHolder.trackerCorpsesTrackingDuration.getFloat();
            canTrackCorpses = CustomOptionHolder.trackerCanTrackCorpses.getBool();
        }
    }

    public static class Vampire
    {
        public static PlayerControl vampire;
        public static Color color = Palette.ImpostorRed;

        public static float delay = 10f;
        public static float cooldown = 30f;
        public static bool canKillNearGarlics = true;
        public static bool localPlacedGarlic;
        public static bool garlicsActive = true;

        public static PlayerControl currentTarget;
        public static PlayerControl bitten;
        public static bool targetNearGarlic;

        private static Sprite buttonSprite;

        private static Sprite garlicButtonSprite;

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.VampireButton.png", 115f);
            return buttonSprite;
        }

        public static Sprite getGarlicButtonSprite()
        {
            if (garlicButtonSprite) return garlicButtonSprite;
            garlicButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.GarlicButton.png", 115f);
            return garlicButtonSprite;
        }

        public static void clearAndReload()
        {
            vampire = null;
            bitten = null;
            targetNearGarlic = false;
            localPlacedGarlic = false;
            currentTarget = null;
            garlicsActive = CustomOptionHolder.vampireSpawnRate.getSelection() > 0;
            delay = CustomOptionHolder.vampireKillDelay.getFloat();
            cooldown = CustomOptionHolder.vampireCooldown.getFloat();
            canKillNearGarlics = CustomOptionHolder.vampireCanKillNearGarlics.getBool();
        }
    }

    public static class Snitch
    {
        public static PlayerControl snitch;
        public static Color color = new Color32(184, 251, 79, byte.MaxValue);

        public static List<Arrow> localArrows = new();
        public static int taskCountForReveal = 1;
        public static bool includeTeamJackal;
        public static bool includePelican;
        public static bool useDifferentArrowColor;


        public static void clearAndReload()
        {
            if (localArrows != null)
                foreach (Arrow arrow in localArrows)
                    if (arrow?.arrow != null)
                        Object.Destroy(arrow.arrow);
            localArrows = new List<Arrow>();
            taskCountForReveal = Mathf.RoundToInt(CustomOptionHolder.snitchLeftTasksForReveal.getFloat());
            includeTeamJackal = CustomOptionHolder.snitchIncludeTeamJackal.getBool();
            includePelican = CustomOptionHolder.snitchIncludePelican.getBool();
            useDifferentArrowColor = CustomOptionHolder.snitchUseDifferentArrowColor.getBool();
            snitch = null;
        }
    }

    public static class Jackal
    {
        public static PlayerControl jackal;
        public static Color color = new Color32(0, 180, 235, byte.MaxValue);
        public static PlayerControl fakeSidekick;
        public static PlayerControl currentTarget;
        public static List<PlayerControl> formerJackals = new();

        public static float cooldown = 30f;
        public static float createSidekickCooldown = 30f;
        public static bool canUseVents = true;
        public static bool canCreateSidekick = true;
        public static Sprite buttonSprite;
        public static bool jackalPromotedFromSidekickCanCreateSidekick = true;
        public static bool canCreateSidekickFromImpostor = true;
        public static bool canCreateSidekickFromFox = true;
        public static bool hasImpostorVision;
        public static bool wasTeamRed;
        public static bool wasImpostor;
        public static bool wasSpy;

        public static Sprite getSidekickButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.SidekickButton.png", 115f);
            return buttonSprite;
        }

        public static void removeCurrentJackal()
        {
            if (!formerJackals.Any(x => x.PlayerId == jackal.PlayerId)) formerJackals.Add(jackal);
            jackal = null;
            currentTarget = null;
            fakeSidekick = null;
            cooldown = CustomOptionHolder.jackalKillCooldown.getFloat();
            createSidekickCooldown = CustomOptionHolder.jackalCreateSidekickCooldown.getFloat();
        }

        public static void clearAndReload()
        {
            jackal = null;
            currentTarget = null;
            fakeSidekick = null;
            cooldown = CustomOptionHolder.jackalKillCooldown.getFloat();
            createSidekickCooldown = CustomOptionHolder.jackalCreateSidekickCooldown.getFloat();
            canUseVents = CustomOptionHolder.jackalCanUseVents.getBool();
            canCreateSidekick = CustomOptionHolder.jackalCanCreateSidekick.getBool();
            jackalPromotedFromSidekickCanCreateSidekick =
                CustomOptionHolder.jackalPromotedFromSidekickCanCreateSidekick.getBool();
            canCreateSidekickFromImpostor = CustomOptionHolder.jackalCanCreateSidekickFromImpostor.getBool();
            canCreateSidekickFromFox = CustomOptionHolder.jackalCanCreateSidekickFromFox.getBool();
            formerJackals.Clear();
            hasImpostorVision = CustomOptionHolder.jackalAndSidekickHaveImpostorVision.getBool();
            wasTeamRed = wasImpostor = wasSpy = false;
        }
    }

    public static class Sidekick
    {
        public static PlayerControl sidekick;
        public static Color color = new Color32(0, 180, 235, byte.MaxValue);

        public static PlayerControl currentTarget;

        public static float cooldown = 30f;
        public static bool canUseVents = true;
        public static bool canKill = true;
        public static bool promotesToJackal = true;
        public static bool hasImpostorVision;
        public static bool wasTeamRed;
        public static bool wasImpostor;
        public static bool wasSpy;

        public static void clearAndReload()
        {
            sidekick = null;
            currentTarget = null;
            cooldown = CustomOptionHolder.jackalKillCooldown.getFloat();
            canUseVents = CustomOptionHolder.sidekickCanUseVents.getBool();
            canKill = CustomOptionHolder.sidekickCanKill.getBool();
            promotesToJackal = CustomOptionHolder.sidekickPromotesToJackal.getBool();
            hasImpostorVision = CustomOptionHolder.jackalAndSidekickHaveImpostorVision.getBool();
            wasTeamRed = wasImpostor = wasSpy = false;
        }
    }

    public static class Eraser
    {
        public static PlayerControl eraser;
        public static Color color = Palette.ImpostorRed;

        public static List<PlayerControl> futureErased = new();
        public static PlayerControl currentTarget;
        public static float cooldown = 30f;
        public static float cooldownIncrease = 10f;
        public static bool canEraseAnyone;

        private static Sprite buttonSprite;

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.EraserButton.png", 115f);
            return buttonSprite;
        }

        public static void clearAndReload()
        {
            eraser = null;
            futureErased = new List<PlayerControl>();
            currentTarget = null;
            cooldown = CustomOptionHolder.eraserCooldown.getFloat();
            cooldownIncrease = CustomOptionHolder.eraserCooldownIncrease.getFloat();
            canEraseAnyone = CustomOptionHolder.eraserCanEraseAnyone.getBool();
        }
    }

    public static class Spy
    {
        public static PlayerControl spy;
        public static Color color = Palette.ImpostorRed;

        public static bool impostorsCanKillAnyone = true;
        public static bool canEnterVents;
        public static bool hasImpostorVision;

        public static void clearAndReload()
        {
            spy = null;
            impostorsCanKillAnyone = CustomOptionHolder.spyImpostorsCanKillAnyone.getBool();
            canEnterVents = CustomOptionHolder.spyCanEnterVents.getBool();
            hasImpostorVision = CustomOptionHolder.spyHasImpostorVision.getBool();
        }
    }

    public static class Trickster
    {
        public static PlayerControl trickster;
        public static Color color = Palette.ImpostorRed;
        public static float placeBoxCooldown = 30f;
        public static float lightsOutCooldown = 30f;
        public static float lightsOutDuration = 10f;
        public static float lightsOutTimer;

        private static Sprite placeBoxButtonSprite;
        private static Sprite lightOutButtonSprite;
        private static Sprite tricksterVentButtonSprite;

        public static Sprite getPlaceBoxButtonSprite()
        {
            if (placeBoxButtonSprite) return placeBoxButtonSprite;
            placeBoxButtonSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.PlaceJackInTheBoxButton.png", 115f);
            return placeBoxButtonSprite;
        }

        public static Sprite getLightsOutButtonSprite()
        {
            if (lightOutButtonSprite) return lightOutButtonSprite;
            lightOutButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.LightsOutButton.png", 115f);
            return lightOutButtonSprite;
        }

        public static Sprite getTricksterVentButtonSprite()
        {
            if (tricksterVentButtonSprite) return tricksterVentButtonSprite;
            tricksterVentButtonSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TricksterVentButton.png", 115f);
            return tricksterVentButtonSprite;
        }

        public static void clearAndReload()
        {
            trickster = null;
            lightsOutTimer = 0f;
            placeBoxCooldown = CustomOptionHolder.tricksterPlaceBoxCooldown.getFloat();
            lightsOutCooldown = CustomOptionHolder.tricksterLightsOutCooldown.getFloat();
            lightsOutDuration = CustomOptionHolder.tricksterLightsOutDuration.getFloat();
            JackInTheBox
                .UpdateStates(); // if the role is erased, we might have to update the state of the created objects
        }
    }

    public static class Cleaner
    {
        public static PlayerControl cleaner;
        public static Color color = Palette.ImpostorRed;

        public static float cooldown = 30f;

        private static Sprite buttonSprite;

        public static Sprite getButtonSprite()
        {
            if (buttonSprite) return buttonSprite;
            buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CleanButton.png", 115f);
            return buttonSprite;
        }

        public static void clearAndReload()
        {
            cleaner = null;
            cooldown = CustomOptionHolder.cleanerCooldown.getFloat();
        }
    }

    public static class Warlock
    {
        public static PlayerControl warlock;
        public static Color color = Palette.ImpostorRed;

        public static PlayerControl currentTarget;
        public static PlayerControl curseVictim;
        public static PlayerControl curseVictimTarget;

        public static float cooldown = 30f;
        public static float rootTime = 5f;

        private static Sprite curseButtonSprite;
        private static Sprite curseKillButtonSprite;

        public static Sprite getCurseButtonSprite()
        {
            if (curseButtonSprite) return curseButtonSprite;
            curseButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CurseButton.png", 115f);
            return curseButtonSprite;
        }

        public static Sprite getCurseKillButtonSprite()
        {
            if (curseKillButtonSprite) return curseKillButtonSprite;
            curseKillButtonSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CurseKillButton.png.png", 115f);
            return curseKillButtonSprite;
        }

        public static void clearAndReload()
        {
            warlock = null;
            currentTarget = null;
            curseVictim = null;
            curseVictimTarget = null;
            cooldown = CustomOptionHolder.warlockCooldown.getFloat();
            rootTime = CustomOptionHolder.warlockRootTime.getFloat();
        }

        public static void resetCurse()
        {
            HudManagerStartPatch.warlockCurseButton.Timer = HudManagerStartPatch.warlockCurseButton.MaxTimer;
            HudManagerStartPatch.warlockCurseButton.Sprite = getCurseButtonSprite();
            HudManagerStartPatch.warlockCurseButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
            currentTarget = null;
            curseVictim = null;
            curseVictimTarget = null;
        }
    }

    public static class SecurityGuard
    {
        public static PlayerControl securityGuard;
        public static Color color = new Color32(195, 178, 95, byte.MaxValue);

        public static float cooldown = 30f;
        public static int remainingScrews = 7;
        public static int totalScrews = 7;
        public static int ventPrice = 1;
        public static int camPrice = 2;
        public static int placedCameras;
        public static float duration = 10f;
        public static int maxCharges = 5;
        public static int rechargeTasksNumber = 3;
        public static int rechargedTasks = 3;
        public static int charges = 1;
        public static bool cantMove = true;
        public static Vent ventTarget;
        public static Minigame minigame;

        private static Sprite closeVentButtonSprite;

        private static Sprite placeCameraButtonSprite;

        private static Sprite animatedVentSealedSprite;
        private static float lastPPU;

        private static Sprite staticVentSealedSprite;
        private static Sprite submergedCentralUpperVentSealedSprite;

        private static Sprite submergedCentralLowerVentSealedSprite;

        private static Sprite camSprite;

        private static Sprite logSprite;

        private static Sprite fungleVentSealedSprite;

        public static Sprite getCloseVentButtonSprite()
        {
            if (closeVentButtonSprite) return closeVentButtonSprite;
            closeVentButtonSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CloseVentButton.png", 115f);
            return closeVentButtonSprite;
        }

        public static Sprite getPlaceCameraButtonSprite()
        {
            if (placeCameraButtonSprite) return placeCameraButtonSprite;
            placeCameraButtonSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.PlaceCameraButton.png", 115f);
            return placeCameraButtonSprite;
        }

        public static Sprite getAnimatedVentSealedSprite()
        {
            float ppu = 185f;
            if (SubmergedCompatibility.isSubmerged()) ppu = 120f;
            if (lastPPU != ppu)
            {
                animatedVentSealedSprite = null;
                lastPPU = ppu;
            }

            if (animatedVentSealedSprite) return animatedVentSealedSprite;
            animatedVentSealedSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.AnimatedVentSealed.png", ppu);
            return animatedVentSealedSprite;
        }

        public static Sprite getStaticVentSealedSprite()
        {
            if (staticVentSealedSprite) return staticVentSealedSprite;
            staticVentSealedSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.StaticVentSealed.png", 160f);
            return staticVentSealedSprite;
        }

        public static Sprite getFungleVentSealedSprite()
        {
            if (fungleVentSealedSprite) return fungleVentSealedSprite;
            fungleVentSealedSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.FungleVentSealed.png", 160f);
            return fungleVentSealedSprite;
        }

        public static Sprite getSubmergedCentralUpperSealedSprite()
        {
            if (submergedCentralUpperVentSealedSprite) return submergedCentralUpperVentSealedSprite;
            submergedCentralUpperVentSealedSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CentralUpperBlocked.png", 145f);
            return submergedCentralUpperVentSealedSprite;
        }

        public static Sprite getSubmergedCentralLowerSealedSprite()
        {
            if (submergedCentralLowerVentSealedSprite) return submergedCentralLowerVentSealedSprite;
            submergedCentralLowerVentSealedSprite =
                Helpers.loadSpriteFromResources("TheOtherRoles.Resources.CentralLowerBlocked.png", 145f);
            return submergedCentralLowerVentSealedSprite;
        }

        public static Sprite getCamSprite()
        {
            if (camSprite) return camSprite;
            camSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.CamsButton]
                .Image;
            return camSprite;
        }

        public static Sprite getLogSprite()
        {
            if (logSprite) return logSprite;
            logSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton
                .fastUseSettings[ImageNames.DoorLogsButton].Image;
            return logSprite;
        }

        public static void clearAndReload()
        {
            securityGuard = null;
            ventTarget = null;
            minigame = null;
            duration = CustomOptionHolder.securityGuardCamDuration.getFloat();
            maxCharges = Mathf.RoundToInt(CustomOptionHolder.securityGuardCamMaxCharges.getFloat());
            rechargeTasksNumber = Mathf.RoundToInt(CustomOptionHolder.securityGuardCamRechargeTasksNumber.getFloat());
            rechargedTasks = Mathf.RoundToInt(CustomOptionHolder.securityGuardCamRechargeTasksNumber.getFloat());
            charges = Mathf.RoundToInt(CustomOptionHolder.securityGuardCamMaxCharges.getFloat()) / 2;
            placedCameras = 0;
            cooldown = CustomOptionHolder.securityGuardCooldown.getFloat();
            totalScrews = remainingScrews = Mathf.RoundToInt(CustomOptionHolder.securityGuardTotalScrews.getFloat());
            camPrice = Mathf.RoundToInt(CustomOptionHolder.securityGuardCamPrice.getFloat());
            ventPrice = Mathf.RoundToInt(CustomOptionHolder.securityGuardVentPrice.getFloat());
            cantMove = CustomOptionHolder.securityGuardNoMove.getBool();
        }
    }

    public static class Arsonist
    {
        public static PlayerControl arsonist;
        public static Color color = new Color32(238, 112, 46, byte.MaxValue);

        public static float cooldown = 30f;
        public static float duration = 3f;
        public static bool triggerArsonistWin;
        public static bool dousedEveryone;

        public static PlayerControl currentTarget;
        public static PlayerControl douseTarget;
        public static List<PlayerControl> dousedPlayers = new();

        private static Sprite douseSprite;

        private static Sprite igniteSprite;

        public static Sprite getDouseSprite()
        {
            if (douseSprite) return douseSprite;
            douseSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.DouseButton.png", 115f);
            return douseSprite;
        }

        public static Sprite getIgniteSprite()
        {
            if (igniteSprite) return igniteSprite;
            igniteSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.IgniteButton.png", 115f);
            return igniteSprite;
        }

        public static bool dousedEveryoneAlive()
        {
            return PlayerControl.AllPlayerControls.ToArray().All(x =>
            {
                return x == arsonist || x.Data.IsDead || x.Data.Disconnected || x.isGM() ||
                       dousedPlayers.Any(y => y.PlayerId == x.PlayerId);
            });
        }

        public static void updateStatus()
        {
            if (arsonist != null && arsonist == PlayerControl.LocalPlayer)
                dousedEveryone = dousedEveryoneAlive();
        }

        public static void updateIcons()
        {
            foreach (PoolablePlayer pp in TORMapOptions.playerIcons.Values) pp.gameObject.SetActive(false);

            if (arsonist != null && arsonist == PlayerControl.LocalPlayer)
            {
                int visibleCounter = 0;

                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    if (!TORMapOptions.playerIcons.ContainsKey(p.PlayerId)) continue;

                    if (p.Data.IsDead || p.Data.Disconnected)
                        TORMapOptions.playerIcons[p.PlayerId].gameObject.SetActive(false);
                    else
                    {
                        TORMapOptions.playerIcons[p.PlayerId].gameObject.SetActive(true);
                        TORMapOptions.playerIcons[p.PlayerId].transform.localPosition =
                            IntroCutsceneOnDestroyPatch.bottomLeft + new Vector3(0f, -0.35f, -62f);
                        TORMapOptions.playerIcons[p.PlayerId].transform.localScale = Vector3.one * 0.4f;
                        visibleCounter++;
                    }

                    bool isDoused = dousedPlayers.Any(x => x.PlayerId == p.PlayerId);
                    TORMapOptions.playerIcons[p.PlayerId].setSemiTransparent(!isDoused);
                }
            }
        }

        public static void clearAndReload()
        {
            arsonist = null;
            currentTarget = null;
            douseTarget = null;
            triggerArsonistWin = false;
            dousedEveryone = false;
            dousedPlayers = new List<PlayerControl>();
            foreach (PoolablePlayer p in TORMapOptions.playerIcons.Values)
                if (p != null && p.gameObject != null)
                    p.gameObject.SetActive(false);
            cooldown = CustomOptionHolder.arsonistCooldown.getFloat();
            duration = CustomOptionHolder.arsonistDuration.getFloat();
        }
    }

    public static class Guesser
    {
        public static PlayerControl niceGuesser;
        public static PlayerControl evilGuesser;
        public static Color color = new Color32(255, 255, 0, byte.MaxValue);
        private static Sprite targetSprite;

        public static int remainingShotsEvilGuesser = 2;
        public static int remainingShotsNiceGuesser = 2;
        public static bool onlyAvailableRoles = true;
        public static bool hasMultipleShotsPerMeeting;
        public static bool showInfoInGhostChat = true;
        public static bool killsThroughShield = true;
        public static bool evilGuesserCanGuessSpy = true;
        public static bool guesserCantGuessSnitch;

        public static Sprite getTargetSprite()
        {
            if (targetSprite) return targetSprite;
            targetSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TargetIcon.png", 150f);
            return targetSprite;
        }

        public static bool isGuesser(byte playerId)
        {
            if ((niceGuesser != null && niceGuesser.PlayerId == playerId) ||
                (evilGuesser != null && evilGuesser.PlayerId == playerId)) return true;
            return false;
        }

        public static void clear(byte playerId)
        {
            if (niceGuesser != null && niceGuesser.PlayerId == playerId) niceGuesser = null;
            else if (evilGuesser != null && evilGuesser.PlayerId == playerId) evilGuesser = null;
        }

        public static int remainingShots(PlayerControl player, bool shoot = false)
        {
            int remainingShots = 0;
            if (niceGuesser == player)
            {
                remainingShots = remainingShotsNiceGuesser;
                if (shoot) remainingShotsNiceGuesser = Mathf.Max(0, remainingShotsNiceGuesser - 1);
            }
            else if (evilGuesser == player)
            {
                remainingShots = remainingShotsEvilGuesser;
                if (player.hasModifier(ModifierType.LastImpostor) && LastImpostor.canGuess())
                    remainingShots += LastImpostor.remainingShots;
                if (shoot)
                {
                    // ラストインポスターの弾数を優先的に消費させる
                    if (player.hasModifier(ModifierType.LastImpostor) && LastImpostor.canGuess())
                        LastImpostor.remainingShots = Mathf.Max(0, LastImpostor.remainingShots - 1);
                    else
                        remainingShotsEvilGuesser = Mathf.Max(0, remainingShotsEvilGuesser - 1);
                }
            }
            else if (player.hasModifier(ModifierType.LastImpostor) && LastImpostor.canGuess())
            {
                remainingShots = LastImpostor.remainingShots;
                if (shoot) LastImpostor.remainingShots = Mathf.Max(0, LastImpostor.remainingShots - 1);
            }

            return remainingShots;
        }

        public static void clearAndReload()
        {
            niceGuesser = null;
            evilGuesser = null;
            guesserCantGuessSnitch = CustomOptionHolder.guesserCantGuessSnitchIfTaskDone.getBool();
            remainingShotsEvilGuesser = Mathf.RoundToInt(CustomOptionHolder.guesserNumberOfShots.getFloat());
            remainingShotsNiceGuesser = Mathf.RoundToInt(CustomOptionHolder.guesserNumberOfShots.getFloat());
            onlyAvailableRoles = CustomOptionHolder.guesserOnlyAvailableRoles.getBool();
            hasMultipleShotsPerMeeting = CustomOptionHolder.guesserHasMultipleShotsPerMeeting.getBool();
            showInfoInGhostChat = CustomOptionHolder.guesserShowInfoInGhostChat.getBool();
            killsThroughShield = CustomOptionHolder.guesserKillsThroughShield.getBool();
            evilGuesserCanGuessSpy = CustomOptionHolder.guesserEvilCanKillSpy.getBool();
        }
    }

    public static class BountyHunter
    {
        public static PlayerControl bountyHunter;
        public static Color color = Palette.ImpostorRed;

        public static Arrow arrow;
        public static float bountyDuration = 30f;
        public static bool showArrow = true;
        public static float bountyKillCooldown;
        public static float punishmentTime = 15f;
        public static float arrowUpdateInterval = 10f;

        public static float arrowUpdateTimer;
        public static float bountyUpdateTimer;
        public static PlayerControl bounty;
        public static TextMeshPro cooldownText;

        public static void clearAndReload()
        {
            arrow = new Arrow(color);
            bountyHunter = null;
            bounty = null;
            arrowUpdateTimer = 0f;
            bountyUpdateTimer = 0f;
            if (arrow != null && arrow.arrow != null) Object.Destroy(arrow.arrow);
            arrow = null;
            if (cooldownText != null && cooldownText.gameObject != null) Object.Destroy(cooldownText.gameObject);
            cooldownText = null;
            foreach (PoolablePlayer p in TORMapOptions.playerIcons.Values)
                if (p != null && p.gameObject != null)
                    p.gameObject.SetActive(false);


            bountyDuration = CustomOptionHolder.bountyHunterBountyDuration.getFloat();
            bountyKillCooldown = CustomOptionHolder.bountyHunterReducedCooldown.getFloat();
            punishmentTime = CustomOptionHolder.bountyHunterPunishmentTime.getFloat();
            showArrow = CustomOptionHolder.bountyHunterShowArrow.getBool();
            arrowUpdateInterval = CustomOptionHolder.bountyHunterArrowUpdateInterval.getFloat();
        }
    }

    public static class Bait
    {
        public static PlayerControl bait;
        public static Color color = new Color32(0, 247, 255, byte.MaxValue);

        public static bool highlightAllVents;
        public static float reportDelay;
        public static bool showKillFlash = true;
        public static bool canBeGuessed = true;

        public static bool reported;

        public static void clearAndReload()
        {
            bait = null;
            reported = false;
            highlightAllVents = CustomOptionHolder.baitHighlightAllVents.getBool();
            reportDelay = CustomOptionHolder.baitReportDelay.getFloat();
            showKillFlash = CustomOptionHolder.baitShowKillFlash.getBool();
            canBeGuessed = CustomOptionHolder.baitCanBeGuessed.getBool();
        }
    }
}

public static class Vulture
{
    public static PlayerControl vulture;
    public static Color color = new Color32(139, 69, 19, byte.MaxValue);
    public static List<Arrow> localArrows = new();
    public static float cooldown = 30f;
    public static int vultureNumberToWin = 4;
    public static int eatenBodies;
    public static bool triggerVultureWin;
    public static bool canUseVents = true;
    public static bool showArrows = true;
    private static Sprite buttonSprite;

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.VultureButton.png", 115f);
        return buttonSprite;
    }

    public static void clearAndReload()
    {
        vulture = null;
        vultureNumberToWin = Mathf.RoundToInt(CustomOptionHolder.vultureNumberToWin.getFloat());
        eatenBodies = 0;
        cooldown = CustomOptionHolder.vultureCooldown.getFloat();
        triggerVultureWin = false;
        canUseVents = CustomOptionHolder.vultureCanUseVents.getBool();
        showArrows = CustomOptionHolder.vultureShowArrows.getBool();
        if (localArrows != null)
            foreach (Arrow arrow in localArrows)
                if (arrow?.arrow != null)
                    Object.Destroy(arrow.arrow);
        localArrows = new List<Arrow>();
    }
}

public static class Medium
{
    public static PlayerControl medium;
    public static DeadPlayer target;
    public static DeadPlayer soulTarget;
    public static Color color = new Color32(98, 120, 115, byte.MaxValue);
    public static List<Tuple<DeadPlayer, Vector3>> deadBodies = new();
    public static List<Tuple<DeadPlayer, Vector3>> featureDeadBodies = new();
    public static List<SpriteRenderer> souls = new();
    public static DateTime meetingStartTime = DateTime.UtcNow;

    public static float cooldown = 30f;
    public static float duration = 3f;
    public static bool oneTimeUse;

    private static Sprite soulSprite;

    private static Sprite question;

    public static Sprite getSoulSprite()
    {
        if (soulSprite) return soulSprite;
        soulSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Soul.png", 500f);
        return soulSprite;
    }

    public static Sprite getQuestionSprite()
    {
        if (question) return question;
        question = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.MediumButton.png", 115f);
        return question;
    }

    public static void clearAndReload()
    {
        medium = null;
        target = null;
        soulTarget = null;
        deadBodies = new List<Tuple<DeadPlayer, Vector3>>();
        featureDeadBodies = new List<Tuple<DeadPlayer, Vector3>>();
        souls = new List<SpriteRenderer>();
        meetingStartTime = DateTime.UtcNow;
        cooldown = CustomOptionHolder.mediumCooldown.getFloat();
        duration = CustomOptionHolder.mediumDuration.getFloat();
        oneTimeUse = CustomOptionHolder.mediumOneTimeUse.getBool();
    }
}

public static class Lawyer
{
    public static PlayerControl lawyer;
    public static PlayerControl target;
    public static Color color = new Color32(134, 153, 25, byte.MaxValue);
    public static Sprite targetSprite;
    public static bool triggerLawyerWin;
    public static int meetings;

    public static bool targetKnows;
    public static float vision = 1f;
    public static bool winsAfterMeetings;
    public static int neededMeetings = 4;
    public static bool lawyerKnowsRole;

    public static Sprite getTargetSprite()
    {
        if (targetSprite) return targetSprite;
        targetSprite = Helpers.loadSpriteFromResources("", 150f);
        return targetSprite;
    }

    public static void clearAndReload()
    {
        lawyer = null;
        target = null;
        triggerLawyerWin = false;
        meetings = 0;

        targetKnows = CustomOptionHolder.lawyerTargetKnows.getBool();
        winsAfterMeetings = CustomOptionHolder.lawyerWinsAfterMeetings.getBool();
        neededMeetings = Mathf.RoundToInt(CustomOptionHolder.lawyerNeededMeetings.getFloat());
        vision = CustomOptionHolder.lawyerVision.getFloat();
        lawyerKnowsRole = CustomOptionHolder.lawyerKnowsRole.getBool();
    }
}

public static class Pursuer
{
    public static PlayerControl pursuer;
    public static PlayerControl target;
    public static Color color = Lawyer.color;
    public static List<PlayerControl> blankedList = new();
    public static int blanks;
    public static Sprite blank;
    public static bool notAckedExiled;

    public static float cooldown = 30f;
    public static int blanksNumber = 5;

    public static Sprite getTargetSprite()
    {
        if (blank) return blank;
        blank = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.PursuerButton.png", 115f);
        return blank;
    }

    public static void clearAndReload()
    {
        pursuer = null;
        target = null;
        blankedList = new List<PlayerControl>();
        blanks = 0;
        notAckedExiled = false;

        cooldown = CustomOptionHolder.pursuerCooldown.getFloat();
        blanksNumber = Mathf.RoundToInt(CustomOptionHolder.pursuerBlanksNumber.getFloat());
    }
}

public static class Witch
{
    public static PlayerControl witch;
    public static Color color = Palette.ImpostorRed;

    public static List<PlayerControl> futureSpelled = new();
    public static PlayerControl currentTarget;
    public static PlayerControl spellCastingTarget;
    public static float cooldown = 30f;
    public static float spellCastingDuration = 2f;
    public static float cooldownAddition = 10f;
    public static float currentCooldownAddition;
    public static bool canSpellAnyone;
    public static bool triggerBothCooldowns = true;
    public static bool witchVoteSavesTargets = true;

    private static Sprite buttonSprite;

    private static Sprite spelledOverlaySprite;

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.SpellButton.png", 115f);
        return buttonSprite;
    }

    public static Sprite getSpelledOverlaySprite()
    {
        if (spelledOverlaySprite) return spelledOverlaySprite;
        spelledOverlaySprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.SpellButtonMeeting.png", 225f);
        return spelledOverlaySprite;
    }


    public static void clearAndReload()
    {
        witch = null;
        futureSpelled = new List<PlayerControl>();
        currentTarget = spellCastingTarget = null;
        cooldown = CustomOptionHolder.witchCooldown.getFloat();
        cooldownAddition = CustomOptionHolder.witchAdditionalCooldown.getFloat();
        currentCooldownAddition = CustomOptionHolder.witchCooldown.getFloat();
        canSpellAnyone = CustomOptionHolder.witchCanSpellAnyone.getBool();
        spellCastingDuration = CustomOptionHolder.witchSpellCastingDuration.getFloat();
        triggerBothCooldowns = CustomOptionHolder.witchTriggerBothCooldowns.getBool();
        witchVoteSavesTargets = CustomOptionHolder.witchVoteSavesTargets.getBool();
    }
}

public static class Assassin
{
    public static PlayerControl assassin;
    public static Color color = Palette.ImpostorRed;

    public static PlayerControl assassinMarked;
    public static PlayerControl currentTarget;
    public static float cooldown = 30f;
    public static float traceTime = 1f;
    public static bool knowsTargetLocation;

    private static Sprite markButtonSprite;
    private static Sprite killButtonSprite;
    public static Arrow arrow = new(Color.black);

    public static Sprite getMarkButtonSprite()
    {
        if (markButtonSprite) return markButtonSprite;
        markButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.AssassinMarkButton.png", 115f);
        return markButtonSprite;
    }

    public static Sprite getKillButtonSprite()
    {
        if (killButtonSprite) return killButtonSprite;
        killButtonSprite =
            Helpers.loadSpriteFromResources("TheOtherRoles.Resources.AssassinAssassinateButton.png", 115f);
        return killButtonSprite;
    }

    public static void clearAndReload()
    {
        assassin = null;
        currentTarget = assassinMarked = null;
        cooldown = CustomOptionHolder.assassinCooldown.getFloat();
        knowsTargetLocation = CustomOptionHolder.assassinKnowsTargetLocation.getBool();
        traceTime = CustomOptionHolder.assassinTraceTime.getFloat();

        if (arrow?.arrow != null) Object.Destroy(arrow.arrow);
        arrow = new Arrow(Color.black);
        if (arrow.arrow != null) arrow.arrow.SetActive(false);
    }
}
