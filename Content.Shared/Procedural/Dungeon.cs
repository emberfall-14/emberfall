namespace Content.Shared.Procedural;

public sealed class Dungeon
{
    public readonly List<DungeonRoom> Rooms = new();

    /// <summary>
    /// Hashset of the tiles across all rooms.
    /// </summary>
    public readonly HashSet<Vector2i> RoomTiles = new();

    public readonly HashSet<Vector2i> ExteriorTiles = new();
}
