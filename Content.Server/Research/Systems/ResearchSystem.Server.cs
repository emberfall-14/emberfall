using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeServer()
    {
        SubscribeLocalEvent<ResearchServerComponent, ComponentStartup>(OnServerStartup);
        SubscribeLocalEvent<ResearchServerComponent, ComponentShutdown>(OnServerShutdown);
        SubscribeLocalEvent<ResearchServerComponent, TechnologyDatabaseModifiedEvent>(OnServerDatabaseModified);
    }

    private void OnServerStartup(EntityUid uid, ResearchServerComponent component, ComponentStartup args)
    {
        var unusedId = EntityQuery<ResearchServerComponent>(true)
            .Max(s => s.Id) + 1;
        component.Id = unusedId;
        Dirty(component);
    }

    private void OnServerShutdown(EntityUid uid, ResearchServerComponent component, ComponentShutdown args)
    {
        foreach (var client in new List<EntityUid>(component.Clients))
        {
            UnregisterClient(client, uid, serverComponent: component, dirtyServer: false);
        }
    }

    private void OnServerDatabaseModified(EntityUid uid, ResearchServerComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref args);
        }
    }

    private bool CanRun(EntityUid uid)
    {
        return this.IsPowered(uid, EntityManager);
    }

    private void UpdateServer(EntityUid uid, int time, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!CanRun(uid))
            return;
        ChangePointsOnServer(uid, PointsPerSecond(uid, component) * time, component);
    }

    /// <summary>
    /// Registers a client to the specified server.
    /// </summary>
    /// <param name="client">The client being registered</param>
    /// <param name="server">The server the client is being registered to</param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer">Whether or not to dirty the server component after registration</param>
    /// <returns>Whether or not the client wa successfully registered</returns>
    public bool RegisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null,
        ResearchServerComponent? serverComponent = null,  bool dirtyServer = true)
    {
        if (!Resolve(client, ref clientComponent) || !Resolve(server, ref serverComponent))
            return false;

        if (serverComponent.Clients.Contains(client))
            return false;

        serverComponent.Clients.Add(client);
        clientComponent.Server = server;

        if (dirtyServer)
            Dirty(serverComponent);

        var ev = new ResearchRegistrationChangedEvent(server);
        RaiseLocalEvent(client, ref ev);
        return true;
    }

    /// <summary>
    /// Unregisterse a client from its server
    /// </summary>
    /// <param name="client"></param>
    /// <param name="clientComponent"></param>
    /// <param name="dirtyServer"></param>
    public void UnregisterClient(EntityUid client, ResearchClientComponent? clientComponent = null, bool dirtyServer = true)
    {
        if (!Resolve(client, ref clientComponent))
            return;

        if (clientComponent.Server is not { } server)
            return;

        UnregisterClient(client, server, clientComponent, dirtyServer: dirtyServer);
    }

    /// <summary>
    /// Unregisters a client from its server
    /// </summary>
    /// <param name="client"></param>
    /// <param name="server"></param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer"></param>
    public void UnregisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null,
        ResearchServerComponent? serverComponent = null, bool dirtyServer = true)
    {
        if (!Resolve(client, ref clientComponent) || !Resolve(server, ref serverComponent))
            return;

        serverComponent.Clients.Remove(client);
        clientComponent.Server = null;

        if (dirtyServer)
        {
            Dirty(serverComponent);
        }

        var ev = new ResearchRegistrationChangedEvent(null);
        RaiseLocalEvent(client, ref ev);
    }

    public int PointsPerSecond(EntityUid uid, ResearchServerComponent? component = null)
    {
        var points = 0;

        if (!Resolve(uid, ref component))
            return points;

        if (!CanRun(uid))
            return points;

        var ev = new ResearchServerGetPointsPerSecondEvent(component.Owner, points);
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }
        return ev.Points;
    }

    public void ChangePointsOnServer(EntityUid uid, int points, ResearchServerComponent? component = null)
    {
        if (points == 0)
            return;

        if (!Resolve(uid, ref component))
            return;
        component.Points += points;
        var ev = new ResearchServerPointsChangedEvent(uid, component.Points, points);
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }
        Dirty(component);
    }
}
