namespace Content.Client.NPC;

[RegisterComponent]
public sealed class NPCSteeringComponent : Component
{
    /* Not hooked up to the server component as it's used for debugging only.
     */

    public Vector2 Direction;

    public float[] DangerMap = Array.Empty<float>();
    public float[] InterestMap = Array.Empty<float>();
}
