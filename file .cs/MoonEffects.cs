using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoonMod
{
    public partial class MoonMod
    {
        private void ActivateGiftDisarm()
        {
            LoadGiftTimerConfigFromIni();

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftDisarmDurationMs
                );

            // Gift normal tetap menambah durasi jika masih aktif.
            // Random Chaos selalu memakai durasi tepat dari
            // RandomChaosXDuration.
            if (_modeDurationOverrideMs > 0)
            {
                _giftDisarmEndTime =
                    currentTime +
                    activeDurationMs;
            }
            else
            {
                _giftDisarmEndTime =
                    currentTime <
                    _giftDisarmEndTime
                        ? _giftDisarmEndTime +
                          activeDurationMs
                        : currentTime +
                          activeDurationMs;
            }

            ApplyChaosNoWeapon(
                player
            );

            ShowHudNotification(
                "~r~DISARM ACTIVE! ~w~" +
                (activeDurationMs / 1000) +
                " SEC"
            );
        }

        private void ProcessGiftDisarm(
            int currentTime)
        {
            if (_giftDisarmEndTime <= 0)
                return;

            Ped player =
                Game.Player.Character;

            if (currentTime <
                _giftDisarmEndTime)
            {
                if (player != null &&
                    player.Exists() &&
                    !player.IsDead &&
                    _playerAnimalTransformPhase != 2 &&
                    _playerAnimalTransformPhase != 3)
                {
                    ApplyChaosNoWeapon(
                        player
                    );
                }

                return;
            }

            _giftDisarmEndTime =
                0;

            // Jangan restore jika Random Chaos DISARM masih aktif.
            if (IsActiveRandomChaosEffect(
                    "disarm"
                ))
            {
                return;
            }

            // Kalau masih animal, weapon direstore nanti saat kembali human.
            if (_playerAnimalTransformPhase == 2 ||
                _playerAnimalTransformPhase == 3)
            {
                return;
            }

            GiveDefaultPlayerWeapons();

            ShowHudNotification(
                "~g~DISARM ENDED! ~w~Weapons restored."
            );
        }

        private int GetSleepVehicleDoorIndex(
            Vehicle vehicle,
            Ped player)
        {
            if (vehicle == null ||
                !vehicle.Exists() ||
                player == null ||
                !player.Exists())
            {
                return 0;
            }

            // GTA seat index:
            // -1 = driver
            //  0 = front passenger
            //  1 = left rear
            //  2 = right rear
            int[] seatIndexes =
            {
                -1,
                0,
                1,
                2
            };

            int[] doorIndexes =
            {
                0, // front left / driver
                1, // front right
                2, // rear left
                3  // rear right
            };

            for (int i = 0;
                 i < seatIndexes.Length;
                 i++)
            {
                int occupantHandle =
                    Function.Call<int>(
                        Hash.GET_PED_IN_VEHICLE_SEAT,
                        vehicle.Handle,
                        seatIndexes[i],
                        false
                    );

                if (occupantHandle ==
                    player.Handle)
                {
                    return
                        doorIndexes[i];
                }
            }

            // Fallback driver door.
            return 0;
        }

        private void StartSleepVehicleFallOut(
            Ped player,
            Vehicle vehicle,
            int currentTime)
        {
            if (player == null ||
                !player.Exists() ||
                vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            int doorIndex =
                GetSleepVehicleDoorIndex(
                    vehicle,
                    player
                );

            // PINTU DIBUKA OTOMATIS / INSTANT.
            // Jadi player tidak menjalankan animasi tangan membuka pintu.
            Function.Call(
                Hash.SET_VEHICLE_DOOR_OPEN,
                vehicle.Handle,
                doorIndex,
                false,
                true
            );

            Function.Call(
                Hash.SET_PED_CAN_RAGDOLL,
                player.Handle,
                true
            );

            // 4160 = bail-out / jatuh keluar.
            // Karena pintu sudah dibuka instant oleh script,
            // player langsung keluar/jatuh dari kursi.
            Function.Call(
                Hash.TASK_LEAVE_VEHICLE,
                player.Handle,
                vehicle.Handle,
                4160
            );

            _giftSleepVehicleBailoutPending =
                true;

            _giftSleepVehicleBailoutRetryTime =
                currentTime +
                SLEEP_VEHICLE_BAILOUT_RETRY_MS;
        }

        private void ActivateSleepGift()
        {
            if (_customDeathActive)
                return;

            LoadGiftTimerConfigFromIni();

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftSleepDurationMs
                );

            if (_modeDurationOverrideMs > 0)
            {
                _giftSleepEndTime =
                    currentTime +
                    activeDurationMs;
            }
            else
            {
                _giftSleepEndTime =
                    currentTime <
                    _giftSleepEndTime
                        ? _giftSleepEndTime +
                          activeDurationMs
                        : currentTime +
                          activeDurationMs;
            }

            // Kalau player sedang di kendaraan:
            // pintu yang sesuai kursinya dibuka OTOMATIS / INSTANT,
            // lalu player bail-out/jatuh dari kursi.
            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle != null &&
                    vehicle.Exists())
                {
                    StartSleepVehicleFallOut(
                        player,
                        vehicle,
                        currentTime
                    );
                }
            }
            else
            {
                _giftSleepVehicleBailoutPending =
                    false;

                _giftSleepVehicleBailoutRetryTime =
                    0;

                Function.Call(
                    Hash.SET_PED_TO_RAGDOLL,
                    player.Handle,
                    1200,
                    1200,
                    0,
                    true,
                    true,
                    false
                );
            }

            ShowHudNotification(
                "~b~GO TO SLEEP! ~w~" +
                (activeDurationMs / 1000) +
                " SEC"
            );
        }

        private void ProcessSleepGift(
            int currentTime)
        {
            if (_giftSleepEndTime <= 0)
                return;

            // Custom death punya sistem ragdoll sendiri.
            // Jangan biarkan SLEEP bertabrakan dengan custom death.
            if (_customDeathActive)
            {
                _giftSleepEndTime =
                    0;

                _giftSleepVehicleBailoutPending =
                    false;

                _giftSleepVehicleBailoutRetryTime =
                    0;

                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                _giftSleepEndTime =
                    0;

                _giftSleepVehicleBailoutPending =
                    false;

                return;
            }

            if (currentTime >=
                _giftSleepEndTime)
            {
                _giftSleepEndTime =
                    0;

                _giftSleepVehicleBailoutPending =
                    false;

                _giftSleepVehicleBailoutRetryTime =
                    0;

                Function.Call(
                    Hash.CLEAR_PED_TASKS,
                    player.Handle
                );

                ShowHudNotification(
                    "~g~WAKE UP!"
                );

                return;
            }

            // Lock kontrol utama supaya player tidak bisa bangun / bergerak
            // sebelum timer sleep selesai.
            int[] disabledControls =
            {
                21, // sprint
                22, // jump
                23, // enter vehicle
                24, // attack
                25, // aim
                30, // move left/right
                31, // move forward/back
                75  // exit vehicle
            };

            foreach (
                int control in
                disabledControls)
            {
                Function.Call(
                    Hash.DISABLE_CONTROL_ACTION,
                    0,
                    control,
                    true
                );
            }

            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle != null &&
                    vehicle.Exists() &&
                    (
                        !_giftSleepVehicleBailoutPending ||
                        currentTime >=
                            _giftSleepVehicleBailoutRetryTime
                    ))
                {
                    StartSleepVehicleFallOut(
                        player,
                        vehicle,
                        currentTime
                    );
                }

                return;
            }

            // Sudah benar-benar jatuh keluar dari kendaraan:
            // lanjutkan ragdoll selama timer SLEEP aktif.
            _giftSleepVehicleBailoutPending =
                false;

            _giftSleepVehicleBailoutRetryTime =
                0;

            Function.Call(
                Hash.SET_PED_TO_RAGDOLL,
                player.Handle,
                1000,
                1000,
                0,
                true,
                true,
                false
            );
        }

        private void RequestRockRainBallModels(
            int currentTime)
        {
            if (_rockRainModelRequestStartTime <= 0)
            {
                _rockRainModelRequestStartTime =
                    currentTime;
            }

            for (
                int i = 0;
                i < _rockRainBallProfiles.Count;
                i++)
            {
                RockRainBallProfile profile =
                    _rockRainBallProfiles[i];

                Model model =
                    new Model(
                        profile.ModelName
                    );

                if (!model.IsValid)
                {
                    continue;
                }

                if (!model.IsLoaded)
                {
                    model.Request();
                }
            }
        }

        private bool HasAnyValidRockRainBallModel()
        {
            for (
                int i = 0;
                i < _rockRainBallProfiles.Count;
                i++)
            {
                Model model =
                    new Model(
                        _rockRainBallProfiles[i].ModelName
                    );

                if (model.IsValid)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyLoadedRockRainBallModel()
        {
            for (
                int i = 0;
                i < _rockRainBallProfiles.Count;
                i++)
            {
                Model model =
                    new Model(
                        _rockRainBallProfiles[i].ModelName
                    );

                if (model.IsValid &&
                    model.IsLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        private void ActivateRockRainGift()
        {
            if (_customDeathActive)
                return;

            LoadGiftTimerConfigFromIni();

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftRockRainDurationMs
                );

            _rockRainEndTime =
                currentTime +
                activeDurationMs;

            _nextRockRainSpawnTime =
                currentTime;

            _rockRainBallCycleIndex =
                0;

            _rockRainModelRequestStartTime =
                currentTime;

            if (!HasAnyValidRockRainBallModel())
            {
                ShowHudNotification(
                    "~r~[BALL RAIN ERROR]~w~ Semua model bola tidak valid."
                );

                _rockRainEndTime =
                    0;

                return;
            }

            RequestRockRainBallModels(
                currentTime
            );

            ShowHudNotification(
                "~o~BALL RAIN! ~w~" +
                (activeDurationMs / 1000) +
                " SEC"
            );
        }

        private bool TryGetNextRockRainBall(
            out RockRainBallProfile selectedProfile,
            out Model selectedModel)
        {
            selectedProfile =
                null;

            selectedModel =
                default(Model);

            int total =
                _rockRainBallProfiles.Count;

            if (total <= 0)
                return false;

            for (
                int attempt = 0;
                attempt < total;
                attempt++)
            {
                int index =
                    _rockRainBallCycleIndex %
                    total;

                _rockRainBallCycleIndex =
                    (_rockRainBallCycleIndex + 1) %
                    total;

                RockRainBallProfile profile =
                    _rockRainBallProfiles[index];

                Model model =
                    new Model(
                        profile.ModelName
                    );

                if (!model.IsValid)
                {
                    continue;
                }

                if (!model.IsLoaded)
                {
                    model.Request();
                    continue;
                }

                selectedProfile =
                    profile;

                selectedModel =
                    model;

                return true;
            }

            return false;
        }

        private void SpawnRockRainProp(
            Ped player,
            int currentTime)
        {
            if (player == null ||
                !player.Exists())
            {
                return;
            }

            RockRainBallProfile profile;
            Model ballModel;

            if (!TryGetNextRockRainBall(
                    out profile,
                    out ballModel))
            {
                return;
            }

            float angle =
                (float)(
                    _random.NextDouble() *
                    Math.PI *
                    2.0
                );

            // Area dekat player supaya ada peluang nyata mengenai player,
            // tetapi tetap menyebar seperti hujan.
            float radius =
                0.5f +
                (float)_random.NextDouble() *
                8.0f;

            float targetX =
                player.Position.X +
                (float)Math.Cos(angle) *
                radius;

            float targetY =
                player.Position.Y +
                (float)Math.Sin(angle) *
                radius;

            float groundProbeZ =
                Math.Max(
                    player.Position.Z + 80.0f,
                    1000.0f
                );

            float groundZ =
                GetGroundHeightCompat(
                    new Vector3(
                        targetX,
                        targetY,
                        groundProbeZ
                    )
                );

            if (groundZ == 0.0f &&
                player.Position.Z > 5.0f)
            {
                groundZ =
                    player.Position.Z -
                    1.0f;
            }

            float spawnHeight =
                14.0f +
                (float)_random.NextDouble() *
                10.0f;

            Vector3 spawnPos =
                new Vector3(
                    targetX,
                    targetY,
                    Math.Max(
                        player.Position.Z,
                        groundZ
                    ) +
                    spawnHeight
                );

            Prop ball =
                Prop.Create(
                    ballModel,
                    spawnPos,
                    true,
                    false
                );

            if (ball == null ||
                !ball.Exists())
            {
                return;
            }

            Function.Call(
                Hash.SET_ENTITY_DYNAMIC,
                ball.Handle,
                true
            );

            Function.Call(
                Hash.SET_ENTITY_COLLISION,
                ball.Handle,
                true,
                true
            );

            Function.Call(
                Hash.SET_ENTITY_HAS_GRAVITY,
                ball.Handle,
                true
            );

            float driftX =
                -1.0f +
                (float)_random.NextDouble() *
                2.0f;

            float driftY =
                -1.0f +
                (float)_random.NextDouble() *
                2.0f;

            // Bola besar sedikit lebih lambat supaya collision engine
            // lebih stabil dan impact terlihat jelas.
            float sizeSpeedReduction =
                Math.Min(
                    5.0f,
                    profile.GroundOffset *
                    1.1f
                );

            float fallSpeed =
                15.0f +
                (float)_random.NextDouble() *
                6.0f -
                sizeSpeedReduction;

            fallSpeed =
                Math.Max(
                    8.0f,
                    fallSpeed
                );

            Function.Call(
                Hash.SET_ENTITY_VELOCITY,
                ball.Handle,
                driftX,
                driftY,
                -fallSpeed
            );

            _rockRainProps.Add(
                new RockRainPropTracker
                {
                    Rock =
                        ball,

                    Profile =
                        profile,

                    GroundZ =
                        groundZ,

                    HasHitGround =
                        false,

                    PhysicsTrackingEndTime =
                        currentTime +
                        8000
                }
            );

            ScheduleEntityDespawn(
                ball,
                BALL_RAIN_DESPAWN_MS
            );
        }

        private void ProcessRockRainGroundPhysics(
            int currentTime)
        {
            for (
                int i =
                    _rockRainProps.Count - 1;
                i >= 0;
                i--)
            {
                RockRainPropTracker tracker =
                    _rockRainProps[i];

                Prop ball =
                    tracker.Rock;

                if (ball == null ||
                    !ball.Exists() ||
                    tracker.Profile == null)
                {
                    continue;
                }

                float contactZ =
                    tracker.GroundZ +
                    tracker.Profile.GroundOffset;

                Vector3 pos =
                    ball.Position;

                if (!tracker.HasHitGround)
                {
                    if (pos.Z <=
                        contactZ + 0.40f)
                    {
                        Vector3 velocity =
                            ball.Velocity;

                        ball.Position =
                            new Vector3(
                                pos.X,
                                pos.Y,
                                contactZ
                            );

                        // Semakin besar bola, semakin kecil bounce vertikalnya.
                        float bounceZ =
                            Math.Max(
                                0.35f,
                                1.25f -
                                tracker.Profile.GroundOffset *
                                0.18f
                            );

                        Function.Call(
                            Hash.SET_ENTITY_VELOCITY,
                            ball.Handle,
                            velocity.X * 0.45f,
                            velocity.Y * 0.45f,
                            bounceZ
                        );

                        tracker.HasHitGround =
                            true;

                        tracker.PhysicsTrackingEndTime =
                            Math.Max(
                                tracker.PhysicsTrackingEndTime,
                                currentTime + 3000
                            );
                    }

                    continue;
                }

                if (pos.Z <
                    tracker.GroundZ +
                    Math.Max(
                        0.05f,
                        tracker.Profile.GroundOffset * 0.45f
                    ))
                {
                    ball.Position =
                        new Vector3(
                            pos.X,
                            pos.Y,
                            contactZ
                        );

                    Vector3 velocity =
                        ball.Velocity;

                    Function.Call(
                        Hash.SET_ENTITY_VELOCITY,
                        ball.Handle,
                        velocity.X * 0.60f,
                        velocity.Y * 0.60f,
                        0.0f
                    );
                }
            }
        }

        private void CleanupExpiredRockRainProps(
            int currentTime)
        {
            for (
                int i =
                    _rockRainProps.Count - 1;
                i >= 0;
                i--)
            {
                RockRainPropTracker tracker =
                    _rockRainProps[i];

                Prop ball =
                    tracker.Rock;

                // Entity sudah dihapus oleh internal despawn
                // atau tidak valid lagi -> buang tracker lokal saja.
                if (ball == null ||
                    !ball.Exists())
                {
                    _rockRainProps.RemoveAt(
                        i
                    );

                    continue;
                }

                // Setelah bola sudah menyentuh ground dan physics guard
                // dipantau beberapa detik, tracker lokal boleh dilepas.
                // Entity bolanya TIDAK dihapus di sini.
                if (tracker.HasHitGround &&
                    currentTime >=
                        tracker.PhysicsTrackingEndTime)
                {
                    _rockRainProps.RemoveAt(
                        i
                    );
                }
            }
        }

        private void StopRockRainGift(
            bool showEndedNotification = false)
        {
            _rockRainEndTime =
                0;

            _nextRockRainSpawnTime =
                0;

            // JANGAN delete bola di sini.
            // Bola tetap di world dan timer hapusnya sepenuhnya
            // diatur oleh internal timer hardcoded 60 detik.

            for (
                int i = 0;
                i < _rockRainBallProfiles.Count;
                i++)
            {
                Model model =
                    new Model(
                        _rockRainBallProfiles[i].ModelName
                    );

                if (model.IsValid)
                {
                    model.MarkAsNoLongerNeeded();
                }
            }

            _rockRainModelRequestStartTime =
                0;

            _rockRainBallCycleIndex =
                0;

            if (showEndedNotification)
            {
                ShowHudNotification(
                    "~g~BALL RAIN ENDED!"
                );
            }
        }

        private void ProcessRockRainGift(
            int currentTime)
        {
            ProcessRockRainGroundPhysics(
                currentTime
            );

            CleanupExpiredRockRainProps(
                currentTime
            );

            if (_rockRainEndTime <= 0)
                return;

            if (_customDeathActive)
            {
                StopRockRainGift();
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                StopRockRainGift();
                return;
            }

            if (currentTime >=
                _rockRainEndTime)
            {
                StopRockRainGift(
                    showEndedNotification: true
                );

                return;
            }

            RequestRockRainBallModels(
                currentTime
            );

            if (!HasAnyLoadedRockRainBallModel())
            {
                if (
                    currentTime -
                    _rockRainModelRequestStartTime >=
                    MODEL_LOAD_TIMEOUT_MS
                )
                {
                    ShowHudNotification(
                        "~r~[BALL RAIN ERROR]~w~ Tidak ada model bola yang berhasil load."
                    );

                    StopRockRainGift();
                }

                return;
            }

            if (currentTime <
                _nextRockRainSpawnTime)
            {
                return;
            }

            _nextRockRainSpawnTime =
                currentTime +
                ROCK_RAIN_SPAWN_INTERVAL_MS;

            SpawnRockRainProp(
                player,
                currentTime
            );
        }

        private void ScheduleEntityDespawn(
            Entity entity,
            int delayMs)
        {
            if (entity == null ||
                !entity.Exists())
            {
                return;
            }

            int handle =
                entity.Handle;

            for (
                int i = 0;
                i < _scheduledDespawns.Count;
                i++)
            {
                ScheduledDespawnTracker existing =
                    _scheduledDespawns[i];

                if (existing != null &&
                    existing.EntityHandle ==
                        handle)
                {
                    return;
                }
            }

            _scheduledDespawns.Add(
                new ScheduledDespawnTracker
                {
                    Entity =
                        entity,

                    EntityHandle =
                        handle,

                    DeleteAtTime =
                        Game.GameTime +
                        Math.Max(
                            1,
                            delayMs
                        )
                }
            );
        }

        private void ProcessScheduledDespawns(
            int currentTime)
        {
            for (
                int i =
                    _scheduledDespawns.Count - 1;
                i >= 0;
                i--)
            {
                ScheduledDespawnTracker tracker =
                    _scheduledDespawns[i];

                if (tracker == null ||
                    tracker.Entity == null ||
                    !tracker.Entity.Exists())
                {
                    _scheduledDespawns.RemoveAt(
                        i
                    );

                    continue;
                }

                if (!HasGameTimeReached(
                        currentTime,
                        tracker.DeleteAtTime
                    ))
                {
                    continue;
                }

                try
                {
                    if (tracker.Entity.Exists())
                    {
                        tracker.Entity.Delete();
                    }
                }
                catch
                {
                }

                _scheduledDespawns.RemoveAt(
                    i
                );
            }
        }

        private void DeleteAllScheduledDespawns()
        {
            for (
                int i =
                    _scheduledDespawns.Count - 1;
                i >= 0;
                i--)
            {
                ScheduledDespawnTracker tracker =
                    _scheduledDespawns[i];

                try
                {
                    if (tracker != null &&
                        tracker.Entity != null &&
                        tracker.Entity.Exists())
                    {
                        tracker.Entity.Delete();
                    }
                }
                catch
                {
                }
            }

            _scheduledDespawns.Clear();
        }

        private string NormalizeRandomGatchaEffectKey(
            string rawKey)
        {
            return
                NormalizeConfigurableRandomEffectKey(
                    rawKey
                );
        }

        private string GetRandomGatchaDisplayName(
            string key)
        {
            return
                GetConfigurableRandomEffectDisplayName(
                    key
                );
        }

        private bool HasVehicleRouteAccess(
            Vector3 fromPosition,
            Vector3 targetPosition)
        {
            if (targetPosition == Vector3.Zero)
                return false;

            if (fromPosition == Vector3.Zero)
                return true;

            try
            {
                float travelDistance =
                    Function.Call<float>(
                        Hash.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS,
                        fromPosition.X,
                        fromPosition.Y,
                        fromPosition.Z,
                        targetPosition.X,
                        targetPosition.Y,
                        targetPosition.Z
                    );

                // Native biasanya mengembalikan nilai sangat besar ketika
                // tidak menemukan route kendaraan yang valid.
                if (float.IsNaN(travelDistance) ||
                    float.IsInfinity(travelDistance) ||
                    travelDistance <= 1.0f ||
                    travelDistance >=
                        ROUTE_MAX_TRAVEL_DISTANCE_SENTINEL)
                {
                    return false;
                }

                float straightDistance =
                    fromPosition.DistanceTo(
                        targetPosition
                    );

                float maxReasonableTravelDistance =
                    Math.Max(
                        ROUTE_MAX_DETOUR_MINIMUM,
                        straightDistance *
                            ROUTE_MAX_DETOUR_MULTIPLIER
                    );

                // Cegah node yang secara garis lurus dekat tetapi route
                // kendaraan harus memutar sangat ekstrem karena pagar /
                // compound tertutup. Batas dibuat longgar agar jalan gunung
                // dan highway normal tetap diterima.
                if (travelDistance >
                    maxReasonableTravelDistance)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                // Jika native travel-distance gagal pada build tertentu,
                // jangan matikan seluruh route generator. Restricted-area
                // blacklist + road-node check tetap aktif sebagai fallback.
                return true;
            }
        }

        private void RecordEnemyKill(
            Ped enemy,
            Ped player)
        {
            if (enemy == null ||
                !enemy.Exists() ||
                player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            int killerHandle =
                Function.Call<int>(
                    Hash.GET_PED_SOURCE_OF_DEATH,
                    enemy.Handle
                );

            bool killedByPlayer =
                killerHandle ==
                player.Handle;

            if (!killedByPlayer &&
                player.IsInVehicle())
            {
                Vehicle playerVehicle =
                    player.CurrentVehicle;

                if (playerVehicle != null &&
                    playerVehicle.Exists() &&
                    killerHandle ==
                    playerVehicle.Handle)
                {
                    killedByPlayer =
                        true;
                }
            }

            if (!killedByPlayer)
                return;

            // Tetap simpan statistik kill host saja.
            _enemyKillCount++;
            MarkCurrentHostProfileDirty();
        }

        private string GetInstantEnemyDisplayName(
            CustomNpcConfig config)
        {
            if (config == null)
                return "ENEMY";

            string webhook =
                config.WebhookName ?? string.Empty;

            if (webhook.StartsWith(
                    "spawn_",
                    StringComparison.OrdinalIgnoreCase))
            {
                webhook =
                    webhook.Substring(
                        "spawn_".Length
                    );
            }

            webhook =
                webhook
                    .Replace("_", " ")
                    .Trim();

            if (!string.IsNullOrWhiteSpace(webhook))
            {
                return webhook.ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(
                    config.DefaultName))
            {
                return config.DefaultName
                    .Trim()
                    .ToUpperInvariant();
            }

            return "ENEMY";
        }

        private string GetInstantBodyguardDisplayName(
            CustomNpcConfig config)
        {
            if (config == null)
                return "BODYGUARD";

            if (!string.IsNullOrWhiteSpace(
                    config.DefaultName))
            {
                return config.DefaultName
                    .Trim()
                    .ToUpperInvariant();
            }

            string webhook =
                config.WebhookName ?? string.Empty;

            if (webhook.StartsWith(
                    "spawn_bodyguard_",
                    StringComparison.OrdinalIgnoreCase))
            {
                webhook =
                    webhook.Substring(
                        "spawn_bodyguard_".Length
                    );
            }
            else if (webhook.StartsWith(
                    "spawn_",
                    StringComparison.OrdinalIgnoreCase))
            {
                webhook =
                    webhook.Substring(
                        "spawn_".Length
                    );
            }

            webhook =
                webhook
                    .Replace("_", " ")
                    .Trim();

            return string.IsNullOrWhiteSpace(webhook)
                ? "BODYGUARD"
                : webhook.ToUpperInvariant();
        }

        private string GetInstantAnimalDisplayName(
            CustomNpcConfig config)
        {
            if (config == null)
                return "ANIMAL";

            // Jika hanya ada satu model animal, nama model khusus adalah
            // nama paling akurat untuk Instant HUD (contoh MOUNTAIN LION).
            if (config.PedModelDisplayNames != null &&
                config.PedModelDisplayNames.Count == 1 &&
                !string.IsNullOrWhiteSpace(
                    config.PedModelDisplayNames[0]))
            {
                return config.PedModelDisplayNames[0]
                    .Trim()
                    .ToUpperInvariant();
            }

            // Jika pool animal berisi beberapa model random, gunakan nama
            // kategori/fallback agar UI tidak mengklaim model yang belum dipilih.
            if (!string.IsNullOrWhiteSpace(
                    config.DefaultName))
            {
                return config.DefaultName
                    .Trim()
                    .ToUpperInvariant();
            }

            string webhook =
                config.WebhookName ?? string.Empty;

            if (webhook.StartsWith(
                    "spawn_",
                    StringComparison.OrdinalIgnoreCase))
            {
                webhook =
                    webhook.Substring(
                        "spawn_".Length
                    );
            }

            webhook =
                webhook
                    .Replace("_", " ")
                    .Trim();

            return string.IsNullOrWhiteSpace(webhook)
                ? "ANIMAL"
                : webhook.ToUpperInvariant();
        }

        private void RefreshTeleportGachaPoolGlobal()
        {
            if (_teleportGachaPool == null)
            {
                _teleportGachaPool =
                    new List<TeleportLocationItem>();
            }

            if (_teleportGachaPool.Count == 0 &&
                _startPosition != Vector3.Zero)
            {
                _teleportGachaPool.Add(
                    new TeleportLocationItem
                    {
                        DisplayName =
                            "CURRENT ROUND START",

                        TargetPosition =
                            _startPosition,

                        ManualZ =
                            _startPosition.Z
                    }
                );
            }

            _isTeleportGachaActive =
                false;

            _teleportGachaLocked =
                false;

            _currentTeleportGachaIndex =
                0;
        }

        private void InitializeTeleportGachaPool()
        {
            _teleportGachaPool =
                new List<TeleportLocationItem>();

            string iniPath =
                "scripts/MoonEffectConfig.ini";

            if (File.Exists(iniPath))
            {
                try
                {
                    string[] lines =
                        File.ReadAllLines(
                            iniPath
                        );

                    string currentSection =
                        "";

                    foreach (string line in lines)
                    {
                        string trimmed =
                            line.Trim();

                        if (string.IsNullOrEmpty(trimmed) ||
                            trimmed.StartsWith(";") ||
                            trimmed.StartsWith("#"))
                        {
                            continue;
                        }

                        if (trimmed.StartsWith("[") &&
                            trimmed.EndsWith("]"))
                        {
                            currentSection =
                                trimmed.Substring(
                                    1,
                                    trimmed.Length - 2
                                )
                                .Trim();

                            continue;
                        }

                        if (!IsTeleportLocationSection(
                                currentSection
                            ) ||
                            !trimmed.Contains("="))
                        {
                            continue;
                        }

                        string[] keyVal =
                            trimmed.Split(
                                new[] { '=' },
                                2
                            );

                        string key =
                            keyVal[0].Trim();

                        string[] parts =
                            keyVal[1]
                                .Trim()
                                .Split(',');

                        if (parts.Length < 3)
                            continue;

                        if (!float.TryParse(
                                parts[0].Trim(),
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out float x
                            ) ||
                            !float.TryParse(
                                parts[1].Trim(),
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out float y
                            ) ||
                            !float.TryParse(
                                parts[2].Trim(),
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out float z
                            ))
                        {
                            continue;
                        }

                        _teleportGachaPool.Add(
                            new TeleportLocationItem
                            {
                                DisplayName =
                                    key.ToUpperInvariant(),

                                TargetPosition =
                                    new Vector3(
                                        x,
                                        y,
                                        z
                                    ),

                                ManualZ =
                                    z
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    LogError(
                        "TELEPORT LOCATION ERROR",
                        ex
                    );

                    ShowHudNotification(
                        "~r~[TELEPORT LOCATION ERROR]~w~ " +
                        ex.Message,
                        false
                    );
                }
            }

            RefreshTeleportGachaPoolGlobal();
        }

        private void StartHttpServer()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(
                    $"http://127.0.0.1:{_webhookPort}/"
                );
                _listener.Start();
                Task.Run(() => ListenForWebhooks());
            }
            catch (Exception ex)
            {
                LogError(
                    "WEBHOOK START ERROR",
                    ex
                );

                ShowHudNotification(
                    "Webhook Error: " + ex.Message,
                    false
                );
            }
        }

        private async Task ListenForWebhooks()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    HttpListenerContext context =
                        await _listener.GetContextAsync();

                    HttpListenerRequest request =
                        context.Request;

                    // =========================================================
                    // AMBIL ENDPOINT DARI URL
                    //
                    // Contoh:
                    // spawn_enemy
                    // spawn_enemy:2
                    // spawn_enemy:5
                    // =========================================================
                    string rawEndpoint =
                        request.Url.AbsolutePath
                            .Trim('/')
                            .ToLower();

                    string endpoint =
                        rawEndpoint;

                    // Default tanpa :angka = 1x spawn
                    int spawnCount = 1;

                    // =========================================================
                    // BACA MULTIPLIER ":JUMLAH"
                    //
                    // spawn_enemy     = 1x
                    // spawn_enemy:2   = 2x
                    // spawn_enemy:10  = 10x
                    //
                    // Maksimal 25 untuk endpoint entity (NPC/BODYGUARD/ANIMAL)
                    // =========================================================
                    int lastColonIndex =
                        rawEndpoint.LastIndexOf(':');

                    if (lastColonIndex > 0 &&
                        lastColonIndex < rawEndpoint.Length - 1)
                    {
                        string endpointWithoutMultiplier =
                            rawEndpoint.Substring(
                                0,
                                lastColonIndex
                            );

                        string multiplierText =
                            rawEndpoint.Substring(
                                lastColonIndex + 1
                            );

                        if (int.TryParse(
                                multiplierText,
                                out int parsedCount) &&
                            parsedCount >= 1)
                        {
                            endpoint =
                                endpointWithoutMultiplier;

                            spawnCount =
                                Math.Min(
                                    parsedCount,
                                    25
                                );
                        }
                    }

                    bool endpointRecognized = true;

                    // Built-in effect tetap satu action per request. Multiplier
                    // :2..:25 memang digunakan untuk NPC/BODYGUARD/ANIMAL.
                    // responseCount harus melaporkan jumlah action yang benar-
                    // benar dijadwalkan, bukan angka suffix mentah.
                    int responseCount = 1;

                    // =========================================================
                    // WEBHOOK ACTION
                    // =========================================================
                    if (endpoint == "flip")
                    {
                        QueueMainThreadAction(endpoint, () =>
                        {
                            ShowInstantGiftHud(
                                _instantPlayerFlipLabel,
                                false
                            );

                            ActivatePlayerFlip();
                        });
                    }
                    else if (endpoint == "falling_vehicles")
                    {
                        QueueMainThreadAction(endpoint,
                            TriggerFallingVehicles
                        );
                    }
                    else if (endpoint == "give_vehicle")
                    {
                        QueueMainThreadAction(endpoint,
                            TriggerGiveVehicle
                        );
                    }
                    else if (endpoint == "destroy_car")
                    {
                        QueueMainThreadAction(endpoint, () =>
                        {
                            ShowInstantGiftHud(
                                _instantDestroyCarLabel,
                                false
                            );

                            ActivateDestroyCarGift();
                        });
                    }
                    else if (endpoint == "cage")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateGiftCage
                        );
                    }
                    else if (endpoint == "disarm")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateGiftDisarm
                        );
                    }
                    else if (endpoint == "sleep")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateSleepGift
                        );
                    }
                    else if (endpoint == "ball_rain")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateRockRainGift
                        );
                    }
                    else if (endpoint == "transform_animal")
                    {
                        QueueMainThreadAction(endpoint,
                            TriggerPlayerAnimalTransform
                        );
                    }
                    else if (endpoint == "superspeed")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateSuperSpeed
                        );
                    }
                    else if (endpoint == "invincible")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateInvincible
                        );
                    }
                    else if (endpoint == "random_teleport")
                    {
                        QueueMainThreadAction(endpoint, () =>
                        {
                            ShowInstantGiftHud(
                                _instantRandomTeleportLabel,
                                false
                            );

                            StartRandomTeleport();
                        });
                    }
                    else if (endpoint == "random_gatcha" || endpoint == "random_gacha")
                    {
                        QueueMainThreadAction("random_gatcha",
                            StartRandomGatcha
                        );
                    }
                    else if (endpoint == "blackhole")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateBlackhole
                        );
                    }
                    else if (endpoint == "back_to_start")
                    {
                        QueueMainThreadAction(endpoint, () =>
                        {
                            ShowInstantGiftHud(
                                _instantBackToStartLabel,
                                false
                            );

                            TeleportToStart(
                                isFromGift: true
                            );
                        });
                    }
                    else if (endpoint == "goto_finish")
                    {
                        QueueMainThreadAction(endpoint, () =>
                        {
                            ShowInstantGiftHud(
                                _instantGoToFinishLabel,
                                true
                            );

                            TeleportToFinish(
                                false
                            );
                        });
                    }
                    else if (endpoint == "wanted_5_star")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateWantedFiveStarGift
                        );
                    }
                    else if (endpoint == "teleport_sky")
                    {
                        QueueMainThreadAction(endpoint, () =>
                        {
                            ShowInstantGiftHud(
                                _instantTeleportSkyLabel,
                                false
                            );

                            ActivateTeleportSkyGift();
                        });
                    }
                    else if (endpoint == "earthquake")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateEarthquakeGift
                        );
                    }
                    else if (endpoint == "hit_by_vehicle")
                    {
                        QueueMainThreadAction(endpoint, () =>
                        {
                            ShowInstantGiftHud(
                                _instantHitByVehicleLabel,
                                false
                            );

                            TriggerHitByVehicle();
                        });
                    }

                    // =========================================================
                    // WEBHOOK EFFECTS 20-32 - REVISED
                    // =========================================================
                    else if (endpoint == "u_turn")
                    {
                        QueueMainThreadAction(endpoint, () =>
                        {
                            ShowInstantGiftHud(
                                _instantUTurnLabel,
                                false
                            );

                            ActivateUTurnWebhook();
                        });
                    }
                    else if (endpoint == "get_towed")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateGetTowedWebhook
                        );
                    }
                    else if (endpoint == "traffic_magnet")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateTrafficMagnetWebhook
                        );
                    }
                    else if (endpoint == "reckless_traffic")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateRecklessTrafficWebhook
                        );
                    }
                    else if (endpoint == "vehicle_fire_timer")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateVehicleFireTimerWebhook
                        );
                    }
                    else if (endpoint == "burning")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateBurningWebhook
                        );
                    }
                    else if (endpoint == "remove_tire")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateRemoveTireWebhook
                        );
                    }
                    else if (endpoint == "police_roadblock")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivatePoliceRoadblockWebhook
                        );
                    }
                    else if (endpoint == "random_explosion_nearby")
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateRandomExplosionNearbyWebhook
                        );
                    }
                    else if ((endpoint == "give_health" || endpoint == "give_health_armor"))
                    {
                        QueueMainThreadAction(endpoint,
                            ActivateGiveHealthWebhook
                        );
                    }

                    // =========================================================
                    // ENEMY / MONSTER
                    //
                    // SUPPORT:
                    // endpoint
                    // endpoint:2
                    // endpoint:3
                    // ...
                    // endpoint:25
                    // =========================================================
                    else if (
                        _npcDatabase.TryGetValue(
                            endpoint,
                            out CustomNpcConfig npcConfig))
                    {
                        int capturedSpawnCount =
                            spawnCount;

                        responseCount =
                            capturedSpawnCount;

                        QueueMainThreadAction(
                            "hud:npc:" + endpoint + ":" + capturedSpawnCount,
                            () =>
                            {
                                string instantEnemyText =
                                    FormatHudText(
                                        _instantEnemyLabel,
                                        GetInstantEnemyDisplayName(
                                            npcConfig
                                        )
                                    );

                                ShowInstantGiftHud(
                                    capturedSpawnCount > 1
                                        ? FormatHudText(
                                            _instantCountFormat,
                                            instantEnemyText,
                                            capturedSpawnCount
                                        )
                                        : instantEnemyText,
                                    false
                                );
                            }
                        );

                        CustomNpcConfig capturedConfig =
                            npcConfig;

                        QueueMainThreadAction(
                            "spawn:npc:" + endpoint,
                            () =>
                            {
                                TriggerSpawnNpc(
                                    capturedConfig
                                );
                            },
                            spawnCount
                        );
                    }

                    // =========================================================
                    // BODYGUARD
                    // =========================================================
                    else if (
                        _bodyguardDatabase.TryGetValue(
                            endpoint,
                            out CustomNpcConfig bgConfig))
                    {
                        int capturedSpawnCount =
                            spawnCount;

                        responseCount =
                            capturedSpawnCount;

                        QueueMainThreadAction(
                            "hud:bodyguard:" + endpoint + ":" + capturedSpawnCount,
                            () =>
                            {
                                string instantBodyguardText =
                                    FormatHudText(
                                        _instantBodyguardLabel,
                                        GetInstantBodyguardDisplayName(
                                            bgConfig
                                        )
                                    );

                                ShowInstantGiftHud(
                                    capturedSpawnCount > 1
                                        ? FormatHudText(
                                            _instantCountFormat,
                                            instantBodyguardText,
                                            capturedSpawnCount
                                        )
                                        : instantBodyguardText,
                                    true
                                );
                            }
                        );

                        CustomNpcConfig capturedConfig =
                            bgConfig;

                        QueueMainThreadAction(
                            "spawn:bodyguard:" + endpoint,
                            () =>
                            {
                                TriggerSpawnBodyguard(
                                    capturedConfig
                                );
                            },
                            spawnCount
                        );
                    }

                    // =========================================================
                    // ANIMAL
                    // =========================================================
                    else if (
                        _animalDatabase.TryGetValue(
                            endpoint,
                            out CustomNpcConfig animalConfig))
                    {
                        int capturedSpawnCount =
                            spawnCount;

                        responseCount =
                            capturedSpawnCount;

                        QueueMainThreadAction(
                            "hud:animal:" + endpoint + ":" + capturedSpawnCount,
                            () =>
                            {
                                string instantAnimalText =
                                    FormatHudText(
                                        _instantAnimalLabel,
                                        GetInstantAnimalDisplayName(
                                            animalConfig
                                        )
                                    );

                                ShowInstantGiftHud(
                                    capturedSpawnCount > 1
                                        ? FormatHudText(
                                            _instantCountFormat,
                                            instantAnimalText,
                                            capturedSpawnCount
                                        )
                                        : instantAnimalText,
                                    false
                                );
                            }
                        );

                        CustomNpcConfig capturedConfig =
                            animalConfig;

                        QueueMainThreadAction(
                            "spawn:animal:" + endpoint,
                            () =>
                            {
                                TriggerSpawnAnimal(
                                    capturedConfig
                                );
                            },
                            spawnCount
                        );
                    }
                    else
                    {
                        endpointRecognized = false;
                    }

                    // =========================================================
                    // RESPONSE WEBHOOK
                    // =========================================================
                    string responseJson;

                    if (endpointRecognized)
                    {
                        responseJson =
                            "{\"status\":\"queued\"," +
                            "\"message\":\"Request queued for processing\"," +
                            "\"count\":" +
                            responseCount +
                            "}";
                    }
                    else
                    {
                        responseJson =
                            "{\"status\":\"error\"," +
                            "\"message\":\"Unknown endpoint\"}";
                    }

                    byte[] buffer =
                        Encoding.UTF8.GetBytes(
                            responseJson
                        );

                    HttpListenerResponse response =
                        context.Response;

                    response.StatusCode =
                        endpointRecognized
                            ? 200
                            : 404;

                    response.ContentLength64 =
                        buffer.Length;

                    response.ContentType =
                        "application/json";

                    using (Stream output = response.OutputStream)
                    {
                        await output.WriteAsync(
                            buffer,
                            0,
                            buffer.Length
                        );
                    }
                }
                catch (HttpListenerException ex)
                {
                    // Normal saat listener dihentikan karena script di-abort
                    if (!_isRunning ||
                        _listener == null ||
                        !_listener.IsListening)
                    {
                        break;
                    }

                    // Error asli: log lengkap + tampilkan lewat GTA main thread
                    LogError(
                        "WEBHOOK ERROR",
                        ex
                    );

                    string errorMessage =
                        ex.Message;

                    QueueMainThreadAction(
                        "webhook_error",
                        () =>
                        {
                            ShowHudNotification(
                                "~r~[WEBHOOK ERROR]~w~ " +
                                errorMessage,
                                false
                            );
                        }
                    );
                }
                catch (Exception ex)
                {
                    if (!_isRunning)
                    {
                        break;
                    }

                    LogError(
                        "WEBHOOK ERROR",
                        ex
                    );

                    string errorMessage =
                        ex.Message;

                    QueueMainThreadAction(
                        "webhook_error",
                        () =>
                        {
                            ShowHudNotification(
                                "~r~[WEBHOOK ERROR]~w~ " +
                                errorMessage,
                                false
                            );
                        }
                    );
                }
            }
        }

        private int CalculateGiftCageFenceCount(
            float radius)
        {
            float circumference =
                2.0f *
                (float)Math.PI *
                radius;

            int count =
                (int)Math.Ceiling(
                    circumference /
                    GIFT_CAGE_FENCE_SPACING
                );

            return
                Math.Max(
                    GIFT_CAGE_MIN_FENCES,
                    Math.Min(
                        GIFT_CAGE_MAX_FENCES,
                        count
                    )
                );
        }

        private void ActivateGiftCage()
        {
            LoadGiftTimerConfigFromIni();

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftCageDurationMs
                );

            // Gift berikutnya mengganti cage gift lama.
            CleanupGiftCage();

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            Entity targetEntity =
                player;

            bool isVehicleCage =
                false;

            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle != null &&
                    vehicle.Exists())
                {
                    targetEntity =
                        vehicle;

                    isVehicleCage =
                        true;
                }
            }

            _giftCageCenter =
                targetEntity.Position;

            _giftCageRadius =
                GIFT_CAGE_WALK_RADIUS;

            if (isVehicleCage)
            {
                float calculatedRadius;

                if (TryGetEntityFootprintRadius(
                        targetEntity,
                        out calculatedRadius
                    ))
                {
                    _giftCageRadius =
                        calculatedRadius;
                }
                else
                {
                    // Fallback vehicle jika native dimensions gagal.
                    _giftCageRadius =
                        3.25f;
                }
            }

            _giftCageFenceCount =
                CalculateGiftCageFenceCount(
                    _giftCageRadius
                );

            Model cageModel =
                new Model(
                    CHAOS_CAGE_MODEL_NAME
                );

            if (!cageModel.IsValid)
            {
                ShowHudNotification(
                    "~r~[CAGE ERROR]~w~ Model cage tidak valid: " +
                    CHAOS_CAGE_MODEL_NAME
                );

                return;
            }

            cageModel.Request();

            _giftCageModel =
                cageModel;

            _isGiftCageModelRequested =
                true;

            _giftCageModelRequestStartTime =
                currentTime;

            _giftCageEndTime =
                currentTime +
                activeDurationMs;

            string cageTargetText =
                isVehicleCage
                    ? "VEHICLE"
                    : "PLAYER";

            ShowHudNotification(
                "~r~[CAGE] ~w~" +
                cageTargetText +
                " TRAPPED! ~y~" +
                (activeDurationMs / 1000) +
                " SEC"
            );
        }

        private void BuildGiftCage()
        {
            if (!_isGiftCageModelRequested ||
                !_giftCageModel.IsLoaded)
            {
                return;
            }

            // Hapus prop setengah jadi jika ada.
            for (
                int i = _giftCageProps.Count - 1;
                i >= 0;
                i--
            )
            {
                Prop oldFence =
                    _giftCageProps[i];

                if (oldFence != null &&
                    oldFence.Exists())
                {
                    oldFence.Delete();
                }
            }

            _giftCageProps.Clear();

            for (
                int i = 0;
                i < _giftCageFenceCount;
                i++
            )
            {
                float angle =
                    i *
                    (
                        2.0f *
                        (float)Math.PI /
                        _giftCageFenceCount
                    );

                float offsetX =
                    (float)Math.Cos(angle) *
                    _giftCageRadius;

                float offsetY =
                    (float)Math.Sin(angle) *
                    _giftCageRadius;

                Vector3 target2D =
                    new Vector3(
                        _giftCageCenter.X +
                            offsetX,

                        _giftCageCenter.Y +
                            offsetY,

                        _giftCageCenter.Z
                    );

                float groundZ =
                    GetGroundHeightCompat(
                        target2D
                    );

                if (Math.Abs(groundZ) <
                    0.001f)
                {
                    groundZ =
                        _giftCageCenter.Z -
                        1.0f;
                }

                Vector3 spawnPos =
                    new Vector3(
                        target2D.X,
                        target2D.Y,
                        groundZ
                    );

                Prop fence =
                    Prop.Create(
                        _giftCageModel,
                        spawnPos,
                        false,
                        false
                    );

                if (fence == null ||
                    !fence.Exists())
                {
                    continue;
                }

                // Tangensial terhadap lingkaran,
                // sama seperti cage Random Chaos.
                fence.Heading =
                    angle *
                    (
                        180.0f /
                        (float)Math.PI
                    ) +
                    90.0f;

                Function.Call(
                    Hash.SET_ENTITY_COLLISION,
                    fence.Handle,
                    true,
                    true
                );

                fence.IsPositionFrozen =
                    true;

                _giftCageProps.Add(
                    fence
                );
            }

            _giftCageModel.MarkAsNoLongerNeeded();

            _isGiftCageModelRequested =
                false;
        }

        private void ProcessGiftCage(
            int currentTime)
        {
            if (_giftCageEndTime <= 0)
                return;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                CleanupGiftCage();
                return;
            }

            if (currentTime >=
                _giftCageEndTime)
            {
                CleanupGiftCage();

                ShowHudNotification(
                    "~g~[CAGE] ~w~Released!"
                );

                return;
            }

            // =========================================================
            // MODEL LOADING NON-BLOCKING
            // =========================================================
            if (_isGiftCageModelRequested &&
                _giftCageProps.Count == 0)
            {
                if (!_giftCageModel.IsLoaded)
                {
                    if (
                        currentTime -
                        _giftCageModelRequestStartTime >=
                        MODEL_LOAD_TIMEOUT_MS
                    )
                    {
                        ShowHudNotification(
                            "~r~[CAGE ERROR]~w~ Model cage gagal load dalam 8 detik."
                        );

                        CleanupGiftCage();
                        return;
                    }

                    _giftCageModel.Request();
                }
                else
                {
                    BuildGiftCage();
                }
            }

            // Cage tetap physical seperti versi Random Chaos.
            // Jump/climb dimatikan agar player tidak memanjat pagar.
            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                22,
                true
            );

            // Jika sedang di vehicle, jangan keluar.
            // Jika sedang jalan kaki, jangan masuk vehicle untuk exploit cage.
            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                23,
                true
            );
        }

        private void CleanupGiftCage()
        {
            for (
                int i = _giftCageProps.Count - 1;
                i >= 0;
                i--
            )
            {
                Prop cageProp =
                    _giftCageProps[i];

                if (cageProp != null &&
                    cageProp.Exists())
                {
                    cageProp.Delete();
                }
            }

            _giftCageProps.Clear();

            if (_isGiftCageModelRequested &&
                _giftCageModel.IsValid)
            {
                _giftCageModel.MarkAsNoLongerNeeded();
            }

            _isGiftCageModelRequested =
                false;

            _giftCageModelRequestStartTime =
                0;

            _giftCageEndTime =
                0;

            _giftCageCenter =
                Vector3.Zero;

            _giftCageRadius =
                GIFT_CAGE_WALK_RADIUS;

            _giftCageFenceCount =
                GIFT_CAGE_MIN_FENCES;
        }

        private void CleanupChaosCage()
        {
            for (
                int i = _chaosCageProps.Count - 1;
                i >= 0;
                i--
            )
            {
                Prop cageProp =
                    _chaosCageProps[i];

                if (cageProp != null &&
                    cageProp.Exists())
                {
                    cageProp.Delete();
                }
            }

            _chaosCageProps.Clear();

            if (_isChaosCageModelRequested &&
                _chaosCageModel.IsValid)
            {
                _chaosCageModel.MarkAsNoLongerNeeded();
            }

            _isChaosCageModelRequested =
                false;
            _chaosCageCenter =
                            Vector3.Zero;
        }

        private Vector3 GetEnemySpawnPosition(
            Ped player,
            CustomNpcConfig config)
        {
            if (player == null ||
                !player.Exists())
            {
                return Vector3.Zero;
            }

            float spawnDistance =
                5.0f;

            if (config != null)
            {
                spawnDistance =
                    Math.Max(
                        0.0f,
                        config.SpawnDistance
                    );
            }

            Vector3 spawnPos =
                player.Position +
                (
                    player.ForwardVector *
                    spawnDistance
                );

            float groundZ =
                GetGroundHeightCompat(
                    spawnPos
                );

            // Hindari ped tiba-tiba dipindahkan jauh ke bawah
            // ketika player berada di jembatan / tempat tinggi.
            if (groundZ > 0.0f &&
                Math.Abs(
                    groundZ -
                    player.Position.Z
                ) <= 4.0f)
            {
                spawnPos.Z =
                    groundZ + 0.5f;
            }
            else
            {
                spawnPos.Z =
                    player.Position.Z;
            }

            return spawnPos;
        }

        private Vector3 GetEnemyVehicleSpawnPosition(
            Ped player,
            CustomNpcConfig config,
            bool isHelicopter)
        {
            if (player == null ||
                !player.Exists())
            {
                return Vector3.Zero;
            }

            float configuredDistance =
                config != null
                    ? Math.Max(0.0f, config.SpawnDistance)
                    : 8.0f;

            float respawnDistance =
                config != null
                    ? config.RespawnDistance
                    : 120.0f;

            // Vehicle tidak boleh dibuat 0.5-1m dari player karena model besar
            // dapat overlap dengan player/vehicle lain. Untuk config normal
            // gunakan minimum 8m.
            float radialDistance =
                Math.Max(8.0f, configuredDistance);

            // Kalau RespawnDistance aktif, pastikan posisi spawn berada dengan
            // margin aman di dalam radius tersebut. Ini mencegah unit baru
            // langsung dianggap terlalu jauh pada tick berikutnya.
            if (respawnDistance > 0.0f)
            {
                float safeRadius =
                    Math.Max(8.0f, respawnDistance - 3.0f);

                radialDistance =
                    Math.Min(radialDistance, safeRadius);
            }

            // Sebar maksimal 16 slot mengelilingi player. Request batch tidak
            // lagi membuat seluruh mobil/tank/heli pada koordinat identik.
            int sequence =
                _enemyVehicleSpawnSequence;

            _enemyVehicleSpawnSequence =
                (_enemyVehicleSpawnSequence + 1) % 4096;

            double angle =
                (sequence % 16) *
                (Math.PI * 2.0 / 16.0);

            Vector3 spawnPos =
                player.Position +
                new Vector3(
                    (float)Math.Cos(angle) * radialDistance,
                    (float)Math.Sin(angle) * radialDistance,
                    0.0f
                );

            if (isHelicopter)
            {
                float altitude = 25.0f;

                if (respawnDistance > 0.0f)
                {
                    // Cari altitude terbesar (maks 25m) yang masih membuat
                    // total 3D distance berada di dalam RespawnDistance.
                    float targetRadius =
                        Math.Max(10.0f, respawnDistance - 3.0f);

                    float verticalSquared =
                        (targetRadius * targetRadius) -
                        (radialDistance * radialDistance);

                    if (verticalSquared > 25.0f)
                    {
                        altitude =
                            Math.Min(
                                25.0f,
                                (float)Math.Sqrt(verticalSquared)
                            );
                    }
                    else
                    {
                        altitude = 5.0f;
                    }
                }

                spawnPos.Z =
                    player.Position.Z +
                    Math.Max(5.0f, altitude);

                return spawnPos;
            }

            float groundZ =
                GetGroundHeightCompat(spawnPos);

            if (groundZ > 0.0f &&
                Math.Abs(
                    groundZ - player.Position.Z
                ) <= 8.0f)
            {
                spawnPos.Z =
                    groundZ + 0.5f;
            }
            else
            {
                spawnPos.Z =
                    player.Position.Z + 0.5f;
            }

            return spawnPos;
        }

        private Vector3 GetEnemyRespawnPosition(
            Ped player,
            CustomNpcConfig config,
            bool forVehicle,
            bool isHelicopter = false)
        {
            if (player == null ||
                !player.Exists())
            {
                return Vector3.Zero;
            }

            float configuredDistance =
                config != null
                    ? Math.Max(
                        0.0f,
                        config.SpawnDistance
                    )
                    : 5.0f;

            float safeDistance =
                forVehicle
                    ? Math.Max(
                        8.0f,
                        configuredDistance
                    )
                    : Math.Max(
                        3.0f,
                        configuredDistance
                    );

            Vector3 spawnPos =
                player.Position +
                (
                    player.ForwardVector *
                    safeDistance
                );

            if (isHelicopter)
            {
                spawnPos.Z =
                    player.Position.Z + 25.0f;

                return spawnPos;
            }

            float groundZ =
                GetGroundHeightCompat(
                    spawnPos
                );

            if (groundZ > 0.0f &&
                Math.Abs(
                    groundZ -
                    player.Position.Z
                ) <= 8.0f)
            {
                spawnPos.Z =
                    groundZ + 0.5f;
            }
            else
            {
                spawnPos.Z =
                    player.Position.Z + 0.5f;
            }

            return spawnPos;
        }

        private void TriggerSpawnNpc(
            CustomNpcConfig config,
            bool isRespawn = false)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                config == null)
            {
                return;
            }

            // =========================================================
            // POSISI SPAWN
            // =========================================================
            Vector3 spawnPos =
                GetEnemySpawnPosition(
                    player,
                    config
                );

            // =========================================================
            // PILIH MODEL + NAMA DISPLAY
            // =========================================================
            string selectedModelName =
                null;

            string selectedNpcName =
                config.DefaultName;

            if (config.PedModels != null &&
                config.PedModels.Count > 0)
            {
                int selectedIndex =
                    _random.Next(
                        config.PedModels.Count
                    );

                selectedModelName =
                    config.PedModels[
                        selectedIndex
                    ];

                if (
                    config.PedModelDisplayNames != null &&
                    selectedIndex <
                    config.PedModelDisplayNames.Count &&
                    !string.IsNullOrEmpty(
                        config.PedModelDisplayNames[
                            selectedIndex
                        ]
                    )
                )
                {
                    selectedNpcName =
                        config.PedModelDisplayNames[
                            selectedIndex
                        ];
                }
            }
            else if (!string.IsNullOrEmpty(
                config.ModelDriver))
            {
                selectedModelName =
                    config.ModelDriver;
            }

            if (string.IsNullOrEmpty(
                selectedModelName))
            {
                ShowHudNotification(
                    $"~r~[ENEMY ERROR]~w~ Model untuk {config.DefaultName} tidak ditemukan!"
                );

                return;
            }

            // =========================================================
            // PED MODEL
            // =========================================================
            Model pedModel =
                uint.TryParse(
                    selectedModelName,
                    NumberStyles.HexNumber,
                    null,
                    out uint pedHash
                )
                    ? new Model((int)pedHash)
                    : new Model(selectedModelName);

            if (!pedModel.IsValid)
            {
                ShowHudNotification(
                    $"~r~[ENEMY ERROR]~w~ Model {selectedModelName} tidak valid!"
                );

                return;
            }

            if (!pedModel.IsPed)
            {
                ShowHudNotification(
                    $"~r~[ENEMY ERROR]~w~ {selectedModelName} bukan PED model!"
                );

                return;
            }

            pedModel.Request();

            // =========================================================
            // VEHICLE OPTIONAL
            // =========================================================
            Model? vehicleModel =
                null;

            if (!string.IsNullOrEmpty(
                config.VehicleModel))
            {
                Model tempVehicleModel =
                    uint.TryParse(
                        config.VehicleModel,
                        NumberStyles.HexNumber,
                        null,
                        out uint vehicleHash
                    )
                        ? new Model(
                            (int)vehicleHash
                        )
                        : new Model(
                            config.VehicleModel
                        );

                if (!tempVehicleModel.IsValid)
                {
                    ShowHudNotification(
                        $"~r~[ENEMY ERROR]~w~ Vehicle model {config.VehicleModel} tidak valid!"
                    );

                    pedModel.MarkAsNoLongerNeeded();
                    return;
                }

                if (!tempVehicleModel.IsVehicle)
                {
                    ShowHudNotification(
                        $"~r~[ENEMY ERROR]~w~ {config.VehicleModel} bukan VEHICLE model!"
                    );

                    pedModel.MarkAsNoLongerNeeded();
                    return;
                }

                tempVehicleModel.Request();

                vehicleModel =
                    tempVehicleModel;
            }

            // =========================================================
            // QUEUE
            // =========================================================
            _enemiesToSpawn.Add(
                new EnemyLoadingTracker
                {
                    Config =
                        config,

                    Player =
                        player,

                    SpawnPosition =
                        spawnPos,

                    Heading =
                        (player.Heading + 180.0f) %
                        360.0f,

                    SelectedModelName =
                        selectedModelName,

                    SelectedNpcName =
                        selectedNpcName,

                    PedModel =
                        pedModel,

                    VehicleModel =
                        vehicleModel,

                    IsRespawn =
                        isRespawn,

                    SpawnAttemptCount =
                        0
                }
            );

            ShowHudNotification(
                $"~g~Spawning {selectedNpcName}..."
            );
        }

        private int GetActiveEnemyUnitCount()
        {
            CleanupStaleEnemyBlips();

            int unitCount = 0;

            HashSet<int> countedVehicleHandles =
                new HashSet<int>();

            HashSet<int> countedPedHandles =
                new HashSet<int>();

            foreach (ActiveBossTracker boss in _activeBosses)
            {
                if (boss == null)
                    continue;

                int vehicleHandle =
                    GetEnemyUnitVehicleHandle(boss);

                // Seluruh driver/passenger dari vehicle asal yang sama tetap
                // dihitung SATU unit walaupun sudah keluar/terpental.
                if (vehicleHandle != 0)
                {
                    if (countedVehicleHandles.Add(vehicleHandle))
                    {
                        unitCount++;
                    }

                    continue;
                }

                if (boss.BossPed == null ||
                    !boss.BossPed.Exists())
                {
                    continue;
                }

                if (countedPedHandles.Add(
                        boss.BossPed.Handle
                    ))
                {
                    unitCount++;
                }
            }

            return unitCount;
        }

        private void RemoveOldestEnemyUnit()
        {
            if (_activeBosses.Count == 0)
                return;

            // Cari tracker valid/representatif paling tua.
            ActiveBossTracker oldestBoss =
                _activeBosses.FirstOrDefault(
                    b => b != null
                );

            if (oldestBoss == null)
            {
                _activeBosses.Clear();
                return;
            }

            int vehicleHandle =
                GetEnemyUnitVehicleHandle(oldestBoss);

            // =========================================================
            // OLDEST UNIT = VEHICLE
            // Hapus seluruh tracker yang berasal dari vehicle handle sama,
            // walaupun occupant sudah keluar/terpental.
            // =========================================================
            if (vehicleHandle != 0)
            {
                Vehicle vehicleToDelete =
                    null;

                for (int i = _activeBosses.Count - 1; i >= 0; i--)
                {
                    ActiveBossTracker tracker =
                        _activeBosses[i];

                    if (tracker == null ||
                        GetEnemyUnitVehicleHandle(tracker) != vehicleHandle)
                    {
                        continue;
                    }

                    if (vehicleToDelete == null &&
                        tracker.AssignedVehicle != null &&
                        tracker.AssignedVehicle.Exists())
                    {
                        vehicleToDelete =
                            tracker.AssignedVehicle;
                    }

                    if (tracker.BossPed != null &&
                        tracker.BossPed.Exists())
                    {
                        RemoveEnemyBlip(tracker.BossPed);
                        tracker.BossPed.Delete();
                    }

                    _activeBosses.RemoveAt(i);
                }

                if (vehicleToDelete == null)
                {
                    vehicleToDelete =
                        _enemyVehicles.FirstOrDefault(
                            v =>
                                v != null &&
                                v.Exists() &&
                                v.Handle == vehicleHandle
                        );
                }

                if (vehicleToDelete != null &&
                    vehicleToDelete.Exists())
                {
                    vehicleToDelete.Delete();
                }

                _enemyVehicles.RemoveAll(
                    v =>
                        v == null ||
                        !v.Exists() ||
                        v.Handle == vehicleHandle
                );

                return;
            }

            // =========================================================
            // OLDEST UNIT = ON-FOOT PED
            // =========================================================
            if (oldestBoss.BossPed != null &&
                oldestBoss.BossPed.Exists())
            {
                RemoveEnemyBlip(oldestBoss.BossPed);
                oldestBoss.BossPed.Delete();
            }

            _activeBosses.Remove(oldestBoss);
        }

        private void EnsureEnemyUnitCapacity(
            int maxEnemyUnits)
        {
            // =========================================================
            // MISAL MAX = 25
            //
            // 20 PED + 5 MOBIL ISI 4 ORANG = 25 UNIT
            //
            // BUKAN:
            // 20 + (5 x 4) = 40
            // =========================================================
            while (
                GetActiveEnemyUnitCount() >= maxEnemyUnits &&
                _activeBosses.Count > 0
            )
            {
                RemoveOldestEnemyUnit();
            }
        }

        private void ProcessEnemySpawns()
        {
            for (int i = _enemiesToSpawn.Count - 1; i >= 0; i--)
            {
                EnemyLoadingTracker tracker =
                    _enemiesToSpawn[i];

                CustomNpcConfig config =
                    tracker.Config;

                // =========================================================
                // SELALU PAKAI PLAYER TERBARU
                //
                // Setelah mati/respawn GTA dapat mengganti Ped player.
                // Jangan pakai tracker.Player lama karena bisa membuat
                // enemy spawn di lokasi mayat / lokasi sebelum teleport.
                // =========================================================
                Ped currentPlayer =
                    Game.Player.Character;

                if (currentPlayer == null ||
                    !currentPlayer.Exists() ||
                    currentPlayer.IsDead)
                {
                    // Pause timeout loading selama player mati.
                    tracker.RequestStartTime =
                        Game.GameTime;

                    continue;
                }

                tracker.Player =
                    currentPlayer;

                Model pedModel =
                    tracker.PedModel;

                // =========================================================
                // PED MODEL TIDAK VALID
                // =========================================================
                if (!pedModel.IsValid)
                {
                    ShowHudNotification(
                        $"~r~[ENEMY ERROR]~w~ Model {tracker.SelectedModelName} tidak valid!"
                    );

                    if (tracker.VehicleModel.HasValue)
                    {
                        Model vehicleModel =
                            tracker.VehicleModel.Value;

                        vehicleModel.MarkAsNoLongerNeeded();
                    }

                    _enemiesToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // PLAYER SUDAH TIDAK VALID
                // =========================================================
                if (tracker.Player == null ||
                    !tracker.Player.Exists())
                {
                    pedModel.MarkAsNoLongerNeeded();

                    if (tracker.VehicleModel.HasValue)
                    {
                        Model vehicleModel =
                            tracker.VehicleModel.Value;

                        vehicleModel.MarkAsNoLongerNeeded();
                    }

                    _enemiesToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // PED MODEL BELUM SIAP
                // =========================================================
                if (!pedModel.IsLoaded)
                {
                    if (
                        Game.GameTime -
                        tracker.RequestStartTime >=
                        MODEL_LOAD_TIMEOUT_MS
                    )
                    {
                        ShowHudNotification(
                            $"~r~[ENEMY TIMEOUT]~w~ Model {tracker.SelectedModelName} gagal load dalam 8 detik!"
                        );

                        pedModel.MarkAsNoLongerNeeded();

                        if (tracker.VehicleModel.HasValue)
                        {
                            Model vehicleModel =
                                tracker.VehicleModel.Value;

                            vehicleModel.MarkAsNoLongerNeeded();
                        }

                        _enemiesToSpawn.RemoveAt(i);
                        continue;
                    }

                    pedModel.Request();
                    continue;
                }

                // =========================================================
                // ENEMY MENGGUNAKAN VEHICLE
                // =========================================================
                if (tracker.VehicleModel.HasValue)
                {
                    Model vehModel =
                        tracker.VehicleModel.Value;

                    // =====================================================
                    // VEHICLE MODEL TIDAK VALID
                    // =====================================================
                    if (!vehModel.IsValid)
                    {
                        ShowHudNotification(
                            $"~r~[ENEMY ERROR]~w~ Vehicle model {config.VehicleModel} tidak valid!"
                        );

                        pedModel.MarkAsNoLongerNeeded();
                        vehModel.MarkAsNoLongerNeeded();

                        _enemiesToSpawn.RemoveAt(i);
                        continue;
                    }

                    // =====================================================
                    // VEHICLE MODEL BELUM SIAP
                    // =====================================================
                    if (!vehModel.IsLoaded)
                    {
                        if (
                            Game.GameTime -
                            tracker.RequestStartTime >=
                            MODEL_LOAD_TIMEOUT_MS
                        )
                        {
                            ShowHudNotification(
                                $"~r~[ENEMY VEHICLE TIMEOUT]~w~ {config.VehicleModel} gagal load dalam 8 detik!"
                            );

                            pedModel.MarkAsNoLongerNeeded();
                            vehModel.MarkAsNoLongerNeeded();

                            _enemiesToSpawn.RemoveAt(i);
                            continue;
                        }

                        vehModel.Request();
                        continue;
                    }

                    // =====================================================
                    // MAKSIMAL ENEMY UNIT SESUAI [ENTITY_LIMITS]
                    //
                    // 1 VEHICLE = 1 UNIT
                    // DRIVER + PASSENGER TIDAK DIHITUNG TERPISAH
                    // =====================================================
                    EnsureEnemyUnitCapacity(_maxEnemyUnits);

                    // =====================================================
                    // POSISI SPAWN VEHICLE
                    //
                    // Hitung ulang saat model SUDAH READY.
                    // Jadi tidak memakai posisi player lama.
                    // =====================================================
                    Vector3 spawnPos =
                        GetEnemyVehicleSpawnPosition(
                            tracker.Player,
                            config,
                            vehModel.IsHelicopter
                        );

                    float spawnHeading =
                        (
                            tracker.Player.Heading +
                            180.0f
                        ) % 360.0f;

                    // =====================================================
                    // SPAWN VEHICLE
                    // =====================================================
                    Vehicle veh =
                        Vehicle.Create(
                            vehModel,
                            spawnPos,
                            spawnHeading
                        );

                    // =====================================================
                    // CREATE VEHICLE GAGAL
                    // =====================================================
                    if (veh == null ||
                        !veh.Exists())
                    {
                        if (tracker.IsRespawn &&
                            tracker.SpawnAttemptCount < 5)
                        {
                            tracker.SpawnAttemptCount++;

                            tracker.RequestStartTime =
                                Game.GameTime;

                            pedModel.Request();
                            vehModel.Request();

                            continue;
                        }

                        ShowHudNotification(
                            $"~r~[ENEMY RESPAWN ERROR]~w~ Vehicle {config.DefaultName} gagal dibuat."
                        );

                        pedModel.MarkAsNoLongerNeeded();
                        vehModel.MarkAsNoLongerNeeded();

                        _enemiesToSpawn.RemoveAt(i);
                        continue;
                    }

                    if (
                        !vehModel.IsHelicopter
                    )
                    {
                        veh.PlaceOnGround();
                    }

                    vehModel.MarkAsNoLongerNeeded();

                    // =====================================================
                    // VEHICLE BERHASIL
                    // =====================================================
                    if (
                        veh != null &&
                        veh.Exists()
                    )
                    {
                        // =================================================
                        // DAFTARKAN MONSTER VEHICLE
                        // =================================================
                        if (!_enemyVehicles.Any(
                            v =>
                                v != null &&
                                v.Exists() &&
                                v.Handle == veh.Handle))
                        {
                            _enemyVehicles.Add(veh);
                        }

                        Function.Call(
                            Hash.SET_VEHICLE_DOORS_LOCKED,
                            veh.Handle,
                            4
                        );

                        Function.Call(
                            Hash.SET_VEHICLE_DOORS_LOCKED_FOR_ALL_PLAYERS,
                            veh.Handle,
                            true
                        );

                        // =================================================
                        // SPAWN DRIVER
                        // =================================================
                        Ped driver =
                            Ped.Create(
                                pedModel,
                                veh.Position,
                                spawnHeading
                            );

                        if (
                            driver != null &&
                            driver.Exists()
                        )
                        {
                            driver.SetIntoVehicle(
                                veh,
                                VehicleSeat.Driver
                            );

                            SetupNpcProperties(
                                driver,
                                config,
                                tracker.SelectedNpcName
                            );

                            // =================================================
                            // DRIVER HANYA MENGEJAR PLAYER JIKA
                            // AttackPlayer = true
                            // =================================================
                            if (config.AttackPlayer &&
                                !tracker.Player.IsDead)
                            {
                                Function.Call(
                                    Hash.SET_DRIVER_AGGRESSIVENESS,
                                    driver.Handle,
                                    1.0f
                                );

                                Function.Call(
                                    Hash.SET_DRIVER_ABILITY,
                                    driver.Handle,
                                    1.0f
                                );

                                // =============================================
                                // HELICOPTER
                                // =============================================
                                if (veh.Model.IsHelicopter)
                                {
                                    Function.Call(
                                        Hash.SET_HELI_BLADES_FULL_SPEED,
                                        veh.Handle
                                    );

                                    Function.Call(
                                        Hash.TASK_HELI_MISSION,
                                        driver.Handle,
                                        veh.Handle,
                                        0,
                                        tracker.Player.Handle,
                                        0.0f,
                                        0.0f,
                                        0.0f,
                                        9,
                                        35.0f,
                                        15.0f,
                                        -1.0f,
                                        30,
                                        10,
                                        -1.0f,
                                        0
                                    );
                                }

                                // =============================================
                                // VEHICLE DARAT
                                // =============================================
                                else
                                {
                                    Function.Call(
                                        Hash.TASK_VEHICLE_MISSION_PED_TARGET,
                                        driver.Handle,
                                        veh.Handle,
                                        tracker.Player.Handle,
                                        6,
                                        100.0f,
                                        786468,
                                        2.0f,
                                        1.0f,
                                        true
                                    );
                                }
                            }

                            // =================================================
                            // DRIVER MASUK ACTIVE ENEMY
                            // =================================================
                            _activeBosses.Add(
                                new ActiveBossTracker
                                {
                                    BossPed =
                                        driver,

                                    NpcName =
                                        string.IsNullOrEmpty(
                                            tracker.SelectedNpcName
                                        )
                                            ? config.DefaultName
                                            : tracker.SelectedNpcName,

                                    Config =
                                        config,

                                    AssignedVehicle =
                                        veh,

                                    AssignedVehicleHandle =
                                        veh.Handle,

                                    LastKnownPosition =
                                        veh.Position
                                }
                            );

                            // =================================================
                            // PASSENGER
                            // =================================================
                            SpawnPassenger(
                                config.ModelPassenger,
                                VehicleSeat.Passenger,
                                veh,
                                config,
                                tracker
                            );

                            SpawnPassenger(
                                config.ModelRightRear,
                                VehicleSeat.RightRear,
                                veh,
                                config,
                                tracker
                            );

                            SpawnPassenger(
                                config.ModelLeftRear,
                                VehicleSeat.LeftRear,
                                veh,
                                config,
                                tracker
                            );
                        }
                        else
                        {
                            // Driver gagal dibuat.
                            // Jangan tinggalkan vehicle kosong.
                            int failedVehicleHandle =
                                veh.Handle;

                            veh.Delete();

                            _enemyVehicles.RemoveAll(
                                v =>
                                    v == null ||
                                    !v.Exists() ||
                                    v.Handle ==
                                        failedVehicleHandle
                            );

                            if (tracker.IsRespawn &&
                                tracker.SpawnAttemptCount < 5)
                            {
                                tracker.SpawnAttemptCount++;

                                tracker.RequestStartTime =
                                    Game.GameTime;

                                pedModel.Request();
                                vehModel.Request();

                                continue;
                            }

                            ShowHudNotification(
                                $"~r~[ENEMY RESPAWN ERROR]~w~ Driver {config.DefaultName} gagal dibuat."
                            );
                        }
                    }

                    // =====================================================
                    // SELESAI VEHICLE ENEMY
                    // =====================================================
                    pedModel.MarkAsNoLongerNeeded();

                    _enemiesToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // ENEMY JALAN KAKI
                //
                // 1 PED = 1 ENEMY UNIT
                // =========================================================
                EnsureEnemyUnitCapacity(_maxEnemyUnits);

                // =========================================================
                // HITUNG POSISI SPAWN TERBARU
                //
                // Jangan pakai tracker.SpawnPosition lama.
                // Kalau model loading beberapa detik, player mungkin
                // sudah berpindah.
                // =========================================================
                Vector3 finalSpawnPos =
                    tracker.IsRespawn
                        ? GetEnemyRespawnPosition(
                            tracker.Player,
                            config,
                            false
                        )
                        : GetEnemySpawnPosition(
                            tracker.Player,
                            config
                        );

                float finalHeading =
                    (
                        tracker.Player.Heading +
                        180.0f
                    ) % 360.0f;

                // =========================================================
                // SPAWN ENEMY JALAN KAKI
                // =========================================================
                Ped enemy =
                    Ped.Create(
                        pedModel,
                        finalSpawnPos,
                        finalHeading
                    );

                if (
                    enemy != null &&
                    enemy.Exists()
                )
                {
                    SetupNpcProperties(
                        enemy,
                        config,
                        tracker.SelectedNpcName
                    );

                    // =====================================================
                    // LANGSUNG SERANG PLAYER
                    // HANYA JIKA AttackPlayer=true
                    // =====================================================
                    if (config.AttackPlayer &&
                        !tracker.Player.IsDead)
                    {
                        Function.Call(
                            Hash.TASK_COMBAT_PED,
                            enemy.Handle,
                            tracker.Player.Handle,
                            0,
                            16
                        );
                    }

                    // =====================================================
                    // MASUK ACTIVE ENEMY
                    // =====================================================
                    _activeBosses.Add(
                        new ActiveBossTracker
                        {
                            BossPed =
                                enemy,

                            NpcName =
                                string.IsNullOrEmpty(
                                    tracker.SelectedNpcName
                                )
                                    ? config.DefaultName
                                    : tracker.SelectedNpcName,

                            Config =
                                config,

                            LastKnownPosition =
                                enemy.Position
                        }
                    );
                }
                else
                {
                    if (tracker.IsRespawn &&
                        tracker.SpawnAttemptCount < 5)
                    {
                        tracker.SpawnAttemptCount++;

                        tracker.RequestStartTime =
                            Game.GameTime;

                        pedModel.Request();

                        continue;
                    }

                    ShowHudNotification(
                        $"~r~[ENEMY RESPAWN ERROR]~w~ {config.DefaultName} gagal dibuat."
                    );
                }

                // =========================================================
                // SELESAI ON-FOOT ENEMY
                // =========================================================
                pedModel.MarkAsNoLongerNeeded();

                _enemiesToSpawn.RemoveAt(i);
            }
        }

        private void SpawnPassenger(
      string modelName,
      VehicleSeat seat,
      Vehicle veh,
      CustomNpcConfig config,
      EnemyLoadingTracker tracker)
        {
            if (string.IsNullOrEmpty(modelName))
                return;

            if (veh == null ||
                !veh.Exists())
            {
                return;
            }

            if (tracker.Player == null ||
                !tracker.Player.Exists())
            {
                return;
            }

            Model pModel = uint.TryParse(
                modelName,
                NumberStyles.HexNumber,
                null,
                out uint pHash
            )
                ? new Model((int)pHash)
                : new Model(modelName);

            if (!pModel.IsValid)
            {
                ShowHudNotification(
                    $"~r~[PASSENGER ERROR]~w~ Model {modelName} tidak valid!"
                );

                return;
            }

            if (!pModel.IsPed)
            {
                ShowHudNotification(
                    $"~r~[PASSENGER ERROR]~w~ {modelName} bukan PED model!"
                );

                return;
            }

            pModel.Request();

            _passengersToSpawn.Add(
                new PassengerLoadingTracker
                {
                    Model = pModel,
                    Vehicle = veh,
                    Seat = seat,
                    Config = config,
                    Player = tracker.Player
                }
            );
        }

        private void ProcessPassengerSpawns()
        {
            for (
                int i = _passengersToSpawn.Count - 1;
                i >= 0;
                i--
            )
            {
                PassengerLoadingTracker tracker =
                    _passengersToSpawn[i];

                // =========================================================
                // VEHICLE SUDAH HILANG
                // =========================================================
                if (tracker.Vehicle == null ||
                    !tracker.Vehicle.Exists())
                {
                    tracker.Model.MarkAsNoLongerNeeded();

                    _passengersToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // PLAYER SUDAH TIDAK VALID / MATI
                // =========================================================
                if (tracker.Player == null ||
                    !tracker.Player.Exists() ||
                    tracker.Player.IsDead)
                {
                    tracker.Model.MarkAsNoLongerNeeded();

                    _passengersToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // MODEL PASSENGER TIDAK VALID
                // =========================================================
                if (!tracker.Model.IsValid)
                {
                    string enemyName =
                        tracker.Config != null
                            ? tracker.Config.DefaultName
                            : "Enemy";

                    ShowHudNotification(
                        $"~r~[PASSENGER ERROR]~w~ Model passenger untuk {enemyName} tidak valid!"
                    );

                    _passengersToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // MODEL BELUM SELESAI LOADING
                // =========================================================
                if (!tracker.Model.IsLoaded)
                {
                    if (
                        Game.GameTime -
                        tracker.RequestStartTime >=
                        MODEL_LOAD_TIMEOUT_MS
                    )
                    {
                        string enemyName =
                            tracker.Config != null
                                ? tracker.Config.DefaultName
                                : "Enemy";

                        ShowHudNotification(
                            $"~r~[PASSENGER TIMEOUT]~w~ Model passenger untuk {enemyName} gagal load!"
                        );

                        tracker.Model.MarkAsNoLongerNeeded();

                        _passengersToSpawn.RemoveAt(i);
                        continue;
                    }

                    tracker.Model.Request();
                    continue;
                }

                // =========================================================
                // MODEL SUDAH READY
                // =========================================================
                Ped passenger =
                    Ped.Create(
                        tracker.Model,
                        tracker.Vehicle.Position,
                        tracker.Vehicle.Heading
                    );

                if (
                    passenger != null &&
                    passenger.Exists()
                )
                {
                    passenger.SetIntoVehicle(
                        tracker.Vehicle,
                        tracker.Seat
                    );

                    SetupNpcProperties(
                        passenger,
                        tracker.Config
                    );

                    // =====================================================
                    // PASSENGER HANYA MENYERANG
                    // JIKA AttackPlayer=true
                    // =====================================================
                    bool attackPlayer =
                        tracker.Config == null ||
                        tracker.Config.AttackPlayer;

                    if (attackPlayer &&
                        !tracker.Player.IsDead)
                    {
                        Function.Call(
                            Hash.TASK_COMBAT_PED,
                            passenger.Handle,
                            tracker.Player.Handle,
                            0,
                            16
                        );
                    }

                    // =====================================================
                    // PASSENGER MASUK ACTIVE ENEMY
                    // =====================================================
                    _activeBosses.Add(
                        new ActiveBossTracker
                        {
                            BossPed =
                                passenger,

                            NpcName =
                                tracker.Config != null
                                    ? tracker.Config.DefaultName
                                    : "Enemy",

                            Config =
                                tracker.Config,

                            AssignedVehicle =
                                tracker.Vehicle,

                            AssignedVehicleHandle =
                                tracker.Vehicle != null &&
                                tracker.Vehicle.Exists()
                                    ? tracker.Vehicle.Handle
                                    : 0,

                            LastKnownPosition =
                                tracker.Vehicle != null &&
                                tracker.Vehicle.Exists()
                                    ? tracker.Vehicle.Position
                                    : passenger.Position
                        }
                    );
                }

                // =========================================================
                // SELESAI
                // =========================================================
                tracker.Model.MarkAsNoLongerNeeded();

                _passengersToSpawn.RemoveAt(i);
            }
        }

        private void TriggerSpawnBodyguard(
    CustomNpcConfig config)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                config == null)
            {
                return;
            }

            // =========================================================
            // SPAWN DISTANCE DARI INI
            // =========================================================
            float spawnDistance =
                Math.Max(
                    0.0f,
                    config.SpawnDistance
                );

            Vector3 spawnPos =
                player.Position +
                (
                    player.ForwardVector *
                    spawnDistance
                );

            float groundZ =
                GetGroundHeightCompat(
                    spawnPos
                );

            spawnPos.Z =
                (groundZ > 0)
                    ? groundZ + 0.5f
                    : player.Position.Z;

            // =========================================================
            // PILIH MODEL
            // =========================================================
            string selectedModelName =
                null;

            if (config.PedModels != null &&
                config.PedModels.Count > 0)
            {
                selectedModelName =
                    config.PedModels[
                        _random.Next(
                            config.PedModels.Count
                        )
                    ];
            }
            else if (!string.IsNullOrEmpty(
                config.ModelDriver))
            {
                selectedModelName =
                    config.ModelDriver;
            }

            if (string.IsNullOrEmpty(
                selectedModelName))
            {
                ShowHudNotification(
                    $"~r~[BODYGUARD ERROR]~w~ Model untuk {config.DefaultName} tidak ditemukan!"
                );

                return;
            }

            Model pedModel =
                uint.TryParse(
                    selectedModelName,
                    NumberStyles.HexNumber,
                    null,
                    out uint modelHash
                )
                    ? new Model((int)modelHash)
                    : new Model(selectedModelName);

            if (!pedModel.IsValid)
            {
                ShowHudNotification(
                    $"~r~[BODYGUARD ERROR]~w~ Model {selectedModelName} tidak valid!"
                );

                return;
            }

            if (!pedModel.IsPed)
            {
                ShowHudNotification(
                    $"~r~[BODYGUARD ERROR]~w~ {selectedModelName} bukan PED model!"
                );

                return;
            }

            // =========================================================
            // NON-BLOCKING LOAD
            // =========================================================
            pedModel.Request();

            _bodyguardsToSpawn.Add(
                new BodyguardLoadingTracker
                {
                    Model = pedModel,
                    Config = config,
                    Player = player,
                    SpawnPosition = spawnPos,
                    Heading = player.Heading
                }
            );

            ShowHudNotification(
                $"~b~[BODYGUARD] {config.DefaultName}~w~ dipanggil!"
            );
        }

        private void ProcessBodyguardSpawns()
        {
            for (int i = _bodyguardsToSpawn.Count - 1; i >= 0; i--)
            {
                BodyguardLoadingTracker tracker =
                    _bodyguardsToSpawn[i];

                // =========================================================
                // PLAYER SUDAH TIDAK VALID / MATI
                // =========================================================
                if (tracker.Player == null ||
                    !tracker.Player.Exists() ||
                    tracker.Player.IsDead)
                {
                    tracker.Model.MarkAsNoLongerNeeded();

                    _bodyguardsToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // MODEL BODYGUARD TIDAK VALID
                // =========================================================
                if (!tracker.Model.IsValid)
                {
                    ShowHudNotification(
                        $"~r~[BODYGUARD ERROR]~w~ Model untuk {tracker.Config.DefaultName} tidak valid!"
                    );

                    _bodyguardsToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // MODEL BELUM SELESAI LOADING
                // =========================================================
                if (!tracker.Model.IsLoaded)
                {
                    if (Game.GameTime - tracker.RequestStartTime >= MODEL_LOAD_TIMEOUT_MS)
                    {
                        ShowHudNotification(
                            $"~r~[BODYGUARD TIMEOUT]~w~ Model {tracker.Config.DefaultName} gagal load!"
                        );

                        tracker.Model.MarkAsNoLongerNeeded();

                        _bodyguardsToSpawn.RemoveAt(i);
                        continue;
                    }

                    tracker.Model.Request();
                    continue;
                }

                // =========================================================
                // MODEL SUDAH READY
                // =========================================================

                // Hanya hapus tracker kalau entity memang sudah hilang.
                // Bodyguard mati dibersihkan oleh ProcessActiveBodyguards().
                _activeBodyguards.RemoveAll(
                    b =>
                        b.BodyguardPed == null ||
                        !b.BodyguardPed.Exists()
                );

                // =========================================================
                // MAKSIMAL BODYGUARD SESUAI [ENTITY_LIMITS]
                // =========================================================
                while (_activeBodyguards.Count >= _maxBodyguards)
                {
                    ActiveBodyguardTracker oldestBg =
                        _activeBodyguards[0];

                    if (oldestBg.BodyguardPed != null &&
                        oldestBg.BodyguardPed.Exists())
                    {
                        oldestBg.BodyguardPed.Delete();
                    }

                    _activeBodyguards.RemoveAt(0);
                }

                // =========================================================
                // HITUNG ULANG POSISI SPAWN SAAT MODEL SUDAH READY
                //
                // Jangan pakai posisi lama dari saat webhook pertama diterima.
                // Kalau loading model butuh beberapa detik, player mungkin
                // sudah pindah jauh.
                // =========================================================
                float spawnDistance =
                    3.0f;

                if (tracker.Config != null)
                {
                    spawnDistance =
                        Math.Max(
                            1.0f,
                            tracker.Config.SpawnDistance
                        );
                }

                Vector3 finalSpawnPos =
                    tracker.Player.Position +
                    (
                        tracker.Player.ForwardVector *
                        spawnDistance
                    );

                // Jangan paksa ke ground yang jauh di bawah player.
                // Ini penting saat player berada di jembatan / tempat tinggi.
                float groundZ =
                    GetGroundHeightCompat(
                        finalSpawnPos
                    );

                if (groundZ > 0.0f &&
                    Math.Abs(
                        groundZ -
                        tracker.Player.Position.Z
                    ) <= 3.0f)
                {
                    finalSpawnPos.Z =
                        groundZ + 0.5f;
                }
                else
                {
                    finalSpawnPos.Z =
                        tracker.Player.Position.Z + 0.1f;
                }

                Ped bgPed =
                    Ped.Create(
                        tracker.Model,
                        finalSpawnPos,
                        tracker.Player.Heading
                    );

                if (bgPed != null &&
                    bgPed.Exists())
                {
                    SetupBodyguardProperties(
                        bgPed,
                        tracker.Config,
                        tracker.Player
                    );

                    _activeBodyguards.Add(
                        new ActiveBodyguardTracker
                        {
                            BodyguardPed = bgPed,
                            NpcName = tracker.Config.DefaultName,
                            Config = tracker.Config
                        }
                    );
                }

                // =========================================================
                // SELESAI
                // =========================================================
                tracker.Model.MarkAsNoLongerNeeded();
                _bodyguardsToSpawn.RemoveAt(i);
            }
        }

        private void TriggerSpawnAnimal(
    CustomNpcConfig config)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                config == null)
            {
                return;
            }

            if (config.PedModels == null ||
                config.PedModels.Count == 0)
            {
                ShowHudNotification(
                    $"~r~[ANIMAL ERROR]~w~ Model untuk {config.DefaultName} tidak ditemukan!"
                );

                return;
            }

            // =========================================================
            // PILIH RANDOM INDEX
            // Model dan nama memakai index yang SAMA.
            // =========================================================
            int selectedIndex =
                _random.Next(
                    config.PedModels.Count
                );

            string selectedModelName =
                config.PedModels[
                    selectedIndex
                ];

            string selectedAnimalName =
                config.DefaultName;

            if (config.PedModelDisplayNames != null &&
                selectedIndex <
                config.PedModelDisplayNames.Count &&
                !string.IsNullOrEmpty(
                    config.PedModelDisplayNames[
                        selectedIndex
                    ]
                ))
            {
                selectedAnimalName =
                    config.PedModelDisplayNames[
                        selectedIndex
                    ];
            }

            Model animalModel =
                uint.TryParse(
                    selectedModelName,
                    NumberStyles.HexNumber,
                    null,
                    out uint animHash
                )
                    ? new Model((int)animHash)
                    : new Model(selectedModelName);

            if (!animalModel.IsValid)
            {
                ShowHudNotification(
                    $"~r~[ANIMAL ERROR]~w~ Model {selectedModelName} tidak valid!"
                );

                return;
            }

            if (!animalModel.IsPed)
            {
                ShowHudNotification(
                    $"~r~[ANIMAL ERROR]~w~ {selectedModelName} bukan PED model!"
                );

                return;
            }

            // =========================================================
            // POSISI AWAL
            // Akan dihitung ulang saat model selesai load.
            // =========================================================
            float spawnDistance =
                Math.Max(
                    1.0f,
                    config.SpawnDistance
                );

            Vector3 spawnPos =
                player.Position +
                (
                    player.ForwardVector *
                    spawnDistance
                );

            animalModel.Request();

            _animalsToSpawn.Add(
                new AnimalLoadingTracker
                {
                    Model = animalModel,

                    Config = config,

                    Player = player,

                    SpawnPosition = spawnPos,

                    Heading =
                        (player.Heading + 180.0f) %
                        360.0f,

                    SelectedModelName =
                        selectedModelName,

                    SelectedAnimalName =
                        selectedAnimalName
                }
            );

            ShowHudNotification(
                $"~o~[ANIMAL] Spawning {selectedAnimalName}..."
            );
        }

        private void ProcessAnimalSpawns()
        {
            for (
                int i = _animalsToSpawn.Count - 1;
                i >= 0;
                i--
            )
            {
                AnimalLoadingTracker tracker =
                    _animalsToSpawn[i];

                // =========================================================
                // PLAYER TIDAK VALID
                //
                // JANGAN cek IsDead.
                // Animal loading tetap boleh lanjut ketika player mati.
                // =========================================================
                if (tracker.Player == null ||
                    !tracker.Player.Exists())
                {
                    tracker.Model.MarkAsNoLongerNeeded();

                    _animalsToSpawn.RemoveAt(i);
                    continue;
                }

                if (!tracker.Model.IsValid)
                {
                    ShowHudNotification(
                        $"~r~[ANIMAL ERROR]~w~ Model {tracker.SelectedModelName} tidak valid!"
                    );

                    _animalsToSpawn.RemoveAt(i);
                    continue;
                }

                // =========================================================
                // TUNGGU MODEL
                // =========================================================
                if (!tracker.Model.IsLoaded)
                {
                    if (
                        Game.GameTime -
                        tracker.RequestStartTime >=
                        MODEL_LOAD_TIMEOUT_MS
                    )
                    {
                        ShowHudNotification(
                            $"~r~[ANIMAL TIMEOUT]~w~ Model {tracker.SelectedModelName} gagal load!"
                        );

                        tracker.Model.MarkAsNoLongerNeeded();

                        _animalsToSpawn.RemoveAt(i);
                        continue;
                    }

                    tracker.Model.Request();
                    continue;
                }

                // =========================================================
                // AMBIL PLAYER TERBARU
                // =========================================================
                Ped currentPlayer =
                    Game.Player.Character;

                if (currentPlayer == null ||
                    !currentPlayer.Exists())
                {
                    currentPlayer =
                        tracker.Player;
                }

                // =========================================================
                // HITUNG POSISI SPAWN SEKARANG
                // =========================================================
                float spawnDistance =
                    Math.Max(
                        1.0f,
                        tracker.Config.SpawnDistance
                    );

                Vector3 spawnPos =
                    currentPlayer.Position +
                    (
                        currentPlayer.ForwardVector *
                        spawnDistance
                    );

                float groundZ =
                    GetGroundHeightCompat(
                        spawnPos
                    );

                if (groundZ > 0.0f &&
                    Math.Abs(
                        groundZ -
                        currentPlayer.Position.Z
                    ) <= 4.0f)
                {
                    spawnPos.Z =
                        groundZ + 0.5f;
                }
                else
                {
                    spawnPos.Z =
                        currentPlayer.Position.Z;
                }

                float heading =
                    (
                        currentPlayer.Heading +
                        180.0f
                    ) % 360.0f;

                // =========================================================
                // MAKSIMAL ANIMAL SESUAI [ENTITY_LIMITS]
                // =========================================================
                _activeAnimals.RemoveAll(
                    a =>
                        a == null ||
                        a.AnimalPed == null ||
                        !a.AnimalPed.Exists()
                );

                while (
                    _activeAnimals.Count >=
                        _maxAnimals
                )
                {
                    ActiveAnimalTracker oldestAnimal =
                        _activeAnimals[0];

                    if (oldestAnimal.AnimalPed != null &&
                        oldestAnimal.AnimalPed.Exists())
                    {
                        oldestAnimal.AnimalPed.Delete();
                    }

                    _activeAnimals.RemoveAt(0);
                }

                // =========================================================
                // SPAWN
                // =========================================================
                Ped animalPed =
                    Ped.Create(
                        tracker.Model,
                        spawnPos,
                        heading
                    );

                if (animalPed != null &&
                    animalPed.Exists())
                {
                    SetupAnimalProperties(
                        animalPed,
                        tracker.Config,
                        currentPlayer,
                        tracker.SelectedAnimalName
                    );

                    _activeAnimals.Add(
                        new ActiveAnimalTracker
                        {
                            AnimalPed =
                                animalPed,

                            AnimalName =
                                tracker.SelectedAnimalName,

                            Config =
                                tracker.Config
                        }
                    );
                }

                tracker.Model.MarkAsNoLongerNeeded();

                _animalsToSpawn.RemoveAt(i);
            }
        }

        private void RemoveEnemyBlipByHandle(
            int pedHandle)
        {
            if (pedHandle == 0)
                return;

            if (_enemyBlips.TryGetValue(
                    pedHandle,
                    out Blip blip
                ))
            {
                if (blip != null &&
                    blip.Exists())
                {
                    blip.Delete();
                }

                _enemyBlips.Remove(
                    pedHandle
                );
            }
        }

        private void RemoveEnemyBlip(
            Ped enemy)
        {
            if (enemy == null)
                return;

            RemoveEnemyBlipByHandle(
                enemy.Handle
            );
        }

        private void CleanupStaleEnemyBlips()
        {
            if (_enemyBlips.Count == 0)
                return;

            HashSet<int> activePedHandles =
                new HashSet<int>();

            for (
                int i = 0;
                i < _activeBosses.Count;
                i++
            )
            {
                ActiveBossTracker boss =
                    _activeBosses[i];

                if (boss == null ||
                    boss.BossPed == null ||
                    !boss.BossPed.Exists())
                {
                    continue;
                }

                activePedHandles.Add(
                    boss.BossPed.Handle
                );
            }

            List<int> registeredHandles =
                _enemyBlips.Keys.ToList();

            for (
                int i = 0;
                i < registeredHandles.Count;
                i++
            )
            {
                int pedHandle =
                    registeredHandles[i];

                if (!activePedHandles.Contains(
                        pedHandle
                    ))
                {
                    RemoveEnemyBlipByHandle(
                        pedHandle
                    );
                }
            }
        }

        private void ClearAllEnemyBlips()
        {
            List<int> registeredHandles =
                _enemyBlips.Keys.ToList();

            for (
                int i = 0;
                i < registeredHandles.Count;
                i++
            )
            {
                RemoveEnemyBlipByHandle(
                    registeredHandles[i]
                );
            }

            _enemyBlips.Clear();
        }

        private void SetupNpcProperties(
       Ped enemy,
       CustomNpcConfig config,
       string displayName = null)
        {
            if (enemy == null ||
                !enemy.Exists() ||
                config == null)
            {
                return;
            }

            // Prevent GTA cleanup/streaming from silently dropping active enemy entities.
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, enemy.Handle, true, true);

            Ped player =
                Game.Player.Character;

            // =========================================================
            // =========================================================
            int health =
                Math.Max(
                    1,
                    config.Health
                );


            Function.Call(
                Hash.SET_PED_MAX_HEALTH,
                enemy.Handle,
                health
            );

            Function.Call(
                Hash.SET_ENTITY_MAX_HEALTH,
                enemy.Handle,
                health
            );

            enemy.MaxHealth =
                health;

            enemy.Health =
                health;

            enemy.Armor = 0;

            Function.Call(
                Hash.SET_PED_ARMOUR,
                enemy.Handle,
                    0
            );

            // =========================================================
            // DAMAGE / PROTECTION
            // =========================================================
            Function.Call(
                Hash.SET_ENTITY_PROOFS,
                enemy.Handle,
                config.BulletProof,
                config.FireProof,
                config.ExplosionProof,
                config.CollisionProof,
                config.MeleeProof,
                false,
                false,
                false
            );

            Function.Call(
                Hash.SET_ENTITY_ONLY_DAMAGED_BY_PLAYER,
                enemy.Handle,
                config.OnlyDamagedByPlayer
            );

            // =========================================================
            // CRITICAL HIT
            // =========================================================
            Function.Call(
                Hash.SET_PED_SUFFERS_CRITICAL_HITS,
                enemy.Handle,
                config.CanSufferCriticalHits
            );

            // =========================================================
            // RAGDOLL
            // =========================================================
            enemy.CanRagdoll =
                config.CanRagdoll;

            if (config.CanRagdoll)
            {
                Function.Call(
                    Hash.SET_RAGDOLL_BLOCKING_FLAGS,
                    enemy.Handle,
                    1 | 4
                );
            }

            Function.Call(
                Hash.SET_PED_CAN_RAGDOLL_FROM_PLAYER_IMPACT,
                enemy.Handle,
                config.RagdollFromPlayerImpact
            );

            Function.Call(
                Hash.SET_PED_CONFIG_FLAG,
                enemy.Handle,
                222,
                true
            );

            Function.Call(
                Hash.SET_PED_CONFIG_FLAG,
                enemy.Handle,
                167,
                true
            );

            Function.Call(
                Hash.SET_PED_CONFIG_FLAG,
                enemy.Handle,
                291,
                true
            );

            // =========================================================
            // WEAPON
            //
            // AMMO TETAP 9999.
            // TIDAK ADA NpcXWeaponAmmo DI INI.
            // =========================================================
            enemy.Weapons.Give(
                config.Weapon,
                9999,
                true,
                true
            );

            enemy.Weapons.Select(
                config.Weapon
            );

            if (enemy.Weapons.Current != null)
            {
                enemy.Weapons.Current.InfiniteAmmoClip =
                    config.WeaponNoReload;

                enemy.Weapons.Current.InfiniteAmmo =
                    config.WeaponNoReload;
            }

            Function.Call(
                Hash.SET_PED_INFINITE_AMMO_CLIP,
                enemy.Handle,
                config.WeaponNoReload
            );

            // =========================================================
            // COMBAT VALUES
            // =========================================================
            int accuracy =
                Math.Max(
                    0,
                    Math.Min(
                        100,
                        config.Accuracy
                    )
                );

            int shootRate =
                Math.Max(
                    0,
                    Math.Min(
                        1000,
                        config.ShootRate
                    )
                );

            int combatAbility =
                Math.Max(
                    0,
                    Math.Min(
                        2,
                        config.CombatAbility
                    )
                );

            int combatRange =
                Math.Max(
                    0,
                    Math.Min(
                        2,
                        config.CombatRange
                    )
                );

            int combatMovement =
                Math.Max(
                    0,
                    Math.Min(
                        3,
                        config.CombatMovement
                    )
                );

            Function.Call(
                Hash.SET_PED_FIRING_PATTERN,
                enemy.Handle,
                0xC6EE6B4C
            );

            Function.Call(
                Hash.SET_PED_SHOOT_RATE,
                enemy.Handle,
                shootRate
            );

            enemy.Accuracy =
                accuracy;

            Function.Call(
                Hash.SET_PED_COMBAT_ABILITY,
                enemy.Handle,
                combatAbility
            );

            Function.Call(
                Hash.SET_PED_COMBAT_RANGE,
                enemy.Handle,
                combatRange
            );

            Function.Call(
                Hash.SET_PED_COMBAT_MOVEMENT,
                enemy.Handle,
                combatMovement
            );

            // =========================================================
            // SEEING RANGE
            // =========================================================
            if (config.SeeingRange > 0.0f)
            {
                Function.Call(
                    Hash.SET_PED_SEEING_RANGE,
                    enemy.Handle,
                    config.SeeingRange
                );
            }

            // =========================================================
            // HEARING RANGE
            // =========================================================
            if (config.HearingRange > 0.0f)
            {
                Function.Call(
                    Hash.SET_PED_HEARING_RANGE,
                    enemy.Handle,
                    config.HearingRange
                );
            }

            // =========================================================
            // COMBAT BEHAVIOR
            // =========================================================
            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                enemy.Handle,
                5,
                !config.CanFlee
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                enemy.Handle,
                14,
                false
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                enemy.Handle,
                3,
                false
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                enemy.Handle,
                0,
                false
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                enemy.Handle,
                46,
                true
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                enemy.Handle,
                2,
                true
            );

            // =========================================================
            // RELATIONSHIP
            // =========================================================
            if (player != null &&
                player.Exists())
            {
                if (config.AttackPlayer)
                {
                    enemy.RelationshipGroup =
                        _relMonsterGroup;

                    int playerGroupHash =
                        player.RelationshipGroup.Hash;

                    Function.Call(
                        Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                        5,
                        _relMonsterGroup.Hash,
                        playerGroupHash
                    );

                    Function.Call(
                        Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                        5,
                        playerGroupHash,
                        _relMonsterGroup.Hash
                    );

                    enemy.BlockPermanentEvents =
                        true;
                }
                else
                {
                    enemy.RelationshipGroup =
                        player.RelationshipGroup;

                    enemy.BlockPermanentEvents =
                        false;
                }
            }

            // =========================================================
            // BLIP
            //
            // SetupNpcProperties dipanggil lagi saat enemy on-foot
            // dipindahkan karena RespawnDistance. Jadi blip lama WAJIB
            // dihapus dulu supaya tidak meninggalkan jejak di minimap.
            // =========================================================
            RemoveEnemyBlipByHandle(
                enemy.Handle
            );

            if (config.UseBlip)
            {
                Blip blip =
                    enemy.AddBlip();

                if (blip != null &&
                    blip.Exists())
                {
                    Function.Call(
                        Hash.SET_BLIP_COLOUR,
                        blip.Handle,
                        config.BlipColorId
                    );

                    blip.Scale =
                        config.BlipScale;

                    // Nama blip mengikuti model random
                    // kalau displayName diberikan.
                    blip.Name =
                        string.IsNullOrEmpty(
                            displayName
                        )
                            ? config.DefaultName
                            : displayName;

                    _enemyBlips[
                        enemy.Handle
                    ] =
                        blip;
                }
            }
        }

        private void SetupBodyguardProperties(
            Ped bodyguard,
            CustomNpcConfig config,
            Ped player)
        {
            if (bodyguard == null ||
                !bodyguard.Exists() ||
                config == null ||
                player == null ||
                !player.Exists())
            {
                return;
            }

            // =========================================================
            // =========================================================
            int health =
                Math.Max(
                    1,
                    config.Health
                );


            Function.Call(
                Hash.SET_PED_MAX_HEALTH,
                bodyguard.Handle,
                health
            );

            Function.Call(
                Hash.SET_ENTITY_MAX_HEALTH,
                bodyguard.Handle,
                health
            );

            bodyguard.MaxHealth =
                health;

            bodyguard.Health =
                health;

            bodyguard.Armor = 0;

            Function.Call(
                Hash.SET_PED_ARMOUR,
                bodyguard.Handle,
                    0
            );

            // =========================================================
            // PROTECTION
            // =========================================================
            Function.Call(
                Hash.SET_ENTITY_PROOFS,
                bodyguard.Handle,
                config.BulletProof,
                config.FireProof,
                config.ExplosionProof,
                config.CollisionProof,
                config.MeleeProof,
                false,
                false,
                false
            );

            Function.Call(
                Hash.SET_ENTITY_ONLY_DAMAGED_BY_PLAYER,
                bodyguard.Handle,
                config.OnlyDamagedByPlayer
            );

            // =========================================================
            // CRITICAL HIT
            // =========================================================
            Function.Call(
                Hash.SET_PED_SUFFERS_CRITICAL_HITS,
                bodyguard.Handle,
                config.CanSufferCriticalHits
            );

            // =========================================================
            // RAGDOLL
            // =========================================================
            bodyguard.CanRagdoll =
                config.CanRagdoll;

            if (config.CanRagdoll)
            {
                Function.Call(
                    Hash.SET_RAGDOLL_BLOCKING_FLAGS,
                    bodyguard.Handle,
                    1 | 4
                );
            }

            Function.Call(
                Hash.SET_PED_CAN_RAGDOLL_FROM_PLAYER_IMPACT,
                bodyguard.Handle,
                config.RagdollFromPlayerImpact
            );

            Function.Call(
                Hash.SET_PED_CONFIG_FLAG,
                bodyguard.Handle,
                222,
                true
            );

            Function.Call(
                Hash.SET_PED_CONFIG_FLAG,
                bodyguard.Handle,
                167,
                true
            );

            // =========================================================
            // WEAPON
            // =========================================================
            int weaponAmmo =
                Math.Max(
                    0,
                    config.WeaponAmmo
                );

            bodyguard.Weapons.Give(
                config.Weapon,
                weaponAmmo,
                true,
                true
            );

            bodyguard.Weapons.Select(
                config.Weapon
            );

            if (bodyguard.Weapons.Current != null)
            {
                bodyguard.Weapons.Current.InfiniteAmmoClip =
                    config.WeaponNoReload;

                bodyguard.Weapons.Current.InfiniteAmmo =
                    config.WeaponNoReload;
            }

            Function.Call(
                Hash.SET_PED_INFINITE_AMMO_CLIP,
                bodyguard.Handle,
                config.WeaponNoReload
            );

            // =========================================================
            // COMBAT VALUES
            // =========================================================
            int accuracy =
                Math.Max(
                    0,
                    Math.Min(
                        100,
                        config.Accuracy
                    )
                );

            int shootRate =
                Math.Max(
                    0,
                    Math.Min(
                        1000,
                        config.ShootRate
                    )
                );

            int combatAbility =
                Math.Max(
                    0,
                    Math.Min(
                        2,
                        config.CombatAbility
                    )
                );

            int combatRange =
                Math.Max(
                    0,
                    Math.Min(
                        2,
                        config.CombatRange
                    )
                );

            int combatMovement =
                Math.Max(
                    0,
                    Math.Min(
                        3,
                        config.CombatMovement
                    )
                );

            Function.Call(
                Hash.SET_PED_FIRING_PATTERN,
                bodyguard.Handle,
                0xC6EE6B4C
            );

            Function.Call(
                Hash.SET_PED_SHOOT_RATE,
                bodyguard.Handle,
                shootRate
            );

            bodyguard.Accuracy =
                accuracy;

            Function.Call(
                Hash.SET_PED_COMBAT_ABILITY,
                bodyguard.Handle,
                combatAbility
            );

            Function.Call(
                Hash.SET_PED_COMBAT_RANGE,
                bodyguard.Handle,
                combatRange
            );

            Function.Call(
                Hash.SET_PED_COMBAT_MOVEMENT,
                bodyguard.Handle,
                combatMovement
            );

            // 0.0 = jangan override GTA
            if (config.SeeingRange > 0.0f)
            {
                Function.Call(
                    Hash.SET_PED_SEEING_RANGE,
                    bodyguard.Handle,
                    config.SeeingRange
                );
            }

            if (config.HearingRange > 0.0f)
            {
                Function.Call(
                    Hash.SET_PED_HEARING_RANGE,
                    bodyguard.Handle,
                    config.HearingRange
                );
            }

            // =========================================================
            // RELATIONSHIP
            // =========================================================
            bodyguard.RelationshipGroup =
                _relBodyguardGroup;

            int playerGroupHash =
                player.RelationshipGroup.Hash;

            // Bodyguard ↔ Player = friendly
            Function.Call(
                Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                0,
                _relBodyguardGroup.Hash,
                playerGroupHash
            );

            Function.Call(
                Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                0,
                playerGroupHash,
                _relBodyguardGroup.Hash
            );

            // Bodyguard ↔ Monster = hostile
            Function.Call(
                Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                5,
                _relBodyguardGroup.Hash,
                _relMonsterGroup.Hash
            );

            Function.Call(
                Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                5,
                _relMonsterGroup.Hash,
                _relBodyguardGroup.Hash
            );

            // =========================================================
            // FOLLOW PLAYER
            // =========================================================
            if (config.FollowPlayer)
            {
                int playerPedGroup =
                    Function.Call<int>(
                        Hash.GET_PLAYER_GROUP,
                        Game.Player.Handle
                    );

                Function.Call(
                    Hash.SET_PED_AS_GROUP_MEMBER,
                    bodyguard.Handle,
                    playerPedGroup
                );

                Function.Call(
                    Hash.SET_PED_NEVER_LEAVES_GROUP,
                    bodyguard.Handle,
                    config.NeverLeavePlayerGroup
                );
            }

            bodyguard.BlockPermanentEvents =
                true;

            // =========================================================
            // COMBAT ATTRIBUTES
            // =========================================================
            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                bodyguard.Handle,
                14,
                false
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                bodyguard.Handle,
                5,
                false
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                bodyguard.Handle,
                46,
                true
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                bodyguard.Handle,
                0,
                false
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                bodyguard.Handle,
                3,
                false
            );
            // =========================================================
            // BLIP
            // =========================================================
            if (config.UseBlip)
            {
                Blip blip =
                    bodyguard.AddBlip();

                if (blip != null &&
                    blip.Exists())
                {
                    Function.Call(
                        Hash.SET_BLIP_COLOUR,
                        blip.Handle,
                        config.BlipColorId
                    );

                    blip.Scale =
                        config.BlipScale;

                    blip.Name =
                        string.IsNullOrEmpty(
                            config.DefaultName
                        )
                            ? "Bodyguard"
                            : config.DefaultName;
                }
            }
        }

        private void SetupAnimalProperties(
            Ped animal,
            CustomNpcConfig config,
            Ped player,
            string animalName)
        {
            if (animal == null ||
                !animal.Exists() ||
                config == null ||
                player == null ||
                !player.Exists())
            {
                return;
            }

            // =========================================================
            // =========================================================
            int health =
                Math.Max(
                    1,
                    config.Health
                );


            Function.Call(
                Hash.SET_PED_MAX_HEALTH,
                animal.Handle,
                health
            );

            Function.Call(
                Hash.SET_ENTITY_MAX_HEALTH,
                animal.Handle,
                health
            );

            animal.MaxHealth =
                health;

            animal.Health =
                health;

            animal.Armor = 0;

            Function.Call(
                Hash.SET_PED_ARMOUR,
                animal.Handle,
                    0
            );

            // =========================================================
            // DAMAGE / PROTECTION
            // =========================================================
            Function.Call(
                Hash.SET_ENTITY_PROOFS,
                animal.Handle,
                config.BulletProof,
                config.FireProof,
                config.ExplosionProof,
                config.CollisionProof,
                config.MeleeProof,
                false,
                false,
                false
            );

            Function.Call(
                Hash.SET_ENTITY_ONLY_DAMAGED_BY_PLAYER,
                animal.Handle,
                config.OnlyDamagedByPlayer
            );

            // =========================================================
            // CRITICAL HIT / RAGDOLL
            // =========================================================
            Function.Call(
                Hash.SET_PED_SUFFERS_CRITICAL_HITS,
                animal.Handle,
                config.CanSufferCriticalHits
            );

            animal.CanRagdoll =
                config.CanRagdoll;

            Function.Call(
                Hash.SET_PED_CAN_RAGDOLL_FROM_PLAYER_IMPACT,
                animal.Handle,
                config.RagdollFromPlayerImpact
            );

            // =========================================================
            // MOVEMENT
            // =========================================================
            float moveRate =
                Math.Max(
                    0.1f,
                    Math.Min(
                        2.0f,
                        config.MoveRate
                    )
                );

            Function.Call(
                Hash.SET_PED_MOVE_RATE_OVERRIDE,
                animal.Handle,
                moveRate
            );

            // =========================================================
            // SENSES
            // =========================================================
            if (config.SeeingRange > 0.0f)
            {
                Function.Call(
                    Hash.SET_PED_SEEING_RANGE,
                    animal.Handle,
                    config.SeeingRange
                );
            }

            if (config.HearingRange > 0.0f)
            {
                Function.Call(
                    Hash.SET_PED_HEARING_RANGE,
                    animal.Handle,
                    config.HearingRange
                );
            }

            // =========================================================
            // COMBAT
            // =========================================================
            int combatRange =
                Math.Max(
                    0,
                    Math.Min(
                        2,
                        config.CombatRange
                    )
                );

            int combatMovement =
                Math.Max(
                    0,
                    Math.Min(
                        3,
                        config.CombatMovement
                    )
                );

            Function.Call(
                Hash.SET_PED_COMBAT_RANGE,
                animal.Handle,
                combatRange
            );

            Function.Call(
                Hash.SET_PED_COMBAT_MOVEMENT,
                animal.Handle,
                combatMovement
            );

            // false = jangan kabur
            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                animal.Handle,
                5,
                !config.CanFlee
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                animal.Handle,
                46,
                config.AttackPlayer
            );

            Function.Call(
                Hash.SET_PED_COMBAT_ATTRIBUTES,
                animal.Handle,
                0,
                false
            );

            // =========================================================
            // RELATIONSHIP
            // =========================================================
            animal.RelationshipGroup =
                _relAnimalGroup;

            int playerGroupHash =
                player.RelationshipGroup.Hash;

            int relationship =
                config.AttackPlayer
                    ? 5
                    : 3;

            Function.Call(
                Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                relationship,
                _relAnimalGroup.Hash,
                playerGroupHash
            );

            Function.Call(
                Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                relationship,
                playerGroupHash,
                _relAnimalGroup.Hash
            );

            // =========================================================
            // ATTACK PLAYER
            // =========================================================
            if (config.AttackPlayer)
            {
                Function.Call(
                    Hash.TASK_COMBAT_PED,
                    animal.Handle,
                    player.Handle,
                    0,
                    16
                );

                animal.BlockPermanentEvents =
                    true;
            }
            else
            {
                animal.BlockPermanentEvents =
                    false;
            }

            // =========================================================
            // BLIP
            // =========================================================
            if (config.UseBlip)
            {
                Blip blip =
                    animal.AddBlip();

                if (blip != null &&
                    blip.Exists())
                {
                    Function.Call(
                        Hash.SET_BLIP_COLOUR,
                        blip.Handle,
                        config.BlipColorId
                    );

                    blip.Scale =
                        config.BlipScale;

                    blip.Name =
                        string.IsNullOrEmpty(
                            animalName
                        )
                            ? config.DefaultName
                            : animalName;
                }
            }
        }

        private void ProcessActiveBodyguards()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            if (_customDeathActive)
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            for (
                int i = _activeBodyguards.Count - 1;
                i >= 0;
                i--
            )
            {
                ActiveBodyguardTracker bg =
                    _activeBodyguards[i];

                // =========================================================
                // BODYGUARD SUDAH TIDAK ADA
                // =========================================================
                if (bg.BodyguardPed == null ||
                    !bg.BodyguardPed.Exists())
                {
                    _activeBodyguards.RemoveAt(i);
                    continue;
                }

                Ped bodyguard =
                    bg.BodyguardPed;

                CustomNpcConfig config =
                    bg.Config;

                // =========================================================
                // BODYGUARD MATI
                // =========================================================
                if (!bodyguard.IsAlive)
                {
                    bodyguard.Delete();

                    _activeBodyguards.RemoveAt(i);
                    continue;
                }

                float distance =
                    bodyguard.Position.DistanceTo(
                        player.Position
                    );

                // =========================================================
                // UI
                // =========================================================
                if (distance <=
                    _uiRenderDistance)
                {
                    bool showName =
                        config == null ||
                        config.ShowName;

                    bool showHealthBar =
                        config == null ||
                        config.ShowHealthBar;

                    DrawPedHud(
                        bodyguard,
                        bg.NpcName,
                        PedHudType.Bodyguard,
                        showName,
                        showHealthBar
                    );
                }

                // =========================================================
                // BODYGUARD TERLALU JAUH
                // =========================================================
                float respawnDistance =
                    config != null
                        ? config.RespawnDistance
                        : 120.0f;

                if (respawnDistance > 0.0f &&
                    distance > respawnDistance)
                {
                    bodyguard.Delete();

                    _activeBodyguards.RemoveAt(i);

                    if (config != null)
                    {
                        TriggerSpawnBodyguard(
                            config
                        );
                    }

                    continue;
                }

                // =========================================================
                // CEK TARGET
                //
                // Jangan setiap frame.
                // 500ms sudah cukup cepat untuk Bodyguard bereaksi.
                // =========================================================
                if (currentTime <
                    bg.NextCombatCheckTime)
                {
                    continue;
                }

                bg.NextCombatCheckTime =
                    currentTime + 500;
                float searchRadius =
                    config != null
                        ? Math.Max(
                            1.0f,
                            config.TargetSearchRadius
                        )
                        : 150.0f;

                Ped nearestTarget =
                    null;

                float nearestDistance =
                    float.MaxValue;

                // =========================================================
                // CARI ENEMY / MONSTER TERDEKAT
                // =========================================================
                for (
                    int e = 0;
                    e < _activeBosses.Count;
                    e++
                )
                {
                    ActiveBossTracker enemyTracker =
                        _activeBosses[e];

                    if (enemyTracker == null ||
                        enemyTracker.BossPed == null ||
                        !enemyTracker.BossPed.Exists() ||
                        !enemyTracker.BossPed.IsAlive)
                    {
                        continue;
                    }

                    Ped enemy =
                        enemyTracker.BossPed;

                    // Target harus berada di area sekitar player.
                    float enemyToPlayerDistance =
                        enemy.Position.DistanceTo(
                            player.Position
                        );

                    if (enemyToPlayerDistance >
                        searchRadius)
                    {
                        continue;
                    }

                    float bodyguardToEnemyDistance =
                        bodyguard.Position.DistanceTo(
                            enemy.Position
                        );

                    if (bodyguardToEnemyDistance <
                        nearestDistance)
                    {
                        nearestDistance =
                            bodyguardToEnemyDistance;

                        nearestTarget =
                            enemy;
                    }
                }

                // =========================================================
                // CARI ANIMAL YANG SEDANG COMBAT
                //
                // Jadi kalau angry animal menyerang player,
                // Bodyguard juga bisa membantu.
                // =========================================================
                for (
                    int a = 0;
                    a < _activeAnimals.Count;
                    a++
                )
                {
                    ActiveAnimalTracker animalTracker =
                        _activeAnimals[a];

                    if (animalTracker == null ||
                        animalTracker.AnimalPed == null ||
                        !animalTracker.AnimalPed.Exists() ||
                        !animalTracker.AnimalPed.IsAlive)
                    {
                        continue;
                    }

                    Ped animal =
                        animalTracker.AnimalPed;

                    // Jangan serang animal yang sedang tidak combat.
                    if (!animal.IsInCombat)
                    {
                        continue;
                    }

                    float animalToPlayerDistance =
                        animal.Position.DistanceTo(
                            player.Position
                        );

                    if (animalToPlayerDistance >
                        searchRadius)
                    {
                        continue;
                    }

                    float bodyguardToAnimalDistance =
                        bodyguard.Position.DistanceTo(
                            animal.Position
                        );

                    if (bodyguardToAnimalDistance <
                        nearestDistance)
                    {
                        nearestDistance =
                            bodyguardToAnimalDistance;

                        nearestTarget =
                            animal;
                    }
                }
                // =========================================================
                // ADA TARGET VALID
                //
                // Hanya target yang berasal dari:
                // - _activeBosses
                // - _activeAnimals yang valid
                //
                // Civilian random tidak pernah dimasukkan ke sini.
                // =========================================================
                if (nearestTarget != null &&
                    nearestTarget.Exists() &&
                    nearestTarget.IsAlive)
                {
                    bool targetChanged =
                        bg.CurrentTargetHandle !=
                        nearestTarget.Handle;

                    // Beri task baru hanya jika:
                    // - target berubah, ATAU
                    // - bodyguard ternyata sudah keluar dari combat.
                    if (targetChanged ||
                        !bodyguard.IsInCombat)
                    {
                        Function.Call(
                            Hash.CLEAR_PED_TASKS,
                            bodyguard.Handle
                        );

                        Function.Call(
                            Hash.TASK_COMBAT_PED,
                            bodyguard.Handle,
                            nearestTarget.Handle,
                            0,
                            16
                        );

                        bg.CurrentTargetHandle =
                            nearestTarget.Handle;
                    }
                }
                else
                {
                    // =====================================================
                    // TIDAK ADA ENEMY VALID
                    //
                    // Kalau GTA entah bagaimana memasukkan bodyguard
                    // ke combat dengan civilian, STOP combat tersebut.
                    // =====================================================
                    if (bodyguard.IsInCombat ||
                        bg.CurrentTargetHandle != 0)
                    {
                        Function.Call(
                            Hash.CLEAR_PED_TASKS,
                            bodyguard.Handle
                        );

                        bg.CurrentTargetHandle =
                            0;
                    }

                    // Tidak perlu TASK follow manual.
                    // Bodyguard sudah anggota group player.
                }

                // Kalau tidak ada target:
                // tidak perlu kasih TASK apa-apa.
                //
                // Karena Bodyguard sudah anggota group player,
                // GTA akan membuat dia kembali mengikuti player.
            }
        }

        private int GetEnemyUnitVehicleHandle(
            ActiveBossTracker boss)
        {
            if (boss == null)
                return 0;

            if (boss.AssignedVehicleHandle != 0)
            {
                return boss.AssignedVehicleHandle;
            }

            if (boss.AssignedVehicle != null &&
                boss.AssignedVehicle.Exists())
            {
                return boss.AssignedVehicle.Handle;
            }

            if (boss.BossPed != null &&
                boss.BossPed.Exists() &&
                boss.BossPed.IsInVehicle())
            {
                Vehicle veh =
                    boss.BossPed.CurrentVehicle;

                if (veh != null &&
                    veh.Exists())
                {
                    return veh.Handle;
                }
            }

            return 0;
        }

        private void RespawnEnemyUnitNearPlayer(
         ActiveBossTracker sourceBoss)
        {
            if (sourceBoss == null)
                return;

            Ped player =
                Game.Player.Character;

            // Jangan respawn enemy ketika player masih mati.
            // Tunggu player hidup kembali di rumah sakit.
            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            CustomNpcConfig config =
                sourceBoss.Config;

            if (config == null)
                return;

            // =========================================================
            // CARI VEHICLE ASAL UNIT
            // =========================================================
            int vehicleHandle =
                GetEnemyUnitVehicleHandle(
                    sourceBoss
                );

            // =========================================================
            // ENEMY VEHICLE
            //
            // Hapus seluruh unit vehicle:
            // - driver
            // - passenger
            // - vehicle
            //
            // Setelah itu spawn ulang SATU unit dari config.
            // =========================================================
            if (vehicleHandle != 0)
            {
                Vehicle vehicleToDelete =
                    null;

                for (
                    int i = _activeBosses.Count - 1;
                    i >= 0;
                    i--
                )
                {
                    ActiveBossTracker tracker =
                        _activeBosses[i];

                    if (tracker == null)
                        continue;

                    if (GetEnemyUnitVehicleHandle(
                            tracker
                        ) != vehicleHandle)
                    {
                        continue;
                    }

                    if (vehicleToDelete == null &&
                        tracker.AssignedVehicle != null &&
                        tracker.AssignedVehicle.Exists())
                    {
                        vehicleToDelete =
                            tracker.AssignedVehicle;
                    }

                    if (tracker.BossPed != null &&
                        tracker.BossPed.Exists())
                    {
                        RemoveEnemyBlip(
                            tracker.BossPed
                        );

                        tracker.BossPed.Delete();
                    }

                    _activeBosses.RemoveAt(i);
                }

                if (vehicleToDelete == null)
                {
                    vehicleToDelete =
                        _enemyVehicles.FirstOrDefault(
                            v =>
                                v != null &&
                                v.Exists() &&
                                v.Handle ==
                                    vehicleHandle
                        );
                }

                if (vehicleToDelete != null &&
                    vehicleToDelete.Exists())
                {
                    vehicleToDelete.Delete();
                }

                _enemyVehicles.RemoveAll(
                    v =>
                        v == null ||
                        !v.Exists() ||
                        v.Handle ==
                            vehicleHandle
                );
            }

            // =========================================================
            // ENEMY JALAN KAKI
            // =========================================================
            else
            {
                if (sourceBoss.BossPed != null &&
                    sourceBoss.BossPed.Exists())
                {
                    RemoveEnemyBlip(
                        sourceBoss.BossPed
                    );

                    sourceBoss.BossPed.Delete();
                }

                _activeBosses.Remove(
                    sourceBoss
                );
            }

            // =========================================================
            // SPAWN ULANG DEKAT PLAYER TERBARU
            // =========================================================
            TriggerSpawnNpc(
                config,
                true
            );
        }

        private void ProcessCustomDeathEnemyPauseState()
        {
            if (_customDeathActive)
            {
                // PENTING:
                // Assign idle/wander hanya SATU KALI saat custom death mulai.
                // Kalau dipanggil setiap tick, TASK_WANDER selalu di-reset
                // dan enemy terlihat freeze/kaku.
                if (!_enemyCombatPausedForCustomDeath)
                {
                    PauseAllCombatForCustomDeath();

                    _enemyCombatPausedForCustomDeath =
                        true;
                }

                return;
            }

            if (_enemyCombatPausedForCustomDeath)
            {
                ResumeAllCombatAfterCustomDeath();

                _enemyCombatPausedForCustomDeath =
                    false;
            }
        }

        private void ProcessActiveBosses()
        {
            // =========================================================
            // BERSIHKAN BLIP GHOST
            // =========================================================
            CleanupStaleEnemyBlips();

            // =========================================================
            // SELALU AMBIL PLAYER TERBARU
            //
            // Penting karena GTA bisa mengganti entity player setelah:
            // - mati
            // - respawn rumah sakit
            // - character reload
            // =========================================================
            Ped player =
                Game.Player.Character;

            // =========================================================
            // PLAYER BELUM BISA DIJADIKAN REFERENSI
            //
            // Saat mati jangan respawn enemy dekat mayat.
            //
            // Begitu player hidup kembali, tick berikutnya otomatis
            // menggunakan posisi player BARU dan sistem jarak general
            // di bawah akan langsung bekerja.
            // =========================================================
            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            if (_customDeathActive)
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            // =========================================================
            // PROSES SEMUA ENEMY AKTIF
            // =========================================================
            for (
                int i = _activeBosses.Count - 1;
                i >= 0;
                i--
            )
            {
                ActiveBossTracker boss =
                    _activeBosses[i];

                // =====================================================
                // TRACKER NULL
                // =====================================================
                if (boss == null)
                {
                    _activeBosses.RemoveAt(i);
                    continue;
                }

                CustomNpcConfig config =
                    boss.Config;

                // =====================================================
                // PED HILANG / STREAM OUT
                //
                // Kalau tracker masih ada tapi PED sudah tidak ada,
                // jangan buang tracker.
                //
                // Spawn ulang unit tersebut dekat player TERBARU.
                //
                // Ini menangani enemy yang hilang karena:
                // - player sangat jauh
                // - teleport
                // - respawn rumah sakit
                // - streaming GTA
                // =====================================================
                if (boss.BossPed == null ||
                    !boss.BossPed.Exists())
                {
                    // One bad streaming frame is not enough to delete/rebuild a unit.
                    if (boss.InvalidSinceTime == 0)
                    {
                        boss.InvalidSinceTime = currentTime;
                        continue;
                    }

                    if (currentTime - boss.InvalidSinceTime < 750)
                        continue;

                    if (config != null)
                    {
                        RespawnEnemyUnitNearPlayer(boss);
                        return;
                    }

                    _activeBosses.RemoveAt(i);
                    continue;
                }

                boss.InvalidSinceTime = 0;

                Ped enemy =
                    boss.BossPed;

                // =====================================================
                // CARI VEHICLE ENEMY
                // =====================================================
                Vehicle currentVehicle =
                    null;

                bool isVehicleDriver =
                    false;

                if (enemy.IsInVehicle())
                {
                    currentVehicle =
                        enemy.CurrentVehicle;

                    if (currentVehicle != null &&
                        currentVehicle.Exists())
                    {
                        Ped driver =
                            currentVehicle.GetPedOnSeat(
                                VehicleSeat.Driver
                            );

                        if (driver != null &&
                            driver.Exists() &&
                            driver.Handle ==
                            enemy.Handle)
                        {
                            isVehicleDriver =
                                true;
                        }
                    }
                }

                // =====================================================
                // POSISI SEBENARNYA DARI SATU ENEMY UNIT
                //
                // Enemy jalan kaki:
                //     posisi PED
                //
                // Enemy vehicle:
                //     posisi VEHICLE
                //
                // Jadi driver/passenger yang terpental tidak membuat
                // satu vehicle dianggap unit terpisah.
                // =====================================================
                Vehicle unitVehicle =
                    boss.AssignedVehicle != null &&
                    boss.AssignedVehicle.Exists()
                        ? boss.AssignedVehicle
                        : currentVehicle;

                Vector3 unitPosition =
                    unitVehicle != null &&
                    unitVehicle.Exists()
                        ? unitVehicle.Position
                        : enemy.Position;

                // Simpan posisi terakhir sebagai fallback.
                boss.LastKnownPosition =
                    unitPosition;

                // =====================================================
                // ENEMY MASIH HIDUP
                // =====================================================
                if (enemy.IsAlive)
                {
                    // =================================================
                    // GENERAL DISTANCE SYSTEM
                    //
                    // INI ADALAH SATU-SATUNYA LOGIC RESPAWN JARAK.
                    //
                    // Tidak peduli kenapa posisi player berubah:
                    //
                    // - player lari
                    // - player teleport gift
                    // - player pindah karena script
                    // - player jatuh jauh
                    // - player mati lalu respawn rumah sakit
                    // - player berpindah dengan cara lain
                    //
                    // Yang diperiksa HANYA:
                    //
                    // DISTANCE ENEMY ↔ PLAYER
                    //
                    // Jika:
                    //
                    // distance > RespawnDistance
                    //
                    // maka enemy dihapus dan spawn ulang dekat player.
                    // =================================================
                    float distance =
                        unitPosition.DistanceTo(
                            player.Position
                        );

                    float respawnDistance =
                        config != null
                            ? config.RespawnDistance
                            : 120.0f;

                    // =================================================
                    // TERLALU JAUH -> RESPAWN
                    //
                    // Check ini dilakukan SEBELUM:
                    // - combat AI
                    // - vehicle AI
                    // - shooting
                    // - UI
                    //
                    // Jadi distance adalah prioritas utama.
                    // =================================================
                    if (respawnDistance > 0.0f && distance > respawnDistance)
                    {
                        if (boss.OutOfRangeSinceTime == 0)
                        {
                            boss.OutOfRangeSinceTime = currentTime;
                        }
                        else if (currentTime - boss.OutOfRangeSinceTime >= 1200)
                        {
                            RespawnEnemyUnitNearPlayer(boss);
                            return;
                        }

                        continue;
                    }
                    else
                    {
                        boss.OutOfRangeSinceTime = 0;
                    }

                    RecoverEnemyIfBelowGround(boss, player, currentTime);

                    // =================================================
                    // CONFIG ATTACK PLAYER
                    // =================================================
                    bool attackPlayer =
                        config == null ||
                        config.AttackPlayer;

                    // =================================================
                    // UI ENEMY
                    // =================================================
                    if (distance <=
                        _uiRenderDistance)
                    {
                        bool showName =
                            config == null ||
                            config.ShowName;

                        bool showHealthBar =
                            config == null ||
                            config.ShowHealthBar;

                        DrawPedHud(
                            enemy,
                            boss.NpcName,
                            PedHudType.Enemy,
                            showName,
                            showHealthBar
                        );
                    }

                    // =================================================
                    // DRIVER VEHICLE
                    // =================================================
                    if (isVehicleDriver &&
                        currentVehicle != null &&
                        currentVehicle.Exists() &&
                        !currentVehicle.IsDead &&
                        currentVehicle.Health > 0)
                    {
                        if (attackPlayer)
                        {
                            // =========================================
                            // DRIVER BOLEH DRIVE-BY
                            // JANGAN KELUAR VEHICLE
                            // =========================================
                            Function.Call(
                                Hash.SET_PED_COMBAT_ATTRIBUTES,
                                enemy.Handle,
                                2,
                                true
                            );

                            Function.Call(
                                Hash.SET_PED_COMBAT_ATTRIBUTES,
                                enemy.Handle,
                                3,
                                false
                            );

                            // =========================================
                            // HELICOPTER
                            // =========================================
                            if (currentVehicle.Model.IsHelicopter)
                            {
                                if (
                                    boss.NextVehicleTaskRefreshTime == 0 ||
                                    currentTime >=
                                    boss.NextVehicleTaskRefreshTime
                                )
                                {
                                    Function.Call(
                                        Hash.SET_HELI_BLADES_FULL_SPEED,
                                        currentVehicle.Handle
                                    );

                                    Function.Call(
                                        Hash.TASK_HELI_MISSION,
                                        enemy.Handle,
                                        currentVehicle.Handle,
                                        0,
                                        player.Handle,
                                        0.0f,
                                        0.0f,
                                        0.0f,
                                        6,
                                        35.0f,
                                        15.0f,
                                        -1.0f,
                                        30,
                                        10,
                                        -1.0f,
                                        0
                                    );

                                    boss.NextVehicleTaskRefreshTime =
                                        currentTime + 2000;
                                }
                            }

                            // =========================================
                            // GROUND VEHICLE
                            //
                            // RAM DAN SHOOT BERGANTIAN
                            // =========================================
                            else
                            {
                                // =====================================
                                // PERTAMA KALI
                                //
                                // RAM SELAMA 2.5 DETIK
                                // =====================================
                                if (
                                    boss.NextVehicleTaskRefreshTime ==
                                    0
                                )
                                {
                                    boss.IsVehicleShootPhase =
                                        false;

                                    Function.Call(
                                        Hash.SET_DRIVER_AGGRESSIVENESS,
                                        enemy.Handle,
                                        1.0f
                                    );

                                    Function.Call(
                                        Hash.SET_DRIVER_ABILITY,
                                        enemy.Handle,
                                        1.0f
                                    );

                                    Function.Call(
                                        Hash.TASK_VEHICLE_MISSION_PED_TARGET,
                                        enemy.Handle,
                                        currentVehicle.Handle,
                                        player.Handle,
                                        6,
                                        80.0f,
                                        786468,
                                        2.0f,
                                        1.0f,
                                        true
                                    );

                                    boss.NextVehicleTaskRefreshTime =
                                        currentTime + 2500;
                                }

                                // =====================================
                                // WAKTUNYA GANTI FASE
                                // =====================================
                                else if (
                                    currentTime >=
                                    boss.NextVehicleTaskRefreshTime
                                )
                                {
                                    // ===============================
                                    // SHOOT -> BALIK KE RAM
                                    // ===============================
                                    if (boss.IsVehicleShootPhase)
                                    {
                                        Function.Call(
                                            Hash.SET_DRIVER_AGGRESSIVENESS,
                                            enemy.Handle,
                                            1.0f
                                        );

                                        Function.Call(
                                            Hash.SET_DRIVER_ABILITY,
                                            enemy.Handle,
                                            1.0f
                                        );

                                        Function.Call(
                                            Hash.TASK_VEHICLE_MISSION_PED_TARGET,
                                            enemy.Handle,
                                            currentVehicle.Handle,
                                            player.Handle,
                                            6,
                                            80.0f,
                                            786468,
                                            2.0f,
                                            1.0f,
                                            true
                                        );

                                        boss.IsVehicleShootPhase =
                                            false;

                                        boss.NextVehicleTaskRefreshTime =
                                            currentTime + 2500;
                                    }

                                    // ===============================
                                    // RAM -> MASUK SHOOT
                                    // ===============================
                                    else
                                    {
                                        if (config != null)
                                        {
                                            Function.Call(
                                                Hash.SET_CURRENT_PED_WEAPON,
                                                enemy.Handle,
                                                (int)config.Weapon,
                                                true
                                            );
                                        }

                                        int vehicleShootRate =
                                            config != null
                                                ? Math.Max(
                                                    0,
                                                    Math.Min(
                                                        1000,
                                                        config.ShootRate
                                                    )
                                                )
                                                : 1000;

                                        Function.Call(
                                            Hash.SET_PED_SHOOT_RATE,
                                            enemy.Handle,
                                            vehicleShootRate
                                        );

                                        Function.Call(
                                            Hash.TASK_COMBAT_PED,
                                            enemy.Handle,
                                            player.Handle,
                                            0,
                                            16
                                        );

                                        boss.IsVehicleShootPhase =
                                            true;

                                        boss.NextVehicleTaskRefreshTime =
                                            currentTime + 1800;
                                    }
                                }
                            }
                        }
                    }

                    // =================================================
                    // PASSENGER / ENEMY JALAN KAKI
                    //
                    // Termasuk driver yang vehicle-nya sudah hancur.
                    // =================================================
                    else
                    {
                        if (!enemy.IsInCombat &&
                            attackPlayer)
                        {
                            Function.Call(
                                Hash.TASK_COMBAT_PED,
                                enemy.Handle,
                                player.Handle,
                                0,
                                16
                            );
                        }
                    }

                    // =================================================
                    // TIDAK ADA LAGI DISTANCE CHECK DI SINI.
                    //
                    // Distance sudah diperiksa di AWAL enemy.IsAlive.
                    // =================================================
                }

                // =====================================================
                // ENEMY MATI
                // =====================================================
                else
                {
                    // =================================================
                    // CATAT STATISTIK KILL (TANPA REWARD)
                    // =================================================
                    RecordEnemyKill(
                        enemy,
                        player
                    );

                    // =================================================
                    // HAPUS BLIP
                    // =================================================
                    RemoveEnemyBlip(
                        enemy
                    );

                    // =================================================
                    // DESPAWN PED MATI
                    // =================================================
                    // Mayat enemy hilang 3 detik setelah mati.
                    ScheduleEntityDespawn(
                        enemy,
                        STANDARD_DESPAWN_MS
                    );

                    _activeBosses.RemoveAt(i);
                }
            }
        }

        private void ProcessActiveAnimals()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            if (_customDeathActive)
            {
                return;
            }

            for (
                int i = _activeAnimals.Count - 1;
                i >= 0;
                i--
            )
            {
                ActiveAnimalTracker tracker =
                    _activeAnimals[i];

                if (tracker.AnimalPed == null ||
                    !tracker.AnimalPed.Exists())
                {
                    _activeAnimals.RemoveAt(i);
                    continue;
                }

                Ped animal =
                    tracker.AnimalPed;

                CustomNpcConfig config =
                    tracker.Config;

                // =========================================================
                // MATI
                // =========================================================
                if (!animal.IsAlive)
                {
                    ScheduleEntityDespawn(
                        animal,
                        STANDARD_DESPAWN_MS
                    );

                    _activeAnimals.RemoveAt(i);
                    continue;
                }

                float distance =
                    animal.Position.DistanceTo(
                        player.Position
                    );

                // =========================================================
                // UI
                // =========================================================
                if (distance <=
                    _uiRenderDistance)
                {
                    bool showName =
                        config == null ||
                        config.ShowName;

                    bool showHealthBar =
                        config == null ||
                        config.ShowHealthBar;

                    DrawPedHud(
                        animal,
                        tracker.AnimalName,
                        PedHudType.Animal,
                        showName,
                        showHealthBar
                    );
                }

                // =========================================================
                // SERANG PLAYER
                // =========================================================
                bool attackPlayer =
                    config == null ||
                    config.AttackPlayer;

                if (attackPlayer &&
                    !player.IsDead &&
                    !animal.IsInCombat)
                {
                    Function.Call(
                        Hash.TASK_COMBAT_PED,
                        animal.Handle,
                        player.Handle,
                        0,
                        16
                    );
                }

                // =========================================================
                // RESPAWN DISTANCE
                // =========================================================
                float respawnDistance =
                    config != null
                        ? config.RespawnDistance
                        : 100.0f;

                if (respawnDistance > 0.0f &&
                    distance > respawnDistance)
                {
                    animal.Delete();

                    _activeAnimals.RemoveAt(i);

                    if (config != null)
                    {
                        TriggerSpawnAnimal(
                            config
                        );
                    }

                    continue;
                }
            }
        }

        private void ActivateDestroyCarGift()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            if (!player.IsInVehicle())
            {
                ShowHudNotification(
                    "~y~[DESTROY CAR] ~w~Player tidak sedang memakai kendaraan."
                );

                return;
            }

            Vehicle vehicle =
                player.CurrentVehicle;

            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            // =====================================================
            // PECAHKAN KACA
            // =====================================================
            // Index window GTA umumnya 0-7.
            // Native aman dipanggil walaupun model tertentu
            // tidak mempunyai semua window.
            for (int windowIndex = 0;
                 windowIndex < 8;
                 windowIndex++)
            {
                Function.Call(
                    Hash.SMASH_VEHICLE_WINDOW,
                    vehicle.Handle,
                    windowIndex
                );
            }

            // =====================================================
            // HANCURKAN KAP DEPAN
            // =====================================================
            // Door index 4 = hood / kap depan.
            // deleteDoor=false supaya efeknya tetap terlihat rusak,
            // bukan sekadar menghilang total.
            Function.Call(
                Hash.SET_VEHICLE_DOOR_BROKEN,
                vehicle.Handle,
                4,
                false
            );

            // Trunk ikut rusak sedikit agar kendaraan terlihat wrecked.
            Function.Call(
                Hash.SET_VEHICLE_DOOR_BROKEN,
                vehicle.Handle,
                5,
                false
            );

            // =====================================================
            // PECAHKAN BAN
            // =====================================================
            // Dicoba ke seluruh index wheel umum.
            // Model motor / mobil / truck akan memakai index yang valid.
            int[] tyreIndexes =
            {
                0, 1, 2, 3, 4, 5, 6, 7
            };

            foreach (int tyreIndex in tyreIndexes)
            {
                Function.Call(
                    Hash.SET_VEHICLE_TYRE_BURST,
                    vehicle.Handle,
                    tyreIndex,
                    false,
                    1000.0f
                );
            }

            // =====================================================
            // BODY DAMAGE / PENYOK
            // =====================================================
            // Multiple damage points supaya body terlihat hancur
            // tanpa menggunakan explosion / fire.
            Function.Call(
                Hash.SET_VEHICLE_DAMAGE,
                vehicle.Handle,
                0.0f,
                2.0f,
                0.5f,
                200.0f,
                1.2f,
                true
            );

            Function.Call(
                Hash.SET_VEHICLE_DAMAGE,
                vehicle.Handle,
                0.0f,
                -2.0f,
                0.5f,
                180.0f,
                1.2f,
                true
            );

            Function.Call(
                Hash.SET_VEHICLE_DAMAGE,
                vehicle.Handle,
                1.2f,
                0.0f,
                0.4f,
                150.0f,
                1.0f,
                true
            );

            Function.Call(
                Hash.SET_VEHICLE_DAMAGE,
                vehicle.Handle,
                -1.2f,
                0.0f,
                0.4f,
                150.0f,
                1.0f,
                true
            );

            // Matikan mesin + tandai kendaraan tidak driveable.
            // Tidak menyentuh explosion / petrol tank / fire state,
            // jadi kendaraan tidak dibuat gosong atau terbakar.
            Function.Call(
                Hash.SET_VEHICLE_ENGINE_ON,
                vehicle.Handle,
                false,
                true,
                true
            );

            Function.Call(
                Hash.SET_VEHICLE_UNDRIVEABLE,
                vehicle.Handle,
                true
            );

            ShowHudNotification(
                "~r~DESTROY CAR! ~w~Vehicle wrecked - no fire."
            );
        }

        private void StartHitByVehicleForceLaunch(
            Vehicle vehicle,
            Vector3 direction)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            // Pastikan physics entity aktif.
            Function.Call(
                Hash.SET_ENTITY_DYNAMIC,
                vehicle.Handle,
                true
            );

            Function.Call(
                Hash.SET_ENTITY_COLLISION,
                vehicle.Handle,
                true,
                true
            );

            _hitByVehicleForceTrackers.Add(
                new HitByVehicleForceTracker
                {
                    Vehicle =
                        vehicle,

                    Direction =
                        direction,

                    Speed =
                        _hitByVehicleSpeed,

                    EndTime =
                        Game.GameTime +
                        _hitByVehicleForceLaunchMs
                }
            );
        }

        private void ProcessHitByVehicleForceLaunches(
            int currentTime)
        {
            for (
                int i =
                    _hitByVehicleForceTrackers.Count - 1;
                i >= 0;
                i--)
            {
                HitByVehicleForceTracker tracker =
                    _hitByVehicleForceTrackers[i];

                Vehicle vehicle =
                    tracker.Vehicle;

                if (vehicle == null ||
                    !vehicle.Exists() ||
                    currentTime >=
                        tracker.EndTime)
                {
                    _hitByVehicleForceTrackers.RemoveAt(i);
                    continue;
                }

                Vector3 direction =
                    tracker.Direction;

                // =====================================================
                // FORCE-LAUNCH
                //
                // Velocity dipaksa SETIAP TICK untuk beberapa detik.
                // Jadi model berat / non-driveable seperti:
                // freight, freightcar, freightgrain, tankercar,
                // metrotrain, dll tetap meluncur sebagai projectile.
                //
                // Arah TIDAK berubah -> bukan homing / blackhole.
                // =====================================================
                Vector3 forcedVelocity =
                    new Vector3(
                        direction.X *
                            tracker.Speed,
                        direction.Y *
                            tracker.Speed,
                        vehicle.Velocity.Z
                    );

                Function.Call(
                    Hash.SET_ENTITY_VELOCITY,
                    vehicle.Handle,
                    forcedVelocity.X,
                    forcedVelocity.Y,
                    forcedVelocity.Z
                );

                // Tambah force kecil supaya physics body berat tetap "terdorong".
                Function.Call(
                    Hash.APPLY_FORCE_TO_ENTITY,
                    vehicle.Handle,
                    1,
                    direction.X * 3.0f,
                    direction.Y * 3.0f,
                    0.0f,
                    0.0f,
                    0.0f,
                    0.0f,
                    0,
                    false,
                    true,
                    true,
                    false,
                    true
                );

            }
        }

        private void TriggerHitByVehicle()
        {
            if (_customDeathActive)
                return;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            LoadHitByVehicleConfigFromIni();

            if (_hitByVehicleModelNames.Count == 0)
            {
                ShowHudNotification(
                    "~r~[HIT BY VEHICLE]~w~ hitby belum di-set di INI!"
                );

                return;
            }

            // Pilih SATU model secara random dari jumlah baris hitby= yang ada.
            // Jika kebetulan ada model invalid, coba kandidat lain agar gift tidak
            // langsung gagal hanya karena satu entry salah.
            string selectedModelName =
                string.Empty;

            Model vehicleModel =
                default(Model);

            int randomStartIndex =
                _random.Next(
                    _hitByVehicleModelNames.Count
                );

            for (int offset = 0;
                 offset < _hitByVehicleModelNames.Count;
                 offset++)
            {
                int index =
                    (randomStartIndex + offset) %
                    _hitByVehicleModelNames.Count;

                string candidateName =
                    _hitByVehicleModelNames[index];

                if (string.IsNullOrWhiteSpace(
                        candidateName))
                {
                    continue;
                }

                Model candidateModel =
                    new Model(
                        candidateName
                    );

                if (!candidateModel.IsValid ||
                    !candidateModel.IsVehicle)
                {
                    continue;
                }

                selectedModelName =
                    candidateName;

                vehicleModel =
                    candidateModel;

                break;
            }

            if (string.IsNullOrWhiteSpace(
                    selectedModelName) ||
                !vehicleModel.IsValid ||
                !vehicleModel.IsVehicle)
            {
                ShowHudNotification(
                    "~r~[HIT BY VEHICLE]~w~ Tidak ada model hitby yang valid!"
                );

                return;
            }

            _hitByVehicleModelName =
                selectedModelName;

            vehicleModel.Request();

            _hitByVehiclesToSpawn.Add(
                new HitByVehicleLoadingTracker
                {
                    Model =
                        vehicleModel,

                    Player =
                        player,

                    VehicleModelName =
                        selectedModelName
                }
            );
        }

        private void ProcessHitByVehicleSpawns()
        {
            for (
                int i =
                    _hitByVehiclesToSpawn.Count - 1;
                i >= 0;
                i--)
            {
                HitByVehicleLoadingTracker tracker =
                    _hitByVehiclesToSpawn[i];

                if (!tracker.Model.IsValid ||
                    !tracker.Model.IsVehicle)
                {
                    _hitByVehiclesToSpawn.RemoveAt(i);
                    continue;
                }

                Ped player =
                    tracker.Player;

                if (player == null ||
                    !player.Exists() ||
                    player.IsDead ||
                    _customDeathActive)
                {
                    tracker.Model.MarkAsNoLongerNeeded();

                    _hitByVehiclesToSpawn.RemoveAt(i);
                    continue;
                }

                if (!tracker.Model.IsLoaded)
                {
                    if (
                        Game.GameTime -
                        tracker.RequestStartTime >=
                        MODEL_LOAD_TIMEOUT_MS
                    )
                    {
                        ShowHudNotification(
                            "~r~[HIT BY VEHICLE TIMEOUT]~w~ " +
                            (tracker.VehicleModelName ?? "vehicle")
                        );

                        tracker.Model.MarkAsNoLongerNeeded();

                        _hitByVehiclesToSpawn.RemoveAt(i);
                        continue;
                    }

                    tracker.Model.Request();
                    continue;
                }

                // =====================================================
                // SPAWN BEBERAPA METER DI DEPAN PLAYER
                //
                // Default 14m; bisa diubah lewat INI.
                // Kendaraan menghadap balik ke player.
                // =====================================================
                Vector3 playerPosition =
                    player.Position;

                Vector3 spawnPosition =
                    playerPosition +
                    (
                        player.ForwardVector *
                        _hitByVehicleSpawnDistance
                    );

                float groundZ =
                    GetGroundHeightCompat(
                        spawnPosition
                    );

                if (groundZ > 0.0f &&
                    Math.Abs(
                        groundZ -
                        playerPosition.Z
                    ) <= 8.0f)
                {
                    spawnPosition.Z =
                        groundZ + 0.40f;
                }
                else
                {
                    spawnPosition.Z =
                        playerPosition.Z + 0.40f;
                }

                float headingToPlayer =
                    player.Heading +
                    180.0f;

                if (headingToPlayer >= 360.0f)
                {
                    headingToPlayer -=
                        360.0f;
                }

                Vehicle vehicle =
                    Vehicle.Create(
                        tracker.Model,
                        spawnPosition,
                        headingToPlayer
                    );

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    ShowHudNotification(
                        "~r~[HIT BY VEHICLE]~w~ Gagal spawn " +
                        (tracker.VehicleModelName ?? "vehicle") +
                        "!"
                    );

                    tracker.Model.MarkAsNoLongerNeeded();

                    _hitByVehiclesToSpawn.RemoveAt(i);
                    continue;
                }

                vehicle.PlaceOnGround();

                vehicle.Heading =
                    headingToPlayer;

                Function.Call(
                    Hash.SET_VEHICLE_ENGINE_ON,
                    vehicle.Handle,
                    true,
                    true,
                    false
                );

                // =====================================================
                // ARAH IMPACT
                //
                // Hitung vector dari posisi kendaraan menuju player,
                // lalu beri velocity lurus. Bukan homing / blackhole.
                // =====================================================
                Vector3 currentVehiclePos =
                    vehicle.Position;

                float dx =
                    playerPosition.X -
                    currentVehiclePos.X;

                float dy =
                    playerPosition.Y -
                    currentVehiclePos.Y;

                float horizontalLength =
                    (float)Math.Sqrt(
                        (dx * dx) +
                        (dy * dy)
                    );

                float dirX =
                    horizontalLength > 0.001f
                        ? dx / horizontalLength
                        : -player.ForwardVector.X;

                float dirY =
                    horizontalLength > 0.001f
                        ? dy / horizontalLength
                        : -player.ForwardVector.Y;

                Vector3 launchDirection =
                    new Vector3(
                        dirX,
                        dirY,
                        0.0f
                    );

                vehicle.Velocity =
                    new Vector3(
                        dirX *
                            _hitByVehicleSpeed,
                        dirY *
                            _hitByVehicleSpeed,
                        0.0f
                    );

                // Bukan cuma one-shot velocity.
                // Paksa velocity terus beberapa detik supaya train/carriage
                // dan vehicle non-driveable tetap benar-benar meluncur.
                StartHitByVehicleForceLaunch(
                    vehicle,
                    launchDirection
                );

                // Hilang otomatis 60 detik setelah spawn.
                ScheduleEntityDespawn(
                    vehicle,
                    HIT_BY_VEHICLE_DESPAWN_MS
                );

                ShowHudNotification(
                    "~r~HIT BY VEHICLE! ~w~" +
                    (tracker.VehicleModelName ?? "VEHICLE")
                        .ToUpperInvariant()
                );

                tracker.Model.MarkAsNoLongerNeeded();

                _hitByVehiclesToSpawn.RemoveAt(i);
            }
        }

        private void TriggerGiveVehicle()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            if (_giftVehicleModelNames.Count == 0 ||
                _giftVehicleModelNames.Count !=
                    _giftVehicleDisplayNames.Count)
            {
                ShowHudNotification(
                    "~r~[VEHICLE ERROR]~w~ [GiveVehicles] kosong atau tidak valid!"
                );

                return;
            }

            int selectedIndex =
                _random.Next(
                    _giftVehicleModelNames.Count
                );

            string selectedModelName =
                _giftVehicleModelNames[
                    selectedIndex
                ];

            string selectedDisplayName =
                _giftVehicleDisplayNames[
                    selectedIndex
                ];

            Model vehicleModel =
                new Model(
                    selectedModelName
                );

            if (!vehicleModel.IsValid ||
                !vehicleModel.IsVehicle)
            {
                ShowHudNotification(
                    "~r~[VEHICLE ERROR]~w~ Model " +
                    selectedDisplayName +
                    " tidak valid!"
                );

                return;
            }

            ShowInstantGiftHud(
                FormatHudText(
                    _instantGiveVehicleLabel,
                    selectedDisplayName
                ),
                true
            );

            vehicleModel.Request();

            _vehiclesToGive.Add(
                new VehicleLoadingTracker
                {
                    Model =
                        vehicleModel,

                    Player =
                        player,

                    Heading =
                        player.Heading,

                    VehicleName =
                        selectedDisplayName
                }
            );
        }

        private void ProcessVehicleGiftSpawns()
        {
            for (
                int i =
                    _vehiclesToGive.Count - 1;
                i >= 0;
                i--
            )
            {
                VehicleLoadingTracker tracker =
                    _vehiclesToGive[i];

                if (!tracker.Model.IsValid ||
                    !tracker.Model.IsVehicle)
                {
                    ShowHudNotification(
                        "~r~[VEHICLE ERROR]~w~ Model vehicle tidak valid!"
                    );

                    _vehiclesToGive.RemoveAt(i);
                    continue;
                }

                Ped player =
                    tracker.Player;

                if (player == null ||
                    !player.Exists() ||
                    player.IsDead)
                {
                    tracker.Model.MarkAsNoLongerNeeded();

                    _vehiclesToGive.RemoveAt(i);
                    continue;
                }

                if (!tracker.Model.IsLoaded)
                {
                    if (
                        Game.GameTime -
                        tracker.RequestStartTime >=
                        MODEL_LOAD_TIMEOUT_MS
                    )
                    {
                        ShowHudNotification(
                            "~r~[VEHICLE TIMEOUT]~w~ " +
                            (tracker.VehicleName ?? "Vehicle") +
                            " gagal load dalam 8 detik!"
                        );

                        tracker.Model.MarkAsNoLongerNeeded();

                        _vehiclesToGive.RemoveAt(i);
                        continue;
                    }

                    tracker.Model.Request();
                    continue;
                }

                // =====================================================
                // GIVE VEHICLE = REPLACE SYSTEM
                // =====================================================
                // Jika player sedang memakai kendaraan:
                // - posisi / heading / velocity kendaraan lama disimpan,
                // - kendaraan baru dibuat,
                // - kendaraan lama DIHAPUS,
                // - kendaraan baru ditempatkan di posisi kendaraan lama,
                // - player langsung dimasukkan ke kendaraan baru.
                //
                // Jika player sedang jalan kaki:
                // - kendaraan baru tetap spawn di depan player,
                // - gift vehicle sebelumnya (jika masih ada) dihapus.
                // =====================================================

                Vehicle playerOldVehicle =
                    player.IsInVehicle()
                        ? player.CurrentVehicle
                        : null;

                bool replaceCurrentVehicle =
                    playerOldVehicle != null &&
                    playerOldVehicle.Exists();

                // =====================================================
                // GET TOWED + GIVE VEHICLE INTEROP
                // =====================================================
                // Timer GET TOWED adalah sumber utama state effect.
                // Selama timer belum habis, GIVE VEHICLE TIDAK BOLEH
                // mengakhiri effect walaupun target/tow truck lama hilang.
                // Setelah kendaraan baru jadi, rig tow akan dibuat ulang
                // dan langsung menarget kendaraan baru.
                bool getTowedTimerStillActive =
                    _getTowedEndTime > Game.GameTime;

                Vehicle previousTowedTarget =
                    getTowedTimerStillActive
                        ? _getTowedTargetVehicle
                        : null;

                bool previousTowedTargetWasGiftVehicle =
                    previousTowedTarget != null &&
                    previousTowedTarget.Exists() &&
                    _currentGiftVehicle != null &&
                    _currentGiftVehicle.Exists() &&
                    _currentGiftVehicle.Handle ==
                        previousTowedTarget.Handle;

                Vector3 targetSpawnPos;
                float targetHeading;
                Vector3 inheritedVelocity =
                    Vector3.Zero;

                // Jika player sedang jalan kaki tetapi berada di udara
                // (contoh: setelah /teleport_sky), Give Vehicle HARUS
                // mempertahankan altitude player. Jangan snap ke ground.
                bool spawnVehicleInAir =
                    false;

                if (replaceCurrentVehicle)
                {
                    targetSpawnPos =
                        playerOldVehicle.Position;

                    targetHeading =
                        playerOldVehicle.Heading;

                    inheritedVelocity =
                        playerOldVehicle.Velocity;
                }
                else
                {
                    targetSpawnPos =
                        player.Position +
                        (
                            player.ForwardVector *
                            6.0f
                        );

                    targetHeading =
                        tracker.Heading;

                    spawnVehicleInAir =
                        Function.Call<bool>(
                            Hash.IS_ENTITY_IN_AIR,
                            player.Handle
                        );

                    if (spawnVehicleInAir)
                    {
                        // Pertahankan Z player saat ini.
                        // Mobil akan ikut jatuh secara natural dari langit.
                        targetSpawnPos.Z =
                            player.Position.Z;

                        // Warisi momentum player supaya perpindahan ke mobil
                        // tidak membuat teleport / hentakan aneh.
                        inheritedVelocity =
                            player.Velocity;
                    }
                    else
                    {
                        // Hanya player yang benar-benar berada di darat
                        // yang boleh di-snap ke permukaan jalan/tanah.
                        float groundZ =
                            GetGroundHeightCompat(
                                targetSpawnPos
                            );

                        if (Math.Abs(groundZ) >
                            0.001f)
                        {
                            targetSpawnPos.Z =
                                groundZ + 0.35f;
                        }
                        else
                        {
                            targetSpawnPos.Z =
                                player.Position.Z;
                        }
                    }
                }

                // Kalau sedang replace kendaraan aktif, buat kendaraan baru
                // sedikit di atas terlebih dahulu. Kendaraan lama baru dihapus
                // SETELAH spawn baru berhasil supaya player tidak kehilangan
                // kendaraan jika World.CreateVehicle gagal.
                Vector3 createPos =
                    replaceCurrentVehicle
                        ? targetSpawnPos +
                          new Vector3(
                              0.0f,
                              0.0f,
                              2.0f
                          )
                        : targetSpawnPos;

                Vehicle vehicle =
                    Vehicle.Create(
                        tracker.Model,
                        createPos,
                        targetHeading
                    );

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    ShowHudNotification(
                        "~r~[VEHICLE ERROR]~w~ Gagal spawn " +
                        (tracker.VehicleName ?? "Vehicle") +
                        "!"
                    );

                    tracker.Model.MarkAsNoLongerNeeded();

                    _vehiclesToGive.RemoveAt(i);
                    continue;
                }

                // =====================================================
                // GET TOWED: LEPAS TARGET LAMA SEBELUM REPLACE
                // =====================================================
                // SAFE VERSION: GET TOWED tidak lagi memakai native tow-hook.
                // Cukup lepaskan rigid entity attachment sebelum target lama
                // diganti/dihapus.
                if (getTowedTimerStillActive &&
                    previousTowedTarget != null &&
                    previousTowedTarget.Exists())
                {
                    try
                    {
                        Function.Call(
                            Hash.DETACH_ENTITY,
                            previousTowedTarget.Handle,
                            true,
                            true
                        );
                    }
                    catch
                    {
                    }
                }

                // =====================================================
                // HAPUS GIFT VEHICLE SEBELUMNYA
                // =====================================================
                // Kalau previous gift vehicle BUKAN kendaraan yang sedang
                // dipakai player, tetap hapus agar tidak ada mobil gift lama
                // tertinggal di map. Tetapi target GET TOWED lama ditahan dulu
                // sampai transfer tow ke kendaraan baru selesai.
                if (_currentGiftVehicle != null &&
                    _currentGiftVehicle.Exists() &&
                    (
                        playerOldVehicle == null ||
                        !_currentGiftVehicle.Equals(
                            playerOldVehicle
                        )
                    ) &&
                    (
                        previousTowedTarget == null ||
                        !previousTowedTarget.Exists() ||
                        _currentGiftVehicle.Handle !=
                            previousTowedTarget.Handle
                    ))
                {
                    _currentGiftVehicle.Delete();
                }

                // =====================================================
                // REPLACE KENDARAAN YANG SEDANG DIPAKAI
                // =====================================================
                if (replaceCurrentVehicle)
                {
                    if (playerOldVehicle != null &&
                        playerOldVehicle.Exists())
                    {
                        playerOldVehicle.Delete();
                    }

                    // Tempatkan kendaraan baru tepat di lokasi kendaraan lama.
                    vehicle.Position =
                        targetSpawnPos;

                    vehicle.Heading =
                        targetHeading;

                    vehicle.Velocity =
                        inheritedVelocity;
                }
                else if (spawnVehicleInAir)
                {
                    // PENTING:
                    // Jangan pernah PlaceOnGround() saat player sedang di udara.
                    // Kalau dipanggil, vehicle akan dipindahkan ke jalan di bawah
                    // dan SET_PED_INTO_VEHICLE ikut membawa player ke sana.
                    vehicle.Position =
                        targetSpawnPos;

                    vehicle.Heading =
                        targetHeading;

                    vehicle.Velocity =
                        inheritedVelocity;

                    Function.Call(
                        Hash.SET_ENTITY_HAS_GRAVITY,
                        vehicle.Handle,
                        true
                    );
                }
                else
                {
                    vehicle.PlaceOnGround();
                }

                // Tidak ada filter class kendaraan.
                // Semua model valid dari [GiveVehicles] boleh dipakai.
                Function.Call(
                    Hash.SET_PED_INTO_VEHICLE,
                    player.Handle,
                    vehicle.Handle,
                    -1
                );

                // =====================================================
                // GET TOWED: MUNCULKAN LAGI SETELAH GIVE VEHICLE
                // =====================================================
                // Jangan mencoba mempertahankan rig lama. GIVE VEHICLE dapat
                // menghapus target lama dan native tow state ikut rusak/hilang.
                // Selama TIMER GET TOWED masih aktif:
                //   1. kendaraan baru menjadi target,
                //   2. rig tow lama dibersihkan,
                //   3. tow truck + driver BARU dimunculkan,
                //   4. sisa timer tetap sama (TIDAK di-reset).
                //
                // Jika spawn rig gagal sesaat, ProcessGetTowedEffect() akan
                // mencoba lagi otomatis sampai timer benar-benar habis.
                if (getTowedTimerStillActive &&
                    _getTowedEndTime > Game.GameTime)
                {
                    _nextGetTowedRespawnAttemptTime =
                        0;

                    SpawnGetTowedRigForTarget(
                        vehicle
                    );

                    // Kalau target tow lama adalah gift vehicle lama tetapi BUKAN
                    // kendaraan yang baru saja di-replace, hapus setelah target
                    // baru sudah dipilih agar tidak meninggalkan kendaraan yatim.
                    if (previousTowedTargetWasGiftVehicle &&
                        previousTowedTarget != null &&
                        previousTowedTarget.Exists() &&
                        previousTowedTarget.Handle !=
                            vehicle.Handle &&
                        (
                            playerOldVehicle == null ||
                            !playerOldVehicle.Exists() ||
                            previousTowedTarget.Handle !=
                                playerOldVehicle.Handle
                        ))
                    {
                        previousTowedTarget.Delete();
                    }
                }

                // Simpan sebagai SATU-SATUNYA gift vehicle aktif.
                _currentGiftVehicle =
                    vehicle;

                ShowHudNotification(
                    "~g~VEHICLE REPLACED! ~w~Player langsung naik " +
                    (tracker.VehicleName ?? "Vehicle") +
                    "."
                );

                tracker.Model.MarkAsNoLongerNeeded();

                _vehiclesToGive.RemoveAt(i);
            }
        }

        private int GetFallingVehicleSpawnIntervalMs(
            int durationMs)
        {
            return
                Math.Max(
                    1,
                    Math.Max(
                        1,
                        durationMs
                    ) /
                    FALLING_VEHICLES_PER_GIFT
                );
        }

        private void TriggerFallingVehicles()
        {
            LoadGiftTimerConfigFromIni();

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            if (_fallingVehicleHashes.Count == 0)
            {
                ShowHudNotification(
                    "~r~[FALLING VEHICLES ERROR]~w~ [FallingVehicles] kosong!"
                );

                return;
            }

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftFallingVehiclesDurationMs
                );

            int intervalMs =
                GetFallingVehicleSpawnIntervalMs(
                    activeDurationMs
                );

            // =====================================================
            // DURATION EXTEND SYSTEM
            // =====================================================
            // Effect masih aktif:
            //   segmen baru dimulai setelah segmen aktif selesai.
            //
            // Effect sudah selesai:
            //   segmen baru dimulai sekarang.
            //
            // Dengan default:
            //   +2 detik = +3 kendaraan
            //   spawn pada 0 ms, 666 ms, 1332 ms.
            // =====================================================
            bool effectStillActive =
                _fallingVehiclesEffectEndTime > 0 &&
                !HasGameTimeReached(
                    currentTime,
                    _fallingVehiclesEffectEndTime
                );

            int segmentStartTime =
                effectStillActive
                    ? _fallingVehiclesEffectEndTime
                    : currentTime;

            if (!effectStillActive)
            {
                _nextFallingVehicleActualSpawnTime =
                    currentTime;
            }

            int queuedVehicles =
                0;

            for (
                int spawnIndex = 0;
                spawnIndex < FALLING_VEHICLES_PER_GIFT;
                spawnIndex++)
            {
                uint selectedHash =
                    _fallingVehicleHashes[
                        _random.Next(
                            _fallingVehicleHashes.Count
                        )
                    ];

                Model model =
                    new Model(
                        (int)selectedHash
                    );

                if (!model.IsValid ||
                    !model.IsVehicle)
                {
                    continue;
                }

                _modelsToSpawn.Add(
                    new ModelLoadingTracker
                    {
                        Model =
                            model,

                        Player =
                            player,

                        Heading =
                            player.Heading,

                        ModelHash =
                            selectedHash,

                        SpawnNotBeforeTime =
                            segmentStartTime +
                            (
                                spawnIndex *
                                intervalMs
                            ),

                        SpawnIntervalMs =
                            intervalMs,

                        RequestStartTime =
                            0
                    }
                );

                queuedVehicles++;
            }

            _fallingVehiclesEffectEndTime =
                segmentStartTime +
                activeDurationMs;

            int remainingMs =
                Math.Max(
                    0,
                    _fallingVehiclesEffectEndTime -
                    currentTime
                );

            int remainingSeconds =
                (int)Math.Ceiling(
                    remainingMs /
                    1000.0
                );

            ShowHudNotification(
                "~o~FALLING VEHICLES! ~w~+" +
                (activeDurationMs / 1000) +
                " SEC | " +
                queuedVehicles +
                " VEHICLES | ACTIVE " +
                remainingSeconds +
                " SEC"
            );
        }

        private void ProcessFallingVehicleSpawns(
            int currentTime)
        {
            if (_modelsToSpawn.Count == 0)
            {
                return;
            }

            // =========================================================
            // VALIDATE + REQUEST SEMUA MODEL
            // =========================================================
            for (
                int i =
                    _modelsToSpawn.Count - 1;
                i >= 0;
                i--)
            {
                ModelLoadingTracker tracker =
                    _modelsToSpawn[i];

                if (!tracker.Model.IsValid ||
                    !tracker.Model.IsVehicle)
                {
                    _modelsToSpawn.RemoveAt(
                        i
                    );

                    continue;
                }

                if (tracker.Player == null ||
                    !tracker.Player.Exists() ||
                    tracker.Player.IsDead)
                {
                    tracker.Model.MarkAsNoLongerNeeded();

                    _modelsToSpawn.RemoveAt(
                        i
                    );

                    continue;
                }

                if (!tracker.Model.IsLoaded)
                {
                    // Jangan request seluruh model masa depan sekaligus ketika
                    // banyak gift menumpuk. Prefetch 1 detik sebelum jadwal
                    // supaya penggunaan streaming/model GTA tetap ringan.
                    int requestNotBeforeTime =
                        tracker.SpawnNotBeforeTime -
                        1000;

                    if (!HasGameTimeReached(
                            currentTime,
                            requestNotBeforeTime
                        ))
                    {
                        continue;
                    }

                    if (tracker.RequestStartTime <= 0)
                    {
                        tracker.RequestStartTime =
                            currentTime;
                    }

                    tracker.Model.Request();

                    if (
                        HasGameTimeReached(
                            currentTime,
                            tracker.SpawnNotBeforeTime
                        ) &&
                        unchecked(
                            currentTime -
                            tracker.RequestStartTime
                        ) >= MODEL_LOAD_TIMEOUT_MS
                    )
                    {
                        ShowHudNotification(
                            "~r~[FALLING VEHICLES TIMEOUT]~w~ Vehicle gagal load."
                        );

                        tracker.Model.MarkAsNoLongerNeeded();

                        _modelsToSpawn.RemoveAt(
                            i
                        );
                    }
                }
            }

            if (_modelsToSpawn.Count == 0)
            {
                return;
            }

            // Minimum spacing nyata. Kalau beberapa model baru selesai load
            // bersamaan, kendaraan tetap tidak boleh keluar menumpuk.
            if (
                _nextFallingVehicleActualSpawnTime > 0 &&
                !HasGameTimeReached(
                    currentTime,
                    _nextFallingVehicleActualSpawnTime
                )
            )
            {
                return;
            }

            int selectedIndex =
                -1;

            int selectedScheduleTime =
                int.MaxValue;

            // Ambil HANYA SATU tracker yang:
            // - model sudah loaded,
            // - jadwalnya sudah tiba,
            // - jadwalnya paling awal.
            for (
                int i = 0;
                i < _modelsToSpawn.Count;
                i++)
            {
                ModelLoadingTracker tracker =
                    _modelsToSpawn[i];

                if (!tracker.Model.IsLoaded)
                {
                    continue;
                }

                if (!HasGameTimeReached(
                        currentTime,
                        tracker.SpawnNotBeforeTime
                    ))
                {
                    continue;
                }

                if (
                    selectedIndex < 0 ||
                    tracker.SpawnNotBeforeTime <
                        selectedScheduleTime
                )
                {
                    selectedIndex =
                        i;

                    selectedScheduleTime =
                        tracker.SpawnNotBeforeTime;
                }
            }

            if (selectedIndex < 0)
            {
                return;
            }

            ModelLoadingTracker selectedTracker =
                _modelsToSpawn[
                    selectedIndex
                ];

            Ped player =
                selectedTracker.Player;

            Vector3 predictedPos =
                player.Position +
                (
                    player.Velocity *
                    0.65f
                );

            Vector3 spawnPos =
                predictedPos +
                new Vector3(
                    (float)(
                        _random.NextDouble() *
                        2.0 -
                        1.0
                    ),
                    (float)(
                        _random.NextDouble() *
                        2.0 -
                        1.0
                    ),
                    10.0f
                );

            Vehicle veh =
                Vehicle.Create(
                    selectedTracker.Model,
                    spawnPos,
                    selectedTracker.Heading
                );

            if (veh != null &&
                veh.Exists())
            {
                Function.Call(
                    Hash.SET_ENTITY_DYNAMIC,
                    veh.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_ENTITY_COLLISION,
                    veh.Handle,
                    true,
                    true
                );

                Function.Call(
                    Hash.SET_ENTITY_RECORDS_COLLISIONS,
                    veh.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_ENTITY_HAS_GRAVITY,
                    veh.Handle,
                    true
                );

                Vector3 directionToPlayer =
                    (
                        predictedPos +
                        new Vector3(
                            0.0f,
                            0.0f,
                            0.5f
                        )
                    ) -
                    spawnPos;

                float directionLength =
                    (float)Math.Sqrt(
                        (
                            directionToPlayer.X *
                            directionToPlayer.X
                        ) +
                        (
                            directionToPlayer.Y *
                            directionToPlayer.Y
                        ) +
                        (
                            directionToPlayer.Z *
                            directionToPlayer.Z
                        )
                    );

                if (directionLength >
                    0.001f)
                {
                    directionToPlayer.Normalize();
                }
                else
                {
                    directionToPlayer =
                        new Vector3(
                            0.0f,
                            0.0f,
                            -1.0f
                        );
                }

                veh.Velocity =
                    directionToPlayer *
                    35.0f;

                // TIDAK ADA despawn 3 detik dari waktu spawn.
                // Timer 3 detik baru dimulai SETELAH impact + explosion.
                _activeFallingVehicles.Add(
                    new FallingVehicleActiveTracker
                    {
                        Vehicle =
                            veh,

                        WasInAir =
                            true,

                        SpawnTime =
                            currentTime
                    }
                );
            }
            else
            {
                ShowHudNotification(
                    "~r~[FALLING VEHICLES ERROR]~w~ Gagal membuat vehicle!"
                );
            }

            selectedTracker.Model.MarkAsNoLongerNeeded();

            _modelsToSpawn.RemoveAt(
                selectedIndex
            );

            _nextFallingVehicleActualSpawnTime =
                currentTime +
                Math.Max(
                    1,
                    selectedTracker.SpawnIntervalMs
                );
        }

        private bool DidFallingVehicleHitPlayer(
            Vehicle vehicle,
            Ped player)
        {
            if (vehicle == null ||
                !vehicle.Exists() ||
                player == null ||
                !player.Exists())
            {
                return false;
            }

            bool touchingPlayer =
                Function.Call<bool>(
                    Hash.IS_ENTITY_TOUCHING_ENTITY,
                    vehicle.Handle,
                    player.Handle
                );

            bool damagedPlayer =
                Function.Call<bool>(
                    Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,
                    player.Handle,
                    vehicle.Handle,
                    true
                );

            if (touchingPlayer ||
                damagedPlayer)
            {
                return true;
            }

            // Jika player sedang berada di kendaraan, impact ke kendaraan
            // player juga dihitung sebagai "kena player".
            if (player.IsInVehicle())
            {
                Vehicle playerVehicle =
                    player.CurrentVehicle;

                if (
                    playerVehicle != null &&
                    playerVehicle.Exists() &&
                    playerVehicle.Handle !=
                        vehicle.Handle
                )
                {
                    bool touchingPlayerVehicle =
                        Function.Call<bool>(
                            Hash.IS_ENTITY_TOUCHING_ENTITY,
                            vehicle.Handle,
                            playerVehicle.Handle
                        );

                    bool damagedPlayerVehicle =
                        Function.Call<bool>(
                            Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,
                            playerVehicle.Handle,
                            vehicle.Handle,
                            true
                        );

                    if (
                        touchingPlayerVehicle ||
                        damagedPlayerVehicle
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool DidFallingVehicleHitGround(
            FallingVehicleActiveTracker tracker,
            int currentTime)
        {
            if (tracker == null ||
                tracker.Vehicle == null ||
                !tracker.Vehicle.Exists())
            {
                return false;
            }

            Vehicle vehicle =
                tracker.Vehicle;

            bool isInAir =
                Function.Call<bool>(
                    Hash.IS_ENTITY_IN_AIR,
                    vehicle.Handle
                );

            if (isInAir)
            {
                tracker.WasInAir =
                    true;
            }

            // Beri waktu arming singkat agar frame pertama setelah CreateVehicle
            // tidak salah dibaca sebagai ground impact.
            if (
                unchecked(
                    currentTime -
                    tracker.SpawnTime
                ) < 150
            )
            {
                return false;
            }

            // Cara utama: kendaraan sebelumnya berada di udara lalu sekarang
            // sudah tidak dianggap airborne oleh GTA.
            if (
                tracker.WasInAir &&
                !isInAir
            )
            {
                return true;
            }

            // Fallback untuk impact cepat yang langsung memantul lagi.
            // Collision recording diaktifkan saat spawn.
            bool hasCollided =
                Function.Call<bool>(
                    Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING,
                    vehicle.Handle
                );

            if (
                tracker.WasInAir &&
                hasCollided
            )
            {
                float groundZ =
                    GetGroundHeightCompat(
                        vehicle.Position
                    );

                // Toleransi cukup besar karena origin/center setiap model
                // vehicle berbeda (motor, sedan, bus, dump truck, dll).
                if (
                    Math.Abs(groundZ) >
                        0.001f &&
                    vehicle.Position.Z <=
                        groundZ + 6.0f
                )
                {
                    return true;
                }
            }

            return false;
        }

        private void ExplodeFallingVehicle(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            try
            {
                Function.Call(
                    Hash.EXPLODE_VEHICLE,
                    vehicle.Handle,
                    true,
                    false
                );
            }
            catch
            {
            }

            // Sesudah meledak, wreck tetap terlihat 3 detik,
            // lalu benar-benar dihapus.
            ScheduleEntityDespawn(
                vehicle,
                3 * 1000
            );
        }

        private void ProcessActiveFallingVehicles(
            int currentTime)
        {
            Ped player =
                Game.Player.Character;

            for (
                int i =
                    _activeFallingVehicles.Count - 1;
                i >= 0;
                i--)
            {
                FallingVehicleActiveTracker tracker =
                    _activeFallingVehicles[i];

                Vehicle vehicle =
                    tracker != null
                        ? tracker.Vehicle
                        : null;

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    _activeFallingVehicles.RemoveAt(
                        i
                    );

                    continue;
                }

                bool hitPlayer =
                    DidFallingVehicleHitPlayer(
                        vehicle,
                        player
                    );

                bool hitGround =
                    DidFallingVehicleHitGround(
                        tracker,
                        currentTime
                    );

                // Kalau vehicle sudah hancur oleh physics GTA sebelum check ini,
                // tetap mulai timer despawn 3 detik.
                bool alreadyDestroyed =
                    vehicle.IsDead;

                if (
                    !hitPlayer &&
                    !hitGround &&
                    !alreadyDestroyed
                )
                {
                    continue;
                }

                if (!alreadyDestroyed)
                {
                    ExplodeFallingVehicle(
                        vehicle
                    );
                }
                else
                {
                    ScheduleEntityDespawn(
                        vehicle,
                        3 * 1000
                    );
                }

                _activeFallingVehicles.RemoveAt(
                    i
                );
            }
        }

        private void ProcessEnemyVehicleCleanup(
            int currentTime)
        {
            for (int i = _enemyVehicles.Count - 1; i >= 0; i--)
            {
                Vehicle veh =
                    _enemyVehicles[i];

                // =========================================================
                // VEHICLE SUDAH TIDAK ADA
                // =========================================================
                if (veh == null ||
                    !veh.Exists())
                {
                    _enemyVehicles.RemoveAt(i);
                    continue;
                }

                bool hasLivingTroops = false;

                // =========================================================
                // CEK SEMUA PASUKAN YANG BERASAL DARI VEHICLE INI
                // =========================================================
                foreach (ActiveBossTracker boss in _activeBosses)
                {
                    if (boss == null ||
                        boss.BossPed == null ||
                        !boss.BossPed.Exists() ||
                        !boss.BossPed.IsAlive)
                    {
                        continue;
                    }

                    if (boss.AssignedVehicle != null &&
                        boss.AssignedVehicle.Exists() &&
                        boss.AssignedVehicle.Handle ==
                        veh.Handle)
                    {
                        hasLivingTroops = true;
                        break;
                    }
                }

                // =========================================================
                // CEK PASSENGER YANG MASIH LOADING
                //
                // Jangan mulai timer kalau passenger belum sempat spawn.
                // =========================================================
                bool hasPendingPassenger = false;

                if (!hasLivingTroops)
                {
                    foreach (
                        PassengerLoadingTracker passenger
                        in _passengersToSpawn)
                    {
                        if (passenger.Vehicle != null &&
                            passenger.Vehicle.Exists() &&
                            passenger.Vehicle.Handle ==
                            veh.Handle)
                        {
                            hasPendingPassenger = true;
                            break;
                        }
                    }
                }

                // =========================================================
                // MASIH ADA PASUKAN
                // =========================================================
                if (hasLivingTroops ||
                    hasPendingPassenger)
                {
                    continue;
                }

                // =========================================================
                // SEMUA PASUKAN SUDAH MATI
                //
                // Semua pasukan mati -> vehicle hilang 3 detik kemudian.
                // =========================================================
                ScheduleEntityDespawn(
                    veh,
                    STANDARD_DESPAWN_MS
                );

                // Tidak perlu diproses lagi oleh list vehicle aktif.
                _enemyVehicles.RemoveAt(i);
            }
        }

        private void TriggerPlayerAnimalTransform()
        {
            LoadGiftTimerConfigFromIni();

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftAnimalTransformDurationMs
                );

            // Gift normal saat masih transform = tambah durasi.
            // Random Chaos = reset menjadi tepat RandomChaosXDuration.
            if (_playerAnimalTransformPhase != 0)
            {
                if (_modeDurationOverrideMs > 0)
                {
                    if (_playerAnimalTransformPhase == 2)
                    {
                        _playerAnimalTransformEndTime =
                            currentTime +
                            activeDurationMs;
                    }
                    else if (_playerAnimalTransformPhase == 1)
                    {
                        _playerAnimalRequestedDurationMs =
                            activeDurationMs;

                        _playerAnimalExtraDurationMs =
                            0;
                    }
                }
                else
                {
                    if (_playerAnimalTransformPhase == 2)
                    {
                        _playerAnimalTransformEndTime +=
                            activeDurationMs;
                    }
                    else if (_playerAnimalTransformPhase == 1)
                    {
                        _playerAnimalExtraDurationMs +=
                            activeDurationMs;
                    }
                }

                ShowHudNotification(
                    "~o~[ANIMAL TRANSFORM] ~w~" +
                    (_modeDurationOverrideMs > 0
                        ? "RESET "
                        : "+") +
                    (activeDurationMs / 1000) +
                    " seconds"
                );

                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            // =========================================================
            // MODEL TRANSFORM KHUSUS
            //
            // Jangan ambil dari _animalDatabase / [AnimalX].
            // Angry Animal (a_c_mtlion) tetap khusus sistem spawn animal.
            // =========================================================
            if (_playerTransformAnimalModels == null ||
                _playerTransformAnimalNames == null ||
                _playerTransformAnimalModels.Length == 0 ||
                _playerTransformAnimalModels.Length !=
                    _playerTransformAnimalNames.Length)
            {
                ShowHudNotification(
                    "~r~[ANIMAL TRANSFORM ERROR]~w~ Transform animal model list invalid."
                );

                return;
            }

            int selectedIndex =
                _random.Next(
                    _playerTransformAnimalModels.Length
                );

            string selectedModelName =
                _playerTransformAnimalModels[
                    selectedIndex
                ];

            string selectedDisplayName =
                _playerTransformAnimalNames[
                    selectedIndex
                ];

            string parseModelName =
                selectedModelName.Trim();

            if (parseModelName.StartsWith(
                    "0x",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                parseModelName =
                    parseModelName.Substring(2);
            }

            Model animalModel =
                uint.TryParse(
                    parseModelName,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out uint animalHash
                )
                    ? new Model(
                        unchecked((int)animalHash)
                    )
                    : new Model(
                        selectedModelName
                    );

            if (!animalModel.IsValid ||
                !animalModel.IsPed)
            {
                ShowHudNotification(
                    "~r~[ANIMAL TRANSFORM ERROR]~w~ Model " +
                    selectedModelName +
                    " tidak valid / bukan PED."
                );

                return;
            }

            // Kalau sedang ada di sepeda/kendaraan, keluarkan dahulu.
            // Animal ped tidak dipaksa duduk di vehicle.
            if (player.IsInVehicle())
            {
                Function.Call(
                    Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                    player.Handle
                );
            }

            _playerAnimalSnapshot =
                CapturePlayerAnimalSnapshot(
                    player
                );

            if (_playerAnimalSnapshot == null)
            {
                return;
            }

            _playerAnimalDisplayName =
                string.IsNullOrWhiteSpace(
                    selectedDisplayName
                )
                    ? "ANIMAL"
                    : selectedDisplayName.ToUpperInvariant();

            _playerAnimalModelName =
                selectedModelName;

            _playerAnimalPendingModel =
                animalModel;

            _playerOriginalPendingModel =
                new Model(
                    _playerAnimalSnapshot.OriginalModelHash
                );

            _playerAnimalExtraDurationMs =
                0;

            _playerAnimalRequestedDurationMs =
                activeDurationMs;

            _playerAnimalTransformEndTime =
                0;

            _playerAnimalTransformPhase =
                1;

            _playerAnimalModelRequestStartTime =
                currentTime;

            animalModel.Request();

            ShowHudNotification(
                "~o~[ANIMAL TRANSFORM]~w~ Transforming into " +
                _playerAnimalDisplayName +
                "..."
            );
        }

        private PlayerAnimalTransformSnapshot CapturePlayerAnimalSnapshot(
            Ped player)
        {
            if (player == null ||
                !player.Exists())
            {
                return null;
            }

            PlayerAnimalTransformSnapshot snapshot =
                new PlayerAnimalTransformSnapshot
                {
                    OriginalModelHash =
                        player.Model.Hash,

                    Health =
                        Math.Max(
                            1,
                            _customHealth
                        ),


                    SelectedWeapon =
                        player.Weapons.Current != null
                            ? player.Weapons.Current.Hash
                            : WeaponHash.Unarmed
                };

            // Outfit components 0..11
            for (int component = 0;
                 component < 12;
                 component++)
            {
                snapshot.ComponentDrawable[component] =
                    Function.Call<int>(
                        Hash.GET_PED_DRAWABLE_VARIATION,
                        player.Handle,
                        component
                    );

                snapshot.ComponentTexture[component] =
                    Function.Call<int>(
                        Hash.GET_PED_TEXTURE_VARIATION,
                        player.Handle,
                        component
                    );

                snapshot.ComponentPalette[component] =
                    Function.Call<int>(
                        Hash.GET_PED_PALETTE_VARIATION,
                        player.Handle,
                        component
                    );
            }

            // Outfit props 0..7 (hat/glasses/ears/etc.)
            for (int prop = 0;
                 prop < 8;
                 prop++)
            {
                snapshot.PropIndex[prop] =
                    Function.Call<int>(
                        Hash.GET_PED_PROP_INDEX,
                        player.Handle,
                        prop
                    );

                snapshot.PropTexture[prop] =
                    Function.Call<int>(
                        Hash.GET_PED_PROP_TEXTURE_INDEX,
                        player.Handle,
                        prop
                    );
            }

            // Simpan weapon yang memang sedang dimiliki player.
            // Script saat ini memakai whitelist, tetapi snapshot ini membuat
            // restore tidak bergantung pada efek SET_PLAYER_MODEL.
            foreach (
                WeaponHash weaponHash
                in _defaultPlayerWeapons)
            {
                if (!player.Weapons.HasWeapon(
                        weaponHash
                    ))
                {
                    continue;
                }

                snapshot.Weapons.Add(
                    weaponHash
                );

                int ammo =
                    Function.Call<int>(
                        Hash.GET_AMMO_IN_PED_WEAPON,
                        player.Handle,
                        unchecked((uint)(int)weaponHash)
                    );

                snapshot.WeaponAmmo.Add(
                    Math.Max(
                        1,
                        ammo
                    )
                );
            }

            return snapshot;
        }

        private void ProcessPlayerAnimalTransform(
            int currentTime)
        {
            if (_playerAnimalTransformPhase == 0)
            {
                return;
            }

            // =========================================================
            // PHASE 1: LOAD ANIMAL + GANTI MODEL
            // =========================================================
            if (_playerAnimalTransformPhase == 1)
            {
                if (!_playerAnimalPendingModel.IsValid ||
                    !_playerAnimalPendingModel.IsPed)
                {
                    CancelPlayerAnimalTransformLoading(
                        "Animal model invalid."
                    );

                    return;
                }

                if (!_playerAnimalPendingModel.IsLoaded)
                {
                    _playerAnimalPendingModel.Request();

                    if (currentTime -
                        _playerAnimalModelRequestStartTime >=
                        MODEL_LOAD_TIMEOUT_MS)
                    {
                        CancelPlayerAnimalTransformLoading(
                            "Animal model load timeout."
                        );
                    }

                    return;
                }

                Ped oldPlayer =
                    Game.Player.Character;

                if (oldPlayer == null ||
                    !oldPlayer.Exists())
                {
                    return;
                }

                Vector3 currentPosition =
                    oldPlayer.Position;

                float currentHeading =
                    oldPlayer.Heading;

                int carryHealth =
                    Math.Max(
                        1,
                        _customHealth
                    );


                Function.Call(
                    Hash.SET_PLAYER_MODEL,
                    Game.Player.Handle,
                    unchecked((uint)_playerAnimalPendingModel.Hash)
                );

                Ped animalPlayer =
                    Game.Player.Character;

                if (animalPlayer == null ||
                    !animalPlayer.Exists())
                {
                    return;
                }

                animalPlayer.Position =
                    currentPosition;

                animalPlayer.Heading =
                    currentHeading;

                ApplyCarriedPlayerVitalsAfterModelSwap(
                    animalPlayer,
                    carryHealth
                );

                // Animal tidak boleh menerima weapon manusia.
                Function.Call(
                    Hash.REMOVE_ALL_PED_WEAPONS,
                    animalPlayer.Handle,
                    true
                );

                _playerAnimalPendingModel.MarkAsNoLongerNeeded();

                int activeDurationMs =
                    _playerAnimalRequestedDurationMs > 0
                        ? _playerAnimalRequestedDurationMs
                        : _giftAnimalTransformDurationMs;

                _playerAnimalTransformEndTime =
                    currentTime +
                    activeDurationMs +
                    _playerAnimalExtraDurationMs;

                int totalDurationMs =
                    activeDurationMs +
                    _playerAnimalExtraDurationMs;

                _playerAnimalExtraDurationMs =
                    0;

                _playerAnimalTransformPhase =
                    2;

                ShowHudNotification(
                    "~o~[ANIMAL TRANSFORM]~w~ " +
                    _playerAnimalDisplayName +
                    " for " +
                    (totalDurationMs / 1000) +
                    " seconds!"
                );

                return;
            }

            // =========================================================
            // PHASE 2: ANIMAL AKTIF
            // =========================================================
            if (_playerAnimalTransformPhase == 2)
            {
                if (currentTime <
                    _playerAnimalTransformEndTime)
                {
                    return;
                }

                Ped animalPlayer =
                    Game.Player.Character;

                int carryHealth =
                    _playerAnimalSnapshot != null
                        ? _playerAnimalSnapshot.Health
                        : 1;

                if (animalPlayer != null &&
                    animalPlayer.Exists())
                {
                    carryHealth =
                        Math.Max(
                            1,
                            animalPlayer.Health
                        );
                }

                if (_playerAnimalSnapshot != null)
                {
                    _playerAnimalSnapshot.Health =
                        carryHealth;

                }

                if (!_playerOriginalPendingModel.IsValid ||
                    !_playerOriginalPendingModel.IsPed)
                {
                    ShowHudNotification(
                        "~r~[ANIMAL RESTORE ERROR]~w~ Original player model invalid."
                    );

                    return;
                }

                _playerOriginalPendingModel.Request();

                _playerAnimalModelRequestStartTime =
                    currentTime;

                _playerAnimalTransformPhase =
                    3;

                return;
            }

            // =========================================================
            // PHASE 3: LOAD HUMAN MODEL + RESTORE SNAPSHOT
            // =========================================================
            if (_playerAnimalTransformPhase == 3)
            {
                if (!_playerOriginalPendingModel.IsLoaded)
                {
                    _playerOriginalPendingModel.Request();
                    return;
                }

                Ped animalPlayer =
                    Game.Player.Character;

                if (animalPlayer == null ||
                    !animalPlayer.Exists())
                {
                    return;
                }

                Vector3 currentPosition =
                    animalPlayer.Position;

                float currentHeading =
                    animalPlayer.Heading;

                int carryHealth =
                    _playerAnimalSnapshot != null
                        ? Math.Max(
                            1,
                            _playerAnimalSnapshot.Health
                        )
                        : Math.Max(
                            1,
                            animalPlayer.Health
                        );


                Function.Call(
                    Hash.SET_PLAYER_MODEL,
                    Game.Player.Handle,
                    unchecked((uint)_playerOriginalPendingModel.Hash)
                );

                Ped restoredPlayer =
                    Game.Player.Character;

                if (restoredPlayer == null ||
                    !restoredPlayer.Exists())
                {
                    return;
                }

                restoredPlayer.Position =
                    currentPosition;

                restoredPlayer.Heading =
                    currentHeading;

                RestorePlayerOutfitFromAnimalSnapshot(
                    restoredPlayer
                );

                ApplyCarriedPlayerVitalsAfterModelSwap(
                    restoredPlayer,
                    carryHealth
                );

                RestorePlayerWeaponsFromAnimalSnapshot(
                    restoredPlayer
                );

                _playerOriginalPendingModel.MarkAsNoLongerNeeded();

                ShowHudNotification(
                    "~g~[ANIMAL TRANSFORM] ~w~Player restored: health, weapons and outfit preserved."
                );

                ClearPlayerAnimalTransformState();
            }
        }

        private void RestorePlayerOutfitFromAnimalSnapshot(
            Ped player)
        {
            if (_playerAnimalSnapshot == null ||
                player == null ||
                !player.Exists())
            {
                return;
            }

            for (int component = 0;
                 component < 12;
                 component++)
            {
                Function.Call(
                    Hash.SET_PED_COMPONENT_VARIATION,
                    player.Handle,
                    component,
                    _playerAnimalSnapshot.ComponentDrawable[component],
                    _playerAnimalSnapshot.ComponentTexture[component],
                    _playerAnimalSnapshot.ComponentPalette[component]
                );
            }

            Function.Call(
                Hash.CLEAR_ALL_PED_PROPS,
                player.Handle
            );

            for (int prop = 0;
                 prop < 8;
                 prop++)
            {
                int propIndex =
                    _playerAnimalSnapshot.PropIndex[prop];

                if (propIndex < 0)
                {
                    Function.Call(
                        Hash.CLEAR_PED_PROP,
                        player.Handle,
                        prop
                    );

                    continue;
                }

                Function.Call(
                    Hash.SET_PED_PROP_INDEX,
                    player.Handle,
                    prop,
                    propIndex,
                    _playerAnimalSnapshot.PropTexture[prop],
                    true
                );
            }
        }

        private void RestorePlayerWeaponsFromAnimalSnapshot(
            Ped player)
        {
            if (player == null ||
                !player.Exists())
            {
                return;
            }

            // Kalau Random Chaos DISARM masih aktif, jangan bypass effect.
            if (IsActiveRandomChaosEffect(
                    "disarm"
                ))
            {
                ApplyChaosNoWeapon(
                    player
                );

                return;
            }

            // Gift DISARM biasa juga tidak boleh dibypass oleh restore animal.
            if (Game.GameTime <
                _giftDisarmEndTime)
            {
                ApplyChaosNoWeapon(
                    player
                );

                return;
            }

            Function.Call(
                Hash.REMOVE_ALL_PED_WEAPONS,
                player.Handle,
                true
            );

            if (_playerAnimalSnapshot != null)
            {
                for (
                    int i = 0;
                    i < _playerAnimalSnapshot.Weapons.Count;
                    i++)
                {
                    WeaponHash weaponHash =
                        _playerAnimalSnapshot.Weapons[i];

                    int ammo =
                        i < _playerAnimalSnapshot.WeaponAmmo.Count
                            ? Math.Max(
                                1,
                                _playerAnimalSnapshot.WeaponAmmo[i]
                            )
                            : 9999;

                    player.Weapons.Give(
                        weaponHash,
                        ammo,
                        false,
                        false
                    );
                }
            }

            // Sistem bawaan script tetap menjadi sumber akhir weapon player.
            GiveDefaultPlayerWeapons();

            WeaponHash selectedWeapon =
                _playerAnimalSnapshot != null
                    ? _playerAnimalSnapshot.SelectedWeapon
                    : WeaponHash.Unarmed;

            if (IsAllowedPlayerWeapon(
                    selectedWeapon
                ))
            {
                Function.Call(
                    Hash.SET_CURRENT_PED_WEAPON,
                    player.Handle,
                    unchecked((uint)(int)selectedWeapon),
                    true
                );
            }
        }

        private void CancelPlayerAnimalTransformLoading(
            string errorMessage)
        {
            if (_playerAnimalPendingModel.IsValid)
            {
                _playerAnimalPendingModel.MarkAsNoLongerNeeded();
            }

            ShowHudNotification(
                "~r~[ANIMAL TRANSFORM ERROR]~w~ " +
                errorMessage
            );

            ClearPlayerAnimalTransformState();
        }

        private void ClearPlayerAnimalTransformState()
        {
            _playerAnimalTransformPhase = 0;
            _playerAnimalTransformEndTime = 0;
            _playerAnimalExtraDurationMs = 0;
            _playerAnimalRequestedDurationMs = 0;
            _playerAnimalModelRequestStartTime = 0;
            _playerAnimalDisplayName = "";
            _playerAnimalModelName = "";
            _playerAnimalSnapshot = null;
            _playerAnimalPendingModel = default(Model);
            _playerOriginalPendingModel = default(Model);
        }

        private void RestorePlayerAnimalTransformOnAbort()
        {
            if (_playerAnimalTransformPhase == 0 ||
                _playerAnimalSnapshot == null)
            {
                return;
            }

            Ped currentPlayer =
                Game.Player.Character;

            Vector3 currentPosition =
                currentPlayer != null && currentPlayer.Exists()
                    ? currentPlayer.Position
                    : Vector3.Zero;

            float currentHeading =
                currentPlayer != null && currentPlayer.Exists()
                    ? currentPlayer.Heading
                    : 0.0f;

            int carryHealth =
                currentPlayer != null && currentPlayer.Exists()
                    ? Math.Max(1, currentPlayer.Health)
                    : Math.Max(1, _playerAnimalSnapshot.Health);


            Model originalModel =
                new Model(
                    _playerAnimalSnapshot.OriginalModelHash
                );

            if (originalModel.IsValid &&
                originalModel.IsPed)
            {
                originalModel.Request(1000);

                if (originalModel.IsLoaded)
                {
                    Function.Call(
                        Hash.SET_PLAYER_MODEL,
                        Game.Player.Handle,
                        unchecked((uint)originalModel.Hash)
                    );

                    Ped restoredPlayer =
                        Game.Player.Character;

                    if (restoredPlayer != null &&
                        restoredPlayer.Exists())
                    {
                        if (currentPosition != Vector3.Zero)
                        {
                            restoredPlayer.Position =
                                currentPosition;
                        }

                        restoredPlayer.Heading =
                            currentHeading;

                        RestorePlayerOutfitFromAnimalSnapshot(
                            restoredPlayer
                        );

                        ApplyCarriedPlayerVitalsAfterModelSwap(
                            restoredPlayer,
                            carryHealth
                        );

                        RestorePlayerWeaponsFromAnimalSnapshot(
                            restoredPlayer
                        );
                    }

                    originalModel.MarkAsNoLongerNeeded();
                }
            }

            ClearPlayerAnimalTransformState();
        }

        private bool TryGetPlayerVehicle(
            out Ped player,
            out Vehicle vehicle)
        {
            player =
                Game.Player.Character;

            vehicle =
                null;

            if (player == null ||
                !player.Exists() ||
                player.IsDead ||
                !player.IsInVehicle())
            {
                return false;
            }

            vehicle =
                player.CurrentVehicle;

            return
                vehicle != null &&
                vehicle.Exists();
        }

        private void ActivateGiveHealthWebhook()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead || _customDeathActive) return;
            LoadGiveHealthConfigFromIni();
            int before = _customHealth;
            _customHealth = (int)Math.Min(_playerMaxHealth, (long)_customHealth + Math.Max(0, _giveHealthAmount));
            _giveHealthShown = Math.Max(0, _customHealth - before);
            ShowInstantGiftHud(FormatHudText(_instantGiveHealthFormat, _giveHealthShown), true);
            ShowHudNotification("~g~HEALTH +" + _giveHealthShown);
        }

        private void ActivateUTurnWebhook()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    return;
                }

                float currentSpeed =
                    Function.Call<float>(
                        Hash.GET_ENTITY_SPEED,
                        vehicle.Handle
                    );

                vehicle.Heading =
                    (vehicle.Heading +
                     180.0f) %
                    360.0f;

                Function.Call(
                    Hash.SET_VEHICLE_FORWARD_SPEED,
                    vehicle.Handle,
                    currentSpeed
                );

                // Khusus U-TURN: kamera langsung snap mengikuti arah baru
                // kendaraan, tidak menunggu kamera GTA berputar sendiri.
                Function.Call(
                    Hash.SET_GAMEPLAY_CAM_RELATIVE_HEADING,
                    0.0f
                );
            }
            else
            {
                player.Heading =
                    (player.Heading +
                     180.0f) %
                    360.0f;
            }

            ShowHudNotification(
                "~y~U-TURN!"
            );
        }

        private bool IsGetTowedTwoWheelVehicle(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return false;
            }

            int vehicleClass =
                Function.Call<int>(
                    Hash.GET_VEHICLE_CLASS,
                    vehicle.Handle
                );

            return
                vehicleClass == 8 ||
                vehicleClass == 13;
        }

        private bool IsGetTowedTargetSupported(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return false;
            }

            // Jangan pernah mencoba tow rig itu sendiri.
            if (_getTowedTowTruck != null &&
                _getTowedTowTruck.Exists() &&
                vehicle.Handle ==
                    _getTowedTowTruck.Handle)
            {
                return false;
            }

            int vehicleClass =
                Function.Call<int>(
                    Hash.GET_VEHICLE_CLASS,
                    vehicle.Handle
                );

            // Class GTA:
            // 14 = Boats
            // 15 = Helicopters
            // 16 = Planes
            // 21 = Trains
            //
            // Empat class ini sengaja ditolak. Attachment paksa pada
            // entity jenis ini jauh lebih rawan physics/native crash.
            if (vehicleClass == 14 ||
                vehicleClass == 15 ||
                vehicleClass == 16 ||
                vehicleClass == 21)
            {
                return false;
            }

            return true;
        }

        private void HardenGetTowedDriverAndTruck()
        {
            if (_getTowedTowTruck != null &&
                _getTowedTowTruck.Exists())
            {
                Function.Call(
                    Hash.SET_ENTITY_INVINCIBLE,
                    _getTowedTowTruck.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_ENTITY_AS_MISSION_ENTITY,
                    _getTowedTowTruck.Handle,
                    true,
                    true
                );

                Function.Call(
                    Hash.SET_VEHICLE_ENGINE_ON,
                    _getTowedTowTruck.Handle,
                    true,
                    true,
                    false
                );
            }

            if (_getTowedDriver != null &&
                _getTowedDriver.Exists())
            {
                Function.Call(
                    Hash.SET_ENTITY_INVINCIBLE,
                    _getTowedDriver.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_ENTITY_AS_MISSION_ENTITY,
                    _getTowedDriver.Handle,
                    true,
                    true
                );

                Function.Call(
                    Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS,
                    _getTowedDriver.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_PED_FLEE_ATTRIBUTES,
                    _getTowedDriver.Handle,
                    0,
                    false
                );

                Function.Call(
                    Hash.SET_PED_CAN_RAGDOLL,
                    _getTowedDriver.Handle,
                    false
                );

                Function.Call(
                    Hash.SET_PED_CAN_BE_DRAGGED_OUT,
                    _getTowedDriver.Handle,
                    false
                );

                Function.Call(
                    Hash.SET_PED_STAY_IN_VEHICLE_WHEN_JACKED,
                    _getTowedDriver.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_PED_KEEP_TASK,
                    _getTowedDriver.Handle,
                    true
                );
            }
        }

        private void EnsureGetTowedTargetAttached()
        {
            Vehicle towTruck =
                _getTowedTowTruck;

            Vehicle target =
                _getTowedTargetVehicle;

            if (towTruck == null ||
                !towTruck.Exists() ||
                target == null ||
                !target.Exists() ||
                !IsGetTowedTargetSupported(target))
            {
                return;
            }

            // SAFETY:
            // Native ATTACH_VEHICLE_TO_TOW_TRUCK sengaja TIDAK dipakai lagi.
            // Native tow-hook GTA dapat tidak stabil pada beberapa model,
            // terutama saat target berubah/dihapus ketika physics aktif.
            //
            // Semua kendaraan yang didukung memakai rigid entity attachment
            // yang sama seperti sistem lama untuk motorcycle/bicycle.
            bool alreadyAttached =
                Function.Call<bool>(
                    Hash.IS_ENTITY_ATTACHED_TO_ENTITY,
                    target.Handle,
                    towTruck.Handle
                );

            if (alreadyAttached)
            {
                return;
            }

            // Bersihkan attachment lama tanpa memanggil native tow-hook.
            try
            {
                Function.Call(
                    Hash.DETACH_ENTITY,
                    target.Handle,
                    true,
                    true
                );
            }
            catch
            {
            }

            bool twoWheelVehicle =
                IsGetTowedTwoWheelVehicle(
                    target
                );

            float offsetY =
                twoWheelVehicle
                    ? -3.35f
                    : -4.15f;

            float offsetZ =
                twoWheelVehicle
                    ? 0.95f
                    : 0.75f;

            Function.Call(
                Hash.ATTACH_ENTITY_TO_ENTITY,
                target.Handle,
                towTruck.Handle,
                0,
                0.0f,
                offsetY,
                offsetZ,
                0.0f,
                0.0f,
                0.0f,
                false,
                false,
                false,
                false,
                2,
                true
            );
        }

        private void RefreshGetTowedDriverTask(
            int currentTime,
            bool forceRefresh)
        {
            if (_getTowedTowTruck == null ||
                !_getTowedTowTruck.Exists() ||
                _getTowedDriver == null ||
                !_getTowedDriver.Exists())
            {
                return;
            }

            // Penting: throttle dilakukan SEBELUM native lain.
            // Versi lama tetap menjalankan harden + cek seat setiap Tick,
            // walaupun TASK_VEHICLE_DRIVE_WANDER hanya perlu direfresh berkala.
            if (!forceRefresh &&
                currentTime <
                    _nextGetTowedDriverTaskRefreshTime)
            {
                return;
            }

            try
            {
                HardenGetTowedDriverAndTruck();

                Ped currentDriver =
                    _getTowedTowTruck.GetPedOnSeat(
                        VehicleSeat.Driver
                    );

                bool driverStillInSeat =
                    currentDriver != null &&
                    currentDriver.Exists() &&
                    currentDriver.Handle ==
                        _getTowedDriver.Handle;

                if (!driverStillInSeat)
                {
                    _getTowedDriver.SetIntoVehicle(
                        _getTowedTowTruck,
                        VehicleSeat.Driver
                    );
                }

                Function.Call(
                    Hash.TASK_VEHICLE_DRIVE_WANDER,
                    _getTowedDriver.Handle,
                    _getTowedTowTruck.Handle,
                    32.0f,
                    1074528293
                );

                Function.Call(
                    Hash.SET_PED_KEEP_TASK,
                    _getTowedDriver.Handle,
                    true
                );
            }
            catch
            {
                // Jangan biarkan exception SHVDN menghentikan Tick utama.
            }

            _nextGetTowedDriverTaskRefreshTime =
                currentTime +
                GET_TOWED_DRIVER_TASK_REFRESH_MS;
        }

        private void CleanupGetTowedRigOnly(
            bool detachTarget)
        {
            Vehicle target =
                _getTowedTargetVehicle;

            Ped driver =
                _getTowedDriver;

            Vehicle towTruck =
                _getTowedTowTruck;

            // Putus referensi rig lebih dulu supaya Tick berikutnya tidak
            // memakai entity yang sedang dalam proses cleanup.
            _getTowedDriver = null;
            _getTowedTowTruck = null;

            if (detachTarget &&
                target != null &&
                target.Exists())
            {
                try
                {
                    Function.Call(
                        Hash.DETACH_ENTITY,
                        target.Handle,
                        true,
                        true
                    );
                }
                catch
                {
                }
            }

            try
            {
                if (driver != null &&
                    driver.Exists())
                {
                    driver.Delete();
                }
            }
            catch
            {
            }

            try
            {
                if (towTruck != null &&
                    towTruck.Exists())
                {
                    towTruck.Delete();
                }
            }
            catch
            {
            }

            _nextGetTowedAttachmentRefreshTime = 0;
            _nextGetTowedDriverTaskRefreshTime = 0;
        }

        private bool SpawnGetTowedRigForTarget(
            Vehicle targetVehicle)
        {
            if (targetVehicle == null ||
                !targetVehicle.Exists() ||
                !IsGetTowedTargetSupported(
                    targetVehicle
                ))
            {
                return false;
            }

            // Bersihkan rig lama dulu tetapi pertahankan timer effect.
            CleanupGetTowedRigOnly(
                true
            );

            Model towModel =
                new Model(
                    "towtruck"
                );

            Model driverModel =
                new Model(
                    "s_m_m_trucker_01"
                );

            if (!towModel.IsValid ||
                !driverModel.IsValid)
            {
                return false;
            }

            towModel.Request(
                1000
            );

            driverModel.Request(
                1000
            );

            if (!towModel.IsLoaded ||
                !driverModel.IsLoaded)
            {
                towModel.MarkAsNoLongerNeeded();
                driverModel.MarkAsNoLongerNeeded();
                return false;
            }

            Vector3 spawnPos =
                targetVehicle.Position -
                (
                    targetVehicle.ForwardVector *
                    9.0f
                ) +
                (
                    Vector3.WorldUp *
                    1.0f
                );

            spawnPos =
                SnapEffectPositionToGround(
                    spawnPos,
                    0.5f
                );

            Vehicle towTruck =
                Vehicle.Create(
                    towModel,
                    spawnPos,
                    targetVehicle.Heading
                );

            if (towTruck == null ||
                !towTruck.Exists())
            {
                towModel.MarkAsNoLongerNeeded();
                driverModel.MarkAsNoLongerNeeded();
                return false;
            }

            towTruck.PlaceOnGround();

            Ped towDriver =
                Ped.Create(
                    driverModel,
                    towTruck.Position,
                    towTruck.Heading
                );

            if (towDriver == null ||
                !towDriver.Exists())
            {
                towTruck.Delete();

                towModel.MarkAsNoLongerNeeded();
                driverModel.MarkAsNoLongerNeeded();
                return false;
            }

            _getTowedTowTruck =
                towTruck;

            _getTowedDriver =
                towDriver;

            _getTowedTargetVehicle =
                targetVehicle;

            _nextGetTowedAttachmentRefreshTime =
                0;

            _nextGetTowedDriverTaskRefreshTime =
                0;

            towDriver.SetIntoVehicle(
                towTruck,
                VehicleSeat.Driver
            );

            Function.Call(
                Hash.SET_DRIVER_ABILITY,
                towDriver.Handle,
                1.0f
            );

            Function.Call(
                Hash.SET_DRIVER_AGGRESSIVENESS,
                towDriver.Handle,
                1.0f
            );

            HardenGetTowedDriverAndTruck();

            EnsureGetTowedTargetAttached();

            RefreshGetTowedDriverTask(
                Game.GameTime,
                true
            );

            towModel.MarkAsNoLongerNeeded();
            driverModel.MarkAsNoLongerNeeded();

            return true;
        }

        private void ActivateGetTowedWebhook()
        {
            LoadGiftTimerConfigFromIni();

            // =========================================================
            // GET TOWED - TIMER ALWAYS STARTS
            // =========================================================
            // Gift tidak boleh dibatalkan hanya karena saat gift masuk
            // player sedang berjalan kaki / belum mempunyai kendaraan.
            //
            // Timer adalah sumber utama state effect:
            // - Jika player punya kendaraan valid sekarang -> langsung tow.
            // - Jika player sedang jalan kaki -> timer tetap berjalan.
            // - Jika sebelum timer habis player masuk kendaraan valid,
            //   ProcessGetTowedEffect() otomatis membuat rig tow.
            // - Jika timer habis tanpa kendaraan -> effect selesai normal.
            // =========================================================

            // Gift baru selalu memulai state/timer baru.
            CleanupGetTowedEffect();

            int activeDurationMs =
                GetModeEffectDuration(
                    _getTowedDurationMs
                );

            _getTowedEndTime =
                Game.GameTime +
                activeDurationMs;

            _getTowedTargetVehicle =
                null;

            _nextGetTowedRespawnAttemptTime =
                0;

            Ped player =
                Game.Player.Character;

            Vehicle targetVehicle =
                player != null &&
                player.Exists() &&
                !player.IsDead &&
                player.IsInVehicle()
                    ? player.CurrentVehicle
                    : null;

            // Player sedang jalan kaki:
            // jangan cancel gift. Biarkan ProcessGetTowedEffect()
            // menunggu kendaraan sambil timer terus berkurang.
            if (targetVehicle == null ||
                !targetVehicle.Exists())
            {
                ShowHudNotification(
                    "~o~GET TOWED!~w~ " +
                    (activeDurationMs / 1000) +
                    " SEC ~y~(WAITING FOR VEHICLE)"
                );

                return;
            }

            // Kendaraan yang memang tidak aman untuk sistem towing juga
            // tidak membatalkan timer. Effect tetap menunggu sampai player
            // memakai kendaraan darat yang didukung sebelum timer habis.
            if (!IsGetTowedTargetSupported(
                    targetVehicle
                ))
            {
                ShowHudNotification(
                    "~o~GET TOWED!~w~ " +
                    (activeDurationMs / 1000) +
                    " SEC ~y~(WAITING FOR SUPPORTED VEHICLE)"
                );

                return;
            }

            _getTowedTargetVehicle =
                targetVehicle;

            if (!SpawnGetTowedRigForTarget(
                    targetVehicle
                ))
            {
                // Spawn rig boleh gagal karena model/streaming belum siap.
                // JANGAN cancel timer. ProcessGetTowedEffect() akan retry.
                _getTowedTargetVehicle =
                    targetVehicle;

                _nextGetTowedRespawnAttemptTime =
                    Game.GameTime +
                    GET_TOWED_RESPAWN_RETRY_MS;

                ShowHudNotification(
                    "~o~GET TOWED!~w~ " +
                    (activeDurationMs / 1000) +
                    " SEC ~y~(TOW RETRY)"
                );

                return;
            }

            ShowHudNotification(
                "~o~GET TOWED!~w~ " +
                (activeDurationMs / 1000) +
                " SEC"
            );
        }

        private void ProcessGetTowedEffect(
            int currentTime)
        {
            if (_getTowedEndTime <= 0)
                return;

            // TIMER adalah sumber utama. Baru benar-benar selesai ketika timer habis.
            if (currentTime >=
                _getTowedEndTime)
            {
                CleanupGetTowedEffect();
                return;
            }

            Ped player =
                Game.Player.Character;

            Vehicle currentPlayerVehicle =
                player != null &&
                player.Exists() &&
                !player.IsDead &&
                player.IsInVehicle()
                    ? player.CurrentVehicle
                    : null;

            bool playerHasVehicle =
                currentPlayerVehicle != null &&
                currentPlayerVehicle.Exists();

            bool targetValid =
                _getTowedTargetVehicle != null &&
                _getTowedTargetVehicle.Exists();

            bool rigValid =
                _getTowedTowTruck != null &&
                _getTowedTowTruck.Exists() &&
                _getTowedDriver != null &&
                _getTowedDriver.Exists() &&
                !_getTowedDriver.IsDead;

            // GIVE VEHICLE / replace dapat membuat target lama hilang atau
            // membuat player sekarang berada di kendaraan baru. Selama timer
            // masih aktif, effect TIDAK BOLEH cleanup permanen.
            bool playerChangedVehicle =
                playerHasVehicle &&
                (
                    !targetValid ||
                    currentPlayerVehicle.Handle !=
                        _getTowedTargetVehicle.Handle
                );

            if (!rigValid ||
                !targetValid ||
                playerChangedVehicle)
            {
                if (currentTime <
                    _nextGetTowedRespawnAttemptTime)
                {
                    return;
                }

                _nextGetTowedRespawnAttemptTime =
                    currentTime +
                    GET_TOWED_RESPAWN_RETRY_MS;

                Vehicle desiredTarget =
                    playerHasVehicle
                        ? currentPlayerVehicle
                        : (
                            targetValid
                                ? _getTowedTargetVehicle
                                : null
                          );

                if (desiredTarget != null &&
                    desiredTarget.Exists() &&
                    !IsGetTowedTargetSupported(
                        desiredTarget
                    ))
                {
                    desiredTarget =
                        null;
                }

                if (desiredTarget != null &&
                    desiredTarget.Exists())
                {
                    if (SpawnGetTowedRigForTarget(
                            desiredTarget
                        ))
                    {
                        _nextGetTowedRespawnAttemptTime =
                            0;
                    }
                }
                else
                {
                    // Tidak ada kendaraan valid untuk saat ini.
                    // Hapus rig fisik saja dan TUNGGU. Timer tetap berjalan.
                    // Begitu GIVE VEHICLE memberi kendaraan baru, tow akan
                    // otomatis muncul lagi pada tick berikutnya.
                    CleanupGetTowedRigOnly(
                        true
                    );

                    _getTowedTargetVehicle =
                        null;
                }

                return;
            }

            // Driver dipertahankan di kursi dan task wander direfresh.
            RefreshGetTowedDriverTask(
                currentTime,
                false
            );

            // Re-lock attachment secara berkala.
            if (currentTime >=
                _nextGetTowedAttachmentRefreshTime)
            {
                EnsureGetTowedTargetAttached();

                _nextGetTowedAttachmentRefreshTime =
                    currentTime +
                    GET_TOWED_ATTACHMENT_REFRESH_MS;
            }
        }

        private void CleanupGetTowedEffect()
        {
            Vehicle target =
                _getTowedTargetVehicle;

            Ped driver =
                _getTowedDriver;

            Vehicle towTruck =
                _getTowedTowTruck;

            // Reset state lebih dulu supaya tidak ada Tick berikutnya yang
            // mencoba menggunakan handle entity yang sedang dihapus.
            _getTowedDriver = null;
            _getTowedTowTruck = null;
            _getTowedTargetVehicle = null;

            _getTowedEndTime = 0;
            _nextGetTowedAttachmentRefreshTime = 0;
            _nextGetTowedDriverTaskRefreshTime = 0;
            _nextGetTowedRespawnAttemptTime = 0;

            if (target != null &&
                target.Exists())
            {
                try
                {
                    Function.Call(
                        Hash.DETACH_ENTITY,
                        target.Handle,
                        true,
                        true
                    );
                }
                catch
                {
                }
            }

            try
            {
                if (driver != null &&
                    driver.Exists())
                {
                    driver.Delete();
                }
            }
            catch
            {
            }

            try
            {
                if (towTruck != null &&
                    towTruck.Exists())
                {
                    towTruck.Delete();
                }
            }
            catch
            {
            }
        }

        private void ActivateTrafficMagnetWebhook()
        {
            LoadGiftTimerConfigFromIni();

            int activeDurationMs =
                GetModeEffectDuration(
                    _trafficMagnetDurationMs
                );

            _trafficMagnetEndTime =
                Game.GameTime +
                activeDurationMs;

            _nextTrafficMagnetUpdateTime = 0;

            ShowHudNotification(
                "~r~TRAFFIC MAGNET!~w~ " +
                (activeDurationMs / 1000) +
                " SEC"
            );
        }

        private void ProcessTrafficMagnetEffect(
            int currentTime)
        {
            if (_trafficMagnetEndTime <= 0)
                return;

            if (currentTime >=
                _trafficMagnetEndTime)
            {
                _trafficMagnetEndTime =
                    0;
                _nextTrafficMagnetUpdateTime =
                    0;

                return;
            }

            if (currentTime <
                _nextTrafficMagnetUpdateTime)
            {
                return;
            }

            _nextTrafficMagnetUpdateTime =
                currentTime +
                TRAFFIC_MAGNET_UPDATE_INTERVAL_MS;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            Vehicle playerVehicle =
                player.IsInVehicle()
                    ? player.CurrentVehicle
                    : null;

            // TRAFFIC MAGNET tidak pernah menarget karakter.
            // Kalau player sedang jalan kaki, timer tetap berjalan tetapi
            // tidak ada kendaraan yang ditarik.
            if (playerVehicle == null ||
                !playerVehicle.Exists())
            {
                return;
            }

            Vector3 targetPosition =
                playerVehicle.Position;

            Vehicle[] nearbyVehicles =
                World.GetNearbyVehicles(
                    player.Position,
                    TRAFFIC_MAGNET_RADIUS
                );

            foreach (
                Vehicle vehicle in
                nearbyVehicles)
            {
                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    continue;
                }

                // Jangan magnet kendaraan yang sedang dipakai player.
                if (playerVehicle != null &&
                    playerVehicle.Exists() &&
                    vehicle.Handle ==
                        playerVehicle.Handle)
                {
                    continue;
                }

                Vector3 direction =
                    targetPosition -
                    vehicle.Position;

                float distance =
                    direction.Length();

                bool touchingPlayerVehicle =
                    Function.Call<bool>(
                        Hash.IS_ENTITY_TOUCHING_ENTITY,
                        vehicle.Handle,
                        playerVehicle.Handle
                    );

                bool collidedVeryClose =
                    distance <= 4.5f &&
                    Function.Call<bool>(
                        Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING,
                        vehicle.Handle
                    );

                // Sekali traffic vehicle menghantam kendaraan player,
                // hapus langsung agar tidak menumpuk dan menurunkan FPS.
                // Fallback jarak+collision menangkap impact singkat yang kadang
                // lolos dari IS_ENTITY_TOUCHING_ENTITY di antara dua tick.
                if (touchingPlayerVehicle ||
                    collidedVeryClose)
                {
                    vehicle.Delete();
                    continue;
                }

                if (distance <=
                    0.75f)
                {
                    continue;
                }

                direction.Normalize();

                // Semakin dekat tetap diarahkan kuat ke player supaya benar-benar
                // "nempel"/menabrak, tetapi Z dibatasi supaya tidak terbang liar.
                float pullSpeed =
                    TRAFFIC_MAGNET_PULL_SPEED;

                Vector3 velocity =
                    direction *
                    pullSpeed;

                velocity.Z =
                    Math.Max(
                        -3.0f,
                        Math.Min(
                            4.0f,
                            velocity.Z
                        )
                    );

                Function.Call(
                    Hash.SET_ENTITY_VELOCITY,
                    vehicle.Handle,
                    velocity.X,
                    velocity.Y,
                    velocity.Z
                );
            }
        }

        private void ActivateRecklessTrafficWebhook()
        {
            LoadGiftTimerConfigFromIni();

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _recklessTrafficDurationMs
                );

            _recklessTrafficEndTime =
                currentTime +
                activeDurationMs;

            // Scan langsung pada Tick berikutnya.
            _nextRecklessTrafficUpdateTime =
                0;

            // NOS burst pertama juga langsung aktif.
            _nextRecklessTrafficNitroTime =
                currentTime;

            ShowHudNotification(
                "~r~RECKLESS TRAFFIC 500M!~w~ " +
                (activeDurationMs / 1000) +
                " SEC"
            );
        }

        private bool IsRecklessTrafficRoadVehicle(
            Vehicle vehicle)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return false;
            }

            int vehicleClass =
                Function.Call<int>(
                    Hash.GET_VEHICLE_CLASS,
                    vehicle.Handle
                );

            // 14 = boat, 15 = helicopter, 16 = plane, 21 = train.
            // Mobil, truck, bus, van, emergency vehicle dan MOTOR tetap kena.
            return
                vehicleClass != 14 &&
                vehicleClass != 15 &&
                vehicleClass != 16 &&
                vehicleClass != 21;
        }

        private void ApplyRecklessTrafficNos(
            Vehicle vehicle,
            bool doSpeedBurst)
        {
            if (vehicle == null ||
                !vehicle.Exists() ||
                !IsRecklessTrafficRoadVehicle(vehicle))
            {
                return;
            }

            // SET_VEHICLE_CHEAT_POWER_INCREASE.
            // Native ini harus dipanggil berulang supaya torque boost terus aktif.
            Function.Call(
                (Hash)0xB59E4BD37AE292DBUL,
                vehicle.Handle,
                RECKLESS_TRAFFIC_NOS_POWER
            );

            // Boost audio. Tidak bergantung kendaraan memang punya rocket boost.
            Function.Call(
                (Hash)0x4A04DE7CAB2739A1UL,
                vehicle.Handle,
                true
            );

            if (!doSpeedBurst)
                return;

            float currentSpeed =
                Function.Call<float>(
                    Hash.GET_ENTITY_SPEED,
                    vehicle.Handle
                );

            float boostedSpeed =
                Math.Min(
                    RECKLESS_TRAFFIC_NOS_MAX_SPEED,
                    Math.Max(
                        RECKLESS_TRAFFIC_NOS_MIN_SPEED,
                        currentSpeed +
                            RECKLESS_TRAFFIC_NOS_SPEED_ADD
                    )
                );

            Function.Call(
                Hash.SET_VEHICLE_FORWARD_SPEED,
                vehicle.Handle,
                boostedSpeed
            );
        }

        private void ConfigureRecklessTrafficVehicle(
            Vehicle vehicle,
            Ped player,
            bool doSpeedBurst)
        {
            if (vehicle == null ||
                !vehicle.Exists() ||
                !IsRecklessTrafficRoadVehicle(vehicle))
            {
                return;
            }

            Vehicle playerVehicle =
                player != null &&
                player.Exists() &&
                player.IsInVehicle()
                    ? player.CurrentVehicle
                    : null;

            if (playerVehicle != null &&
                playerVehicle.Exists() &&
                vehicle.Handle ==
                    playerVehicle.Handle)
            {
                return;
            }

            Ped driver =
                vehicle.Driver;

            if (driver == null ||
                !driver.Exists() ||
                driver.IsDead ||
                (
                    player != null &&
                    player.Exists() &&
                    driver.Handle ==
                        player.Handle
                ))
            {
                return;
            }

            // Driver jangan takut lalu keluar / membatalkan task karena chaos.
            Function.Call(
                Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS,
                driver.Handle,
                true
            );

            Function.Call(
                Hash.SET_PED_KEEP_TASK,
                driver.Handle,
                true
            );

            Function.Call(
                Hash.SET_DRIVER_ABILITY,
                driver.Handle,
                1.0f
            );

            Function.Call(
                Hash.SET_DRIVER_AGGRESSIVENESS,
                driver.Handle,
                1.0f
            );

            // Style yang sudah dipakai script sebelumnya, tetapi speed dinaikkan
            // supaya traffic benar-benar rusuh dan tidak kembali santai.
            Function.Call(
                Hash.TASK_VEHICLE_DRIVE_WANDER,
                driver.Handle,
                vehicle.Handle,
                RECKLESS_TRAFFIC_DRIVE_SPEED,
                1074528293
            );

            ApplyRecklessTrafficNos(
                vehicle,
                doSpeedBurst
            );

            bool alreadyTracked =
                _recklessTrafficTouchedVehicles.Any(
                    v =>
                        v != null &&
                        v.Exists() &&
                        v.Handle ==
                            vehicle.Handle
                );

            if (!alreadyTracked)
            {
                _recklessTrafficTouchedVehicles.Add(
                    vehicle
                );
            }
        }

        private void RestoreRecklessTrafficVehicles()
        {
            Ped currentPlayer =
                Game.Player.Character;

            for (
                int i =
                    _recklessTrafficTouchedVehicles.Count - 1;
                i >= 0;
                i--)
            {
                Vehicle vehicle =
                    _recklessTrafficTouchedVehicles[i];

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    continue;
                }

                // Matikan boost audio dan kembalikan torque normal.
                Function.Call(
                    (Hash)0x4A04DE7CAB2739A1UL,
                    vehicle.Handle,
                    false
                );

                Function.Call(
                    (Hash)0xB59E4BD37AE292DBUL,
                    vehicle.Handle,
                    1.0f
                );

                Ped driver =
                    vehicle.Driver;

                if (driver == null ||
                    !driver.Exists() ||
                    driver.IsDead ||
                    (
                        currentPlayer != null &&
                        currentPlayer.Exists() &&
                        driver.Handle ==
                            currentPlayer.Handle
                    ))
                {
                    continue;
                }

                Function.Call(
                    Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS,
                    driver.Handle,
                    false
                );

                Function.Call(
                    Hash.SET_DRIVER_AGGRESSIVENESS,
                    driver.Handle,
                    0.25f
                );

                Function.Call(
                    Hash.TASK_VEHICLE_DRIVE_WANDER,
                    driver.Handle,
                    vehicle.Handle,
                    12.0f,
                    786603
                );
            }

            _recklessTrafficTouchedVehicles.Clear();
        }

        private void ProcessRecklessTrafficEffect(
            int currentTime)
        {
            if (_recklessTrafficEndTime <= 0)
                return;

            if (currentTime >=
                _recklessTrafficEndTime)
            {
                RestoreRecklessTrafficVehicles();

                _recklessTrafficEndTime =
                    0;

                _nextRecklessTrafficUpdateTime =
                    0;

                _nextRecklessTrafficNitroTime =
                    0;

                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            bool doSpeedBurst =
                currentTime >=
                    _nextRecklessTrafficNitroTime;

            if (doSpeedBurst)
            {
                _nextRecklessTrafficNitroTime =
                    currentTime +
                    RECKLESS_TRAFFIC_NITRO_INTERVAL_MS;
            }

            // =====================================================
            // NOS PERSISTENT UNTUK SEMUA TRAFFIC YANG SUDAH KENA
            // =====================================================
            // Torque boost dipanggil setiap Tick. Vehicle yang sudah keluar
            // dari area 500 m dilepas dari tracker supaya tidak terus diproses.
            for (
                int i =
                    _recklessTrafficTouchedVehicles.Count - 1;
                i >= 0;
                i--)
            {
                Vehicle touchedVehicle =
                    _recklessTrafficTouchedVehicles[i];

                if (touchedVehicle == null ||
                    !touchedVehicle.Exists())
                {
                    _recklessTrafficTouchedVehicles.RemoveAt(i);
                    continue;
                }

                float distanceFromPlayer =
                    (
                        touchedVehicle.Position -
                        player.Position
                    ).Length();

                if (distanceFromPlayer >
                    RECKLESS_TRAFFIC_RADIUS +
                    25.0f)
                {
                    Function.Call(
                        (Hash)0x4A04DE7CAB2739A1UL,
                        touchedVehicle.Handle,
                        false
                    );

                    _recklessTrafficTouchedVehicles.RemoveAt(i);
                    continue;
                }

                Vehicle playerVehicle =
                    player.IsInVehicle()
                        ? player.CurrentVehicle
                        : null;

                if (playerVehicle != null &&
                    playerVehicle.Exists() &&
                    touchedVehicle.Handle ==
                        playerVehicle.Handle)
                {
                    Function.Call(
                        (Hash)0x4A04DE7CAB2739A1UL,
                        touchedVehicle.Handle,
                        false
                    );

                    _recklessTrafficTouchedVehicles.RemoveAt(i);
                    continue;
                }

                ApplyRecklessTrafficNos(
                    touchedVehicle,
                    doSpeedBurst
                );
            }

            // Scan 500 meter berkala untuk menangkap traffic baru yang masuk
            // streaming area saat player bergerak.
            if (currentTime <
                _nextRecklessTrafficUpdateTime)
            {
                return;
            }

            _nextRecklessTrafficUpdateTime =
                currentTime +
                RECKLESS_TRAFFIC_SCAN_INTERVAL_MS;

            Vehicle[] nearbyVehicles =
                World.GetNearbyVehicles(
                    player.Position,
                    RECKLESS_TRAFFIC_RADIUS
                );

            foreach (
                Vehicle vehicle in
                nearbyVehicles)
            {
                ConfigureRecklessTrafficVehicle(
                    vehicle,
                    player,
                    doSpeedBurst
                );
            }
        }

        private string GetRemoveTireWheelBoneName(
            VehicleWheel wheel)
        {
            if (wheel == null)
                return null;

            switch (wheel.BoneId)
            {
                case VehicleWheelBoneId.WheelLeftFront:
                    return "wheel_lf";

                case VehicleWheelBoneId.WheelRightFront:
                    return "wheel_rf";

                case VehicleWheelBoneId.WheelLeftRear:
                    return "wheel_lr";

                case VehicleWheelBoneId.WheelRightRear:
                    return "wheel_rr";

                case VehicleWheelBoneId.WheelLeftMiddle1:
                    return "wheel_lm1";

                case VehicleWheelBoneId.WheelRightMiddle1:
                    return "wheel_rm1";

                case VehicleWheelBoneId.WheelLeftMiddle2:
                    return "wheel_lm2";

                case VehicleWheelBoneId.WheelRightMiddle2:
                    return "wheel_rm2";

                case VehicleWheelBoneId.WheelLeftMiddle3:
                    return "wheel_lm3";

                case VehicleWheelBoneId.WheelRightMiddle3:
                    return "wheel_rm3";
            }

            return null;
        }

        private bool IsRemoveTireLeftWheel(
            VehicleWheel wheel)
        {
            if (wheel == null)
                return false;

            switch (wheel.BoneId)
            {
                case VehicleWheelBoneId.WheelLeftFront:
                case VehicleWheelBoneId.WheelLeftRear:
                case VehicleWheelBoneId.WheelLeftMiddle1:
                case VehicleWheelBoneId.WheelLeftMiddle2:
                case VehicleWheelBoneId.WheelLeftMiddle3:
                    return true;
            }

            return false;
        }

        private Vector3 GetRemoveTireWheelWorldPosition(
            Vehicle vehicle,
            VehicleWheel wheel)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return Vector3.Zero;
            }

            string boneName =
                GetRemoveTireWheelBoneName(
                    wheel
                );

            if (!string.IsNullOrWhiteSpace(
                    boneName
                ))
            {
                try
                {
                    int boneIndex =
                        Function.Call<int>(
                            Hash.GET_ENTITY_BONE_INDEX_BY_NAME,
                            vehicle.Handle,
                            boneName
                        );

                    if (boneIndex >= 0)
                    {
                        Vector3 bonePosition =
                            Function.Call<Vector3>(
                                Hash.GET_WORLD_POSITION_OF_ENTITY_BONE,
                                vehicle.Handle,
                                boneIndex
                            );

                        if (bonePosition !=
                            Vector3.Zero)
                        {
                            return bonePosition;
                        }
                    }
                }
                catch
                {
                }
            }

            // Fallback bila model kendaraan memakai skeleton roda yang tidak biasa.
            Vector3 forward =
                vehicle.ForwardVector;

            Vector3 right =
                new Vector3(
                    -forward.Y,
                    forward.X,
                    0.0f
                );

            if (right.Length() > 0.001f)
                right.Normalize();

            float side =
                IsRemoveTireLeftWheel(wheel)
                    ? -1.0f
                    : 1.0f;

            float longitudinal =
                0.0f;

            if (wheel != null)
            {
                switch (wheel.BoneId)
                {
                    case VehicleWheelBoneId.WheelLeftFront:
                    case VehicleWheelBoneId.WheelRightFront:
                        longitudinal = 1.35f;
                        break;

                    case VehicleWheelBoneId.WheelLeftRear:
                    case VehicleWheelBoneId.WheelRightRear:
                        longitudinal = -1.35f;
                        break;
                }
            }

            return
                vehicle.Position +
                (right * side * 0.95f) +
                (forward * longitudinal) -
                (Vector3.WorldUp * 0.35f);
        }

        private Prop SpawnDetachedRollingWheel(
            Vehicle vehicle,
            VehicleWheel sourceWheel)
        {
            if (vehicle == null ||
                !vehicle.Exists() ||
                sourceWheel == null)
            {
                return null;
            }

            // prop_wheel_01..06 adalah wheel lengkap (ban + pelek).
            // Beberapa model kendaraan/DLC bisa gagal load satu prop tertentu,
            // jadi tersedia beberapa fallback.
            string[] wheelPropModels =
            {
                "prop_wheel_01",
                "prop_wheel_02",
                "prop_wheel_03",
                "prop_wheel_04",
                "prop_wheel_05",
                "prop_wheel_06",
                "prop_stockade_wheel"
            };

            int startIndex =
                _random.Next(
                    wheelPropModels.Length
                );

            Vector3 wheelPosition =
                GetRemoveTireWheelWorldPosition(
                    vehicle,
                    sourceWheel
                );

            if (wheelPosition ==
                Vector3.Zero)
            {
                wheelPosition =
                    vehicle.Position;
            }

            Vector3 forward =
                vehicle.ForwardVector;

            Vector3 right =
                new Vector3(
                    -forward.Y,
                    forward.X,
                    0.0f
                );

            if (right.Length() > 0.001f)
                right.Normalize();

            float side =
                IsRemoveTireLeftWheel(
                    sourceWheel
                )
                    ? -1.0f
                    : 1.0f;

            Vector3 outward =
                right * side;

            for (int attempt = 0;
                 attempt < wheelPropModels.Length;
                 attempt++)
            {
                string modelName =
                    wheelPropModels[
                        (
                            startIndex +
                            attempt
                        ) %
                        wheelPropModels.Length
                    ];

                Model wheelModel =
                    new Model(
                        modelName
                    );

                if (!wheelModel.IsValid)
                    continue;

                wheelModel.Request(
                    350
                );

                if (!wheelModel.IsLoaded)
                {
                    wheelModel.MarkAsNoLongerNeeded();
                    continue;
                }

                // Spawn agak keluar dari body kendaraan supaya prop tidak
                // bertabrakan/menekan kendaraan saat baru dibuat.
                Prop detachedWheel =
                    Prop.Create(
                        wheelModel,
                        wheelPosition +
                        (outward * 0.75f) +
                        (Vector3.WorldUp * 0.20f),
                        true,
                        false
                    );

                wheelModel.MarkAsNoLongerNeeded();

                if (detachedWheel == null ||
                    !detachedWheel.Exists())
                {
                    continue;
                }

                // Buat benar-benar menjadi loose physics object.
                Function.Call(
                    Hash.SET_ENTITY_DYNAMIC,
                    detachedWheel.Handle,
                    true
                );

                Function.Call(
                    Hash.SET_ENTITY_COLLISION,
                    detachedWheel.Handle,
                    true,
                    true
                );

                Function.Call(
                    Hash.SET_ENTITY_HAS_GRAVITY,
                    detachedWheel.Handle,
                    true
                );

                // Berdirikan roda supaya lebih mudah menggelinding, bukan rebah.
                Function.Call(
                    Hash.SET_ENTITY_ROTATION,
                    detachedWheel.Handle,
                    0.0f,
                    90.0f,
                    vehicle.Heading,
                    2,
                    true
                );

                // Warisi momentum kendaraan lalu lempar ke arah luar.
                Vector3 inheritedVelocity =
                    vehicle.Velocity;

                Vector3 launchVelocity =
                    inheritedVelocity +
                    (outward * 4.0f) +
                    (Vector3.WorldUp * 0.85f);

                Function.Call(
                    Hash.SET_ENTITY_VELOCITY,
                    detachedWheel.Handle,
                    launchVelocity.X,
                    launchVelocity.Y,
                    launchVelocity.Z
                );

                // Force + torque kecil supaya roda mental dan mulai berputar.
                Function.Call(
                    Hash.APPLY_FORCE_TO_ENTITY,
                    detachedWheel.Handle,
                    1,
                    outward.X * 3.0f,
                    outward.Y * 3.0f,
                    0.65f,
                    side * 0.10f,
                    side * 2.1f,
                    0.20f,
                    0,
                    false,
                    true,
                    true,
                    false,
                    true
                );

                ScheduleEntityDespawn(
                    detachedWheel,
                    30 * 1000
                );

                return detachedWheel;
            }

            return null;
        }

        private void ActivateRemoveTireWebhook()
        {
            if (!TryGetPlayerVehicle(
                    out Ped player,
                    out Vehicle vehicle))
            {
                return;
            }

            VehicleWheel[] wheels;

            try
            {
                // Hanya izinkan tire burst.
                // Jangan aktifkan wheel-break native karena dapat mengubah
                // suspension/physics kendaraan dan membuat body turun ke tanah.
                vehicle.CanTiresBurst = true;

                wheels =
                    vehicle.Wheels.GetAllWheels();
            }
            catch
            {
                return;
            }

            if (wheels == null ||
                wheels.Length == 0)
            {
                return;
            }

            List<VehicleWheel> intactWheels =
                new List<VehicleWheel>();

            foreach (VehicleWheel wheel in wheels)
            {
                if (wheel == null)
                    continue;

                try
                {
                    if (!wheel.IsBurst)
                    {
                        intactWheels.Add(
                            wheel
                        );
                    }
                }
                catch
                {
                    // Wheel yang tidak valid / tidak punya tire dilewati.
                }
            }

            // Semua ban sudah copot/burst -> biarkan saja.
            if (intactWheels.Count == 0)
                return;

            VehicleWheel selectedWheel =
                intactWheels[
                    _random.Next(
                        intactWheels.Count
                    )
                ];

            // Ambil posisi sebelum Burst(), sehingga prop keluar persis dari
            // area roda yang baru saja terkena gift.
            Vector3 originalWheelPosition =
                GetRemoveTireWheelWorldPosition(
                    vehicle,
                    selectedWheel
                );

            try
            {
                selectedWheel.Burst();
            }
            catch (Exception ex)
            {
                LogError(
                    "REMOVE TIRE BURST ERROR",
                    ex
                );

                return;
            }

            try
            {
                Prop rollingWheel =
                    SpawnDetachedRollingWheel(
                        vehicle,
                        selectedWheel
                    );

                // Bila skeleton kendaraan berubah setelah Burst(), paksa prop
                // kembali dekat titik roda awal agar efek visual tidak muncul
                // dari tengah kendaraan.
                if (rollingWheel != null &&
                    rollingWheel.Exists() &&
                    originalWheelPosition !=
                        Vector3.Zero &&
                    rollingWheel.Position.DistanceTo(
                        originalWheelPosition
                    ) > 3.0f)
                {
                    rollingWheel.Position =
                        originalWheelPosition +
                        (Vector3.WorldUp * 0.10f);
                }

                ShowHudNotification(
                    rollingWheel != null &&
                    rollingWheel.Exists()
                        ? "~r~REMOVE TIRE!~w~ WHEEL DETACHED"
                        : "~r~REMOVE TIRE!"
                );
            }
            catch (Exception ex)
            {
                // Ban asli tetap burst walaupun prop visual gagal dibuat.
                LogError(
                    "REMOVE TIRE ROLLING WHEEL ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~REMOVE TIRE!"
                );
            }
        }

        private void ActivateVehicleFireTimerWebhook()
        {
            LoadGiftTimerConfigFromIni();

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _vehicleFireTimerDurationMs
                );

            _vehicleFireTimerEndTime =
                currentTime +
                activeDurationMs;

            ResetVehicleFireCycle(
                true
            );

            // Sengaja TIDAK menampilkan notification di sini.
            // Warning hanya boleh muncul jika player benar-benar sedang
            // berada di dalam kendaraan. Process akan memulai cycle.
        }

        private void StartVehicleFireCycle(
            Vehicle vehicle,
            int currentTime)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            _vehicleFireTarget =
                vehicle;

            _vehicleFireIgnited =
                false;

            _vehicleFireIgniteTime =
                currentTime +
                VEHICLE_FIRE_WARNING_MS;

            _vehicleFireExplodeTime =
                _vehicleFireIgniteTime +
                VEHICLE_FIRE_TO_EXPLOSION_MS;

            ShowHudNotification(
                "~r~VEHICLE FIRE IN 5 SEC!"
            );
        }

        private void ProcessVehicleFireTimerEffect(
            int currentTime)
        {
            if (_vehicleFireTimerEndTime <= 0)
                return;

            if (currentTime >=
                _vehicleFireTimerEndTime)
            {
                _vehicleFireTimerEndTime =
                    0;

                ResetVehicleFireCycle(
                    true
                );

                return;
            }

            // Jika target cycle lama sudah hilang/destroyed, bersihkan cycle.
            if (_vehicleFireTarget != null &&
                !_vehicleFireTarget.Exists())
            {
                ResetVehicleFireCycle(
                    false
                );
            }

            // Tidak ada cycle aktif: baru mulai kalau player sedang berada
            // di kendaraan. Jalan kaki tidak menampilkan warning apa pun.
            if (_vehicleFireTarget == null)
            {
                if (TryGetPlayerVehicle(
                        out Ped player,
                        out Vehicle currentVehicle))
                {
                    StartVehicleFireCycle(
                        currentVehicle,
                        currentTime
                    );
                }

                return;
            }

            // PENTING: setelah cycle sudah mengunci target, cycle TETAP lanjut
            // walaupun player turun / terpental dari kendaraan. Ini memperbaiki
            // motor yang sebelumnya apinya langsung dihentikan saat rider jatuh.
            if (!_vehicleFireIgnited &&
                currentTime >=
                    _vehicleFireIgniteTime)
            {
                _vehicleFireIgnited =
                    true;

                Function.Call(
                    Hash.START_ENTITY_FIRE,
                    _vehicleFireTarget.Handle
                );

                ShowHudNotification(
                    "~r~VEHICLE ON FIRE!~w~ 5 SEC TO EXPLOSION"
                );
            }

            if (_vehicleFireIgnited &&
                _vehicleFireTarget != null &&
                _vehicleFireTarget.Exists())
            {
                // Paksa entity fire tetap hidup setiap Tick. Penting khususnya
                // untuk motorcycle/bike yang kadang memadamkan entity fire
                // ketika rider terpental atau state kendaraan berubah.
                Function.Call(
                    Hash.START_ENTITY_FIRE,
                    _vehicleFireTarget.Handle
                );
            }

            if (_vehicleFireIgnited &&
                currentTime >=
                    _vehicleFireExplodeTime)
            {
                Vehicle explodedVehicle =
                    _vehicleFireTarget;

                if (explodedVehicle != null &&
                    explodedVehicle.Exists())
                {
                    Function.Call(
                        Hash.EXPLODE_VEHICLE,
                        explodedVehicle.Handle,
                        true,
                        false
                    );

                    ScheduleEntityDespawn(
                        explodedVehicle,
                        5 * 1000
                    );
                }

                // Timer utama tetap hidup. Setelah cycle ini selesai, cycle
                // berikutnya akan dimulai saat player masuk kendaraan lagi.
                ResetVehicleFireCycle(
                    false
                );
            }
        }

        private void ResetVehicleFireCycle(
            bool stopExistingFire)
        {
            if (stopExistingFire &&
                _vehicleFireTarget != null &&
                _vehicleFireTarget.Exists())
            {
                Function.Call(
                    Hash.STOP_ENTITY_FIRE,
                    _vehicleFireTarget.Handle
                );
            }

            _vehicleFireTarget =
                null;

            _vehicleFireIgniteTime =
                0;

            _vehicleFireExplodeTime =
                0;

            _vehicleFireIgnited =
                false;
        }

        private void ResetVehicleFireTimerEffect()
        {
            _vehicleFireTimerEndTime =
                0;

            ResetVehicleFireCycle(
                true
            );
        }

        private void ActivateBurningWebhook()
        {
            LoadGiftTimerConfigFromIni();

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _burningDurationMs
                );

            // Jika Burning sudah menunggu / sedang membakar, gift baru hanya
            // menambah total durasi api. Countdown pertama tidak diulang.
            if (_burningEndTime >
                currentTime)
            {
                _burningEndTime +=
                    activeDurationMs;

                ShowHudNotification(
                    "~r~BURNING EXTENDED! ~w~+" +
                    (activeDurationMs / 1000) +
                    " SEC"
                );

                return;
            }

            _burningIgnited =
                false;

            _burningIgniteTime =
                currentTime +
                _burningCountdownDurationMs;

            _burningEndTime =
                _burningIgniteTime +
                activeDurationMs;

            _nextBurningFireEnsureTime =
                0;

            Function.Call(
                Hash.STOP_ENTITY_FIRE,
                player.Handle
            );

            ShowHudNotification(
                "~r~BURNING IN " +
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        _burningCountdownDurationMs /
                        1000.0
                    )
                ) +
                " SEC!"
            );
        }

        private void ProcessBurningEffect(
            int currentTime)
        {
            if (_burningEndTime <= 0)
                return;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead ||
                currentTime >=
                    _burningEndTime)
            {
                ResetBurningEffect();
                return;
            }

            if (!_burningIgnited &&
                currentTime >=
                    _burningIgniteTime)
            {
                _burningIgnited =
                    true;

                // Nyalakan native entity fire SEKALI agar particle/flame GTA
                // sempat hidup normal pada ped. Memanggil ini setiap Tick
                // dapat terus me-reset FX dan menyisakan glow merah saja.
                Function.Call(
                    Hash.START_ENTITY_FIRE,
                    player.Handle
                );

                _nextBurningFireEnsureTime =
                    currentTime +
                    BURNING_FIRE_ENSURE_INTERVAL_MS;

                ShowHudNotification(
                    "~r~YOU ARE BURNING!"
                );
            }

            if (_burningIgnited &&
                currentTime >=
                    _nextBurningFireEnsureTime)
            {
                bool isActuallyOnFire =
                    Function.Call<bool>(
                        Hash.IS_ENTITY_ON_FIRE,
                        player.Handle
                    );

                // Jika native GTA memadamkan api sebelum timer script habis,
                // hidupkan lagi. Jangan restart api ketika masih menyala.
                if (!isActuallyOnFire)
                {
                    Function.Call(
                        Hash.START_ENTITY_FIRE,
                        player.Handle
                    );
                }

                _nextBurningFireEnsureTime =
                    currentTime +
                    BURNING_FIRE_ENSURE_INTERVAL_MS;
            }
        }

        private void ResetBurningEffect()
        {
            Ped player =
                Game.Player.Character;

            if (player != null &&
                player.Exists())
            {
                Function.Call(
                    Hash.STOP_ENTITY_FIRE,
                    player.Handle
                );
            }

            _burningIgniteTime =
                0;

            _burningEndTime =
                0;

            _burningIgnited =
                false;

            _nextBurningFireEnsureTime =
                0;
        }

        private void ActivatePoliceRoadblockWebhook()
        {
            LoadGiftTimerConfigFromIni();

            int activeDurationMs =
                GetModeEffectDuration(
                    _policeRoadblockDurationMs
                );

            _policeRoadblockEndTime =
                Game.GameTime +
                activeDurationMs;

            // Spawn batch pertama langsung.
            _nextPoliceRoadblockSpawnTime =
                Game.GameTime;

            ShowHudNotification(
                "~b~POLICE ROADBLOCK!~w~ " +
                (activeDurationMs / 1000) +
                " SEC"
            );
        }

        private void CleanupPoliceRoadblockVehicles()
        {
            for (int i =
                    _policeRoadblockVehicles.Count - 1;
                 i >= 0;
                 i--)
            {
                Vehicle vehicle =
                    _policeRoadblockVehicles[i];

                if (vehicle != null &&
                    vehicle.Exists())
                {
                    vehicle.Delete();
                }
            }

            _policeRoadblockVehicles.Clear();
        }

        private void PrunePoliceRoadblockVehicles()
        {
            for (int i =
                    _policeRoadblockVehicles.Count - 1;
                 i >= 0;
                 i--)
            {
                Vehicle vehicle =
                    _policeRoadblockVehicles[i];

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    _policeRoadblockVehicles.RemoveAt(i);
                }
            }
        }

        private void SpawnPoliceRoadblockBatch()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            Vector3 forward =
                GetPlayerTravelForward(
                    player
                );

            Vector3 right =
                new Vector3(
                    -forward.Y,
                    forward.X,
                    0.0f
                );

            float baseHeading =
                player.IsInVehicle() &&
                player.CurrentVehicle != null &&
                player.CurrentVehicle.Exists()
                    ? player.CurrentVehicle.Heading
                    : player.Heading;

            PrunePoliceRoadblockVehicles();

            Model model =
                new Model(
                    "riot"
                );

            if (!model.IsValid)
                return;

            model.Request(
                1000
            );

            if (!model.IsLoaded)
            {
                model.MarkAsNoLongerNeeded();
                return;
            }

            // Satu garis roadblock berisi TOTAL 5 RIOT.
            // Posisi dibuat simetris terhadap pusat jalan:
            // -2, -1, 0, +1, +2 * SIDE_SPACING.
            Vector3 rowCenter =
                player.Position +
                (forward * POLICE_ROADBLOCK_AHEAD_DISTANCE);

            for (int index = 0;
                 index < POLICE_ROADBLOCK_VEHICLE_COUNT;
                 index++)
            {
                float sideOffset =
                    (index -
                     ((POLICE_ROADBLOCK_VEHICLE_COUNT - 1) / 2.0f)) *
                    POLICE_ROADBLOCK_SIDE_SPACING;

                Vector3 spawnPos =
                    rowCenter +
                    (right * sideOffset);

                spawnPos =
                    SnapEffectPositionToGround(
                        spawnPos,
                        0.6f
                    );

                Vehicle policeVehicle =
                    Vehicle.Create(
                        model,
                        spawnPos,
                        (
                            baseHeading +
                            90.0f
                        ) %
                        360.0f
                    );

                if (policeVehicle == null ||
                    !policeVehicle.Exists())
                {
                    continue;
                }

                policeVehicle.PlaceOnGround();

                policeVehicle.IsPositionFrozen =
                    true;

                Function.Call(
                    Hash.SET_VEHICLE_SIREN,
                    policeVehicle.Handle,
                    true
                );

                _policeRoadblockVehicles.Add(
                    policeVehicle
                );

                ScheduleEntityDespawn(
                    policeVehicle,
                    POLICE_ROADBLOCK_BATCH_DESPAWN_MS
                );
            }

            model.MarkAsNoLongerNeeded();
        }

        private void ProcessPoliceRoadblockEffect(
            int currentTime)
        {
            if (_policeRoadblockEndTime <= 0)
                return;

            if (currentTime >=
                _policeRoadblockEndTime)
            {
                _policeRoadblockEndTime =
                    0;

                _nextPoliceRoadblockSpawnTime =
                    0;

                CleanupPoliceRoadblockVehicles();

                return;
            }

            if (currentTime <
                _nextPoliceRoadblockSpawnTime)
            {
                return;
            }

            SpawnPoliceRoadblockBatch();

            _nextPoliceRoadblockSpawnTime =
                currentTime +
                POLICE_ROADBLOCK_SPAWN_INTERVAL_MS;
        }

        private void ActivateRandomExplosionNearbyWebhook()
        {
            LoadGiftTimerConfigFromIni();

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _randomExplosionDurationMs
                );

            _randomExplosionEndTime =
                currentTime +
                activeDurationMs;

            _randomExplosionSpawnedCount =
                0;

            _randomExplosionIntervalMs =
                Math.Max(
                    1,
                    activeDurationMs /
                        Math.Max(
                            1,
                            _randomExplosionTotal
                        )
                );

            _nextRandomExplosionTime =
                currentTime;

            ShowHudNotification(
                "~r~RANDOM EXPLOSIONS NEARBY!~w~ " +
                (activeDurationMs / 1000) +
                " SEC | " +
                _randomExplosionTotal +
                " EXPLOSIONS"
            );
        }

        private void SpawnOneRandomExplosionNearby(
            Ped player)
        {
            double angle =
                _random.NextDouble() *
                Math.PI *
                2.0;

            // Uniform random di SELURUH luas lingkaran 0-28 meter.
            // Tidak ada minimum radius, jadi ledakan boleh muncul sangat
            // dekat / tepat di sekitar player dan dapat mengenai player.
            float radius =
                (float)Math.Sqrt(
                    _random.NextDouble()
                ) *
                28.0f;

            Vector3 explosionPos =
                player.Position +
                new Vector3(
                    (float)Math.Cos(
                        angle
                    ) *
                    radius,
                    (float)Math.Sin(
                        angle
                    ) *
                    radius,
                    0.0f
                );

            explosionPos =
                SnapEffectPositionToGround(
                    explosionPos,
                    0.15f
                );

            Function.Call(
                Hash.ADD_EXPLOSION,
                explosionPos.X,
                explosionPos.Y,
                explosionPos.Z,
                0,
                1.0f,
                true,
                false,
                0.35f,
                false
            );
        }

        private void ProcessRandomExplosionNearbyEffect(
            int currentTime)
        {
            if (_randomExplosionEndTime <= 0)
                return;

            if (_randomExplosionSpawnedCount >=
                _randomExplosionTotal)
            {
                _randomExplosionEndTime =
                    0;

                _nextRandomExplosionTime =
                    0;

                _randomExplosionSpawnedCount =
                    0;

                return;
            }

            if (currentTime >=
                _randomExplosionEndTime)
            {
                _randomExplosionEndTime =
                    0;

                _nextRandomExplosionTime =
                    0;

                _randomExplosionSpawnedCount =
                    0;

                return;
            }

            if (currentTime <
                _nextRandomExplosionTime)
            {
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            // Catch-up ringan jika frame sempat terlambat.
            // Default 60 detik + 300 total = 1 ledakan tiap 200 ms,
            // sama dengan perilaku lama 5 ledakan per detik.
            int safetyBurstLimit =
                25;

            int spawnedThisFrame =
                0;

            while (
                _randomExplosionSpawnedCount <
                    _randomExplosionTotal &&
                currentTime >=
                    _nextRandomExplosionTime &&
                spawnedThisFrame <
                    safetyBurstLimit
            )
            {
                SpawnOneRandomExplosionNearby(
                    player
                );

                _randomExplosionSpawnedCount++;
                spawnedThisFrame++;

                _nextRandomExplosionTime +=
                    _randomExplosionIntervalMs;
            }
        }

        private void ProcessStandaloneWebhookEffects(
            int currentTime)
        {
            ProcessGetTowedEffect(
                currentTime
            );

            ProcessTrafficMagnetEffect(
                currentTime
            );

            ProcessRecklessTrafficEffect(
                currentTime
            );

            ProcessVehicleFireTimerEffect(
                currentTime
            );

            ProcessBurningEffect(
                currentTime
            );

            ProcessPoliceRoadblockEffect(
                currentTime
            );

            ProcessRandomExplosionNearbyEffect(
                currentTime
            );
        }

        private void CleanupStandaloneWebhookEffects()
        {
            CleanupGetTowedEffect();

            _trafficMagnetEndTime =
                0;

            RestoreRecklessTrafficVehicles();

            _recklessTrafficEndTime =
                0;

            _nextRecklessTrafficUpdateTime =
                0;

            _nextRecklessTrafficNitroTime =
                0;

            ResetVehicleFireTimerEffect();

            ResetBurningEffect();

            _policeRoadblockEndTime =
                0;

            _nextPoliceRoadblockSpawnTime =
                0;

            CleanupPoliceRoadblockVehicles();

            _randomExplosionEndTime =
                0;

            _nextRandomExplosionTime =
                0;
        }

        private void ActivatePlayerFlip()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            if (player.IsInVehicle())
            {
                Vehicle veh = player.CurrentVehicle;
                if (veh != null && veh.Exists())
                {
                    veh.ApplyForce(Vector3.WorldUp * 2.5f);
                    veh.ApplyForceRelative(new Vector3(0, 0, 1.5f), new Vector3(25.0f, 0, 0), ForceType.InternalImpulse);
                }
            }
            else
            {
                player.CanRagdoll = true;

                Function.Call(Hash.SET_PED_TO_RAGDOLL, player, 1000, 1000, 2, true, true, false);

                player.ApplyForce(Vector3.WorldUp * 1.8f);
                player.ApplyForceRelative(new Vector3(0, -2.0f, 2.5f), new Vector3(0, 0, 0.8f), ForceType.InternalImpulse);
            }
        }

        private void ActivateSuperSpeed()
        {
            LoadGiftTimerConfigFromIni();

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftSuperSpeedDurationMs
                );

            if (_modeDurationOverrideMs > 0)
            {
                _superSpeedEndTime =
                    currentTime +
                    activeDurationMs;
            }
            else
            {
                _superSpeedEndTime =
                    currentTime < _superSpeedEndTime
                        ? _superSpeedEndTime +
                          activeDurationMs
                        : currentTime +
                          activeDurationMs;
            }

            // Vehicle-specific behavior:
            // give one immediate forward push when SUPERSPEED is activated.
            // No accelerator input is required for this launch. After the push,
            // normal GTA physics lets the vehicle keep coasting; holding gas still
            // enables the existing per-frame Super Speed acceleration below.
            ApplySuperSpeedInitialVehicleLaunch();
        }

        private void ApplySuperSpeedInitialVehicleLaunch()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead ||
                !player.IsInVehicle())
            {
                return;
            }

            Vehicle vehicle =
                player.CurrentVehicle;

            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            Ped driver =
                vehicle.Driver;

            if (driver == null ||
                !driver.Exists() ||
                driver.Handle != player.Handle)
            {
                return;
            }

            int vehicleClass =
                Function.Call<int>(
                    Hash.GET_VEHICLE_CLASS,
                    vehicle.Handle
                );

            // 14 boats, 15 helicopters, 16 planes, 21 trains.
            // Keep the initial launch ground-safe, same as the sustained logic.
            bool supportsGroundSuperSpeed =
                vehicleClass != 14 &&
                vehicleClass != 15 &&
                vehicleClass != 16 &&
                vehicleClass != 21;

            if (!supportsGroundSuperSpeed)
                return;

            bool isInAir =
                Function.Call<bool>(
                    Hash.IS_ENTITY_IN_AIR,
                    vehicle.Handle
                );

            bool isOnAllWheels =
                Function.Call<bool>(
                    Hash.IS_VEHICLE_ON_ALL_WHEELS,
                    vehicle.Handle
                );

            if (isInAir ||
                !isOnAllWheels)
            {
                return;
            }

            Vector3 velocity =
                Function.Call<Vector3>(
                    Hash.GET_ENTITY_VELOCITY,
                    vehicle.Handle
                );

            float horizontalSpeed =
                (float)Math.Sqrt(
                    (velocity.X * velocity.X) +
                    (velocity.Y * velocity.Y)
                );

            float maxSuperSpeed =
                vehicleClass == 13
                    ? 28.0f
                    : 60.0f;

            float initialBoost =
                vehicleClass == 13
                    ? _superSpeedBicycleInitialBoost
                    : _superSpeedVehicleInitialBoost;

            if (initialBoost <= 0.0f)
                return;

            float launchedSpeed =
                Math.Min(
                    maxSuperSpeed,
                    horizontalSpeed + initialBoost
                );

            Vector3 forward =
                vehicle.ForwardVector;

            float horizontalForwardLength =
                (float)Math.Sqrt(
                    (forward.X * forward.X) +
                    (forward.Y * forward.Y)
                );

            if (horizontalForwardLength <= 0.001f)
                return;

            float dirX =
                forward.X / horizontalForwardLength;

            float dirY =
                forward.Y / horizontalForwardLength;

            Function.Call(
                Hash.SET_ENTITY_VELOCITY,
                vehicle.Handle,
                dirX * launchedSpeed,
                dirY * launchedSpeed,
                velocity.Z
            );
        }

        private void ApplySuperSpeedLogic()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            // =========================================================
            // IF: PLAYER SEDANG MENGENDARAI VEHICLE
            //
            // Semua kendaraan DARAT boleh mendapat Super Speed.
            // Bicycle tetap memakai batas lebih rendah.
            // =========================================================
            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    return;
                }

                Ped driver =
                    vehicle.Driver;

                if (driver == null ||
                    !driver.Exists() ||
                    driver.Handle !=
                        player.Handle)
                {
                    return;
                }

                bool isAccelerating =
                    IsControlPressedCompat(
                        GTA.Control.VehicleAccelerate
                    );

                if (!isAccelerating)
                    return;

                int vehicleClass =
                    Function.Call<int>(
                        Hash.GET_VEHICLE_CLASS,
                        vehicle.Handle
                    );

                // GTA vehicle class:
                // 14 = Boats
                // 15 = Helicopters
                // 16 = Planes
                // 21 = Trains
                //
                // Driving tetap BEBAS untuk semuanya.
                // Hanya physics Super Speed versi ground-safe ini
                // tidak dipaksakan ke vehicle non-darat.
                bool supportsGroundSuperSpeed =
                    vehicleClass != 14 &&
                    vehicleClass != 15 &&
                    vehicleClass != 16 &&
                    vehicleClass != 21;

                if (!supportsGroundSuperSpeed)
                    return;

                // Jangan boost saat kendaraan sedang airborne.
                bool isInAir =
                    Function.Call<bool>(
                        Hash.IS_ENTITY_IN_AIR,
                        vehicle.Handle
                    );

                bool isOnAllWheels =
                    Function.Call<bool>(
                        Hash.IS_VEHICLE_ON_ALL_WHEELS,
                        vehicle.Handle
                    );

                if (isInAir ||
                    !isOnAllWheels)
                {
                    return;
                }

                Vector3 velocity =
                    Function.Call<Vector3>(
                        Hash.GET_ENTITY_VELOCITY,
                        vehicle.Handle
                    );

                float horizontalSpeed =
                    (float)Math.Sqrt(
                        (velocity.X * velocity.X) +
                        (velocity.Y * velocity.Y)
                    );

                // Bicycle = 28 m/s (~101 km/h)
                // Semua ground vehicle lain = 60 m/s (~216 km/h)
                float maxSuperSpeed =
                    vehicleClass == 13
                        ? 28.0f
                        : 60.0f;

                float accelerationPerFrame =
                    vehicleClass == 13
                        ? 0.28f
                        : 0.45f;

                if (horizontalSpeed <
                    maxSuperSpeed)
                {
                    float boostedSpeed =
                        Math.Min(
                            maxSuperSpeed,
                            horizontalSpeed +
                            accelerationPerFrame
                        );

                    Vector3 forward =
                        vehicle.ForwardVector;

                    // Boost hanya X/Y.
                    // Z tetap mengikuti physics GTA supaya tidak terbang
                    // saat menanjak atau melewati ramp.
                    float horizontalForwardLength =
                        (float)Math.Sqrt(
                            (forward.X * forward.X) +
                            (forward.Y * forward.Y)
                        );

                    if (horizontalForwardLength >
                        0.001f)
                    {
                        float dirX =
                            forward.X /
                            horizontalForwardLength;

                        float dirY =
                            forward.Y /
                            horizontalForwardLength;

                        Function.Call(
                            Hash.SET_ENTITY_VELOCITY,
                            vehicle.Handle,
                            dirX * boostedSpeed,
                            dirY * boostedSpeed,
                            velocity.Z
                        );
                    }
                }

                return;
            }

            // =========================================================
            // ELSE: PLAYER JALAN KAKI
            // =========================================================
            Game.Player.SetRunSpeedMultThisFrame(
                1.49f
            );

            Function.Call(
                Hash.SET_PED_MOVE_RATE_OVERRIDE,
                player.Handle,
                3.0f
            );
        }

        private void ResetSuperSpeed()
        {
            _superSpeedEndTime = 0;

            Ped player =
                Game.Player.Character;

            if (player != null &&
                player.Exists())
            {
                // Reset hanya movement player.
                // Kendaraan tidak menyimpan multiplier permanen;
                // setelah timer habis kendaraan kembali memakai
                // physics/akselerasi normal GTA.
                Function.Call(
                    Hash.SET_PED_MOVE_RATE_OVERRIDE,
                    player.Handle,
                    1.0f
                );
            }

            Game.Player.SetRunSpeedMultThisFrame(
                1.0f
            );
        }

        private void ActivateInvincible()
        {
            LoadGiftTimerConfigFromIni();

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftInvincibleDurationMs
                );

            if (_modeDurationOverrideMs > 0)
            {
                _invincibleEndTime =
                    currentTime +
                    activeDurationMs;
            }
            else
            {
                _invincibleEndTime =
                    currentTime < _invincibleEndTime
                        ? _invincibleEndTime +
                          activeDurationMs
                        : currentTime +
                          activeDurationMs;
            }
        }

        private void ApplyInvincibleLogic()
        {
            Ped player =
                Game.Player.Character;

            if (player != null &&
                player.Exists())
            {
                player.IsInvincible =
                    true;
            }
        }

        private void ResetInvincible()
        {
            _invincibleEndTime =
                0;

            Ped player =
                Game.Player.Character;

            if (player != null &&
                player.Exists())
            {
                player.IsInvincible =
                    false;
            }
        }

        private void ApplyPlayerWeaponWhitelist()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            // =========================================================
            // PLAYER SEDANG MENJADI ANIMAL
            // Jangan pernah berikan weapon ke animal ped.
            // Weapon manusia akan direstore setelah model asli kembali.
            // =========================================================
            if (_playerAnimalTransformPhase == 2 ||
                _playerAnimalTransformPhase == 3)
            {
                return;
            }

            // =========================================================
            // RANDOM CHAOS DISARM
            // =========================================================
            if (IsActiveRandomChaosEffect(
                    "disarm"
                ))
            {
                ApplyChaosNoWeapon(
                    player
                );

                return;
            }

            // =========================================================
            // GIFT DISARM
            // =========================================================
            if (Game.GameTime <
                _giftDisarmEndTime)
            {
                ApplyChaosNoWeapon(
                    player
                );

                return;
            }

            // =========================================================
            // HAPUS SEMUA WEAPON YANG TIDAK ADA DI WHITELIST
            // =========================================================
            foreach (
                WeaponHash weaponHash
                in Enum.GetValues(typeof(WeaponHash))
            )
            {
                if (weaponHash == WeaponHash.Unarmed)
                    continue;

                if (!IsAllowedPlayerWeapon(weaponHash) &&
                    player.Weapons.HasWeapon(weaponHash))
                {
                    Function.Call(
                        Hash.REMOVE_WEAPON_FROM_PED,
                        player.Handle,
                        unchecked((uint)(int)weaponHash)
                    );
                }
            }

            // =========================================================
            // PROTECTION UNTUK CUSTOM/MODDED WEAPON
            // =========================================================
            if (player.Weapons.Current != null)
            {
                WeaponHash currentHash =
                    player.Weapons.Current.Hash;

                if (!IsAllowedPlayerWeapon(currentHash))
                {
                    Function.Call(
                        Hash.REMOVE_WEAPON_FROM_PED,
                        player.Handle,
                        unchecked((uint)(int)currentHash)
                    );
                }
            }

            // Pastikan semua weapon dari [PLAYER_WEAPONS] tersedia.
            GiveDefaultPlayerWeapons();
        }

        private void EnsureBlackholePropLoading(
            Ped player)
        {
            if (player == null ||
                !player.Exists())
            {
                return;
            }

            // Kalau prop belum ada DAN belum sedang loading,
            // mulai request model.
            if ((_blackHoleProp == null ||
                 !_blackHoleProp.Exists()) &&
                _blackHoleToSpawn == null)
            {
                _blackHoleCenter =
                    player.Position +
                    new Vector3(
                        0,
                        0,
                        150.0f
                    );

                Model sphereModel =
                    new Model(
                        "prop_asteroids_01"
                    );

                // NON-BLOCKING
                sphereModel.Request();

                _blackHoleToSpawn =
                    new BlackHoleLoadingTracker
                    {
                        Model =
                            sphereModel
                    };
            }
        }

        private bool StartBlackholeForDuration(
            int durationMs)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return false;
            }

            int safeDurationMs =
                Math.Max(
                    1,
                    durationMs
                );

            EnsureBlackholePropLoading(
                player
            );

            _blackholeEndTime =
                Game.GameTime +
                safeDurationMs;

            ShowHudNotification(
                "~p~BLACK HOLE ACTIVATED! ~w~" +
                (int)Math.Ceiling(
                    safeDurationMs /
                    1000.0
                ) +
                " SEC"
            );

            return true;
        }

        private void ActivateBlackhole()
        {
            LoadGiftTimerConfigFromIni();

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            int requestedDurationMs =
                _modeDurationOverrideMs > 0
                    ? _modeDurationOverrideMs
                    : _giftBlackholeDurationMs;

            requestedDurationMs =
                Math.Max(
                    1,
                    requestedDurationMs
                );

            // EARTHQUAKE sedang aktif -> setiap Blackhole gift masuk queue.
            // Semua gift memakai durasi yang sama; tidak ada Initial/Extend.
            if (currentTime <
                _earthquakeEndTime)
            {
                _pendingBlackholeDurationMs +=
                    requestedDurationMs;

                int waitSeconds =
                    Math.Max(
                        0,
                        (int)Math.Ceiling(
                            (_earthquakeEndTime -
                             currentTime) /
                            1000.0
                        )
                    );

                ShowHudNotification(
                    "~p~BLACK HOLE QUEUED! ~w~Starts after EARTHQUAKE (" +
                    waitSeconds +
                    "s)"
                );

                return;
            }

            // Jika Blackhole masih aktif, gift berikutnya menambah TOTAL timer
            // memakai nilai BlackholeSeconds yang sama.
            if (currentTime <
                _blackholeEndTime)
            {
                EnsureBlackholePropLoading(
                    player
                );

                _blackholeEndTime +=
                    requestedDurationMs;

                ShowHudNotification(
                    "~p~BLACK HOLE EXTENDED! ~w~+" +
                    (int)Math.Ceiling(
                        requestedDurationMs /
                        1000.0
                    ) +
                    " SEC"
                );

                return;
            }

            StartBlackholeForDuration(
                requestedDurationMs
            );
        }

        private void ProcessBlackHoleSpawn()
        {
            if (_blackHoleToSpawn == null)
                return;

            // =========================================================
            // BLACK HOLE SUDAH HABIS SEBELUM MODEL SIAP
            // =========================================================
            if (Game.GameTime >= _blackholeEndTime)
            {
                _blackHoleToSpawn.Model.MarkAsNoLongerNeeded();

                _blackHoleToSpawn = null;
                return;
            }

            // =========================================================
            // MODEL BLACK HOLE TIDAK VALID
            // =========================================================
            if (!_blackHoleToSpawn.Model.IsValid)
            {
                ShowHudNotification(
                    "~r~[BLACK HOLE ERROR]~w~ Model Black Hole tidak valid!"
                );

                _blackHoleToSpawn = null;
                return;
            }

            // =========================================================
            // MODEL BELUM SIAP
            // =========================================================
            if (!_blackHoleToSpawn.Model.IsLoaded)
            {
                _blackHoleToSpawn.Model.Request();
                return;
            }

            // =========================================================
            // MODEL SUDAH SIAP
            // =========================================================
            Model sphereModel =
                _blackHoleToSpawn.Model;

            _blackHoleProp = Prop.Create(
                sphereModel,
                _blackHoleCenter,
                false,
                false
            );

            if (_blackHoleProp != null &&
                _blackHoleProp.Exists())
            {
                _blackHoleProp.IsPositionFrozen = true;
            }
            else
            {
                ShowHudNotification(
                    "~r~[BLACK HOLE ERROR]~w~ Gagal membuat prop Black Hole!"
                );
            }

            // =========================================================
            // SELESAI
            // =========================================================
            sphereModel.MarkAsNoLongerNeeded();

            _blackHoleToSpawn = null;
        }

        private void RestoreBlackholeAntiStuckCollision()
        {
            if (_blackholeAntiStuckPhasedHandle != 0)
            {
                bool entityStillExists =
                    Function.Call<bool>(
                        Hash.DOES_ENTITY_EXIST,
                        _blackholeAntiStuckPhasedHandle
                    );

                if (entityStillExists)
                {
                    Function.Call(
                        Hash.SET_ENTITY_COLLISION,
                        _blackholeAntiStuckPhasedHandle,
                        true,
                        true
                    );
                }
            }

            _blackholeAntiStuckPhasedHandle =
                0;

            _blackholeAntiStuckPhaseEndTime =
                0;
        }

        private void ResetBlackholeAntiStuckState(
            bool restoreCollision = true)
        {
            if (restoreCollision)
            {
                RestoreBlackholeAntiStuckCollision();
            }

            _blackholeAntiStuckLastPosition =
                Vector3.Zero;

            _blackholeAntiStuckTrackedHandle =
                0;

            _blackholeAntiStuckLastSampleTime =
                0;

            _blackholeAntiStuckNoProgressMs =
                0;

            _blackholeAntiStuckCooldownEndTime =
                0;
        }

        private Entity GetBlackholeAntiStuckTarget(
            Ped player)
        {
            if (player == null ||
                !player.Exists())
            {
                return null;
            }

            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle != null &&
                    vehicle.Exists())
                {
                    return vehicle;
                }
            }

            return player;
        }

        private void BeginBlackholeAntiStuckPhase(
            Entity target,
            int currentTime)
        {
            if (target == null ||
                !target.Exists())
            {
                return;
            }

            _blackholeAntiStuckPhasedHandle =
                target.Handle;

            _blackholeAntiStuckPhaseEndTime =
                currentTime +
                BLACKHOLE_ESCAPE_PHASE_MS;

            _blackholeAntiStuckCooldownEndTime =
                _blackholeAntiStuckPhaseEndTime +
                BLACKHOLE_ESCAPE_COOLDOWN_MS;

            _blackholeAntiStuckNoProgressMs =
                0;

            // Collision map dimatikan SANGAT singkat agar player/vehicle
            // bisa lolos dari plafon / bangunan yang menahan tarikan.
            Function.Call(
                Hash.SET_ENTITY_COLLISION,
                target.Handle,
                false,
                false
            );
        }

        private void ApplyBlackholeAntiStuckEscapeVelocity(
            Entity target)
        {
            if (target == null ||
                !target.Exists())
            {
                return;
            }

            Vector3 direction =
                _blackHoleCenter -
                target.Position;

            float length =
                direction.Length();

            if (length <= 0.001f)
                return;

            direction.Normalize();

            Vector3 escapeVelocity =
                direction *
                BLACKHOLE_ESCAPE_SPEED;

            // Bangunan biasanya menahan dari atas.
            // Pastikan fase escape selalu punya komponen vertikal
            // yang cukup untuk benar-benar menembus obstruction.
            if (escapeVelocity.Z <
                BLACKHOLE_ESCAPE_MIN_VERTICAL_SPEED)
            {
                escapeVelocity.Z =
                    BLACKHOLE_ESCAPE_MIN_VERTICAL_SPEED;
            }

            Function.Call(
                Hash.SET_ENTITY_VELOCITY,
                target.Handle,
                escapeVelocity.X,
                escapeVelocity.Y,
                escapeVelocity.Z
            );
        }

        private void ProcessBlackholeAntiStuck(
            Ped player,
            int currentTime)
        {
            if (player == null ||
                !player.Exists() ||
                player.IsDead ||
                _customDeathActive ||
                currentTime <
                    _customDeathGroundProtectionEndTime ||
                currentTime >=
                    _blackholeEndTime)
            {
                ResetBlackholeAntiStuckState();
                return;
            }

            Entity target =
                GetBlackholeAntiStuckTarget(
                    player
                );

            if (target == null ||
                !target.Exists())
            {
                ResetBlackholeAntiStuckState();
                return;
            }

            // =====================================================
            // PHASE ESCAPE SEDANG BERJALAN
            // =====================================================
            if (_blackholeAntiStuckPhaseEndTime > 0)
            {
                if (target.Handle !=
                    _blackholeAntiStuckPhasedHandle)
                {
                    // Player pindah masuk/keluar kendaraan saat phase.
                    // Pastikan entity lama collision-nya dikembalikan.
                    RestoreBlackholeAntiStuckCollision();

                    _blackholeAntiStuckTrackedHandle =
                        target.Handle;

                    _blackholeAntiStuckLastPosition =
                        target.Position;

                    _blackholeAntiStuckLastSampleTime =
                        currentTime;

                    _blackholeAntiStuckNoProgressMs =
                        0;

                    return;
                }

                if (currentTime <
                    _blackholeAntiStuckPhaseEndTime)
                {
                    ApplyBlackholeAntiStuckEscapeVelocity(
                        target
                    );

                    return;
                }

                // Fase selesai -> collision WAJIB ON lagi.
                RestoreBlackholeAntiStuckCollision();

                _blackholeAntiStuckTrackedHandle =
                    target.Handle;

                _blackholeAntiStuckLastPosition =
                    target.Position;

                _blackholeAntiStuckLastSampleTime =
                    currentTime;

                _blackholeAntiStuckNoProgressMs =
                    0;

                return;
            }

            // =====================================================
            // TARGET BERUBAH: on-foot <-> vehicle
            // =====================================================
            if (_blackholeAntiStuckTrackedHandle !=
                target.Handle)
            {
                _blackholeAntiStuckTrackedHandle =
                    target.Handle;

                _blackholeAntiStuckLastPosition =
                    target.Position;

                _blackholeAntiStuckLastSampleTime =
                    currentTime;

                _blackholeAntiStuckNoProgressMs =
                    0;

                return;
            }

            if (currentTime <
                _blackholeAntiStuckCooldownEndTime)
            {
                return;
            }

            if (_blackholeAntiStuckLastSampleTime <= 0)
            {
                _blackholeAntiStuckLastPosition =
                    target.Position;

                _blackholeAntiStuckLastSampleTime =
                    currentTime;

                return;
            }

            int elapsed =
                currentTime -
                _blackholeAntiStuckLastSampleTime;

            if (elapsed <
                BLACKHOLE_STUCK_SAMPLE_INTERVAL_MS)
            {
                return;
            }

            Vector3 currentPosition =
                target.Position;

            float zProgress =
                currentPosition.Z -
                _blackholeAntiStuckLastPosition.Z;

            float distanceToBlackhole =
                currentPosition.DistanceTo(
                    _blackHoleCenter
                );

            bool shouldStillBePulledUp =
                distanceToBlackhole >
                BLACKHOLE_ESCAPE_MIN_DISTANCE_TO_CENTER;

            // Progress vertikal terlalu kecil = kemungkinan tertahan
            // atap / underside bangunan / map collision.
            if (shouldStillBePulledUp &&
                zProgress <
                    BLACKHOLE_STUCK_MIN_Z_PROGRESS)
            {
                _blackholeAntiStuckNoProgressMs +=
                    elapsed;
            }
            else
            {
                _blackholeAntiStuckNoProgressMs =
                    0;
            }

            _blackholeAntiStuckLastPosition =
                currentPosition;

            _blackholeAntiStuckLastSampleTime =
                currentTime;

            if (_blackholeAntiStuckNoProgressMs >=
                BLACKHOLE_STUCK_REQUIRED_MS)
            {
                BeginBlackholeAntiStuckPhase(
                    target,
                    currentTime
                );
            }
        }

        private void ApplyTrueBlackholeLogic()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            // Player / player vehicle punya anti-stuck khusus.
            // Entity lain tetap mengikuti physics Blackhole normal.
            int currentTime =
                Game.GameTime;

            ProcessBlackholeAntiStuck(
                player,
                currentTime
            );

            if (currentTime <
                _nextBlackholeEffectUpdateTime)
            {
                return;
            }

            _nextBlackholeEffectUpdateTime =
                currentTime +
                BLACKHOLE_EFFECT_UPDATE_INTERVAL_MS;

            foreach (Vehicle veh in World.GetNearbyVehicles(player.Position, 150.0f))
            {
                if (veh != null && veh.Exists())
                {
                    // Saat player vehicle sedang phase escape,
                    // velocity khusus anti-stuck yang mengontrol geraknya.
                    if (_blackholeAntiStuckPhaseEndTime > 0 &&
                        veh.Handle ==
                            _blackholeAntiStuckPhasedHandle)
                    {
                        continue;
                    }

                    Vector3 dir = (_blackHoleCenter - veh.Position);
                    dir.Normalize();
                    veh.ApplyForce(dir * 12.0f);
                }
            }

            foreach (Ped ped in World.GetNearbyPeds(player.Position, 150.0f))
            {
                if (ped != null && ped.Exists())
                {
                    // Saat custom death / sesaat setelah respawn,
                    // jangan biarkan BLACKHOLE mengangkat player lagi.
                    if (ped.Handle ==
                            player.Handle &&
                        (
                            _customDeathActive ||
                            Game.GameTime <
                                _customDeathGroundProtectionEndTime
                        ))
                    {
                        continue;
                    }

                    // Saat player on-foot sedang phase escape,
                    // jangan timpa velocity escape dengan force normal.
                    if (_blackholeAntiStuckPhaseEndTime > 0 &&
                        ped.Handle ==
                            _blackholeAntiStuckPhasedHandle)
                    {
                        continue;
                    }

                    ped.CanRagdoll = true;
                    Function.Call(Hash.SET_PED_TO_RAGDOLL, ped, 1000, 1000, 0, true, true, false);
                    Vector3 dir = (_blackHoleCenter - ped.Position);
                    dir.Normalize();
                    ped.ApplyForce(dir * 12.0f);
                }
            }
        }

        private void CleanupBlackHoleProp()
        {
            // Safety utama anti-stuck:
            // Blackhole berakhir -> collision player/vehicle WAJIB normal lagi.
            ResetBlackholeAntiStuckState();

            _nextBlackholeEffectUpdateTime = 0;

            // Hapus visual Black Hole
            if (_blackHoleProp != null &&
                _blackHoleProp.Exists())
            {
                _blackHoleProp.Delete();
                _blackHoleProp = null;
            }

            // Kalau model masih loading, batalkan juga
            if (_blackHoleToSpawn != null)
            {
                _blackHoleToSpawn.Model.MarkAsNoLongerNeeded();
                _blackHoleToSpawn = null;
            }
        }

        private void ActivateWantedFiveStarGift()
        {
            // Reload supaya edit GoToMountain.ini langsung dipakai
            // pada gift berikutnya tanpa rebuild C#.
            LoadGiftTimerConfigFromIni();

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            _wantedFiveGiftActive =
                true;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftWantedFiveStarDurationMs
                );

            // Gift normal memakai WantedFiveStarSeconds.
            // Random Chaos memakai RandomChaosXDuration.
            _wantedFiveGiftEndTime =
                Game.GameTime +
                activeDurationMs;

            Function.Call(
                Hash.SET_MAX_WANTED_LEVEL,
                5
            );

            Function.Call(
                Hash.SET_POLICE_IGNORE_PLAYER,
                Game.Player.Handle,
                false
            );

            Function.Call(
                Hash.SET_PLAYER_WANTED_LEVEL,
                Game.Player.Handle,
                5,
                false
            );

            Function.Call(
                Hash.SET_PLAYER_WANTED_LEVEL_NOW,
                Game.Player.Handle,
                false
            );

            ShowHudNotification(
                "~r~5 STAR WANTED! ~w~" +
                (activeDurationMs / 1000) +
                " SEC OR UNTIL DEATH"
            );
        }

        private void ProcessNeverWantedSystem()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            if (_wantedFiveGiftActive)
            {
                // =====================================================
                // TIMER HABIS -> KEMBALI NEVER WANTED
                // =====================================================
                if (_wantedFiveGiftEndTime <= 0 ||
                    currentTime >=
                        _wantedFiveGiftEndTime)
                {
                    ResetWantedAfterCustomDeath();

                    ShowHudNotification(
                        "~g~5 STAR WANTED ENDED! ~w~Never Wanted restored."
                    );

                    return;
                }

                // Gift masih aktif: wanted harus tetap 5.
                Function.Call(
                    Hash.SET_MAX_WANTED_LEVEL,
                    5
                );

                Function.Call(
                    Hash.SET_POLICE_IGNORE_PLAYER,
                    Game.Player.Handle,
                    false
                );

                if (Game.Player.Wanted.WantedLevel != 5)
                {
                    Function.Call(
                        Hash.SET_PLAYER_WANTED_LEVEL,
                        Game.Player.Handle,
                        5,
                        false
                    );

                    Function.Call(
                        Hash.SET_PLAYER_WANTED_LEVEL_NOW,
                        Game.Player.Handle,
                        false
                    );
                }

                return;
            }

            // =========================================================
            // DEFAULT = NEVER WANTED
            // =========================================================
            Function.Call(
                Hash.SET_MAX_WANTED_LEVEL,
                0
            );

            Function.Call(
                Hash.SET_POLICE_IGNORE_PLAYER,
                Game.Player.Handle,
                true
            );

            if (Game.Player.Wanted.WantedLevel != 0)
            {
                Function.Call(
                    Hash.SET_PLAYER_WANTED_LEVEL,
                    Game.Player.Handle,
                    0,
                    false
                );

                Function.Call(
                    Hash.SET_PLAYER_WANTED_LEVEL_NOW,
                    Game.Player.Handle,
                    false
                );
            }
        }

        private void ResetWantedAfterCustomDeath()
        {
            _wantedFiveGiftActive =
                false;

            _wantedFiveGiftEndTime =
                0;

            Function.Call(
                Hash.SET_PLAYER_WANTED_LEVEL,
                Game.Player.Handle,
                0,
                false
            );

            Function.Call(
                Hash.SET_PLAYER_WANTED_LEVEL_NOW,
                Game.Player.Handle,
                false
            );

            Function.Call(
                Hash.SET_MAX_WANTED_LEVEL,
                0
            );

            Function.Call(
                Hash.SET_POLICE_IGNORE_PLAYER,
                Game.Player.Handle,
                true
            );
        }

        private void ActivateTeleportSkyGift()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            CleanupGiftCage();

            // Pastikan player punya parachute.
            Function.Call(
                Hash.GIVE_WEAPON_TO_PED,
                player.Handle,
                PARACHUTE_WEAPON_HASH,
                1,
                false,
                true
            );

            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    return;
                }

                Vector3 currentPosition =
                    vehicle.Position;

                Function.Call(
                    Hash.SET_ENTITY_COORDS_NO_OFFSET,
                    vehicle.Handle,
                    currentPosition.X,
                    currentPosition.Y,
                    currentPosition.Z +
                        TELEPORT_SKY_HEIGHT,
                    false,
                    false,
                    false
                );
            }
            else
            {
                Vector3 currentPosition =
                    player.Position;

                // Bersihkan state ragdoll/combat lama.
                Function.Call(
                    Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                    player.Handle
                );

                Function.Call(
                    Hash.SET_ENTITY_COORDS_NO_OFFSET,
                    player.Handle,
                    currentPosition.X,
                    currentPosition.Y,
                    currentPosition.Z +
                        TELEPORT_SKY_HEIGHT,
                    false,
                    false,
                    false
                );

                // Hilangkan momentum aneh yang bikin jatuh dalam keadaan ragdoll.
                Function.Call(
                    Hash.SET_ENTITY_VELOCITY,
                    player.Handle,
                    0.0f,
                    0.0f,
                    0.0f
                );

                // Masuk ke state skydiving supaya parachute bisa dibuka normal.
                Function.Call(
                    Hash.TASK_SKY_DIVE,
                    player.Handle,
                    true
                );
            }

            ShowHudNotification(
                "~b~TELEPORTED TO THE SKY! ~w~Parachute ready."
            );
        }

        private void StartEarthquakeForDuration(
            int durationMs)
        {
            int currentTime =
                Game.GameTime;

            int safeDurationMs =
                Math.Max(
                    1,
                    durationMs
                );

            _earthquakeEndTime =
                currentTime +
                safeDurationMs;

            _nextEarthquakePulseTime =
                currentTime;

            _nextEarthquakeCameraKickTime =
                currentTime;

            _earthquakePulseIndex =
                0;

            _earthquakeCameraStarted =
                false;

            ShowHudNotification(
                "~o~EARTHQUAKE! ~w~" +
                (int)Math.Ceiling(
                    safeDurationMs /
                    1000.0
                ) +
                " SECONDS"
            );
        }

        private void ActivateEarthquakeGift()
        {
            // Reload supaya edit GoToMountain.ini langsung dipakai
            // pada gift berikutnya tanpa rebuild C#.
            LoadGiftTimerConfigFromIni();

            int currentTime =
                Game.GameTime;

            int activeDurationMs =
                GetModeEffectDuration(
                    _giftEarthquakeDurationMs
                );

            // =====================================================
            // BLACKHOLE SEDANG AKTIF -> EARTHQUAKE WAJIB MENUNGGU.
            // Gift Earthquake tambahan saat menunggu menambah total
            // durasi Earthquake yang akan dijalankan nanti.
            // =====================================================
            if (currentTime <
                _blackholeEndTime)
            {
                _pendingEarthquakeDurationMs +=
                    Math.Max(
                        1,
                        activeDurationMs
                    );

                int waitSeconds =
                    Math.Max(
                        0,
                        (int)Math.Ceiling(
                            (_blackholeEndTime -
                             currentTime) /
                            1000.0
                        )
                    );

                ShowHudNotification(
                    "~o~EARTHQUAKE QUEUED! ~w~Starts after BLACK HOLE (" +
                    waitSeconds +
                    "s)"
                );

                return;
            }

            StartEarthquakeForDuration(
                activeDurationMs
            );
        }

        private void RestartEarthquakeCameraShake(
            int currentTime,
            float amplitude)
        {
            if (currentTime <
                _nextEarthquakeCameraKickTime)
            {
                return;
            }

            _nextEarthquakeCameraKickTime =
                currentTime +
                EARTHQUAKE_CAMERA_KICK_INTERVAL_MS;

            // Kamera sengaja SANGAT ringan.
            // Fokus utama earthquake ada di physics object, bukan bikin viewer pusing.
            Function.Call(
                Hash.SHAKE_GAMEPLAY_CAM,
                "SMALL_EXPLOSION_SHAKE",
                amplitude
            );

            Function.Call(
                Hash.SET_GAMEPLAY_CAM_SHAKE_AMPLITUDE,
                amplitude
            );

            _earthquakeCameraStarted =
                true;
        }

        private void StopEarthquakeGift()
        {
            _earthquakeEndTime =
                0;

            _nextEarthquakePulseTime =
                0;

            _nextEarthquakeCameraKickTime =
                0;

            _earthquakePulseIndex =
                0;

            Function.Call(
                Hash.STOP_GAMEPLAY_CAM_SHAKING,
                true
            );

            _earthquakeCameraStarted =
                false;
        }

        private void ProcessBlackholeEarthquakeQueue(
            int currentTime)
        {
            // Selama salah satu masih aktif, effect lawan tetap menunggu.
            if (currentTime <
                    _blackholeEndTime ||
                currentTime <
                    _earthquakeEndTime)
            {
                return;
            }

            // Normalnya hanya satu queue yang mungkin terisi.
            // Blackhole dicek lebih dulu sebagai fallback jika suatu kondisi
            // eksternal membuat keduanya terisi bersamaan.
            if (_pendingBlackholeDurationMs > 0)
            {
                int queuedDurationMs =
                    _pendingBlackholeDurationMs;

                if (StartBlackholeForDuration(
                        queuedDurationMs
                    ))
                {
                    _pendingBlackholeDurationMs =
                        0;
                }

                return;
            }

            if (_pendingEarthquakeDurationMs > 0)
            {
                int queuedDurationMs =
                    _pendingEarthquakeDurationMs;

                _pendingEarthquakeDurationMs =
                    0;

                StartEarthquakeForDuration(
                    queuedDurationMs
                );
            }
        }

        private void ApplyEarthquakeForceToVehicle(
            Vehicle vehicle,
            Vector3 playerPosition,
            float forceX,
            float forceY,
            float forceZ,
            float rollOffset,
            HashSet<int> affectedHandles)
        {
            if (vehicle == null ||
                !vehicle.Exists())
            {
                return;
            }

            if (affectedHandles.Contains(
                    vehicle.Handle))
            {
                return;
            }

            if (vehicle.Position.DistanceTo(
                    playerPosition) >
                EARTHQUAKE_ENTITY_RADIUS)
            {
                return;
            }

            affectedHandles.Add(
                vehicle.Handle
            );

            // Force diberikan sedikit off-center.
            // Hasilnya bukan cuma bergeser, tetapi body kendaraan ikut
            // miring / rocking seperti tanah menghentak dari bawah.
            Function.Call(
                Hash.APPLY_FORCE_TO_ENTITY,
                vehicle.Handle,
                1,
                forceX,
                forceY,
                forceZ,
                rollOffset,
                0.0f,
                0.15f,
                0,
                false,
                true,
                true,
                false,
                true
            );
        }

        private void ApplyEarthquakeForceToProp(
            Prop prop,
            Vector3 playerPosition,
            float forceX,
            float forceY,
            float forceZ,
            HashSet<int> affectedHandles)
        {
            if (prop == null ||
                !prop.Exists())
            {
                return;
            }

            if (affectedHandles.Contains(
                    prop.Handle))
            {
                return;
            }

            if (prop.Position.DistanceTo(
                    playerPosition) >
                EARTHQUAKE_ENTITY_RADIUS)
            {
                return;
            }

            affectedHandles.Add(
                prop.Handle
            );

            // Hanya loose / physics-enabled props yang benar-benar akan bergerak.
            // Static map geometry akan mengabaikan force secara natural.
            Function.Call(
                Hash.SET_ENTITY_DYNAMIC,
                prop.Handle,
                true
            );

            Function.Call(
                Hash.APPLY_FORCE_TO_ENTITY,
                prop.Handle,
                1,
                forceX,
                forceY,
                forceZ,
                0.25f,
                -0.20f,
                0.20f,
                0,
                false,
                true,
                true,
                false,
                true
            );
        }

        private void ApplyEarthquakeForceToPed(
            Ped ped,
            Ped player,
            float forceX,
            float forceY,
            float forceZ,
            bool strongPulse,
            HashSet<int> affectedHandles)
        {
            if (ped == null ||
                !ped.Exists() ||
                !ped.IsAlive)
            {
                return;
            }

            if (affectedHandles.Contains(
                    ped.Handle))
            {
                return;
            }

            if (ped.Position.DistanceTo(
                    player.Position) >
                EARTHQUAKE_ENTITY_RADIUS)
            {
                return;
            }

            // Ped yang sedang di vehicle tidak dipaksa keluar.
            // Vehicle-nya sendiri diguncang keras.
            if (ped.IsInVehicle())
            {
                return;
            }

            affectedHandles.Add(
                ped.Handle
            );

            ped.CanRagdoll =
                true;

            // Selama earthquake, semua PED jalan kaki dibuat kehilangan
            // keseimbangan terus menerus. Durasi lebih panjang dari pulse
            // supaya mereka tidak sempat berdiri normal di tengah gempa.
            Function.Call(
                Hash.SET_PED_TO_RAGDOLL,
                ped.Handle,
                strongPulse ? 1100 : 750,
                strongPulse ? 1500 : 1000,
                0,
                true,
                true,
                false
            );

            ped.ApplyForce(
                new Vector3(
                    forceX,
                    forceY,
                    forceZ
                )
            );
        }

        private void ProcessEarthquakeGift(
            int currentTime)
        {
            if (_earthquakeEndTime <= 0)
                return;

            if (currentTime >=
                _earthquakeEndTime)
            {
                StopEarthquakeGift();

                ShowHudNotification(
                    "~g~EARTHQUAKE ENDED"
                );

                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            // Saat custom death jangan bentrok dengan black overlay / death state.
            // Timer tetap berjalan.
            if (_customDeathActive)
            {
                if (_earthquakeCameraStarted)
                {
                    Function.Call(
                        Hash.STOP_GAMEPLAY_CAM_SHAKING,
                        true
                    );

                    _earthquakeCameraStarted =
                        false;
                }

                return;
            }

            // =========================================================
            // PLAYER HARUS MENYENTUH TANAH
            // =========================================================
            // Gempa berasal dari tanah. Jika karakter / kendaraan player
            // sedang airborne, player tidak menerima shake/ragdoll/force.
            Entity playerGroundEntity =
                player.IsInVehicle() &&
                player.CurrentVehicle != null &&
                player.CurrentVehicle.Exists()
                    ? (Entity)player.CurrentVehicle
                    : player;

            bool playerTouchesGround =
                playerGroundEntity != null &&
                playerGroundEntity.Exists() &&
                !Function.Call<bool>(
                    Hash.IS_ENTITY_IN_AIR,
                    playerGroundEntity.Handle
                );

            // =========================================================
            // CAMERA SHAKE - hanya saat player menyentuh tanah
            // =========================================================
            // Dua gelombang berbeda membuat shake tidak terasa flat.
            float waveA =
                (float)Math.Abs(
                    Math.Sin(
                        currentTime *
                        0.019
                    )
                );

            float waveB =
                (float)Math.Abs(
                    Math.Sin(
                        currentTime *
                        0.0067
                    )
                );

            // Kamera cuma memberi sedikit rasa getaran.
            // Range kira-kira 0.14 - 0.30 supaya viewer tidak pusing.
            float cameraAmplitude =
                0.14f +
                (waveA * 0.10f) +
                (waveB * 0.06f);

            if (playerTouchesGround)
            {
                RestartEarthquakeCameraShake(
                    currentTime,
                    cameraAmplitude
                );
            }
            else if (_earthquakeCameraStarted)
            {
                Function.Call(
                    Hash.STOP_GAMEPLAY_CAM_SHAKING,
                    true
                );

                _earthquakeCameraStarted =
                    false;
            }

            // Physical quake tidak perlu dieksekusi tiap frame.
            if (currentTime <
                _nextEarthquakePulseTime)
            {
                return;
            }

            _nextEarthquakePulseTime =
                currentTime +
                EARTHQUAKE_PULSE_INTERVAL_MS;

            _earthquakePulseIndex++;

            // =========================================================
            // HITUNG PULSE GEMPA
            // =========================================================
            bool strongPulse =
                (_earthquakePulseIndex % 7 == 0) ||
                (_earthquakePulseIndex % 11 == 0);

            float signX =
                (_earthquakePulseIndex % 2 == 0)
                    ? 1.0f
                    : -1.0f;

            float signY =
                (_earthquakePulseIndex % 3 == 0)
                    ? -1.0f
                    : 1.0f;

            float jitterX =
                ((float)_random.NextDouble() -
                 0.5f) *
                2.2f;

            float jitterY =
                ((float)_random.NextDouble() -
                 0.5f) *
                2.2f;

            // Horizontal sangat dominan seperti tanah bergeser.
            float horizontalStrength =
                strongPulse
                    ? 14.0f
                    : 8.5f;

            float verticalStrength =
                strongPulse
                    ? 4.2f
                    : 1.80f;

            float vehicleForceX =
                (signX *
                 horizontalStrength) +
                jitterX;

            float vehicleForceY =
                (signY *
                 (horizontalStrength * 0.85f)) +
                jitterY;

            float vehicleForceZ =
                verticalStrength;

            float rollOffset =
                (_earthquakePulseIndex % 2 == 0)
                    ? 1.15f
                    : -1.15f;

            Vector3 playerPosition =
                player.Position;

            HashSet<int> affectedVehicleHandles =
                new HashSet<int>();

            HashSet<int> affectedPedHandles =
                new HashSet<int>();

            Vehicle playerControlledVehicle =
                player.IsInVehicle()
                    ? player.CurrentVehicle
                    : null;

            // =========================================================
            // ALL NEARBY VEHICLES
            // =========================================================
            // Bukan hanya enemy vehicle. Traffic sekitar juga ikut
            // rocking sehingga lingkungan terlihat benar-benar gempa.
            foreach (
                Vehicle vehicle in
                World.GetNearbyVehicles(
                    playerPosition,
                    EARTHQUAKE_ENTITY_RADIUS
                ))
            {
                // Kendaraan yang sedang dipakai player diproses TERPISAH
                // di bawah supaya benar-benar no-effect saat airborne.
                if (playerControlledVehicle != null &&
                    playerControlledVehicle.Exists() &&
                    vehicle != null &&
                    vehicle.Exists() &&
                    vehicle.Handle ==
                        playerControlledVehicle.Handle)
                {
                    continue;
                }

                ApplyEarthquakeForceToVehicle(
                    vehicle,
                    playerPosition,
                    vehicleForceX,
                    vehicleForceY,
                    vehicleForceZ,
                    rollOffset,
                    affectedVehicleHandles
                );
            }

            // =========================================================
            // PLAYER
            // =========================================================
            if (playerTouchesGround)
            {
                if (player.IsInVehicle())
                {
                    Vehicle playerVehicle =
                        player.CurrentVehicle;

                    ApplyEarthquakeForceToVehicle(
                        playerVehicle,
                        playerPosition,
                        vehicleForceX * 1.10f,
                        vehicleForceY * 1.10f,
                        vehicleForceZ * 1.10f,
                        rollOffset,
                        affectedVehicleHandles
                    );
                }
                else
                {
                    // Player jalan kaki hanya terkena earthquake saat benar-benar
                    // menyentuh tanah. Saat melompat / jatuh / airborne: no effect.
                    player.CanRagdoll =
                        true;

                    Function.Call(
                        Hash.SET_PED_TO_RAGDOLL,
                        player.Handle,
                        strongPulse ? 1200 : 800,
                        strongPulse ? 1600 : 1100,
                        0,
                        true,
                        true,
                        false
                    );

                    player.ApplyForce(
                        new Vector3(
                            vehicleForceX * 0.62f,
                            vehicleForceY * 0.62f,
                            strongPulse
                                ? 2.6f
                                : 1.0f
                        )
                    );

                    affectedPedHandles.Add(
                        player.Handle
                    );
                }
            }

            // =========================================================
            // ALL NEARBY PROPS / OBJECT PHYSICS
            // =========================================================
            // Loose object seperti tong, cone, trash, box, dsb ikut
            // berguncang. Static building/map geometry tetap tidak bergerak.
            HashSet<int> affectedPropHandles =
                new HashSet<int>();

            foreach (
                Prop prop in
                World.GetNearbyProps(
                    playerPosition,
                    EARTHQUAKE_ENTITY_RADIUS
                ))
            {
                ApplyEarthquakeForceToProp(
                    prop,
                    playerPosition,
                    vehicleForceX * 0.75f,
                    vehicleForceY * 0.75f,
                    strongPulse
                        ? 3.2f
                        : 1.25f,
                    affectedPropHandles
                );
            }

            // =========================================================
            // ALL NEARBY PEDS
            // =========================================================
            // Semua PED jalan kaki terus ragdoll / terdorong selama gempa.
            foreach (
                Ped ped in
                World.GetNearbyPeds(
                    playerPosition,
                    EARTHQUAKE_ENTITY_RADIUS
                ))
            {
                if (ped == null ||
                    !ped.Exists())
                {
                    continue;
                }

                // Player sudah diproses di atas.
                if (ped.Handle ==
                    player.Handle)
                {
                    continue;
                }

                ApplyEarthquakeForceToPed(
                    ped,
                    player,
                    vehicleForceX * 0.45f,
                    vehicleForceY * 0.45f,
                    strongPulse
                        ? 2.0f
                        : 0.65f,
                    strongPulse,
                    affectedPedHandles
                );
            }
        }

        private Vector3 GetCustomDeathGroundRespawnPosition(
            Vector3 deathPosition)
        {
            Vector3 safePosition;

            // =========================================================
            // 1. COBA TANAH TEPAT DI BAWAH TITIK MATI
            // =========================================================
            // Contoh BLACKHOLE:
            // player mati di Z tinggi -> respawn di ground X/Y yang sama.
            // =========================================================
            if (TryGetCustomDeathGroundAt(
                    deathPosition.X,
                    deathPosition.Y,
                    deathPosition.Z,
                    out safePosition
                ))
            {
                return safePosition;
            }

            // =========================================================
            // 2. CARI DARAT TERDEKAT DI SEKITAR TITIK MATI
            // =========================================================
            for (
                float radius = 10.0f;
                radius <= 300.0f;
                radius += 10.0f)
            {
                for (
                    int angleDegree = 0;
                    angleDegree < 360;
                    angleDegree += 30)
                {
                    float radians =
                        angleDegree *
                        ((float)Math.PI / 180.0f);

                    float x =
                        deathPosition.X +
                        (float)Math.Cos(radians) *
                        radius;

                    float y =
                        deathPosition.Y +
                        (float)Math.Sin(radians) *
                        radius;

                    if (TryGetCustomDeathGroundAt(
                            x,
                            y,
                            deathPosition.Z,
                            out safePosition
                        ))
                    {
                        return safePosition;
                    }
                }
            }

            // =========================================================
            // 3. FALLBACK KE ROAD NODE TERDEKAT
            // =========================================================
            for (
                float radius = 0.0f;
                radius <= 1000.0f;
                radius += 100.0f)
            {
                for (
                    int angleDegree = 0;
                    angleDegree < 360;
                    angleDegree += 45)
                {
                    float radians =
                        angleDegree *
                        ((float)Math.PI / 180.0f);

                    Vector3 searchPos =
                        new Vector3(
                            deathPosition.X +
                                (float)Math.Cos(radians) *
                                radius,
                            deathPosition.Y +
                                (float)Math.Sin(radians) *
                                radius,
                            Math.Max(
                                500.0f,
                                deathPosition.Z
                            )
                        );

                    Vector3 streetPos =
                        World.GetNextPositionOnStreet(
                            searchPos
                        );

                    if (streetPos ==
                        Vector3.Zero)
                    {
                        continue;
                    }

                    // Street node GTA adalah permukaan jalan/darat.
                    return
                        streetPos +
                        new Vector3(
                            0.0f,
                            0.0f,
                            1.0f
                        );
                }
            }

            // =========================================================
            // 4. FALLBACK TERAKHIR = START YANG AMAN
            // =========================================================
            Vector3 fallbackStart =
                _missionGoToMountainEnabled
                    ? _missionStartPosition
                    : _startPosition;

            if (fallbackStart ==
                Vector3.Zero)
            {
                fallbackStart =
                    _missionStartPosition;
            }

            if (TryGetCustomDeathGroundAt(
                    fallbackStart.X,
                    fallbackStart.Y,
                    fallbackStart.Z,
                    out safePosition
                ))
            {
                return safePosition;
            }

            return
                fallbackStart +
                new Vector3(
                    0.0f,
                    0.0f,
                    1.0f
                );
        }

        private void StopCheckpointVehicleDance()
        {
            if (_checkpointCelebrationVehicle != null &&
                _checkpointCelebrationVehicle.Exists())
            {
                // Kembalikan pitch / roll mendekati posisi awal.
                // Heading memakai heading terbaru agar tidak memutar kendaraan
                // kembali ke arah saat pertama memasuki checkpoint.
                float currentHeading =
                    _checkpointCelebrationVehicle.Heading;

                Function.Call(
                    Hash.SET_ENTITY_ROTATION,
                    _checkpointCelebrationVehicle.Handle,
                    _checkpointCelebrationVehicleBaseRotation.X,
                    _checkpointCelebrationVehicleBaseRotation.Y,
                    currentHeading,
                    2,
                    true
                );
            }

            _checkpointCelebrationVehicle =
                null;

            _checkpointCelebrationVehicleBaseRotation =
                Vector3.Zero;

            _checkpointCelebrationVehicleStartTime =
                0;
        }

        private void StartRandomTeleport()
        {
            LoadGiftTimerConfigFromIni();
            LoadRandomTeleportConfigFromIni();

            if (_randomTeleportUseGachaCards)
            {
                // Kalau sebelumnya mode safe-global sedang mencari titik,
                // batalkan agar tidak menembak teleport kedua saat gacha aktif.
                _safeRandomTeleportPending = false;
                _safeRandomTeleportCandidate = Vector3.Zero;
                _safeRandomTeleportCandidateStartTime = 0;
                _safeRandomTeleportCandidateRetryCount = 0;
                ReleaseRandomTeleportGroundGuard();

                // Mode lama: roulette/card memakai daftar [LOCATIONS].
                StartTeleportGachaRoulette();
                return;
            }

            // Mode safe-global: hentikan roulette lama supaya card tidak tetap
            // berjalan ketika setting sudah diubah ke false.
            _teleportGachaQueue.Clear();
            _isTeleportGachaActive = false;
            _teleportGachaLocked = false;

            BeginSafeGlobalRandomTeleport();
        }

        private void BeginSafeGlobalRandomTeleport()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead ||
                _customDeathActive)
            {
                return;
            }

            ReleaseRandomTeleportGroundGuard();

            _safeRandomTeleportPending = true;
            _safeRandomTeleportCandidate = Vector3.Zero;
            _safeRandomTeleportCandidateStartTime = 0;
            _safeRandomTeleportCandidateRetryCount = 0;
        }

        private bool TryCreateGlobalRandomTeleportCandidate(
            out Vector3 candidate)
        {
            candidate =
                Vector3.Zero;

            for (
                int attempt = 0;
                attempt < RANDOM_TELEPORT_SAFE_NODE_ATTEMPTS;
                attempt++)
            {
                float randomX =
                    GLOBAL_ROUTE_MIN_X +
                    ((float)_random.NextDouble() *
                     (GLOBAL_ROUTE_MAX_X -
                      GLOBAL_ROUTE_MIN_X));

                float randomY =
                    GLOBAL_ROUTE_MIN_Y +
                    ((float)_random.NextDouble() *
                     (GLOBAL_ROUTE_MAX_Y -
                      GLOBAL_ROUTE_MIN_Y));

                Vector3 searchPos =
                    new Vector3(
                        randomX,
                        randomY,
                        500.0f
                    );

                Vector3 streetPos =
                    World.GetNextPositionOnStreet(
                        searchPos
                    );

                if (streetPos == Vector3.Zero)
                    continue;

                // Road node dipakai sebagai filter exterior. Ini menghindari
                // koordinat interior / rumah / ruangan tertutup.
                if (streetPos.Z < -20.0f ||
                    streetPos.Z > 1000.0f)
                {
                    continue;
                }

                OutputArgument waterZArg =
                    new OutputArgument();

                bool hasWater =
                    Function.Call<bool>(
                        Hash.GET_WATER_HEIGHT,
                        streetPos.X,
                        streetPos.Y,
                        streetPos.Z + 2.0f,
                        waterZArg
                    );

                if (hasWater)
                {
                    float waterZ =
                        waterZArg.GetResult<float>();

                    // Tolak road/node yang ternyata berada di bawah air.
                    if (waterZ >
                        streetPos.Z + 0.75f)
                    {
                        continue;
                    }
                }

                candidate =
                    streetPos;

                return true;
            }

            return false;
        }

        private bool TryValidateGlobalRandomTeleportGround(
            Vector3 candidate,
            out Vector3 safePosition)
        {
            safePosition =
                Vector3.Zero;

            if (candidate == Vector3.Zero)
                return false;

            float probeZ =
                Math.Max(
                    1000.0f,
                    candidate.Z + 500.0f
                );

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                candidate.X,
                candidate.Y,
                probeZ
            );

            float groundZ =
                GetGroundHeightCompat(
                    new Vector3(
                        candidate.X,
                        candidate.Y,
                        probeZ
                    )
                );

            // 0 biasanya berarti collision/ground belum tersedia. Jangan pernah
            // memakai angka itu sebagai Z teleport.
            if (groundZ == 0.0f)
                return false;

            // Road node dan ground harus menunjuk permukaan yang sama.
            // Ini menolak bridge/roof/underground mismatch yang bisa membuat
            // player jatuh atau muncul di tempat aneh.
            if (Math.Abs(
                    groundZ -
                    candidate.Z
                ) > RANDOM_TELEPORT_SAFE_MAX_GROUND_DELTA)
            {
                return false;
            }

            OutputArgument waterZArg =
                new OutputArgument();

            bool hasWater =
                Function.Call<bool>(
                    Hash.GET_WATER_HEIGHT,
                    candidate.X,
                    candidate.Y,
                    groundZ + 2.0f,
                    waterZArg
                );

            if (hasWater)
            {
                float waterZ =
                    waterZArg.GetResult<float>();

                if (waterZ >
                    groundZ + 0.75f)
                {
                    return false;
                }
            }

            safePosition =
                new Vector3(
                    candidate.X,
                    candidate.Y,
                    groundZ +
                        RANDOM_TELEPORT_SAFE_Z_OFFSET
                );

            return true;
        }

        private void ProcessSafeRandomTeleportLogic(
            int currentTime)
        {
            if (!_safeRandomTeleportPending)
                return;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead ||
                _customDeathActive)
            {
                _safeRandomTeleportPending = false;
                _safeRandomTeleportCandidate = Vector3.Zero;
                return;
            }

            if (_safeRandomTeleportCandidate ==
                Vector3.Zero)
            {
                if (_safeRandomTeleportCandidateRetryCount >=
                    RANDOM_TELEPORT_SAFE_CANDIDATE_RETRIES)
                {
                    _safeRandomTeleportPending = false;

                    ShowHudNotification(
                        "~y~RANDOM TELEPORT CANCELLED ~w~- no safe ground found."
                    );

                    return;
                }

                Vector3 nextCandidate;

                if (!TryCreateGlobalRandomTeleportCandidate(
                        out nextCandidate
                    ))
                {
                    _safeRandomTeleportPending = false;

                    ShowHudNotification(
                        "~y~RANDOM TELEPORT CANCELLED ~w~- no valid road node found."
                    );

                    return;
                }

                _safeRandomTeleportCandidate =
                    nextCandidate;

                _safeRandomTeleportCandidateStartTime =
                    currentTime;

                _safeRandomTeleportCandidateRetryCount++;

                Function.Call(
                    Hash.REQUEST_COLLISION_AT_COORD,
                    nextCandidate.X,
                    nextCandidate.Y,
                    Math.Max(
                        1000.0f,
                        nextCandidate.Z + 500.0f
                    )
                );

                return;
            }

            Vector3 safePosition;

            if (TryValidateGlobalRandomTeleportGround(
                    _safeRandomTeleportCandidate,
                    out safePosition
                ))
            {
                ExecuteGlobalSafeRandomTeleport(
                    safePosition,
                    currentTime
                );

                _safeRandomTeleportPending = false;
                _safeRandomTeleportCandidate = Vector3.Zero;
                _safeRandomTeleportCandidateStartTime = 0;

                return;
            }

            // Beri collision loader kesempatan sebentar. Kalau tetap tidak
            // valid, buang kandidat dan cari road node lain.
            if (unchecked(
                    currentTime -
                    _safeRandomTeleportCandidateStartTime
                ) >= RANDOM_TELEPORT_SAFE_CANDIDATE_TIMEOUT_MS)
            {
                _safeRandomTeleportCandidate =
                    Vector3.Zero;

                _safeRandomTeleportCandidateStartTime =
                    0;
            }
        }

        private void ExecuteGlobalSafeRandomTeleport(
            Vector3 safePosition,
            int currentTime)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            CleanupGiftCage();

            Entity target =
                player.IsInVehicle()
                    ? (Entity)player.CurrentVehicle
                    : player;

            if (target == null ||
                !target.Exists())
            {
                return;
            }

            _randomTeleportGroundGuardFallbackPosition =
                target.Position;

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                safePosition.X,
                safePosition.Y,
                safePosition.Z
            );

            // Freeze sebelum perpindahan agar physics tidak sempat menjatuhkan
            // entity ketika area tujuan baru saja di-stream.
            target.IsPositionFrozen =
                true;

            target.Position =
                safePosition;

            target.Velocity =
                Vector3.Zero;

            Vehicle targetVehicle =
                target as Vehicle;

            if (targetVehicle != null &&
                targetVehicle.Exists())
            {
                targetVehicle.PlaceOnGround();
                targetVehicle.Velocity = Vector3.Zero;
            }

            _randomTeleportGroundGuardEntity =
                target;

            _randomTeleportGroundGuardPosition =
                safePosition;

            _randomTeleportGroundGuardEndTime =
                currentTime +
                RANDOM_TELEPORT_SAFE_GROUND_HOLD_MS;

            _randomTeleportGroundGuardActive =
                true;

            ShowHudNotification(
                "~g~RANDOM TELEPORT SAFE! ~w~Outdoor ground locked."
            );
        }

        private void ProcessRandomTeleportGroundGuard(
            int currentTime)
        {
            if (!_randomTeleportGroundGuardActive)
                return;

            Entity target =
                _randomTeleportGroundGuardEntity;

            if (target == null ||
                !target.Exists())
            {
                ReleaseRandomTeleportGroundGuard();
                return;
            }

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                _randomTeleportGroundGuardPosition.X,
                _randomTeleportGroundGuardPosition.Y,
                _randomTeleportGroundGuardPosition.Z
            );

            Vector3 verifiedPosition;

            bool groundStillValid =
                TryValidateGlobalRandomTeleportGround(
                    _randomTeleportGroundGuardPosition,
                    out verifiedPosition
                );

            if (groundStillValid)
            {
                target.Position =
                    verifiedPosition;

                target.Velocity =
                    Vector3.Zero;
            }

            if (currentTime <
                _randomTeleportGroundGuardEndTime)
            {
                return;
            }

            if (!groundStillValid)
            {
                // Fail-safe terakhir: jangan biarkan player jatuh di void.
                // Kembalikan ke posisi sebelum teleport jika ground tujuan
                // tiba-tiba gagal tervalidasi setelah perpindahan.
                target.Position =
                    _randomTeleportGroundGuardFallbackPosition;

                target.Velocity =
                    Vector3.Zero;

                ShowHudNotification(
                    "~y~RANDOM TELEPORT REVERTED ~w~- ground safety check failed."
                );
            }
            else
            {
                Vehicle targetVehicle =
                    target as Vehicle;

                if (targetVehicle != null &&
                    targetVehicle.Exists())
                {
                    targetVehicle.PlaceOnGround();
                    targetVehicle.Velocity = Vector3.Zero;
                }
            }

            ReleaseRandomTeleportGroundGuard();
        }

        private void ReleaseRandomTeleportGroundGuard()
        {
            Entity target =
                _randomTeleportGroundGuardEntity;

            if (target != null &&
                target.Exists())
            {
                try
                {
                    target.IsPositionFrozen =
                        false;
                }
                catch
                {
                }
            }

            _randomTeleportGroundGuardActive = false;
            _randomTeleportGroundGuardEntity = null;
            _randomTeleportGroundGuardPosition = Vector3.Zero;
            _randomTeleportGroundGuardFallbackPosition = Vector3.Zero;
            _randomTeleportGroundGuardEndTime = 0;
        }

        private void StartTeleportGachaRoulette()
        {
            LoadGiftTimerConfigFromIni();

            // =========================================================
            // BATASI ANTREAN TELEPORT GACHA
            //
            // Maksimal 3 pending.
            // Kalau penuh, buang request paling lama.
            // =========================================================
            while (_teleportGachaQueue.Count >= MAX_TELEPORT_GACHA_QUEUE)
            {
                _teleportGachaQueue.Dequeue();
            }

            _teleportGachaQueue.Enqueue(
                "TRIGGER"
            );
        }

        private void ProcessTeleportGachaLogic(int currentTime)
        {
            if (_teleportGachaPoolReloadPending &&
                !_isTeleportGachaActive)
            {
                InitializeTeleportGachaPool();
                _teleportGachaPoolReloadPending = false;
            }

            if (_teleportGachaPool == null ||
                _teleportGachaPool.Count == 0)
            {
                RefreshTeleportGachaPoolGlobal();
            }

            if (_teleportGachaPool == null ||
                _teleportGachaPool.Count == 0)
            {
                return;
            }

            if (!_isTeleportGachaActive)
            {
                if (_teleportGachaQueue.Count > 0)
                {
                    _teleportGachaQueue.Dequeue();
                    _isTeleportGachaActive = true; _teleportGachaLocked = false; _teleportGachaStartTime = currentTime; _lastTeleportGachaItemChange = currentTime;
                    _teleportGachaDuration = _giftRandomTeleportRouletteDurationMs; _teleportGachaItemChangeInterval = 80;
                }
                else return;
            }

            int elapsedTime = currentTime - _teleportGachaStartTime;
            if (!_teleportGachaLocked)
            {
                if (currentTime - _lastTeleportGachaItemChange >= _teleportGachaItemChangeInterval)
                {
                    if (_teleportGachaPool != null && _teleportGachaPool.Count > 0) _currentTeleportGachaIndex = _random.Next(_teleportGachaPool.Count);
                    _lastTeleportGachaItemChange = currentTime;
                    PlayFrontendSoundCompat("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
                if (elapsedTime >= _teleportGachaDuration)
                {
                    _teleportGachaLocked = true; _teleportGachaLockEndTime = currentTime + _giftRandomTeleportLockDurationMs;
                    if (_currentTeleportGachaIndex >= 0 && _currentTeleportGachaIndex < _teleportGachaPool.Count)
                    {
                        var target = _teleportGachaPool[_currentTeleportGachaIndex];
                        ExecuteSafeTeleport(target.TargetPosition.X, target.TargetPosition.Y, target.ManualZ, target.DisplayName);
                    }
                }
            }

            if (_teleportGachaPool != null && _currentTeleportGachaIndex >= 0 && _currentTeleportGachaIndex < _teleportGachaPool.Count) DrawTeleportGachaCardsUI(_teleportGachaPool[_currentTeleportGachaIndex], _teleportGachaLocked);
            if (_teleportGachaLocked && currentTime >= _teleportGachaLockEndTime) _isTeleportGachaActive = false;
        }

        private void StartRandomGatcha()
        {
            while (_randomGatchaQueue.Count >= MAX_RANDOM_GATCHA_QUEUE)
            {
                _randomGatchaQueue.Dequeue();
            }

            _randomGatchaQueue.Enqueue("TRIGGER");
        }

        private void ProcessRandomGatchaLogic(int currentTime)
        {
            if (_randomGatchaOptions == null ||
                _randomGatchaOptions.Count == 0)
            {
                return;
            }

            if (!_randomGatchaActive)
            {
                if (_randomGatchaQueue.Count <= 0)
                    return;

                _randomGatchaQueue.Dequeue();
                _randomGatchaActive = true;
                _randomGatchaLocked = false;
                _randomGatchaEffectExecuted = false;
                _randomGatchaStartTime = currentTime;
                _randomGatchaLastSwitchTime = currentTime;
                _randomGatchaLockEndTime = 0;
                _randomGatchaIndex = _random.Next(_randomGatchaOptions.Count);
            }

            if (!_randomGatchaLocked)
            {
                if (currentTime - _randomGatchaLastSwitchTime >=
                    _randomGatchaSwitchMs)
                {
                    _randomGatchaIndex =
                        _random.Next(_randomGatchaOptions.Count);

                    _randomGatchaLastSwitchTime = currentTime;

                    PlayFrontendSoundCompat(
                        "NAV_UP_DOWN",
                        "HUD_FRONTEND_DEFAULT_SOUNDSET"
                    );
                }

                if (currentTime - _randomGatchaStartTime >=
                    _randomGatchaRollMs)
                {
                    _randomGatchaLocked = true;
                    _randomGatchaLockEndTime =
                        currentTime + _randomGatchaLockMs;

                    PlayFrontendSoundCompat(
                        "SELECT",
                        "HUD_FRONTEND_DEFAULT_SOUNDSET"
                    );
                }
            }

            if (_randomGatchaLocked &&
                !_randomGatchaEffectExecuted)
            {
                _randomGatchaEffectExecuted = true;

                int safeIndex = Math.Max(
                    0,
                    Math.Min(
                        _randomGatchaOptions.Count - 1,
                        _randomGatchaIndex
                    )
                );

                RandomGatchaOption selectedOption =
                    _randomGatchaOptions[
                        safeIndex
                    ];

                _modeDurationOverrideMs =
                    Math.Max(
                        0,
                        selectedOption.DurationMs
                    );

                try
                {
                    ExecuteRandomGatchaEffect(
                        selectedOption.Key
                    );
                }
                finally
                {
                    _modeDurationOverrideMs =
                        0;
                }
            }

            if (_hudRandomGatchaEnabled &&
                !IsNormalHudSuppressed())
            {
                DrawRandomGatchaUI(
                    currentTime
                );
            }

            if (_randomGatchaLocked &&
                currentTime >= _randomGatchaLockEndTime)
            {
                _randomGatchaActive = false;
                _randomGatchaLocked = false;
                _randomGatchaEffectExecuted = false;
                _randomGatchaStartTime = 0;
                _randomGatchaLockEndTime = 0;
            }
        }

        private void ExecuteRandomGatchaEffect(string key)
        {
            ExecuteConfigurableRandomEffect(
                key
            );
        }

        private void ShowInstantGiftHud(
            string text,
            bool isGood)
        {
            if (string.IsNullOrWhiteSpace(
                    text
                ))
            {
                return;
            }

            int currentTime =
                Game.GameTime;

            // INSTANT HUD hanya satu kartu. Gift baru langsung mengganti
            // kartu lama pada frame yang sama, tanpa delay dan tanpa stack.
            // Setiap gift baru juga me-reset waktu tampil kartu menjadi DurationMs.
            _instantGiftHudItems.Clear();
            _instantGiftHudItems.Add(
                new InstantGiftHudItem
                {
                    Text = text,
                    EndTime =
                        currentTime +
                        Math.Max(
                            100,
                            _instantHudDurationMs
                        ),
                    IsGood = isGood
                }
            );
        }

        private bool WasPlayerDamagedByTrackedEnemy(Ped player)
        {
            if (player == null || !player.Exists()) return false;

            // Hanya NPC yang tercatat sebagai ENEMY (_activeBosses).
            // Animal dan sumber damage lain tidak ikut menghitung penalti 3x enemy death.
            for (int i = 0; i < _activeBosses.Count; i++)
            {
                ActiveBossTracker boss = _activeBosses[i];
                if (boss == null || boss.BossPed == null || !boss.BossPed.Exists()) continue;
                if (Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY,
                    player.Handle, boss.BossPed.Handle, true))
                    return true;
            }

            return false;
        }

        private void RecoverEnemyIfBelowGround(ActiveBossTracker boss, Ped player, int currentTime)
        {
            if (boss == null || boss.BossPed == null || !boss.BossPed.Exists()) return;
            Ped enemy = boss.BossPed;
            if (!enemy.IsAlive || enemy.IsInVehicle()) return;
            if (currentTime - boss.LastGroundRecoveryTime < 1000) return;

            Vector3 pos = enemy.Position;
            Vector3 probe = new Vector3(pos.X, pos.Y, Math.Max(pos.Z + 50.0f, 500.0f));
            float groundZ = GetGroundHeightCompat(probe);

            bool clearlyBelowGround = groundZ > 0.0f && pos.Z < groundZ - 1.25f;
            bool clearlyBelowWorld = pos.Z < -20.0f;
            if (!clearlyBelowGround && !clearlyBelowWorld) return;

            boss.LastGroundRecoveryTime = currentTime;

            if (groundZ > 0.0f)
            {
                enemy.Position = new Vector3(pos.X, pos.Y, groundZ + 0.85f);
                Function.Call(Hash.SET_ENTITY_VELOCITY, enemy.Handle, 0.0f, 0.0f, 0.0f);
            }
            else if (player != null && player.Exists())
            {
                Vector3 safe = GetEnemyRespawnPosition(player, boss.Config, false);
                enemy.Position = safe;
            }

            if (boss.Config == null || boss.Config.AttackPlayer)
            {
                if (player != null && player.Exists() && !player.IsDead)
                    Function.Call(Hash.TASK_COMBAT_PED, enemy.Handle, player.Handle, 0, 16);
            }
        }

    }
}
