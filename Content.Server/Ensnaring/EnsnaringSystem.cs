﻿using Content.Server.Ensnaring.Components;
using Content.Shared.Alert;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Interaction;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Throwing;

namespace Content.Server.Ensnaring;

public sealed class EnsnaringSystem : EntitySystem
{
    //TODO: AfterInteractionEvent is for testing purposes only, needs to be reworked into a rightclick verb
    [Dependency] private readonly EnsnareableSystem _ensnareable = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnaringComponent, StepTriggerAttemptEvent>(AttemptStepTrigger);
        SubscribeLocalEvent<EnsnaringComponent, StepTriggeredEvent>(OnStepTrigger);
        SubscribeLocalEvent<EnsnaringComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void AttemptStepTrigger(EntityUid uid, EnsnaringComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnStepTrigger(EntityUid uid, EnsnaringComponent component, ref StepTriggeredEvent args)
    {
        TryEnsnare(uid, args.Tripper, component);
    }

    private void OnThrowHit(EntityUid uid, EnsnaringComponent component, ThrowDoHitEvent args)
    {
        if (!component.CanThrowTrigger)
            return;

        TryEnsnare(uid, args.Target, component);
    }

    /// <summary>
    /// Used where you want to try to ensnare an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="ensnaringEntity">The entity that will be used to ensnare</param>
    /// <param name="target">The entity that will be ensnared</param>
    /// <param name="component">The ensnaring component</param>
    public void TryEnsnare(EntityUid ensnaringEntity, EntityUid target, EnsnaringComponent component)
    {
        //Don't do anything if they don't have the ensnareable component.
        if (!TryComp<EnsnareableComponent>(target, out var ensnareable))
            return;

        ensnareable.EnsnaringEntity = ensnaringEntity;
        ensnareable.Container.Insert(ensnaringEntity);
        ensnareable.IsEnsnared = true;

        _ensnareable.UpdateAlert(ensnareable);
        var ev = new EnsnareChangeEvent(ensnaringEntity, component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(target, ev, false);
    }
}
