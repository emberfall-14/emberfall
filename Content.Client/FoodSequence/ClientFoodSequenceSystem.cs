using Content.Shared.FoodSequence;
using Robust.Client.GameObjects;

namespace Content.Client.FoodSequence;

public sealed partial class ClientFoodSequenceSystem : SharedFoodSequenceSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<FoodSequenceStartPointComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<FoodSequenceStartPointComponent> start, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(start, out var sprite))
            return;

        UpdateFoodVisuals(start, sprite);
    }

    private void UpdateFoodVisuals(Entity<FoodSequenceStartPointComponent> start, SpriteComponent? sprite = null)
    {
        if (!Resolve(start, ref sprite, false))
            return;

        //Remove old layers
        foreach (var key in start.Comp.RevealedLayers)
        {
            sprite.RemoveLayer(key);
        }
        start.Comp.RevealedLayers.Clear();

        //Add new layers
        var counter = 0;
        foreach (var state in start.Comp.FoodLayers)
        {
            counter++;

            var keyCode = $"food-layer-{counter}";
            start.Comp.RevealedLayers.Add(keyCode);

            //Set image
            var index = sprite.LayerMapReserveBlank(keyCode);
            sprite.LayerSetRSI(index, start.Comp.RsiPath);
            sprite.LayerSetState(index, state);

            //Offset the layer
            var LayerPos = start.Comp.StartPosition;
            LayerPos += start.Comp.Offset * counter;
            sprite.LayerSetOffset(index, LayerPos);
        }
    }
}
