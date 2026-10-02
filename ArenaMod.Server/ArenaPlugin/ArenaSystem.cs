using ArenaMod.Common;
using ArenaMod.Server.ArenaPlugin.Enums;
using ArenaMod.Server.Conditions;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion.ECS.Components;
using ReadyM.Relay.Common.Oblivion.ECS.Values;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.SDK.Attributes;

namespace ArenaMod.Server.ArenaPlugin;

// Known problems:
// 1) TransformComponent.Position override not doing anything
// 2) Rebirth merging two save files (world.sav + player_guid.sav)
//      Results in appearance matching player.sav while attributes and inventory are that of world.sav
//
// 3) Items durability may be worn out after a match or two depending on the use
//      This may be an issue should the person want to remain on given character
//      Arrows and magic scrolls can be refilled using SDK.AddItemToInventory
//      May need to force the player to reload the game, possibly having different character upon connecting
[Service]
internal sealed partial class ArenaSystem(Arena arena, EcsApi ecsApi, RArenaServerRpc sRpc, ILogger logger)
{
    private float _arenaSystemsElapsed;
    private const float ArenaSystemsInterval = 0.3f;
    private event Action ArenaSystemsCallback = () => { };
    private bool _doFfAonce;

    private void Update()
    {
        _arenaSystemsElapsed += Time.DeltaTime;

        if (_arenaSystemsElapsed > ArenaSystemsInterval)
        {
            _arenaSystemsElapsed -= ArenaSystemsInterval;
            ArenaSystemsCallback?.Invoke();
        }

        if (arena.ArenaStatus == ArenaStatus.NotStarted) return;

        if (arena.ArenaStatus == ArenaStatus.Finished)
        {
            logger.LogInformation("Match has concluded with winner: {}",
                arena.Participants.Any(b => b.Value == ArenaParticipantStatus.Win)
                    ? arena.Participants.Where(b => b.Value == ArenaParticipantStatus.Win)
                    : "None");
        }

        switch (arena.ArenaMode)
        {
            case ArenaMode.ArenaNone:
                HandleArenaNone();
                break;
            case ArenaMode.ArenaFfa:
                HandleArenaFfa();
                break;
            case ArenaMode.ArenaTournament:
                HandleArenaTournament();
                break;
            default:
                HandleArenaNone();
                break;
        }
    }

    private void Start()
    {
        ArenaUtils.Arena = arena;
        ArenaUtils.EcsApi = ecsApi;
        ArenaUtils.SRpc = sRpc;

        arena.ArenaWinningConditions.Add(new ArenaLmsCondition(arena, priority: 10));
        arena.ArenaWinningConditions.Add(new ArenaPlayerLossCondition(arena, ecsApi, sRpc, priority: 1, arenaHpLossCondition: ArenaLossCondition.PlayerHpBelowLimit));

        ArenaTimerCountdown.InitializeTimerStamps(30,
            onCountdown: (d) =>
            {
                foreach (var p in arena.Participants.Keys)
                {
                    sRpc.SendArenaCountdown(p, (int)d);
                }
            }
        );

        // Disqualify player  outside the bounds
        ArenaSystemsCallback += () =>
        {
            if (arena.ArenaStatus != ArenaStatus.Ongoing) return;

            CheckPlayersOutsideArena();
        };

        // Ensure immortality for players before the match starts, this may be incompatible with other mods
        // needs more checks to ensure immortality in arena scoped cells
        ArenaSystemsCallback += HandleAutoReadySystem;
        ArenaSystemsCallback += HandleArenaStatusNotStarted;

        // Check winning condition
        ArenaSystemsCallback += HandleMatchConditions;

        // Teleport not participating players out
        ArenaSystemsCallback += HandleNotParticipatingPlayers;
    }

    private void HandleMatchConditions()
    {
        switch (arena.ArenaMode)
        {
            case ArenaMode.ArenaFfa:
                if (arena.ArenaStatus == ArenaStatus.Ongoing)
                {
                    foreach (var c in arena.ArenaWinningConditions)
                    {
                        c.OnUpdate();
                        if (c.WasFullfilled()) ConcludeArenaMatch(c);
                    }
                }

                break;
        }
    }

    private void HandleArenaStatusNotStarted()
    {
        if (arena.ArenaStatus == ArenaStatus.NotStarted)
        {
            if (arena.PlayersImmortal)
            {
                ecsApi.QueryWithEntity<MainCharacterComponent, VitalsComponent>((ref main, ref vital, id) =>
                {
                    if (arena.Participants.ContainsKey(main.PlayerId))
                    {
                        //logger.LogError("Vital modified, arena status: {status}", rArena.ArenaStatus.ToString());
                        ArenaUtils.Heal(id, ref vital);
                    }
                });
            }

            CheckStartingCondition();
        }
    }

    public void Reset()
    {
        logger.LogInformation("Arena reset invoked");
        arena.ArenaMode = ArenaMode.ArenaNone;
        arena.ArenaStatus = ArenaStatus.NotStarted;
        _doFfAonce = false;
        SetPlayersImmortality(true);
    }

    private void ConcludeArenaMatch(ArenaCondition c)
    {
        switch (c)
        {
            case ArenaLmsCondition lastManStanding:
                logger.LogInformation("{Name} has won!", lastManStanding.Winner);
                arena.ArenaStatus = ArenaStatus.Finished;
                arena.Participants.Clear();
                arena.ParticipantsStatusTimeouts.Clear();

                ecsApi.Query<MainCharacterComponent>((ref main) =>
                {
                    //sRpc.SendAnnounceFightEnded(main.PlayerId);
                    sRpc.SendAnnounceWinner(main.PlayerId, lastManStanding.Winner);
                });

                foreach (var condition in arena.ArenaWinningConditions)
                {
                    condition.Reset();
                }

                Reset();
                break;
            default:
                Reset();
                break;
        }
    }

    private void CheckStartingCondition()
    {
        if (arena.Participants.Count > 1)
        {
            ecsApi.Query<MainCharacterComponent, ParentCellComponent>((ref player, ref cell) =>
            {
                if (arena.Participants.ContainsKey(player.PlayerId))
                {
                    if (arena.Participants[player.PlayerId] == ArenaParticipantStatus.NotReady) return;
                    if (cell.Kind != ParentCellKind.Interior)
                    {
                        logger.LogInformation("Setting {player} as not ready", player.PlayerId);
                        arena.Participants[player.PlayerId] = ArenaParticipantStatus.NotReady;
                    }
                }
            });

            int totalPlayers = arena.Participants.Count;
            int readyPlayers = arena.Participants.Count(p => p.Value == ArenaParticipantStatus.Ready);

            bool isMajorityReady = readyPlayers > (totalPlayers / 2);
            //bool isMajorityReady = readyPlayers >= (readyPlayers / totalPlayers);

            List<PlayerId> playersToKick = [];

            foreach (var participant in arena.Participants)
            {
                if (participant.Value == ArenaParticipantStatus.NotReady)
                {
                    if (isMajorityReady)
                    {
                        ArenaTimerCountdown.Update(ArenaSystemsInterval);
                        if (!arena.ParticipantsStatusTimeouts.ContainsKey(participant.Key))
                        {
                            arena.ParticipantsStatusTimeouts[participant.Key] = 0f;
                        }

                        arena.ParticipantsStatusTimeouts[participant.Key] += ArenaSystemsInterval;

                        if (arena.ParticipantsStatusTimeouts[participant.Key] >= 30f)
                        {
                            playersToKick.Add(participant.Key);
                        }
                    }
                    else
                    {
                        ArenaTimerCountdown.ResetCountdown();
                        arena.ParticipantsStatusTimeouts.Remove(participant.Key);
                    }
                }
            }

            foreach (var pId in playersToKick)
            {
                logger.LogInformation("{Player} has been timed out for waiting too long.", pId);
                arena.Participants.Remove(pId);
                arena.ParticipantsStatusTimeouts.Remove(pId);
                sRpc?.SendArenaParticipantRegistered(pId, false, arena.Participants.Count);
            }

            if (playersToKick.Count > 0)
            {
                ArenaUtils.BroadcastRegistrationUpdate();
                ArenaUtils.BroadcastReadyUpdate();
            }

            if (arena.Participants.All(b => b.Value == ArenaParticipantStatus.Ready)
                && arena.ArenaStatus == ArenaStatus.NotStarted)
            {
                Reset();
                // TODO decide how to pick a mode
                // File config per server?
                // rArena.ArenaMode = EArenaMode
                // For now FFA only
                // Game ending conditions are sorted descending based on priority
                // I.E LastManStandingCondition with priority 2 will end match before the timer runs out.
                arena.ArenaWinningConditions.Sort((a, b) => (b.Priority.CompareTo(a.Priority)));
                arena.ArenaMode = ArenaMode.ArenaFfa;
                arena.ArenaStatus = ArenaStatus.Ongoing;
                SetPlayersStatus(ArenaParticipantStatus.Fighting);
                foreach (var p in arena.Participants.Keys)
                {
                    sRpc?.SendAnnounceFightStarted(p);
                }
            }
        }
    }

    public void HandleAutoReadySystem()
    {
        if (arena.ArenaStatus == ArenaStatus.NotStarted)
        {
            ecsApi.Query<MainCharacterComponent, TransformComponent>((ref player, ref t) =>
            {
                var dx = t.Position.X - RArenaCommon.Data.ArenaMiddle.X;
                var dy = t.Position.Y - RArenaCommon.Data.ArenaMiddle.Y;

                double distance = Math.Sqrt(dx * dx + dy * dy);
                //double readyRingRadius = RArenaCommon.Data.ArenaRadius / 2;
                double readyRingRadius = RArenaCommon.Data.ArenaInnerRingRadius;

                bool isParticipant = arena.Participants.TryGetValue(player.PlayerId, out var status);

                if (distance > RArenaCommon.Data.ArenaRadius || t.Position.Z > 300)
                {
                    if (isParticipant)
                    {
                        logger.LogInformation("{player} left arena area, removing as contestant", player.PlayerId);
                        arena.Participants.Remove(player.PlayerId);
                        arena.ParticipantsStatusTimeouts.Remove(player.PlayerId);
                        sRpc?.SendArenaParticipantRegistered(player.PlayerId, false, arena.Participants.Count);
                        ArenaUtils.BroadcastRegistrationUpdate();
                        ArenaUtils.BroadcastReadyUpdate();
                    }
                }
                else if (distance <= RArenaCommon.Data.ArenaRadius && distance > readyRingRadius)
                {
                    if (!isParticipant)
                    {
                        logger.LogInformation("{player} entered arena area, adding as contestant", player.PlayerId);
                        arena.Participants[player.PlayerId] = ArenaParticipantStatus.NotReady;
                        sRpc?.SendArenaParticipantRegistered(player.PlayerId, true, arena.Participants.Count);
                        ArenaUtils.BroadcastRegistrationUpdate();
                    }
                    else if (status != ArenaParticipantStatus.NotReady)
                    {
                        logger.LogInformation("{player} left inner ring, marking as not ready", player.PlayerId);
                        arena.Participants[player.PlayerId] = ArenaParticipantStatus.NotReady;
                        sRpc?.SendArenaParticipantReady(player.PlayerId, false, arena.Participants.Count(p => p.Value == ArenaParticipantStatus.Ready));
                        ArenaUtils.BroadcastReadyUpdate();
                    }
                }
                else if (distance <= readyRingRadius)
                {
                    if (isParticipant && status != ArenaParticipantStatus.Ready)
                    {
                        logger.LogInformation("{player} has entered inner ring, marking as ready", player.PlayerId);
                        arena.Participants[player.PlayerId] = ArenaParticipantStatus.Ready;
                        sRpc?.SendArenaParticipantReady(player.PlayerId, true, arena.Participants.Count(p => p.Value == ArenaParticipantStatus.Ready));
                        ArenaUtils.BroadcastReadyUpdate();
                    }
                }
            });
        }
    }

    public void HandleNotParticipatingPlayers()
    {
        if (arena.ArenaStatus == ArenaStatus.Ongoing)
        {
            ecsApi.QueryWithEntity<MainCharacterComponent, VitalsComponent>((ref main, ref vital, id) =>
            {
                ref var t = ref ecsApi.GetComponentRef<TransformComponent>(id);
                ref var cell = ref ecsApi.GetComponentRef<ParentCellComponent>(id);
                //if (!rArena.Participants.ContainsKey(main.PlayerId)) return;
                if (cell.Kind == ParentCellKind.Exterior) return;
                //logger.LogWarning(rArena.PlayersAreas.Values.ToString());
                if (!arena.PlayersAreas[main.PlayerId].Contains(RArenaCommon.Data.ArenaCellId)) return;
                if (!arena.Participants.ContainsKey(main.PlayerId) || arena.Participants[main.PlayerId] == ArenaParticipantStatus.Loss)
                {
                    if (t.Position.Z < 210)
                    {
                        ArenaUtils.Teleport(id, ref t, RArenaCommon.Data.ArenaSpectatorStand);
                    }

                    ArenaUtils.Heal(id, ref vital);
                }
            });
        }
    }

    public void HandleArenaNone()
    {
        if (arena.ArenaStatus == ArenaStatus.Ongoing)
        {
            Reset();
            logger.LogWarning("Match started in invalid mode, reset invoked!");
            return;
        }
    }

    public void HandleArenaFfa()
    {
        if (arena.ArenaMode != ArenaMode.ArenaFfa)
        {
            Reset();
            logger.LogInformation("FFA Match started in invalid mode, reset invoked!");
            return;
        }

        if (!_doFfAonce)
        {
            _doFfAonce = true;
            logger.LogInformation("FFA Match has started");
            SetPlayersImmortality(false);
        }
    }

    public void HandleArenaTournament()
    {
        //TODO
    }

    public void MercyGiveUp(PlayerId playerId)
    {
        // Called when player passes the mercy point of arena about 3 meters past the exit gate
        arena.Participants[playerId] = ArenaParticipantStatus.Loss;
        foreach (var p in arena.Participants.Keys)
        {
            sRpc.SendPlayerEliminated(p, playerId);
        }
    }

    public void CheckPlayersOutsideArena()
    {
        ecsApi.Query<MainCharacterComponent, TransformComponent, ParentCellComponent>((ref player, ref t, ref cell) =>
        {
            if (arena.Participants.TryGetValue(player.PlayerId, out var status) && status == ArenaParticipantStatus.Fighting)
            {
                if (cell.Kind == ParentCellKind.Exterior)
                {
                    MercyGiveUp(player.PlayerId);
                    return;
                }

                float dx = t.Position.X - RArenaCommon.Data.ArenaMiddle.X;
                float dy = t.Position.Y - RArenaCommon.Data.ArenaMiddle.Y;

                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > RArenaCommon.Data.ArenaRadius + RArenaCommon.Data.MercyGiveUpMargin)
                {
                    logger.LogInformation("{Player} mercifully gave up", player.PlayerId);
                    MercyGiveUp(player.PlayerId);
                }
            }
        });
    }

    public void SetPlayersImmortality(bool enabled)
    {
        logger.LogInformation("{State} players immortality!", enabled ? "Enabling" : "Lifting");

        ecsApi.QueryWithEntity<VitalsComponent>((ref vital, id) =>
        {
            if (enabled)
            {
                //logger.LogError("Vital modified, arena status: {status}", rArena.ArenaStatus.ToString());
                // vital.Hp = vital.HpBase;
                ArenaUtils.Heal(id, ref vital);
            }
        });

        arena.PlayersImmortal = enabled;
    }

    public void SetPlayersStatus(ArenaParticipantStatus status)
    {
        lock (arena.Participants)
        {
            foreach (var p in arena.Participants.Keys)
            {
                arena.Participants[p] = status;
            }
        }
    }
}