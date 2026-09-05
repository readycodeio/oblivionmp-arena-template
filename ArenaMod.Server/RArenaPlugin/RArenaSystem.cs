using Microsoft.Extensions.Logging;
using ArenaMod.Common;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion.ECS.Components;
using ReadyM.Relay.Common.Oblivion.ECS.Values;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;

namespace ArenaMod.Server.RArenaPlugin
{
    // Known problems:
    // 1) TransformComponent.Position override not doing anything
    // 2) Rebirth merging two save files (world.sav + player_guid.sav)
    //      Results in appearance matching player.sav while attributes and inventory are that of world.sav
    //
    // 3) Items durability may be worn out after a match or two depending on the use
    //      This may be an issue should the person want to remain on given character
    //      Arrows and magic scrolls can be refilled using SDK.AddItemToInventory
    //      May need to force the player to reload the game, possibly having different character upon connecting
    // 4)

    internal class RArenaSystem(RArena rArena, EcsApi ecsApi, RArenaServerRpc sRpc, ILogger logger) : ModSystemBase
    {
        private float _arenaSystemsElapsed = 0f;
        private float _arenaSystemsInterval = 0.3f;
        private event Action _arenaSystemsCallback = () => { };
        private bool _hasInit = false;
        private bool _doFFAonce = false;

        private UpdateTick _tick;

        protected override void OnUpdate(UpdateTick tick)
        {
            if (!_hasInit)
            {
                Init();
            }
            _tick = tick;
            _arenaSystemsElapsed += tick.DeltaTime;

            if (_arenaSystemsElapsed > _arenaSystemsInterval)
            {
                _arenaSystemsElapsed -= _arenaSystemsInterval;
                _arenaSystemsCallback?.Invoke();
            }

            if (rArena.ArenaStatus == EArenaStatus.NotStarted || rArena.tournament.ArenaTournamentConcluded) return;

            if (rArena.ArenaStatus == EArenaStatus.Finished)
            {

                logger.LogInformation("Match has concluded with winner: {}",
                    rArena.Participants.Any(b => b.Value == EArenaParticipantStatus.Win)
                    ? rArena.Participants.Where(b => b.Value == EArenaParticipantStatus.Win)
                    : "None");
            }

            switch (rArena.ArenaMode)
            {
                case EArenaMode.ArenaNone:
                    HandleArenaNone();
                    break;
                case EArenaMode.ArenaFFA:
                    HandleArenaFFA();
                    break;
                case EArenaMode.ArenaTournament:
                    HandleArenaTournament();
                    break;
                default:
                    HandleArenaNone();
                    break;
            }
        }

        private void Init()
        {
            _hasInit = true;
            RArenaUtils.logger = logger;
            RArenaUtils.rArena = rArena;
            RArenaUtils.ecsApi = ecsApi;
            RArenaUtils.sRpc = sRpc;

            rArena.ArenaWinningConditions.Add(new RArenaLMSCondition(rArena, priority: 10));
            rArena.ArenaWinningConditions.Add(new RArenaPlayerLossCondition(rArena, ecsApi, sRpc, priority: 1, arenaHpLossCondition: EArenaLossCondition.PlayerHpBelowLimit));

            RArenaTimerCountdown.InitializeTimerStamps(30,
                onCountdown: (d) =>
                {
                    foreach (var p in rArena.Participants.Keys)
                    {
                        sRpc.SendArenaCountdown(p, (int)d);
                    }
                }
                );

            // Disqualify player  outside the bounds
            _arenaSystemsCallback += () =>
            {
                if (rArena.ArenaStatus != EArenaStatus.Ongoing) return;

                CheckPlayersOutsideArena();
            };

            // Ensure immortality for players before the match starts, this may be incompatible with other mods
            // needs more checks to ensure immortality in arena scoped cells
            _arenaSystemsCallback += HandleAutoReadySystem;
            _arenaSystemsCallback += HandleArenaStatusNotStarted;

            // Check winning condition
            _arenaSystemsCallback += HandleMatchConditions;

            // Teleport not participating players out
            _arenaSystemsCallback += HandleNotParticipatingPlayers;
        }

        private void HandleMatchConditions()
        {
            switch (rArena.ArenaMode)
            {
                case EArenaMode.ArenaFFA:
                    if (rArena.ArenaStatus == EArenaStatus.Ongoing)
                    {
                        foreach (var c in rArena.ArenaWinningConditions)
                        {
                            c.OnUpdate(_tick.DeltaTime);
                            if (c.WasFullfilled()) ConcludeArenaMatch(c);
                        }
                    }
                    break;
            }
        }

        private void HandleArenaStatusNotStarted()
        {
            if (rArena.ArenaStatus == EArenaStatus.NotStarted)
            {
                if (rArena.PlayersImmortal)
                {
                    ecsApi.QueryWithEntity<MainCharacterComponent, VitalsComponent>((ref main, ref vital, id) =>
                    {
                        if (rArena.Participants.ContainsKey(main.PlayerId))
                        {
                            //logger.LogError("Vital modified, arena status: {status}", rArena.ArenaStatus.ToString());
                            RArenaUtils.Heal(id, ref vital);
                        }
                    });
                }

                CheckStartingCondition();
            }
        }

        public void Reset()
        {
            logger.LogInformation("Arena reset invoked");
            rArena.ArenaMode = EArenaMode.ArenaNone;
            rArena.ArenaStatus = EArenaStatus.NotStarted;
            _doFFAonce = false;
            SetPlayersImmortality(true);
        }

        private void ConcludeArenaMatch(IRArenaCondition c)
        {
            switch (c)
            {
                case RArenaLMSCondition LastManStanding:
                    logger.LogInformation("{Name} has won!", LastManStanding.Winner);
                    rArena.ArenaStatus = EArenaStatus.Finished;
                    rArena.Participants.Clear();
                    rArena.ParticipantsStatusTimeouts.Clear();

                    ecsApi.Query<MainCharacterComponent>((ref main) =>
                    {
                        //sRpc.SendAnnounceFightEnded(main.PlayerId);
                        sRpc.SendAnnounceWinner(main.PlayerId, LastManStanding.Winner);
                    });

                    foreach (var condition in rArena.ArenaWinningConditions)
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
            if (rArena.Participants.Count > 1)
            {
                ecsApi.Query<MainCharacterComponent, ParentCellComponent>((ref player, ref cell) =>
                {
                    if (rArena.Participants.ContainsKey(player.PlayerId))
                    {
                        if (rArena.Participants[player.PlayerId] == EArenaParticipantStatus.NotReady) return;
                        if (cell.Kind != ParentCellKind.Interior)
                        {
                            logger.LogInformation("Setting {player} as not ready", player.PlayerId);
                            rArena.Participants[player.PlayerId] = EArenaParticipantStatus.NotReady;
                        }
                    }
                });

                int totalPlayers = rArena.Participants.Count;
                int readyPlayers = rArena.Participants.Count(p => p.Value == EArenaParticipantStatus.Ready);

                bool isMajorityReady = readyPlayers > (totalPlayers / 2);
                //bool isMajorityReady = readyPlayers >= (readyPlayers / totalPlayers);

                List<PlayerId> playersToKick = new List<PlayerId>();

                foreach (var participant in rArena.Participants)
                {
                    if (participant.Value == EArenaParticipantStatus.NotReady)
                    {
                        if (isMajorityReady)
                        {
                            RArenaTimerCountdown.Update(_arenaSystemsInterval);
                            if (!rArena.ParticipantsStatusTimeouts.ContainsKey(participant.Key))
                            {
                                rArena.ParticipantsStatusTimeouts[participant.Key] = 0f;
                            }

                            rArena.ParticipantsStatusTimeouts[participant.Key] += _arenaSystemsInterval;

                            if (rArena.ParticipantsStatusTimeouts[participant.Key] >= 30f)
                            {
                                playersToKick.Add(participant.Key);
                            }
                        }
                        else
                        {
                            RArenaTimerCountdown.ResetCountdown();
                            rArena.ParticipantsStatusTimeouts.Remove(participant.Key);
                        }
                    }
                }

                foreach (var pId in playersToKick)
                {
                    logger.LogInformation("{Player} has been timed out for waiting too long.", pId);
                    rArena.Participants.Remove(pId);
                    rArena.ParticipantsStatusTimeouts.Remove(pId);
                    sRpc?.SendArenaParticipantRegistered(pId, false, rArena.Participants.Count);
                }

                if (playersToKick.Count > 0)
                {
                    RArenaUtils.BroadcastRegistrationUpdate();
                    RArenaUtils.BroadcastReadyUpdate();
                }

                if (rArena.Participants.All(b => b.Value == EArenaParticipantStatus.Ready)
                    && rArena.ArenaStatus == EArenaStatus.NotStarted)
                {
                    Reset();
                    // TODO decide how to pick a mode
                    // File config per server?
                    // rArena.ArenaMode = EArenaMode
                    // For now FFA only
                    // Game ending conditions are sorted descending based on priority
                    // I.E LastManStandingCondition with priority 2 will end match before the timer runs out.
                    rArena.ArenaWinningConditions.Sort((a, b) => (b.Priority.CompareTo(a.Priority)));
                    rArena.ArenaMode = EArenaMode.ArenaFFA;
                    rArena.ArenaStatus = EArenaStatus.Ongoing;
                    SetPlayersStatus(EArenaParticipantStatus.Fighting);
                    foreach (var p in rArena.Participants.Keys)
                    {
                        sRpc?.SendAnnounceFightStarted(p);
                    }
                }
            }
        }

        public void HandleAutoReadySystem()
        {
            if (rArena == null) return;
            if (rArena.ArenaStatus == EArenaStatus.NotStarted)
            {
                ecsApi.Query<MainCharacterComponent, TransformComponent>((ref player, ref t) =>
                {
                    var dx = t.Position.X - RArenaCommon.Data.ArenaMiddle.X;
                    var dy = t.Position.Y - RArenaCommon.Data.ArenaMiddle.Y;

                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    //double readyRingRadius = RArenaCommon.Data.ArenaRadius / 2;
                    double readyRingRadius = RArenaCommon.Data.ArenaInnerRingRadius;

                    bool isParticipant = rArena.Participants.TryGetValue(player.PlayerId, out var status);

                    if (distance > RArenaCommon.Data.ArenaRadius || t.Position.Z > 300)
                    {
                        if (isParticipant)
                        {
                            logger.LogInformation("{player} left arena area, removing as contestant", player.PlayerId);
                            rArena.Participants.Remove(player.PlayerId);
                            rArena.ParticipantsStatusTimeouts.Remove(player.PlayerId);
                            sRpc?.SendArenaParticipantRegistered(player.PlayerId, false, rArena.Participants.Count);
                            RArenaUtils.BroadcastRegistrationUpdate();
                            RArenaUtils.BroadcastReadyUpdate();
                        }
                    }
                    else if (distance <= RArenaCommon.Data.ArenaRadius && distance > readyRingRadius)
                    {
                        if (!isParticipant)
                        {
                            logger.LogInformation("{player} entered arena area, adding as contestant", player.PlayerId);
                            rArena.Participants[player.PlayerId] = EArenaParticipantStatus.NotReady;
                            sRpc?.SendArenaParticipantRegistered(player.PlayerId, true, rArena.Participants.Count);
                            RArenaUtils.BroadcastRegistrationUpdate();
                        }
                        else if (status != EArenaParticipantStatus.NotReady)
                        {
                            logger.LogInformation("{player} left inner ring, marking as not ready", player.PlayerId);
                            rArena.Participants[player.PlayerId] = EArenaParticipantStatus.NotReady;
                            sRpc?.SendArenaParticipantReady(player.PlayerId, false, rArena.Participants.Count(p => p.Value == EArenaParticipantStatus.Ready));
                            RArenaUtils.BroadcastReadyUpdate();
                        }
                    }
                    else if (distance <= readyRingRadius)
                    {
                        if (isParticipant && status != EArenaParticipantStatus.Ready)
                        {
                            logger.LogInformation("{player} has entered inner ring, marking as ready", player.PlayerId);
                            rArena.Participants[player.PlayerId] = EArenaParticipantStatus.Ready;
                            sRpc?.SendArenaParticipantReady(player.PlayerId, true, rArena.Participants.Count(p => p.Value == EArenaParticipantStatus.Ready));
                            RArenaUtils.BroadcastReadyUpdate();
                        }
                    }
                });
            }
        }

        public void HandleNotParticipatingPlayers()
        {
            if (rArena.ArenaStatus == EArenaStatus.Ongoing)
            {
                ecsApi.QueryWithEntity<MainCharacterComponent, VitalsComponent>((ref main, ref vital, id) =>
                {
                    ref var t = ref ecsApi.GetComponentRef<TransformComponent>(id);
                    ref var cell = ref ecsApi.GetComponentRef<ParentCellComponent>(id);
                    //if (!rArena.Participants.ContainsKey(main.PlayerId)) return;
                    if (cell.Kind == ParentCellKind.Exterior) return;
                    //logger.LogWarning(rArena.PlayersAreas.Values.ToString());
                    if (!rArena.PlayersAreas[main.PlayerId].Contains(RArenaCommon.Data.ArenaCellId)) return;
                    if (!rArena.Participants.ContainsKey(main.PlayerId) || rArena.Participants[main.PlayerId] == EArenaParticipantStatus.Loss)
                    {
                        if (t.Position.Z < 210)
                        {
                            RArenaUtils.Teleport(id, ref t, RArenaCommon.Data.ArenaSpectatorStand);
                        }
                        RArenaUtils.Heal(id, ref vital);
                    }
                });
            }
        }

        public void HandleArenaNone()
        {
            if (rArena.ArenaStatus == EArenaStatus.Ongoing)
            {
                Reset();
                logger.LogWarning("Match started in invalid mode, reset invoked!");
                return;
            }
        }

        public void HandleArenaFFA()
        {
            if (rArena.ArenaMode != EArenaMode.ArenaFFA)
            {
                Reset();
                logger.LogInformation("FFA Match started in invalid mode, reset invoked!");
                return;
            }

            if (!_doFFAonce)
            {
                _doFFAonce = true;
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
            rArena.Participants[playerId] = EArenaParticipantStatus.Loss;
            foreach (var p in rArena.Participants.Keys)
            {
                sRpc.SendPlayerEliminated(p, playerId);
            }
        }

        public void CheckPlayersOutsideArena()
        {
            ecsApi.Query<MainCharacterComponent, TransformComponent, ParentCellComponent>((ref player, ref t, ref cell) =>
            {
                if (rArena.Participants.TryGetValue(player.PlayerId, out var status) && status == EArenaParticipantStatus.Fighting)
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

            List<VitalsComponent> playersVitals = new List<VitalsComponent>();
            ecsApi.QueryWithEntity<VitalsComponent>((ref vital, id) =>
            {
                if (enabled)
                {
                    //logger.LogError("Vital modified, arena status: {status}", rArena.ArenaStatus.ToString());
                    // vital.Hp = vital.HpBase;
                    RArenaUtils.Heal(id, ref vital);
                }
            });

            rArena.PlayersImmortal = enabled;
        }

        public void SetPlayersStatus(EArenaParticipantStatus status)
        {
            lock (rArena.Participants)
            {
                foreach (var p in rArena.Participants.Keys)
                {
                    rArena.Participants[p] = status;

                }
            }
        }
    }
}
