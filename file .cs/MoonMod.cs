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
    public partial class MoonMod : Script
    {
        // ERROR SCREEN STATE - POS LOADING FAILURE
        private bool _posLoadingFailed = false;
        private int _posFailureStartTime = 0;

        private void OnHardwareBlockedTick(object sender, EventArgs e)
        {
            try
            {
                // Sembunyikan HUD/radar GTA agar layar benar-benar tertutup.
                Function.Call(
                    Hash.HIDE_HUD_AND_RADAR_THIS_FRAME
                );

                // Full-screen black overlay.
                Function.Call(
                    Hash.DRAW_RECT,
                    0.5f,
                    0.5f,
                    1.0f,
                    1.0f,
                    0,
                    0,
                    0,
                    255
                );

                // Text utama di atas overlay hitam.
                Function.Call(
                    Hash.SET_TEXT_FONT,
                    7
                );

                Function.Call(
                    Hash.SET_TEXT_SCALE,
                    1.10f,
                    1.10f
                );

                Function.Call(
                    Hash.SET_TEXT_COLOUR,
                    255,
                    60,
                    60,
                    255
                );

                Function.Call(
                    Hash.SET_TEXT_WRAP,
                    0.0f,
                    1.0f
                );

                Function.Call(
                    Hash.SET_TEXT_CENTRE,
                    true
                );

                Function.Call(
                    Hash.SET_TEXT_EDGE,
                    2,
                    0,
                    0,
                    0,
                    255
                );

                Function.Call(
                    Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT,
                    "STRING"
                );

                Function.Call(
                    Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                    "MOD BLOCKED BY OWNER"
                );

                Function.Call(
                    Hash.END_TEXT_COMMAND_DISPLAY_TEXT,
                    0.5f,
                    0.46f
                );
            }
            catch
            {
                // Fail silently: HWID tetap diblok walaupun render gagal.
            }
        }

        private void DrawPosFailureErrorScreen(int elapsedMs)
        {
            // Full black background
            Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            Function.Call(
                Hash.DRAW_RECT,
                0.5f, 0.5f,
                1.0f, 1.0f,
                0, 0, 0, 255
            );

            // Main error message - BIG AND CENTERED
            Function.Call(Hash.SET_TEXT_FONT, 7);
            Function.Call(Hash.SET_TEXT_SCALE, 2.0f, 2.0f);
            Function.Call(Hash.SET_TEXT_COLOUR, 255, 100, 100, 255);
            Function.Call(Hash.SET_TEXT_CENTRE, true);
            Function.Call(Hash.SET_TEXT_EDGE, 6, 0, 0, 0, 255);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "POS GAGAL MEMUAT");
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, 0.5f, 0.4f);

            // Countdown - calculate which number to show
            int countdownNum = 3;
            if (elapsedMs >= 2000) countdownNum = 1;
            else if (elapsedMs >= 1000) countdownNum = 2;

            Function.Call(Hash.SET_TEXT_FONT, 7);
            Function.Call(Hash.SET_TEXT_SCALE, 3.5f, 3.5f);
            Function.Call(Hash.SET_TEXT_COLOUR, 255, 150, 150, 255);
            Function.Call(Hash.SET_TEXT_CENTRE, true);
            Function.Call(Hash.SET_TEXT_EDGE, 8, 0, 0, 0, 255);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, countdownNum.ToString());
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, 0.5f, 0.55f);

            // "AKAN DI ULANG KEMBALI" - subtitle
            Function.Call(Hash.SET_TEXT_FONT, 4);
            Function.Call(Hash.SET_TEXT_SCALE, 1.5f, 1.5f);
            Function.Call(Hash.SET_TEXT_COLOUR, 200, 100, 100, 255);
            Function.Call(Hash.SET_TEXT_CENTRE, true);
            Function.Call(Hash.SET_TEXT_EDGE, 4, 0, 0, 0, 255);

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, "AKAN DI ULANG KEMBALI");
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, 0.5f, 0.65f);
        }

        public MoonMod()
        {
            // 1. HWID selalu diperiksa paling awal.
            if (!ValidateHardwareLicense())
            {
                // PC berbeda / HWID tidak cocok:
                // jangan aktifkan gameplay, entity, webhook, atau input nama.
                _isRunning = false;
                Tick += OnHardwareBlockedTick;
                return;
            }

            // 2. Load alur startup global dari MoonModConfig.ini.
            // Ini harus dibaca SEBELUM startup UI supaya PROFILE / GAMEPLAY
            // dapat dilewati sesuai setting.
            LoadStartupFlowConfigFromIni();

            // HUD preset harus sudah diketahui sebelum startup UI dibaca.
            LoadHudPresetSelectionFromIni();

            // 3. Load pilihan TOTAL POS + DefaultPosCount per gameplay sebelum UI dibuka.
            // GoToMountainConfig.ini dan SurvivalConfig.ini punya daftar/default sendiri.
            LoadStartupPosOptionsFromIni();

            // 4. Load design khusus PLAYER SETUP sebelum input dibuka.
            // Hanya membaca [HUD_PLAYER_SETUP] dari MoonHUD.ini.
            LoadStartupPlayerSetupDesignFromIni();
            LoadStartupGameplaySelectDesignFromIni();
            LoadStartupDifficultySelectDesignFromIni();
            LoadStartupLoadingDesignFromIni();

            // Profile database hanya diperlukan jika layar PROFILE memang dipakai.
            // SkipProfileSelection=true = session tanpa host, jadi jangan pilih HOST1
            // dan jangan membuat/mengubah ProfileHost.ini dari startup ini.
            if (!_startupSkipProfileSelection)
            {
                LoadHostProfilesFromIni();
            }

            // Terapkan startup flow setelah config/desain siap.
            // GameplayMode=gotomountain/survival melewati layar GAMEPLAY.
            InitializeStartupFlowFromConfig();

            // PLAYER PROFILE sekarang full mouse: dropdown nama + tombol NEXT.
            // Jika profile/gameplay di-skip, OnStartupNameTick langsung menggambar
            // stage berikutnya sesuai konfigurasi.

            // 5. Setelah HWID valid, buka startup flow sesuai MoonModConfig.ini.
            Aborted += OnStartupAborted;
            Tick += OnStartupNameTick;
        }

        private void OnStartupAborted(object sender, EventArgs e)
        {
            // Never leave GTA frozen if the script is reloaded while setup/loading UI is open.
            try { SetStartupLoadingProtection(false); } catch { }
            try { Function.Call(Hash.SET_TIME_SCALE, 1.0f); } catch { }
        }

        private void InitializeGameplayAfterNameInput()
        {
            // Profile is committed only when START is pressed after DIFFICULTY selection.
            ApplySelectedHostProfileToSession();

            LoadMoonModConfigFromIni();
            LoadSurvivalConfigFromIni();
            LoadGiftTimerConfigFromIni();
            LoadRandomTeleportConfigFromIni();
            LoadGameplayConfigFromIni(applyRuntimeChanges: false);
            LoadRandomGatchaConfigFromIni();
            LoadRandomCheckpointConfigFromIni(applyRuntimeChanges: false);
            LoadEntityLimitsConfigFromIni(applyRuntimeChanges: false);
            LoadMissionConfigFromIni(applyRuntimeChanges: false);

            // UI selection is authoritative over legacy INI mode switches.
            ApplySelectedGameplaySettingsToLegacyFlags();
            ApplySelectedDifficultySettingsToRuntimeFlags();

            InitializeTeleportGachaPool();

            LoadNpcConfigFromIni();
            LoadBodyguardConfigFromIni();
            LoadAnimalConfigFromIni();
            LoadGiveVehicleConfigFromIni();
            LoadFallingVehiclesConfigFromIni();
            LoadHitByVehicleConfigFromIni();
            LoadUnderwaterConfigFromIni();

            // Runtime timer + webhook sengaja BELUM dimulai di sini.
            // Keduanya baru dibuka setelah startup loading gate memastikan route siap.
            LoadPlayerAndHealthBarConfig();
            LoadDesignConfigFromIni();

            // Cache SEMUA gameplay timestamps sekaligus.
            // Sebelumnya hanya GoToMountainConfig.ini yang dicache, sehingga 1 detik
            // setelah fresh launch script mengira config berubah dan reload ulang.
            _lastGameplayIniWriteTimeUtc = GetGameplayConfigCompositeLastWriteTimeUtc();
            _lastDesignIniWriteTimeUtc = GetConfigLastWriteTimeUtc(GetActiveGameplayHudIniPath());
            _lastHudLayoutIniWriteTimeUtc = GetConfigLastWriteTimeUtc(GetActiveHudLayoutIniPath());
            _lastHudPresetIniWriteTimeUtc = GetConfigLastWriteTimeUtc(GetActiveHudPresetIniPath());
            _lastLoadedHudLayoutName = _hudLayoutName;
            _lastLoadedHudPresetName = _hudPresetName;
            _lastEntityIniWriteTimeUtc = GetConfigLastWriteTimeUtc("scripts/Entity.ini");
            _nextGameplayConfigReloadTime = Game.GameTime + GAMEPLAY_CONFIG_RELOAD_INTERVAL_MS;

            _relMonsterGroup = World.AddRelationshipGroup("TIKTOK_MONSTERS");
            _relBodyguardGroup = World.AddRelationshipGroup("TIKTOK_BODYGUARDS");
            _relAnimalGroup = World.AddRelationshipGroup("TIKTOK_ANIMALS");

            _initialRouteSetupPending = true;
            _nextInitialRouteSetupAttemptTime =
                Game.GameTime + INITIAL_ROUTE_SETUP_DELAY_MS;
        }

        private void SetStartupLoadingProtection(bool enabled)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // Freeze entity hanya untuk mencegah player jatuh/bergerak sementara
                // world tetap berjalan untuk collision dan path streaming.
                Function.Call(Hash.FREEZE_ENTITY_POSITION, player.Handle, enabled);
                Function.Call(Hash.SET_ENTITY_INVINCIBLE, player.Handle, enabled);

                if (player.IsInVehicle())
                {
                    Vehicle vehicle = player.CurrentVehicle;
                    if (vehicle != null && vehicle.Exists())
                    {
                        Function.Call(Hash.FREEZE_ENTITY_POSITION, vehicle.Handle, enabled);
                    }
                }
            }
            catch
            {
            }
        }

        private bool IsStartupGameplayRouteReady()
        {
            if (_initialRouteSetupPending || _routeGenerationPending)
                return false;

            if (_activeFinishPosition == Vector3.Zero)
                return false;

            bool routeNeedsPos = !IsSelectedDirectRouteMode();

            if (routeNeedsPos && _activePosList.Count == 0)
                return false;

            return true;
        }

        private void FinalizeStartupLoadingGate()
        {
            _startupRouteReadySince = 0;

            // Lepaskan freeze/protection startup sebelum gameplay utama berjalan.
            SetStartupLoadingProtection(false);

            // Runtime systems baru hidup setelah route + startup state benar-benar siap.
            StartApocalypseTimer();
            StartHttpServer();

            Function.Call(Hash.SET_TIME_SCALE, 1.0f);

            Tick -= OnStartupLoadingTick;
            Aborted -= OnStartupAborted;

            Tick += OnTick;
            Aborted += OnAborted;
        }

        private void OnStartupLoadingTick(object sender, EventArgs e)
        {
            try
            {
                // If POS failure is active, show error screen and countdown
                if (_posLoadingFailed)
                {
                    int elapsedMs = Game.GameTime - _posFailureStartTime;
                    DrawPosFailureErrorScreen(elapsedMs);
                    SetStartupLoadingProtection(true);

                    // After 3 seconds, return to gameplay selection
                    if (elapsedMs >= 3000)
                    {
                        _posLoadingFailed = false;
                        _selectedGameplayMode = MoonGameplayMode.None;
                        _selectedDifficultyMode = MoonDifficultyMode.None;
                        _startupUiStage = StartupUiStage.Gameplay;
                        
                        // Clear route data
                        _activePosList.Clear();
                        _activeFinishPosition = Vector3.Zero;
                        _currentPosIndex = 0;
                        
                        // Unfreeze and return to UI
                        SetStartupLoadingProtection(false);
                        Function.Call(Hash.SET_TIME_SCALE, 1.0f);
                        Tick -= OnStartupLoadingTick;
                        Tick += OnStartupNameTick;
                    }
                    return;
                }

                // Selalu gambar black screen dahulu supaya frame setup GTA tidak pernah terlihat.
                // ProcessStartupLoadingScreen menjaga TimeScale=1 selama fase ini:
                // world/path streaming jalan, tetapi player tetap tidak dapat bergerak.
                ProcessStartupLoadingScreen();
                SetStartupLoadingProtection(true);

                int currentTime = Game.GameTime;
                int wallTime = Environment.TickCount;

                // Hanya jalankan pekerjaan yang dibutuhkan untuk menyiapkan gameplay.
                // Jangan jalankan seluruh OnTick sebelum route selesai.
                ProcessInitialRouteSetup();
                ProcessPendingRouteGeneration(currentTime);

                if (!IsStartupGameplayRouteReady())
                {
                    _startupRouteReadySince = 0;

                    // Failsafe startup: jika GoToMountain POS belum siap sampai
                    // MaxWaitMs, jangan buka gameplay dengan route yang masih pending.
                    int elapsed = unchecked(wallTime - _startupLoadingStartTime);
                    if (elapsed >= Math.Max(5000, _startupLoadingMaxWaitMs))
                    {
                        // TRIGGER POS FAILURE SCREEN
                        _posLoadingFailed = true;
                        _posFailureStartTime = Game.GameTime;
                    }
                    return;
                }

                if (_startupRouteReadySince <= 0)
                {
                    _startupRouteReadySince = wallTime;
                    return;
                }

                if (unchecked(wallTime - _startupLoadingStartTime) <
                    Math.Max(0, _startupLoadingMinDisplayMs))
                {
                    return;
                }

                if (unchecked(wallTime - _startupRouteReadySince) <
                    Math.Max(0, _startupLoadingReadyHoldMs))
                {
                    return;
                }

                FinalizeStartupLoadingGate();
            }
            catch (Exception ex)
            {
                LogError("STARTUP LOADING ERROR", ex);

                // FAIL-OPEN: startup loading tidak boleh mengunci layar hitam.
                // Jika generator route melempar exception, buka gameplay utama
                // dan biarkan OnTick melanjutkan retry route secara incremental.
                try
                {
                    ResetIncrementalRouteBuildState();
                    ScheduleRouteGeneration(250);
                }
                catch { }

                try
                {
                    FinalizeStartupLoadingGate();
                }
                catch
                {
                    try { SetStartupLoadingProtection(false); } catch { }
                    try { Function.Call(Hash.SET_TIME_SCALE, 1.0f); } catch { }
                    try { Tick -= OnStartupLoadingTick; } catch { }
                    try { Tick += OnTick; } catch { }
                }
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            // =========================================================================
            // 1. SEMBUNYIKAN HUD BAWAAN GTA
            // =========================================================================
            Function.Call(
                Hash.HIDE_HUD_COMPONENT_THIS_FRAME,
                3
            );

            int currentTime =
                Game.GameTime;

            // Player selalu bersih dari darah/luka visual dan tidak bersuara.
            MaintainCleanSilentPlayer();

            ProcessProfileHostPersistence(
                currentTime
            );

            // Nama yang dipilih saat startup ditampilkan di atas kepala player.
            // Design name-tag hardcoded dan tidak bergantung pada file .ini.
            DrawEnteredPlayerNameAboveHead();

            ProcessScheduledDespawns(
                currentTime
            );

            // Cek perubahan file config sekitar setiap 1 detik.
            // File hanya dibaca ulang jika timestamp-nya berubah.
            ProcessRuntimeGameplayConfig(
                currentTime
            );

            // Normal = NEVER WANTED.
            // Gift /wanted_5_star = 5 bintang selama 5 menit atau sampai mati.
            ProcessNeverWantedSystem();

            // =========================================================================
            // 2. PROSES WEBHOOK ACTION
            // =========================================================================
            int processedActions = 0;

            // Maksimal unit action per Tick.
            // Satu batch besar diproses bertahap dan dikembalikan ke ekor queue,
            // sehingga endpoint lain tetap mendapat giliran (round-robin).
            const int maxActionsPerTick = 20;

            while (
                processedActions < maxActionsPerTick &&
                _mainThreadQueue.TryDequeue(
                    out BatchedMainThreadAction batch
                )
            )
            {
                Action actionToRun = null;

                lock (batch.SyncRoot)
                {
                    if (batch.PendingCount > 0)
                    {
                        batch.PendingCount--;
                        actionToRun = batch.Action;
                    }
                    else
                    {
                        batch.IsQueued = false;
                    }
                }

                if (actionToRun == null)
                {
                    continue;
                }

                try
                {
                    actionToRun.Invoke();
                }
                catch (Exception ex)
                {
                    LogError(
                        "ACTION ERROR",
                        ex
                    );

                    ShowHudNotification(
                        "~r~[ACTION ERROR]~w~ " +
                        ex.Message,
                        false
                    );
                }
                finally
                {
                    lock (batch.SyncRoot)
                    {
                        if (batch.PendingCount > 0)
                        {
                            // Masih ada gift/action dengan key yang sama.
                            // Taruh kembali ke belakang agar endpoint lain tidak kelaparan.
                            _mainThreadQueue.Enqueue(batch);
                        }
                        else
                        {
                            batch.IsQueued = false;
                        }
                    }
                }

                processedActions++;
            }

            // =========================================================================
            // 3. PLAYER / ROUTE LOGIC
            // =========================================================================
            // Saat script pertama aktif, posisi player sekarang menjadi START.
            // Tidak ada teleport awal.
            ProcessInitialRouteSetup();

            ProcessPlayerAnimalTransform(
                currentTime
            );

            CheckUnderwaterStatus(
                currentTime
            );

            // Custom HP membaca damage GTA lalu langsung mengembalikan
            // internal GTA HP ke buffer aman.
            ApplyPlayerHealth();

            ProcessCustomDeathSystem(
                currentTime
            );

            // Setelah custom death selesai, overlay masih memudar 700ms.
            if (!_customDeathActive &&
                _customDeathOverlayFadeInEndTime > 0 &&
                currentTime <
                    _customDeathOverlayFadeInEndTime)
            {
                DrawCustomDeathBlackOverlay(
                    currentTime,
                    false,
                    0
                );
            }
            else if (_customDeathOverlayFadeInEndTime > 0 &&
                     currentTime >=
                        _customDeathOverlayFadeInEndTime)
            {
                _customDeathOverlayFadeInStartTime =
                    0;

                _customDeathOverlayFadeInEndTime =
                    0;
            }

            ProcessPendingRouteGeneration(
                currentTime
            );

            ProcessRouteLogic();

            // Random Checkpoint membaca progress route SETELAH route logic,
            // sehingga pergantian POS langsung menghasilkan leg/marker baru.
            ProcessRandomCheckpointSystem(
                currentTime
            );

            // =========================================================================
            // 4. HUD DASAR
            // =========================================================================
            // Saat custom death / black screen:
            // SEMUA HUD script disembunyikan.
            // Countdown RESPAWN IN... digambar langsung oleh
            // ProcessCustomDeathSystem().
            if (!IsNormalHudSuppressed())
            {
                DrawDeathCounter();

                DrawRandomCheckpointUI(
                    currentTime
                );
            }

            // =========================================================================
            // 5. KONTROL PLAYER
            // =========================================================================
            DisableCharacterSwitching();

            if (_selectedGameplayMode == MoonGameplayMode.Survival)
            {
                EnforceSurvivalNoVehicle();
                DrawSurvivalNoVehicleWarning();
            }

            // GoToMountain tetap memakai aturan vehicle lama; Survival tidak boleh kendaraan.
            // Tidak ada lagi filter class / pembatasan enter vehicle.

            // =========================================================================
            // 6. RANDOM CHAOS TIMER / ROULETTE / SURVIVAL
            // =========================================================================

            ProcessCountdownLogic(
                currentTime
            );

            // Kalau timer 60 menit habis, roulette berjalan di sini.
            ProcessChaosRouletteLogic(
                currentTime
            );

            // Kalau hasil roulette sudah LOCK, effect berjalan sesuai RandomChaosXDuration.
            ProcessChaosSurvivalLogic(
                currentTime
            );

            if (!IsNormalHudSuppressed())
            {
                DrawCountdownUI(
                    currentTime
                );
            }

            // =========================================================================
            // 7. STATUS / TELEPORT GACHA
            // =========================================================================
            if (!IsNormalHudSuppressed())
            {
                DrawActiveStatusHUD(
                    currentTime
                );

                ProcessTeleportGachaLogic(
                    currentTime
                );
            }

            // Random Gatcha logic tetap berjalan walaupun HUD sedang disembunyikan.
            // Draw function-nya sendiri mengikuti IsNormalHudSuppressed().
            ProcessRandomGatchaLogic(
                currentTime
            );

            // Safe-global Random Teleport harus tetap diproses walaupun HUD
            // sedang disembunyikan. Player baru dipindahkan setelah ground valid.
            ProcessSafeRandomTeleportLogic(
                currentTime
            );

            ProcessRandomTeleportGroundGuard(
                currentTime
            );

            // =========================================================================
            // =========================================================================
            ApplyPlayerWeaponWhitelist();

            if (!IsNormalHudSuppressed())
            {
                DrawPlayerHealthBar();
            }

            // =========================================================================
            // 9. UPDATE ENTITY AKTIF
            //
            // Saat custom death:
            // seluruh combat enemy + bodyguard dipause.
            // Setelah respawn, AI dilepas dan otomatis menyerang lagi.
            // =========================================================================
            ProcessCustomDeathEnemyPauseState();

            ProcessActiveBodyguards();

            ProcessActiveBosses();

            ProcessActiveAnimals();

            // =========================================================================
            // 10. PROSES SPAWN BARU
            // =========================================================================
            ProcessFallingVehicleSpawns(
                currentTime
            );

            ProcessActiveFallingVehicles(
                currentTime
            );

            ProcessVehicleGiftSpawns();

            ProcessHitByVehicleSpawns();

            ProcessHitByVehicleForceLaunches(
                currentTime
            );

            // Jangan selesaikan spawn enemy baru selama custom death.
            // Queue tetap ada dan otomatis diproses setelah respawn.
            if (!_customDeathActive)
            {
                ProcessEnemySpawns();

                ProcessPassengerSpawns();
            }

            // =========================================================
            // MONSTER VEHICLE CLEANUP
            //
            // Setelah semua passenger selesai diproses,
            // cek apakah seluruh pasukan vehicle sudah mati.
            // Kalau sudah, jadwalkan vehicle hilang 3 detik kemudian.
            // =========================================================
            ProcessEnemyVehicleCleanup(
                currentTime
            );

            ProcessBodyguardSpawns();

            ProcessBlackHoleSpawn();

            if (!_customDeathActive)
            {
                ProcessAnimalSpawns();
            }

            // =========================================================================
            // 12. EFFECT / TIMER AKTIF
            // =========================================================================
            ProcessActiveTimers(
                currentTime
            );
        }

        private void OnAborted(object sender, EventArgs e)
        {
            SaveCurrentHostProfileStats();

            Function.Call(
                Hash.SET_MAX_WANTED_LEVEL,
                5
            );

            Function.Call(
                Hash.SET_POLICE_IGNORE_PLAYER,
                Game.Player.Handle,
                false
            );

            // Pastikan earthquake tidak meninggalkan camera shake
            // setelah script direload/stop.
            StopEarthquakeGift();

            _pendingBlackholeDurationMs =
                0;

            _pendingEarthquakeDurationMs =
                0;

            DeleteAllScheduledDespawns();

            StopRockRainGift();

            // Pastikan vehicle enemy tidak tertinggal handbrake jika script
            // direload ketika player sedang custom death.
            ResumeAllCombatAfterCustomDeath();

            _enemyCombatPausedForCustomDeath =
                false;

            Ped abortPlayer =
                Game.Player.Character;

            if (abortPlayer != null &&
                abortPlayer.Exists() &&
                _customDeathActive)
            {
                abortPlayer.IsInvincible =
                    false;
            }

            _customDeathOverlayFadeInStartTime =
                0;

            _customDeathOverlayFadeInEndTime =
                0;

            _customDeathHudResumeTime =
                0;

            // =========================================================
            // STOP SCRIPT / WEBHOOK
            // =========================================================
            _isRunning = false;

            if (_listener != null)
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }

                _listener.Close();
            }

            // =========================================================
            // RESET EFFECT PLAYER / CHAOS
            // =========================================================
            RestorePlayerAnimalTransformOnAbort();

            CancelChaosMode();
            CleanupChaosCage();
            CleanupGiftCage();

            _giftDisarmEndTime = 0;

            bool sleepWasActive =
                _giftSleepEndTime > 0;

            _giftSleepEndTime =
                0;

            _giftSleepVehicleBailoutPending =
                false;

            _giftSleepVehicleBailoutRetryTime =
                0;

            if (sleepWasActive &&
                abortPlayer != null &&
                abortPlayer.Exists() &&
                !_customDeathActive)
            {
                Function.Call(
                    Hash.CLEAR_PED_TASKS,
                    abortPlayer.Handle
                );
            }

            ResetSuperSpeed();
            ResetInvincible();

            CleanupStandaloneWebhookEffects();

            CleanupBlackHoleProp();

            for (
                int i =
                    _vehiclesToGive.Count - 1;
                i >= 0;
                i--
            )
            {
                VehicleLoadingTracker tracker =
                    _vehiclesToGive[i];

                if (tracker.Model.IsValid)
                {
                    tracker.Model.MarkAsNoLongerNeeded();
                }
            }

            _vehiclesToGive.Clear();

            for (
                int i =
                    _hitByVehiclesToSpawn.Count - 1;
                i >= 0;
                i--)
            {
                HitByVehicleLoadingTracker tracker =
                    _hitByVehiclesToSpawn[i];

                if (tracker.Model.IsValid)
                {
                    tracker.Model.MarkAsNoLongerNeeded();
                }
            }

            _hitByVehiclesToSpawn.Clear();

            _hitByVehicleForceTrackers.Clear();

            // =========================================================
            // HAPUS BODYGUARD AKTIF
            // =========================================================
            for (int i = _activeBodyguards.Count - 1; i >= 0; i--)
            {
                ActiveBodyguardTracker bg =
                    _activeBodyguards[i];

                if (bg.BodyguardPed != null &&
                    bg.BodyguardPed.Exists())
                {
                    bg.BodyguardPed.Delete();
                }
            }

            _activeBodyguards.Clear();

            // =========================================================
            // HAPUS ENEMY AKTIF
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

            // Hapus juga vehicle asal yang tetap ditrack walaupun driver atau
            // passenger sudah keluar/terpental. Ini mencegah orphan vehicle
            // tertinggal setelah script reload/abort.
            for (int i = _enemyVehicles.Count - 1; i >= 0; i--)
            {
                Vehicle enemyVehicle =
                    _enemyVehicles[i];

                if (enemyVehicle != null &&
                    enemyVehicle.Exists())
                {
                    enemyVehicle.Delete();
                }
            }

            _enemyVehicles.Clear();

            // =========================================================
            // HAPUS ANIMAL AKTIF
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
            // BODYGUARD MASIH LOADING
            // =========================================================
            for (int i = _bodyguardsToSpawn.Count - 1; i >= 0; i--)
            {
                BodyguardLoadingTracker tracker =
                    _bodyguardsToSpawn[i];

                if (tracker.Model.IsValid)
                {
                    tracker.Model.MarkAsNoLongerNeeded();
                }
            }

            _bodyguardsToSpawn.Clear();

            // =========================================================
            // ANIMAL MASIH LOADING
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
            // PASSENGER MASIH LOADING
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
            // ENEMY MASIH LOADING
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
            // FALLING VEHICLES MASIH LOADING
            // =========================================================
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

            // =========================================================
            // FALLING VEHICLES YANG MASIH AKTIF
            // =========================================================
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
                    if (
                        tracker != null &&
                        tracker.Vehicle != null &&
                        tracker.Vehicle.Exists()
                    )
                    {
                        tracker.Vehicle.Delete();
                    }
                }
                catch
                {
                }
            }

            _activeFallingVehicles.Clear();

            _fallingVehiclesEffectEndTime =
                0;

            _nextFallingVehicleActualSpawnTime =
                0;

            // =========================================================
            // BUANG MAIN THREAD ACTION YANG MASIH ANTRE
            // =========================================================
            while (_mainThreadQueue.TryDequeue(
                out BatchedMainThreadAction pendingAction))
            {
                // Buang semua pending batch dari queue fisik.
            }

            foreach (BatchedMainThreadAction batch in
                _mainThreadBatches.Values)
            {
                lock (batch.SyncRoot)
                {
                    batch.PendingCount = 0;
                    batch.IsQueued = false;
                }
            }

            _mainThreadBatches.Clear();

            // =========================================================
            // HAPUS ROUTE BLIP
            // =========================================================
            for (int i = _routeBlips.Count - 1; i >= 0; i--)
            {
                Blip blip =
                    _routeBlips[i];

                if (blip != null &&
                    blip.Exists())
                {
                    blip.Delete();
                }
            }

            _routeBlips.Clear();
        }

    }
}
