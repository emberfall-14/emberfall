﻿using Content.Server.Mining.Components;
using Content.Shared.Destructible;
using Content.Shared.Mining;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Mining;

/// <summary>
/// This handles creating ores when the entity is destroyed.
/// </summary>
public sealed class MiningSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OreVeinComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnDestruction(EntityUid uid, OreVeinComponent component, DestructionEventArgs args)
    {
        if (!_random.Prob(component.OreChance))
            return;

        string? currentOre = null;

        if (component.CurrentOre != null)
        {
            currentOre = component.CurrentOre;
        }
        else
        {
            string? weightedRandom = null;

            if (component.MappedTools != null)
            {
                var gatherer = args.Gatherer;

                foreach (var (toolTag, mappedWeightedRandom) in component.MappedTools)
                {
                    if (gatherer != null && _tagSystem.HasTag(gatherer.Value, toolTag) || gatherer == null && toolTag == "Hand" || toolTag == "All")
                    {
                        weightedRandom = mappedWeightedRandom;
                        break;
                    }
                }
            }

            if (weightedRandom == null && component.OreRarityPrototypeId != null)
            {
                weightedRandom = component.OreRarityPrototypeId;
            }

            if (weightedRandom != null)
            {
                currentOre = _proto.Index<WeightedRandomPrototype>(weightedRandom).Pick(_random);
            }
        }

        OrePrototype? proto = null;

        if (currentOre != null)
        {
            proto = _proto.Index<OrePrototype>(currentOre);
        }

        if (proto?.OreEntity == null)
            return;

        var coords = Transform(uid).Coordinates;
        var amountToSpawn = _random.Next(proto.MinOreYield, proto.MaxOreYield);

        for (var i = 0; i < amountToSpawn; i++)
        {
            Spawn(proto.OreEntity, coords.Offset(_random.NextVector2(component.Radius)));
        }
    }
}
