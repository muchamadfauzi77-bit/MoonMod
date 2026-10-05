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
        private static float GetGroundHeightCompat(Vector3 position)
        {
            return World.GetGroundHeight(
                position,
                out float groundHeight,
                GetGroundHeightMode.Normal
            )
                ? groundHeight
                : 0.0f;
        }

        private static void ShowNotificationCompat(string message)
        {
            Notification.PostTicker(message, false);
        }

        private static int GenerateHashCompat(string value)
        {
            // GET_HASH_KEY uses GTA's Jenkins/joaat hash, matching the old Game.GenerateHash behavior.
            return Function.Call<int>(Hash.GET_HASH_KEY, value ?? string.Empty);
        }

        private static bool IsControlPressedCompat(GTA.Control control)
        {
            // Pertahankan perilaku lama Game.IsControlPressed secara persis,
            // tanpa memakai overload GTA.Input.Controls yang butuh
            // ControlType + ControlAction.
            return Function.Call<bool>(
                Hash.IS_DISABLED_CONTROL_PRESSED,
                0,
                (int)control
            );
        }

        private static void PlayFrontendSoundCompat(string soundName, string soundSet)
        {
            Audio.PlaySoundFrontendAndForget(soundName, soundSet);
        }

        private bool HasGameTimeReached(
            int currentTime,
            int targetTime)
        {
            return
                unchecked(
                    currentTime - targetTime
                ) >= 0;
        }

        private static void WriteHwidDiagnosticLog(
            string status,
            HardwareSnapshot current)
        {
            try
            {
                string directory =
                    Path.GetDirectoryName(
                        HWID_DIAGNOSTIC_LOG
                    );

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(
                        directory
                    );
                }

                if (current == null)
                    current = new HardwareSnapshot();

                StringBuilder log =
                    new StringBuilder();

                log.AppendLine(
                    "============================================================"
                );

                log.AppendLine(
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture
                    ) +
                    " | " +
                    (string.IsNullOrWhiteSpace(status)
                        ? "UNKNOWN"
                        : status.Trim())
                );

                log.AppendLine(
                    "MachineGuid=" +
                    NormalizeHardwareComponent(current.MachineGuid)
                );

                log.AppendLine(
                    "VolumeSerial=" +
                    NormalizeHardwareComponent(current.VolumeSerial)
                );

                log.AppendLine(
                    "CpuIdentifier=" +
                    NormalizeHardwareComponent(current.CpuIdentifier)
                );

                log.AppendLine(
                    "SystemProductName=" +
                    NormalizeHardwareComponent(current.SystemProductName)
                );

                log.AppendLine(
                    "SystemSku=" +
                    NormalizeHardwareComponent(current.SystemSku)
                );

                log.AppendLine(
                    "CombinedHash=" +
                    NormalizeHardwareComponent(current.CombinedHash)
                );

                File.AppendAllText(
                    HWID_DIAGNOSTIC_LOG,
                    log.ToString(),
                    Encoding.UTF8
                );
            }
            catch
            {
            }
        }

        private void OnStartupNameTick(object sender, EventArgs e)
        {
            try
            {
                ProcessStartupPlayerNameInput();

                if (!_startupNameConfirmed)
                {
                    return;
                }

                // PROFILE + GAMEPLAY/ROUTE + DIFFICULTY SELECT selesai, tetapi GTA BELUM dibuka.
                // Tetap TimeScale=0 dan masuk ke black loading gate sampai route siap.
                Tick -= OnStartupNameTick;

                // Pakai wall-clock, bukan Game.GameTime. Profile/Game Mode memakai
                // TimeScale=0 sehingga Game.GameTime dapat berhenti/terlambat maju.
                _startupLoadingStartTime = Environment.TickCount;
                _startupRouteReadySince = 0;

                InitializeGameplayAfterNameInput();

                // Setelah START world harus berjalan di balik black screen supaya
                // collision/path-node/streaming dapat selesai. Player tetap dibekukan.
                Function.Call(Hash.SET_TIME_SCALE, 1.0f);
                SetStartupLoadingProtection(true);

                // Jangan aktifkan OnTick utama dulu. Loading tick hanya menyiapkan
                // start position + route agar world GTA tidak terlihat setengah siap.
                Tick += OnStartupLoadingTick;
            }
            catch (Exception ex)
            {
                LogError(
                    "STARTUP NAME ERROR",
                    ex
                );

                // Jika ada error pada UI input, jangan lanjutkan gameplay setengah jadi.
                try { Function.Call(Hash.SET_TIME_SCALE, 1.0f); } catch { }
                _isRunning = false;
            }
        }

        private string NormalizeConfigurableRandomEffectKey(
            string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                return string.Empty;

            string normalized =
                rawKey
                    .Trim()
                    .TrimStart('/')
                    .ToLowerInvariant()
                    .Replace(" ", "_")
                    .Replace("-", "_");

            while (normalized.Contains("__"))
            {
                normalized =
                    normalized.Replace("__", "_");
            }

            // Alias manusia / compatibility.
            switch (normalized)
            {
                case "transformanimal":
                case "animal":
                case "transform":
                    normalized = "transform_animal";
                    break;

                case "black_hole":
                    normalized = "blackhole";
                    break;

                case "wanted5star":
                case "wanted_5star":
                case "wanted5_star":
                case "wanted":
                    normalized = "wanted_5_star";
                    break;

                case "go_to_finish":
                case "finish":
                    normalized = "goto_finish";
                    break;

                case "give_health_armor":
                    normalized = "give_health";
                    break;
            }

            switch (normalized)
            {
                case "flip":
                case "falling_vehicles":
                case "give_vehicle":
                case "destroy_car":
                case "cage":
                case "disarm":
                case "sleep":
                case "ball_rain":
                case "transform_animal":
                case "superspeed":
                case "invincible":
                case "random_teleport":
                case "blackhole":
                case "back_to_start":
                case "goto_finish":
                case "wanted_5_star":
                case "teleport_sky":
                case "earthquake":
                case "hit_by_vehicle":
                case "u_turn":
                case "get_towed":
                case "traffic_magnet":
                case "reckless_traffic":
                case "vehicle_fire_timer":
                case "burning":
                case "remove_tire":
                case "police_roadblock":
                case "random_explosion_nearby":
                case "give_health":
                    return normalized;

                default:
                    return string.Empty;
            }
        }

        private string GetConfigurableRandomEffectDisplayName(
            string rawKey)
        {
            string key =
                NormalizeConfigurableRandomEffectKey(
                    rawKey
                );

            switch (key)
            {
                case "flip":
                    return "FLIP";
                case "falling_vehicles":
                    return "FALLING VEHICLES";
                case "give_vehicle":
                    return "GIVE VEHICLE";
                case "destroy_car":
                    return "DESTROY CAR";
                case "cage":
                    return "CAGE";
                case "disarm":
                    return "DISARM";
                case "sleep":
                    return "SLEEP";
                case "ball_rain":
                    return "BALL RAIN";
                case "transform_animal":
                    return "TRANSFORM ANIMAL";
                case "superspeed":
                    return "SUPERSPEED";
                case "invincible":
                    return "INVINCIBLE";
                case "random_teleport":
                    return "RANDOM TELEPORT";
                case "blackhole":
                    return "BLACKHOLE";
                case "back_to_start":
                    return "BACK TO START";
                case "goto_finish":
                    return "GO TO FINISH";
                case "wanted_5_star":
                    return "WANTED 5 STAR";
                case "teleport_sky":
                    return "TELEPORT SKY";
                case "earthquake":
                    return "EARTHQUAKE";
                case "hit_by_vehicle":
                    return "HIT BY VEHICLE";
                case "u_turn":
                    return "U TURN";
                case "get_towed":
                    return "GET TOWED";
                case "traffic_magnet":
                    return "TRAFFIC MAGNET";
                case "reckless_traffic":
                    return "RECKLESS TRAFFIC";
                case "vehicle_fire_timer":
                    return "VEHICLE FIRE TIMER";
                case "burning":
                    return "BURNING";
                case "remove_tire":
                    return "REMOVE TIRE";
                case "police_roadblock":
                    return "POLICE ROADBLOCK";
                case "random_explosion_nearby":
                    return "RANDOM EXPLOSION NEARBY";
                case "give_health":
                    return "GIVE HEALTH";
                default:
                    return "UNKNOWN";
            }
        }

        private bool IsTimedConfigurableRandomEffect(
            string rawKey)
        {
            string key =
                NormalizeConfigurableRandomEffectKey(
                    rawKey
                );

            return
                !string.IsNullOrEmpty(key) &&
                GetDefaultEffectDurationMs(key) > 0;
        }

        private void ExecuteConfigurableRandomEffect(
            string rawKey)
        {
            string key =
                NormalizeConfigurableRandomEffectKey(
                    rawKey
                );

            switch (key)
            {
                case "flip":
                    ShowInstantGiftHud(
                        _instantPlayerFlipLabel,
                        false
                    );
                    ActivatePlayerFlip();
                    break;

                case "falling_vehicles":
                    TriggerFallingVehicles();
                    break;

                case "give_vehicle":
                    TriggerGiveVehicle();
                    break;

                case "destroy_car":
                    ShowInstantGiftHud(
                        _instantDestroyCarLabel,
                        false
                    );
                    ActivateDestroyCarGift();
                    break;

                case "cage":
                    ActivateGiftCage();
                    break;

                case "disarm":
                    ActivateGiftDisarm();
                    break;

                case "sleep":
                    ActivateSleepGift();
                    break;

                case "ball_rain":
                    ActivateRockRainGift();
                    break;

                case "transform_animal":
                    TriggerPlayerAnimalTransform();
                    break;

                case "superspeed":
                    ActivateSuperSpeed();
                    break;

                case "invincible":
                    ActivateInvincible();
                    break;

                case "random_teleport":
                    ShowInstantGiftHud(
                        _instantRandomTeleportLabel,
                        false
                    );
                    StartRandomTeleport();
                    break;

                case "blackhole":
                    ActivateBlackhole();
                    break;

                case "back_to_start":
                    ShowInstantGiftHud(
                        _instantBackToStartLabel,
                        false
                    );
                    TeleportToStart(
                        isFromGift: true
                    );
                    break;

                case "goto_finish":
                    ShowInstantGiftHud(
                        _instantGoToFinishLabel,
                        true
                    );
                    TeleportToFinish(
                        false
                    );
                    break;

                case "wanted_5_star":
                    ActivateWantedFiveStarGift();
                    break;

                case "teleport_sky":
                    ShowInstantGiftHud(
                        _instantTeleportSkyLabel,
                        false
                    );
                    ActivateTeleportSkyGift();
                    break;

                case "earthquake":
                    ActivateEarthquakeGift();
                    break;

                case "hit_by_vehicle":
                    ShowInstantGiftHud(
                        _instantHitByVehicleLabel,
                        false
                    );
                    TriggerHitByVehicle();
                    break;

                case "u_turn":
                    ShowInstantGiftHud(
                        _instantUTurnLabel,
                        false
                    );
                    ActivateUTurnWebhook();
                    break;

                case "get_towed":
                    ActivateGetTowedWebhook();
                    break;

                case "traffic_magnet":
                    ActivateTrafficMagnetWebhook();
                    break;

                case "reckless_traffic":
                    ActivateRecklessTrafficWebhook();
                    break;

                case "vehicle_fire_timer":
                    ActivateVehicleFireTimerWebhook();
                    break;

                case "burning":
                    ActivateBurningWebhook();
                    break;

                case "remove_tire":
                    ActivateRemoveTireWebhook();
                    break;

                case "police_roadblock":
                    ActivatePoliceRoadblockWebhook();
                    break;

                case "random_explosion_nearby":
                    ActivateRandomExplosionNearbyWebhook();
                    break;

                case "give_health":
                    ActivateGiveHealthWebhook();
                    break;
            }
        }

        private void StopConfigurableRandomEffect(
            string rawKey)
        {
            string key =
                NormalizeConfigurableRandomEffectKey(
                    rawKey
                );

            Ped player =
                Game.Player.Character;

            switch (key)
            {
                case "falling_vehicles":
                    for (int i = _modelsToSpawn.Count - 1; i >= 0; i--)
                    {
                        ModelLoadingTracker tracker =
                            _modelsToSpawn[i];

                        if (tracker.Model.IsValid)
                        {
                            tracker.Model.MarkAsNoLongerNeeded();
                        }
                    }

                    _modelsToSpawn.Clear();

                    for (
                        int i =
                            _activeFallingVehicles.Count - 1;
                        i >= 0;
                        i--)
                    {
                        FallingVehicleActiveTracker tracker =
                            _activeFallingVehicles[i];

                        try
                        {
                            if (tracker != null &&
                                tracker.Vehicle != null &&
                                tracker.Vehicle.Exists())
                            {
                                tracker.Vehicle.Delete();
                            }
                        }
                        catch
                        {
                        }
                    }

                    _activeFallingVehicles.Clear();
                    _fallingVehiclesEffectEndTime = 0;
                    _nextFallingVehicleActualSpawnTime = 0;
                    break;

                case "cage":
                    CleanupGiftCage();
                    break;

                case "disarm":
                    _giftDisarmEndTime = 0;
                    RestoreChaosNoWeapon();
                    break;

                case "sleep":
                    _giftSleepEndTime = 0;
                    _giftSleepVehicleBailoutPending = false;
                    _giftSleepVehicleBailoutRetryTime = 0;

                    if (player != null &&
                        player.Exists() &&
                        !_customDeathActive)
                    {
                        Function.Call(
                            Hash.CLEAR_PED_TASKS,
                            player.Handle
                        );
                    }
                    break;

                case "ball_rain":
                    StopRockRainGift();
                    break;

                case "transform_animal":
                    if (_playerAnimalTransformPhase == 1)
                    {
                        if (_playerAnimalPendingModel.IsValid)
                        {
                            _playerAnimalPendingModel.MarkAsNoLongerNeeded();
                        }

                        ClearPlayerAnimalTransformState();
                    }
                    else if (_playerAnimalTransformPhase == 2)
                    {
                        _playerAnimalTransformEndTime =
                            Game.GameTime;
                    }
                    break;

                case "superspeed":
                    ResetSuperSpeed();
                    break;

                case "invincible":
                    ResetInvincible();
                    break;

                case "blackhole":
                    _pendingBlackholeDurationMs = 0;
                    _blackholeEndTime = 0;
                    CleanupBlackHoleProp();
                    break;

                case "wanted_5_star":
                    if (_wantedFiveGiftActive)
                    {
                        ResetWantedAfterCustomDeath();
                    }
                    break;

                case "earthquake":
                    _pendingEarthquakeDurationMs = 0;
                    StopEarthquakeGift();
                    break;

                case "get_towed":
                    CleanupGetTowedEffect();
                    break;

                case "traffic_magnet":
                    _trafficMagnetEndTime = 0;
                    _nextTrafficMagnetUpdateTime = 0;
                    break;

                case "reckless_traffic":
                    RestoreRecklessTrafficVehicles();
                    _recklessTrafficEndTime = 0;
                    _nextRecklessTrafficUpdateTime = 0;
                    _nextRecklessTrafficNitroTime = 0;
                    break;

                case "vehicle_fire_timer":
                    ResetVehicleFireTimerEffect();
                    break;

                case "burning":
                    ResetBurningEffect();
                    break;

                case "police_roadblock":
                    _policeRoadblockEndTime = 0;
                    _nextPoliceRoadblockSpawnTime = 0;
                    CleanupPoliceRoadblockVehicles();
                    break;

                case "random_explosion_nearby":
                    _randomExplosionEndTime = 0;
                    _nextRandomExplosionTime = 0;
                    _randomExplosionSpawnedCount = 0;
                    break;

                    // One-shot effects tidak punya state timer untuk di-reset:
                    // flip, give_vehicle, destroy_car, random_teleport,
                    // back_to_start, goto_finish, teleport_sky,
                    // hit_by_vehicle, u_turn, remove_tire, give_health.
            }
        }

        private string NormalizeRandomChaosKey(
            string value)
        {
            return
                NormalizeConfigurableRandomEffectKey(
                    value
                );
        }

        private string GetRandomChaosDisplayName(
            string key)
        {
            return
                GetConfigurableRandomEffectDisplayName(
                    key
                );
        }

        private int GetModeEffectDuration(
            int configuredDurationMs)
        {
            return
                _modeDurationOverrideMs > 0
                    ? _modeDurationOverrideMs
                    : configuredDurationMs;
        }

        private int GetDefaultEffectDurationMs(
            string effectKey)
        {
            string key =
                (effectKey ?? string.Empty)
                    .Trim()
                    .TrimStart('/')
                    .ToLowerInvariant();

            switch (key)
            {
                case "cage":
                    return _giftCageDurationMs;

                case "sleep":
                    return _giftSleepDurationMs;

                case "ball_rain":
                    return _giftRockRainDurationMs;

                case "falling_vehicles":
                    return _giftFallingVehiclesDurationMs;

                case "disarm":
                    return _giftDisarmDurationMs;

                case "superspeed":
                    return _giftSuperSpeedDurationMs;

                case "invincible":
                    return _giftInvincibleDurationMs;

                case "transform_animal":
                    return _giftAnimalTransformDurationMs;

                case "blackhole":
                    return _giftBlackholeDurationMs;

                case "wanted_5_star":
                    return _giftWantedFiveStarDurationMs;

                case "earthquake":
                    return _giftEarthquakeDurationMs;

                case "get_towed":
                    return _getTowedDurationMs;

                case "traffic_magnet":
                    return _trafficMagnetDurationMs;

                case "reckless_traffic":
                    return _recklessTrafficDurationMs;

                case "vehicle_fire_timer":
                    return _vehicleFireTimerDurationMs;

                case "burning":
                    return _burningDurationMs;

                case "police_roadblock":
                    return _policeRoadblockDurationMs;

                case "random_explosion_nearby":
                    return _randomExplosionDurationMs;

                // One-shot: tidak mempunyai timer effect.
                case "teleport_sky":
                    return 0;

                default:
                    return 0;
            }
        }

        private int ResolveConfiguredEffectDurationMs(
            string effectKey,
            int configuredDurationMs)
        {
            return
                configuredDurationMs > 0
                    ? configuredDurationMs
                    : GetDefaultEffectDurationMs(effectKey);
        }

        private bool IsActiveRandomChaosEffect(
            string key)
        {
            return
                _isChaosActive &&
                !string.IsNullOrEmpty(
                    _activeChaosKey
                ) &&
                _activeChaosKey.Equals(
                    key,
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private string NormalizeRandomCheckpointEffectKey(
            string rawKey)
        {
            return
                NormalizeConfigurableRandomEffectKey(
                    rawKey
                );
        }

        private string GetRandomCheckpointEffectDisplayName(
            string effectKey)
        {
            string normalized =
                NormalizeConfigurableRandomEffectKey(
                    effectKey
                );

            return
                string.IsNullOrEmpty(normalized)
                    ? "NO EFFECT LEFT"
                    : GetConfigurableRandomEffectDisplayName(
                        normalized
                    );
        }

        private int ClampEntityLimit(
            int value,
            int fallback)
        {
            if (value <= 0)
                return fallback;

            return Math.Max(
                1,
                Math.Min(
                    200,
                    value
                )
            );
        }

        // =========================================================
        // GO TO MOUNTAIN - HARD-CODED ORDERED ROUTE
        // =========================================================
        // Tidak memakai [ROUTE_DISTANCE]. Jumlah POS langsung membagi jalur
        // START -> FINISH menjadi N+1 bagian. Karena itu:
        //   5 POS  = slot lebih renggang
        //   10 POS = lebih rapat
        //   15 POS = makin rapat
        //   20 POS = paling rapat
        // POS 1 selalu berada di bagian awal rute dan POS terakhir selalu
        // berada di bagian akhir sebelum FINISH. Random hanya jitter kecil
        // lalu titik di-snap ke street/vehicle road node GTA.
        // =========================================================
        private Vector3 GetGoToMountainOrderedTarget(
            int checkpointIndex,
            int totalCheckpoints,
            Vector3 routeFinishPosition,
            float longitudinalOffset,
            float lateralOffset)
        {
            if (totalCheckpoints <= 0 ||
                _missionStartPosition == Vector3.Zero ||
                routeFinishPosition == Vector3.Zero)
            {
                return Vector3.Zero;
            }

            float routeDx = routeFinishPosition.X - _missionStartPosition.X;
            float routeDy = routeFinishPosition.Y - _missionStartPosition.Y;
            float routeLength2D = (float)Math.Sqrt(
                (routeDx * routeDx) + (routeDy * routeDy));

            if (routeLength2D < 1.0f)
                return Vector3.Zero;

            float dirX = routeDx / routeLength2D;
            float dirY = routeDy / routeLength2D;
            float perpX = -dirY;
            float perpY = dirX;

            float progressFraction =
                (checkpointIndex + 1.0f) /
                (totalCheckpoints + 1.0f);

            float idealX = _missionStartPosition.X +
                (routeDx * progressFraction);
            float idealY = _missionStartPosition.Y +
                (routeDy * progressFraction);

            return new Vector3(
                idealX + (dirX * longitudinalOffset) + (perpX * lateralOffset),
                idealY + (dirY * longitudinalOffset) + (perpY * lateralOffset),
                500.0f);
        }

        private float GetGoToMountainOrderedProgress(
            Vector3 position,
            Vector3 routeFinishPosition)
        {
            if (position == Vector3.Zero ||
                routeFinishPosition == Vector3.Zero)
            {
                return 0.0f;
            }

            float routeDx = routeFinishPosition.X - _missionStartPosition.X;
            float routeDy = routeFinishPosition.Y - _missionStartPosition.Y;
            float routeLength2D = (float)Math.Sqrt(
                (routeDx * routeDx) + (routeDy * routeDy));

            if (routeLength2D < 1.0f)
                return 0.0f;

            float dirX = routeDx / routeLength2D;
            float dirY = routeDy / routeLength2D;

            return
                ((position.X - _missionStartPosition.X) * dirX) +
                ((position.Y - _missionStartPosition.Y) * dirY);
        }

        private bool IsGoToMountainOrderedCandidateValid(
            Vector3 candidate,
            Vector3 fromPos,
            int checkpointIndex,
            int totalCheckpoints,
            List<Vector3> usedPoints,
            Vector3 routeFinishPosition,
            float idealSpacing,
            float targetProgress)
        {
            if (candidate == Vector3.Zero)
                return false;

            float previousProgress = GetGoToMountainOrderedProgress(
                fromPos,
                routeFinishPosition);
            float candidateProgress = GetGoToMountainOrderedProgress(
                candidate,
                routeFinishPosition);

            // POS harus maju sesuai urutan. Tidak ada random-walk / mundur.
            float minimumForwardProgress = Math.Max(20.0f, idealSpacing * 0.08f);
            if (candidateProgress <= previousProgress + minimumForwardProgress)
                return false;

            // Jangan biarkan checkpoint terakhir melewati FINISH.
            float routeDx = routeFinishPosition.X - _missionStartPosition.X;
            float routeDy = routeFinishPosition.Y - _missionStartPosition.Y;
            float routeLength2D = (float)Math.Sqrt(
                (routeDx * routeDx) + (routeDy * routeDy));
            if (candidateProgress >= routeLength2D - Math.Max(60.0f, idealSpacing * 0.10f))
                return false;

            // Road snap boleh melenceng, tetapi tetap harus berada di slot POS-nya.
            if (Math.Abs(candidateProgress - targetProgress) >
                Math.Max(250.0f, idealSpacing * 0.70f))
            {
                return false;
            }

            if (!IsRoutePointFarEnough(
                    candidate,
                    usedPoints,
                    Math.Max(35.0f, idealSpacing * 0.06f)))
            {
                return false;
            }

            return true;
        }

        // =========================================================
        // GO TO MOUNTAIN - FORWARD ROAD CORRIDOR
        // =========================================================
        // Berbeda dari Survival:
        // - Survival memilih titik random dalam annulus.
        // - GoToMountain selalu punya FINISH tetap dan setiap POS harus
        //   benar-benar membuat progress mendekati FINISH.
        //
        // Kita tidak lagi memaksa road node berada di "slot garis lurus"
        // START -> FINISH. Jalan GTA dapat berbelok jauh dari garis lurus,
        // terutama menuju Chiliad. Memaksa progress slot terlalu ketat dapat
        // membuat satu POS tidak pernah ditemukan dan LOADING ROUTE menggantung.
        // =========================================================
        private Vector3 GetGoToMountainOrderedRoutePosition(
            Vector3 fromPos,
            int checkpointIndex,
            int totalCheckpoints,
            List<Vector3> usedPoints,
            Vector3 routeFinishPosition,
            int searchTier = 0)
        {
            if (fromPos == Vector3.Zero ||
                routeFinishPosition == Vector3.Zero ||
                totalCheckpoints <= 0 ||
                checkpointIndex < 0 ||
                checkpointIndex >= totalCheckpoints)
            {
                return Vector3.Zero;
            }

            float toFinishX = routeFinishPosition.X - fromPos.X;
            float toFinishY = routeFinishPosition.Y - fromPos.Y;
            float currentDistanceToFinish = (float)Math.Sqrt(
                (toFinishX * toFinishX) +
                (toFinishY * toFinishY));

            if (currentDistanceToFinish < 10.0f)
                return Vector3.Zero;

            // Jumlah leg yang masih tersisa:
            // POS saat ini + POS berikutnya + 1 leg terakhir ke FINISH.
            int remainingLegs =
                Math.Max(
                    1,
                    (totalCheckpoints - checkpointIndex) + 1
                );

            float idealLegDistance =
                currentDistanceToFinish /
                remainingLegs;

            float baseHeading =
                (float)Math.Atan2(
                    toFinishY,
                    toFinishX
                );

            // Jika seluruh route harus diulang, beri bias corridor kecil supaya
            // builder tidak memilih cabang jalan yang persis sama lagi.
            int corridorAttemptIndex =
                Math.Max(
                    0,
                    _pendingRouteBuildWholeAttempts - 1
                );

            float corridorBiasDeg = 0.0f;

            switch (corridorAttemptIndex % 3)
            {
                case 1:
                    corridorBiasDeg = 12.0f;
                    break;

                case 2:
                    corridorBiasDeg = -12.0f;
                    break;
            }

            baseHeading +=
                corridorBiasDeg *
                ((float)Math.PI / 180.0f);

            // Tier 0 = corridor normal.
            // Tier 1 = corridor diperlebar.
            // Tier 2 = recovery corridor untuk area road sulit / pegunungan.
            float[] radiusMultipliers;
            float[] angleOffsetsDeg;
            float minimumGain;
            float minimumUsedSeparation;

            if (searchTier <= 0)
            {
                radiusMultipliers =
                    new float[]
                    {
                        0.80f,
                        1.00f,
                        1.20f
                    };

                angleOffsetsDeg =
                    new float[]
                    {
                        -30.0f,
                        0.0f,
                        30.0f
                    };

                minimumGain =
                    Math.Max(
                        20.0f,
                        idealLegDistance * 0.06f
                    );

                minimumUsedSeparation =
                    Math.Max(
                        35.0f,
                        idealLegDistance * 0.05f
                    );
            }
            else if (searchTier == 1)
            {
                radiusMultipliers =
                    new float[]
                    {
                        0.60f,
                        0.85f,
                        1.10f,
                        1.40f
                    };

                angleOffsetsDeg =
                    new float[]
                    {
                        -60.0f,
                        -30.0f,
                        0.0f,
                        30.0f,
                        60.0f
                    };

                minimumGain =
                    Math.Max(
                        10.0f,
                        idealLegDistance * 0.03f
                    );

                minimumUsedSeparation =
                    Math.Max(
                        24.0f,
                        idealLegDistance * 0.035f
                    );
            }
            else
            {
                radiusMultipliers =
                    new float[]
                    {
                        0.40f,
                        0.65f,
                        0.90f,
                        1.20f,
                        1.55f
                    };

                angleOffsetsDeg =
                    new float[]
                    {
                        -90.0f,
                        -60.0f,
                        -35.0f,
                        0.0f,
                        35.0f,
                        60.0f,
                        90.0f
                    };

                // Recovery tier hanya mensyaratkan bahwa POS benar-benar
                // maju menuju FINISH dan bukan mengulang titik lama.
                minimumGain = 4.0f;
                minimumUsedSeparation = 15.0f;
            }

            Vector3 bestCandidate =
                Vector3.Zero;

            float bestScore =
                float.MaxValue;

            float desiredRemainingDistance =
                Math.Max(
                    0.0f,
                    currentDistanceToFinish -
                    idealLegDistance
                );

            foreach (float radiusMultiplier in radiusMultipliers)
            {
                float searchRadius =
                    Math.Max(
                        80.0f,
                        idealLegDistance *
                        radiusMultiplier
                    );

                foreach (float angleOffsetDeg in angleOffsetsDeg)
                {
                    float angle =
                        baseHeading +
                        (angleOffsetDeg *
                         ((float)Math.PI / 180.0f));

                    Vector3 searchTarget =
                        new Vector3(
                            fromPos.X +
                            ((float)Math.Cos(angle) *
                             searchRadius),
                            fromPos.Y +
                            ((float)Math.Sin(angle) *
                             searchRadius),
                            500.0f
                        );

                    // Warm-up area kandidat. Ini aman walaupun collision
                    // belum langsung tersedia pada Tick yang sama.
                    Function.Call(
                        Hash.REQUEST_COLLISION_AT_COORD,
                        searchTarget.X,
                        searchTarget.Y,
                        searchTarget.Z
                    );

                    Vector3 roadPosition =
                        GetRoadRoutePosition(
                            searchTarget
                        );

                    if (roadPosition ==
                        Vector3.Zero)
                    {
                        continue;
                    }

                    float finishDx =
                        routeFinishPosition.X -
                        roadPosition.X;

                    float finishDy =
                        routeFinishPosition.Y -
                        roadPosition.Y;

                    float candidateDistanceToFinish =
                        (float)Math.Sqrt(
                            (finishDx * finishDx) +
                            (finishDy * finishDy)
                        );

                    // Kunci utama GoToMountain:
                    // POS berikutnya wajib lebih dekat ke FINISH.
                    if (candidateDistanceToFinish >
                        currentDistanceToFinish -
                        minimumGain)
                    {
                        continue;
                    }

                    // Sisakan FINISH sebagai tujuan terpisah.
                    if (candidateDistanceToFinish <
                        12.0f)
                    {
                        continue;
                    }

                    if (!IsRoutePointFarEnough(
                            roadPosition,
                            usedPoints,
                            minimumUsedSeparation))
                    {
                        continue;
                    }

                    // Pilih kandidat yang paling dekat dengan panjang leg ideal.
                    // Sedikit penalti sudut menjaga route cenderung maju ke depan,
                    // tetapi jalan tetap boleh berbelok jauh bila memang dibutuhkan.
                    float score =
                        Math.Abs(
                            candidateDistanceToFinish -
                            desiredRemainingDistance
                        ) +
                        (Math.Abs(angleOffsetDeg) *
                         Math.Max(
                             0.20f,
                             idealLegDistance * 0.0006f
                         ));

                    if (score <
                        bestScore)
                    {
                        bestScore = score;
                        bestCandidate = roadPosition;
                    }
                }
            }

            if (bestCandidate !=
                Vector3.Zero)
            {
                Function.Call(
                    Hash.REQUEST_COLLISION_AT_COORD,
                    bestCandidate.X,
                    bestCandidate.Y,
                    bestCandidate.Z
                );

                return bestCandidate;
            }

            return Vector3.Zero;
        }

        // =========================================================
        // GO TO MOUNTAIN - LAST RESORT ROAD SEARCH
        // =========================================================
        // Dipakai hanya setelah beberapa corridor search gagal.
        // Tetap HARUS vehicle-road node dan tetap HARUS maju ke FINISH,
        // tetapi tidak memaksa panjang leg ideal. Jadi tidak ada fallback
        // raw ground / tanah kosong hanya demi menyelesaikan loading.
        // =========================================================
        private Vector3 GetGoToMountainEmergencyRoadPosition(
            Vector3 fromPos,
            int checkpointIndex,
            int totalCheckpoints,
            List<Vector3> usedPoints,
            Vector3 routeFinishPosition)
        {
            if (fromPos == Vector3.Zero ||
                routeFinishPosition == Vector3.Zero ||
                totalCheckpoints <= 0)
            {
                return Vector3.Zero;
            }

            float currentDx =
                routeFinishPosition.X -
                fromPos.X;

            float currentDy =
                routeFinishPosition.Y -
                fromPos.Y;

            float currentDistance =
                (float)Math.Sqrt(
                    (currentDx * currentDx) +
                    (currentDy * currentDy)
                );

            if (currentDistance <
                10.0f)
            {
                return Vector3.Zero;
            }

            int remainingLegs =
                Math.Max(
                    1,
                    (totalCheckpoints - checkpointIndex) + 1
                );

            float baseStep =
                Math.Max(
                    120.0f,
                    currentDistance /
                    remainingLegs
                );

            float baseHeading =
                (float)Math.Atan2(
                    currentDy,
                    currentDx
                );

            float[] radiusMultipliers =
                new float[]
                {
                    0.30f,
                    0.50f,
                    0.75f,
                    1.00f,
                    1.35f,
                    1.75f,
                    2.20f
                };

            float[] angleOffsetsDeg =
                new float[]
                {
                    -120.0f,
                    -90.0f,
                    -60.0f,
                    -30.0f,
                    0.0f,
                    30.0f,
                    60.0f,
                    90.0f,
                    120.0f
                };

            Vector3 bestCandidate =
                Vector3.Zero;

            float desiredRemainingDistance =
                Math.Max(
                    0.0f,
                    currentDistance -
                    baseStep
                );

            float bestScore =
                float.MaxValue;

            foreach (float radiusMultiplier in radiusMultipliers)
            {
                float radius =
                    Math.Max(
                        75.0f,
                        baseStep *
                        radiusMultiplier
                    );

                foreach (float angleOffsetDeg in angleOffsetsDeg)
                {
                    float angle =
                        baseHeading +
                        (angleOffsetDeg *
                         ((float)Math.PI / 180.0f));

                    Vector3 searchTarget =
                        new Vector3(
                            fromPos.X +
                            ((float)Math.Cos(angle) *
                             radius),
                            fromPos.Y +
                            ((float)Math.Sin(angle) *
                             radius),
                            500.0f
                        );

                    Function.Call(
                        Hash.REQUEST_COLLISION_AT_COORD,
                        searchTarget.X,
                        searchTarget.Y,
                        searchTarget.Z
                    );

                    Vector3 roadPosition =
                        GetRoadRoutePosition(
                            searchTarget
                        );

                    if (roadPosition ==
                        Vector3.Zero)
                    {
                        continue;
                    }

                    if (!IsRoutePointFarEnough(
                            roadPosition,
                            usedPoints,
                            10.0f))
                    {
                        continue;
                    }

                    float finishDx =
                        routeFinishPosition.X -
                        roadPosition.X;

                    float finishDy =
                        routeFinishPosition.Y -
                        roadPosition.Y;

                    float distanceToFinish =
                        (float)Math.Sqrt(
                            (finishDx * finishDx) +
                            (finishDy * finishDy)
                        );

                    // Tetap wajib maju. Bahkan emergency tidak boleh
                    // menghasilkan POS yang menjauh dari gunung.
                    if (distanceToFinish >=
                        currentDistance - 1.0f)
                    {
                        continue;
                    }

                    if (distanceToFinish <
                        8.0f)
                    {
                        continue;
                    }

                    float score =
                        Math.Abs(
                            distanceToFinish -
                            desiredRemainingDistance
                        );

                    if (score <
                        bestScore)
                    {
                        bestScore = score;

                        bestCandidate =
                            roadPosition;
                    }
                }
            }

            if (bestCandidate !=
                Vector3.Zero)
            {
                Function.Call(
                    Hash.REQUEST_COLLISION_AT_COORD,
                    bestCandidate.X,
                    bestCandidate.Y,
                    bestCandidate.Z
                );
            }

            return bestCandidate;
        }

        private bool GenerateGoToMountainRoute()
        {
            LoadGameplayConfigFromIni(
                applyRuntimeChanges: false
            );

            _activePosList.Clear();

            _currentPosIndex =
                0;

            _isInsideCheckpoint =
                false;

            _checkpointTouchStartTime =
                0;

            // =========================================================
            // DIRECT MODE - PENGECUALIAN ROAD SNAP
            // =========================================================
            // GoToMountain=true + GotoMountainPos=false
            // sengaja tetap memakai FINISH_POS persis dari INI karena
            // gameplay hanya START -> FINISH.
            // =========================================================
            if (!_missionGoToMountainPosEnabled)
            {
                _activeFinishPosition =
                    _missionFinishPosition;

                UpdateCheckpointBlip();
                return true;
            }

            // =========================================================
            // POS MODE - FINISH PAKAI KOORDINAT INI PERSIS
            // =========================================================
            // GotoMountainPos=true tetap membuat POS 1..N pada road/street node,
            // tetapi FINISH TIDAK di-snap ke jalan. Ini penting untuk FINISH
            // khusus seperti puncak gunung, rooftop, stage, dll.
            //
            // Dengan ini ronde pertama dan ronde berikutnya selalu memakai
            // FINISH_POS yang sama persis dari [MISSION].
            // =========================================================
            Vector3 routeFinishPosition =
                _missionFinishPosition;

            if (routeFinishPosition ==
                Vector3.Zero)
            {
                _activeFinishPosition =
                    Vector3.Zero;

                return false;
            }

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                routeFinishPosition.X,
                routeFinishPosition.Y,
                routeFinishPosition.Z
            );

            _activeFinishPosition =
                routeFinishPosition;

            // =========================================================
            // ORDERED POS MODE - START -> MOUNTAIN
            // =========================================================
            int totalCheckpoints =
                Math.Max(
                    MIN_ROUTE_CHECKPOINT_COUNT,
                    Math.Min(
                        MAX_ROUTE_CHECKPOINT_COUNT,
                        _routeCheckpointCount
                    )
                );

            List<Vector3> generatedCheckpoints =
                new List<Vector3>();

            List<Vector3> usedPoints =
                new List<Vector3>();

            usedPoints.Add(_missionStartPosition);
            usedPoints.Add(routeFinishPosition);

            Vector3 reference = _missionStartPosition;

            for (int i = 0; i < totalCheckpoints; i++)
            {
                Vector3 checkpoint = Vector3.Zero;

                // Synchronous compatibility path:
                // coba corridor normal -> wide -> recovery -> emergency.
                for (int tier = 0;
                     tier <= 2 && checkpoint == Vector3.Zero;
                     tier++)
                {
                    checkpoint = GetGoToMountainOrderedRoutePosition(
                        reference,
                        i,
                        totalCheckpoints,
                        usedPoints,
                        routeFinishPosition,
                        tier);
                }

                if (checkpoint == Vector3.Zero)
                {
                    checkpoint = GetGoToMountainEmergencyRoadPosition(
                        reference,
                        i,
                        totalCheckpoints,
                        usedPoints,
                        routeFinishPosition);
                }

                if (checkpoint == Vector3.Zero)
                {
                    _activePosList.Clear();
                    _activeFinishPosition = Vector3.Zero;
                    return false;
                }

                generatedCheckpoints.Add(checkpoint);
                usedPoints.Add(checkpoint);
                reference = checkpoint;
            }

            _activePosList.Clear();
            _activePosList.AddRange(generatedCheckpoints);
            _activeFinishPosition = routeFinishPosition;
            _currentPosIndex = 0;
            _isInsideCheckpoint = false;
            _checkpointTouchStartTime = 0;

            if (generatedCheckpoints.Count > 0)
                _lastGeneratedFirstPos = generatedCheckpoints[0];

            UpdateCheckpointBlip();
            return true;
        }

        private bool GenerateRouteForCurrentMode()
        {
            bool generated;

            if (_selectedGameplayMode == MoonGameplayMode.Survival)
            {
                generated = GenerateSurvivalRoute();
            }
            else if (_missionGoToMountainEnabled)
            {
                generated = GenerateGoToMountainRoute();
            }
            else
            {
                generated = GenerateRandomRoute();
            }

            if (generated)
            {
                _randomCheckpointRouteVersion++;
                ResetRandomCheckpointProgressTracking();
            }

            return generated;
        }

        private void ProcessInitialRouteSetup()
        {
            if (!_initialRouteSetupPending)
                return;

            if (Game.GameTime < _nextInitialRouteSetupAttemptTime)
                return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
                return;

            _isInsideCheckpoint = false;
            _checkpointTouchStartTime = 0;

            if (_selectedGameplayMode == MoonGameplayMode.Survival)
            {
                if (!PrepareSurvivalRandomStart(player))
                {
                    // Jangan spam pencarian random START setiap Tick saat path nodes
                    // belum siap pada fresh launch. Coba lagi setelah jeda pendek.
                    _nextInitialRouteSetupAttemptTime =
                        Game.GameTime + ROUTE_GENERATION_RETRY_MS;
                    return;
                }

                _nextInitialRouteSetupAttemptTime = 0;
                _initialRouteSetupPending = false;
                ScheduleRouteGeneration(ROUTE_GENERATION_STREAM_DELAY_MS);
                ShowHudNotification(
                    _selectedSurvivalDirectFinish
                        ? "~g~[SURVIVAL] ~w~Random START -> DIRECT FINISH LINE"
                        : "~g~[SURVIVAL] ~w~Random START -> POS -> FINISH"
                );
                return;
            }

            if (_missionGoToMountainEnabled)
            {
                _startPosition = _missionStartPosition;
                Entity target = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD,
                    _missionStartPosition.X, _missionStartPosition.Y, _missionStartPosition.Z);
                target.Position = _missionStartPosition;

                if (player.IsInVehicle())
                {
                    Vehicle vehicle = player.CurrentVehicle;
                    if (vehicle != null && vehicle.Exists())
                        vehicle.PlaceOnGround();
                }

                _nextInitialRouteSetupAttemptTime = 0;
                _initialRouteSetupPending = false;
                ScheduleRouteGeneration(ROUTE_GENERATION_STREAM_DELAY_MS);
                ShowHudNotification(
                    _missionGoToMountainPosEnabled
                        ? "~g~[GO TO MOUNTAIN] ~w~START -> POS -> FINISH"
                        : "~g~[GO TO MOUNTAIN] ~w~START -> FINISH"
                );
                return;
            }

            _startPosition = player.Position;
            _nextInitialRouteSetupAttemptTime = 0;
            _initialRouteSetupPending = false;
            ScheduleRouteGeneration(ROUTE_GENERATION_STREAM_DELAY_MS);
        }

        private void ClearRouteBlipsOnly()
        {
            foreach (Blip blip in _routeBlips)
            {
                if (blip != null &&
                    blip.Exists())
                {
                    blip.Delete();
                }
            }

            _routeBlips.Clear();
        }

        private void ResetIncrementalRouteBuildState()
        {
            _pendingRouteBuildMode = PendingRouteBuildMode.None;
            _pendingRouteBuildInitialized = false;
            _pendingRouteBuildCheckpoints.Clear();
            _pendingRouteBuildUsedPoints.Clear();
            _pendingRouteBuildTotalCheckpoints = 0;
            _pendingRouteBuildCheckpointIndex = 0;
            _pendingRouteBuildCandidateAttempts = 0;
            _pendingRouteBuildWholeAttempts = 0;
            _pendingRouteBuildReference = Vector3.Zero;
            _pendingRouteBuildFinish = Vector3.Zero;
            _pendingRouteBuildMinLegDistance = 0.0f;
            _pendingRouteBuildMaxLegDistance = 0.0f;
            _goToMountainRouteBuildDeadlineTime = 0;
        }

        private void ScheduleRouteGeneration(
            int delayMs)
        {
            _routeGenerationPending = true;

            _nextRouteGenerationAttemptTime =
                Game.GameTime +
                Math.Max(
                    0,
                    delayMs
                );

            _activePosList.Clear();
            _activeFinishPosition = Vector3.Zero;
            _currentPosIndex = 0;

            _isInsideCheckpoint = false;
            _checkpointTouchStartTime = 0;

            ClearRouteBlipsOnly();
            ResetIncrementalRouteBuildState();

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                _startPosition.X,
                _startPosition.Y,
                _startPosition.Z
            );
        }

        private void CompletePendingRouteGeneration()
        {
            _routeGenerationPending = false;
            _randomCheckpointRouteVersion++;
            ResetRandomCheckpointProgressTracking();
            ResetIncrementalRouteBuildState();
        }

        private bool ForceGoToMountainDirectRouteFallback(
            string reason)
        {
            if (!_goToMountainFallbackToDirectOnRouteFail)
                return false;

            // Ubah run aktif menjadi DIRECT.
            _selectedGoToMountainDirect = true;
            _selectedGameplayPosCount = 0;

            _missionGoToMountainEnabled = true;
            _missionGoToMountainPosEnabled = false;
            _routeCheckpointCount = 0;

            // Hapus semua POS parsial yang sempat dibuat.
            _activePosList.Clear();
            _activeFinishPosition = _missionFinishPosition;
            _currentPosIndex = 0;
            _isInsideCheckpoint = false;
            _checkpointTouchStartTime = 0;

            ResetIncrementalRouteBuildState();
            UpdateCheckpointBlip();

            ShowHudNotification(
                "~y~[ROUTE FAILSAFE] ~w~" +
                (string.IsNullOrWhiteSpace(reason)
                    ? "POS route failed"
                    : reason) +
                ". DIRECT TO MOUNTAIN activated."
            );

            return true;
        }

        private void RestartGoToMountainIncrementalAttempt()
        {
            _pendingRouteBuildCheckpoints.Clear();
            _pendingRouteBuildUsedPoints.Clear();

            _pendingRouteBuildReference = _missionStartPosition;
            _pendingRouteBuildCheckpointIndex = 0;
            _pendingRouteBuildCandidateAttempts = 0;
            _pendingRouteBuildWholeAttempts++;

            if (_missionStartPosition != Vector3.Zero)
                _pendingRouteBuildUsedPoints.Add(_missionStartPosition);

            if (_pendingRouteBuildFinish != Vector3.Zero)
                _pendingRouteBuildUsedPoints.Add(_pendingRouteBuildFinish);
        }

        private void InitializeGoToMountainIncrementalRouteBuild()
        {
            _pendingRouteBuildMode = PendingRouteBuildMode.GoToMountain;
            _pendingRouteBuildInitialized = true;
            _pendingRouteBuildTotalCheckpoints = Math.Max(
                MIN_ROUTE_CHECKPOINT_COUNT,
                Math.Min(MAX_ROUTE_CHECKPOINT_COUNT, _routeCheckpointCount));
            _pendingRouteBuildFinish = _missionFinishPosition;

            _goToMountainRouteBuildDeadlineTime =
                unchecked(
                    Game.GameTime +
                    Math.Max(
                        3000,
                        _goToMountainRouteBuildTimeoutMs
                    )
                );

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                _missionFinishPosition.X,
                _missionFinishPosition.Y,
                _missionFinishPosition.Z);

            RestartGoToMountainIncrementalAttempt();
        }

        private bool ProcessGoToMountainIncrementalRouteBuild(
            int currentTime)
        {
            if (!_missionGoToMountainPosEnabled)
            {
                _activePosList.Clear();
                _activeFinishPosition = _missionFinishPosition;
                _currentPosIndex = 0;
                _isInsideCheckpoint = false;
                _checkpointTouchStartTime = 0;
                UpdateCheckpointBlip();
                return true;
            }

            if (_missionStartPosition == Vector3.Zero ||
                _missionFinishPosition == Vector3.Zero)
            {
                _nextRouteGenerationAttemptTime =
                    currentTime + ROUTE_GENERATION_RETRY_MS;
                return false;
            }

            if (!_pendingRouteBuildInitialized ||
                _pendingRouteBuildMode != PendingRouteBuildMode.GoToMountain)
            {
                InitializeGoToMountainIncrementalRouteBuild();
            }

            // Hard timeout: jangan biarkan builder POS menggantung terlalu lama.
            if (_goToMountainFallbackToDirectOnRouteFail &&
                _goToMountainRouteBuildDeadlineTime != 0 &&
                HasGameTimeReached(
                    currentTime,
                    _goToMountainRouteBuildDeadlineTime
                ))
            {
                return ForceGoToMountainDirectRouteFallback(
                    "POS route timeout"
                );
            }

            // GoToMountain sengaja memakai budget kecil sendiri.
            // Survival tetap memakai ROUTE_BUILD_CANDIDATES_PER_TICK,
            // sedangkan GoToMountain mencari road corridor yang lebih kompleks.
            // Maksimal 2 POS dibangun per Tick agar startup tidak stutter.
            const int GO_TO_MOUNTAIN_POS_WORK_PER_TICK = 2;

            for (int work = 0;
                 work < GO_TO_MOUNTAIN_POS_WORK_PER_TICK;
                 work++)
            {
                if (_pendingRouteBuildCheckpointIndex >=
                    _pendingRouteBuildTotalCheckpoints)
                {
                    _activePosList.Clear();
                    _activePosList.AddRange(_pendingRouteBuildCheckpoints);
                    _activeFinishPosition = _pendingRouteBuildFinish;
                    _currentPosIndex = 0;
                    _isInsideCheckpoint = false;
                    _checkpointTouchStartTime = 0;

                    if (_pendingRouteBuildCheckpoints.Count > 0)
                        _lastGeneratedFirstPos = _pendingRouteBuildCheckpoints[0];

                    UpdateCheckpointBlip();
                    return true;
                }

                // Gagal beberapa kali pada POS yang sama tidak lagi mengulang
                // aturan yang sama selamanya. Search tier otomatis melebar.
                int searchTier =
                    Math.Min(
                        2,
                        _pendingRouteBuildCandidateAttempts / 2
                    );

                Vector3 checkpoint =
                    GetGoToMountainOrderedRoutePosition(
                        _pendingRouteBuildReference,
                        _pendingRouteBuildCheckpointIndex,
                        _pendingRouteBuildTotalCheckpoints,
                        _pendingRouteBuildUsedPoints,
                        _pendingRouteBuildFinish,
                        searchTier);

                // Setelah corridor normal/wide/recovery gagal beberapa kali,
                // gunakan emergency ROAD search. Tetap bukan raw ground.
                if (checkpoint == Vector3.Zero &&
                    _pendingRouteBuildCandidateAttempts >= 6)
                {
                    checkpoint =
                        GetGoToMountainEmergencyRoadPosition(
                            _pendingRouteBuildReference,
                            _pendingRouteBuildCheckpointIndex,
                            _pendingRouteBuildTotalCheckpoints,
                            _pendingRouteBuildUsedPoints,
                            _pendingRouteBuildFinish);
                }

                if (checkpoint == Vector3.Zero)
                {
                    _pendingRouteBuildCandidateAttempts++;

                    // Jangan hammer native road-node pada setiap frame.
                    _nextRouteGenerationAttemptTime =
                        currentTime + 120;

                    if (_pendingRouteBuildCandidateAttempts >=
                        Math.Min(
                            ROUTE_BUILD_MAX_POINT_ATTEMPTS,
                            12
                        ))
                    {
                        // Jika satu susunan corridor benar-benar buntu,
                        // mulai ulang route agar POS sebelumnya dapat memilih
                        // cabang jalan yang berbeda.
                        if (_pendingRouteBuildWholeAttempts <
                            ROUTE_BUILD_MAX_WHOLE_ATTEMPTS)
                        {
                            RestartGoToMountainIncrementalAttempt();
                        }
                        else
                        {
                            // Jangan mengulang route selamanya. Setelah seluruh
                            // route attempt habis, langsung turun ke DIRECT.
                            if (ForceGoToMountainDirectRouteFallback(
                                    "POS route generation failed"
                                ))
                            {
                                return true;
                            }

                            // Jika failsafe dimatikan dari INI, pertahankan
                            // behavior retry lama.
                            _pendingRouteBuildWholeAttempts = 0;
                            RestartGoToMountainIncrementalAttempt();

                            _nextRouteGenerationAttemptTime =
                                currentTime +
                                ROUTE_GENERATION_RETRY_MS;
                        }
                    }

                    return false;
                }

                _pendingRouteBuildCandidateAttempts = 0;

                _pendingRouteBuildCheckpoints.Add(checkpoint);
                _pendingRouteBuildUsedPoints.Add(checkpoint);
                _pendingRouteBuildReference = checkpoint;
                _pendingRouteBuildCheckpointIndex++;
            }

            return false;
        }

        private void ProcessPendingRouteGeneration(
            int currentTime)
        {
            if (!_routeGenerationPending)
                return;

            if (currentTime <
                _nextRouteGenerationAttemptTime)
            {
                return;
            }

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                _startPosition.X,
                _startPosition.Y,
                _startPosition.Z
            );

            bool generated = false;

            if (_selectedGameplayMode == MoonGameplayMode.Survival)
            {
                generated = ProcessSurvivalIncrementalRouteBuild(currentTime);
            }
            else if (_missionGoToMountainEnabled)
            {
                generated = ProcessGoToMountainIncrementalRouteBuild(currentTime);
            }
            else
            {
                // Legacy/global mode bukan pilihan utama MoonMod baru.
                // Tetap dipertahankan untuk compatibility.
                generated = GenerateRandomRoute();
            }

            if (generated)
            {
                CompletePendingRouteGeneration();
                return;
            }

            // Incremental builder akan melanjutkan state yang sama pada Tick berikutnya.
            // Untuk builder yang me-reset diri karena gagal total, timestamp retry di atas
            // sudah digeser sehingga tidak memukul frame berulang-ulang.
        }

        private bool IsRoutePointFarEnough(
            Vector3 candidate,
            List<Vector3> usedPoints,
            float minSeparation)
        {
            if (candidate == Vector3.Zero)
                return false;

            if (usedPoints == null ||
                usedPoints.Count == 0)
            {
                return true;
            }

            float minDistanceSquared =
                minSeparation *
                minSeparation;

            foreach (Vector3 usedPoint in usedPoints)
            {
                float dx =
                    candidate.X -
                    usedPoint.X;

                float dy =
                    candidate.Y -
                    usedPoint.Y;

                float distanceSquared =
                    (dx * dx) +
                    (dy * dy);

                if (distanceSquared <
                    minDistanceSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInsideRouteRestrictedEllipse(
            Vector3 position,
            float centerX,
            float centerY,
            float radiusX,
            float radiusY)
        {
            if (position == Vector3.Zero ||
                radiusX <= 0.0f ||
                radiusY <= 0.0f)
            {
                return false;
            }

            float dx =
                (position.X - centerX) /
                radiusX;

            float dy =
                (position.Y - centerY) /
                radiusY;

            return
                ((dx * dx) + (dy * dy)) <=
                1.0f;
        }

        private bool IsRandomRouteRestrictedArea(
            Vector3 position)
        {
            if (position == Vector3.Zero)
                return true;

            // LSIA / Los Santos International Airport - airside / runway area.
            if (IsInsideRouteRestrictedEllipse(
                    position,
                    -1335.0f,
                    -3040.0f,
                    1050.0f,
                    820.0f
                ))
            {
                return true;
            }

            // Bolingbroke Penitentiary - area di balik pagar prison.
            if (IsInsideRouteRestrictedEllipse(
                    position,
                    1690.0f,
                    2570.0f,
                    520.0f,
                    470.0f
                ))
            {
                return true;
            }

            // Fort Zancudo - military base / restricted roads.
            if (IsInsideRouteRestrictedEllipse(
                    position,
                    -2050.0f,
                    3130.0f,
                    1050.0f,
                    820.0f
                ))
            {
                return true;
            }

            // Humane Labs compound.
            if (IsInsideRouteRestrictedEllipse(
                    position,
                    3615.0f,
                    3740.0f,
                    650.0f,
                    520.0f
                ))
            {
                return true;
            }

            // NOOSE Headquarters compound.
            if (IsInsideRouteRestrictedEllipse(
                    position,
                    2535.0f,
                    -385.0f,
                    470.0f,
                    380.0f
                ))
            {
                return true;
            }

            return false;
        }
        private Vector3 GetRoadRoutePosition(
            Vector3 searchPosition)
        {
            if (searchPosition == Vector3.Zero)
                return Vector3.Zero;

            // FAST MAIN-ROAD LOOKUP
            // Jangan memakai GET_CLOSEST_MAJOR_VEHICLE_NODE di sini.
            // Native itu pada fresh launch dapat menunggu path-node area jauh
            // ikut ter-stream, sehingga route baru selesai setelah player bergerak.
            // nodeFlags=12 memprioritaskan node di tengah main/asphalt vehicle road.
            OutputArgument outRoad = new OutputArgument();

            bool foundRoad = Function.Call<bool>(
                Hash.GET_CLOSEST_VEHICLE_NODE,
                searchPosition.X,
                searchPosition.Y,
                searchPosition.Z,
                outRoad,
                12,
                3.0f,
                0.0f
            );

            // Fallback masih vehicle-road lookup, bukan raw ground coordinate.
            // Flag 8 dipakai bila tipe 12 tidak tersedia pada area tertentu.
            if (!foundRoad)
            {
                outRoad = new OutputArgument();
                foundRoad = Function.Call<bool>(
                    Hash.GET_CLOSEST_VEHICLE_NODE,
                    searchPosition.X,
                    searchPosition.Y,
                    searchPosition.Z,
                    outRoad,
                    8,
                    3.0f,
                    0.0f
                );
            }

            if (!foundRoad)
                return Vector3.Zero;

            Vector3 roadPosition = outRoad.GetResult<Vector3>();
            if (roadPosition == Vector3.Zero)
                return Vector3.Zero;

            // Pastikan hasil memang memiliki vehicle-node properties.
            // Density tidak dipakai sebagai hard filter karena jalan utama rural
            // dekat Chiliad dapat memiliki density rendah.
            OutputArgument outDensity = new OutputArgument();
            OutputArgument outFlags = new OutputArgument();
            bool hasNodeProperties = Function.Call<bool>(
                Hash.GET_VEHICLE_NODE_PROPERTIES,
                roadPosition.X,
                roadPosition.Y,
                roadPosition.Z,
                outDensity,
                outFlags
            );

            if (!hasNodeProperties)
                return Vector3.Zero;

            // Jangan reject bridge hanya karena ada water height di bawahnya.
            // Tolak hanya jika node praktis berada di permukaan air.
            OutputArgument outWaterZ = new OutputArgument();
            bool hasWater = Function.Call<bool>(
                Hash.GET_WATER_HEIGHT,
                roadPosition.X,
                roadPosition.Y,
                roadPosition.Z + 2.0f,
                outWaterZ
            );

            if (hasWater)
            {
                float waterZ = outWaterZ.GetResult<float>();
                if (roadPosition.Z <= waterZ + 1.25f)
                    return Vector3.Zero;
            }

            return roadPosition;
        }

        private Vector3 GetGlobalRandomRoutePosition(
            Vector3 fromPos,
            List<Vector3> usedPoints,
            bool isFirstCheckpoint)
        {
            for (
                int attempt = 0;
                attempt < GLOBAL_ROUTE_POINT_ATTEMPTS;
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

                Vector3 roadPosition =
                    GetRoadRoutePosition(
                        searchPos
                    );

                if (roadPosition ==
                    Vector3.Zero)
                {
                    continue;
                }

                // Global random POS memakai safety yang sama: hindari
                // compound tertutup dan pastikan ada akses kendaraan.
                if (IsRandomRouteRestrictedArea(
                        roadPosition
                    ))
                {
                    continue;
                }

                if (!HasVehicleRouteAccess(
                        fromPos,
                        roadPosition
                    ))
                {
                    continue;
                }

                // Tidak ada MAX distance.
                // Hanya cegah checkpoint berikutnya terlalu dekat.
                if (fromPos != Vector3.Zero &&
                    roadPosition.DistanceTo(
                        fromPos
                    ) <
                    GLOBAL_ROUTE_MIN_LEG_DISTANCE)
                {
                    continue;
                }

                if (!IsRoutePointFarEnough(
                        roadPosition,
                        usedPoints,
                        GLOBAL_ROUTE_MIN_POINT_SEPARATION
                    ))
                {
                    continue;
                }

                // POS 1 harus benar-benar berbeda dari ronde sebelumnya.
                if (isFirstCheckpoint &&
                    _lastGeneratedFirstPos !=
                        Vector3.Zero &&
                    roadPosition.DistanceTo(
                        _lastGeneratedFirstPos
                    ) <
                    GLOBAL_ROUTE_FIRST_POS_DIFFERENCE)
                {
                    continue;
                }

                return roadPosition;
            }

            return Vector3.Zero;
        }

        private bool GenerateRandomRoute()
        {
            // Ambil PosCount terbaru sebelum membuat route baru.
            LoadGameplayConfigFromIni(
                applyRuntimeChanges: false
            );

            int totalCheckpoints =
                Math.Max(
                    MIN_ROUTE_CHECKPOINT_COUNT,
                    Math.Min(
                        MAX_ROUTE_CHECKPOINT_COUNT,
                        _routeCheckpointCount
                    )
                );

            const int maxWholeRouteAttempts =
                6;

            for (
                int routeAttempt = 0;
                routeAttempt < maxWholeRouteAttempts;
                routeAttempt++)
            {
                List<Vector3> generatedCheckpoints =
                    new List<Vector3>();

                List<Vector3> usedPoints =
                    new List<Vector3>();

                if (_startPosition !=
                    Vector3.Zero)
                {
                    usedPoints.Add(
                        _startPosition
                    );
                }

                Vector3 currentReference =
                    _startPosition;

                bool routeValid =
                    true;

                for (
                    int i = 0;
                    i < totalCheckpoints;
                    i++)
                {
                    Vector3 nextPos =
                        GetGlobalRandomRoutePosition(
                            currentReference,
                            usedPoints,
                            i == 0
                        );

                    if (nextPos ==
                        Vector3.Zero)
                    {
                        routeValid =
                            false;

                        break;
                    }

                    generatedCheckpoints.Add(
                        nextPos
                    );

                    usedPoints.Add(
                        nextPos
                    );

                    currentReference =
                        nextPos;
                }

                if (!routeValid)
                    continue;

                Vector3 generatedFinish =
                    GetGlobalRandomRoutePosition(
                        currentReference,
                        usedPoints,
                        false
                    );

                if (generatedFinish ==
                    Vector3.Zero)
                {
                    continue;
                }

                if (!IsRoutePointFarEnough(
                        generatedFinish,
                        usedPoints,
                        GLOBAL_ROUTE_MIN_POINT_SEPARATION
                    ))
                {
                    continue;
                }

                // Publish route baru.
                _activePosList.Clear();

                foreach (
                    Vector3 checkpoint
                    in generatedCheckpoints)
                {
                    _activePosList.Add(
                        checkpoint
                    );
                }

                _activeFinishPosition =
                    generatedFinish;

                _currentPosIndex =
                    0;

                _isInsideCheckpoint =
                    false;

                _checkpointTouchStartTime =
                    0;

                if (generatedCheckpoints.Count >
                    0)
                {
                    _lastGeneratedFirstPos =
                        generatedCheckpoints[0];
                }

                UpdateCheckpointBlip();

                return true;
            }

            _activePosList.Clear();
            _activeFinishPosition =
                Vector3.Zero;
            _currentPosIndex =
                0;

            return false;
        }

        private void UpdateCheckpointBlip()
        {
            bool directMissionFinish =
                _missionGoToMountainEnabled &&
                !_missionGoToMountainPosEnabled;

            bool routeNeedsPos =
                !IsSelectedDirectRouteMode();

            if (!_hudRouteBlipEnabled)
            {
                ClearRouteBlipsOnly();
                return;
            }

            if (_activeFinishPosition == Vector3.Zero ||
                (routeNeedsPos &&
                 _activePosList.Count == 0))
            {
                ClearRouteBlipsOnly();
                return;
            }

            // PENTING:
            // Setiap refresh, semua blip route lama dihapus dahulu.
            // Karena di bawah hanya dibuat SATU POS aktif, maka:
            // POS yang sudah selesai otomatis hilang dan POS berikutnya
            // baru muncul setelah _currentPosIndex bertambah.
            ClearRouteBlipsOnly();

            // =========================================================
            // GO TO MOUNTAIN - DIRECT FINISH
            // =========================================================
            if (directMissionFinish)
            {
                if (!_routeFinishBlipEnabled)
                {
                    return;
                }

                Blip missionFinishBlip =
                    Blip.Create(
                        _activeFinishPosition
                    );

                missionFinishBlip.Sprite =
                    _routeFinishBlipSprite;

                missionFinishBlip.Color =
                    _routeActiveFinishBlipColor;

                missionFinishBlip.Scale =
                    _routeActiveFinishBlipScale;

                missionFinishBlip.IsShortRange =
                    false;

                missionFinishBlip.Name =
                    _routeDirectMissionFinishName;

                missionFinishBlip.ShowRoute =
                    _routeBlipShowRoute;

                _routeBlips.Add(
                    missionFinishBlip
                );

                return;
            }

            // =========================================================
            // ROUTE DENGAN POS
            // HANYA POS YANG SEDANG AKTIF YANG TAMPIL.
            //
            // Contoh:
            // currentPosIndex = 0 -> hanya POS 1
            // selesai POS 1     -> POS 1 dihapus, hanya POS 2
            // selesai POS 2     -> POS 2 dihapus, hanya POS 3
            // dst.
            // =========================================================
            bool hasActivePos =
                _currentPosIndex >= 0 &&
                _currentPosIndex <
                    _activePosList.Count;

            if (_routePosBlipEnabled &&
                hasActivePos)
            {
                int activePosIndex =
                    _currentPosIndex;

                Blip posBlip =
                    Blip.Create(
                        _activePosList[
                            activePosIndex
                        ]
                    );

                posBlip.Sprite =
                    _routePosBlipSprite;

                posBlip.Color =
                    _routeActivePosBlipColor;

                posBlip.Scale =
                    _routeActivePosBlipScale;

                posBlip.IsShortRange =
                    false;

                posBlip.Name =
                    _missionGoToMountainEnabled
                        ? FormatHudText(
                            _routeMissionPosNameFormat,
                            activePosIndex + 1
                        )
                        : FormatHudText(
                            _routeNormalPosNameFormat,
                            activePosIndex + 1
                        );

                posBlip.ShowRoute =
                    _routeBlipShowRoute;

                _routeBlips.Add(
                    posBlip
                );
            }

            // =========================================================
            // FINISH
            // Perilaku FINISH tidak diubah:
            // - sebelum semua POS selesai = FUTURE FINISH
            // - setelah semua POS selesai = ACTIVE FINISH
            // =========================================================
            if (!_routeFinishBlipEnabled)
            {
                return;
            }

            Blip finishBlip =
                Blip.Create(
                    _activeFinishPosition
                );

            finishBlip.Sprite =
                _routeFinishBlipSprite;

            finishBlip.IsShortRange =
                false;

            bool allPosCompleted =
                _currentPosIndex >=
                _activePosList.Count;

            if (allPosCompleted)
            {
                finishBlip.Color =
                    _routeActiveFinishBlipColor;

                finishBlip.Scale =
                    _routeActiveFinishBlipScale;

                finishBlip.Name =
                    _missionGoToMountainEnabled
                        ? _routeMissionFinishName
                        : _routeNormalFinishName;

                finishBlip.ShowRoute =
                    _routeBlipShowRoute;
            }
            else
            {
                finishBlip.Color =
                    _routeFutureFinishBlipColor;

                finishBlip.Scale =
                    _routeFutureFinishBlipScale;

                finishBlip.Name =
                    _missionGoToMountainEnabled
                        ? _routeFutureMissionFinishName
                        : _routeFutureNormalFinishName;

                finishBlip.ShowRoute =
                    _routeBlipShowRoute;
            }

            _routeBlips.Add(
                finishBlip
            );
        }

        private void ApplyPlayerHealth()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                _isPlayerHealthInitialized =
                    false;

                return;
            }

            if (player.Armor != 0) player.Armor = 0;

            int currentTime =
                Game.GameTime;

            // =========================================================
            // INITIALIZE / HOT-RELOAD RECOVERY
            // =========================================================
            // PENTING:
            // Ini dijalankan SEBELUM pengecekan player.IsDead.
            // Jadi kalau script sebelumnya membuat player masuk death-loop,
            // reload versi FIX ini akan memulihkan player SATU KALI tanpa
            // menambah Death Counter.
            if (!_isPlayerHealthInitialized)
            {
                if (player.IsDead)
                {
                    Function.Call(
                        Hash.RESURRECT_PED,
                        player.Handle
                    );

                    Function.Call(
                        Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                        player.Handle
                    );
                }

                Function.Call(
                    Hash.SET_PED_MAX_HEALTH,
                    player.Handle,
                    GTA_DEATH_GUARD_HEALTH
                );

                Function.Call(
                    Hash.SET_ENTITY_MAX_HEALTH,
                    player.Handle,
                    GTA_DEATH_GUARD_HEALTH
                );

                player.MaxHealth =
                    GTA_DEATH_GUARD_HEALTH;

                player.Health =
                    GTA_DEATH_GUARD_HEALTH;

                Function.Call(
                    Hash.SET_PED_SUFFERS_CRITICAL_HITS,
                    player.Handle,
                    false
                );

                _customHealth =
                    _playerMaxHealth;



                Function.Call(
                    Hash.SET_PED_ARMOUR,
                    player.Handle,
                    0
                );

                // JANGAN asumsi GTA benar-benar menyimpan angka yang kita set.
                // Selalu cache nilai AKTUAL yang dibaca kembali dari GTA.
                _lastGtaHealth =
                    Math.Max(
                        1,
                        player.Health
                    );



                _lastHealthDamageTime =
                    currentTime;

                _customDeathActive =
                    false;

                _customDeathStartTime =
                    0;

                _customDeathEndTime =
                    0;

                _customDeathFadeStarted =
                    false;

                _customDeathHudResumeTime =
                    0;

                _customDeathOverlayFadeInStartTime =
                    0;

                _customDeathOverlayFadeInEndTime =
                    0;

                _enemyCombatPausedForCustomDeath =
                    false;

                _nextNativeDeathRecoveryTime =
                    0;

                _isPlayerHealthInitialized =
                    true;

                return;
            }

            // =========================================================
            // NATIVE DEATH GUARD
            // =========================================================
            if (player.MaxHealth !=
                GTA_DEATH_GUARD_HEALTH)
            {
                Function.Call(
                    Hash.SET_PED_MAX_HEALTH,
                    player.Handle,
                    GTA_DEATH_GUARD_HEALTH
                );

                Function.Call(
                    Hash.SET_ENTITY_MAX_HEALTH,
                    player.Handle,
                    GTA_DEATH_GUARD_HEALTH
                );

                player.MaxHealth =
                    GTA_DEATH_GUARD_HEALTH;
            }

            Function.Call(
                Hash.SET_PED_SUFFERS_CRITICAL_HITS,
                player.Handle,
                false
            );

            // Saat custom death aktif, ProcessCustomDeathSystem yang pegang.
            if (_customDeathActive)
            {
                if (!player.IsDead)
                {
                    if (player.Health !=
                        GTA_DEATH_GUARD_HEALTH)
                    {
                        player.Health =
                            GTA_DEATH_GUARD_HEALTH;
                    }

                    _lastGtaHealth =
                        Math.Max(
                            1,
                            player.Health
                        );
                }

                return;
            }

            // Native death akan ditangani oleh ProcessCustomDeathSystem.
            if (player.IsDead)
            {
                return;
            }

            // =========================================================
            // BACA DAMAGE GTA SEBAGAI SENSOR
            // =========================================================
            int currentGtaHealth = Math.Max(1, player.Health);
            int healthDamage = Math.Max(0, _lastGtaHealth - currentGtaHealth);
            if (healthDamage > 0)
            {
                _customHealth = Math.Max(0, _customHealth - healthDamage);
                _lastHealthDamageTime = currentTime;

                bool wasTrackedEnemyDamage =
                    WasPlayerDamagedByTrackedEnemy(player);

                if (wasTrackedEnemyDamage)
                {
                    _lastTrackedEnemyDamageTime = currentTime;
                }

                // Jangan biarkan native last-damage source "nempel" ke damage berikutnya.
                // Tanpa clear ini, jatuh/tertabrak sesudah pernah ditembak enemy dapat
                // salah terbaca sebagai enemy-caused death.
                Function.Call(
                    Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY,
                    player.Handle
                );
            }

            // RESTORE INTERNAL GTA HEALTH
            // =========================================================
            if (player.Health !=
                GTA_DEATH_GUARD_HEALTH)
            {
                player.Health =
                    GTA_DEATH_GUARD_HEALTH;
            }

            // CACHE WAJIB dari nilai aktual GTA, bukan dari constant.
            _lastGtaHealth =
                Math.Max(
                    1,
                    player.Health
                );

            // =========================================================
            // CUSTOM DEATH TRIGGER
            // =========================================================
            if (_customHealth <= 0)
            {
                TriggerCustomDeath(
                    player,
                    currentTime
                );

                return;
            }

            // =========================================================
            // CUSTOM HEALTH REGEN
            // =========================================================
            if (_customHealth <
                    _playerMaxHealth &&
                _customHealth > 0)
            {
                if (currentTime -
                    _lastHealthDamageTime >=
                    _healthRegenDelay)
                {
                    if (currentTime -
                        _lastHealthRegenTick >=
                        _healthRegenInterval)
                    {
                        _customHealth =
                            Math.Min(
                                _playerMaxHealth,
                                _customHealth +
                                _healthRegenAmount
                            );

                        _lastHealthRegenTick =
                            currentTime;
                    }
                }
            }
        }

        private void DisableCharacterSwitching()
        {
            Function.Call(
                Hash.DISABLE_CONTROL_ACTION,
                0,
                19,
                true
            );
        }

        private string GetRouteBlipVisualKey()
        {
            return string.Join(
                "|",
                new object[]
                {
                    _hudRouteBlipEnabled,
                    _routeBlipShowRoute,
                    _routePosBlipEnabled,
                    _routeFinishBlipEnabled,
                    _routePosBlipSprite,
                    _routeActivePosBlipColor,
                    _routeActivePosBlipScale,
                    _routeFinishBlipSprite,
                    _routeActiveFinishBlipColor,
                    _routeActiveFinishBlipScale,
                    _routeFutureFinishBlipColor,
                    _routeFutureFinishBlipScale,
                    _routeDirectMissionFinishName,
                    _routeMissionPosNameFormat,
                    _routeNormalPosNameFormat,
                    _routeMissionFinishName,
                    _routeNormalFinishName,
                    _routeFutureMissionFinishName,
                    _routeFutureNormalFinishName
                }
            );
        }

        private string GetLocationName(Vector3 pos)
        {
            string zoneName = World.GetZoneLocalizedName(pos);
            if (string.IsNullOrEmpty(zoneName))
            {
                return "UNKNOWN";
            }
            return zoneName;
        }

        private bool IsTeleportLocationSection(
            string sectionName)
        {
            if (string.IsNullOrEmpty(
                    sectionName
                ))
            {
                return false;
            }

            string section =
                sectionName
                    .Trim()
                    .ToUpperInvariant();

            // Format baru.
            if (section == "LOCATIONS")
                return true;

            // Backward compatibility:
            // section lama tetap dibaca tetapi semuanya digabung
            // menjadi satu pool GLOBAL.
            return
                section == "LOCATIONS_LOS_SANTOS" ||
                section == "LOCATIONS_PALETO_BAY" ||
                section == "LOCATIONS_GRAPESEED" ||
                section == "LOCATIONS_SANDY_SHORES";
        }

        private void QueueMainThreadAction(
            string key,
            Action action,
            int repeatCount = 1)
        {
            if (action == null || repeatCount <= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                key = "anonymous";
            }

            BatchedMainThreadAction batch =
                _mainThreadBatches.GetOrAdd(
                    key,
                    _ => new BatchedMainThreadAction
                    {
                        Action = action,
                        PendingCount = 0,
                        IsQueued = false
                    }
                );

            lock (batch.SyncRoot)
            {
                // Refresh delegate untuk key yang sama. Ini menjaga closure
                // diagnostik/HUD tetap memakai data request terbaru, sementara
                // PendingCount menyimpan seluruh jumlah action yang belum diproses.
                batch.Action = action;
                batch.PendingCount += repeatCount;

                if (!batch.IsQueued)
                {
                    batch.IsQueued = true;
                    _mainThreadQueue.Enqueue(batch);
                }
            }
        }

        private void StartApocalypseTimer()
        {
            // Random Chaos OFF = seluruh countdown / roulette / survival
            // tidak dijalankan.
            if (!_randomChaosEnabled)
            {
                _countdownRemainingMs =
                    0;

                _isCountdownActive =
                    false;

                _isChaosRouletteActive =
                    false;

                _chaosRouletteLocked =
                    false;

                return;
            }

            // =========================================================
            // MULAI / RESET TIMER RANDOM CHAOS DARI INI
            // =========================================================
            _countdownRemainingMs =
                _countdownDurationMs;

            _lastCountdownUpdateTime =
                Game.GameTime;

            _isCountdownActive =
                true;

            // State roulette/survival sudah selesai.
            _isChaosRouletteActive =
                false;

            _chaosRouletteLocked =
                false;

            _isChaosActive =
                false;

            _activeChaosIndex =
                -1;

            _activeChaosKey =
                "";

            _activeChaosName =
                "";

            _chaosRemainingMs =
                0;
            _chaosSpawnWebhookNames.Clear();
        }

        private void AddApocalypseTimeSeconds(
            int seconds)
        {
            // Bonus waktu hanya boleh masuk ke TIMER NORMAL Random Chaos.
            // Saat roulette / Chaos Survival berjalan, timer normal pause.
            if (!_isCountdownActive ||
                _isChaosRouletteActive ||
                _isChaosActive ||
                seconds <= 0)
            {
                return;
            }

            long addedMs =
                (long)seconds * 1000L;

            long newRemaining =
                (long)_countdownRemainingMs +
                addedMs;

            _countdownRemainingMs =
                (int)Math.Min(
                    int.MaxValue,
                    newRemaining
                );
        }

        private void ProcessCountdownLogic(int currentTime)
        {
            if (!_randomChaosEnabled)
                return;

            // Timer countdown hanya berjalan pada NORMAL MODE.
            if (!_isCountdownActive ||
                _isChaosRouletteActive ||
                _isChaosActive)
            {
                return;
            }

            int elapsed =
                currentTime -
                _lastCountdownUpdateTime;

            if (elapsed < 0)
            {
                elapsed = 0;
            }

            _lastCountdownUpdateTime =
                currentTime;

            if (elapsed > 0)
            {
                _countdownRemainingMs =
                    Math.Max(
                        0,
                        _countdownRemainingMs -
                        elapsed
                    );
            }

            // =========================================================
            // TIMER COUNTDOWN HABIS
            //
            // JANGAN reset countdown di sini.
            // Masuk ke RANDOM CHAOS ROULETTE dahulu.
            // =========================================================
            if (_countdownRemainingMs <= 0)
            {
                _countdownRemainingMs =
                    0;

                _isCountdownActive =
                    false;

                StartChaosRoulette(
                    currentTime
                );
            }
        }

        private string FormatChaosHudClock(int milliseconds)
        {
            int seconds = (int)Math.Ceiling(Math.Max(0, milliseconds) / 1000.0);
            return (seconds / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private void StartChaosRoulette(
            int currentTime)
        {
            if (!_randomChaosEnabled)
                return;

            if (_isChaosRouletteActive ||
                _isChaosActive)
            {
                return;
            }

            if (_randomChaosOptions == null ||
                _randomChaosOptions.Count == 0)
            {
                ShowHudNotification(
                    "~y~[RANDOM CHAOS] ~w~Tidak ada pilihan RandomChaosX yang valid di INI."
                );

                StartApocalypseTimer();
                return;
            }

            _isCountdownActive =
                false;

            _countdownRemainingMs =
                0;

            _isChaosRouletteActive =
                true;

            _chaosRouletteLocked =
                false;

            _chaosRouletteStartTime =
                currentTime;

            _lastChaosRouletteItemChange =
                currentTime;

            _chaosRouletteLockEndTime =
                0;

            _currentChaosRouletteIndex =
                _random.Next(
                    _randomChaosOptions.Count
                );

            ShowHudNotification(
                "~r~[RANDOM CHAOS] ~w~Roulette dimulai!"
            );
        }

        private void ProcessChaosRouletteLogic(
            int currentTime)
        {
            if (!_randomChaosEnabled)
                return;

            if (!_isChaosRouletteActive)
                return;

            if (_randomChaosOptions == null ||
                _randomChaosOptions.Count == 0)
            {
                _isChaosRouletteActive =
                    false;

                StartApocalypseTimer();
                return;
            }

            int elapsed =
                currentTime -
                _chaosRouletteStartTime;

            if (!_chaosRouletteLocked)
            {
                if (
                    currentTime -
                    _lastChaosRouletteItemChange >=
                    _chaosRouletteItemIntervalMs
                )
                {
                    _currentChaosRouletteIndex =
                        _random.Next(
                            _randomChaosOptions.Count
                        );

                    _lastChaosRouletteItemChange =
                        currentTime;

                    PlayFrontendSoundCompat(
                        "NAV_UP_DOWN",
                        "HUD_FRONTEND_DEFAULT_SOUNDSET"
                    );
                }

                if (elapsed >=
                    _chaosRouletteDurationMs)
                {
                    _chaosRouletteLocked =
                        true;

                    _chaosRouletteLockEndTime =
                        currentTime +
                        _chaosRouletteLockMs;

                    PlayFrontendSoundCompat(
                        "SELECT",
                        "HUD_FRONTEND_DEFAULT_SOUNDSET"
                    );

                    StartChaosSurvival(
                        currentTime,
                        _currentChaosRouletteIndex
                    );
                }
            }

            int safeIndex =
                Math.Max(
                    0,
                    Math.Min(
                        _randomChaosOptions.Count - 1,
                        _currentChaosRouletteIndex
                    )
                );

            string selectedName =
                _randomChaosOptions[
                    safeIndex
                ].DisplayName;

            // Unified HUD draws once after all chaos state updates.

            if (_chaosRouletteLocked &&
                currentTime >=
                _chaosRouletteLockEndTime)
            {
                _isChaosRouletteActive =
                    false;

                _chaosRouletteLocked =
                    false;
            }
        }

        private void StartChaosSurvival(
            int currentTime,
            int chaosIndex)
        {
            if (!_randomChaosEnabled)
                return;

            if (_isChaosActive)
                return;

            if (_randomChaosOptions == null ||
                _randomChaosOptions.Count == 0)
            {
                StartApocalypseTimer();
                return;
            }

            _isChaosActive =
                true;

            _activeChaosIndex =
                Math.Max(
                    0,
                    Math.Min(
                        _randomChaosOptions.Count - 1,
                        chaosIndex
                    )
                );

            RandomChaosOption selectedOption =
                _randomChaosOptions[
                    _activeChaosIndex
                ];

            _activeChaosKey =
                selectedOption.Key;

            _activeChaosName =
                selectedOption.DisplayName;

            _chaosRemainingMs =
                IsTimedConfigurableRandomEffect(
                    _activeChaosKey
                )
                    ? Math.Max(
                        1000,
                        selectedOption.DurationMs
                    )
                    : 1000;

            _lastChaosUpdateTime =
                currentTime;

            _chaosSpawnWebhookNames.Clear();

            ExecuteSelectedChaos(
                currentTime
            );

            ShowHudNotification(
                "~r~[CHAOS] ~w~" +
                _activeChaosName +
                " | " +
                (_chaosRemainingMs / 1000) +
                " SECONDS"
            );
        }

        private void ProcessChaosSurvivalLogic(
            int currentTime)
        {
            if (!_randomChaosEnabled)
                return;

            if (!_isChaosActive)
                return;

            int elapsed =
                currentTime -
                _lastChaosUpdateTime;

            if (elapsed < 0)
            {
                elapsed = 0;
            }

            _lastChaosUpdateTime =
                currentTime;

            // Jika Random Chaos memilih Blackhole/Earthquake tetapi
            // effect lawannya sedang aktif, timer CHAOS ikut MENUNGGU.
            // Jadi durasi chaos tidak habis sebelum effect sempat mulai.
            bool queuedMutualChaosEffect =
                (
                    _activeChaosKey ==
                        "blackhole" &&
                    _pendingBlackholeDurationMs >
                        0 &&
                    currentTime <
                        _earthquakeEndTime
                ) ||
                (
                    _activeChaosKey ==
                        "earthquake" &&
                    _pendingEarthquakeDurationMs >
                        0 &&
                    currentTime <
                        _blackholeEndTime
                );

            if (queuedMutualChaosEffect)
            {
                // Unified HUD is drawn after this processor returns.
                return;
            }

            if (elapsed > 0)
            {
                _chaosRemainingMs =
                    Math.Max(
                        0,
                        _chaosRemainingMs -
                        elapsed
                    );
            }

            // Unified HUD is drawn after this processor returns.

            if (_chaosRemainingMs <= 0)
            {
                EndChaosSurvival();
            }
        }

        private void ExecuteSelectedChaos(
            int currentTime)
        {
            if (string.IsNullOrEmpty(
                    _activeChaosKey
                ) ||
                _chaosRemainingMs <= 0)
            {
                return;
            }

            _modeDurationOverrideMs =
                _chaosRemainingMs;

            try
            {
                ExecuteConfigurableRandomEffect(
                    _activeChaosKey
                );
            }
            finally
            {
                _modeDurationOverrideMs =
                    0;
            }
        }

        private void ApplyChaosNoWeapon(
            Ped player)
        {
            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            Function.Call(
                Hash.REMOVE_ALL_PED_WEAPONS,
                player.Handle,
                true
            );

            Function.Call(
                Hash.SET_CURRENT_PED_WEAPON,
                player.Handle,
                unchecked((uint)(int)WeaponHash.Unarmed),
                true
            );
        }

        private void RestoreChaosNoWeapon()
        {
            // Gift DISARM biasa masih aktif -> tetap tanpa weapon.
            if (Game.GameTime <
                _giftDisarmEndTime)
            {
                return;
            }

            // Jangan coba memasang weapon ketika model player masih animal.
            // Setelah player kembali menjadi manusia, restore transform akan
            // mengembalikan weapon lalu whitelist normal berjalan lagi.
            if (_playerAnimalTransformPhase == 2 ||
                _playerAnimalTransformPhase == 3)
            {
                return;
            }

            // Mengembalikan senjata memakai sistem bawaan script.
            // Kalau player sedang mati, ApplyPlayerWeaponWhitelist()
            // akan memberikannya lagi otomatis setelah player hidup.
            GiveDefaultPlayerWeapons();
        }

        private bool TryGetEntityFootprintRadius(
            Entity entity,
            out float radius)
        {
            radius =
                GIFT_CAGE_WALK_RADIUS;

            if (entity == null ||
                !entity.Exists())
            {
                return false;
            }

            int modelHash =
                Function.Call<int>(
                    Hash.GET_ENTITY_MODEL,
                    entity.Handle
                );

            if (modelHash == 0)
                return false;

            OutputArgument minArg =
                new OutputArgument();

            OutputArgument maxArg =
                new OutputArgument();

            Function.Call(
                Hash.GET_MODEL_DIMENSIONS,
                modelHash,
                minArg,
                maxArg
            );

            Vector3 minDim =
                minArg.GetResult<Vector3>();

            Vector3 maxDim =
                maxArg.GetResult<Vector3>();

            float width =
                Math.Abs(
                    maxDim.X -
                    minDim.X
                );

            float length =
                Math.Abs(
                    maxDim.Y -
                    minDim.Y
                );

            if (width <= 0.01f ||
                length <= 0.01f)
            {
                return false;
            }

            // Pakai setengah diagonal footprint.
            // Ini aman walaupun vehicle sedang miring 30/45/90 derajat
            // terhadap arah dunia karena cage berbentuk lingkaran.
            float halfWidth =
                width * 0.5f;

            float halfLength =
                length * 0.5f;

            float footprintHalfDiagonal =
                (float)Math.Sqrt(
                    (halfWidth * halfWidth) +
                    (halfLength * halfLength)
                );

            radius =
                footprintHalfDiagonal +
                GIFT_CAGE_EXTRA_CLEARANCE;

            // Safety clamp:
            // motor tetap tidak terlalu sempit,
            // bus/truck besar tetap mendapat ruang cukup.
            radius =
                Math.Max(
                    1.65f,
                    Math.Min(
                        12.0f,
                        radius
                    )
                );

            return true;
        }

        private bool IsChaosTrackedConfig(
            CustomNpcConfig config)
        {
            if (config == null ||
                string.IsNullOrEmpty(
                    config.WebhookName
                ))
            {
                return false;
            }

            return
                _chaosSpawnWebhookNames.Contains(
                    config.WebhookName
                );
        }

        private void EndChaosSurvival()
        {
            if (!_isChaosActive)
                return;

            string finishedChaosName =
                _activeChaosName;

            // Effect gift memakai timer yang sama dengan CHAOS HUD,
            // jadi pada akhir normal biarkan processor gift melakukan
            // cleanup normalnya pada tick yang sama.
            _isChaosActive =
                false;

            _activeChaosIndex =
                -1;

            _activeChaosKey =
                "";

            _activeChaosName =
                "";

            _chaosRemainingMs =
                0;
            _chaosSpawnWebhookNames.Clear();

            ShowHudNotification(
                "~g~[CHAOS ENDED] ~w~" +
                finishedChaosName +
                " selesai. Timer Random Chaos dimulai lagi."
            );

            StartApocalypseTimer();
        }

        private void CancelActiveConfiguredChaosEffect()
        {
            if (!_isChaosActive ||
                string.IsNullOrEmpty(
                    _activeChaosKey
                ))
            {
                return;
            }

            StopConfigurableRandomEffect(
                _activeChaosKey
            );
        }

        private void CancelChaosMode()
        {
            CancelActiveConfiguredChaosEffect();

            _isChaosActive =
                false;

            _isChaosRouletteActive =
                false;

            _chaosRouletteLocked =
                false;

            _activeChaosIndex =
                -1;

            _activeChaosKey =
                "";

            _activeChaosName =
                "";

            _chaosRemainingMs =
                0;
            _chaosSpawnWebhookNames.Clear();
        }

        private void ClearAllChasingMonsters()
        {
            // Jika round di-reset saat Random Chaos sedang berjalan,
            // hentikan dahulu effect/roulette Chaos.
            CancelChaosMode();

            // =========================================================
            // HAPUS ENEMY YANG MASIH LOADING
            // =========================================================
            for (int i = _enemiesToSpawn.Count - 1; i >= 0; i--)
            {
                EnemyLoadingTracker tracker =
                    _enemiesToSpawn[i];

                if (tracker.PedModel.IsValid)
                {
                    tracker.PedModel.MarkAsNoLongerNeeded();
                }

                if (tracker.VehicleModel.HasValue)
                {
                    Model vehicleModel =
                        tracker.VehicleModel.Value;

                    if (vehicleModel.IsValid)
                    {
                        vehicleModel.MarkAsNoLongerNeeded();
                    }
                }
            }

            _enemiesToSpawn.Clear();

            // =========================================================
            // HAPUS ANIMAL YANG MASIH LOADING
            // =========================================================
            for (int i = _animalsToSpawn.Count - 1; i >= 0; i--)
            {
                AnimalLoadingTracker tracker =
                    _animalsToSpawn[i];

                if (tracker.Model.IsValid)
                {
                    tracker.Model.MarkAsNoLongerNeeded();
                }
            }

            _animalsToSpawn.Clear();

            // =========================================================
            // HAPUS PASSENGER ENEMY YANG MASIH LOADING
            // =========================================================
            for (int i = _passengersToSpawn.Count - 1; i >= 0; i--)
            {
                PassengerLoadingTracker tracker =
                    _passengersToSpawn[i];

                if (tracker.Model.IsValid)
                {
                    tracker.Model.MarkAsNoLongerNeeded();
                }
            }

            _passengersToSpawn.Clear();

            // =========================================================
            // HAPUS SEMUA ENEMY AKTIF
            // =========================================================
            ClearAllEnemyBlips();

            for (int i = _activeBosses.Count - 1; i >= 0; i--)
            {
                ActiveBossTracker boss =
                    _activeBosses[i];

                if (boss.BossPed != null &&
                    boss.BossPed.Exists())
                {
                    if (boss.BossPed.IsInVehicle())
                    {
                        Vehicle veh =
                            boss.BossPed.CurrentVehicle;

                        if (veh != null &&
                            veh.Exists())
                        {
                            veh.Delete();
                        }
                    }

                    boss.BossPed.Delete();
                }
            }

            _activeBosses.Clear();

            // =========================================================
            // HAPUS SEMUA ANIMAL AKTIF
            // =========================================================
            for (int i = _activeAnimals.Count - 1; i >= 0; i--)
            {
                ActiveAnimalTracker animal =
                    _activeAnimals[i];

                if (animal.AnimalPed != null &&
                    animal.AnimalPed.Exists())
                {
                    animal.AnimalPed.Delete();
                }
            }

            _activeAnimals.Clear();

            // =========================================================
            // MULAI TIMER APOCALYPSE LAGI
            // =========================================================
            StartApocalypseTimer();
        }

        private void ProcessActiveTimers(int currentTime)
        {
            // =========================================================
            // GIFT EARTHQUAKE
            // =========================================================
            ProcessEarthquakeGift(
                currentTime
            );

            // =========================================================
            // GIFT DISARM
            // =========================================================
            ProcessGiftDisarm(
                currentTime
            );

            // =========================================================
            // GIFT SLEEP
            // =========================================================
            ProcessSleepGift(
                currentTime
            );

            // =========================================================
            // GIFT ROCK RAIN
            // =========================================================
            ProcessRockRainGift(
                currentTime
            );

            // =========================================================
            // GIFT CAGE
            // =========================================================
            ProcessGiftCage(
                currentTime
            );

            // =========================================================
            // RANDOM CHAOS CONFIGURABLE
            // =========================================================
            // Tidak punya processor effect duplikat.
            // Cage / Disarm / Transform Animal / Blackhole /
            // Wanted 5 Star / Earthquake memakai processor gift masing-masing.
            // =========================================================

            // Black Hole
            if (currentTime < _blackholeEndTime)
            {
                ApplyTrueBlackholeLogic();
            }
            else
            {
                CleanupBlackHoleProp();
            }

            // =========================================================
            // BLACKHOLE <-> EARTHQUAKE QUEUE
            // Jika effect aktif baru saja selesai pada tick ini,
            // jalankan effect lawan yang sedang menunggu.
            // =========================================================
            ProcessBlackholeEarthquakeQueue(
                currentTime
            );

            // Super Speed
            if (currentTime < _superSpeedEndTime)
            {
                ApplySuperSpeedLogic();
            }
            else if (_superSpeedEndTime != 0)
            {
                ResetSuperSpeed();
            }

            // Invincible
            if (currentTime < _invincibleEndTime)
            {
                ApplyInvincibleLogic();
            }
            else if (_invincibleEndTime != 0)
            {
                ResetInvincible();
            }

            // =========================================================
            // WEBHOOK EFFECTS 20-32
            // =========================================================
            ProcessStandaloneWebhookEffects(
                currentTime
            );
        }

        private void PauseAllCombatForCustomDeath()
        {
            Ped player =
                Game.Player.Character;

            // =========================================================
            // RELATIONSHIP SEMENTARA: ENEMY TIDAK MENGANGGAP PLAYER TARGET
            // =========================================================
            if (player != null &&
                player.Exists())
            {
                int playerGroupHash =
                    player.RelationshipGroup.Hash;

                // Monster <-> Player = neutral sementara.
                Function.Call(
                    Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                    3,
                    _relMonsterGroup.Hash,
                    playerGroupHash
                );

                Function.Call(
                    Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                    3,
                    playerGroupHash,
                    _relMonsterGroup.Hash
                );

                // Bodyguard <-> Monster = neutral sementara juga,
                // supaya bodyguard tidak membantai enemy yang sedang santai.
                Function.Call(
                    Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                    3,
                    _relBodyguardGroup.Hash,
                    _relMonsterGroup.Hash
                );

                Function.Call(
                    Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,
                    3,
                    _relMonsterGroup.Hash,
                    _relBodyguardGroup.Hash
                );
            }

            // =========================================================
            // ENEMY / MONSTER
            // =========================================================
            for (
                int i = _activeBosses.Count - 1;
                i >= 0;
                i--)
            {
                ActiveBossTracker boss =
                    _activeBosses[i];

                if (boss == null ||
                    boss.BossPed == null ||
                    !boss.BossPed.Exists() ||
                    !boss.BossPed.IsAlive)
                {
                    continue;
                }

                Ped enemy =
                    boss.BossPed;

                Vehicle currentVehicle =
                    enemy.IsInVehicle()
                        ? enemy.CurrentVehicle
                        : null;

                bool isDriver =
                    false;

                if (currentVehicle != null &&
                    currentVehicle.Exists())
                {
                    Ped driver =
                        currentVehicle.GetPedOnSeat(
                            VehicleSeat.Driver
                        );

                    isDriver =
                        driver != null &&
                        driver.Exists() &&
                        driver.Handle ==
                            enemy.Handle;
                }

                // =====================================================
                // ENEMY JALAN KAKI
                // =====================================================
                if (currentVehicle == null ||
                    !currentVehicle.Exists())
                {
                    // Aman clear task karena PED memang sedang jalan kaki.
                    Function.Call(
                        Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                        enemy.Handle
                    );

                    Function.Call(
                        Hash.TASK_WANDER_STANDARD,
                        enemy.Handle,
                        10.0f,
                        10
                    );
                }
                // =====================================================
                // DRIVER VEHICLE
                // =====================================================
                else if (isDriver)
                {
                    // PENTING:
                    // JANGAN CLEAR_PED_TASKS / CLEAR_PED_TASKS_IMMEDIATELY.
                    // Clear task pada ped di vehicle bisa membuat driver turun.

                    // Matikan weapon sementara supaya tidak drive-by / menembak.
                    Function.Call(
                        Hash.SET_CURRENT_PED_WEAPON,
                        enemy.Handle,
                        unchecked((uint)(int)WeaponHash.Unarmed),
                        true
                    );

                    // Ganti task combat menjadi drive santai TANPA keluar vehicle.
                    Function.Call(
                        Hash.TASK_VEHICLE_DRIVE_WANDER,
                        enemy.Handle,
                        currentVehicle.Handle,
                        8.0f,
                        786603
                    );
                }
                // =====================================================
                // PASSENGER VEHICLE
                // =====================================================
                else
                {
                    // Jangan clear primary task. Biarkan dia tetap duduk.
                    // Hapus secondary animation/aim saja.
                    Function.Call(
                        Hash.CLEAR_PED_SECONDARY_TASK,
                        enemy.Handle
                    );

                    // Hilangkan senjata sementara agar passenger tidak bisa nembak.
                    Function.Call(
                        Hash.SET_CURRENT_PED_WEAPON,
                        enemy.Handle,
                        unchecked((uint)(int)WeaponHash.Unarmed),
                        true
                    );

                    // Tidak diberikan TASK_PAUSE / TASK_LEAVE_VEHICLE.
                    // Ped tetap duduk di seat yang sama.
                }

                // Setelah respawn, AI combat/drive akan direfresh.
                boss.NextVehicleTaskRefreshTime =
                    0;

                boss.IsVehicleShootPhase =
                    false;
            }

            // =========================================================
            // ANGRY ANIMAL / MELEE ENEMY
            // =========================================================
            for (
                int i = _activeAnimals.Count - 1;
                i >= 0;
                i--)
            {
                ActiveAnimalTracker tracker =
                    _activeAnimals[i];

                if (tracker == null ||
                    tracker.AnimalPed == null ||
                    !tracker.AnimalPed.Exists() ||
                    !tracker.AnimalPed.IsAlive)
                {
                    continue;
                }

                Ped animal =
                    tracker.AnimalPed;

                Function.Call(
                    Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                    animal.Handle
                );

                Function.Call(
                    Hash.TASK_WANDER_STANDARD,
                    animal.Handle,
                    10.0f,
                    10
                );
            }

            // =========================================================
            // BODYGUARD
            // =========================================================
            for (
                int i = _activeBodyguards.Count - 1;
                i >= 0;
                i--)
            {
                ActiveBodyguardTracker tracker =
                    _activeBodyguards[i];

                if (tracker == null ||
                    tracker.BodyguardPed == null ||
                    !tracker.BodyguardPed.Exists() ||
                    !tracker.BodyguardPed.IsAlive)
                {
                    continue;
                }

                Ped bodyguard =
                    tracker.BodyguardPed;

                Function.Call(
                    Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                    bodyguard.Handle
                );

                Function.Call(
                    Hash.TASK_WANDER_STANDARD,
                    bodyguard.Handle,
                    10.0f,
                    10
                );

                tracker.CurrentTargetHandle =
                    0;

                tracker.NextCombatCheckTime =
                    0;
            }
        }

        private void ResumeAllCombatAfterCustomDeath()
        {
            Ped player =
                Game.Player.Character;

            // =========================================================
            // RESTORE RELATIONSHIP NORMAL
            // =========================================================
            if (player != null &&
                player.Exists())
            {
                int playerGroupHash =
                    player.RelationshipGroup.Hash;

                // Monster <-> Player kembali hostile.
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

                // Bodyguard <-> Monster kembali hostile.
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
            }

            // =========================================================
            // ENEMY / MONSTER
            // =========================================================
            for (
                int i = _activeBosses.Count - 1;
                i >= 0;
                i--)
            {
                ActiveBossTracker boss =
                    _activeBosses[i];

                if (boss == null)
                    continue;

                Ped enemy =
                    boss.BossPed;

                boss.NextVehicleTaskRefreshTime =
                    0;

                boss.IsVehicleShootPhase =
                    false;

                if (enemy != null &&
                    enemy.Exists() &&
                    enemy.IsAlive)
                {
                    // Kalau enemy masih berada di vehicle:
                    // JANGAN clear primary task karena bisa membuat dia turun.
                    if (enemy.IsInVehicle())
                    {
                        Function.Call(
                            Hash.CLEAR_PED_SECONDARY_TASK,
                            enemy.Handle
                        );

                        // Kembalikan weapon dari config.
                        if (boss.Config != null)
                        {
                            Function.Call(
                                Hash.SET_CURRENT_PED_WEAPON,
                                enemy.Handle,
                                unchecked(
                                    (uint)(int)boss.Config.Weapon
                                ),
                                true
                            );
                        }
                    }
                    else
                    {
                        // Enemy jalan kaki boleh dibersihkan dari wander.
                        Function.Call(
                            Hash.CLEAR_PED_TASKS,
                            enemy.Handle
                        );

                        if (boss.Config != null)
                        {
                            Function.Call(
                                Hash.SET_CURRENT_PED_WEAPON,
                                enemy.Handle,
                                unchecked(
                                    (uint)(int)boss.Config.Weapon
                                ),
                                true
                            );
                        }
                    }
                }
            }

            // =========================================================
            // ANIMAL
            // =========================================================
            for (
                int i = _activeAnimals.Count - 1;
                i >= 0;
                i--)
            {
                ActiveAnimalTracker tracker =
                    _activeAnimals[i];

                if (tracker != null &&
                    tracker.AnimalPed != null &&
                    tracker.AnimalPed.Exists() &&
                    tracker.AnimalPed.IsAlive)
                {
                    Function.Call(
                        Hash.CLEAR_PED_TASKS,
                        tracker.AnimalPed.Handle
                    );
                }
            }

            // =========================================================
            // BODYGUARD
            // =========================================================
            for (
                int i = _activeBodyguards.Count - 1;
                i >= 0;
                i--)
            {
                ActiveBodyguardTracker tracker =
                    _activeBodyguards[i];

                if (tracker == null)
                    continue;

                tracker.CurrentTargetHandle =
                    0;

                tracker.NextCombatCheckTime =
                    0;

                if (tracker.BodyguardPed != null &&
                    tracker.BodyguardPed.Exists() &&
                    tracker.BodyguardPed.IsAlive)
                {
                    Function.Call(
                        Hash.CLEAR_PED_TASKS,
                        tracker.BodyguardPed.Handle
                    );
                }
            }
        }

        private void ApplyCarriedPlayerVitalsAfterModelSwap(
            Ped player,
            int health)
        {
            if (player == null ||
                !player.Exists())
            {
                return;
            }

            Function.Call(
                Hash.SET_PED_MAX_HEALTH,
                player.Handle,
                GTA_DEATH_GUARD_HEALTH
            );

            Function.Call(
                Hash.SET_ENTITY_MAX_HEALTH,
                player.Handle,
                GTA_DEATH_GUARD_HEALTH
            );

            player.MaxHealth =
                GTA_DEATH_GUARD_HEALTH;

            Function.Call(
                Hash.SET_PED_SUFFERS_CRITICAL_HITS,
                player.Handle,
                false
            );

            _customHealth =
                Math.Max(
                    1,
                    Math.Min(
                        _playerMaxHealth,
                        health
                    )
                );

            player.Health =
                GTA_DEATH_GUARD_HEALTH;



            Function.Call(
                Hash.SET_PED_ARMOUR,
                player.Handle,
                    0
            );

            _lastGtaHealth =
                Math.Max(
                    1,
                    player.Health
                );


            _isPlayerHealthInitialized =
                true;
        }

        private void MaintainCleanSilentPlayer()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            // Bersihkan blood decal + visible wound/damage.
            player.ClearBloodDamage();
            player.ClearVisibleDamage();

            // STOP_PED_SPEAKING dibuat persisten dengan true, sedangkan
            // stop-current dipanggil setiap Tick untuk memotong speech yang
            // sempat dimulai oleh game/script lain pada frame berjalan.
            Function.Call(
                Hash.STOP_PED_SPEAKING,
                player.Handle,
                true
            );

            Function.Call(
                Hash.DISABLE_PED_PAIN_AUDIO,
                player.Handle,
                true
            );

            Function.Call(
                Hash.STOP_CURRENT_PLAYING_SPEECH,
                player.Handle
            );

            Function.Call(
                Hash.STOP_CURRENT_PLAYING_AMBIENT_SPEECH,
                player.Handle
            );
        }

        private Vector3 GetPlayerTravelForward(
            Ped player)
        {
            Entity source =
                player;

            if (player != null &&
                player.Exists() &&
                player.IsInVehicle())
            {
                Vehicle veh =
                    player.CurrentVehicle;

                if (veh != null &&
                    veh.Exists())
                {
                    source =
                        veh;
                }
            }

            Vector3 forward =
                source != null &&
                source.Exists()
                    ? Function.Call<Vector3>(
                        Hash.GET_ENTITY_FORWARD_VECTOR,
                        source.Handle
                    )
                    : new Vector3(
                        0.0f,
                        1.0f,
                        0.0f
                    );

            forward.Z =
                0.0f;

            float length =
                forward.Length();

            if (length <=
                0.001f)
            {
                return new Vector3(
                    0.0f,
                    1.0f,
                    0.0f
                );
            }

            forward.Normalize();

            return forward;
        }

        private Vector3 SnapEffectPositionToGround(
            Vector3 position,
            float zOffset = 0.15f)
        {
            float groundZ =
                GetGroundHeightCompat(
                    position
                );

            if (Math.Abs(
                    groundZ
                ) >
                0.001f)
            {
                position.Z =
                    groundZ +
                    zOffset;
            }

            return position;
        }

        private bool IsAllowedPlayerWeapon(
            WeaponHash weaponHash)
        {
            if (weaponHash == WeaponHash.Unarmed)
                return true;

            return _defaultPlayerWeapons.Contains(
                weaponHash
            );
        }

        private void GiveDefaultPlayerWeapons()
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            // Daftar weapon berasal dari:
            // MoonModConfig.ini -> [PLAYER_WEAPONS]
            foreach (WeaponHash weaponHash in _defaultPlayerWeapons)
            {
                if (!player.Weapons.HasWeapon(weaponHash))
                {
                    player.Weapons.Give(
                        weaponHash,
                        9999,
                        false,
                        true
                    );
                }

                Weapon weapon =
                    player.Weapons[weaponHash];

                if (weapon != null)
                {
                    // Unlimited ammo
                    weapon.Ammo = 9999;
                    weapon.InfiniteAmmo = true;
                    weapon.InfiniteAmmoClip = true;
                }

                // Native infinite ammo khusus weapon tersebut
                Function.Call(
                    Hash.SET_PED_INFINITE_AMMO,
                    player.Handle,
                    true,
                    unchecked((uint)(int)weaponHash)
                );
            }

            // Tidak perlu reload / clip tidak habis
            Function.Call(
                Hash.SET_PED_INFINITE_AMMO_CLIP,
                player.Handle,
                true
            );
        }

        private void ExecuteSafeTeleport(float targetX, float targetY, float? manualZ, string label)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            // Jangan tinggalkan cage gift sebagai prop hantu di lokasi lama.
            CleanupGiftCage();

            Entity target = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            target.Position = new Vector3(targetX, targetY, manualZ ?? 30.0f);
            ShowHudNotification($"~purple~TELEPORTED TO {label}!");
        }

        private void TeleportToStart(
            bool isFromGift = false,
            bool clearMonstersAndResetApocalypse = false)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            if (_startPosition ==
                Vector3.Zero)
            {
                _startPosition =
                    player.Position;
            }

            CleanupGiftCage();

            if (clearMonstersAndResetApocalypse)
            {
                ClearAllChasingMonsters();
            }

            Entity target =
                player.IsInVehicle()
                    ? (Entity)player.CurrentVehicle
                    : player;

            target.Position =
                _startPosition;

            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle != null &&
                    vehicle.Exists())
                {
                    vehicle.PlaceOnGround();
                }
            }

            // BACK TO START hanya memberi penalti SCORE -3.
            // Jumlah WIN tidak dikurangi agar statistik kemenangan tetap historis.
            if (isFromGift)
            {
                _score -= SCORE_BACK_TO_START_PENALTY;
                MarkCurrentHostProfileDirty();

                if (_scoreModeEnabled)
                {
                    ShowHudNotification(
                        $"~r~BACK TO START! SCORE -{SCORE_BACK_TO_START_PENALTY} | SCORE: {_score}"
                    );
                }
                else
                {
                    ShowHudNotification(
                        $"~r~BACK TO START! SCORE -{SCORE_BACK_TO_START_PENALTY} | SCORE: {_score}"
                    );
                }
            }

            // Reset route sesuai mode yang sedang aktif.
            if (_missionGoToMountainEnabled)
            {
                _startPosition =
                    _missionStartPosition;
            }

            // Jangan generate route sinkron pada frame gift/teleport.
            // Beri waktu streaming lalu bangun route sedikit per Tick.
            ScheduleRouteGeneration(
                ROUTE_GENERATION_STREAM_DELAY_MS
            );
        }

        private void TeleportToFinish(bool isFromGift = false)
        {
            Ped player = Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            // GO TO FINISH membatalkan cage gift agar prop tidak tertinggal.
            CleanupGiftCage();

            bool routeNeedsPos =
                !IsSelectedDirectRouteMode();

            if (_routeGenerationPending ||
                _activeFinishPosition == Vector3.Zero ||
                (routeNeedsPos &&
                 _activePosList.Count == 0))
            {
                ShowHudNotification(
                    "~y~ROUTE IS LOADING... ~w~Please wait."
                );

                return;
            }

            // =========================================================
            // SKIP SEMUA POS AKTIF
            // =========================================================
            _currentPosIndex = _activePosList.Count;

            // Reset checkpoint state supaya timer dari POS sebelumnya
            // tidak terbawa ke Finish.
            _isInsideCheckpoint = false;
            _checkpointTouchStartTime = 0;

            // =========================================================
            // UPDATE BLIP
            // Finish sekarang menjadi TARGET aktif
            // =========================================================
            UpdateCheckpointBlip();

            // =========================================================
            // TELEPORT PLAYER / VEHICLE KE FINISH
            // =========================================================
            Entity target =
                player.IsInVehicle()
                    ? (Entity)player.CurrentVehicle
                    : player;

            target.Position =
                _activeFinishPosition;

            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle != null &&
                    vehicle.Exists())
                {
                    vehicle.PlaceOnGround();
                }
            }

            ShowHudNotification(
                "~g~GO TO FINISH! ~w~Semua checkpoint dilewati. Finish Line sekarang aktif!"
            );
        }

        private void CheckUnderwaterStatus(int currentTime)
        {
            // Config UNDERWATER sudah ikut direload oleh
            // ProcessRuntimeGameplayConfig() ketika GoToMountain.ini berubah.

            // false = GTA berjalan normal.
            // Tidak ada timer, warning, atau auto-teleport dari script.
            if (!_underwaterSystemEnabled)
            {
                _isUnderwaterTimerActive =
                    false;

                _underwaterStartTime =
                    0;

                _underwaterCooldownEndTime =
                    0;

                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                _isUnderwaterTimerActive =
                    false;

                _underwaterStartTime =
                    0;

                _underwaterCooldownEndTime =
                    0;

                return;
            }

            bool isUnderwater =
                Function.Call<bool>(
                    Hash.IS_PED_SWIMMING_UNDER_WATER,
                    player.Handle
                );

            if (isUnderwater)
            {
                if (!_isUnderwaterTimerActive &&
                    _underwaterCooldownEndTime <= 0)
                {
                    _isUnderwaterTimerActive =
                        true;

                    _underwaterStartTime =
                        currentTime;
                }

                if (_isUnderwaterTimerActive &&
                    currentTime -
                        _underwaterStartTime >=
                        _underwaterTriggerDurationMs)
                {
                    _isUnderwaterTimerActive =
                        false;

                    _underwaterStartTime =
                        0;

                    _underwaterCooldownEndTime =
                        currentTime +
                        _underwaterTeleportWarningDurationMs;

                    ShowHudNotification(
                        "~r~Peringatan: Terlalu lama menyelam! " +
                        "~w~Teleport ke daratan dalam " +
                        (_underwaterTeleportWarningDurationMs / 1000) +
                        " detik..."
                    );
                }
            }
            else
            {
                // Keluar dari kondisi underwater sebelum batas waktu:
                // timer awal direset.
                if (_underwaterCooldownEndTime <= 0)
                {
                    _isUnderwaterTimerActive =
                        false;

                    _underwaterStartTime =
                        0;
                }
            }

            if (_underwaterCooldownEndTime > 0 &&
                currentTime >=
                    _underwaterCooldownEndTime)
            {
                _underwaterCooldownEndTime =
                    0;

                TeleportToNearestLand(
                    player
                );
            }
        }

        private void TeleportToNearestLand(Ped player)
        {
            Vector3 currentPos = player.Position;
            Vector3 safeLandPos = currentPos;
            bool landFound = false;

            for (
                float radius = 10.0f;
                radius <= _underwaterSearchLandMaxDistance;
                radius += 10.0f)
            {
                for (int angleDegree = 0; angleDegree < 360; angleDegree += 30)
                {
                    float radians = angleDegree * ((float)Math.PI / 180.0f);
                    float checkX = currentPos.X + (float)Math.Cos(radians) * radius;
                    float checkY = currentPos.Y + (float)Math.Sin(radians) * radius;
                    float groundZ = GetGroundHeightCompat(new Vector3(checkX, checkY, currentPos.Z + 20.0f));

                    OutputArgument outWaterZ = new OutputArgument();
                    bool isWater = Function.Call<bool>(Hash.GET_WATER_HEIGHT, checkX, checkY, groundZ, outWaterZ);

                    if (!isWater && groundZ > 0.0f) { safeLandPos = new Vector3(checkX, checkY, groundZ + 1.0f); landFound = true; break; }
                }
                if (landFound) break;
            }

            Entity targetEntity = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
            if (landFound) { targetEntity.Position = safeLandPos; ShowHudNotification("~g~Berhasil dipindahkan ke daratan terdekat!"); }
            else { TeleportToStart(false); ShowHudNotification("~y~Daratan tidak ditemukan di sekitar, dikembalikan ke Start!"); }
        }

        private bool TryGetCustomDeathGroundAt(
            float x,
            float y,
            float referenceZ,
            out Vector3 safePosition)
        {
            safePosition =
                Vector3.Zero;

            float probeZ =
                Math.Max(
                    1000.0f,
                    referenceZ + 500.0f
                );

            Function.Call(
                Hash.REQUEST_COLLISION_AT_COORD,
                x,
                y,
                probeZ
            );

            float groundZ =
                GetGroundHeightCompat(
                    new Vector3(
                        x,
                        y,
                        probeZ
                    )
                );

            // World.GetGroundHeight mengembalikan 0 saat gagal pada
            // banyak area. Jangan gunakan hasil gagal sebagai spawn.
            if (groundZ == 0.0f)
            {
                return false;
            }

            // Jangan respawn di dasar laut / di bawah permukaan air.
            OutputArgument waterZArg =
                new OutputArgument();

            bool hasWater =
                Function.Call<bool>(
                    Hash.GET_WATER_HEIGHT,
                    x,
                    y,
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
                    x,
                    y,
                    groundZ + 1.0f
                );

            return true;
        }

        private void TriggerCustomDeath(
            Ped player,
            int currentTime)
        {
            if (_customDeathActive ||
                player == null ||
                !player.Exists())
            {
                return;
            }

            _customHealth =
                0;

            _customDeathActive =
                true;

            _customDeathStartTime =
                currentTime;

            _customDeathEndTime =
                currentTime +
                CUSTOM_DEATH_DURATION_MS;

            _customDeathFadeStarted =
                false;

            // Simpan titik MATI asli sebelum ragdoll / blackhole /
            // physics menggeser player selama countdown respawn.
            _customDeathWorldPosition =
                player.Position;

            _customDeathHeading =
                player.Heading;

            _deathCount++;
            MarkCurrentHostProfileDirty();

            // SCORE -1 only after 3 deaths caused by tracked ENEMY NPC.
            // Animal, falls, water, and unrelated explosions do not count.
            bool enemyCausedDeath =
                _lastTrackedEnemyDamageTime > 0 &&
                currentTime - _lastTrackedEnemyDamageTime <= ENEMY_DEATH_CAUSE_WINDOW_MS;

            if (enemyCausedDeath)
            {
                _enemyDeathsForScorePenalty++;

                if ((_enemyDeathsForScorePenalty % SCORE_DEATH_PENALTY_EVERY) == 0)
                {
                    _score -= SCORE_DEATH_PENALTY;
                    MarkCurrentHostProfileDirty();
                }
            }

            _lastTrackedEnemyDamageTime = 0;

            // SLEEP langsung selesai saat masuk custom death
            // supaya ragdoll gift tidak bertabrakan dengan sistem respawn.
            _giftSleepEndTime =
                0;

            _giftSleepVehicleBailoutPending =
                false;

            _giftSleepVehicleBailoutRetryTime =
                0;

            // Hujan batu selesai saat player masuk custom death.
            StopRockRainGift();

            // Gift 5-star selesai saat player custom death.
            // Sesudah ini gameplay kembali NEVER WANTED.
            ResetWantedAfterCustomDeath();

            // Selama animasi custom death, cegah GTA native death mengambil alih.
            // Nanti saat respawn custom selesai, invincible dikembalikan sesuai
            // status gift INVINCIBLE.
            player.IsInvincible =
                true;

            // Jangan biarkan countdown checkpoint/finish tetap berjalan
            // ketika custom death sedang berlangsung.
            _isInsideCheckpoint =
                false;

            _checkpointTouchStartTime =
                0;

            // Kalau sedang di vehicle, keluarkan player dulu.
            if (player.IsInVehicle())
            {
                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle != null &&
                    vehicle.Exists())
                {
                    Function.Call(
                        Hash.TASK_LEAVE_VEHICLE,
                        player.Handle,
                        vehicle.Handle,
                        16
                    );
                }
            }
            else
            {
                Function.Call(
                    Hash.SET_PED_TO_RAGDOLL,
                    player.Handle,
                    1500,
                    1500,
                    0,
                    true,
                    true,
                    false
                );
            }

            ShowHudNotification(
                $"~r~CUSTOM DEATH! ~w~TOTAL DEATHS: {_deathCount}"
            );
        }

        private void ProcessCustomDeathSystem(
            int currentTime)
        {
            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists())
            {
                return;
            }

            // =========================================================
            // FALLBACK NATIVE DEATH
            // =========================================================
            // Dengan buffer 30.000 + critical hit OFF, normalnya native
            // death tidak terjadi. Kalau mod/impact ekstrem tetap membuat
            // player native-dead, recovery dibatasi maksimal 1x / 500ms
            // supaya TIDAK ada death-resurrect loop per frame.
            if (player.IsDead)
            {
                if (currentTime <
                    _nextNativeDeathRecoveryTime)
                {
                    return;
                }

                _nextNativeDeathRecoveryTime =
                    currentTime +
                    NATIVE_DEATH_RECOVERY_INTERVAL_MS;

                Vector3 deathPosition =
                    player.Position;

                float deathHeading =
                    player.Heading;

                Function.Call(
                    Hash.RESURRECT_PED,
                    player.Handle
                );

                Function.Call(
                    Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                    player.Handle
                );

                player.Position =
                    deathPosition;

                player.Heading =
                    deathHeading;

                Function.Call(
                    Hash.SET_PED_MAX_HEALTH,
                    player.Handle,
                    GTA_DEATH_GUARD_HEALTH
                );

                Function.Call(
                    Hash.SET_ENTITY_MAX_HEALTH,
                    player.Handle,
                    GTA_DEATH_GUARD_HEALTH
                );

                player.MaxHealth =
                    GTA_DEATH_GUARD_HEALTH;

                player.Health =
                    GTA_DEATH_GUARD_HEALTH;

                _lastGtaHealth =
                    Math.Max(
                        1,
                        player.Health
                    );

                if (!_customDeathActive)
                {
                    _customHealth =
                        0;

                    TriggerCustomDeath(
                        player,
                        currentTime
                    );
                }

                return;
            }

            if (!_customDeathActive)
                return;

            // Pastikan GTA tidak membunuh player selama animasi custom death.
            player.IsInvincible =
                true;

            Function.Call(
                Hash.SET_PED_SUFFERS_CRITICAL_HITS,
                player.Handle,
                false
            );

            if (player.Health !=
                GTA_DEATH_GUARD_HEALTH)
            {
                player.Health =
                    GTA_DEATH_GUARD_HEALTH;
            }

            _lastGtaHealth =
                Math.Max(
                    1,
                    player.Health
                );

            // Lock kontrol utama selama custom death.
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
                int control
                in disabledControls)
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
                    vehicle.Exists())
                {
                    Function.Call(
                        Hash.TASK_LEAVE_VEHICLE,
                        player.Handle,
                        vehicle.Handle,
                        16
                    );
                }
            }
            else
            {
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

            int elapsed =
                currentTime -
                _customDeathStartTime;

            if (!_customDeathFadeStarted &&
                elapsed >=
                    CUSTOM_DEATH_FADE_DELAY_MS)
            {
                // Flag tetap dipakai sebagai state,
                // tetapi layar hitam sekarang digambar MANUAL.
                _customDeathFadeStarted =
                    true;
            }

            // =========================================================
            // MANUAL BLACK SCREEN + RESPAWN COUNTDOWN
            // =========================================================
            int blackScreenStartTime =
                _customDeathStartTime +
                CUSTOM_DEATH_FADE_DELAY_MS +
                CUSTOM_DEATH_FADE_DURATION_MS;

            int remainingSeconds =
                0;

            bool showRespawnText =
                false;

            if (currentTime >=
                    blackScreenStartTime &&
                currentTime <
                    _customDeathEndTime)
            {
                int remainingMs =
                    _customDeathEndTime -
                    currentTime;

                remainingSeconds =
                    Math.Max(
                        1,
                        Math.Min(
                            CUSTOM_RESPAWN_COUNTDOWN_SECONDS,
                            (int)Math.Ceiling(
                                remainingMs /
                                1000.0f
                            )
                        )
                    );

                showRespawnText =
                    true;
            }

            // Selalu gambar overlay selama fase custom death,
            // bahkan saat masih fade-to-black.
            DrawCustomDeathBlackOverlay(
                currentTime,
                showRespawnText,
                remainingSeconds
            );

            if (currentTime <
                _customDeathEndTime)
            {
                return;
            }

            // =========================================================
            // CUSTOM RESPAWN
            // =========================================================
            _customDeathActive =
                false;

            _customDeathStartTime =
                0;

            _customDeathEndTime =
                0;

            _customDeathFadeStarted =
                false;

            _customHealth =
                _playerMaxHealth;


            player.Health =
                GTA_DEATH_GUARD_HEALTH;

            _lastGtaHealth =
                Math.Max(
                    1,
                    player.Health
                );


            Function.Call(
                Hash.SET_PED_ARMOUR,
                player.Handle,
                    0
            );



            _lastHealthDamageTime =
                currentTime;

            _nextNativeDeathRecoveryTime =
                0;

            Function.Call(
                Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                player.Handle
            );

            // =========================================================
            // RESPAWN SELALU DI DARAT
            // =========================================================
            Vector3 deathOrigin =
                _customDeathWorldPosition !=
                    Vector3.Zero
                        ? _customDeathWorldPosition
                        : player.Position;

            Vector3 groundRespawnPosition =
                GetCustomDeathGroundRespawnPosition(
                    deathOrigin
                );

            player.Position =
                groundRespawnPosition;

            player.Heading =
                _customDeathHeading;

            // Buang momentum dari BLACKHOLE / jatuh / ledakan.
            Function.Call(
                Hash.SET_ENTITY_VELOCITY,
                player.Handle,
                0.0f,
                0.0f,
                0.0f
            );

            Function.Call(
                Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                player.Handle
            );

            // Cegah blackhole yang masih aktif menarik player kembali
            // pada frame respawn yang sama. Setelah 2 detik, efek normal lagi.
            _customDeathGroundProtectionEndTime =
                currentTime +
                2000;

            _customDeathWorldPosition =
                Vector3.Zero;

            // Jangan mematikan gift INVINCIBLE yang masih aktif.
            if (currentTime >=
                _invincibleEndTime)
            {
                player.IsInvincible =
                    false;
            }
            else
            {
                player.IsInvincible =
                    true;
            }

            // Manual fade-in dari black overlay ke gameplay.
            _customDeathOverlayFadeInStartTime =
                currentTime;

            _customDeathOverlayFadeInEndTime =
                currentTime +
                700;

            // Normal HUD baru boleh kembali setelah manual fade-in selesai.
            _customDeathHudResumeTime =
                _customDeathOverlayFadeInEndTime;

            ShowHudNotification(
                "~g~RESPAWNED! ~w~Custom HP restored."
            );
        }

        private void StartOrUpdateCheckpointCelebration(
            Ped player,
            int currentTime)
        {
            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return;
            }

            // =====================================================
            // PLAYER DI KENDARAAN -> VEHICLE DANCE
            // =====================================================
            if (player.IsInVehicle())
            {
                // Mode jalan kaki sudah tidak dipakai.
                _checkpointCelebrationOnFootActive = false;
                _checkpointCelebrationNextHandsUpRefreshTime = 0;

                Vehicle vehicle =
                    player.CurrentVehicle;

                if (vehicle == null ||
                    !vehicle.Exists())
                {
                    return;
                }

                // Kalau kendaraan berubah saat masih di area checkpoint,
                // akhiri dance kendaraan lama dulu lalu mulai kendaraan baru.
                if (_checkpointCelebrationVehicle == null ||
                    !_checkpointCelebrationVehicle.Exists() ||
                    _checkpointCelebrationVehicle.Handle != vehicle.Handle)
                {
                    StopCheckpointVehicleDance();

                    _checkpointCelebrationVehicle =
                        vehicle;

                    _checkpointCelebrationVehicleBaseRotation =
                        vehicle.Rotation;

                    _checkpointCelebrationVehicleStartTime =
                        currentTime;
                }

                float danceSeconds =
                    Math.Max(
                        0,
                        currentTime -
                        _checkpointCelebrationVehicleStartTime
                    ) / 1000.0f;

                // Dance dibuat dari kombinasi pitch + roll.
                // Heading tetap mengikuti kendaraan supaya tidak memaksa
                // mobil / motor menghadap arah tertentu.
                float pitchOffset =
                    (float)Math.Sin(
                        danceSeconds * 6.0f
                    ) * 2.4f;

                float rollOffset =
                    (float)Math.Sin(
                        danceSeconds * 8.5f
                    ) * 4.0f;

                float heading =
                    vehicle.Heading;

                Function.Call(
                    Hash.SET_ENTITY_ROTATION,
                    vehicle.Handle,
                    _checkpointCelebrationVehicleBaseRotation.X +
                        pitchOffset,
                    _checkpointCelebrationVehicleBaseRotation.Y +
                        rollOffset,
                    heading,
                    2,
                    true
                );

                return;
            }

            // =====================================================
            // PLAYER JALAN KAKI -> ANGKAT TANGAN
            // =====================================================
            // Kalau sebelumnya dance memakai kendaraan, normalisasi dulu.
            StopCheckpointVehicleDance();

            // Refresh task secara berkala supaya tangan tetap terangkat
            // sepanjang countdown POS / FINISH, tetapi tidak di-restart
            // setiap frame.
            if (!_checkpointCelebrationOnFootActive ||
                currentTime >=
                    _checkpointCelebrationNextHandsUpRefreshTime)
            {
                Function.Call(
                    Hash.TASK_HANDS_UP,
                    player.Handle,
                    1400,
                    -1,
                    -1,
                    false
                );

                _checkpointCelebrationOnFootActive =
                    true;

                _checkpointCelebrationNextHandsUpRefreshTime =
                    currentTime +
                    1000;
            }
        }

        private void StopCheckpointCelebration(
            Ped player)
        {
            StopCheckpointVehicleDance();

            if (_checkpointCelebrationOnFootActive &&
                player != null &&
                player.Exists() &&
                !player.IsInVehicle())
            {
                Function.Call(
                    Hash.CLEAR_PED_TASKS_IMMEDIATELY,
                    player.Handle
                );
            }

            _checkpointCelebrationOnFootActive =
                false;

            _checkpointCelebrationNextHandsUpRefreshTime =
                0;
        }

        private void ProcessRouteLogic()
        {
            Ped player =
                Game.Player.Character;

            if (_customDeathActive)
            {
                _isInsideCheckpoint =
                    false;

                StopCheckpointCelebration(
                    player
                );

                return;
            }

            bool routeNeedsPos =
                !IsSelectedDirectRouteMode();

            if (_routeGenerationPending ||
                _activeFinishPosition == Vector3.Zero ||
                (routeNeedsPos &&
                 _activePosList.Count == 0))
            {
                _isInsideCheckpoint = false;

                StopCheckpointCelebration(
                    player
                );

                return;
            }

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                _isInsideCheckpoint = false;

                StopCheckpointCelebration(
                    player
                );

                return;
            }

            int currentTime =
                Game.GameTime;

            if (_currentPosIndex <
                _activePosList.Count)
            {
                Vector3 targetPos =
                    _activePosList[
                        _currentPosIndex
                    ];

                float distance =
                    player.Position.DistanceTo(
                        targetPos
                    );

                // =================================================
                // POS
                // =================================================
                // Ketika pemain masuk lingkaran POS (<= 4 meter):
                // - jalan kaki -> angkat tangan
                // - kendaraan  -> vehicle dance
                // =================================================
                if (distance <= 4.0f)
                {
                    if (!_isInsideCheckpoint)
                    {
                        // POS disentuh = semua effect dari Random Checkpoint
                        // pada leg sebelumnya langsung dihentikan.
                        CancelRandomCheckpointSequence(
                            clearPending: true
                        );
                        ResetActiveRandomCheckpointEffects();

                        _isInsideCheckpoint =
                            true;

                        _checkpointTouchStartTime =
                            currentTime;
                    }

                    StartOrUpdateCheckpointCelebration(
                        player,
                        currentTime
                    );

                    int elapsed =
                        currentTime -
                        _checkpointTouchStartTime;

                    int remainingSec =
                        (int)Math.Ceiling(
                            (5000 - elapsed) /
                            1000.0f
                        );

                    if (remainingSec < 1)
                    {
                        remainingSec = 1;
                    }

                    // Countdown POS.
                    if (_hudCheckpointCountdownEnabled)
                    {
                        DrawBoldTextOnScreen(
                            FormatHudText(
                                _checkpointCountdownTextFormat,
                                remainingSec
                            ),
                            _checkpointCountdownX,
                            _checkpointCountdownY,
                            _checkpointCountdownScale,
                            _checkpointCountdownR,
                            _checkpointCountdownG,
                            _checkpointCountdownB,
                            _checkpointCountdownA,
                            _checkpointCountdownFont
                        );
                    }

                    // Jika selesai bertahan 5 detik.
                    if (elapsed >= 5000)
                    {
                        StopCheckpointCelebration(
                            player
                        );

                        _isInsideCheckpoint =
                            false;

                        _currentPosIndex++;

                        // POS berhasil diselesaikan = SCORE +1.
                        // Diletakkan setelah countdown POS selesai agar tidak
                        // bisa bertambah berulang-ulang saat player berdiri di marker.
                        _score += SCORE_POS_REWARD;
                        MarkCurrentHostProfileDirty();

                        // POS route: RANDOM CHECKPOINT baru mulai SETELAH
                        // countdown POS selesai. Tidak ada trigger di tengah leg.
                        TriggerRandomCheckpointAfterCompletedPos(
                            currentTime
                        );

                        // Bonus checkpoint hanya masuk saat timer
                        // 60 menit NORMAL aktif.
                        bool checkpointTimeBonusAdded =
                            _isCountdownActive &&
                            !_isChaosRouletteActive &&
                            !_isChaosActive;

                        if (checkpointTimeBonusAdded)
                        {
                            AddApocalypseTimeSeconds(
                                RANDOM_CHAOS_POS_BONUS_SECONDS
                            );
                        }

                        string checkpointBonusText =
                            !_randomChaosEnabled
                                ? string.Empty
                                : checkpointTimeBonusAdded
                                    ? " ~b~(+15 Menit Random Chaos)"
                                    : " ~r~(Chaos Active - no time bonus)";

                        if (_currentPosIndex <
                            _activePosList.Count)
                        {
                            ShowHudNotification(
                                $"~g~[POS {_currentPosIndex}/{_activePosList.Count} SELESAI] ~w~Lanjut ke Pos {_currentPosIndex + 1}!" +
                                checkpointBonusText
                            );
                        }
                        else
                        {
                            ShowHudNotification(
                                $"~g~[POS {_activePosList.Count}/{_activePosList.Count} SELESAI!] ~w~Menuju ~r~FINISH LINE!" +
                                checkpointBonusText
                            );
                        }

                        UpdateCheckpointBlip();
                    }
                }
                else
                {
                    // Keluar lingkaran sebelum 5 detik:
                    // timer + celebration langsung berhenti.
                    StopCheckpointCelebration(
                        player
                    );

                    if (_isInsideCheckpoint)
                    {
                        _isInsideCheckpoint =
                            false;
                    }

                    // Marker Tiang Pos
                    // (hanya muncul jika pemain di luar lingkaran).
                    if (_hudRouteMarkerEnabled &&
                        distance <
                            _routeMarkerRenderDistance)
                    {
                        World.DrawMarker(
                            _routeMarkerType,
                            targetPos -
                                new Vector3(
                                    0,
                                    0,
                                    _routeMarkerOffsetZ
                                ),
                            Vector3.Zero,
                            Vector3.Zero,
                            new Vector3(
                                _routePosMarkerSizeX,
                                _routePosMarkerSizeY,
                                _routePosMarkerSizeZ
                            ),
                            Color.FromArgb(
                                _routePosMarkerA,
                                _routePosMarkerR,
                                _routePosMarkerG,
                                _routePosMarkerB
                            )
                        );
                    }
                }
            }
            else
            {
                // =================================================
                // FINISH LINE
                // =================================================
                float distanceToFinish =
                    player.Position.DistanceTo(
                        _activeFinishPosition
                    );

                // Ketika pemain masuk lingkaran FINISH (<= 5 meter):
                // - jalan kaki -> angkat tangan
                // - kendaraan  -> vehicle dance
                if (distanceToFinish <= 5.0f)
                {
                    if (!_isInsideCheckpoint)
                    {
                        // FINISH disentuh = semua effect dari Random Checkpoint
                        // langsung dihentikan sebelum countdown FINISH berjalan.
                        CancelRandomCheckpointSequence(
                            clearPending: true
                        );
                        ResetActiveRandomCheckpointEffects();

                        _isInsideCheckpoint =
                            true;

                        _checkpointTouchStartTime =
                            currentTime;
                    }

                    StartOrUpdateCheckpointCelebration(
                        player,
                        currentTime
                    );

                    int elapsed =
                        currentTime -
                        _checkpointTouchStartTime;

                    int remainingSec =
                        (int)Math.Ceiling(
                            (10000 - elapsed) /
                            1000.0f
                        );

                    if (remainingSec < 1)
                    {
                        remainingSec = 1;
                    }

                    // Countdown Finish.
                    if (_hudFinishCountdownEnabled)
                    {
                        DrawBoldTextOnScreen(
                            FormatHudText(
                                _finishCountdownTextFormat,
                                remainingSec
                            ),
                            _finishCountdownX,
                            _finishCountdownY,
                            _finishCountdownScale,
                            _finishCountdownR,
                            _finishCountdownG,
                            _finishCountdownB,
                            _finishCountdownA,
                            _finishCountdownFont
                        );
                    }

                    // Jika selesai bertahan 10 detik.
                    if (elapsed >= 10000)
                    {
                        StopCheckpointCelebration(
                            player
                        );

                        _isInsideCheckpoint =
                            false;

                        // FINISH selalu membersihkan seluruh RANDOM CHECKPOINT.
                        // Direct maupun POS tidak boleh membawa effect/roulette
                        // ke ronde berikutnya.
                        CancelRandomCheckpointSequence(
                            clearPending: true
                        );
                        ResetActiveRandomCheckpointEffects();
                        ResetRandomCheckpointProgressTracking();

                        _winCount++;
                        _score += SCORE_FINISH_REWARD;
                        MarkCurrentHostProfileDirty();

                        PlayFrontendSoundCompat(
                            "RACE_PLACED",
                            "HUD_AWARDS"
                        );

                        if (_scoreModeEnabled)
                        {
                            ShowHudNotification(
                                $"~g~[FINISH!] ~w~SCORE +{SCORE_FINISH_REWARD} | SCORE: {_score}"
                            );
                        }
                        else
                        {
                            ShowHudNotification(
                                $"~g~[FINISH!] ~w~Selamat, kamu berhasil menyelesaikan tantangan! | TOTAL WIN: {_winCount}"
                            );
                        }

                        CleanupGiftCage();

                        // ClearAllChasingMonsters() juga membatalkan Chaos aktif
                        // dan memanggil StartApocalypseTimer(), sehingga FINISH
                        // selalu me-reset Random Chaos ke countdown penuh (60 menit).
                        ClearAllChasingMonsters();

                        if (_selectedGameplayMode == MoonGameplayMode.Survival)
                        {
                            BeginNextSurvivalRound(player);
                        }
                        else if (_missionGoToMountainEnabled)
                        {
                            // =========================================
                            // GO TO MOUNTAIN -> LOOP KE START TETAP
                            // =========================================
                            _startPosition =
                                _missionStartPosition;

                            Entity missionResetTarget =
                                player.IsInVehicle()
                                    ? (Entity)player.CurrentVehicle
                                    : player;

                            Function.Call(
                                Hash.REQUEST_COLLISION_AT_COORD,
                                _missionStartPosition.X,
                                _missionStartPosition.Y,
                                _missionStartPosition.Z
                            );

                            missionResetTarget.Position =
                                _missionStartPosition;

                            if (player.IsInVehicle())
                            {
                                Vehicle missionResetVehicle =
                                    player.CurrentVehicle;

                                if (missionResetVehicle != null &&
                                    missionResetVehicle.Exists())
                                {
                                    missionResetVehicle.PlaceOnGround();
                                }
                            }

                            ScheduleRouteGeneration(
                                ROUTE_GENERATION_STREAM_DELAY_MS
                            );

                            ShowHudNotification(
                                _missionGoToMountainPosEnabled
                                    ? "~g~[NEW GO TO MOUNTAIN] ~w~Kembali ke START. POS mission di-reset."
                                    : "~g~[NEW GO TO MOUNTAIN] ~w~Kembali ke START. Menuju FINISH."
                            );
                        }
                        else
                        {
                            // =========================================
                            // NORMAL MODE -> behavior lama
                            // =========================================
                            // Player tetap di FINISH dan lokasi tersebut
                            // menjadi START route global berikutnya.
                            _startPosition =
                                player.Position;

                            ScheduleRouteGeneration(
                                ROUTE_GENERATION_RETRY_MS
                            );

                            ShowHudNotification(
                                "~b~[NEW GLOBAL ROUTE] ~w~POS dan FINISH sudah di-random ulang tanpa teleport."
                            );
                        }
                    }
                }
                else
                {
                    // Keluar area finish sebelum 10 detik:
                    // timer + celebration langsung berhenti.
                    StopCheckpointCelebration(
                        player
                    );

                    if (_isInsideCheckpoint)
                    {
                        _isInsideCheckpoint =
                            false;
                    }

                    // Marker Tiang Finish
                    // (hanya muncul jika pemain di luar lingkaran).
                    if (_hudRouteMarkerEnabled &&
                        distanceToFinish <
                            _routeMarkerRenderDistance)
                    {
                        World.DrawMarker(
                            _routeMarkerType,
                            _activeFinishPosition -
                                new Vector3(
                                    0,
                                    0,
                                    _routeMarkerOffsetZ
                                ),
                            Vector3.Zero,
                            Vector3.Zero,
                            new Vector3(
                                _routeFinishMarkerSizeX,
                                _routeFinishMarkerSizeY,
                                _routeFinishMarkerSizeZ
                            ),
                            Color.FromArgb(
                                _routeFinishMarkerA,
                                _routeFinishMarkerR,
                                _routeFinishMarkerG,
                                _routeFinishMarkerB
                            )
                        );
                    }
                }
            }
        }

        private void ResetOneRandomCheckpointEffect(
            string effectKey)
        {
            StopConfigurableRandomEffect(
                effectKey
            );
        }

        private void ResetActiveRandomCheckpointEffects()
        {
            if (_activeRandomCheckpointEffects.Count == 0)
                return;

            string[] activeEffects =
                _activeRandomCheckpointEffects.ToArray();

            _activeRandomCheckpointEffects.Clear();

            foreach (string effectKey in activeEffects)
            {
                ResetOneRandomCheckpointEffect(
                    effectKey
                );
            }
        }

        private void ResetRandomCheckpointProgressTracking()
        {
            _randomCheckpointThresholds.Clear();
            _randomCheckpointTriggeredIndices.Clear();

            _randomCheckpointLegKey =
                string.Empty;

            _randomCheckpointPreviousProgress =
                0.0f;

            _randomCheckpointProgressInitialized =
                false;
        }

        private void CancelRandomCheckpointSequence(
            bool clearPending)
        {
            _randomCheckpointSequenceActive =
                false;

            _randomCheckpointPhase =
                0;

            _randomCheckpointPhaseStartTime =
                0;

            _randomCheckpointNextRouletteSwitchTime =
                0;

            _randomCheckpointActiveTriggerIndex =
                -1;

            _randomCheckpointCurrentEffectKey =
                string.Empty;

            _randomCheckpointLockedEffectKey =
                string.Empty;

            _randomCheckpointSequenceLegKey =
                string.Empty;

            _randomCheckpointSequenceDirectMode =
                false;

            if (clearPending)
            {
                _randomCheckpointPendingQueue.Clear();
            }
        }

        private bool TryGetRandomCheckpointProgress(
            out float progress,
            out string legKey,
            out bool directMode)
        {
            progress =
                0.0f;

            legKey =
                string.Empty;

            directMode =
                IsSelectedDirectRouteMode();

            bool routeNeedsPos =
                !IsSelectedDirectRouteMode();

            if (_routeGenerationPending ||
                _activeFinishPosition == Vector3.Zero ||
                (routeNeedsPos &&
                 _activePosList.Count == 0))
            {
                return false;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                return false;
            }

            bool isGoingToFinish =
                _currentPosIndex >=
                _activePosList.Count;

            Vector3 currentTargetPos =
                (!isGoingToFinish &&
                 _activePosList.Count >
                    _currentPosIndex)
                    ? _activePosList[
                        _currentPosIndex
                      ]
                    : _activeFinishPosition;

            Vector3 legStartPos;

            if (isGoingToFinish)
            {
                if (_activePosList.Count > 0)
                {
                    legStartPos =
                        _activePosList[
                            _activePosList.Count - 1
                        ];
                }
                else
                {
                    legStartPos =
                        _startPosition;
                }
            }
            else if (_currentPosIndex == 0)
            {
                legStartPos =
                    _startPosition;
            }
            else
            {
                legStartPos =
                    _activePosList[
                        _currentPosIndex - 1
                    ];
            }

            float totalDistance =
                legStartPos.DistanceTo(
                    currentTargetPos
                );

            float currentDistance =
                player.Position.DistanceTo(
                    currentTargetPos
                );

            float targetRadius =
                isGoingToFinish
                    ? 5.0f
                    : 4.0f;

            if (currentDistance <=
                targetRadius)
            {
                progress =
                    1.0f;
            }
            else
            {
                float effectiveTotalDistance =
                    totalDistance -
                    targetRadius;

                if (effectiveTotalDistance <=
                    0.001f)
                {
                    effectiveTotalDistance =
                        1.0f;
                }

                float traveledDistance =
                    totalDistance -
                    currentDistance;

                float rawProgress =
                    traveledDistance /
                    effectiveTotalDistance;

                progress =
                    Math.Max(
                        0.0f,
                        Math.Min(
                            0.99f,
                            rawProgress
                        )
                    );
            }

            string routePrefix =
                "R" +
                _randomCheckpointRouteVersion +
                ":";

            if (directMode)
            {
                legKey =
                    routePrefix +
                    "DIRECT";
            }
            else if (isGoingToFinish)
            {
                legKey =
                    routePrefix +
                    "FINISH:" +
                    _activePosList.Count;
            }
            else
            {
                legKey =
                    routePrefix +
                    "POS:" +
                    _currentPosIndex;
            }

            return true;
        }

        private void PrepareRandomCheckpointLeg(
            string legKey,
            bool directMode,
            float currentProgress)
        {
            _randomCheckpointCurrentLegDirectMode =
                directMode;

            // Pool no-repeat hanya berlaku untuk Struktur 1 & 2.
            // Begitu pindah leg/POS, daftar pemenang di-reset.
            // Struktur 3 tetap boleh mendapatkan effect yang sama berkali-kali.
            if (!_randomCheckpointUsedEffectsLegKey.Equals(
                    legKey ?? string.Empty,
                    StringComparison.Ordinal
                ))
            {
                _randomCheckpointUsedEffectsThisLeg.Clear();

                _randomCheckpointUsedEffectsLegKey =
                    legKey ?? string.Empty;
            }

            if (_randomCheckpointProgressInitialized &&
                _randomCheckpointLegKey.Equals(
                    legKey,
                    StringComparison.Ordinal
                ))
            {
                return;
            }

            _randomCheckpointThresholds.Clear();
            _randomCheckpointTriggeredIndices.Clear();

            _randomCheckpointLegKey =
                legKey ?? string.Empty;

            int markerCount =
                directMode
                    ? _randomCheckpointDirectCount
                    : _randomCheckpointPerLeg;

            markerCount =
                Math.Max(
                    1,
                    markerCount
                );

            // N marker dibagi N+1 bagian supaya tidak ada marker tepat
            // di START maupun tepat di POS/FINISH.
            for (
                int i = 0;
                i < markerCount;
                i++)
            {
                float threshold =
                    (i + 1.0f) /
                    (markerCount + 1.0f);

                _randomCheckpointThresholds.Add(
                    threshold
                );

                // Jika config diaktifkan / di-reload di tengah perjalanan,
                // marker yang sudah berada di belakang player tidak dipicu
                // secara retroaktif.
                if (threshold <=
                    currentProgress +
                    0.0001f)
                {
                    _randomCheckpointTriggeredIndices.Add(
                        i
                    );
                }
            }

            _randomCheckpointPreviousProgress =
                currentProgress;

            _randomCheckpointProgressInitialized =
                true;
        }

        private void StartNextRandomCheckpointSequence(
            int currentTime)
        {
            if (_randomCheckpointSequenceActive ||
                _randomCheckpointPendingQueue.Count == 0)
            {
                return;
            }

            _randomCheckpointActiveTriggerIndex =
                _randomCheckpointPendingQueue.Dequeue();

            _randomCheckpointSequenceLegKey =
                _randomCheckpointLegKey;

            _randomCheckpointSequenceDirectMode =
                _randomCheckpointCurrentLegDirectMode;

            // POS mode memang hanya menghasilkan satu trigger per POS.
            // Direct mode mempertahankan queue supaya semua 7 garis merah
            // tetap benar-benar menghasilkan Random Checkpoint walau player cepat.
            if (!_randomCheckpointCurrentLegDirectMode)
            {
                _randomCheckpointPendingQueue.Clear();
            }

            _randomCheckpointSequenceActive =
                true;

            _randomCheckpointPhase =
                1;

            _randomCheckpointPhaseStartTime =
                currentTime;

            _randomCheckpointNextRouletteSwitchTime =
                0;

            _randomCheckpointCurrentEffectKey =
                string.Empty;

            _randomCheckpointLockedEffectKey =
                string.Empty;
        }

        private void SelectNextRandomCheckpointEffect()
        {
            if (_randomCheckpointEffectPool.Count == 0)
            {
                _randomCheckpointCurrentEffectKey =
                    string.Empty;

                return;
            }

            List<string> candidates;

            if (_randomCheckpointSequenceDirectMode)
            {
                // Struktur 3: semua effect selalu boleh muncul lagi.
                candidates =
                    new List<string>(
                        _randomCheckpointEffectPool
                    );
            }
            else
            {
                // Struktur 1 & 2: effect yang sudah pernah MENANG
                // pada leg/POS aktif tidak boleh masuk roulette lagi.
                candidates =
                    _randomCheckpointEffectPool
                        .Where(effectKey =>
                            !_randomCheckpointUsedEffectsThisLeg.Contains(
                                NormalizeRandomCheckpointEffectKey(
                                    effectKey
                                )
                            )
                        )
                        .ToList();
            }

            if (candidates.Count == 0)
            {
                // Tidak membuka ulang effect lama. Jika user membuat jumlah
                // checkpoint per leg melebihi jumlah effect unik, checkpoint
                // berikutnya tidak menjalankan effect yang sudah pernah menang.
                _randomCheckpointCurrentEffectKey =
                    string.Empty;

                return;
            }

            int index =
                _random.Next(
                    candidates.Count
                );

            _randomCheckpointCurrentEffectKey =
                candidates[
                    index
                ];
        }

        private void ExecuteRandomCheckpointEffect(
            string effectKey)
        {
            string normalized =
                NormalizeConfigurableRandomEffectKey(
                    effectKey
                );

            if (string.IsNullOrEmpty(normalized))
                return;

            int configuredDurationMs =
                0;

            _randomCheckpointEffectDurationsMs.TryGetValue(
                normalized,
                out configuredDurationMs
            );

            _modeDurationOverrideMs =
                IsTimedConfigurableRandomEffect(normalized)
                    ? ResolveConfiguredEffectDurationMs(
                        normalized,
                        configuredDurationMs
                    )
                    : 0;

            try
            {
                ExecuteConfigurableRandomEffect(
                    normalized
                );
            }
            finally
            {
                _modeDurationOverrideMs =
                    0;
            }

            // Hanya effect bertimer yang perlu di-reset otomatis
            // saat player menyentuh POS / FINISH.
            if (IsTimedConfigurableRandomEffect(normalized))
            {
                _activeRandomCheckpointEffects.Add(
                    normalized
                );
            }
        }

        private void TriggerRandomCheckpointAfterCompletedPos(
            int currentTime)
        {
            if (!_randomCheckpointEnabled ||
                IsSelectedDirectRouteMode())
            {
                return;
            }

            // Satu POS selesai = satu RANDOM CHECKPOINT.
            // Sequence leg lama sudah dibatalkan saat player pertama masuk area POS.
            CancelRandomCheckpointSequence(
                clearPending: true
            );

            string posKey =
                "R" +
                _randomCheckpointRouteVersion +
                ":POS_COMPLETED:" +
                _currentPosIndex;

            _randomCheckpointUsedEffectsThisLeg.Clear();
            _randomCheckpointUsedEffectsLegKey = posKey;
            _randomCheckpointLegKey = posKey;
            _randomCheckpointCurrentLegDirectMode = false;
            _randomCheckpointProgressInitialized = false;
            _randomCheckpointThresholds.Clear();
            _randomCheckpointTriggeredIndices.Clear();
            _randomCheckpointPendingQueue.Clear();
            _randomCheckpointPendingQueue.Enqueue(0);
            _randomCheckpointPreviousProgress = 0.0f;

            // ProcessRandomCheckpointSystem dipanggil setelah ProcessRouteLogic
            // pada Tick yang sama, jadi countdown Random Checkpoint langsung mulai.
        }

        private void ProcessRandomCheckpointSequence(
            int currentTime)
        {
            StartNextRandomCheckpointSequence(
                currentTime
            );

            if (!_randomCheckpointSequenceActive)
                return;

            if (_randomCheckpointPhase == 1)
            {
                int countdownDurationMs =
                    _randomCheckpointCountdownSeconds *
                    1000;

                if (currentTime -
                        _randomCheckpointPhaseStartTime >=
                    countdownDurationMs)
                {
                    _randomCheckpointPhase =
                        2;

                    _randomCheckpointPhaseStartTime =
                        currentTime;

                    _randomCheckpointNextRouletteSwitchTime =
                        currentTime;

                    SelectNextRandomCheckpointEffect();
                }

                return;
            }

            if (_randomCheckpointPhase == 2)
            {
                if (currentTime >=
                    _randomCheckpointNextRouletteSwitchTime)
                {
                    SelectNextRandomCheckpointEffect();

                    _randomCheckpointNextRouletteSwitchTime =
                        currentTime +
                        _randomCheckpointGachaSwitchIntervalMs;
                }

                if (currentTime -
                        _randomCheckpointPhaseStartTime >=
                    _randomCheckpointGachaDurationMs)
                {
                    if (string.IsNullOrEmpty(
                            _randomCheckpointCurrentEffectKey
                        ))
                    {
                        SelectNextRandomCheckpointEffect();
                    }

                    _randomCheckpointLockedEffectKey =
                        _randomCheckpointCurrentEffectKey;

                    _randomCheckpointPhase =
                        3;

                    _randomCheckpointPhaseStartTime =
                        currentTime;

                    string normalizedLockedEffect =
                        NormalizeRandomCheckpointEffectKey(
                            _randomCheckpointLockedEffectKey
                        );

                    // Struktur 1 & 2: setelah MENANG, effect langsung
                    // dihapus dari kandidat gacha untuk checkpoint berikutnya
                    // pada leg/POS yang sama. Struktur 3 tidak memakai filter ini.
                    if (!_randomCheckpointSequenceDirectMode &&
                        _randomCheckpointSequenceLegKey.Equals(
                            _randomCheckpointUsedEffectsLegKey,
                            StringComparison.Ordinal
                        ) &&
                        !string.IsNullOrEmpty(
                            normalizedLockedEffect
                        ))
                    {
                        _randomCheckpointUsedEffectsThisLeg.Add(
                            normalizedLockedEffect
                        );
                    }

                    // Effect baru keluar SETELAH hasil sudah LOCK.
                    // Jika pool unik habis, tidak ada effect lama yang diulang.
                    if (!string.IsNullOrEmpty(
                            normalizedLockedEffect
                        ))
                    {
                        ExecuteRandomCheckpointEffect(
                            normalizedLockedEffect
                        );
                    }
                }

                return;
            }

            if (_randomCheckpointPhase == 3)
            {
                if (currentTime -
                        _randomCheckpointPhaseStartTime >=
                    _randomCheckpointLockedDisplayMs)
                {
                    // Direct mode dapat memiliki beberapa marker yang terlewati
                    // saat roulette sebelumnya masih tampil. Queue dipertahankan
                    // supaya seluruh 7 checkpoint tetap diproses satu per satu.
                    CancelRandomCheckpointSequence(
                        clearPending: !_randomCheckpointSequenceDirectMode
                    );
                }
            }
        }

        private void ProcessRandomCheckpointSystem(
            int currentTime)
        {
            if (!_randomCheckpointEnabled)
                return;

            // Saat player masuk POS / FINISH, sequence dan effect lama berhenti.
            if (_isInsideCheckpoint)
            {
                CancelRandomCheckpointSequence(
                    clearPending: true
                );

                ResetActiveRandomCheckpointEffects();
                return;
            }

            // =========================================================
            // DIRECT ROUTE
            // =========================================================
            // Tanpa POS: progress START -> FINISH dibagi menjadi 7 marker
            // (atau DirectCheckpointCount dari INI). Semua marker yang benar-benar
            // dilewati masuk queue dan diproses berurutan, jadi tidak hilang.
            // =========================================================
            if (IsSelectedDirectRouteMode())
            {
                float currentProgress;
                string legKey;
                bool directMode;

                if (TryGetRandomCheckpointProgress(
                        out currentProgress,
                        out legKey,
                        out directMode
                    ))
                {
                    PrepareRandomCheckpointLeg(
                        legKey,
                        true,
                        currentProgress
                    );

                    float previousProgress =
                        _randomCheckpointPreviousProgress;

                    if (currentProgress > previousProgress)
                    {
                        for (int i = 0; i < _randomCheckpointThresholds.Count; i++)
                        {
                            if (_randomCheckpointTriggeredIndices.Contains(i))
                                continue;

                            float threshold = _randomCheckpointThresholds[i];
                            if (previousProgress < threshold &&
                                currentProgress >= threshold)
                            {
                                _randomCheckpointTriggeredIndices.Add(i);
                                _randomCheckpointPendingQueue.Enqueue(i);
                            }
                        }
                    }

                    _randomCheckpointPreviousProgress = currentProgress;
                }
            }

            // POS route tidak memakai progress marker. Trigger dimasukkan oleh
            // TriggerRandomCheckpointAfterCompletedPos() setelah countdown POS selesai.
            ProcessRandomCheckpointSequence(
                currentTime
            );
        }

        private string GetCounterLabelPrefix(
            string format,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(format))
                return fallback;

            int placeholder =
                format.IndexOf(
                    "{0}",
                    StringComparison.Ordinal
                );

            string value =
                placeholder >= 0
                    ? format.Substring(
                        0,
                        placeholder
                    )
                    : format;

            value = value.Trim();

            return string.IsNullOrEmpty(value)
                ? fallback
                : value;
        }

        private float GetRandomCheckpointCenterTextScale(
            string text)
        {
            float scale =
                _randomCheckpointGachaScale;

            int length =
                string.IsNullOrEmpty(text)
                    ? 0
                    : text.Length;

            // Tetap besar untuk nama normal, tetapi effect yang sangat panjang
            // otomatis sedikit dikecilkan supaya tidak menabrak kolom kiri/kanan.
            if (length >= 24)
            {
                scale =
                    Math.Min(
                        scale,
                        0.50f
                    );
            }
            else if (length >= 19)
            {
                scale =
                    Math.Min(
                        scale,
                        0.56f
                    );
            }
            else if (length >= 15)
            {
                scale =
                    Math.Min(
                        scale,
                        0.62f
                    );
            }

            return scale;
        }

        private float MeasureReferenceProgressText(string text, float scale, int font)
        {
            Function.Call(Hash.SET_TEXT_FONT, font);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call((Hash)0x54CE8AC98E120CABUL, "STRING"); // BEGIN_TEXT_COMMAND_GET_SCREEN_WIDTH_OF_DISPLAY_TEXT
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text ?? string.Empty);
            return Function.Call<float>((Hash)0x85F061DA64ED2F67UL, true); // END_TEXT_COMMAND_GET_SCREEN_WIDTH_OF_DISPLAY_TEXT
        }

        private void SplitTimerStatusText(
            string sourceText,
            out string label,
            out string timer)
        {
            label =
                sourceText ??
                string.Empty;

            timer =
                string.Empty;

            if (string.IsNullOrEmpty(
                    label
                ))
            {
                return;
            }

            int openIndex =
                label.LastIndexOf(
                    " (",
                    StringComparison.Ordinal
                );

            if (openIndex < 0 ||
                !label.EndsWith(
                    ")",
                    StringComparison.Ordinal
                ))
            {
                return;
            }

            int timerStart =
                openIndex +
                2;

            int timerLength =
                label.Length -
                timerStart -
                1;

            if (timerLength <= 0)
            {
                return;
            }

            timer =
                label.Substring(
                    timerStart,
                    timerLength
                );

            label =
                label.Substring(
                    0,
                    openIndex
                );
        }

        private List<ActiveStatusItem> SelectActiveHudCards(List<ActiveStatusItem> statuses)
        {
            var byKey = statuses.ToDictionary(item => item.Key);
            foreach (string key in _activeHudKnown.Keys.ToList())
                if (!byKey.ContainsKey(key))
                {
                    _activeHudKnown.Remove(key);
                    _activeHudOrder.Remove(key);
                    _hudTimerPeakSeconds.Remove(key);
                }
            int limit = Math.Max(1, Math.Min(3, _activeHudMaxVisible));
            foreach (ActiveStatusItem item in statuses)
            {
                int prior;
                bool fresh = !_activeHudKnown.TryGetValue(item.Key, out prior);
                // An extended or restarted effect counts as a new arrival.
                if (fresh || item.Revision != prior)
                {
                    _activeHudOrder.Remove(item.Key);
                    _activeHudOrder.Add(item.Key);
                    while (_activeHudOrder.Count > limit) _activeHudOrder.RemoveAt(0);
                }
                _activeHudKnown[item.Key] = item.Revision;
            }
            while (_activeHudOrder.Count > limit) _activeHudOrder.RemoveAt(0);
            return _activeHudOrder.AsEnumerable().Reverse().Select(key => byKey[key]).ToList();
        }

    }
}
