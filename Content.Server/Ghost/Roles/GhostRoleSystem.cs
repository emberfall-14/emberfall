using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Ghost.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles;
using Content.Shared.Mobs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Roles
{
    [UsedImplicitly]
    public sealed class GhostRoleSystem : EntitySystem
    {
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly FollowerSystem _followerSystem = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;

        private uint _nextRoleIdentifier = 1;
        private bool _needsUpdateGhostRoleCount = true;
        private readonly Dictionary<uint, GhostRoleInfo> _ghostRoles = new();
        private readonly Dictionary<IPlayerSession, GhostRolesEui> _openUis = new();
        private readonly Dictionary<IPlayerSession, MakeGhostRoleEui> _openMakeGhostRoleUis = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<GhostRoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GhostRoleComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<GhostRoleComponent, EntityPausedEvent>(OnPaused);
            SubscribeLocalEvent<GhostRoleComponent, EntityUnpausedEvent>(OnUnpaused);
            SubscribeLocalEvent<GhostRoleMobSpawnerComponent, TakeGhostRoleEvent>(OnSpawnerTakeRole);
            SubscribeLocalEvent<GhostTakeoverAvailableComponent, TakeGhostRoleEvent>(OnTakeoverTakeRole);
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private void OnMobStateChanged(EntityUid uid, GhostTakeoverAvailableComponent component, MobStateChangedEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole))
                return;

            switch (args.NewMobState)
            {
                case MobState.Alive:
                {
                    if (!ghostRole.Taken)
                        RegisterGhostRole(uid, ghostRole);
                    break;
                }
                case MobState.Critical:
                case MobState.Dead:
                    UnregisterGhostRole(uid, ghostRole);
                    break;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= PlayerStatusChanged;
        }

        private uint GetNextRoleIdentifier()
        {
            return unchecked(_nextRoleIdentifier++);
        }

        public void OpenEui(IPlayerSession session)
        {
            if (session.AttachedEntity is not {Valid: true} attached ||
                !EntityManager.HasComponent<GhostComponent>(attached))
                return;

            if(_openUis.ContainsKey(session))
                CloseEui(session);

            var eui = _openUis[session] = new GhostRolesEui();
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void OpenMakeGhostRoleEui(IPlayerSession session, EntityUid uid)
        {
            if (session.AttachedEntity == null)
                return;

            if (_openMakeGhostRoleUis.ContainsKey(session))
                CloseEui(session);

            var eui = _openMakeGhostRoleUis[session] = new MakeGhostRoleEui(uid);
            _euiManager.OpenEui(eui, session);
            eui.StateDirty();
        }

        public void CloseEui(IPlayerSession session)
        {
            if (!_openUis.ContainsKey(session)) return;

            _openUis.Remove(session, out var eui);

            eui?.Close();
        }

        public void CloseMakeGhostRoleEui(IPlayerSession session)
        {
            if (_openMakeGhostRoleUis.Remove(session, out var eui))
            {
                eui.Close();
            }
        }

        public void UpdateAllEui()
        {
            foreach (var eui in _openUis.Values)
            {
                eui.StateDirty();
            }
            // Note that this, like the EUIs, is deferred.
            // This is for roughly the same reasons, too:
            // Someone might spawn a ton of ghost roles at once.
            _needsUpdateGhostRoleCount = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (_needsUpdateGhostRoleCount)
            {
                _needsUpdateGhostRoleCount = false;
                var response = new GhostUpdateGhostRoleCountEvent(GhostRoles().Count());
                foreach (var player in _playerManager.Sessions)
                {
                    RaiseNetworkEvent(response, player.ConnectedClient);
                }
            }
        }

        private void PlayerStatusChanged(object? blah, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.InGame)
            {
                var response = new GhostUpdateGhostRoleCountEvent(_ghostRoles.Count);
                RaiseNetworkEvent(response, args.Session.ConnectedClient);
            }
        }

        // Triggered on GhostRole Component addition. userId is passed back in the TakeGhostRoleEvent
        public void RegisterGhostRole(EntityUid uid, GhostRoleComponent role, string userId = "")
        {
            if (_ghostRoles.ContainsKey(role.Identifier))
                return;
            var info = new GhostRoleInfo()
                { Identifier = 0, Name = role.RoleName, Description = role.RoleDescription, Rules = role.RoleRules, Owner = uid, UserId = userId};

            RegisterGhostRole(ref info);
            role.Identifier = info.Identifier;
        }

        // Triggered by setters inside GhostRole to update the info relating to that role.
        public void UpdateGhostRole(GhostRoleComponent role)
        {
            if (_ghostRoles.TryGetValue(role.Identifier, out var info))
            {
                // Copy new information from the role.
                info.Name = role.RoleName;
                info.Description = role.RoleDescription;
                info.Rules = role.RoleRules;
                UpdateAllEui();
            }
        }

        public void RegisterGhostRole(ref GhostRoleInfo info)
        {
            if (info.Identifier == 0)
                info.Identifier = GetNextRoleIdentifier();
            _ghostRoles[info.Identifier] = info;
            UpdateAllEui();
        }

        public void UnregisterGhostRole(EntityUid uid, GhostRoleComponent role)
        {
            if (!_ghostRoles.ContainsKey(role.Identifier) || _ghostRoles[role.Identifier].Owner != uid) return;
            _ghostRoles.Remove(role.Identifier);
            UpdateAllEui();
        }

        public void UnregisterGhostRole(GhostRoleInfo info)
        {
            if (_ghostRoles.Remove(info.Identifier))
                UpdateAllEui();
        }

        public void Takeover(IPlayerSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var role)) return;

            var ev = new TakeGhostRoleEvent(player, role.UserId);
            RaiseLocalEvent(role.Owner, ref ev);

            if (!ev.TookRole) return;

            if (player.AttachedEntity != null)
                _adminLogger.Add(LogType.GhostRoleTaken, LogImpact.Low, $"{player:player} took the {role.Name:roleName} ghost role {ToPrettyString(player.AttachedEntity.Value):entity}");

            CloseEui(player);
        }

        public void Follow(IPlayerSession player, uint identifier)
        {
            if (!_ghostRoles.TryGetValue(identifier, out var role)) return;
            if (player.AttachedEntity == null) return;

            _followerSystem.StartFollowingEntity(player.AttachedEntity.Value, role.Owner);
        }

        public void GhostRoleInternalCreateMindAndTransfer(IPlayerSession player, EntityUid roleUid, EntityUid mob, GhostRoleComponent? role = null)
        {
            if (!Resolve(roleUid, ref role)) return;

            DebugTools.AssertNotNull(player.ContentData());

            var newMind = _mindSystem.CreateMind(player.UserId,
                EntityManager.GetComponent<MetaDataComponent>(mob).EntityName);
            _mindSystem.AddRole(newMind, new GhostRoleMarkerRole(newMind, role.RoleName));

            _mindSystem.SetUserId(newMind, player.UserId);
            _mindSystem.TransferTo(newMind, mob);
        }

        public IEnumerable<GhostRoleInfo> GhostRoles()
        {
            var metaQuery = GetEntityQuery<MetaDataComponent>();

            return _ghostRoles.Values.Where((info) => !metaQuery.GetComponent(info.Owner).EntityPaused);
        }

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {
            // Close the session of any player that has a ghost roles window open and isn't a ghost anymore.
            if (!_openUis.ContainsKey(message.Player)) return;
            if (EntityManager.HasComponent<GhostComponent>(message.Entity)) return;
            CloseEui(message.Player);
        }

        private void OnMindAdded(EntityUid uid, GhostTakeoverAvailableComponent component, MindAddedMessage args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole))
                return;

            ghostRole.Taken = true;
            UnregisterGhostRole(uid, ghostRole);
        }

        private void OnMindRemoved(EntityUid uid, GhostTakeoverAvailableComponent component, MindRemovedMessage args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole))
                return;

            // Avoid re-registering it for duplicate entries and potential exceptions.
            if (!ghostRole.ReregisterOnGhost || component.LifeStage > ComponentLifeStage.Running)
                return;

            ghostRole.Taken = false;
            RegisterGhostRole(uid, ghostRole);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var session in _openUis.Keys)
            {
                CloseEui(session);
            }

            _openUis.Clear();
            _ghostRoles.Clear();
            _nextRoleIdentifier = 0;
        }

        private void OnPaused(EntityUid uid, GhostRoleComponent component, ref EntityPausedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            UpdateAllEui();
        }

        private void OnUnpaused(EntityUid uid, GhostRoleComponent component, ref EntityUnpausedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            UpdateAllEui();
        }

        private void OnInit(EntityUid uid, GhostRoleComponent role, ComponentInit args)
        {
            if (role.Probability < 1f && !_random.Prob(role.Probability))
            {
                RemComp<GhostRoleComponent>(uid);
                return;
            }

            if (role.RoleRules == "")
                role.RoleRules = Loc.GetString("ghost-role-component-default-rules");
            RegisterGhostRole(uid, role);
        }

        private void OnShutdown(EntityUid uid, GhostRoleComponent role, ComponentShutdown args)
        {
            UnregisterGhostRole(uid, role);
        }

        private void OnSpawnerTakeRole(EntityUid uid, GhostRoleMobSpawnerComponent component, ref TakeGhostRoleEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                !CanTakeGhost(uid, ghostRole))
            {
                args.TookRole = false;
                return;
            }

            if (string.IsNullOrEmpty(component.Prototype))
                throw new NullReferenceException("Prototype string cannot be null or empty!");

            var mob = Spawn(component.Prototype, Transform(uid).Coordinates);
            _transform.AttachToGridOrMap(mob);

            var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, mob);
            RaiseLocalEvent(mob, spawnedEvent);

            if (ghostRole.MakeSentient)
                MakeSentientCommand.MakeSentient(mob, EntityManager, ghostRole.AllowMovement, ghostRole.AllowSpeech);

            mob.EnsureComponent<MindContainerComponent>();

            GhostRoleInternalCreateMindAndTransfer(args.Player, uid, mob, ghostRole);

            if (++component.CurrentTakeovers < component.AvailableTakeovers)
            {
                args.TookRole = true;
                return;
            }

            ghostRole.Taken = true;

            if (component.DeleteOnSpawn)
                QueueDel(uid);

            args.TookRole = true;
        }

        private bool CanTakeGhost(EntityUid uid, GhostRoleComponent? component = null)
        {
            return Resolve(uid, ref component, false) &&
                   !component.Taken &&
                   !MetaData(uid).EntityPaused;
        }

        private void OnTakeoverTakeRole(EntityUid uid, GhostTakeoverAvailableComponent component, ref TakeGhostRoleEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                !CanTakeGhost(uid, ghostRole))
            {
                args.TookRole = false;
                return;
            }

            ghostRole.Taken = true;

            var mind = EnsureComp<MindContainerComponent>(uid);

            if (mind.HasMind)
            {
                args.TookRole = false;
                return;
            }

            if (ghostRole.MakeSentient)
                MakeSentientCommand.MakeSentient(uid, EntityManager, ghostRole.AllowMovement, ghostRole.AllowSpeech);

            GhostRoleInternalCreateMindAndTransfer(args.Player, uid, uid, ghostRole);
            UnregisterGhostRole(uid, ghostRole);

            args.TookRole = true;
        }
    }

    [AnyCommand]
    public sealed class GhostRoles : IConsoleCommand
    {
        public string Command => "ghostroles";
        public string Description => "Opens the ghost role request window.";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if(shell.Player != null)
                EntitySystem.Get<GhostRoleSystem>().OpenEui((IPlayerSession)shell.Player);
            else
                shell.WriteLine("You can only open the ghost roles UI on a client.");
        }
    }
}
