using System;
using System.Collections.Generic;
using UnityEngine;
using static TheOtherRoles.TheOtherRolesGM;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace TheOtherRoles.Objects;

internal class Footprint
{
    private static readonly List<Footprint> footprints = new();
    private static Sprite sprite;
    private readonly Color color;
    private readonly GameObject footprint;
    private readonly PlayerControl owner;
    private readonly SpriteRenderer spriteRenderer;
    private bool anonymousFootprints;

    public Footprint(float footprintDuration, bool anonymousFootprints, PlayerControl player)
    {
        owner = player;
        this.anonymousFootprints = anonymousFootprints;
        if (anonymousFootprints)
            color = Palette.PlayerColors[6];
        else
            color = Palette.PlayerColors[player.Data.DefaultOutfit.ColorId];

        footprint = new GameObject("Footprint");
        Vector3 position = new(player.transform.position.x, player.transform.position.y,
            player.transform.position.z + 1f);
        footprint.transform.position = position;
        footprint.transform.localPosition = position;
        footprint.transform.SetParent(player.transform.parent);

        footprint.transform.Rotate(0.0f, 0.0f, Random.Range(0.0f, 360.0f));


        spriteRenderer = footprint.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = getFootprintSprite();
        spriteRenderer.color = color;

        footprint.SetActive(true);
        footprints.Add(this);

        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(footprintDuration,
            new Action<float>(p =>
            {
                Color c = color;
                if (!anonymousFootprints && owner != null)
                {
                    if (owner == Morphling.morphling && Morphling.morphTimer > 0 && Morphling.morphTarget?.Data != null)
                        c = Palette.ShadowColors[Morphling.morphTarget.Data.DefaultOutfit.ColorId];
                    else if (Camouflager.camouflageTimer > 0)
                        c = Palette.PlayerColors[6];
                }

                if (spriteRenderer) spriteRenderer.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(1 - p));

                if (p == 1f && footprint != null)
                {
                    Object.Destroy(footprint);
                    footprints.Remove(this);
                }
            })));
    }

    public static Sprite getFootprintSprite()
    {
        if (sprite) return sprite;
        sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Footprint.png", 600f);
        return sprite;
    }
}
