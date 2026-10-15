using ArenaMod.Common;
using ArenaMod.Server.ArenaPlugin.Enums;
using ArenaMod.Server.Conditions;
using Microsoft.Extensions.Logging;
using OblivionMp.Sdk.Common.Archetypes;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion.ECS.Values;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;

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
internal sealed partial class ArenaSystem(
    Arena arena,
    IEntities entities,
    ArenaUtils arenaUtils,
    RpcHandlers rpc,
    ILogger logger
)
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
        arena.ArenaWinningConditions.Add(new LastManStandingCondition(arena, priority: 10));
        arena.ArenaWinningConditions.Add(new PlayerLossCondition(arena, entities, rpc, priority: 1, arenaHpLossCondition: ArenaLossCondition.PlayerHpBelowLimit));

        ArenaTimerCountdown.InitializeTimerStamps(30,
            onCountdown: (d) =>
            {
                foreach (var p in arena.Participants.Keys)
                {
                    rpc.SendArenaCountdown(p, (int)d);
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
                foreach (var main in entities.Query<MainCharacter>())
                {
                    if (arena.Participants.ContainsKey(main.PlayerId))
                    {
                        ArenaUtils.Heal(main);
                    }
                }
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
            case LastManStandingCondition lastManStanding:
                logger.LogInformation("{Name} has won!", lastManStanding.Winner);
                arena.ArenaStatus = ArenaStatus.Finished;
                arena.Participants.Clear();
                arena.ParticipantsStatusTimeouts.Clear();

                foreach (var main in entities.Query<MainCharacter>())
                {
                    rpc.SendAnnounceWinner(main.PlayerId, lastManStanding.Winner);
                }

                foreach (var condition in arena.ArenaWinningConditions)
                {
                    condition.Reset();
                }

                break;
        }

        Reset();
    }

    private void CheckStartingCondition()
    {
        if (arena.Participants.Count > 1)
        {
            foreach (var main in entities.Query<MainCharacter>())
            {
                if (arena.Participants.ContainsKey(main.PlayerId))
                {
                    if (arena.Participants[main.PlayerId] == ArenaParticipantStatus.NotReady) return;
                    if (main.ParentCellKind != ParentCellKind.Interior)
                    {
                        logger.LogInformation("Setting {player} as not ready", main.PlayerId);
                        arena.Participants[main.PlayerId] = ArenaParticipantStatus.NotReady;
                    }
                }
            }

            var totalPlayers = arena.Participants.Count;
            var readyPlayers = arena.Participants.Count(p => p.Value == ArenaParticipantStatus.Ready);

            var isMajorityReady = readyPlayers > (totalPlayers / 2);
            //bool isMajorityReady = readyPlayers >= (readyPlayers / totalPlayers);

            List<PlayerId> playersToKick = [];

            foreach (var participant in arena.Participants)
            {
                if (participant.Value == ArenaParticipantStatus.NotReady)
                {
                    if (isMajorityReady)
                    {
                        ArenaTimerCountdown.Update(ArenaSystemsInterval);

                        arena.ParticipantsStatusTimeouts.TryAdd(participant.Key, 0f);
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
                rpc?.SendArenaParticipantRegistered(pId, false, arena.Participants.Count);
            }

            if (playersToKick.Count > 0)
            {
                arenaUtils.BroadcastRegistrationUpdate();
                arenaUtils.BroadcastReadyUpdate();
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
                    rpc?.SendAnnounceFightStarted(p);
                }
            }
        }
    }

    public void HandleAutoReadySystem()
    {
        if (arena.ArenaStatus == ArenaStatus.NotStarted)
        {
            foreach (var main in entities.Query<MainCharacter>())
            {
                var dx = main.Position.X - RArenaCommon.Data.ArenaMiddle.X;
                var dy = main.Position.Y - RArenaCommon.Data.ArenaMiddle.Y;

                var distance = Math.Sqrt(dx * dx + dy * dy);
                //double readyRingRadius = RArenaCommon.Data.ArenaRadius / 2;
                double readyRingRadius = RArenaCommon.Data.ArenaInnerRingRadius;

                var isParticipant = arena.Participants.TryGetValue(main.PlayerId, out var status);

                if (distance > RArenaCommon.Data.ArenaRadius || main.Position.Z > 300)
                {
                    if (isParticipant)
                    {
                        logger.LogInformation("{player} left arena area, removing as contestant", main.PlayerId);
                        arena.Participants.Remove(main.PlayerId);
                        arena.ParticipantsStatusTimeouts.Remove(main.PlayerId);
                        rpc?.SendArenaParticipantRegistered(main.PlayerId, false, arena.Participants.Count);
                        arenaUtils.BroadcastRegistrationUpdate();
                        arenaUtils.BroadcastReadyUpdate();
                    }
                }
                else if (distance <= RArenaCommon.Data.ArenaRadius && distance > readyRingRadius)
                {
                    if (!isParticipant)
                    {
                        logger.LogInformation("{player} entered arena area, adding as contestant", main.PlayerId);
                        arena.Participants[main.PlayerId] = ArenaParticipantStatus.NotReady;
                        rpc?.SendArenaParticipantRegistered(main.PlayerId, true, arena.Participants.Count);
                        arenaUtils.BroadcastRegistrationUpdate();
                    }
                    else if (status != ArenaParticipantStatus.NotReady)
                    {
                        logger.LogInformation("{player} left inner ring, marking as not ready", main.PlayerId);
                        arena.Participants[main.PlayerId] = ArenaParticipantStatus.NotReady;
                        rpc?.SendArenaParticipantReady(main.PlayerId, false, arena.Participants.Count(p => p.Value == ArenaParticipantStatus.Ready));
                        arenaUtils.BroadcastReadyUpdate();
                    }
                }
                else if (distance <= readyRingRadius)
                {
                    if (isParticipant && status != ArenaParticipantStatus.Ready)
                    {
                        logger.LogInformation("{player} has entered inner ring, marking as ready", main.PlayerId);
                        arena.Participants[main.PlayerId] = ArenaParticipantStatus.Ready;
                        rpc?.SendArenaParticipantReady(main.PlayerId, true, arena.Participants.Count(p => p.Value == ArenaParticipantStatus.Ready));
                        arenaUtils.BroadcastReadyUpdate();
                    }
                }
            }
        }
    }

    public void HandleNotParticipatingPlayers()
    {
        if (arena.ArenaStatus != ArenaStatus.Ongoing) 
            return;
        
        foreach (var main in entities.Query<MainCharacter>())
        {
            if (main.ParentCellKind == ParentCellKind.Exterior)
                return;
                
            if (!arena.PlayersAreas[main.PlayerId].Contains(RArenaCommon.Data.ArenaCellId))
                return;
                
            if (!arena.Participants.ContainsKey(main.PlayerId) || arena.Participants[main.PlayerId] == ArenaParticipantStatus.Loss)
            {
                if (main.Position.Z < 210)
                {
                    main.Position = RArenaCommon.Data.ArenaSpectatorStand;
                }

                ArenaUtils.Heal(main);
            }
        }
    }

    public void HandleArenaNone()
    {
        if (arena.ArenaStatus == ArenaStatus.Ongoing)
        {
            Reset();
            logger.LogWarning("Match started in invalid mode, reset invoked!");
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
        // TODO
    }

    public void MercyGiveUp(PlayerId playerId)
    {
        // Called when player passes the mercy point of arena about 3 meters past the exit gate
        arena.Participants[playerId] = ArenaParticipantStatus.Loss;
        foreach (var p in arena.Participants.Keys)
        {
            rpc.SendPlayerEliminated(p, playerId);
        }
    }

    public void CheckPlayersOutsideArena()
    {
        foreach (var main in entities.Query<MainCharacter>())
        {
            if (arena.Participants.TryGetValue(main.PlayerId, out var status) && status == ArenaParticipantStatus.Fighting)
            {
                if (main.ParentCellKind == ParentCellKind.Exterior)
                {
                    MercyGiveUp(main.PlayerId);
                    return;
                }

                var dx = main.Position.X - RArenaCommon.Data.ArenaMiddle.X;
                var dy = main.Position.Y - RArenaCommon.Data.ArenaMiddle.Y;

                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > RArenaCommon.Data.ArenaRadius + RArenaCommon.Data.MercyGiveUpMargin)
                {
                    logger.LogInformation("{Player} mercifully gave up", main.PlayerId);
                    MercyGiveUp(main.PlayerId);
                }
            }
        }
    }

    public void SetPlayersImmortality(bool enabled)
    {
        logger.LogInformation("{State} players immortality!", enabled ? "Enabling" : "Lifting");

        foreach (var main in entities.Query<MainCharacter>())
        {
            if (enabled)
            {
                ArenaUtils.Heal(main);
            }
        }

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