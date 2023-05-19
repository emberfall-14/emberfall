using System.Linq;
using Content.Server.Construction.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Construction.Components;
using Content.Shared.Exchanger;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using Content.Shared.Wires;

namespace Content.Server.Construction;

public sealed class PartExchangerSystem : EntitySystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PartExchangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PartExchangerComponent, ExchangerDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, PartExchangerComponent component, DoAfterEvent args)
    {
        component.AudioStream?.Stop();
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<ServerStorageComponent>(uid, out var storage) || storage.Storage == null)
            return; //the parts are stored in here

        var machineParts = new List<MachinePartComponent>();

        foreach (var ent in storage.Storage.ContainedEntities) //get parts in RPED
        {
            if (TryComp<MachinePartComponent>(ent, out var part))
                machineParts.Add(part);
        }

        TryExchangeMachineParts(args.Args.Target.Value, storage, machineParts);
        TryConstructMachineParts(args.Args.Target.Value, storage, machineParts);

        args.Handled = true;
    }

    private void TryExchangeMachineParts(EntityUid uid, ServerStorageComponent storage, List<MachinePartComponent> machineParts)
    {
        if (!TryComp<MachineComponent>(uid, out var machine) || storage.Storage == null)
            return;

        var board = machine.BoardContainer.ContainedEntities.FirstOrNull();

        if (board == null || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        foreach (var ent in new List<EntityUid>(machine.PartContainer.ContainedEntities)) //clone so don't modify during enumeration
        {
            if (TryComp<MachinePartComponent>(ent, out var part))
            {
                machineParts.Add(part);
                _container.RemoveEntity(uid, ent);
            }
        }

        //order by highest rating
        machineParts = machineParts.OrderByDescending(p => p.Rating).ToList();

        var updatedParts = new List<MachinePartComponent>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            var target = machineParts.Where(p => p.PartType == type).Take(amount);
            updatedParts.AddRange(target);
        }
        foreach (var part in updatedParts)
        {
            machine.PartContainer.Insert(part.Owner, EntityManager);
            machineParts.Remove(part);
        }

        //put the unused parts back into rped. (this also does the "swapping")
        foreach (var unused in machineParts)
        {
            storage.Storage.Insert(unused.Owner);
            _storage.Insert(uid, unused.Owner, null, false);
        }
        _construction.RefreshParts(uid, machine);
    }

    private void TryConstructMachineParts(EntityUid uid, ServerStorageComponent storage, List<MachinePartComponent> machineParts)
    {
        if (!TryComp<MachineFrameComponent>(uid, out var machine) || storage.Storage == null)
            return;

        var board = machine.BoardContainer.ContainedEntities.FirstOrNull();

        if (!machine.HasBoard || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        foreach (var ent in new List<EntityUid>(machine.PartContainer.ContainedEntities)) //clone so don't modify during enumeration
        {
            if (TryComp<MachinePartComponent>(ent, out var part))
            {
                machineParts.Add(part);
                _container.RemoveEntity(uid, ent);
                machine.Progress[part.PartType]--;
            }
        }

        //order by highest rating
        machineParts = machineParts.OrderByDescending(p => p.Rating).ToList();

        var updatedParts = new List<MachinePartComponent>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            var target = machineParts.Where(p => p.PartType == type).Take(amount);
            updatedParts.AddRange(target);
        }
        foreach (var part in updatedParts)
        {
            if (!machine.Requirements.ContainsKey(part.PartType))
                continue;

            machine.PartContainer.Insert(part.Owner, EntityManager);
            machine.Progress[part.PartType]++;
            machineParts.Remove(part);
        }

        //put the unused parts back into rped. (this also does the "swapping")
        foreach (var unused in machineParts)
        {
            storage.Storage.Insert(unused.Owner);
            _storage.Insert(uid, unused.Owner, null, false);
        }
    }

    private void OnAfterInteract(EntityUid uid, PartExchangerComponent component, AfterInteractEvent args)
    {
        if (component.DoDistanceCheck && !args.CanReach)
            return;

        if (args.Target == null)
            return;

        if (!HasComp<MachineComponent>(args.Target) && !HasComp<MachineFrameComponent>(args.Target))
            return;

        if (TryComp<WiresPanelComponent>(args.Target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("construction-step-condition-wire-panel-open"),
                args.Target.Value);
            return;
        }

        component.AudioStream = _audio.PlayPvs(component.ExchangeSound, uid);

        _doAfter.TryStartDoAfter(new DoAfterArgs(args.User, HasComp<MachineComponent>(args.Target) ? component.ExchangeDuration : component.ConstructDuration, new ExchangerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true
        });
    }

}
