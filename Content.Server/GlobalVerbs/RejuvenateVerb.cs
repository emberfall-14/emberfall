﻿using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects;
using Robust.Server.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GlobalVerbs
{
    /// <summary>
    ///     Completely removes all damage from the DamageableComponent (heals the mob).
    /// </summary>
    [GlobalVerb]
    class RejuvenateVerb : GlobalVerb
    {
        public override string GetText(IEntity user, IEntity target) => "Rejuvenate";
        public override bool RequireInteractionRange => false;

        public override VerbVisibility GetVisibility(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();

            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (groupController.CanCommand(player.playerSession, "rejuvenate"))
                    return VerbVisibility.Visible;
            }
            return VerbVisibility.Invisible;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            var groupController = IoCManager.Resolve<IConGroupController>();
            if (user.TryGetComponent<IActorComponent>(out var player))
            {
                if (groupController.CanCommand(player.playerSession, "rejuvenate"))
                PerformRejuvenate(target);
            }

        }
        public static void PerformRejuvenate(IEntity target)
        {
            if (target.TryGetComponent(out DamageableComponent damage))
            {
                damage.HealAllDamage();
            }
            if (target.TryGetComponent(out HungerComponent hunger))
            {
                hunger.ResetFood();
            }
            if (target.TryGetComponent(out ThirstComponent thirst))
            {
                thirst.ResetThirst();
            }
        }
    }
}
