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
    public class CustomNpcConfig
    {
        public string WebhookName { get; set; }
        public string DefaultName { get; set; }

        public List<string> PedModels { get; set; } =
            new List<string>();

        public List<string> PedModelDisplayNames { get; set; } =
            new List<string>();

        // =========================================================
        // BASIC
        // =========================================================
        public int Health { get; set; } = 100;


        // =========================================================
        // WEAPON
        // =========================================================
        public WeaponHash Weapon { get; set; }
        public int Accuracy { get; set; }
        public bool WeaponNoReload { get; set; }

        // WeaponAmmo dipakai Bodyguard.
        // Enemy tetap hardcoded 9999 di C#.
        public int WeaponAmmo { get; set; } = 9999;

        public int ShootRate { get; set; } = 1000;

        // =========================================================
        // DAMAGE / PROTECTION
        // =========================================================
        public bool OnlyDamagedByPlayer { get; set; }

        public bool BulletProof { get; set; } = false;
        public bool FireProof { get; set; }
        public bool ExplosionProof { get; set; }
        public bool CollisionProof { get; set; } = false;
        public bool MeleeProof { get; set; } = false;

        // =========================================================
        // CRITICAL HIT / RAGDOLL
        // =========================================================
        public bool CanSufferCriticalHits { get; set; } = false;
        public bool CanRagdoll { get; set; } = true;
        public bool RagdollFromPlayerImpact { get; set; } = false;

        // =========================================================
        // COMBAT AI
        // =========================================================
        public int CombatAbility { get; set; } = 2;
        public int CombatRange { get; set; } = 2;
        public int CombatMovement { get; set; } = 2;

        public float SeeingRange { get; set; } = 0.0f;
        public float HearingRange { get; set; } = 0.0f;

        public bool AttackPlayer { get; set; } = true;
        public bool CanFlee { get; set; } = false;

        // =========================================================
        // BODYGUARD
        // =========================================================
        public float TargetSearchRadius { get; set; } = 150.0f;

        public bool FollowPlayer { get; set; } = true;
        public bool NeverLeavePlayerGroup { get; set; } = true;

        // =========================================================
        // ANIMAL
        // =========================================================
        public float MoveRate { get; set; } = 1.0f;

        // =========================================================
        // SPAWN / RESPAWN
        // =========================================================
        public float SpawnDistance { get; set; } = 3.0f;
        public float RespawnDistance { get; set; } = 120.0f;
        // =========================================================
        // UI
        // =========================================================
        public bool ShowName { get; set; } = true;
        public bool ShowHealthBar { get; set; } = true;

        // =========================================================
        // BLIP
        // =========================================================
        public bool UseBlip { get; set; }
        public int BlipColorId { get; set; }
        public float BlipScale { get; set; }

        // =========================================================
        // VEHICLE / PASSENGER
        // =========================================================
        public string VehicleModel { get; set; }
        public string ModelDriver { get; set; }
        public string ModelPassenger { get; set; }
        public string ModelRightRear { get; set; }
        public string ModelLeftRear { get; set; }
    }

    public partial class MoonMod
    {

        // =========================================================================
        // SHVDN V3 MODERN API COMPATIBILITY HELPERS
        // Keeps existing gameplay behavior while avoiding obsolete API warnings.
        // =========================================================================
        // =========================================================================
        // 2. KELAS BANTUAN (TRACKERS & DATA ITEMS)
        // =========================================================================

        public class TeleportLocationItem
        {
            public string DisplayName { get; set; }
            public Vector3 TargetPosition { get; set; }
            public float? ManualZ { get; set; }
        }

        private class RandomChaosOption
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }
            public int DurationMs { get; set; }
        }

        private class RandomGatchaOption
        {
            public string Key { get; set; }
            public string DisplayName { get; set; }

            // 0 = effect one-shot / tidak memakai timer.
            // >0 = durasi khusus jika hasil RANDOM_GATCHA ini menang.
            public int DurationMs { get; set; }
        }

        private class HostProfile
        {
            public string SectionName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Score { get; set; } = 0;
            public int Wins { get; set; } = 0;
            public int Deaths { get; set; } = 0;
            public int EnemyKills { get; set; } = 0;
        }

        private int _playerMaxHealth = 10000;


        // =========================================================
        // Default dapat diedit dari:
        // [GIVE_HEALTH]
        // HealthAmount=2500
        // Health-only gift.
        // =========================================================
        private int _giveHealthAmount = 2500;


        // HUD instant card memakai stack yang sama dengan SUPERSPEED.
        private int _giveHealthShown = 0;


        // =========================================================
        // CUSTOM HEALTH / CUSTOM DEATH
        // =========================================================
        // HP gameplay tidak lagi bergantung pada HP mati bawaan GTA.
        // HP GTA hanya dipakai sebagai SENSOR DAMAGE lalu langsung
        // dipulihkan ke buffer besar agar native death/WASTED tidak terjadi.
        private int _customHealth = 10000;


        private bool _isPlayerHealthInitialized = false;

        private const int GTA_DEATH_GUARD_HEALTH = 30000;
        private int _lastGtaHealth = GTA_DEATH_GUARD_HEALTH;


        private bool _customDeathActive = false;
        private int _customDeathStartTime = 0;
        private int _customDeathEndTime = 0;
        private bool _customDeathFadeStarted = false;

        // Posisi saat custom death pertama kali terjadi.
        // Respawn TIDAK lagi memakai posisi player di udara saat countdown selesai.
        private Vector3 _customDeathWorldPosition =
            Vector3.Zero;

        private float _customDeathHeading =
            0.0f;

        // Setelah respawn, beri waktu singkat supaya BLACKHOLE yang masih aktif
        // tidak langsung menarik player kembali ke udara pada tick yang sama.
        private int _customDeathGroundProtectionEndTime =
            0;

        // Semua HUD/UI script disembunyikan selama custom death
        // dan sampai fade-in selesai. Yang tetap tampil hanya:
        //   RESPAWN IN 5 / 4 / 3 / 2 / 1
        private int _customDeathHudResumeTime = 0;

        // Manual black overlay dipakai menggantikan native screen fade.
        // Ini penting supaya text RESPAWN IN... bisa dirender DI ATAS layar hitam.
        private int _customDeathOverlayFadeInStartTime = 0;
        private int _customDeathOverlayFadeInEndTime = 0;

        // =========================================================
        // CUSTOM DEATH: PAUSE ENEMY COMBAT
        // =========================================================
        // Karena custom death tidak memakai native player.IsDead,
        // enemy GTA masih menganggap player valid untuk diserang.
        // State ini membuat seluruh combat AI berhenti selama countdown
        // dan aktif lagi otomatis setelah player respawn.
        private bool _enemyCombatPausedForCustomDeath = false;

        // Throttle fallback native revive supaya tidak pernah resurrect
        // berkali-kali dalam setiap frame jika GTA masih transisi death.
        private int _nextNativeDeathRecoveryTime = 0;
        private const int NATIVE_DEATH_RECOVERY_INTERVAL_MS = 500;

        // Custom death timing:
        // 0.9s  = player jatuh / ragdoll dulu
        // 0.5s  = fade menuju hitam
        // 5.0s  = layar hitam + countdown RESPAWN IN 5..1
        private const int CUSTOM_DEATH_FADE_DELAY_MS = 900;
        private const int CUSTOM_DEATH_FADE_DURATION_MS = 500;
        private const int CUSTOM_RESPAWN_COUNTDOWN_SECONDS = 5;
        private const int CUSTOM_DEATH_DURATION_MS =
            CUSTOM_DEATH_FADE_DELAY_MS +
            CUSTOM_DEATH_FADE_DURATION_MS +
            (CUSTOM_RESPAWN_COUNTDOWN_SECONDS * 1000);

        private int _healthRegenAmount = 100;     // Jumlah darah bertambah tiap tick (+100 HP)
        private int _healthRegenDelay = 5000;     // Jeda/cooldown setelah kena damage (5 detik)
        private int _healthRegenInterval = 500;   // Kecepatan regen (tiap 0,5 detik)
        private int _lastHealthDamageTime = 0;
        private int _lastHealthRegenTick = 0;
        // Jeda/cooldown setelah kena tembak sebelum regen mulai (5000ms = 5 detik)



        // Konfigurasi HUD Health Bar (Tengah Bawah)
        private float _hpBarX = 0.50f;
        private float _hpBarY = 0.93f;
        private float _hpBarWidth = 0.22f;
        private float _hpBarHeight = 0.035f;

        private int _hpBgR = 20, _hpBgG = 20, _hpBgB = 20, _hpBgA = 220;
        private int _hpFillR = 200, _hpFillG = 30, _hpFillB = 30, _hpFillA = 230;


        private float _hpTextScale = 0.45f;
        private int _hpTextFont = 4;
        private int _hpTextR = 255, _hpTextG = 255, _hpTextB = 255, _hpTextA = 255;
        private string _hpTextFormat = "{0} / {1} HP";


        // Variabel Konfigurasi HUD Death
        private string _deathLabel = "DEATHS : {0}";
        private string _deathSubtitle = string.Empty;
        private float _deathSubtitleScale = 0.30f;
        private int _deathSubtitleR = 155, _deathSubtitleG = 175, _deathSubtitleB = 200, _deathSubtitleA = 255;
        private float _deathPosX = 0.23f;
        private float _deathPosY = 0.035f;
        private float _deathScale = 0.65f;
        private int _deathR = 255, _deathG = 60, _deathB = 60, _deathA = 255;
        private int _deathFont = 4;

        // Variabel Konfigurasi HUD Win
        private string _winLabel = "WINS : {0}";
        private string _winSubtitle = string.Empty;
        private float _winSubtitleScale = 0.30f;
        private int _winSubtitleR = 155, _winSubtitleG = 175, _winSubtitleB = 200, _winSubtitleA = 255;
        private float _winPosX = 0.77f;
        private float _winPosY = 0.035f;
        private float _winScale = 0.65f;
        private int _winR = 60, _winG = 255, _winB = 60, _winA = 255;
        private int _winFont = 4;

        // Variabel Background HUD Death
        private float _deathBgWidth = 0.14f;
        private float _deathBgHeight = 0.045f;
        private int _deathBgR = 20, _deathBgG = 20, _deathBgB = 20, _deathBgA = 200;

        // Variabel Background HUD Win
        private float _winBgWidth = 0.14f;
        private float _winBgHeight = 0.045f;
        private int _winBgR = 20, _winBgG = 20, _winBgB = 20, _winBgA = 200;

        // Variabel Konfigurasi HUD Progress Bar
        private float _barX = 0.50f;
        private float _barY = 0.038f;
        private float _barWidth = 0.28f;
        private float _barHeight = 0.052f;

        private int _bgR = 20, _bgG = 20, _bgB = 20, _bgA = 240;
        private int _fillR = 0, _fillG = 210, _fillB = 255, _fillA = 230;

        private float _textMeterScale = 0.65f;
        private float _textMeterOffsetY = 0.034f;
        private int _textMeterFont = 4;
        private int _textMeterR = 255, _textMeterG = 255, _textMeterB = 255, _textMeterA = 255;
        // Variabel Konfigurasi HUD Apocalypse
        private float _apocalypsePosX = 0.10f;
        private float _apocalypsePosY = 0.14f;
        private float _apocalypseScale = 0.85f;
        private int _apocalypseR = 241, _apocalypseG = 196, _apocalypseB = 15, _apocalypseA = 255;
        private int _apocalypseFont = 7;

        // Variabel Background HUD Apocalypse
        private float _apocalypseBgWidth = 0.15f;
        private float _apocalypseBgHeight = 0.090f;
        private int _apocalypseBgR = 10, _apocalypseBgG = 10, _apocalypseBgB = 10, _apocalypseBgA = 180;

        // =========================================================
        // MODERN TOP HUD THEME
        // Dipakai bersama oleh WINS / DEATHS / RANDOM CHAOS.
        // Dibuat sengaja sama dengan tema panel MISSION: putih,
        // text gelap, divider tipis, shadow gelap, sudut tegas.
        // =========================================================
        private const int MODERN_HUD_TEXT_R = 245;
        private const int MODERN_HUD_TEXT_G = 248;
        private const int MODERN_HUD_TEXT_B = 252;
        private const int MODERN_HUD_TEXT_A = 255;

        private const int MODERN_HUD_DIVIDER_R = 130;
        private const int MODERN_HUD_DIVIDER_G = 143;
        private const int MODERN_HUD_DIVIDER_B = 160;
        private const int MODERN_HUD_DIVIDER_A = 175;

        private const int MODERN_HUD_TRACK_R = 58;
        private const int MODERN_HUD_TRACK_G = 68;
        private const int MODERN_HUD_TRACK_B = 84;
        private const int MODERN_HUD_TRACK_A = 255;

        private const float MODERN_HUD_SHADOW_OFFSET_X = 0.0035f;
        private const float MODERN_HUD_SHADOW_OFFSET_Y = 0.0050f;
        private const float MODERN_HUD_SHADOW_EXTRA_WIDTH = 0.0060f;
        private const float MODERN_HUD_SHADOW_EXTRA_HEIGHT = 0.0080f;
        private const int MODERN_HUD_SHADOW_A = 105;

        private const float MODERN_HUD_ACCENT_WIDTH = 0.0045f;
        private const float MODERN_HUD_PROGRESS_HEIGHT = 0.0100f;
        private class ActiveBossTracker
        {
            public Ped BossPed { get; set; }
            public string NpcName { get; set; }
            public CustomNpcConfig Config { get; set; }

            // Vehicle asal enemy.
            // Tetap tersimpan walaupun NPC keluar / terpental dari vehicle.
            public Vehicle AssignedVehicle { get; set; }

            // Handle kendaraan disimpan permanen agar satu vehicle tetap
            // dikenali sebagai satu unit walaupun driver/passenger terpental.
            public int AssignedVehicleHandle { get; set; } = 0;

            // Posisi terakhir unit. Dipakai sebagai fallback jika entity
            // tiba-tiba invalid saat player sudah berpindah jauh.
            public Vector3 LastKnownPosition { get; set; } = Vector3.Zero;

            // Waktu pergantian fase driver
            public int NextVehicleTaskRefreshTime { get; set; } = 0;

            // false = sedang mengejar / nabrak
            // true  = sedang combat / menembak
            public bool IsVehicleShootPhase { get; set; } = false;

            // Debounce respawn agar enemy tidak "menghilang" karena satu frame
            // distance/streaming spike.
            public int OutOfRangeSinceTime { get; set; } = 0;
            public int InvalidSinceTime { get; set; } = 0;
            public int LastGroundRecoveryTime { get; set; } = 0;

        }
        private class ActiveBodyguardTracker
        {
            public Ped BodyguardPed { get; set; }
            public string NpcName { get; set; }
            public CustomNpcConfig Config { get; set; }

            public int NextCombatCheckTime { get; set; } = 0;

            // Target yang MEMANG dipilih oleh script.
            // Bukan target random dari AI GTA.
            public int CurrentTargetHandle { get; set; } = 0;
        }
        private class ActiveAnimalTracker { public Ped AnimalPed { get; set; } public string AnimalName { get; set; } public CustomNpcConfig Config { get; set; } }
        private class EnemyLoadingTracker
        {
            public CustomNpcConfig Config { get; set; }
            public Ped Player { get; set; }

            public Vector3 SpawnPosition { get; set; }
            public float Heading { get; set; }

            public string SelectedModelName { get; set; }
            public string SelectedNpcName { get; set; }

            public Model PedModel { get; set; }

            public Model? VehicleModel { get; set; }

            // true jika spawn ini berasal dari sistem RespawnDistance.
            public bool IsRespawn { get; set; } = false;

            // Retry jika CreatePed / CreateVehicle gagal sesaat.
            public int SpawnAttemptCount { get; set; } = 0;

            public int RequestStartTime { get; set; } =
                Game.GameTime;
        }

        private class PassengerLoadingTracker
        {
            public Model Model { get; set; }
            public Vehicle Vehicle { get; set; }
            public VehicleSeat Seat { get; set; }
            public CustomNpcConfig Config { get; set; }
            public Ped Player { get; set; }

            public int RequestStartTime { get; set; } = Game.GameTime;
        }

        private class BodyguardLoadingTracker
        {
            public Model Model { get; set; }
            public CustomNpcConfig Config { get; set; }
            public Ped Player { get; set; }
            public Vector3 SpawnPosition { get; set; }
            public float Heading { get; set; }

            public int RequestStartTime { get; set; } = Game.GameTime;
        }

        private class AnimalLoadingTracker
        {
            public Model Model { get; set; }
            public CustomNpcConfig Config { get; set; }
            public Ped Player { get; set; }

            public Vector3 SpawnPosition { get; set; }
            public float Heading { get; set; }

            public string SelectedModelName { get; set; }
            public string SelectedAnimalName { get; set; }

            public int RequestStartTime { get; set; } =
                Game.GameTime;
        }

        private class BlackHoleLoadingTracker
        {
            public Model Model { get; set; }

            public int RequestStartTime { get; set; } = Game.GameTime;
        }

        private class ModelLoadingTracker
        {
            public Model Model { get; set; }
            public Ped Player { get; set; }
            public float Heading { get; set; }
            public uint ModelHash { get; set; }

            // Falling Vehicle tidak harus langsung spawn saat model selesai load.
            // Nilai ini menentukan kapan slot kendaraan tersebut boleh terjun.
            public int SpawnNotBeforeTime { get; set; } = Game.GameTime;

            // Interval spawn disimpan PER TRACKER.
            // Ini penting supaya Random Gatcha / Chaos / Checkpoint yang memakai
            // Duration override tetap mempertahankan spacing miliknya sendiri.
            public int SpawnIntervalMs { get; set; } = 1;

            public int RequestStartTime { get; set; } = Game.GameTime;
        }

        private class FallingVehicleActiveTracker
        {
            public Vehicle Vehicle { get; set; }

            // Dipakai untuk memastikan kendaraan memang sudah sempat berada
            // di udara sebelum dianggap menyentuh tanah.
            public bool WasInAir { get; set; } = false;

            public int SpawnTime { get; set; } = Game.GameTime;
        }

        private class VehicleLoadingTracker
        {
            public Model Model { get; set; }
            public Ped Player { get; set; }
            public float Heading { get; set; }
            public string VehicleName { get; set; }

            public int RequestStartTime { get; set; } = Game.GameTime;
        }

        private class HitByVehicleLoadingTracker
        {
            public Model Model { get; set; }
            public Ped Player { get; set; }
            public string VehicleModelName { get; set; }

            public int RequestStartTime { get; set; } =
                Game.GameTime;
        }

        private class HitByVehicleForceTracker
        {
            public Vehicle Vehicle { get; set; }

            // Arah projectile dikunci saat spawn.
            // Tidak homing ke player.
            public Vector3 Direction { get; set; }

            public float Speed { get; set; }

            public int EndTime { get; set; }
        }

        // =========================================================
        // SNAPSHOT PLAYER SEBELUM TRANSFORM MENJADI ANIMAL
        // =========================================================
        private class PlayerAnimalTransformSnapshot
        {
            public int OriginalModelHash { get; set; }
            public int Health { get; set; }

            public WeaponHash SelectedWeapon { get; set; } = WeaponHash.Unarmed;

            public readonly List<WeaponHash> Weapons =
                new List<WeaponHash>();

            public readonly List<int> WeaponAmmo =
                new List<int>();

            public readonly int[] ComponentDrawable = new int[12];
            public readonly int[] ComponentTexture = new int[12];
            public readonly int[] ComponentPalette = new int[12];

            public readonly int[] PropIndex = new int[8];
            public readonly int[] PropTexture = new int[8];
        }

        private class RockRainBallProfile
        {
            public string ModelName { get; set; }
            public float GroundOffset { get; set; }
            public float ImpactRadius { get; set; }
            public int Damage { get; set; }
            public float ImpactForce { get; set; }
        }

        private class RockRainPropTracker
        {
            public Prop Rock { get; set; }

            // Hanya untuk berapa lama MAIN SCRIPT masih memantau
            // ground physics / anti-tunneling. Ini BUKAN timer despawn.
            public int PhysicsTrackingEndTime { get; set; }

            public float GroundZ { get; set; }
            public bool HasHitGround { get; set; } = false;

            public RockRainBallProfile Profile { get; set; }
        }

        private class ScheduledDespawnTracker
        {
            public Entity Entity { get; set; }
            public int EntityHandle { get; set; }
            public int DeleteAtTime { get; set; }
        }
        // =========================================================================
        // 3. VARIABEL STATE GLOBAL
        // =========================================================================

        // =========================================================
        // CORE / SERVER
        // =========================================================
        private HttpListener _listener;
        private bool _isRunning = true;
        private int _webhookPort = 6722;

        // =========================================================
        // STARTUP PLAYER NAME - DESIGN DARI MoonHUD.ini
        // =========================================================
        // Alur:
        //   HWID valid -> INPUT NAMA -> ENTER -> gameplay utama baru diinisialisasi.
        //
        // Gameplay input tetap di C#.
        // Semua tampilan fitur ini dibaca dari [HUD_PLAYER_SETUP].
        // Custom text input: TIDAK memakai keyboard native GTA, jadi label "Name"
        // bawaan tidak muncul dan seluruh tampilan bisa dikustom dari .ini.
        private bool _startupNameConfirmed = false;
        private string _startupPlayerName = string.Empty;
        private string _startupNameDraft = string.Empty;

        // Database profile host terpisah dari MoonHUD.ini.
        // Nama dropdown + statistik awal dibaca dari scripts/ProfileHost.ini.
        private const string PROFILE_HOST_INI_PATH =
            "scripts/ProfileHost.ini";

        private readonly List<HostProfile> _hostProfiles =
            new List<HostProfile>();

        private int _selectedHostProfileIndex = -1;
        private bool _profileHostDirty = false;
        private int _nextProfileHostSaveTime = 0;
        // Debounce autosave: maksimal sekitar 1 detik setelah perubahan terakhir/dirty.
        // Save paksa juga dilakukan saat script Aborted/reload.
        private const int PROFILE_HOST_SAVE_INTERVAL_MS = 1000;

        // Mouse dropdown name selector. Tidak ada input ketik manual.
        private readonly List<string> _startupNameOptions =
            new List<string>();
        private bool _startupNameDropdownOpen = false;
        private int _startupNameDropdownScrollOffset = 0;
        private int _startupNameDropdownMaxVisible = 6;
        private float _startupNameDropdownRowHeight = 0.052f;
        private float _startupNameDropdownGapY = 0.004f;
        private string _startupNameDropdownArrow = "V";

        // Tombol START berada di kanan bawah panel profile.
        private string _startupStartButtonText = "NEXT";
        private float _startupStartButtonX = 0.655f;
        private float _startupStartButtonY = 0.720f;
        private float _startupStartButtonWidth = 0.250f;
        private float _startupStartButtonHeight = 0.078f;
        private float _startupStartButtonGap = 0.016f; // legacy compatibility
        private float _startupStartButtonTextScale = 0.62f;
        private int _startupStartButtonR = 30;
        private int _startupStartButtonG = 72;
        private int _startupStartButtonB = 94;
        private int _startupStartButtonA = 255;
        private int _startupStartButtonDisabledR = 24;
        private int _startupStartButtonDisabledG = 29;
        private int _startupStartButtonDisabledB = 36;
        private int _startupStartButtonDisabledA = 235;
        private int _startupStartButtonTextR = 245;
        private int _startupStartButtonTextG = 248;
        private int _startupStartButtonTextB = 252;
        private int _startupStartButtonTextA = 255;


        // =========================================================
        // STARTUP FLOW: PROFILE -> GAMEPLAY MODE -> ROUTE/TOTAL POS -> DIFFICULTY -> START
        // =========================================================
        private enum StartupUiStage
        {
            Profile = 0,
            Gameplay = 1,
            Difficulty = 2
        }

        private enum MoonGameplayMode
        {
            None = 0,
            GoToMountain = 1,
            Survival = 2
        }

        private enum MoonDifficultyMode
        {
            None = 0,
            Easy = 1,
            Normal = 2,
            Hard = 3
        }

        private StartupUiStage _startupUiStage = StartupUiStage.Profile;
        private MoonGameplayMode _selectedGameplayMode = MoonGameplayMode.None;
        private MoonDifficultyMode _selectedDifficultyMode = MoonDifficultyMode.None;
        private int _selectedGameplayPosCount = 0;
        private bool _selectedGoToMountainDirect = false;
        private bool _selectedSurvivalDirectFinish = false;
        private bool _gameplaySelectionLocked = false;

        // =========================================================
        // STARTUP LOADING GATE
        // Setelah START, GTA tetap hitam + pause sampai route benar-benar siap.
        // Gameplay Tick, webhook, dan timer baru dibuka setelah gate selesai.
        // =========================================================
        private int _startupLoadingStartTime = 0;
        private int _startupRouteReadySince = 0;

        private bool _startupLoadingTextEnabled = true;
        private string _startupLoadingText = "LOADING";
        private float _startupLoadingTextX = 0.500f;
        private float _startupLoadingTextY = 0.500f;
        private float _startupLoadingTextScale = 1.10f;
        private int _startupLoadingTextFont = 4;
        private int _startupLoadingTextR = 255;
        private int _startupLoadingTextG = 255;
        private int _startupLoadingTextB = 255;
        private int _startupLoadingTextA = 255;

        private string _startupLoadingSubtitle = "PREPARING GAMEPLAY";
        private float _startupLoadingSubtitleY = 0.555f;
        private float _startupLoadingSubtitleScale = 0.42f;
        private int _startupLoadingSubtitleR = 150;
        private int _startupLoadingSubtitleG = 165;
        private int _startupLoadingSubtitleB = 185;
        private int _startupLoadingSubtitleA = 255;

        private int _startupLoadingBgR = 0;
        private int _startupLoadingBgG = 0;
        private int _startupLoadingBgB = 0;
        private int _startupLoadingBgA = 255;
        private int _startupLoadingMinDisplayMs = 700;
        private int _startupLoadingReadyHoldMs = 500;
        // Hard failsafe supaya startup tidak pernah stuck permanen jika GTA gagal
        // menyediakan road/path node. Normalnya gate selesai jauh sebelum batas ini.
        private int _startupLoadingMaxWaitMs = 30000;
        private bool _startupLoadingAnimatedDots = true;

        // GAMEPLAY SELECT HUD (defaults; editable in MoonHUD.ini)
        private bool _gameplaySelectEnabled = true;
        private string _gameplaySelectTitle = "PILIH GAMEPLAY MODE";
        private float _gameplaySelectHeaderX = 0.500f;
        private float _gameplaySelectHeaderY = 0.175f;
        private float _gameplaySelectHeaderWidth = 0.900f;
        private float _gameplaySelectHeaderHeight = 0.145f;
        private float _gameplaySelectTitleScale = 1.15f;
        private int _gameplaySelectTitleFont = 4;

        private float _gameplayModeBoxY = 0.430f;
        private float _gameplayModeBoxWidth = 0.340f;
        private float _gameplayModeBoxHeight = 0.190f;
        private float _gameplaySurvivalBoxX = 0.305f;
        private float _gameplayMountainBoxX = 0.695f;
        private float _gameplayModeTextScale = 0.72f;

        private int _gameplayNeutralR = 20;
        private int _gameplayNeutralG = 25;
        private int _gameplayNeutralB = 32;
        private int _gameplayNeutralA = 245;
        private int _gameplaySelectedR = 30;
        private int _gameplaySelectedG = 116;
        private int _gameplaySelectedB = 160;
        private int _gameplaySelectedA = 255;
        private int _gameplayBorderR = 78;
        private int _gameplayBorderG = 100;
        private int _gameplayBorderB = 124;
        private int _gameplayBorderA = 225;
        private int _gameplayTextR = 245;
        private int _gameplayTextG = 248;
        private int _gameplayTextB = 252;
        private int _gameplayTextA = 255;

        private string _gameplayTotalPosLabel = "TOTAL POS";
        private float _gameplayTotalPosY = 0.590f;
        private float _gameplayTotalPosScale = 0.50f;

        private float _gameplayOptionY = 0.655f;
        private float _gameplayOptionWidth = 0.135f;
        private float _gameplayOptionHeight = 0.075f;
        private float _gameplayOptionGap = 0.015f;
        private float _gameplayOptionTextScale = 0.48f;
        private float _gameplayStartX = 0.500f;
        private float _gameplayStartY = 0.815f;
        private float _gameplayStartWidth = 0.240f;
        private float _gameplayStartHeight = 0.085f;
        private float _gameplayStartTextScale = 0.78f;

        // DIFFICULTY SELECT HUD (screen after gameplay/route selection)
        private bool _difficultySelectEnabled = true;
        private string _difficultySelectTitle = "MODE";
        private float _difficultyHeaderX = 0.500f;
        private float _difficultyHeaderY = 0.175f;
        private float _difficultyHeaderWidth = 0.900f;
        private float _difficultyHeaderHeight = 0.145f;
        private float _difficultyTitleScale = 1.15f;
        private int _difficultyTitleFont = 4;
        private float _difficultyBoxY = 0.500f;
        private float _difficultyBoxWidth = 0.245f;
        private float _difficultyBoxHeight = 0.200f;
        private float _difficultyEasyX = 0.220f;
        private float _difficultyNormalX = 0.500f;
        private float _difficultyHardX = 0.780f;
        private float _difficultyTextScale = 0.78f;
        private float _difficultyStartX = 0.500f;
        private float _difficultyStartY = 0.790f;
        private float _difficultyStartWidth = 0.240f;
        private float _difficultyStartHeight = 0.085f;
        private float _difficultyStartTextScale = 0.78f;
        private int _difficultyNeutralR = 20;
        private int _difficultyNeutralG = 25;
        private int _difficultyNeutralB = 32;
        private int _difficultyNeutralA = 245;
        private int _difficultySelectedR = 30;
        private int _difficultySelectedG = 116;
        private int _difficultySelectedB = 160;
        private int _difficultySelectedA = 255;
        private int _difficultyBorderR = 78;
        private int _difficultyBorderG = 100;
        private int _difficultyBorderB = 124;
        private int _difficultyBorderA = 225;
        private int _difficultyTextR = 245;
        private int _difficultyTextG = 248;
        private int _difficultyTextB = 252;
        private int _difficultyTextA = 255;

        // =========================================================
        // SURVIVAL GAMEPLAY
        // =========================================================
        private bool _survivalNoVehicle = true;

        // SURVIVAL - NO VEHICLE WARNING HUD
        // Triggered when the player attempts to enter a vehicle while Survival blocks vehicles.
        private bool _survivalNoVehicleWarningEnabled = true;
        private string _survivalNoVehicleWarningText = "YOU CANNOT USE VEHICLES IN SURVIVAL MODE";
        private int _survivalNoVehicleWarningDurationMs = 1800;
        private int _survivalNoVehicleWarningCooldownMs = 650;
        private int _survivalNoVehicleWarningUntilTime = 0;
        private int _survivalNoVehicleWarningNextTriggerTime = 0;
        private float _survivalNoVehicleWarningX = 0.500f;
        private float _survivalNoVehicleWarningY = 0.825f;
        private float _survivalNoVehicleWarningWidth = 0.410f;
        private float _survivalNoVehicleWarningHeight = 0.062f;
        private int _survivalNoVehicleWarningBgR = 18;
        private int _survivalNoVehicleWarningBgG = 18;
        private int _survivalNoVehicleWarningBgB = 22;
        private int _survivalNoVehicleWarningBgA = 235;
        private int _survivalNoVehicleWarningBorderR = 244;
        private int _survivalNoVehicleWarningBorderG = 76;
        private int _survivalNoVehicleWarningBorderB = 86;
        private int _survivalNoVehicleWarningBorderA = 255;
        private float _survivalNoVehicleWarningBorderSize = 0.0014f;
        private float _survivalNoVehicleWarningTextScale = 0.55f;
        private int _survivalNoVehicleWarningTextFont = 4;
        private int _survivalNoVehicleWarningTextR = 255;
        private int _survivalNoVehicleWarningTextG = 255;
        private int _survivalNoVehicleWarningTextB = 255;
        private int _survivalNoVehicleWarningTextA = 255;
        private float _survivalNoVehicleWarningTextOffsetY = -0.017f;

        private bool _survivalRandomStart = true;
        private int _survivalPosCount = 5;
        // Direct Finish Line = 0 POS. Karena POS paling sedikit harus paling jauh,
        // direct memakai distance band paling jauh dan tetap editable dari INI.
        private float _survivalDirectMinDistance = 4500.0f;
        private float _survivalDirectMaxDistance = 6500.0f;
        // Survival route spacing is a DISTANCE BAND per leg.
        // More POS = checkpoints closer together while keeping total trip length comparable.
        private float _survivalMinDistance5 = 1000.0f;
        private float _survivalMaxDistance5 = 1400.0f;
        private float _survivalMinDistance10 = 550.0f;
        private float _survivalMaxDistance10 = 800.0f;
        private float _survivalMinDistance15 = 350.0f;
        private float _survivalMaxDistance15 = 550.0f;
        private float _survivalMinDistance20 = 250.0f;
        private float _survivalMaxDistance20 = 400.0f;
        private int _survivalRoutePointAttempts = 80;
        private Vector3 _lastSurvivalStartPosition = Vector3.Zero;

        // =========================================================
        // GO TO MOUNTAIN - ORDERED ROUTE
        // =========================================================
        // Jarak POS tidak memakai config min/max. Posisi dibagi otomatis
        // di sepanjang START -> FINISH: makin banyak POS, makin rapat.

        // Score penalty hanya untuk death yang disebabkan enemy/animal.
        private int _enemyDeathsForScorePenalty = 0;
        private int _lastTrackedEnemyDamageTime = 0;
        private const int ENEMY_DEATH_CAUSE_WINDOW_MS = 3000;

        // PROFILE INFORMATION - tanpa icon, lima baris data.
        private string _startupProfileTitle = "PROFILE";
        private float _startupProfilePanelX = 0.500f;
        private float _startupProfilePanelY = 0.625f;
        private float _startupProfilePanelWidth = 0.740f;
        private float _startupProfilePanelHeight = 0.400f;
        private float _startupProfileTitleOffsetY = -0.165f;
        private float _startupProfileFirstRowOffsetY = -0.105f;
        private float _startupProfileRowSpacing = 0.058f;
        private float _startupProfileLabelPaddingX = 0.035f;
        private float _startupProfileValueOffsetX = 0.365f;
        private float _startupProfileLabelScale = 0.48f;
        private float _startupProfileValueScale = 0.52f;
        private int _startupProfileFont = 4;

        private int _startupProfileLabelR = 205;
        private int _startupProfileLabelG = 215;
        private int _startupProfileLabelB = 226;
        private int _startupProfileLabelA = 255;

        private int _startupProfileNameR = 255;
        private int _startupProfileNameG = 204;
        private int _startupProfileNameB = 90;
        private int _startupProfileNameA = 255;

        private int _startupProfilePositiveR = 95;
        private int _startupProfilePositiveG = 235;
        private int _startupProfilePositiveB = 105;
        private int _startupProfilePositiveA = 255;

        private int _startupProfileNegativeR = 255;
        private int _startupProfileNegativeG = 92;
        private int _startupProfileNegativeB = 92;
        private int _startupProfileNegativeA = 255;

        private int _startupProfileNeutralR = 248;
        private int _startupProfileNeutralG = 250;
        private int _startupProfileNeutralB = 253;
        private int _startupProfileNeutralA = 255;

        private int _startupProfileKillR = 90;
        private int _startupProfileKillG = 210;
        private int _startupProfileKillB = 235;
        private int _startupProfileKillA = 255;

        // Default dipakai jika section [HUD_PLAYER_SETUP] tidak ada.
        private bool _startupSetupEnabled = true;
        // MaxLength = safety limit karakter; MaxWords = aturan utama nama host.
        private int _startupNameMaxLength = 60;
        private int _startupNameMaxWords = 8;

        // Full-screen overlay saat PROFILE / GAMEPLAY SELECT.
        // Default benar-benar hitam supaya world GTA tidak tembus di belakang UI.
        private int _startupOverlayR = 0;
        private int _startupOverlayG = 0;
        private int _startupOverlayB = 0;
        private int _startupOverlayA = 255;

        // Judul besar PLAYER PROFILE di bagian atas layar.
        private float _startupNamePanelX = 0.50f;
        private float _startupNamePanelY = 0.275f;
        private float _startupNamePanelWidth = 0.440f;
        private float _startupNamePanelHeight = 0.085f;

        private int _startupPanelR = 12;
        private int _startupPanelG = 15;
        private int _startupPanelB = 21;
        private int _startupPanelA = 245;

        private int _startupPanelBorderR = 70;
        private int _startupPanelBorderG = 78;
        private int _startupPanelBorderB = 90;
        private int _startupPanelBorderA = 220;

        private int _startupAccentR = 70;
        private int _startupAccentG = 160;
        private int _startupAccentB = 210;
        private int _startupAccentA = 255;
        private float _startupAccentHeight = 0.004f;

        private string _startupInstructionText = "PLAYER PROFILE";
        private float _startupInstructionOffsetY = -0.022f;
        private float _startupInstructionScale = 0.78f;
        private int _startupInstructionFont = 4;
        private int _startupInstructionR = 255;
        private int _startupInstructionG = 255;
        private int _startupInstructionB = 255;
        private int _startupInstructionA = 255;

        // CUSTOM INPUT BOX. Semua bagian di bawah adalah HUD buatan script,
        // bukan DISPLAY_ONSCREEN_KEYBOARD, sehingga judul bebas diubah / ditambah.
        private string _startupInputTitle = "PLAYER NAME";
        private string _startupInputHint = "";
        private string _startupInputPlaceholder = "TYPE YOUR NAME";
        private bool _startupInputLabelEnabled = true;
        private float _startupInputLabelX = 0.50f;
        private float _startupInputLabelY = 0.455f;
        private float _startupInputLabelWidth = 0.440f;
        private float _startupInputLabelHeight = 0.175f;
        private float _startupInputLabelTextPaddingX = 0.018f;
        private float _startupInputLabelTextOffsetY = -0.064f;
        private float _startupInputLabelTextScale = 0.44f;
        private int _startupInputLabelTextFont = 4;
        private int _startupInputLabelTextR = 245;
        private int _startupInputLabelTextG = 248;
        private int _startupInputLabelTextB = 252;
        private int _startupInputLabelTextA = 255;
        private int _startupInputLabelBgR = 12;
        private int _startupInputLabelBgG = 15;
        private int _startupInputLabelBgB = 21;
        private int _startupInputLabelBgA = 242;

        private float _startupInputHintOffsetY = -0.035f;
        private float _startupInputHintScale = 0.29f;
        private int _startupInputHintR = 155;
        private int _startupInputHintG = 166;
        private int _startupInputHintB = 180;
        private int _startupInputHintA = 255;

        private float _startupInputBoxOffsetY = 0.018f;
        private float _startupInputBoxWidth = 0.404f;
        private float _startupInputBoxHeight = 0.052f;
        private int _startupInputBoxR = 6;
        private int _startupInputBoxG = 9;
        private int _startupInputBoxB = 14;
        private int _startupInputBoxA = 255;
        private int _startupInputBoxBorderR = 66;
        private int _startupInputBoxBorderG = 76;
        private int _startupInputBoxBorderB = 90;
        private int _startupInputBoxBorderA = 235;
        private float _startupInputTextScale = 0.43f;
        private int _startupInputTextFont = 4;
        private int _startupInputTextR = 255;
        private int _startupInputTextG = 255;
        private int _startupInputTextB = 255;
        private int _startupInputTextA = 255;
        private int _startupInputPlaceholderR = 105;
        private int _startupInputPlaceholderG = 116;
        private int _startupInputPlaceholderB = 132;
        private int _startupInputPlaceholderA = 220;

        // Modern input decoration. Tetap configurable dari [HUD_PLAYER_SETUP].
        private string _startupInputFooterText = "ENTER  TO CONTINUE";
        private float _startupInputFooterOffsetY = 0.061f;
        private float _startupInputFooterScale = 0.25f;
        private int _startupInputFooterR = 155;
        private int _startupInputFooterG = 166;
        private int _startupInputFooterB = 180;
        private int _startupInputFooterA = 255;

        private bool _startupInputCounterEnabled = true;
        private float _startupInputCounterOffsetY = 0.061f;
        private float _startupInputCounterScale = 0.24f;
        private int _startupInputCounterR = 120;
        private int _startupInputCounterG = 132;
        private int _startupInputCounterB = 148;
        private int _startupInputCounterA = 255;

        // Warning ketika user mencoba membuat kata melebihi MaxWords.
        // Ditampilkan langsung di custom input UI, bukan notification GTA.
        private string _startupInputWarningText = "MAXIMUM {0} WORDS";
        private int _startupInputWarningDurationMs = 1800;
        private float _startupInputWarningOffsetY = 0.061f;
        private float _startupInputWarningScale = 0.28f;
        private int _startupInputWarningR = 244;
        private int _startupInputWarningG = 76;
        private int _startupInputWarningB = 86;
        private int _startupInputWarningA = 255;
        private bool _startupInputWarningFlashField = true;
        private int _startupInputWarningEndTime = 0;

        private float _startupInputActiveBarWidth = 0.0040f;
        private float _startupInputActiveUnderlineHeight = 0.0025f;
        private float _startupInputShadowOffsetX = 0.0030f;
        private float _startupInputShadowOffsetY = 0.0050f;

        // Name tag di atas kepala player.
        private bool _playerNameTagEnabled = false;
        private float _playerNameTagHeadOffsetZ = 0.38f;
        private float _playerNameTagScale = 0.42f;
        private int _playerNameTagFont = 4;
        private float _playerNameTagMaxDistance = 80.0f;

        private int _playerNameTagR = 255;
        private int _playerNameTagG = 255;
        private int _playerNameTagB = 255;
        private int _playerNameTagA = 255;

        private int _playerNameTagBgR = 15;
        private int _playerNameTagBgG = 18;
        private int _playerNameTagBgB = 25;
        private int _playerNameTagBgA = 215;

        private int _playerNameTagAccentR = 0;
        private int _playerNameTagAccentG = 205;
        private int _playerNameTagAccentB = 255;
        private int _playerNameTagAccentA = 255;

        // =========================================================
        // MANUAL HWID / DEVICE LOCK
        // =========================================================
        // ISI NILAI PC YANG DIIZINKAN SECARA MANUAL DI SINI.
        // Tidak ada HWID di file .ini dan tidak ada binder otomatis.
        //
        // Cara ambil nilai:
        // 1. Jalankan CHECK_HWID_MANUAL_STANDALONE_V3.bat pada PC target.
        // 2. Copy kelima nilai yang tampil.
        // 3. Paste ke profile PC di bawah.
        // 4. Compile ulang GoToMountain.cs.
        //
        // Semua komponen harus cocok. Jika satu saja berbeda, script berhenti
        // sebelum Tick, entity, gameplay, dan webhook server diaktifkan.
        private sealed class AuthorizedPcProfile
        {
            public string Name { get; set; }
            public string MachineGuid { get; set; }
            public string VolumeSerial { get; set; }
            public string CpuIdentifier { get; set; }
            public string SystemProductName { get; set; }
            public string SystemSku { get; set; }
        }

        private static readonly AuthorizedPcProfile[] AUTHORIZED_PCS =
        {
            new AuthorizedPcProfile
            {
                Name = "PC UTAMA",
                MachineGuid = "E9CF0F8A-42F4-4AA6-9CC1-C8C8C46E9FFD",
                VolumeSerial = "38F301E8",
                CpuIdentifier = "AMD64 FAMILY 25 MODEL 80 STEPPING 0",
                SystemProductName = "MS-7C96",
                SystemSku = "TO BE FILLED BY O.E.M."
            }

            // Untuk mengizinkan PC kedua, copy blok new AuthorizedPcProfile
            // di atas, tambahkan koma setelah blok sebelumnya, lalu isi HWID PC 2.
        };

        private const string HWID_DIAGNOSTIC_LOG =
            "scripts/GoToMountain_HWID.log";

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern bool GetVolumeInformation(
            string lpRootPathName,
            StringBuilder lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            StringBuilder lpFileSystemNameBuffer,
            int nFileSystemNameSize
        );

        // =========================================================
        // MAIN THREAD WEBHOOK QUEUE - BATCHED / COMPRESSED
        // =========================================================
        // Request dengan endpoint yang sama tidak membuat ribuan delegate
        // terpisah. Semua gift tetap dihitung melalui PendingCount dan
        // diproses satu per satu secara round-robin di GTA main thread.
        // Tidak ada hard limit/reject: gift tidak dibuang oleh queue ini.
        private sealed class BatchedMainThreadAction
        {
            public readonly object SyncRoot = new object();
            public Action Action { get; set; }
            public long PendingCount { get; set; }
            public bool IsQueued { get; set; }
        }

        private readonly ConcurrentQueue<BatchedMainThreadAction> _mainThreadQueue =
            new ConcurrentQueue<BatchedMainThreadAction>();

        private readonly ConcurrentDictionary<string, BatchedMainThreadAction> _mainThreadBatches =
            new ConcurrentDictionary<string, BatchedMainThreadAction>(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly Random _random =
            new Random();

        private const int MODEL_LOAD_TIMEOUT_MS = 8000;

        // =========================================================
        // INTERNAL DESPAWN - HARDCODED
        // =========================================================
        // Tidak memakai script Despawn terpisah dan tidak memakai config INI.
        //
        // 3 detik:
        //   Falling Vehicles SETELAH impact + meledak
        //   Dead Enemy Vehicle
        //   Dead Enemy Ped
        //   Dead Animal
        //
        // 60 detik:
        //   /hit_by_vehicle
        //   /ball_rain
        // =========================================================
        private const int STANDARD_DESPAWN_MS =
            3 * 1000;

        private const int HIT_BY_VEHICLE_DESPAWN_MS =
            60 * 1000;

        private const int BALL_RAIN_DESPAWN_MS =
            60 * 1000;

        private readonly List<ScheduledDespawnTracker> _scheduledDespawns =
            new List<ScheduledDespawnTracker>();

        // =========================================================
        // ROUTE / CHECKPOINT - GLOBAL, TANPA ZONA
        // =========================================================
        // Script tidak lagi memakai START AREA / radius kota.
        //
        // Saat script pertama aktif:
        // - player TIDAK diteleport.
        // - posisi player saat itu menjadi START ronde pertama.
        //
        // Saat FINISH selesai:
        // - player tetap di lokasi FINISH.
        // - lokasi tersebut menjadi START ronde berikutnya.
        // - seluruh POS + FINISH dirandom ulang secara global.
        //
        // Tidak ada batas maksimal jarak antar POS.
        // =========================================================
        private bool _initialRouteSetupPending = true;
        private int _nextInitialRouteSetupAttemptTime = 0;
        private const int INITIAL_ROUTE_SETUP_DELAY_MS = 500;

        private Vector3 _startPosition =
            Vector3.Zero;

        // Pos pertama ronde sebelumnya disimpan supaya ronde berikutnya
        // tidak memilih titik pertama yang terlalu mirip.
        private Vector3 _lastGeneratedFirstPos =
            Vector3.Zero;

        // Batas sampling seluruh mainland GTA V.
        // Ini BUKAN zona gameplay; hanya area koordinat untuk mencari road node.
        private const float GLOBAL_ROUTE_MIN_X = -3500.0f;
        private const float GLOBAL_ROUTE_MAX_X = 4300.0f;
        private const float GLOBAL_ROUTE_MIN_Y = -3800.0f;
        private const float GLOBAL_ROUTE_MAX_Y = 7900.0f;

        // Tidak ada MAX distance antar POS.
        // Nilai di bawah hanya mencegah POS menumpuk di titik yang sama.
        private const float GLOBAL_ROUTE_MIN_LEG_DISTANCE = 300.0f;
        private const float GLOBAL_ROUTE_MIN_POINT_SEPARATION = 180.0f;
        private const float GLOBAL_ROUTE_FIRST_POS_DIFFERENCE = 500.0f;
        private const int GLOBAL_ROUTE_POINT_ATTEMPTS = 500;

        // =========================================================
        // RANDOM POS VEHICLE ACCESS SAFETY
        // =========================================================
        // Road node GTA belum tentu berarti jalan tersebut bisa dimasuki
        // player secara normal. Beberapa node berada di balik pagar / area
        // restricted seperti LSIA airside, prison, military base, dll.
        //
        // Filter di bawah hanya dipakai untuk POS random. START / FINISH
        // mission dari INI tidak diubah oleh filter ini.
        private const float ROUTE_MAX_TRAVEL_DISTANCE_SENTINEL = 99950.0f;
        private const float ROUTE_MAX_DETOUR_MULTIPLIER = 12.0f;
        private const float ROUTE_MAX_DETOUR_MINIMUM = 8000.0f;

        private readonly List<Vector3> _activePosList =
            new List<Vector3>();

        private Vector3 _activeFinishPosition;
        private int _currentPosIndex = 5;
        private readonly List<Blip> _routeBlips = new List<Blip>();
        private bool _isInsideCheckpoint = false;
        private int _checkpointTouchStartTime = 0;

        // =========================================================
        // RANDOM CHECKPOINT - PROGRESS BAR TRIGGER / GACHA
        // =========================================================
        // ROUTE PAKAI POS:
        //   tidak ada marker Random Checkpoint di tengah leg.
        //   Satu Random Checkpoint dimulai SETELAH countdown POS selesai.
        //
        // ROUTE DIRECT / TANPA POS:
        //   START->FINISH dibagi menjadi N Random Checkpoint.
        //   Default N=7 -> 1/8 ... 7/8 dari progress bar.
        //
        // Garis merah hanya tampil pada DIRECT route dan memakai threshold
        // progress yang sama persis dengan trigger.
        // =========================================================
        private bool _randomCheckpointEnabled = true;
        private int _randomCheckpointPerLeg = 3;
        private int _randomCheckpointDirectCount = 7;
        private int _randomCheckpointCountdownSeconds = 5;
        private int _randomCheckpointGachaDurationMs = 2500;
        private int _randomCheckpointGachaSwitchIntervalMs = 120;
        private int _randomCheckpointLockedDisplayMs = 1000;

        private readonly List<string> _randomCheckpointEffectPool =
            new List<string>
            {
                "get_towed",
                "traffic_magnet",
                "reckless_traffic",
                "vehicle_fire_timer",
                "police_roadblock",
                "random_explosion_nearby",
                "wanted_5_star"
            };

        // Durasi khusus per hasil RANDOM_CHECKPOINT.
        // Jika effect tidak punya override di INI, otomatis fallback ke
        // timer effect yang sama dari [TIME_DEFAULT].
        private readonly Dictionary<string, int> _randomCheckpointEffectDurationsMs =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Effect yang benar-benar AKTIF karena menang dari RANDOM CHECKPOINT.
        // Semua effect di set ini dihentikan begitu player menyentuh POS / FINISH.
        private readonly HashSet<string> _activeRandomCheckpointEffects =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // POS mode: satu sequence per POS, pool dimulai baru setelah tiap POS selesai.
        // DIRECT START->FINISH: repeat effect tetap diperbolehkan.
        private readonly HashSet<string> _randomCheckpointUsedEffectsThisLeg =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private string _randomCheckpointUsedEffectsLegKey = string.Empty;
        private bool _randomCheckpointCurrentLegDirectMode = false;

        // Snapshot leg untuk sequence yang sedang berjalan. Dibutuhkan karena
        // player bisa mencapai POS berikutnya saat countdown/roulette lama masih aktif.
        private string _randomCheckpointSequenceLegKey = string.Empty;
        private bool _randomCheckpointSequenceDirectMode = false;

        // Nomor route supaya POS:0 ronde baru tidak dianggap leg yang sama
        // dengan POS:0 ronde sebelumnya.
        private int _randomCheckpointRouteVersion = 0;

        private readonly List<float> _randomCheckpointThresholds =
            new List<float>();

        private readonly HashSet<int> _randomCheckpointTriggeredIndices =
            new HashSet<int>();

        // Jika player sangat cepat dan melewati lebih dari satu marker,
        // trigger berikutnya masuk antrean dan tidak hilang.
        private readonly Queue<int> _randomCheckpointPendingQueue =
            new Queue<int>();

        private string _randomCheckpointLegKey = string.Empty;
        private float _randomCheckpointPreviousProgress = 0.0f;
        private bool _randomCheckpointProgressInitialized = false;

        // 0=idle, 1=countdown, 2=roulette, 3=locked
        private int _randomCheckpointPhase = 0;
        private bool _randomCheckpointSequenceActive = false;
        private int _randomCheckpointPhaseStartTime = 0;
        private int _randomCheckpointNextRouletteSwitchTime = 0;
        private int _randomCheckpointActiveTriggerIndex = -1;
        private string _randomCheckpointCurrentEffectKey = string.Empty;
        private string _randomCheckpointLockedEffectKey = string.Empty;

        // =========================================================
        // POS / FINISH CELEBRATION
        // =========================================================
        // Saat player berada di area POS / FINISH:
        // - Jalan kaki  : angkat tangan.
        // - Kendaraan   : mobil / motor bergoyang (dance).
        // Effect berhenti otomatis saat keluar area atau timer selesai.
        private bool _checkpointCelebrationOnFootActive = false;
        private int _checkpointCelebrationNextHandsUpRefreshTime = 0;

        private Vehicle _checkpointCelebrationVehicle = null;
        private Vector3 _checkpointCelebrationVehicleBaseRotation =
            Vector3.Zero;
        private int _checkpointCelebrationVehicleStartTime = 0;

        // =========================================================
        // ROUTE GENERATION SAFETY
        // =========================================================
        private const int ROUTE_GENERATION_STREAM_DELAY_MS = 150;
        private const int ROUTE_GENERATION_RETRY_MS = 600;

        private bool _routeGenerationPending = false;
        private int _nextRouteGenerationAttemptTime = 0;

        // GO TO MOUNTAIN POS FAILSAFE
        // Jika builder POS terlalu lama / gagal total, route otomatis berubah
        // menjadi DIRECT START -> FINISH agar tidak stuck LOADING ROUTE.
        private bool _goToMountainFallbackToDirectOnRouteFail = true;
        private int _goToMountainRouteBuildTimeoutMs = 8000;
        private int _goToMountainRouteBuildDeadlineTime = 0;

        // =========================================================
        // INCREMENTAL ROUTE BUILDER
        // =========================================================
        // Route random TIDAK boleh dibangun ribuan native call dalam satu Tick.
        // Builder ini membatasi pencarian kandidat per frame sehingga fresh GTA
        // launch tidak stutter sementara road/path nodes masih warming up.
        private enum PendingRouteBuildMode
        {
            None = 0,
            GoToMountain = 1,
            Survival = 2
        }

        private PendingRouteBuildMode _pendingRouteBuildMode = PendingRouteBuildMode.None;
        private bool _pendingRouteBuildInitialized = false;
        private readonly List<Vector3> _pendingRouteBuildCheckpoints = new List<Vector3>();
        private readonly List<Vector3> _pendingRouteBuildUsedPoints = new List<Vector3>();
        private int _pendingRouteBuildTotalCheckpoints = 0;
        private int _pendingRouteBuildCheckpointIndex = 0;
        private int _pendingRouteBuildCandidateAttempts = 0;
        private int _pendingRouteBuildWholeAttempts = 0;
        private Vector3 _pendingRouteBuildReference = Vector3.Zero;
        private Vector3 _pendingRouteBuildFinish = Vector3.Zero;
        private float _pendingRouteBuildMinLegDistance = 0.0f;
        private float _pendingRouteBuildMaxLegDistance = 0.0f;

        // Maksimal kandidat road node yang dites setiap game Tick.
        // Nilai kecil = frame lebih stabil; builder akan lanjut pada Tick berikutnya.
        private const int ROUTE_BUILD_CANDIDATES_PER_TICK = 12;
        private const int ROUTE_BUILD_MAX_POINT_ATTEMPTS = 48;
        private const int ROUTE_BUILD_MAX_WHOLE_ATTEMPTS = 3;

        // =========================================================
        // GAMEPLAY CONFIG FROM INI
        // =========================================================
        // [MISSION]
        // RandomChaosEnabled=true/false
        // RandomCheckpointEnabled=true/false
        // PosCount=1..20
        //
        // Detail roulette tetap berada di [RANDOM_CHAOS]
        // dan [RANDOM_CHECKPOINT].
        //
        // Perubahan config dicek otomatis sekitar setiap 1 detik.
        // File hanya dibaca ulang jika timestamp file berubah.
        // PosCount dipakai saat route berikutnya dibuat supaya route
        // yang sedang berjalan tidak berubah mendadak.
        // =========================================================
        private bool _randomChaosEnabled = true;
        private int _routeCheckpointCount = 5;

        private const int MIN_ROUTE_CHECKPOINT_COUNT = 1;
        private const int MAX_ROUTE_CHECKPOINT_COUNT = 20;

        private int _nextGameplayConfigReloadTime = 0;
        private const int GAMEPLAY_CONFIG_RELOAD_INTERVAL_MS = 1000;

        // File timestamp cache untuk hot-reload config.
        // Tick tetap mengecek sekitar setiap 1 detik, tetapi file hanya
        // dibaca/parsing ulang jika LastWriteTimeUtc benar-benar berubah.
        private DateTime _lastGameplayIniWriteTimeUtc = DateTime.MinValue;
        private DateTime _lastDesignIniWriteTimeUtc = DateTime.MinValue;
        private DateTime _lastHudLayoutIniWriteTimeUtc = DateTime.MinValue;
        private DateTime _lastHudPresetIniWriteTimeUtc = DateTime.MinValue;
        private string _lastLoadedHudLayoutName = string.Empty;
        private string _lastLoadedHudPresetName = string.Empty;
        private DateTime _lastEntityIniWriteTimeUtc = DateTime.MinValue;

        // =========================================================
        // HUD DESIGN / PRESET
        // =========================================================
        // Base umum         : scripts/GameplayHUD.ini
        // Layout aktif      : scripts/design/<layout>.ini
        // Style preset aktif: scripts/design/preset/<preset>.ini
        private string _hudLayoutName = "default";
        private string _hudPresetName = "default";

        // =========================================================
        // MISSION MODE
        // =========================================================
        // GoToMountain=false:
        //   pakai sistem route GLOBAL lama seperti biasa.
        //
        // GoToMountain=true:
        //   START / FINISH tetap dari [MISSION].
        //   GotoMountainPos=true  -> ada POS di antara START dan FINISH.
        //   GotoMountainPos=false -> langsung menuju FINISH.
        //
        // Dalam GoToMountain mode:
        //   GotoMountainPos=false:
        //     - langsung START -> FINISH
        //     - FINISH tampil sebagai marker HIJAU.
        //
        //   GotoMountainPos=true:
        //     - POS 1..N FULL RANDOM di seluruh mainland GTA V.
        //     - Semua POS WAJIB berada pada road/street node.
        //     - HANYA POS yang sedang aktif yang tampil di map.
        //     - Setelah POS selesai, blip lama hilang dan POS berikutnya muncul.
        //     - FINISH memakai FINISH_POS [MISSION] PERSIS (tidak di-snap).
        //
        //   PENGECUALIAN:
        //   GoToMountain=true + GotoMountainPos=false tetap memakai
        //   START_POS -> FINISH_POS persis dari INI tanpa snap ke jalan.
        //
        //   Navigasi/GPS route dimatikan.
        //   Player mengikuti marker POS di map secara manual.
        //   GotoMountainPos=true tetap WAJIB membuat POS 1..N.
        // =========================================================
        private bool _missionGoToMountainEnabled =
            false;

        private bool _missionGoToMountainPosEnabled =
            true;

        private Vector3 _missionStartPosition =
            new Vector3(
                -1012.173f,
                -2733.264f,
                13.7578f
            );

        private Vector3 _missionFinishPosition =
            new Vector3(
                500.8116f,
                5604.0986f,
                797.9102f
            );

        // =========================================================
        // RANDOM CHAOS COUNTDOWN
        // =========================================================
        // Default 60 menit jika CountdownSeconds tidak ada / invalid.
        // Nilai normal dibaca dari:
        // [RANDOM_CHAOS]
        // CountdownSeconds=3600
        private int _countdownDurationMs =
            60 * 60 * 1000;

        private const int RANDOM_CHAOS_POS_BONUS_SECONDS = 15 * 60;

        // Satu-satunya sumber sisa waktu Random Chaos.
        // Bonus checkpoint masuk ke nilai ini.
        private int _countdownRemainingMs = 0;
        private int _lastCountdownUpdateTime = 0;
        private bool _isCountdownActive = false;

        // =========================================================
        // RANDOM CHAOS / CONFIGURABLE EFFECT ROULETTE
        // =========================================================
        // Pilihan efek + durasi dibaca dari [RANDOM_CHAOS]:
        // RandomChaos1=cage
        // RandomChaos1Duration=300
        // dst.
        //
        // Random Chaos hanya memakai efek gift yang sudah ada:
        // cage, disarm, transform_animal, blackhole,
        // wanted_5_star, earthquake.
        // Enemy/Tank/Monster tidak masuk roulette baru.
        // =========================================================
        private const int DEFAULT_RANDOM_CHAOS_DURATION_MS =
            5 * 60 * 1000;

        // Legacy helper compatibility. Roulette baru tidak memakai
        // durasi hardcoded ini untuk effect aktif.
        private const int CHAOS_SURVIVAL_DURATION_MS =
            DEFAULT_RANDOM_CHAOS_DURATION_MS;

        // Timing roulette RANDOM_CHAOS sekarang editable dari GoToMountain.ini.
        private int _chaosRouletteDurationMs =
            7000;

        private int _chaosRouletteLockMs =
            4000;

        private int _chaosRouletteItemIntervalMs =
            80;

        // Konstanta lama dipertahankan hanya untuk helper legacy yang
        // tidak lagi dipilih oleh roulette configurable.
        private const int CHAOS_BLACKHOLE = 0;
        private const int CHAOS_TANKS = 1;
        private const int CHAOS_MONSTERS = 2;
        private const int CHAOS_NO_WEAPON = 3;
        private const int CHAOS_PLAYER_CAGE = 4;

        private readonly List<RandomChaosOption> _randomChaosOptions =
            new List<RandomChaosOption>
            {
                new RandomChaosOption
                {
                    Key = "cage",
                    DisplayName = "CAGE",
                    DurationMs = DEFAULT_RANDOM_CHAOS_DURATION_MS
                },
                new RandomChaosOption
                {
                    Key = "disarm",
                    DisplayName = "DISARM",
                    DurationMs = DEFAULT_RANDOM_CHAOS_DURATION_MS
                },
                new RandomChaosOption
                {
                    Key = "transform_animal",
                    DisplayName = "TRANSFORM ANIMAL",
                    DurationMs = DEFAULT_RANDOM_CHAOS_DURATION_MS
                },
                new RandomChaosOption
                {
                    Key = "blackhole",
                    DisplayName = "BLACKHOLE",
                    DurationMs = DEFAULT_RANDOM_CHAOS_DURATION_MS
                },
                new RandomChaosOption
                {
                    Key = "wanted_5_star",
                    DisplayName = "WANTED 5 STAR",
                    DurationMs = DEFAULT_RANDOM_CHAOS_DURATION_MS
                },
                new RandomChaosOption
                {
                    Key = "earthquake",
                    DisplayName = "EARTHQUAKE",
                    DurationMs = DEFAULT_RANDOM_CHAOS_DURATION_MS
                }
            };

        private bool _isChaosRouletteActive = false;
        private bool _chaosRouletteLocked = false;

        private int _chaosRouletteStartTime = 0;
        private int _lastChaosRouletteItemChange = 0;
        private int _chaosRouletteLockEndTime = 0;
        private int _currentChaosRouletteIndex = 0;

        private bool _isChaosActive = false;
        private int _activeChaosIndex = -1;
        private string _activeChaosKey = "";
        private string _activeChaosName = "";

        private int _chaosRemainingMs = 0;
        private int _lastChaosUpdateTime = 0;

        // Override durasi sementara untuk effect yang dipanggil dari roulette.
        // Dipakai oleh RANDOM_CHAOS, RANDOM_CHECKPOINT, dan RANDOM_GATCHA.
        // Nilai ini hanya aktif selama method effect sedang dieksekusi, sehingga
        // timer default webhook di [TIME_DEFAULT] tidak ikut berubah.
        private int _modeDurationOverrideMs = 0;

        // =========================================================
        // CHAOS TANK WAVES
        // 3 Tank saat mulai, lalu 3 Tank tiap 60 detik.
        // 5 wave selama 5 menit = total 15 Tank.
        // =========================================================
        private const int CHAOS_TANK_WAVE_INTERVAL_MS =
            60 * 1000;

        private const int CHAOS_TANKS_PER_WAVE = 3;
        private const int CHAOS_TANK_TOTAL_WAVES = 5;


        // =========================================================
        // CHAOS PLAYER CAGE
        // 8 panel chain-link membentuk lingkaran rapat di sekitar player.
        // Posisi Z setiap panel mengikuti ground di titik panel tersebut.
        // =========================================================
        private const string CHAOS_CAGE_MODEL_NAME =
            "prop_fnclink_03a";

        private const int CHAOS_CAGE_TOTAL_FENCES = 8;
        private const float CHAOS_CAGE_RADIUS = 1.50f;

        private readonly List<Prop> _chaosCageProps =
            new List<Prop>();

        private Model _chaosCageModel;
        private bool _isChaosCageModelRequested = false;
        private Vector3 _chaosCageCenter = Vector3.Zero;

        // =========================================================
        // GIFT: DISARM
        //
        // Webhook:
        //   disarm
        //
        // Logic senjata memakai sistem yang sama dengan
        // Random Chaos -> NO WEAPON.
        // =========================================================
        // =========================================================
        // GIFT: SLEEP
        // =========================================================
        // Player dipaksa jatuh / ragdoll selama timer aktif.
        // Kontrol gerak dan combat dikunci selama efek berlangsung.
        // Jika gift masuk lagi saat masih aktif, durasinya ditambah.
        // =========================================================
        // =========================================================
        // GIFT: BALL RAIN
        // =========================================================
        // =========================================================
        // BALL RAIN IMPACT
        // =========================================================
        // Tidak ada custom ragdoll, custom force, atau custom damage.
        // Dampak ke player / kendaraan diserahkan ke collision dan
        // physics bawaan GTA V.
        // =========================================================

        // =========================================================
        // GIFT: DYNAMIC CAGE
        // =========================================================
        // Webhook:
        //   cage
        //
        // Berbeda dari Random Chaos PLAYER IN CAGE:
        // - Player TIDAK dipaksa turun dari vehicle.
        // - Radius cage mengikuti ukuran entity player/vehicle.
        // - Jalan kaki = cage kecil.
        // - Motorcycle = menyesuaikan footprint motorcycle.
        // - Car / truck / bus = otomatis membesar sesuai model.
        // =========================================================
        // Durasi gift Cage dibaca dari [GIFT_TIMERS] di INI.
        // Default jika key tidak ada / invalid = 15 detik.
        private int _giftCageDurationMs =
            15 * 1000;

        private const float GIFT_CAGE_WALK_RADIUS =
            1.50f;

        private const float GIFT_CAGE_EXTRA_CLEARANCE =
            1.00f;

        private const int GIFT_CAGE_MIN_FENCES =
            8;

        private const int GIFT_CAGE_MAX_FENCES =
            32;

        private const float GIFT_CAGE_FENCE_SPACING =
            2.00f;

        private readonly List<Prop> _giftCageProps =
            new List<Prop>();

        private Model _giftCageModel;
        private bool _isGiftCageModelRequested = false;
        private int _giftCageModelRequestStartTime = 0;
        private int _giftCageEndTime = 0;

        private Vector3 _giftCageCenter =
            Vector3.Zero;

        private float _giftCageRadius =
            GIFT_CAGE_WALK_RADIUS;

        private int _giftCageFenceCount =
            GIFT_CAGE_MIN_FENCES;

        // Webhook config yang memang dipanggil oleh Chaos.
        // Dipakai agar Tank / Monster Chaos dapat dibersihkan
        // ketika survival 5 menit selesai.
        private readonly HashSet<string> _chaosSpawnWebhookNames =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        // =========================================================
        // PLAYER / GAME STATE
        // =========================================================
        private int _deathCount = 0;
        private int _winCount = 0;
        private int _enemyKillCount = 0;

        // =========================================================
        // SCORE MODE
        // =========================================================
        // Score HUD adalah alternatif WINS + DEATHS.
        // Score=true  -> tampil SCORE PLAYER NAME : VALUE.
        // Score=false -> tampil WINS + DEATHS seperti biasa.
        // Nilai score boleh positif, nol, maupun negatif.
        private int _score = 0;
        private bool _scoreModeEnabled = true;

        private const int SCORE_POS_REWARD = 1;
        private const int SCORE_FINISH_REWARD = 3;
        private const int SCORE_DEATH_PENALTY_EVERY = 3;
        private const int SCORE_DEATH_PENALTY = 1;
        private const int SCORE_BACK_TO_START_PENALTY = 3;

        private int _underwaterStartTime = 0;
        private bool _isUnderwaterTimerActive = false;
        private int _underwaterCooldownEndTime = 0;

        // =========================================================
        // UNDERWATER RESCUE CONFIG
        // =========================================================
        // Enabled=true  -> sistem rescue underwater aktif.
        // Enabled=false -> script tidak ikut campur; GTA berjalan normal.
        private bool _underwaterSystemEnabled =
            true;

        private int _underwaterTriggerDurationMs =
            30 * 1000;

        private int _underwaterTeleportWarningDurationMs =
            5 * 1000;

        private float _underwaterSearchLandMaxDistance =
            100.0f;


        // =========================================================
        // CONFIG DATABASE
        // =========================================================
        // Snapshot database. Loader membangun Dictionary baru sampai selesai,
        // lalu menukar reference sekali saja. Listener webhook berjalan pada
        // background thread, jadi jangan pernah Clear()/mutate Dictionary live.
        private volatile Dictionary<string, CustomNpcConfig> _npcDatabase =
            new Dictionary<string, CustomNpcConfig>(
                StringComparer.OrdinalIgnoreCase
            );

        private volatile Dictionary<string, CustomNpcConfig> _bodyguardDatabase =
            new Dictionary<string, CustomNpcConfig>(
                StringComparer.OrdinalIgnoreCase
            );

        private volatile Dictionary<string, CustomNpcConfig> _animalDatabase =
            new Dictionary<string, CustomNpcConfig>(
                StringComparer.OrdinalIgnoreCase
            );

        // =========================================================
        // SPAWN QUEUES
        // =========================================================
        private readonly List<EnemyLoadingTracker> _enemiesToSpawn =
            new List<EnemyLoadingTracker>();

        private readonly List<PassengerLoadingTracker> _passengersToSpawn =
            new List<PassengerLoadingTracker>();

        private readonly List<BodyguardLoadingTracker> _bodyguardsToSpawn =
            new List<BodyguardLoadingTracker>();

        private readonly List<AnimalLoadingTracker> _animalsToSpawn =
            new List<AnimalLoadingTracker>();

        private readonly List<ModelLoadingTracker> _modelsToSpawn =
            new List<ModelLoadingTracker>();

        private readonly List<VehicleLoadingTracker> _vehiclesToGive =
            new List<VehicleLoadingTracker>();

        private readonly List<HitByVehicleLoadingTracker> _hitByVehiclesToSpawn =
            new List<HitByVehicleLoadingTracker>();

        private readonly List<HitByVehicleForceTracker> _hitByVehicleForceTrackers =
            new List<HitByVehicleForceTracker>();

        // =========================================================
        // ACTIVE ENTITY
        // =========================================================
        private readonly List<ActiveBossTracker> _activeBosses =
            new List<ActiveBossTracker>();

        private readonly List<ActiveBodyguardTracker> _activeBodyguards =
            new List<ActiveBodyguardTracker>();

        private readonly List<ActiveAnimalTracker> _activeAnimals =
            new List<ActiveAnimalTracker>();

        // =========================================================
        // ENTITY LIMITS - CONFIGURABLE FROM [ENTITY_LIMITS]
        // =========================================================
        private int _maxEnemyUnits = 25;
        private int _maxAnimals = 15;
        private int _maxBodyguards = 15;

        private readonly List<Vehicle> _enemyVehicles =
            new List<Vehicle>();

        // Dipakai untuk menyebar batch vehicle enemy mengelilingi player.
        // Mencegah spawn_car:10 / spawn_tank:10 muncul pada koordinat identik.
        private int _enemyVehicleSpawnSequence = 0;

        // Satu PED enemy = satu blip aktif.
        // Mencegah ghost blip setelah teleport / respawn / death.
        private readonly Dictionary<int, Blip> _enemyBlips =
            new Dictionary<int, Blip>();

        private RelationshipGroup _relMonsterGroup;
        private RelationshipGroup _relBodyguardGroup;
        private RelationshipGroup _relAnimalGroup;

        // =========================================================
        // RANDOM TELEPORT
        // =========================================================
        // [RANDOM_TELEPORT] UseGachaCards=true
        //   -> memakai roulette/card dari [LOCATIONS] seperti sistem lama.
        //
        // [RANDOM_TELEPORT] UseGachaCards=false
        //   -> memilih titik outdoor random dari road node seluruh mainland,
        //      menolak air / ground invalid, menunggu collision, lalu baru
        //      memindahkan player setelah tanah benar-benar tervalidasi.
        private bool _randomTeleportUseGachaCards = true;

        private const int MAX_TELEPORT_GACHA_QUEUE = 3;

        private const int RANDOM_TELEPORT_SAFE_NODE_ATTEMPTS = 80;
        private const int RANDOM_TELEPORT_SAFE_CANDIDATE_RETRIES = 12;
        private const int RANDOM_TELEPORT_SAFE_CANDIDATE_TIMEOUT_MS = 500;
        private const int RANDOM_TELEPORT_SAFE_GROUND_HOLD_MS = 800;
        private const float RANDOM_TELEPORT_SAFE_Z_OFFSET = 1.0f;
        private const float RANDOM_TELEPORT_SAFE_MAX_GROUND_DELTA = 8.0f;

        // RANDOM TELEPORT gacha pool.
        // Semua lokasi dari [LOCATIONS] / section lokasi lama digabung.
        private List<TeleportLocationItem> _teleportGachaPool =
            new List<TeleportLocationItem>();

        private readonly Queue<string> _teleportGachaQueue =
            new Queue<string>();

        private bool _isTeleportGachaActive = false;
        private int _teleportGachaStartTime = 0;
        private int _teleportGachaDuration = 7000;
        private int _lastTeleportGachaItemChange = 0;
        private int _teleportGachaItemChangeInterval = 80;
        private int _currentTeleportGachaIndex = 0;
        private bool _teleportGachaLocked = false;
        private int _teleportGachaLockEndTime = 0;

        // Kalau [LOCATIONS] diedit saat roulette masih berjalan, jangan ganti
        // pool di tengah lock/roulette. Reload dilakukan otomatis setelah ronde
        // gacha aktif selesai.
        private bool _teleportGachaPoolReloadPending = false;

        // Safe-global mode tidak memindahkan player sampai kandidat selesai
        // divalidasi. Ini mencegah teleport ke void / laut / Z salah.
        private bool _safeRandomTeleportPending = false;
        private Vector3 _safeRandomTeleportCandidate = Vector3.Zero;
        private int _safeRandomTeleportCandidateStartTime = 0;
        private int _safeRandomTeleportCandidateRetryCount = 0;

        // Sesudah teleport valid, entity ditahan singkat di tanah agar collision
        // area baru sempat stabil sebelum physics dilepas.
        private bool _randomTeleportGroundGuardActive = false;
        private Entity _randomTeleportGroundGuardEntity = null;
        private Vector3 _randomTeleportGroundGuardPosition = Vector3.Zero;
        private Vector3 _randomTeleportGroundGuardFallbackPosition = Vector3.Zero;
        private int _randomTeleportGroundGuardEndTime = 0;

        // =========================================================
        // RANDOM GATCHA GIFT
        // Webhook: /random_gatcha
        // Alias  : /random_gacha
        //
        // Pool effect:
        // sleep, ball_rain, invincible, wanted_5_star, teleport_sky,
        // police_roadblock, random_explosion_nearby, reckless_traffic,
        // traffic_magnet.
        // =========================================================
        private readonly List<RandomGatchaOption> _randomGatchaOptions =
            new List<RandomGatchaOption>
            {
                new RandomGatchaOption { Key = "sleep", DisplayName = "SLEEP" },
                new RandomGatchaOption { Key = "ball_rain", DisplayName = "BALL RAIN" },
                new RandomGatchaOption { Key = "invincible", DisplayName = "INVINCIBLE" },
                new RandomGatchaOption { Key = "wanted_5_star", DisplayName = "WANTED 5 STAR" },
                new RandomGatchaOption { Key = "teleport_sky", DisplayName = "TELEPORT SKY" },
                new RandomGatchaOption { Key = "police_roadblock", DisplayName = "POLICE ROADBLOCK" },
                new RandomGatchaOption { Key = "random_explosion_nearby", DisplayName = "RANDOM EXPLOSION" },
                new RandomGatchaOption { Key = "reckless_traffic", DisplayName = "RECKLESS TRAFFIC" },
                new RandomGatchaOption { Key = "traffic_magnet", DisplayName = "TRAFFIC MAGNET" }
            };

        private readonly Queue<string> _randomGatchaQueue =
            new Queue<string>();

        private const int MAX_RANDOM_GATCHA_QUEUE = 3;

        // Timing RANDOM_GATCHA sekarang editable dari [RANDOM_GATCHA].
        private int _randomGatchaRollMs = 7000;
        private int _randomGatchaLockMs = 3000;
        private int _randomGatchaSwitchMs = 95;

        private bool _randomGatchaActive = false;
        private bool _randomGatchaLocked = false;
        private int _randomGatchaStartTime = 0;
        private int _randomGatchaLockEndTime = 0;
        private int _randomGatchaLastSwitchTime = 0;
        private int _randomGatchaIndex = 0;
        private bool _randomGatchaEffectExecuted = false;

        // RANDOM GATCHA HUD: tema sama dengan HUD modern, layout berbeda
        // dari RANDOM TELEPORT. Semua key opsional dari [HUD_RANDOM_GATCHA].
        private bool _hudRandomGatchaEnabled = true;
        private float _randomGatchaUiX = 0.50f;
        private float _randomGatchaUiY = 0.445f;
        private float _randomGatchaUiWidth = 0.50f;
        private float _randomGatchaUiHeight = 0.145f;
        private int _randomGatchaAccentRollingR = 255;
        private int _randomGatchaAccentRollingG = 184;
        private int _randomGatchaAccentRollingB = 64;
        private int _randomGatchaAccentLockedR = 68;
        private int _randomGatchaAccentLockedG = 210;
        private int _randomGatchaAccentLockedB = 100;

        // =========================================================================
        // PLAYER WEAPONS
        // =========================================================================
        // Daftar weapon TIDAK lagi hardcoded di C#.
        // Sumber: MoonModConfig.ini -> [PLAYER_WEAPONS]
        //
        // Weapon dapat ditulis dengan nama GTA (WEAPON_APPISTOL)
        // atau hash hex (0x22D8FE39).
        // =========================================================
        private readonly List<WeaponHash> _defaultPlayerWeapons =
            new List<WeaponHash>();
        // =========================================================
        // STANDALONE WEBHOOK EFFECTS - REVISED V2
        // Belum dimasukkan ke Random Chaos / Checkpoint / Distance Event.
        //
        // DIHAPUS TOTAL:
        // - vehicle_shrink
        // - traffic_nitro (digabung ke reckless_traffic)
        // - no_vehicle_entry
        // - road_block
        // - oil_trail
        // =========================================================

        // U-TURN = one-shot, support vehicle + on-foot.

        // GET TOWED
        private Vehicle _getTowedTowTruck = null;
        private Ped _getTowedDriver = null;
        private Vehicle _getTowedTargetVehicle = null;
        private int _getTowedEndTime = 0;
        private int _getTowedDurationMs = 30 * 1000;

        // SAFE GET TOWED:
        // - TIDAK memakai native tow-hook GTA lagi.
        // - Semua target memakai rigid entity attachment.
        // - Boat / heli / plane / train ditolak.
        // - Attachment dan task driver direfresh lebih jarang untuk stabilitas.
        private int _nextGetTowedAttachmentRefreshTime = 0;
        private int _nextGetTowedDriverTaskRefreshTime = 0;
        private int _nextGetTowedRespawnAttemptTime = 0;
        // SAFE GET TOWED:
        // Jangan spam attachment/task native setiap frame.
        private const int GET_TOWED_ATTACHMENT_REFRESH_MS = 750;
        private const int GET_TOWED_DRIVER_TASK_REFRESH_MS = 2500;
        private const int GET_TOWED_RESPAWN_RETRY_MS = 1000;

        // TRAFFIC MAGNET
        // HANYA aktif saat player berada di kendaraan. Semua traffic vehicle
        // ditarik ke kendaraan player (bukan ke karakter). Begitu traffic vehicle
        // menyentuh kendaraan player, vehicle tersebut langsung dihapus.
        private int _trafficMagnetEndTime = 0;
        private int _nextTrafficMagnetUpdateTime = 0;
        private int _trafficMagnetDurationMs = 30 * 1000;
        private const float TRAFFIC_MAGNET_RADIUS = 90.0f;
        private const float TRAFFIC_MAGNET_PULL_SPEED = 30.0f;
        private const int TRAFFIC_MAGNET_UPDATE_INTERVAL_MS = 50;
        private const int BLACKHOLE_EFFECT_UPDATE_INTERVAL_MS = 50;

        // RECKLESS TRAFFIC
        // Semua traffic ROAD VEHICLE yang sudah di-stream GTA dalam radius
        // 500 meter dibuat rusuh/agresif dan diberi NOS/torque boost.
        private int _recklessTrafficEndTime = 0;
        private int _nextRecklessTrafficUpdateTime = 0;
        private int _nextRecklessTrafficNitroTime = 0;
        private int _recklessTrafficDurationMs = 60 * 1000;

        private const float RECKLESS_TRAFFIC_RADIUS = 500.0f;
        private const int RECKLESS_TRAFFIC_SCAN_INTERVAL_MS = 400;
        private const int RECKLESS_TRAFFIC_NITRO_INTERVAL_MS = 650;
        private const float RECKLESS_TRAFFIC_DRIVE_SPEED = 65.0f;
        private const float RECKLESS_TRAFFIC_NOS_POWER = 1.80f;
        private const float RECKLESS_TRAFFIC_NOS_MIN_SPEED = 30.0f;
        private const float RECKLESS_TRAFFIC_NOS_SPEED_ADD = 14.0f;
        private const float RECKLESS_TRAFFIC_NOS_MAX_SPEED = 55.0f;

        private readonly List<Vehicle> _recklessTrafficTouchedVehicles =
            new List<Vehicle>();

        // RANDOM WEATHER = one-shot.

        // VEHICLE FIRE TIMER
        // Effect aktif selama durasi (default 60 detik).
        // HANYA berjalan saat player sedang berada di kendaraan:
        // 5 detik warning -> kendaraan terbakar -> 5 detik -> meledak.
        // Setelah meledak / player pindah kendaraan, cycle dimulai lagi
        // selama effect 60 detik masih aktif. Saat player turun, tidak ada warning.
        private int _vehicleFireTimerEndTime = 0;
        private int _vehicleFireTimerDurationMs = 60 * 1000;
        private Vehicle _vehicleFireTarget = null;
        private int _vehicleFireIgniteTime = 0;
        private int _vehicleFireExplodeTime = 0;
        private bool _vehicleFireIgnited = false;
        private const int VEHICLE_FIRE_WARNING_MS = 5 * 1000;
        private const int VEHICLE_FIRE_TO_EXPLOSION_MS = 5 * 1000;

        // BURNING
        // Webhook: /burning
        // 5 detik countdown -> karakter terbakar. Tidak ada ledakan.
        // Durasi api default 10 detik dan dapat diubah dari [TIME_DEFAULT].
        private int _burningCountdownDurationMs = 5 * 1000;
        private int _burningDurationMs = 10 * 1000;
        private int _burningIgniteTime = 0;
        private int _burningEndTime = 0;
        private bool _burningIgnited = false;
        // Jangan START_ENTITY_FIRE setiap frame karena dapat me-reset FX api.
        // Hanya cek berkala dan restart jika native fire benar-benar padam.
        private int _nextBurningFireEnsureTime = 0;
        private const int BURNING_FIRE_ENSURE_INTERVAL_MS = 750;

        // POLICE ROADBLOCK
        // Selama 60 detik, batch roadblock baru terus dibuat
        // beberapa meter di depan arah perjalanan player.
        private int _policeRoadblockEndTime = 0;
        private int _nextPoliceRoadblockSpawnTime = 0;
        private int _policeRoadblockDurationMs = 60 * 1000;
        private const int POLICE_ROADBLOCK_SPAWN_INTERVAL_MS = 5000;
        private const int POLICE_ROADBLOCK_BATCH_DESPAWN_MS = 9000;
        private const float POLICE_ROADBLOCK_AHEAD_DISTANCE = 20.0f;
        // Satu batch = TOTAL 5 RIOT dalam satu garis roadblock, bukan 5 baris.
        private const int POLICE_ROADBLOCK_VEHICLE_COUNT = 5;
        // Jarak pusat-ke-pusat diperbesar agar body RIOT tidak saling menempel.
        private const float POLICE_ROADBLOCK_SIDE_SPACING = 5.5f;

        private readonly List<Vehicle> _policeRoadblockVehicles =
            new List<Vehicle>();

        // RANDOM EXPLOSION NEARBY
        // 5 ledakan sekaligus setiap 1 detik selama 60 detik.
        private int _randomExplosionEndTime = 0;
        private int _nextRandomExplosionTime = 0;
        private int _randomExplosionDurationMs = 60 * 1000;
        private int _randomExplosionTotal = 300;
        private int _randomExplosionSpawnedCount = 0;
        private int _randomExplosionIntervalMs = 200;

        private int _blackholeEndTime = 0;
        private int _nextBlackholeEffectUpdateTime = 0;
        private int _superSpeedEndTime = 0;
        private int _invincibleEndTime = 0;

        // =========================================================
        // TIME DEFAULT / GIFT TIMER CONFIG
        //
        // Semua nilai di bawah dapat dioverride dari:
        // [TIME_DEFAULT] di GoToMountain.ini.
        //
        // Config direload saat gift dipanggil, jadi setelah edit + SAVE
        // INI, gift berikutnya langsung memakai nilai baru tanpa rebuild.
        // =========================================================
        private int _giftDisarmDurationMs = 15 * 1000;
        private int _giftDisarmEndTime = 0;

        // =========================================================
        // GIFT: SLEEP
        // Webhook:
        //   sleep
        //
        // Default = 5 detik.
        // Durasi bisa diubah lewat [TIME_DEFAULT] -> SleepSeconds.
        // =========================================================
        private int _giftSleepDurationMs =
            5 * 1000;

        private int _giftSleepEndTime =
            0;

        // Saat SLEEP dipicu ketika player sedang di kendaraan,
        // pakai bail-out (bukan teleport keluar), lalu lanjut ragdoll.
        private bool _giftSleepVehicleBailoutPending =
            false;

        // Jangan restart animasi keluar kendaraan setiap frame.
        // Kalau masih tertahan di kursi, retry task setelah interval ini.
        private int _giftSleepVehicleBailoutRetryTime =
            0;

        private const int SLEEP_VEHICLE_BAILOUT_RETRY_MS =
            750;

        // =========================================================
        // GIFT: BALL RAIN
        // Webhook:
        //   ball_rain
        //
        // Semua profile disusun dari bola paling kecil -> paling besar.
        // Model invalid otomatis dilewati.
        // =========================================================
        private int _giftRockRainDurationMs =
            10 * 1000;

        private int _rockRainEndTime =
            0;

        private int _nextRockRainSpawnTime =
            0;

        private const int ROCK_RAIN_SPAWN_INTERVAL_MS =
            180;

        private int _rockRainModelRequestStartTime =
            0;

        private int _rockRainBallCycleIndex =
            0;

        private readonly List<RockRainBallProfile> _rockRainBallProfiles =
            new List<RockRainBallProfile>
            {
                                // =========================================================
                // BALL RAIN - BIG BALLS ONLY
                // Hanya prop bola besar / stunt-scale GTA V.
                // Bola kecil/normal seperti pool, golf, tennis,
                // basketball, volleyball, bowling normal, beachball
                // sengaja DIHAPUS dari pool.
                // =========================================================

                // LARGE STUNT SOCCER BALL
                new RockRainBallProfile
                {
                    ModelName = "stt_prop_stunt_soccer_sball",
                    GroundOffset = 0.85f,
                    ImpactRadius = 1.35f,
                    Damage = 130,
                    ImpactForce = 3.5f
                },

                // GIANT STUNT BOWLING BALL
                new RockRainBallProfile
                {
                    ModelName = "stt_prop_stunt_bowling_ball",
                    GroundOffset = 1.35f,
                    ImpactRadius = 1.85f,
                    Damage = 190,
                    ImpactForce = 5.0f
                },

                // GIANT STUNT SOCCER BALL
                new RockRainBallProfile
                {
                    ModelName = "stt_prop_stunt_soccer_ball",
                    GroundOffset = 1.70f,
                    ImpactRadius = 2.20f,
                    Damage = 240,
                    ImpactForce = 6.5f
                },

                // EXTRA LARGE STUNT SOCCER BALL
                new RockRainBallProfile
                {
                    ModelName = "stt_prop_stunt_soccer_lball",
                    GroundOffset = 3.20f,
                    ImpactRadius = 3.70f,
                    Damage = 400,
                    ImpactForce = 9.0f
                }

            };

        private readonly List<RockRainPropTracker> _rockRainProps =
            new List<RockRainPropTracker>();

        private int _giftSuperSpeedDurationMs = 3 * 1000;

        // SUPERSPEED vehicle initial launch (m/s added once per activation).
        // This launch does NOT require the accelerator button, so a stationary
        // vehicle immediately gets pushed forward and can keep coasting.
        private float _superSpeedVehicleInitialBoost = 18.0f;
        private float _superSpeedBicycleInitialBoost = 8.0f;

        private int _giftInvincibleDurationMs = 7 * 1000;

        // Satu timer Blackhole. Setiap gift Blackhole berikutnya menambah
        // durasi yang sama ke total timer; tidak ada Initial/Extend terpisah.
        private int _giftBlackholeDurationMs = 60 * 1000;

        private int _giftRandomTeleportRouletteDurationMs = 7 * 1000;
        private int _giftRandomTeleportLockDurationMs = 4 * 1000;

        // =========================================================
        // GIFT: EARTHQUAKE M8 - FINAL
        // =========================================================
        // Webhook:
        //   earthquake
        //
        // Durasi final: 60 detik.
        //
        // V3: FOKUS FISIK, BUKAN KAMERA.
        // - camera shake sangat ringan supaya viewer tidak pusing
        // - getaran fisik cepat setiap ~140 ms
        // - semua PED jalan kaki terus ragdoll selama gempa
        // - player jalan kaki terus terguncang / ragdoll
        // - semua kendaraan sekitar rocking / bergeser / terangkat
        // - loose props / object physics sekitar ikut terguncang
        // - enemy/bodyguard/animal ikut terkena karena termasuk nearby PED
        // - radius besar supaya lingkungan terlihat benar-benar gempa
        //
        // Tidak memakai teleport/freeze untuk efek earthquake.
        // =========================================================
        // Durasi webhook earthquake dibaca dari [GIFT_TIMERS].
        // Default jika key tidak ada / invalid = 60 detik.
        private int _giftEarthquakeDurationMs =
            60 * 1000;

        private const int EARTHQUAKE_PULSE_INTERVAL_MS =
            140;

        private const int EARTHQUAKE_CAMERA_KICK_INTERVAL_MS =
            220;

        private const float EARTHQUAKE_ENTITY_RADIUS =
            80.0f;

        private int _earthquakeEndTime =
            0;

        private int _nextEarthquakePulseTime =
            0;

        private int _nextEarthquakeCameraKickTime =
            0;

        private int _earthquakePulseIndex =
            0;

        private bool _earthquakeCameraStarted =
            false;

        // =========================================================
        // BLACKHOLE <-> EARTHQUAKE MUTUAL QUEUE
        // Kedua effect ini TIDAK BOLEH aktif bersamaan.
        // Jika lawannya sedang aktif, durasi gift disimpan di queue
        // dan baru dimulai setelah effect aktif selesai.
        // =========================================================
        private int _pendingBlackholeDurationMs =
            0;

        private int _pendingEarthquakeDurationMs =
            0;

        // =========================================================
        // GIFT: NEVER WANTED -> TEMPORARY 5 STAR / TELEPORT SKY
        // =========================================================
        // NORMAL:
        //   player selalu NEVER WANTED.
        //
        // GIFT /wanted_5_star:
        //   wanted langsung 5 bintang sesuai WantedFiveStarSeconds atau sampai CUSTOM DEATH.
        //
        // Setelah timer habis atau custom death:
        //   kembali NEVER WANTED otomatis.
        // =========================================================
        private bool _wantedFiveGiftActive =
            false;

        // Durasi webhook wanted_5_star dibaca dari [GIFT_TIMERS].
        // Default jika key tidak ada / invalid = 300 detik.
        // Kalau player mati lebih dulu, gift langsung selesai.
        private int _giftWantedFiveStarDurationMs =
            5 * 60 * 1000;

        private int _wantedFiveGiftEndTime =
            0;

        private const float TELEPORT_SKY_HEIGHT =
            1000.0f;

        private const uint PARACHUTE_WEAPON_HASH =
            0xFBAB5776u;

        private Prop _blackHoleProp;
        private Vector3 _blackHoleCenter;
        private BlackHoleLoadingTracker _blackHoleToSpawn = null;

        // =========================================================
        // BLACKHOLE PLAYER ANTI-STUCK
        // =========================================================
        // Hanya berlaku untuk player / kendaraan player.
        // NPC, enemy, animal, bodyguard dan vehicle lain tetap
        // memakai physics Blackhole normal.
        //
        // Jika player selama beberapa waktu hampir tidak naik,
        // padahal Blackhole masih menarik dari atas, script menganggap
        // player tertahan plafon / bangunan / map collision.
        //
        // Solusi:
        // - collision player / kendaraan player OFF sebentar
        // - beri escape velocity menuju Blackhole
        // - collision otomatis ON lagi
        // =========================================================
        private Vector3 _blackholeAntiStuckLastPosition =
            Vector3.Zero;

        private int _blackholeAntiStuckTrackedHandle =
            0;

        private int _blackholeAntiStuckLastSampleTime =
            0;

        private int _blackholeAntiStuckNoProgressMs =
            0;

        private int _blackholeAntiStuckPhaseEndTime =
            0;

        private int _blackholeAntiStuckCooldownEndTime =
            0;

        private int _blackholeAntiStuckPhasedHandle =
            0;

        private const int BLACKHOLE_STUCK_SAMPLE_INTERVAL_MS =
            500;

        private const int BLACKHOLE_STUCK_REQUIRED_MS =
            1500;

        private const float BLACKHOLE_STUCK_MIN_Z_PROGRESS =
            0.75f;

        private const int BLACKHOLE_ESCAPE_PHASE_MS =
            650;

        private const int BLACKHOLE_ESCAPE_COOLDOWN_MS =
            1500;

        private const float BLACKHOLE_ESCAPE_SPEED =
            28.0f;

        private const float BLACKHOLE_ESCAPE_MIN_VERTICAL_SPEED =
            18.0f;

        // Jangan phase kalau entity sudah sangat dekat dengan pusat Blackhole.
        private const float BLACKHOLE_ESCAPE_MIN_DISTANCE_TO_CENTER =
            8.0f;

        // =========================================================
        // DESIGN / HUD CONFIG
        // Semua nilai visual dibaca dari:
        // scripts/MoonHUD.ini
        // =========================================================
        private float _uiRenderDistance = 20.0f;

        // =========================================================
        // GLOBAL HUD TEXT STYLE
        // =========================================================
        private bool _normalTextShadowEnabled = false;
        private int _normalTextShadowDistance = 0;
        private int _normalTextShadowR = 0;
        private int _normalTextShadowG = 0;
        private int _normalTextShadowB = 0;
        private int _normalTextShadowA = 0;

        private bool _normalTextEdgeEnabled = true;
        private int _normalTextEdgeSize = 1;
        private int _normalTextEdgeR = 0;
        private int _normalTextEdgeG = 0;
        private int _normalTextEdgeB = 0;
        private int _normalTextEdgeA = 205;

        private bool _boldTextShadowEnabled = true;
        private int _boldTextShadowDistance = 2;
        private int _boldTextShadowR = 0;
        private int _boldTextShadowG = 0;
        private int _boldTextShadowB = 0;
        private int _boldTextShadowA = 255;

        private bool _boldTextEdgeEnabled = true;
        private int _boldTextEdgeSize = 2;
        private int _boldTextEdgeR = 0;
        private int _boldTextEdgeG = 0;
        private int _boldTextEdgeB = 0;
        private int _boldTextEdgeA = 255;

        // Small X offsets previously hardcoded in renderer.
        private float _winTextOffsetX = 0.005f;
        private float _winTextOffsetY = -0.006f;
        private float _winValueTextOffsetY = -0.006f;
        private float _deathTextOffsetX = 0.005f;
        private float _deathTextOffsetY = -0.006f;
        private float _deathValueTextOffsetY = -0.006f;
        private float _apocalypseTextOffsetX = 0.005f;

        // Apocalypse / Chaos text templates.
        private string _apocalypseCountdownLine1Format = "{0} Left";
        private string _apocalypseCountdownLine2 = "RANDOM CHAOS";
        private string _apocalypseChaosLine1Format = "CHAOS : {0}";
        private string _apocalypseChaosLine2Format = "{0}";

        // Progress text templates.
        private string _progressDistanceTextFormat = "{0}m";
        private string _progressMissionGoToMountainText = "MISSION : GO TO MOUNTAIN";
        private string _progressMissionSurvivalText = "MISSION : SURVIVAL MODE";
        private string _progressMissionNormalText = "MISSION : GO TO FINISH LINE";
        private string _progressPosTextFormat = "POS {0}/{1}";
        private string _progressMissionWithPosFormat = "{0} | {1}";
        private bool _progressMissionTextUppercase = true;

        // =========================================================
        // CHECKPOINT / FINISH COUNTDOWN HUD
        // =========================================================
        private bool _hudCheckpointCountdownEnabled = true;
        private float _checkpointCountdownX = 0.50f;
        private float _checkpointCountdownY = 0.18f;
        private float _checkpointCountdownScale = 2.2f;
        private int _checkpointCountdownFont = 7;
        private int _checkpointCountdownR = 255;
        private int _checkpointCountdownG = 180;
        private int _checkpointCountdownB = 0;
        private int _checkpointCountdownA = 255;
        private string _checkpointCountdownTextFormat = "{0}";

        private bool _hudFinishCountdownEnabled = true;
        private float _finishCountdownX = 0.50f;
        private float _finishCountdownY = 0.20f;
        private float _finishCountdownScale = 2.5f;
        private int _finishCountdownFont = 7;
        private int _finishCountdownR = 0;
        private int _finishCountdownG = 255;
        private int _finishCountdownB = 100;
        private int _finishCountdownA = 255;
        private string _finishCountdownTextFormat = "{0}";

        // =========================================================
        // WORLD ROUTE MARKER HUD
        // =========================================================
        private bool _hudRouteMarkerEnabled = true;
        private float _routeMarkerRenderDistance = 200.0f;
        private MarkerType _routeMarkerType = MarkerType.Cylinder;
        private float _routeMarkerOffsetZ = 0.9f;

        private float _routePosMarkerSizeX = 5.0f;
        private float _routePosMarkerSizeY = 5.0f;
        private float _routePosMarkerSizeZ = 100.0f;
        private int _routePosMarkerR = 255;
        private int _routePosMarkerG = 165;
        private int _routePosMarkerB = 0;
        private int _routePosMarkerA = 180;

        private float _routeFinishMarkerSizeX = 8.0f;
        private float _routeFinishMarkerSizeY = 8.0f;
        private float _routeFinishMarkerSizeZ = 100.0f;
        private int _routeFinishMarkerR = 0;
        private int _routeFinishMarkerG = 255;
        private int _routeFinishMarkerB = 100;
        private int _routeFinishMarkerA = 200;

        // =========================================================
        // ROUTE MAP BLIP HUD
        // =========================================================
        private bool _hudRouteBlipEnabled = true;
        private bool _routeBlipShowRoute = false;

        // POS dan FINISH dapat ditampilkan / disembunyikan secara terpisah
        // dari MoonHUD.ini [HUD_ROUTE_BLIP].
        private bool _routePosBlipEnabled = true;
        private bool _routeFinishBlipEnabled = true;

        private BlipSprite _routePosBlipSprite = BlipSprite.Standard;
        private BlipColor _routeActivePosBlipColor = BlipColor.Green;
        private float _routeActivePosBlipScale = 1.4f;

        private BlipSprite _routeFinishBlipSprite = BlipSprite.RaceFinish;
        private BlipColor _routeActiveFinishBlipColor = BlipColor.Green;
        private float _routeActiveFinishBlipScale = 1.4f;
        private BlipColor _routeFutureFinishBlipColor = BlipColor.Purple;
        private float _routeFutureFinishBlipScale = 0.8f;

        private string _routeDirectMissionFinishName = "[GO TO MOUNTAIN] FINISH";
        private string _routeMissionPosNameFormat = "[GO TO MOUNTAIN] POS {0}";
        private string _routeNormalPosNameFormat = "[TARGET] POS {0}";
        private string _routeMissionFinishName = "[GO TO MOUNTAIN] FINISH";
        private string _routeNormalFinishName = "[TARGET] FINISH LINE";
        private string _routeFutureMissionFinishName = "GO TO MOUNTAIN FINISH";
        private string _routeFutureNormalFinishName = "FINISH LINE";

        private float _hpTextOffsetY = -0.016f;
        private string _healthPanelLabel = "HEALTH";

        private float _healthTrackHeight = 0.014f;
        private float _healthTrackOffsetY = 0.022f;
        private float _healthValueColumnRatio = 0.55f;
        private float _healthBorderSize = 0.001f;
        private int _healthBorderR = 154, _healthBorderG = 177, _healthBorderB = 192, _healthBorderA = 240;


        // Progress pin + mission text
        private float _progressPinOuterWidth = 0.007f;
        private float _progressPinOuterExtraHeight = 0.014f;
        private int _progressPinOuterR = 255;
        private int _progressPinOuterG = 255;
        private int _progressPinOuterB = 255;
        private int _progressPinOuterA = 255;

        private float _progressPinInnerWidth = 0.0028f;
        private float _progressPinInnerExtraHeight = 0.008f;
        private int _progressPinInnerR = 20;
        private int _progressPinInnerG = 20;
        private int _progressPinInnerB = 20;
        private int _progressPinInnerA = 255;

        private float _missionTextOffsetX = 0.000f;
        private float _missionTextOffsetY = 0.045f;
        private float _missionTextScale = 0.80f;
        private float _missionTextWidthMultiplier = 0.90f;
        private int _missionTextR = 255;
        private int _missionTextG = 255;
        private int _missionTextB = 0;
        private int _missionTextA = 255;
        private int _missionTextFont = 7;

        // =========================================================
        // MODERN ROUTE PROGRESS PANEL
        // =========================================================
        // PosX / PosY = pusat panel. Width / Height di [HUD_PROGRESS]
        // tetap dipakai untuk TRACK progress, bukan ukuran panel.
        private float _progressPanelWidth = 0.500f;
        private float _progressPanelHeight = 0.105f;
        private float _progressPanelCornerRadius = 0.018f;

        private int _progressPanelR = 248;
        private int _progressPanelG = 250;
        private int _progressPanelB = 253;
        private int _progressPanelA = 245;

        private float _progressPanelShadowOffsetY = 0.004f;
        private float _progressPanelShadowExtraWidth = 0.006f;
        private float _progressPanelShadowExtraHeight = 0.006f;
        private int _progressPanelShadowR = 0;
        private int _progressPanelShadowG = 0;
        private int _progressPanelShadowB = 0;
        private int _progressPanelShadowA = 70;

        // Garis pemisah di dalam panel agar HUD tidak terlihat terlalu flat.
        // HorizontalDividerOffsetY = posisi garis yang memisahkan text row dan progress track.
        // Left/RightDividerOffsetX = pembatas area kiri / tengah / kanan.
        private bool _progressPanelDividerEnabled = true;
        private float _progressPanelHorizontalDividerOffsetY = 0.010f;
        private float _progressPanelDividerThickness = 0.0012f;
        private float _progressPanelLeftDividerOffsetX = -0.175f;
        private float _progressPanelRightDividerOffsetX = 0.175f;
        private int _progressPanelDividerR = 188;
        private int _progressPanelDividerG = 197;
        private int _progressPanelDividerB = 210;
        private int _progressPanelDividerA = 125;

        private float _progressTopTextOffsetY = -0.030f;
        private float _progressTrackOffsetY = 0.026f;

        private float _progressPercentOffsetX = -0.195f;
        private float _progressPercentOffsetY = 0.000f;
        private float _progressPercentScale = 1.35f;
        private int _progressPercentFont = 4;
        private int _progressPercentR = 46;
        private int _progressPercentG = 105;
        private int _progressPercentB = 255;
        private int _progressPercentA = 255;

        private float _progressRightOffsetX = 0.195f;
        private float _progressRightOffsetY = 0.000f;
        private float _progressRightScale = 0.95f;
        private int _progressRightFont = 4;
        private int _progressRightR = 100;
        private int _progressRightG = 112;
        private int _progressRightB = 140;
        private int _progressRightA = 255;

        // Caption kiri dan kanan sengaja DIPISAH.
        // PROGRESS dan POS punya ukuran + posisi sendiri supaya layout tidak ikut berubah bersama.
        private string _progressCaptionText = "PROGRESS";
        private bool _progressHostNameEnabled = true;
        private float _progressHostTextScaleRatio = 0.80f;
        private bool _progressHostNameUppercase = true;

        // Nama host punya warna sendiri agar berbeda dari caption PROGRESS.
        private int _progressHostTextR = 255;
        private int _progressHostTextG = 204;
        private int _progressHostTextB = 90;
        private int _progressHostTextA = 255;

        // Panel progress dapat melebar otomatis mengikuti panjang nama host.
        private bool _progressDynamicPanelEnabled = true;
        private float _progressDynamicPanelMaxWidth = 0.94f;
        private float _progressDynamicPanelPadding = 0.018f;

        private float _progressCaptionOffsetX = 0.000f;
        private float _progressCaptionOffsetY = 0.049f;
        private float _progressCaptionScale = 0.40f;

        private string _progressPosCaptionText = "POS";

        // GO TO MOUNTAIN direct mode (GotoMountainPos=false).
        // Caption + value sengaja terpisah supaya tampil dua baris:
        // TARGET
        // MOUNTAIN
        // dan dapat diedit dari MoonHUD.ini tanpa compile ulang.
        private string _progressDirectCaptionText = "TARGET";

        // Legacy fallback + text khusus tiap gameplay mode.
        private string _progressDirectValueText = "MOUNTAIN";
        private string _progressDirectGoToMountainValueText = "MOUNTAIN";
        private string _progressDirectSurvivalValueText = "FINISH LINE";

        private float _progressPosCaptionOffsetX = 0.000f;
        private float _progressPosCaptionOffsetY = -0.006f;
        private float _progressPosCaptionScale = 0.52f;

        // Posisi vertikal value kanan (contoh 1/7) khusus layout NORMAL.
        // Dipisah dari RightTextOffsetY supaya state Random Checkpoint lama tidak ikut berubah.
        private float _progressRightValueOffsetY = 0.020f;

        private int _progressCaptionR = 155;
        private int _progressCaptionG = 175;
        private int _progressCaptionB = 200;
        private int _progressCaptionA = 255;

        private int _progressMissionAccentR = 49;
        private int _progressMissionAccentG = 169;
        private int _progressMissionAccentB = 255;
        private int _progressMissionAccentA = 255;

        private int _progressTrackR = 215;
        private int _progressTrackG = 222;
        private int _progressTrackB = 234;
        private int _progressTrackA = 255;

        private bool _progressPinEnabled = false;

        // =========================================================
        // RANDOM CHECKPOINT HUD
        // =========================================================
        private bool _hudRandomCheckpointEnabled = true;

        // Tick merah pendek DI DALAM progress bar.
        // HeightMultiplier < 1.0 menjaga marker tidak keluar dari track
        // dan tidak bertabrakan dengan meter / mission text.
        private float _randomCheckpointMarkerWidth = 0.0015f;
        private float _randomCheckpointMarkerHeightMultiplier = 0.62f;
        private float _randomCheckpointMarkerOffsetY = 0.0f;
        private int _randomCheckpointMarkerR = 220;
        private int _randomCheckpointMarkerG = 55;
        private int _randomCheckpointMarkerB = 55;
        private int _randomCheckpointMarkerA = 220;

        // Flag kecil pada ujung marker agar menyerupai design mockup.
        private bool _randomCheckpointMarkerFlagEnabled = true;
        private float _randomCheckpointMarkerFlagWidth = 0.010f;
        private float _randomCheckpointMarkerFlagHeight = 0.006f;
        private float _randomCheckpointMarkerFlagOffsetY = -0.009f;

        // Warna progress track ketika sequence Random Checkpoint aktif.
        private int _randomCheckpointCountdownBarR = 255;
        private int _randomCheckpointCountdownBarG = 82;
        private int _randomCheckpointCountdownBarB = 82;
        private int _randomCheckpointCountdownBarA = 255;

        private int _randomCheckpointGachaBarR = 255;
        private int _randomCheckpointGachaBarG = 149;
        private int _randomCheckpointGachaBarB = 0;
        private int _randomCheckpointGachaBarA = 255;

        private int _randomCheckpointLockedBarR = 46;
        private int _randomCheckpointLockedBarG = 204;
        private int _randomCheckpointLockedBarB = 113;
        private int _randomCheckpointLockedBarA = 255;

        // UI countdown / roulette / locked.
        private float _randomCheckpointUiX = 0.50f;
        private float _randomCheckpointUiY = 0.48f;

        private string _randomCheckpointTitle = "RANDOM CHECK POINT";
        private float _randomCheckpointTitleScale = 1.05f;
        private int _randomCheckpointTitleFont = 7;
        private int _randomCheckpointTitleR = 255;
        private int _randomCheckpointTitleG = 35;
        private int _randomCheckpointTitleB = 35;
        private int _randomCheckpointTitleA = 255;

        private float _randomCheckpointCountdownOffsetY = 0.075f;
        private float _randomCheckpointCountdownScale = 1.80f;
        private int _randomCheckpointCountdownFont = 7;
        private int _randomCheckpointCountdownR = 255;
        private int _randomCheckpointCountdownG = 35;
        private int _randomCheckpointCountdownB = 35;
        private int _randomCheckpointCountdownA = 255;

        private float _randomCheckpointGachaOffsetY = 0.075f;
        private float _randomCheckpointGachaScale = 0.90f;
        private int _randomCheckpointGachaFont = 7;
        private int _randomCheckpointGachaR = 255;
        private int _randomCheckpointGachaG = 255;
        private int _randomCheckpointGachaB = 255;
        private int _randomCheckpointGachaA = 255;

        private string _randomCheckpointLockedText = "LOCKED";
        private float _randomCheckpointLockedOffsetY = 0.135f;
        private float _randomCheckpointLockedScale = 0.65f;
        private int _randomCheckpointLockedFont = 7;
        private int _randomCheckpointLockedR = 255;
        private int _randomCheckpointLockedG = 35;
        private int _randomCheckpointLockedB = 35;
        private int _randomCheckpointLockedA = 255;

        // Custom death overlay
        private float _deathOverlayX = 0.50f;
        private float _deathOverlayY = 0.50f;
        private float _deathOverlayWidth = 1.0f;
        private float _deathOverlayHeight = 1.0f;
        private int _deathOverlayR = 0;
        private int _deathOverlayG = 0;
        private int _deathOverlayB = 0;
        private int _deathOverlayMaxA = 255;

        private float _deathRespawnTextX = 0.50f;
        private float _deathRespawnTextY = 0.46f;
        private float _deathRespawnTextScale = 1.75f;
        private int _deathRespawnTextR = 255;
        private int _deathRespawnTextG = 255;
        private int _deathRespawnTextB = 255;
        private int _deathRespawnTextA = 255;
        private int _deathRespawnTextFont = 7;
        private string _deathRespawnTextFormat = "RESPAWN IN {0}";

        // =========================================================
        // HUD GIFT - DIBAGI 2 BENTUK
        //
        // 1) INSTANT
        //    - box lebih pendek
        //    - text rata tengah
        //    - tanpa timer
        //
        // 2) TIMER / ACTIVE EFFECT
        //    - box lebih tinggi
        //    - nama effect di kiri
        //    - countdown di kanan
        //
        // WARNA BELUM DIBEDAKAN.
        // Semua masih memakai warna netral global.
        // =========================================================

        // -------------------------
        // INSTANT HUD
        // -------------------------
        private float _instantHudX = 0.50f;
        private float _instantHudStartY = 0.69f;
        private float _instantHudBoxSpacing = 0.058f;
        private float _instantHudBoxWidth = 0.300f;
        private float _instantHudBoxHeight = 0.048f;
        private float _instantHudBorderSize = 0.0030f;
        private float _instantHudDashLength = 0.018f;
        private float _instantHudDashGap = 0.010f;
        private int _instantHudDurationMs = 1000;

        // INSTANT HUD hanya boleh menampilkan 1 kartu.
        // Gift instant baru langsung mengganti kartu yang sedang tampil.
        // Setelah gift instant terakhir, kartu bertahan selama DurationMs lalu hilang.

        // GLOBAL COLOR - INSTANT
        // BORDER selalu PUTIH.
        // GOOD/BAD dibedakan dari BACKGROUND.
        private int _instantHudBorderR = 255;
        private int _instantHudBorderG = 255;
        private int _instantHudBorderB = 255;
        private int _instantHudBorderA = 255;

        // GOOD = hijau gelap
        private int _instantGoodBgR = 24;
        private int _instantGoodBgG = 92;
        private int _instantGoodBgB = 54;

        // BAD = merah gelap
        private int _instantBadBgR = 105;
        private int _instantBadBgG = 30;
        private int _instantBadBgB = 35;

        private int _instantHudBgA = 225;

        private float _instantHudTextOffsetY = -0.018f;
        private float _instantHudTextScale = 0.60f;
        private int _instantHudTextFont = 7;
        private int _instantHudTextR = 255;
        private int _instantHudTextG = 255;
        private int _instantHudTextB = 255;
        private int _instantHudTextA = 255;

        // INSTANT HUD labels / formats.
        private string _instantPlayerFlipLabel = "PLAYER FLIP";
        // {0} = nama kendaraan yang benar-benar terpilih.
        private string _instantGiveVehicleLabel = "GIVE {0}";
        private string _instantDestroyCarLabel = "DESTROY CAR";
        private string _instantRandomTeleportLabel = "RANDOM TELEPORT";
        private string _instantBackToStartLabel = "BACK TO START";
        private string _instantGoToFinishLabel = "GO TO FINISH";
        private string _instantTeleportSkyLabel = "TELEPORT SKY";
        private string _instantHitByVehicleLabel = "HIT BY VEHICLE";
        private string _instantUTurnLabel = "U-TURN";
        // {0} = nama spawn dinamis.
        private string _instantEnemyLabel = "SPAWN {0}";
        private string _instantBodyguardLabel = "SPAWN {0}";
        private string _instantAnimalLabel = "SPAWN {0}";
        private string _instantCountFormat = "{0} x{1}";
        private string _instantGiveHealthFormat = "HEALTH +{0}";

        private readonly List<InstantGiftHudItem> _instantGiftHudItems =
            new List<InstantGiftHudItem>();

        // -------------------------
        // TIMER / ACTIVE EFFECT HUD
        // -------------------------
        private float _statusHudX = 0.50f;
        private float _statusHudStartY = 0.85f;
        private float _statusHudBoxSpacing = 0.070f;
        private float _statusHudBoxWidth = 0.340f;
        private float _statusHudBoxHeight = 0.060f;
        private float _statusHudBorderSize = 0.0030f;

        // GLOBAL COLOR - TIMER
        // BORDER selalu PUTIH.
        // GOOD/BAD dibedakan dari BACKGROUND.
        private int _statusHudBorderR = 255;
        private int _statusHudBorderG = 255;
        private int _statusHudBorderB = 255;
        private int _statusHudBorderA = 255;

        // GOOD = hijau gelap
        private int _timerGoodBgR = 24;
        private int _timerGoodBgG = 92;
        private int _timerGoodBgB = 54;

        // BAD = merah gelap
        private int _timerBadBgR = 105;
        private int _timerBadBgG = 30;
        private int _timerBadBgB = 35;

        private int _statusHudBgA = 225;

        private float _statusHudTextOffsetY = -0.020f;
        private float _statusHudTextScale = 0.60f;
        private int _statusHudTextFont = 7;
        private int _statusHudTextR = 255;
        private int _statusHudTextG = 255;
        private int _statusHudTextB = 255;
        private int _statusHudTextA = 255;
        private string _statusHudDisplayTextFormat = "{0} {1}";

        private string _statusSuperSpeedLabel = "SUPER SPEED";

        private string _statusFallingVehiclesLabel = "FALLING VEHICLES";

        private string _statusSleepLabel = "SLEEP";

        private string _statusBallRainLabel = "BALL RAIN";

        private string _statusInvincibleLabel = "INVINCIBLE";

        private string _statusWantedLabel = "WANTED 5 STAR";

        private string _statusDisarmLabel = "DISARM";

        private string _statusBlackholeLabel = "BLACK HOLE";

        private string _statusEarthquakeLabel = "EARTHQUAKE";

        // Waiting/queue UI untuk mutual-exclusive Blackhole <-> Earthquake.
        private string _statusBlackholeWaitingLabel = "BLACK HOLE WILL COME";

        private string _statusEarthquakeWaitingLabel = "EARTHQUAKE WILL COME";

        private string _statusCageLabel = "CAGE";

        private string _statusTransformingLabel = "TRANSFORMING";
        private string _statusAnimalLabel = "ANIMAL";
        private string _statusReturningLabel = "RETURNING TO PLAYER...";

        // Standalone webhook effect status
        private string _statusGetTowedLabel = "GET TOWED";

        private string _statusTrafficMagnetLabel = "TRAFFIC MAGNET";

        private string _statusRecklessTrafficLabel = "RECKLESS TRAFFIC";

        private string _statusVehicleFireWarningLabel = "VEHICLE FIRE";
        private string _statusVehicleBurningLabel = "VEHICLE BURNING";
        private string _statusBurningWarningLabel = "BURNING IN";
        private string _statusBurningLabel = "BURNING";

        private string _statusPoliceRoadblockLabel = "POLICE ROADBLOCK";

        private string _statusRandomExplosionLabel = "RANDOM EXPLOSIONS";

        // Random Chaos roulette card
        private float _chaosUiX = 0.50f;
        private float _chaosUiY = 0.355f;
        private float _chaosUiWidth = 0.46f;
        private float _chaosUiHeight = 0.125f;
        private float _chaosUiBorderSize = 0.0040f;
        private int _chaosUiBorderA = 255;
        private int _chaosUiSelectingR = 255, _chaosUiSelectingG = 140, _chaosUiSelectingB = 0;
        private int _chaosUiLockedR = 255, _chaosUiLockedG = 40, _chaosUiLockedB = 40;
        private int _chaosUiBgR = 20, _chaosUiBgG = 5, _chaosUiBgB = 5, _chaosUiBgA = 235;
        private string _chaosUiSelectingText = "~y~SELECTING RANDOM CHAOS...~w~";
        private string _chaosUiLockedText = "~r~RANDOM CHAOS LOCKED!~w~";
        private float _chaosUiStatusOffsetY = -0.046f;
        private float _chaosUiStatusScale = 0.40f;
        private int _chaosUiStatusR = 255, _chaosUiStatusG = 255, _chaosUiStatusB = 255, _chaosUiStatusA = 255;
        private int _chaosUiStatusFont = 0;
        private float _chaosUiResultOffsetY = -0.010f;
        private float _chaosUiResultScale = 0.78f;
        private int _chaosUiResultR = 255, _chaosUiResultG = 255, _chaosUiResultB = 255, _chaosUiResultA = 255;
        private int _chaosUiResultFont = 7;

        // Random teleport card
        private float _teleportUiX = 0.50f;
        private float _teleportUiY = 0.2975f;
        private float _teleportUiWidth = 0.46f;
        private float _teleportUiHeight = 0.115f;
        private float _teleportUiBorderSize = 0.0035f;
        private int _teleportUiBorderA = 255;
        private int _teleportUiSelectingR = 0, _teleportUiSelectingG = 180, _teleportUiSelectingB = 255;
        private int _teleportUiLockedR = 147, _teleportUiLockedG = 112, _teleportUiLockedB = 219;
        private int _teleportUiBgR = 10, _teleportUiBgG = 10, _teleportUiBgB = 25, _teleportUiBgA = 230;
        private string _teleportUiSelectingText = "~b~SELECTING GLOBAL LOCATION...~w~";
        private string _teleportUiLockedText = "~purple~DESTINATION LOCKED!~w~";
        private float _teleportUiStatusOffsetY = -0.044f;
        private float _teleportUiStatusScale = 0.37f;
        private int _teleportUiStatusR = 255, _teleportUiStatusG = 255, _teleportUiStatusB = 255, _teleportUiStatusA = 255;
        private int _teleportUiStatusFont = 0;
        private float _teleportUiResultOffsetY = -0.014f;
        private float _teleportUiResultScale = 0.69f;
        private int _teleportUiResultR = 255, _teleportUiResultG = 255, _teleportUiResultB = 255, _teleportUiResultA = 255;
        private int _teleportUiResultFont = 7;

        // =========================================================
        // HUD / UI MASTER VISIBILITY TOGGLES
        // Semua default TRUE. Set Enabled=false di MoonHUD.ini
        // untuk menyembunyikan HUD tersebut TANPA mematikan gameplay.
        // =========================================================
        private bool _hudHealthEnabled = true;
        private bool _hudApocalypseEnabled = true;
        private bool _hudProgressEnabled = true;
        private bool _hudInstantEnabled = true;
        private bool _hudTimerEnabled = true;
        private bool _hudChaosRouletteEnabled = true;
        private bool _hudTeleportGachaEnabled = true;
        private bool _hudPedEnabled = true;
        private bool _hudCustomDeathEnabled = true;
        private bool _hudNotificationEnabled = true;

        // =========================================================
        // HUD SCORE
        // =========================================================
        // PosX/PosY memakai kiri-atas box, sama seperti HUD_WIN/HUD_DEATH.
        // Tinggi default 0.100 supaya setara panel HUD_PROGRESS.
        private float _scorePosX = 0.018f;
        private float _scorePosY = 0.016f;
        private float _scoreMinWidth = 0.160f;
        private float _scoreMaxWidth = 0.160f;
        private float _scoreHeight = 0.100f;
        private float _scorePaddingX = 0.018f;
        private float _scoreTextOffsetY = 0.026f;
        private float _scoreTextScale = 0.82f;
        private int _scoreTextFont = 4;

        private int _scoreLabelR = 248;
        private int _scoreLabelG = 250;
        private int _scoreLabelB = 253;
        private int _scoreLabelA = 255;

        private int _scorePositiveR = 95;
        private int _scorePositiveG = 235;
        private int _scorePositiveB = 105;
        private int _scorePositiveA = 255;

        private int _scoreNegativeR = 255;
        private int _scoreNegativeG = 92;
        private int _scoreNegativeB = 92;
        private int _scoreNegativeA = 255;

        private int _scoreZeroR = 248;
        private int _scoreZeroG = 250;
        private int _scoreZeroB = 253;
        private int _scoreZeroA = 255;

        private int _scoreBgR = 14;
        private int _scoreBgG = 18;
        private int _scoreBgB = 24;
        private int _scoreBgA = 222;

        // =========================================================
        // MODERN PED HUD - ENEMY / BODYGUARD / ANIMAL
        // Text-only, TANPA ICON. Nama + health bar menyatu dalam satu card.
        // COMPACT: panel dibuat sangat mepet agar banyak NPC tidak memenuhi layar.
        // Warna health bar dibedakan berdasarkan tipe entity.
        // Semua visual dapat diubah dari [HUD_PED] MoonHUD.ini.
        // =========================================================
        private enum PedHudType
        {
            Enemy,
            Bodyguard,
            Animal
        }

        private float _pedHudHeadOffsetZ = 0.52f;
        private float _pedHudCameraDistance = 35.0f;

        private float _pedHudPanelMinWidth = 0.060f;
        private float _pedHudPanelMaxWidth = 0.150f;
        private float _pedHudPanelHeight = 0.036f;
        private float _pedHudPanelPaddingX = 0.0055f;

        private int _pedHudPanelR = 14;
        private int _pedHudPanelG = 18;
        private int _pedHudPanelB = 24;
        private int _pedHudPanelA = 225;

        private float _pedHudPanelBorderSize = 0.0007f;
        private int _pedHudPanelBorderR = 92;
        private int _pedHudPanelBorderG = 113;
        private int _pedHudPanelBorderB = 134;
        private int _pedHudPanelBorderA = 205;

        private float _pedHudShadowOffsetX = 0.0012f;
        private float _pedHudShadowOffsetY = 0.0018f;
        private float _pedHudShadowExtraWidth = 0.0020f;
        private float _pedHudShadowExtraHeight = 0.0025f;
        private int _pedHudShadowR = 0;
        private int _pedHudShadowG = 0;
        private int _pedHudShadowB = 0;
        private int _pedHudShadowA = 115;

        private bool _pedHudNameUppercase = true;
        private float _pedHudNameOffsetY = -0.0125f;
        private float _pedHudNameScale = 0.34f;
        private int _pedHudNameFont = 4;
        private int _pedHudNameR = 248;
        private int _pedHudNameG = 250;
        private int _pedHudNameB = 253;
        private int _pedHudNameA = 255;

        private float _pedHudHealthOffsetY = 0.0105f;
        private float _pedHudHealthHorizontalPadding = 0.0045f;
        private float _pedHudHealthHeight = 0.0055f;
        private float _pedHudHealthBorderSize = 0.0007f;
        private int _pedHudHealthBorderR = 0;
        private int _pedHudHealthBorderG = 0;
        private int _pedHudHealthBorderB = 0;
        private int _pedHudHealthBorderA = 235;
        private int _pedHudHealthTrackR = 25;
        private int _pedHudHealthTrackG = 29;
        private int _pedHudHealthTrackB = 35;
        private int _pedHudHealthTrackA = 245;

        // Enemy = merah, Bodyguard = teal/green, Animal = amber.
        private int _pedHudEnemyR = 244;
        private int _pedHudEnemyG = 76;
        private int _pedHudEnemyB = 86;

        private int _pedHudBodyguardR = 54;
        private int _pedHudBodyguardG = 225;
        private int _pedHudBodyguardB = 177;

        private int _pedHudAnimalR = 255;
        private int _pedHudAnimalG = 184;
        private int _pedHudAnimalB = 64;

        private int _pedHudHealthFillA = 255;

        // =========================================================
        // GIFT: FALLING VEHICLES - DURATION SYSTEM
        // =========================================================
        // Default:
        //   1 gift = +2 detik.
        //   Setiap tambahan 2 detik = 3 kendaraan.
        //   2000 ms / 3 = sekitar 666 ms antar kendaraan.
        //
        // Gift berikutnya saat effect masih aktif MENAMBAH durasi,
        // bukan me-reset timer.
        // =========================================================
        private List<uint> _fallingVehicleHashes =
            new List<uint>();

        private const int FALLING_VEHICLES_PER_GIFT =
            3;

        private int _giftFallingVehiclesDurationMs =
            2 * 1000;

        private int _fallingVehiclesEffectEndTime =
            0;

        // Menjaga kendaraan tidak keluar bertumpuk pada frame yang sama
        // jika model loading sempat terlambat.
        private int _nextFallingVehicleActualSpawnTime =
            0;

        private readonly List<FallingVehicleActiveTracker> _activeFallingVehicles =
            new List<FallingVehicleActiveTracker>();

        // =========================================================
        // GIFT: HIT BY VEHICLE
        // =========================================================
        // Webhook:
        //   hit_by_vehicle
        //
        // Pilihan kendaraan RANDOM dari semua baris hitby= di [HIT_BY_VEHICLE].
        // Contoh:
        //   hitby=freight
        //   hitby=dinghy
        //   hitby=tug
        // Setiap webhook /hit_by_vehicle memilih SATU model secara random.
        // Tidak memakai suffix model di URL.
        // =========================================================
        private readonly List<string> _hitByVehicleModelNames =
            new List<string>();

        // Menyimpan model yang terpilih untuk request terakhir.
        private string _hitByVehicleModelName =
            "stockade";

        private float _hitByVehicleSpawnDistance =
            14.0f;

        private float _hitByVehicleSpeed =
            25.0f;

        // Selama window ini velocity dipaksa terus.
        // Ini membuat vehicle non-driveable / train / carriage tetap meluncur.
        private int _hitByVehicleForceLaunchMs =
            3 * 1000;

        // =========================================================
        // =========================================================
        // GIFT VEHICLE
        // =========================================================
        // Webhook:
        //   give_vehicle
        //
        // Pool kendaraan dibaca langsung dari:
        //   [GiveVehicles]
        // di GoToMountain.ini.
        //
        // Tidak ada pembatasan class kendaraan untuk player.
        // =========================================================
        private readonly List<string> _giftVehicleModelNames =
            new List<string>();

        private readonly List<string> _giftVehicleDisplayNames =
            new List<string>();

        // Kendaraan terakhir yang dibuat oleh gift give_vehicle.
        // Sistem give_vehicle sekarang bersifat REPLACE:
        // hanya satu gift vehicle yang dipertahankan.
        private Vehicle _currentGiftVehicle = null;

        // =========================================================
        // GIFT: PLAYER TRANSFORM ANIMAL - MODEL KHUSUS
        //
        // PENTING:
        // - List ini TERPISAH dari [AnimalX] di Entity.ini.
        // - spawn_angry_animal / a_c_mtlion tidak ikut pool transform.
        // - Satu gift memilih random 1 dari 10 model di bawah.
        // =========================================================
        private readonly string[] _playerTransformAnimalModels =
        {
            "a_c_coyote",
            "a_c_boar",
            "a_c_poodle",
            "a_c_pug",
            "a_c_westy",
            "a_c_cat_01",
            "a_c_rabbit_01",
            "a_c_deer",
            "a_c_cow",
            "a_c_pig"
        };

        private readonly string[] _playerTransformAnimalNames =
        {
            "COYOTE",
            "BOAR",
            "POODLE",
            "PUG",
            "WESTY",
            "CAT",
            "RABBIT",
            "DEER",
            "COW",
            "PIG"
        };


        // =========================================================
        // GIFT: PLAYER TRANSFORM MENJADI ANIMAL
        // =========================================================
        // Durasi transform animal dibaca dari [GIFT_TIMERS].
        // Default = 30 detik.
        private int _giftAnimalTransformDurationMs =
            30 * 1000;

        // 0 = idle
        // 1 = loading animal model
        // 2 = animal aktif
        // 3 = loading model player asli untuk restore
        private int _playerAnimalTransformPhase = 0;

        private int _playerAnimalTransformEndTime = 0;
        private int _playerAnimalExtraDurationMs = 0;

        // Durasi base untuk transform yang sedang diproses.
        // Normal gift = AnimalTransformSeconds.
        // Random Chaos = RandomChaosXDuration.
        private int _playerAnimalRequestedDurationMs = 0;

        private int _playerAnimalModelRequestStartTime = 0;

        private Model _playerAnimalPendingModel;
        private Model _playerOriginalPendingModel;

        private string _playerAnimalDisplayName = "";
        private string _playerAnimalModelName = "";

        private PlayerAnimalTransformSnapshot _playerAnimalSnapshot = null;

        // =========================================================================
        // 4. MANUAL HWID LICENSE / DEVICE BINDING
        // =========================================================================
        private sealed class HardwareSnapshot
        {
            public string MachineGuid { get; set; }
            public string VolumeSerial { get; set; }
            public string CpuIdentifier { get; set; }
            public string SystemProductName { get; set; }
            public string SystemSku { get; set; }
            public string CombinedHash { get; set; }
        }

        // =========================================================
        // HWID BLOCK SCREEN
        // Hanya Tick ini yang dijalankan bila PC tidak terdaftar.
        // Gameplay, entity, dan webhook server TIDAK diaktifkan.
        // =========================================================
        // =========================================================
        // STARTUP PLAYER NAME UI - DESIGN DARI .INI
        // =========================================================
        // =========================================================================
        // 5. INISIALISASI & SETUP SCRIPT
        // =========================================================================

        // =========================================================
        // GAMEPLAY CONFIG
        // =========================================================
        // RANDOM_GATCHA, RANDOM_CHAOS dan RANDOM_CHECKPOINT memakai
        // SATU daftar effect bersama. Jadi effect tidak perlu lagi ditambah
        // satu-per-satu ke tiga whitelist yang berbeda.
        //
        // Sengaja TIDAK mengizinkan random_gatcha/random_gacha sebagai hasil
        // roulette karena akan membuat roulette memanggil roulette lagi.
        // =========================================================
        // =========================================================
        // DEFAULT DURATION LOOKUP
        // =========================================================
        // Sumber utamanya [TIME_DEFAULT]. Loader tetap menerima nama section
        // lama [GIFT_TIMERS] untuk backward compatibility.
        // =========================================================
        // RANDOM GATCHA CONFIG
        // =========================================================
        // =========================================================
        // RANDOM CHECKPOINT CONFIG
        // =========================================================
        // =========================================================
        // ENTITY LIMITS CONFIG
        // =========================================================
        // =========================================================
        // GIFT TIMERS CONFIG
        // =========================================================
        // =========================================================================
        // ERROR / DIAGNOSTIC LOGGER
        // =========================================================================
        // HUD_NOTIFICATION boleh tetap false. Semua pesan yang mengandung ERROR
        // tetap ditulis ke scripts/MoonMod.log. Untuk exception, LogError()
        // menyimpan stack trace lengkap melalui ex.ToString().
        private static readonly object _errorLogLock =
            new object();

        // =========================================================
        // CATAT STATISTIK KILL ENEMY OLEH PLAYER
        // =========================================================
        // =============================================================
        // BLOKIR CHARACTER SWITCH / LEFT ALT
        // =============================================================
        // =========================================================
        // GIVE VEHICLE CONFIG
        // =========================================================
        // =========================================================
        // HIT BY VEHICLE CONFIG
        // =========================================================
        // =========================================================
        // FALLING VEHICLES CONFIG
        // =========================================================
        private Dictionary<string, Dictionary<string, string>>
            ReadDesignIniFile(
                string iniPath)
        {
            Dictionary<string, Dictionary<string, string>> result =
                new Dictionary<string, Dictionary<string, string>>(
                    StringComparer.OrdinalIgnoreCase
                );

            string currentSection =
                "";

            string[] lines =
                File.ReadAllLines(
                    iniPath
                );

            foreach (
                string rawLine in
                    lines
            )
            {
                string line =
                    rawLine.Trim();

                if (string.IsNullOrEmpty(line) ||
                    line.StartsWith(";") ||
                    line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("[") &&
                    line.EndsWith("]"))
                {
                    currentSection =
                        line.Substring(
                            1,
                            line.Length - 2
                        ).Trim();

                    if (!result.ContainsKey(
                            currentSection
                        ))
                    {
                        result[
                            currentSection
                        ] =
                            new Dictionary<string, string>(
                                StringComparer.OrdinalIgnoreCase
                            );
                    }

                    continue;
                }

                if (string.IsNullOrEmpty(
                        currentSection
                    ))
                {
                    continue;
                }

                int equalIndex =
                    line.IndexOf('=');

                if (equalIndex <= 0)
                    continue;

                string key =
                    line.Substring(
                        0,
                        equalIndex
                    ).Trim();

                string value =
                    line.Substring(
                        equalIndex + 1
                    ).Trim();

                if (string.IsNullOrEmpty(
                        key
                    ))
                {
                    continue;
                }

                result[
                    currentSection
                ][key] =
                    value;
            }

            return result;
        }

        private void LoadHudPresetSelectionFromIni()
        {
            const string path = "scripts/MoonModConfig.ini";

            string selectedLayout = "default";
            string selectedPreset = "default";
            bool foundHudDesign = false;

            if (File.Exists(path))
            {
                try
                {
                    string section = string.Empty;

                    foreach (string raw in File.ReadAllLines(path))
                    {
                        string line = raw.Trim();

                        if (string.IsNullOrEmpty(line) ||
                            line.StartsWith(";") ||
                            line.StartsWith("#"))
                            continue;

                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            section = line.Substring(1, line.Length - 2).Trim();
                            continue;
                        }

                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;

                        string key = line.Substring(0, eq).Trim();
                        string value = line.Substring(eq + 1).Trim();

                        if (section.Equals("HUD_DESIGN", StringComparison.OrdinalIgnoreCase))
                        {
                            foundHudDesign = true;

                            if (key.Equals("Layout", StringComparison.OrdinalIgnoreCase) ||
                                key.Equals("Design", StringComparison.OrdinalIgnoreCase))
                            {
                                selectedLayout = value;
                            }
                            else if (key.Equals("Preset", StringComparison.OrdinalIgnoreCase))
                            {
                                selectedPreset = value;
                            }
                        }
                        else if (!foundHudDesign &&
                                 section.Equals("HUD_PRESET", StringComparison.OrdinalIgnoreCase) &&
                                 key.Equals("Preset", StringComparison.OrdinalIgnoreCase))
                        {
                            // Compatibility paket lama.
                            selectedPreset = value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("HUD DESIGN CONFIG ERROR", ex);
                }
            }

            _hudLayoutName = SanitizeHudDesignName(selectedLayout, "default");
            _hudPresetName = SanitizeHudDesignName(selectedPreset, "default");
        }

        private string SanitizeHudDesignName(string value, string fallback)
        {
            string selected = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(selected)) selected = fallback;

            StringBuilder safe = new StringBuilder();

            foreach (char c in selected)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                    safe.Append(c);
            }

            return safe.Length > 0 ? safe.ToString() : fallback;
        }

        private string GetActiveHudLayoutIniPath()
        {
            return "scripts/design/" + _hudLayoutName + ".ini";
        }


        private string GetActiveHudPresetIniPath()
        {
            return
                "scripts/design/preset/" +
                _hudPresetName +
                ".ini";
        }


        private bool IsSafeHudPresetKey(
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            string k =
                key.Trim();

            // SAFE STYLE ONLY:
            // warna RGBA + font + text shadow/edge.
            // Posisi/ukuran/scale/text/enabled/timer sengaja tidak boleh
            // dioverride preset agar layout tidak berantakan.
            if (k.EndsWith(
                    "R",
                    StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(
                    "G",
                    StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(
                    "B",
                    StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(
                    "A",
                    StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(
                    "Font",
                    StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(
                    "ShadowEnabled",
                    StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(
                    "ShadowDistance",
                    StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(
                    "EdgeEnabled",
                    StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(
                    "EdgeSize",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private Dictionary<string, Dictionary<string, string>>
            ReadHudDesignWithSafePreset(
                string baseIniPath)
        {
            Dictionary<string, Dictionary<string, string>> result =
                ReadDesignIniFile(baseIniPath);

            // 1. Full layout override. Berbeda dari preset, layout BOLEH
            // mengubah posisi, ukuran, text, scale dan key visual lain.
            string layoutPath =
                GetActiveHudLayoutIniPath();

            if (File.Exists(layoutPath))
            {
                Dictionary<string, Dictionary<string, string>> layout =
                    ReadDesignIniFile(layoutPath);

                foreach (KeyValuePair<string, Dictionary<string, string>> section in layout)
                {
                    if (!result.ContainsKey(section.Key))
                    {
                        result[section.Key] =
                            new Dictionary<string, string>(
                                StringComparer.OrdinalIgnoreCase
                            );
                    }

                    foreach (KeyValuePair<string, string> item in section.Value)
                    {
                        result[section.Key][item.Key] = item.Value;
                    }
                }
            }

            // 2. Style preset overlay. Hanya key style aman.
            string presetPath =
                GetActiveHudPresetIniPath();

            if (!File.Exists(presetPath))
                return result;

            Dictionary<string, Dictionary<string, string>> preset =
                ReadDesignIniFile(presetPath);

            foreach (KeyValuePair<string, Dictionary<string, string>> section in preset)
            {
                if (!result.ContainsKey(section.Key))
                    continue;

                foreach (KeyValuePair<string, string> item in section.Value)
                {
                    if (!IsSafeHudPresetKey(item.Key))
                        continue;

                    result[section.Key][item.Key] = item.Value;
                }
            }

            return result;
        }


        private T GetDesignEnum<T>(
            Dictionary<string, Dictionary<string, string>> design,
            string section,
            string key,
            T fallback)
            where T : struct
        {
            string raw =
                GetDesignString(
                    design,
                    section,
                    key,
                    null
                );

            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            T parsed;

            if (Enum.TryParse<T>(
                    raw,
                    true,
                    out parsed
                ))
            {
                return parsed;
            }

            int numericValue;

            if (int.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out numericValue
                ))
            {
                return (T)Enum.ToObject(
                    typeof(T),
                    numericValue
                );
            }

            return fallback;
        }

        // =========================================================================
        // 5. SERVER WEBHOOK TIKTOK LOKAL
        // =========================================================================

        // =========================================================
        // WEBHOOK MAIN-THREAD QUEUE HELPER
        // =========================================================
        // key         : identitas action yang boleh digabung.
        // action      : action GTA yang harus dijalankan di main thread.
        // repeatCount : berapa kali action tersebut wajib dieksekusi.
        //
        // Jika key yang sama sedang antre, kita hanya menambah PendingCount.
        // Dengan begitu spam gift / multiplier besar tidak membuat ribuan
        // object Action terpisah, tetapi TIDAK ADA gift yang dibuang.
        // =========================================================================
        // 6. MAIN GAME LOOP (ON TICK) & CLEANUP
        // =========================================================================

        // =========================================================================
        // LOGIKA APOCALYPSE COUNTDOWN & CLEANUP
        // =========================================================================

        // =========================================================
        // TAMBAH WAKTU KE TIMER APOCALYPSE UTAMA
        // =========================================================
        // All chaos phases share one fixed frame. Rendering never changes countdown state.
        // =========================================================================
        // RANDOM CHAOS ROULETTE + CONFIGURED EFFECT DURATION
        // =========================================================================

        // =========================================================
        // CHAOS: NO WEAPON
        // =========================================================
        // =========================================================
        // GIFT: DYNAMIC CAGE
        // =========================================================
        // =========================================================
        // CHAOS: PLAYER IN CAGE
        // =========================================================
        // =========================================================
        // EFFECT / TIMER PEMAIN YANG MASIH AKTIF
        // =========================================================
        // =========================================================================
        // 7. SISTEM SPAWN & MANAJEMEN ENTITAS (HEWAN, MONSTER & BODYGUARD)
        // =========================================================================
        // =========================================================
        // POSISI SPAWN VEHICLE ENEMY YANG AMAN + BATCH SPREAD
        // =========================================================
        // =========================================================
        // HAPUS SATU BLIP ENEMY BERDASARKAN HANDLE PED
        // =========================================================
        // =========================================================
        // HAPUS BLIP DARI PED ENEMY
        // =========================================================
        // =========================================================
        // BERSIHKAN BLIP HANTU / JEJAK LAMA
        //
        // Kalau PED sudah tidak ada sebagai active enemy, blip juga
        // tidak boleh tertinggal di minimap.
        // =========================================================
        // =========================================================
        // HAPUS SEMUA BLIP ENEMY
        // =========================================================
        // =========================================================
        // ENEMY UNIT VEHICLE HANDLE
        // =========================================================
        // =========================================================
        // RESPAWN SATU ENEMY UNIT DI DEKAT PLAYER
        //
        // - On-foot: hapus 1 ped -> spawn ulang 1 ped.
        // - Vehicle: hapus driver + seluruh passenger + vehicle ->
        //   spawn ulang tepat 1 vehicle unit.
        // =========================================================
        // =========================================================
        // GIFT: DESTROY CAR
        // =========================================================
        // Webhook:
        //   destroy_car
        //
        // Konsep:
        // - Hancurkan kendaraan yang sedang dipakai player.
        // - Pecahkan kaca.
        // - Rusakkan kap depan.
        // - Pecahkan ban.
        // - Tambahkan body deformation.
        // - Kendaraan dibuat tidak bisa dipakai normal lagi.
        // - TIDAK meledak, TIDAK dibuat gosong, TIDAK dibakar.
        // =========================================================
        // =========================================================================
        // 8. LOGIKA FITUR & EFEK PEMAIN
        // =========================================================================

        // =========================================================
        // GIFT: TRANSFORM PLAYER MENJADI ANIMAL SELAMA 30 DETIK
        //
        // Prinsip penting:
        // - model manusia + outfit di-snapshot sebelum transform
        // - damage saat menjadi animal tetap terbawa saat kembali
        // - weapon tidak diberikan ke animal, lalu direstore saat human
        // =========================================================
        // =========================================================================
        // WEBHOOK EFFECTS 20-32 - REVISED
        // =========================================================================

        // =========================================================
        // PLAYER CLEAN + SILENT
        // - Hilangkan darah / luka visual setiap frame.
        // - Matikan pain audio + semua speech player supaya karakter tidak
        //   mengeluarkan respon suara / kata-kata kasar.
        // =========================================================
        // =========================================================
        // Webhook: /give_health (legacy /give_health_armor is an alias)
        //
        // Nilai dibaca ulang setiap webhook dipanggil:
        // [GIVE_HEALTH]
        // HealthAmount=2500
        // Health-only gift.
        // =========================================================
        // =========================================================
        // 20. U-TURN
        // Webhook: /u_turn
        //
        // Vehicle:
        // - kendaraan diputar 180 derajat
        // - speed dipertahankan ke arah baru
        //
        // On-foot:
        // - player hanya dibalik heading 180 derajat
        // =========================================================
        // =========================================================
        // 21. GET TOWED
        // Webhook: /get_towed
        // Durasi: 30 detik
        // =========================================================
        // Hanya membersihkan entity fisik GET TOWED.
        // Timer effect sengaja TIDAK disentuh supaya tow dapat muncul lagi.
        // Membuat ulang tow truck + driver untuk target tertentu TANPA
        // mengubah _getTowedEndTime. Ini yang membuat GET TOWED tetap hidup
        // walaupun GIVE VEHICLE menghapus kendaraan/tow rig sebelumnya.
        // =========================================================
        // TRAFFIC MAGNET
        // Webhook: /traffic_magnet
        //
        // Durasi mengikuti [TIME_DEFAULT] / override mode.
        // SEMUA kendaraan traffic yang berada di radius sekitar player
        // langsung ditarik menuju player / kendaraan player.
        // Tidak ada limit jumlah kendaraan.
        // =========================================================
        // =========================================================
        // 26. RECKLESS TRAFFIC
        // Webhook: /reckless_traffic
        //
        // Selama timer aktif:
        // - SEMUA road traffic yang sudah di-stream GTA dalam radius 500 m
        //   ikut diproses, bukan hanya 110 m.
        // - driver dipaksa tetap membawa kendaraan dan berkendara agresif/rusuh.
        // - motor ikut kena.
        // - NOS memakai torque boost setiap Tick + forward-speed burst berkala.
        // - player vehicle sendiri TIDAK ikut diacak.
        // =========================================================
        // =========================================================
        // GIFT: REMOVE TIRE - SIMPLE ROLL ONLY
        // Webhook: /remove_tire
        //
        // 1 gift = 1 ban random.
        // HANYA:
        // - ban asli dibuat burst,
        // - prop roda/ban muncul sedikit di luar posisi wheel,
        // - prop mental lalu menggelinding.
        //
        // TIDAK mengubah rim radius, wheel health, suspension,
        // posisi kendaraan, ground placement, atau breakable-wheel state.
        // Jadi kendaraan tidak dipaksa turun / masuk ke tanah.
        // =========================================================
        // =========================================================
        // 30. VEHICLE FIRE TIMER
        // Webhook: /vehicle_fire_timer
        //
        // Default 60 detik:
        // - saat player jalan kaki: tidak ada warning / tidak ada effect
        // - saat player naik kendaraan: warning 5 detik
        // - lalu kendaraan terbakar 5 detik
        // - lalu kendaraan meledak
        // - selama timer 60 detik masih aktif, cycle akan mulai lagi
        //   saat player berada di kendaraan berikutnya
        // =========================================================
        // =========================================================
        // BURNING
        // Webhook: /burning
        // 5 detik countdown -> player terbakar selama BurningSeconds.
        // Tidak pernah meledak. Gift berikutnya saat masih aktif menambah
        // durasi api ke total timer tanpa mengulang countdown.
        // =========================================================
        // =========================================================
        // POLICE ROADBLOCK
        // Webhook: /police_roadblock
        //
        // Selama 60 detik:
        // - setiap 5 detik spawn batch baru di depan player
        // - satu batch = TOTAL 5 RIOT + siren dalam satu garis roadblock
        // - antar RIOT diberi jarak supaya body kendaraan tidak overlap/menempel
        // - setiap batch lama dibersihkan otomatis setelah 9 detik
        // sehingga blockade terus mengikuti arah perjalanan player
        // tanpa menumpuk kendaraan permanen.
        // =========================================================
        // =========================================================
        // RANDOM EXPLOSION NEARBY
        // Webhook: /random_explosion_nearby
        //
        // Selama durasi aktif:
        // - total ledakan mengikuti RandomExplosionNearbyTotal
        // - titik ledakan benar-benar random di seluruh area 0-28 meter
        // - TIDAK ADA safe zone di sekitar player; player/kendaraan bisa kena
        // =========================================================
        // =========================================================
        // GIFT: WANTED LEVEL 5
        // =========================================================
        // Webhook:
        //   /wanted_5_star
        //
        // Sesuai request: saat gift masuk, wanted langsung 5 bintang.
        // Ini one-shot: setelah itu sistem wanted GTA berjalan normal.
        // =========================================================
        // =========================================================
        // GIFT: TELEPORT TO SKY
        // =========================================================
        // Webhook:
        //   /teleport_sky
        //
        // Jika player sedang di vehicle, vehicle ikut diteleport
        // supaya player tidak otomatis terpisah dari kendaraannya.
        // Posisi X/Y tetap; hanya Z dinaikkan 1000 meter.
        // =========================================================
        // =========================================================
        // GIFT: EARTHQUAKE M8 - FINAL 60 DETIK
        // =========================================================
        // =========================================================================
        // 9. ATURAN GAME (POS, FINISH, AIR, MATI, TELEPORTASI)
        // =========================================================================
        // =========================================================================
        // 9. ATURAN GAME (POS, FINISH, AIR, MATI, TELEPORTASI)
        // =========================================================================

        // =========================================================
        // POS / FINISH CELEBRATION HELPERS
        // =========================================================
        // =========================================================================
        // 10. RANDOM TELEPORT
        // =========================================================================

        // -------------------------------------------------------------------------
        // GACHA MODE (UseGachaCards=true)
        // -------------------------------------------------------------------------

        // =========================================================================
        // RANDOM CHECKPOINT SYSTEM
        // =========================================================================

        // Random Checkpoint sekarang memakai PANEL YANG SAMA dengan route progress.
        // Method ini sengaja tidak menggambar UI kedua agar tidak ada elemen
        // di luar box utama.
        // =========================================================================
        // 11. SISTEM UI & GAMBAR (HUD)
        // =========================================================================

        // Native text measurement keeps long mission titles / FINISH inside the frame.
        public class ActiveStatusItem
        {
            public string Key { get; set; }
            public int Revision { get; set; }
            public string Text { get; set; }
            public bool IsGood { get; set; }
        }

        public class InstantGiftHudItem
        {
            public string Text { get; set; }
            public int EndTime { get; set; }
            public bool IsGood { get; set; }
        }

        // Presentation-only timer ranges; never alter effect end times.
        private readonly Dictionary<string, int> _hudTimerPeakSeconds = new Dictionary<string, int>();
        private float _giftTimerTimeScale = 0.65f;
        private float _giftTimerTimeOffsetY = -0.030f;
        private float _giftTimerColumnRatio = 0.25f;
        private float _giftTimerBarHeight = 0.006f;
        private float _giftTimerBarOffsetY = 0.023f;
        private int _instantAccentGoodR = 68;
        private int _instantAccentGoodG = 210;
        private int _instantAccentGoodB = 100;
        private int _instantAccentBadR = 244;
        private int _instantAccentBadG = 76;
        private int _instantAccentBadB = 86;
        private int _timerAccentGoodR = 68;
        private int _timerAccentGoodG = 210;
        private int _timerAccentGoodB = 100;
        private int _timerAccentBadR = 244;
        private int _timerAccentBadG = 76;
        private int _timerAccentBadB = 86;

        // Status producers use either seconds (25s) or a clock (WANTED: 01:25).
        private int _activeHudMaxVisible = 3;
        private readonly List<string> _activeHudOrder = new List<string>();
        private readonly Dictionary<string, int> _activeHudKnown = new Dictionary<string, int>();

        // Oldest displayed event is evicted first. Hidden effects stay active in gameplay.


        // =========================================================================
        // MOONMOD - GLOBAL / STARTUP / GAMEPLAY MODE SUPPORT
        // =========================================================================
        // =========================================================================
        // SURVIVAL ROUTE
        // =========================================================================
        // =========================================================================
        // ENEMY STABILITY / SCORE CAUSE DETECTION
        // =========================================================================

        private static string NormalizeHardwareComponent(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "UNKNOWN";

            return value
                .Trim()
                .ToUpperInvariant()
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);
        }

        private static string ReadRegistryString(
            RegistryHive hive,
            string subKey,
            string valueName)
        {
            RegistryView[] views =
            {
                RegistryView.Registry64,
                RegistryView.Registry32
            };

            foreach (RegistryView view in views)
            {
                try
                {
                    using (RegistryKey baseKey =
                        RegistryKey.OpenBaseKey(
                            hive,
                            view
                        ))
                    using (RegistryKey key =
                        baseKey.OpenSubKey(
                            subKey,
                            false
                        ))
                    {
                        if (key == null)
                            continue;

                        object value =
                            key.GetValue(
                                valueName,
                                null,
                                RegistryValueOptions.DoNotExpandEnvironmentNames
                            );

                        if (value != null)
                        {
                            string result =
                                Convert.ToString(
                                    value,
                                    CultureInfo.InvariantCulture
                                );

                            if (!string.IsNullOrWhiteSpace(result))
                                return result.Trim();
                        }
                    }
                }
                catch
                {
                }
            }

            return string.Empty;
        }

        private static string GetSystemVolumeSerial()
        {
            try
            {
                string systemDirectory =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.System
                    );

                string rootPath =
                    Path.GetPathRoot(
                        systemDirectory
                    );

                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    rootPath =
                        Environment.GetEnvironmentVariable(
                            "SystemDrive"
                        );

                    if (!string.IsNullOrWhiteSpace(rootPath) &&
                        !rootPath.EndsWith("\\"))
                    {
                        rootPath += "\\";
                    }
                }

                if (string.IsNullOrWhiteSpace(rootPath))
                    return string.Empty;

                StringBuilder volumeName =
                    new StringBuilder(261);

                StringBuilder fileSystemName =
                    new StringBuilder(261);

                uint serialNumber;
                uint maxComponentLength;
                uint fileSystemFlags;

                bool success =
                    GetVolumeInformation(
                        rootPath,
                        volumeName,
                        volumeName.Capacity,
                        out serialNumber,
                        out maxComponentLength,
                        out fileSystemFlags,
                        fileSystemName,
                        fileSystemName.Capacity
                    );

                if (!success)
                    return string.Empty;

                return serialNumber.ToString(
                    "X8",
                    CultureInfo.InvariantCulture
                );
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string Sha256Hex(
            string value)
        {
            if (value == null)
                value = string.Empty;

            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(value);

                byte[] hash =
                    sha.ComputeHash(bytes);

                StringBuilder result =
                    new StringBuilder(
                        hash.Length * 2
                    );

                for (int i = 0; i < hash.Length; i++)
                {
                    result.Append(
                        hash[i].ToString(
                            "X2",
                            CultureInfo.InvariantCulture
                        )
                    );
                }

                return result.ToString();
            }
        }

        private static HardwareSnapshot GetCurrentHardwareSnapshot()
        {
            HardwareSnapshot snapshot =
                new HardwareSnapshot
                {
                    MachineGuid = NormalizeHardwareComponent(
                        ReadRegistryString(
                            RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Cryptography",
                            "MachineGuid"
                        )
                    ),

                    VolumeSerial = NormalizeHardwareComponent(
                        GetSystemVolumeSerial()
                    ),

                    CpuIdentifier = NormalizeHardwareComponent(
                        ReadRegistryString(
                            RegistryHive.LocalMachine,
                            @"HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                            "Identifier"
                        )
                    ),

                    SystemProductName = NormalizeHardwareComponent(
                        ReadRegistryString(
                            RegistryHive.LocalMachine,
                            @"HARDWARE\DESCRIPTION\System\BIOS",
                            "SystemProductName"
                        )
                    ),

                    SystemSku = NormalizeHardwareComponent(
                        ReadRegistryString(
                            RegistryHive.LocalMachine,
                            @"HARDWARE\DESCRIPTION\System\BIOS",
                            "SystemSKU"
                        )
                    )
                };

            string fingerprint =
                "MG=" + snapshot.MachineGuid + "|" +
                "VOL=" + snapshot.VolumeSerial + "|" +
                "CPU=" + snapshot.CpuIdentifier + "|" +
                "PRODUCT=" + snapshot.SystemProductName + "|" +
                "SKU=" + snapshot.SystemSku;

            snapshot.CombinedHash =
                Sha256Hex(fingerprint);

            return snapshot;
        }

        private static bool IsManualValueConfigured(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string normalized =
                value.Trim().ToUpperInvariant();

            if (normalized.StartsWith("PASTE_"))
                return false;

            if (normalized.Contains("_HERE"))
                return false;

            return true;
        }

        private static bool IsProfileFullyConfigured(
            AuthorizedPcProfile profile)
        {
            if (profile == null)
                return false;

            return
                IsManualValueConfigured(profile.MachineGuid) &&
                IsManualValueConfigured(profile.VolumeSerial) &&
                IsManualValueConfigured(profile.CpuIdentifier) &&
                IsManualValueConfigured(profile.SystemProductName) &&
                IsManualValueConfigured(profile.SystemSku);
        }

        private static bool HardwareComponentMatches(
            string expected,
            string actual)
        {
            return NormalizeHardwareComponent(expected)
                .Equals(
                    NormalizeHardwareComponent(actual),
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private static bool ProfileMatchesHardware(
            AuthorizedPcProfile profile,
            HardwareSnapshot current)
        {
            if (!IsProfileFullyConfigured(profile) ||
                current == null)
            {
                return false;
            }

            return
                HardwareComponentMatches(
                    profile.MachineGuid,
                    current.MachineGuid
                ) &&
                HardwareComponentMatches(
                    profile.VolumeSerial,
                    current.VolumeSerial
                ) &&
                HardwareComponentMatches(
                    profile.CpuIdentifier,
                    current.CpuIdentifier
                ) &&
                HardwareComponentMatches(
                    profile.SystemProductName,
                    current.SystemProductName
                ) &&
                HardwareComponentMatches(
                    profile.SystemSku,
                    current.SystemSku
                );
        }

        private static string FindAuthorizedProfileName(
            HardwareSnapshot current)
        {
            if (AUTHORIZED_PCS == null)
                return string.Empty;

            foreach (AuthorizedPcProfile profile in AUTHORIZED_PCS)
            {
                if (ProfileMatchesHardware(
                        profile,
                        current
                    ))
                {
                    return string.IsNullOrWhiteSpace(profile.Name)
                        ? "AUTHORIZED PC"
                        : profile.Name.Trim();
                }
            }

            return string.Empty;
        }

        private static bool HasConfiguredAuthorizedPc()
        {
            if (AUTHORIZED_PCS == null)
                return false;

            foreach (AuthorizedPcProfile profile in AUTHORIZED_PCS)
            {
                if (IsProfileFullyConfigured(profile))
                    return true;
            }

            return false;
        }

        private bool ValidateHardwareLicense()
        {
            HardwareSnapshot current =
                GetCurrentHardwareSnapshot();

            if (!HasConfiguredAuthorizedPc())
            {
                WriteHwidDiagnosticLog(
                    "LOCKED - MANUAL HWID BELUM DIISI DI SOURCE",
                    current
                );

                try
                {
                    ShowNotificationCompat(
                        "~r~GO TO MOUNTAIN LOCKED~w~\nIsi HWID PC secara manual di AUTHORIZED_PCS lalu compile ulang."
                    );
                }
                catch
                {
                }

                return false;
            }

            string authorizedProfile =
                FindAuthorizedProfileName(
                    current
                );

            if (string.IsNullOrWhiteSpace(
                    authorizedProfile
                ))
            {
                WriteHwidDiagnosticLog(
                    "DENIED - PC TIDAK COCOK DENGAN AUTHORIZED_PCS",
                    current
                );

                try
                {
                    ShowNotificationCompat(
                        "~r~MOD BLOCKED BY OWNER"
                    );
                }
                catch
                {
                }

                return false;
            }

            WriteHwidDiagnosticLog(
                "AUTHORIZED - " + authorizedProfile,
                current
            );

            return true;
        }

        private void LoadHostProfilesFromIni()
        {
            _hostProfiles.Clear();
            _startupNameOptions.Clear();
            _selectedHostProfileIndex = -1;
            _startupNameDraft = string.Empty;

            try
            {
                if (!File.Exists(PROFILE_HOST_INI_PATH))
                {
                    CreateDefaultHostProfiles();
                    SaveAllHostProfilesToIni();
                    return;
                }

                HostProfile currentProfile = null;

                foreach (string rawLine in File.ReadAllLines(PROFILE_HOST_INI_PATH))
                {
                    string line = rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line) ||
                        line.StartsWith(";") ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        if (currentProfile != null &&
                            !string.IsNullOrWhiteSpace(currentProfile.Name))
                        {
                            AddLoadedHostProfile(currentProfile);
                        }

                        string sectionName = line.Substring(1, line.Length - 2).Trim();

                        currentProfile = sectionName.StartsWith(
                            "HOST",
                            StringComparison.OrdinalIgnoreCase)
                                ? new HostProfile { SectionName = sectionName }
                                : null;

                        continue;
                    }

                    if (currentProfile == null || !line.Contains("="))
                        continue;

                    string[] parts = line.Split(new[] { '=' }, 2);
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProfile.Name = NormalizeStartupPlayerName(value);
                    }
                    else if (key.Equals("Score", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProfile.Score = ParseHostProfileInt(value, currentProfile.Score);
                    }
                    else if (key.Equals("Wins", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProfile.Wins = ParseHostProfileInt(value, currentProfile.Wins);
                    }
                    else if (key.Equals("Deaths", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProfile.Deaths = ParseHostProfileInt(value, currentProfile.Deaths);
                    }
                    else if (key.Equals("EnemyKills", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProfile.EnemyKills = ParseHostProfileInt(value, currentProfile.EnemyKills);
                    }
                }

                if (currentProfile != null &&
                    !string.IsNullOrWhiteSpace(currentProfile.Name))
                {
                    AddLoadedHostProfile(currentProfile);
                }

                if (_hostProfiles.Count == 0)
                {
                    CreateDefaultHostProfiles();
                    SaveAllHostProfilesToIni();
                }
            }
            catch (Exception ex)
            {
                LogError("PROFILE HOST LOAD ERROR", ex);

                if (_hostProfiles.Count == 0)
                {
                    CreateDefaultHostProfiles();
                }
            }
        }

        private int ParseHostProfileInt(string value, int fallback)
        {
            if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private void AddLoadedHostProfile(HostProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
                return;

            if (_hostProfiles.Any(
                    existing => string.Equals(
                        existing.Name,
                        profile.Name,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.SectionName))
            {
                profile.SectionName =
                    "HOST" + (_hostProfiles.Count + 1).ToString(CultureInfo.InvariantCulture);
            }

            _hostProfiles.Add(profile);
            _startupNameOptions.Add(profile.Name);
        }

        private void CreateDefaultHostProfiles()
        {
            _hostProfiles.Clear();
            _startupNameOptions.Clear();

            HostProfile[] defaults =
            {
                new HostProfile
                {
                    SectionName = "HOST1",
                    Name = "KEIRA",
                    Score = 100,
                    Wins = 12,
                    Deaths = 8,
                    EnemyKills = 250
                },
                new HostProfile
                {
                    SectionName = "HOST2",
                    Name = "LUNA",
                    Score = 65,
                    Wins = 7,
                    Deaths = 15,
                    EnemyKills = 184
                },
                new HostProfile
                {
                    SectionName = "HOST3",
                    Name = "MIKA",
                    Score = -10,
                    Wins = 3,
                    Deaths = 30,
                    EnemyKills = 96
                }
            };

            foreach (HostProfile profile in defaults)
            {
                AddLoadedHostProfile(profile);
            }
        }

        private HostProfile GetSelectedHostProfile()
        {
            if (_selectedHostProfileIndex < 0 ||
                _selectedHostProfileIndex >= _hostProfiles.Count)
            {
                return null;
            }

            return _hostProfiles[_selectedHostProfileIndex];
        }

        private void ApplySelectedHostProfileToSession()
        {
            HostProfile profile = GetSelectedHostProfile();

            if (profile == null)
                return;

            _startupPlayerName = profile.Name;
            _score = profile.Score;
            _winCount = profile.Wins;
            _deathCount = profile.Deaths;
            _enemyKillCount = profile.EnemyKills;
            _profileHostDirty = false;
        }

        private void MarkCurrentHostProfileDirty()
        {
            HostProfile profile = GetSelectedHostProfile();

            if (profile == null)
                return;

            // Sinkronkan snapshot profile di RAM LANGSUNG setiap statistik berubah.
            // File fisik di-flush oleh ProcessProfileHostPersistence() agar kill
            // yang datang cepat tidak melakukan write disk pada setiap frame.
            profile.Score = _score;
            profile.Wins = _winCount;
            profile.Deaths = _deathCount;
            profile.EnemyKills = _enemyKillCount;

            _profileHostDirty = true;
        }

        private void ProcessProfileHostPersistence(int currentTime)
        {
            if (!_profileHostDirty || currentTime < _nextProfileHostSaveTime)
                return;

            SaveCurrentHostProfileStats();
            _nextProfileHostSaveTime = currentTime + PROFILE_HOST_SAVE_INTERVAL_MS;
        }

        private void SaveCurrentHostProfileStats()
        {
            HostProfile profile = GetSelectedHostProfile();

            if (profile == null)
                return;

            profile.Score = _score;
            profile.Wins = _winCount;
            profile.Deaths = _deathCount;
            profile.EnemyKills = _enemyKillCount;

            // Dirty hanya dibersihkan kalau file benar-benar berhasil ditulis.
            // Jika gagal (misalnya file sedang terkunci), tick berikutnya akan retry.
            if (SaveAllHostProfilesToIni())
            {
                _profileHostDirty = false;
            }
        }

        private bool SaveAllHostProfilesToIni()
        {
            try
            {
                string directory = Path.GetDirectoryName(PROFILE_HOST_INI_PATH);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                StringBuilder output = new StringBuilder();
                output.AppendLine("; =========================================================");
                output.AppendLine("; PLAYER PROFILE DATABASE");
                output.AppendLine("; Edit nilai di sini. Script membaca nama dropdown + statistik host.");
                output.AppendLine("; Score boleh positif, 0, atau negatif.");
                output.AppendLine("; =========================================================");
                output.AppendLine();

                for (int i = 0; i < _hostProfiles.Count; i++)
                {
                    HostProfile profile = _hostProfiles[i];
                    string sectionName = string.IsNullOrWhiteSpace(profile.SectionName)
                        ? "HOST" + (i + 1).ToString(CultureInfo.InvariantCulture)
                        : profile.SectionName.Trim();

                    output.AppendLine("[" + sectionName + "]");
                    output.AppendLine("Name=" + profile.Name);
                    output.AppendLine("Score=" + profile.Score.ToString(CultureInfo.InvariantCulture));
                    output.AppendLine("Wins=" + profile.Wins.ToString(CultureInfo.InvariantCulture));
                    output.AppendLine("Deaths=" + profile.Deaths.ToString(CultureInfo.InvariantCulture));
                    output.AppendLine("EnemyKills=" + profile.EnemyKills.ToString(CultureInfo.InvariantCulture));
                    output.AppendLine();
                }

                File.WriteAllText(
                    PROFILE_HOST_INI_PATH,
                    output.ToString(),
                    Encoding.UTF8);

                return true;
            }
            catch (Exception ex)
            {
                LogError("PROFILE HOST SAVE ERROR", ex);
                return false;
            }
        }

        private bool TryParseRandomChaosConfigKey(
            string key,
            out int optionIndex,
            out bool isDuration)
        {
            optionIndex =
                0;

            isDuration =
                false;

            const string prefix =
                "RandomChaos";

            if (string.IsNullOrWhiteSpace(key) ||
                !key.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return false;
            }

            string suffix =
                key.Substring(
                    prefix.Length
                );

            const string durationSuffix =
                "Duration";

            if (suffix.EndsWith(
                    durationSuffix,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                isDuration =
                    true;

                suffix =
                    suffix.Substring(
                        0,
                        suffix.Length -
                        durationSuffix.Length
                    );
            }

            if (!int.TryParse(
                    suffix,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out optionIndex
                ) ||
                optionIndex <= 0)
            {
                optionIndex =
                    0;

                return false;
            }

            return true;
        }

        private void LoadGameplayConfigFromIni(
            bool applyRuntimeChanges)
        {
            string iniPath =
                "scripts/MoonEffectConfig.ini";

            if (!File.Exists(iniPath))
                return;

            bool previousChaosEnabled =
                _randomChaosEnabled;

            int previousCountdownDurationMs =
                _countdownDurationMs;

            int previousPosCount =
                _routeCheckpointCount;

            bool parsedChaosEnabled =
                _randomChaosEnabled;

            int parsedCountdownDurationMs =
                _countdownDurationMs;

            int parsedChaosRouletteDurationMs =
                _chaosRouletteDurationMs;

            int parsedChaosRouletteSwitchIntervalMs =
                _chaosRouletteItemIntervalMs;

            int parsedChaosLockedDisplayMs =
                _chaosRouletteLockMs;

            int parsedPosCount =
                _routeCheckpointCount;

            Dictionary<int, string> parsedChaosKeys =
                new Dictionary<int, string>();

            Dictionary<int, int> parsedChaosDurations =
                new Dictionary<int, int>();

            try
            {
                string currentSection =
                    string.Empty;

                foreach (
                    string line
                    in File.ReadAllLines(iniPath))
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

                    if (!trimmed.Contains("="))
                        continue;

                    string[] parts =
                        trimmed.Split(
                            new char[] { '=' },
                            2
                        );

                    string key =
                        parts[0].Trim();

                    string value =
                        parts[1].Trim();

                    if (currentSection.Equals(
                            "MISSION",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (key.Equals(
                                "RandomChaosEnabled",
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            if (bool.TryParse(
                                    value,
                                    out bool enabled
                                ))
                            {
                                parsedChaosEnabled =
                                    enabled;
                            }

                            continue;
                        }

                        if (key.Equals(
                                "PosCount",
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            if (int.TryParse(
                                    value,
                                    NumberStyles.Integer,
                                    CultureInfo.InvariantCulture,
                                    out int posCount
                                ))
                            {
                                parsedPosCount =
                                    Math.Max(
                                        MIN_ROUTE_CHECKPOINT_COUNT,
                                        Math.Min(
                                            MAX_ROUTE_CHECKPOINT_COUNT,
                                            posCount
                                        )
                                    );
                            }

                            continue;
                        }
                    }
                    else if (currentSection.Equals(
                            "RANDOM_CHAOS",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        // =================================================
                        // COUNTDOWN SEBELUM RANDOM CHAOS ROULETTE
                        // =================================================
                        // Contoh:
                        // CountdownSeconds=3600  -> 60 menit
                        // CountdownSeconds=300   -> 5 menit
                        // CountdownSeconds=60    -> 1 menit
                        if (key.Equals(
                                "CountdownSeconds",
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            if (int.TryParse(
                                    value,
                                    NumberStyles.Integer,
                                    CultureInfo.InvariantCulture,
                                    out int countdownSeconds
                                ) &&
                                countdownSeconds > 0)
                            {
                                // Safety maksimal 24 jam.
                                countdownSeconds =
                                    Math.Min(
                                        countdownSeconds,
                                        24 * 60 * 60
                                    );

                                parsedCountdownDurationMs =
                                    countdownSeconds *
                                    1000;
                            }

                            continue;
                        }

                        if (key.Equals(
                                "GachaDurationMs",
                                StringComparison.OrdinalIgnoreCase
                            ) ||
                            key.Equals(
                                "RouletteDurationMs",
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            if (int.TryParse(
                                    value,
                                    NumberStyles.Integer,
                                    CultureInfo.InvariantCulture,
                                    out int rouletteMs
                                ))
                            {
                                parsedChaosRouletteDurationMs =
                                    Math.Max(
                                        250,
                                        Math.Min(
                                            60000,
                                            rouletteMs
                                        )
                                    );
                            }

                            continue;
                        }

                        if (key.Equals(
                                "GachaSwitchIntervalMs",
                                StringComparison.OrdinalIgnoreCase
                            ) ||
                            key.Equals(
                                "RouletteSwitchIntervalMs",
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            if (int.TryParse(
                                    value,
                                    NumberStyles.Integer,
                                    CultureInfo.InvariantCulture,
                                    out int switchMs
                                ))
                            {
                                parsedChaosRouletteSwitchIntervalMs =
                                    Math.Max(
                                        30,
                                        Math.Min(
                                            5000,
                                            switchMs
                                        )
                                    );
                            }

                            continue;
                        }

                        if (key.Equals(
                                "LockedDisplayMs",
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            if (int.TryParse(
                                    value,
                                    NumberStyles.Integer,
                                    CultureInfo.InvariantCulture,
                                    out int lockedMs
                                ))
                            {
                                parsedChaosLockedDisplayMs =
                                    Math.Max(
                                        0,
                                        Math.Min(
                                            30000,
                                            lockedMs
                                        )
                                    );
                            }

                            continue;
                        }

                        int optionIndex;
                        bool isDuration;

                        if (TryParseRandomChaosConfigKey(
                                key,
                                out optionIndex,
                                out isDuration
                            ))
                        {
                            if (isDuration)
                            {
                                if (int.TryParse(
                                        value,
                                        NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int durationSeconds
                                    ) &&
                                    durationSeconds > 0)
                                {
                                    durationSeconds =
                                        Math.Min(
                                            durationSeconds,
                                            24 * 60 * 60
                                        );

                                    parsedChaosDurations[
                                        optionIndex
                                    ] =
                                        durationSeconds *
                                        1000;
                                }
                            }
                            else
                            {
                                string normalizedKey =
                                    NormalizeRandomChaosKey(
                                        value
                                    );

                                if (!string.IsNullOrEmpty(
                                        normalizedKey
                                    ))
                                {
                                    parsedChaosKeys[
                                        optionIndex
                                    ] =
                                        normalizedKey;
                                }
                            }

                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "GAMEPLAY CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[GAMEPLAY CONFIG ERROR]~w~ " +
                    ex.Message,
                    false
                );

                return;
            }

            _randomChaosEnabled =
                _selectedDifficultyMode != MoonDifficultyMode.None
                    ? DifficultyEnablesRandomChaos()
                    : parsedChaosEnabled;

            _countdownDurationMs =
                parsedCountdownDurationMs;

            _chaosRouletteDurationMs =
                parsedChaosRouletteDurationMs;

            _chaosRouletteItemIntervalMs =
                parsedChaosRouletteSwitchIntervalMs;

            _chaosRouletteLockMs =
                parsedChaosLockedDisplayMs;

            _routeCheckpointCount =
                parsedPosCount;

            if (parsedChaosKeys.Count > 0)
            {
                List<RandomChaosOption> newOptions =
                    new List<RandomChaosOption>();

                foreach (
                    int optionIndex in
                    parsedChaosKeys.Keys.OrderBy(
                        index => index
                    ))
                {
                    string chaosKey =
                        parsedChaosKeys[
                            optionIndex
                        ];

                    int durationMs =
                        GetDefaultEffectDurationMs(
                            chaosKey
                        );

                    if (durationMs <= 0)
                    {
                        durationMs =
                            DEFAULT_RANDOM_CHAOS_DURATION_MS;
                    }

                    if (IsTimedConfigurableRandomEffect(
                            chaosKey
                        ) &&
                        parsedChaosDurations.TryGetValue(
                            optionIndex,
                            out int configuredDurationMs
                        ))
                    {
                        durationMs =
                            configuredDurationMs;
                    }

                    if (!IsTimedConfigurableRandomEffect(
                            chaosKey
                        ))
                    {
                        durationMs =
                            0;
                    }

                    newOptions.Add(
                        new RandomChaosOption
                        {
                            Key =
                                chaosKey,

                            DisplayName =
                                GetRandomChaosDisplayName(
                                    chaosKey
                                ),

                            DurationMs =
                                durationMs
                        }
                    );
                }

                _randomChaosOptions.Clear();
                _randomChaosOptions.AddRange(
                    newOptions
                );

                if (_currentChaosRouletteIndex >=
                    _randomChaosOptions.Count)
                {
                    _currentChaosRouletteIndex =
                        0;
                }
            }

            if (!applyRuntimeChanges)
                return;

            // =====================================================
            // RANDOM CHAOS TOGGLE BERUBAH SAAT GAME SEDANG JALAN
            // =====================================================
            if (previousChaosEnabled &&
                !_randomChaosEnabled)
            {
                CancelChaosMode();

                _isCountdownActive =
                    false;

                _countdownRemainingMs =
                    0;

                ShowHudNotification(
                    "~y~[RANDOM CHAOS] ~w~DISABLED FROM INI"
                );
            }
            else if (!previousChaosEnabled &&
                     _randomChaosEnabled)
            {
                StartApocalypseTimer();

                ShowHudNotification(
                    "~g~[RANDOM CHAOS] ~w~ENABLED FROM INI"
                );
            }

            // =====================================================
            // RANDOM CHAOS COUNTDOWN BERUBAH SAAT GAME JALAN
            // =====================================================
            if (previousCountdownDurationMs !=
                _countdownDurationMs)
            {
                if (_randomChaosEnabled &&
                    _isCountdownActive &&
                    !_isChaosRouletteActive &&
                    !_isChaosActive)
                {
                    // Sedang NORMAL countdown:
                    // langsung reset ke nilai baru supaya edit INI
                    // terasa saat itu juga.
                    _countdownRemainingMs =
                        _countdownDurationMs;

                    _lastCountdownUpdateTime =
                        Game.GameTime;

                    ShowHudNotification(
                        "~b~[RANDOM CHAOS] ~w~COUNTDOWN = " +
                        (_countdownDurationMs / 1000) +
                        " SEC"
                    );
                }
                else if (_randomChaosEnabled)
                {
                    // Sedang roulette/effect:
                    // value baru dipakai saat countdown normal berikutnya.
                    ShowHudNotification(
                        "~b~[RANDOM CHAOS] ~w~NEXT COUNTDOWN = " +
                        (_countdownDurationMs / 1000) +
                        " SEC"
                    );
                }
            }

            // PosCount sengaja TIDAK meregenerate route aktif.
            // Nilai baru otomatis dipakai saat route berikutnya dibuat.
            if (previousPosCount !=
                _routeCheckpointCount)
            {
                ShowHudNotification(
                    "~b~[ROUTE CONFIG] ~w~POS COUNT = " +
                    _routeCheckpointCount +
                    " (NEXT ROUTE)"
                );
            }
        }

        private bool TryParseRandomGatchaConfigKey(
            string key,
            out int optionIndex,
            out bool isDuration)
        {
            optionIndex = 0;
            isDuration = false;

            const string prefix =
                "RandomGatcha";

            if (string.IsNullOrWhiteSpace(key) ||
                !key.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return false;
            }

            string suffix =
                key.Substring(
                    prefix.Length
                );

            const string durationSuffix =
                "Duration";

            if (suffix.EndsWith(
                    durationSuffix,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                isDuration = true;
                suffix =
                    suffix.Substring(
                        0,
                        suffix.Length -
                        durationSuffix.Length
                    );
            }

            return
                int.TryParse(
                    suffix,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out optionIndex
                ) &&
                optionIndex > 0;
        }

        private void LoadRandomGatchaConfigFromIni()
        {
            string iniPath =
                "scripts/MoonEffectConfig.ini";

            if (!File.Exists(iniPath))
                return;

            int parsedRollMs =
                _randomGatchaRollMs;

            int parsedLockMs =
                _randomGatchaLockMs;

            int parsedSwitchMs =
                _randomGatchaSwitchMs;

            Dictionary<int, string> parsedKeys =
                new Dictionary<int, string>();

            Dictionary<int, int> parsedDurations =
                new Dictionary<int, int>();

            try
            {
                string currentSection =
                    string.Empty;

                foreach (
                    string line in
                    File.ReadAllLines(iniPath))
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

                    if (!currentSection.Equals(
                            "RANDOM_GATCHA",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        !trimmed.Contains("="))
                    {
                        continue;
                    }

                    string[] parts =
                        trimmed.Split(
                            new char[] { '=' },
                            2
                        );

                    string key =
                        parts[0].Trim();

                    string value =
                        parts[1].Trim();

                    if (key.Equals(
                            "GachaDurationMs",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        key.Equals(
                            "RouletteDurationMs",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int rollMs
                            ))
                        {
                            parsedRollMs =
                                Math.Max(
                                    250,
                                    Math.Min(
                                        60000,
                                        rollMs
                                    )
                                );
                        }

                        continue;
                    }

                    if (key.Equals(
                            "GachaSwitchIntervalMs",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        key.Equals(
                            "RouletteSwitchIntervalMs",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int switchMs
                            ))
                        {
                            parsedSwitchMs =
                                Math.Max(
                                    30,
                                    Math.Min(
                                        5000,
                                        switchMs
                                    )
                                );
                        }

                        continue;
                    }

                    if (key.Equals(
                            "LockedDisplayMs",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int lockMs
                            ))
                        {
                            parsedLockMs =
                                Math.Max(
                                    0,
                                    Math.Min(
                                        30000,
                                        lockMs
                                    )
                                );
                        }

                        continue;
                    }

                    if (TryParseRandomGatchaConfigKey(
                            key,
                            out int optionIndex,
                            out bool isDuration
                        ))
                    {
                        if (isDuration)
                        {
                            if (int.TryParse(
                                    value,
                                    NumberStyles.Integer,
                                    CultureInfo.InvariantCulture,
                                    out int seconds
                                ) &&
                                seconds > 0)
                            {
                                seconds =
                                    Math.Min(
                                        seconds,
                                        24 * 60 * 60
                                    );

                                parsedDurations[
                                    optionIndex
                                ] =
                                    seconds * 1000;
                            }
                        }
                        else
                        {
                            string normalized =
                                NormalizeRandomGatchaEffectKey(
                                    value
                                );

                            if (!string.IsNullOrEmpty(
                                    normalized
                                ))
                            {
                                parsedKeys[
                                    optionIndex
                                ] =
                                    normalized;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "RANDOM GATCHA CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[RANDOM GATCHA CONFIG ERROR]~w~ " +
                    ex.Message,
                    false
                );

                return;
            }

            _randomGatchaRollMs =
                parsedRollMs;

            _randomGatchaLockMs =
                parsedLockMs;

            _randomGatchaSwitchMs =
                parsedSwitchMs;

            if (parsedKeys.Count > 0)
            {
                List<RandomGatchaOption> newOptions =
                    new List<RandomGatchaOption>();

                foreach (
                    int optionIndex in
                    parsedKeys.Keys.OrderBy(
                        index => index
                    ))
                {
                    string effectKey =
                        parsedKeys[
                            optionIndex
                        ];

                    int configuredDurationMs =
                        0;

                    parsedDurations.TryGetValue(
                        optionIndex,
                        out configuredDurationMs
                    );

                    newOptions.Add(
                        new RandomGatchaOption
                        {
                            Key =
                                effectKey,

                            DisplayName =
                                GetRandomGatchaDisplayName(
                                    effectKey
                                ),

                            DurationMs =
                                ResolveConfiguredEffectDurationMs(
                                    effectKey,
                                    configuredDurationMs
                                )
                        }
                    );
                }

                _randomGatchaOptions.Clear();
                _randomGatchaOptions.AddRange(
                    newOptions
                );
            }
            else
            {
                // Section/pool tidak ada: tetap isi duration fallback
                // untuk default pool hardcoded agar TIME_DEFAULT berlaku.
                foreach (
                    RandomGatchaOption option in
                    _randomGatchaOptions)
                {
                    if (option != null &&
                        option.DurationMs <= 0)
                    {
                        option.DurationMs =
                            GetDefaultEffectDurationMs(
                                option.Key
                            );
                    }
                }
            }

            if (_randomGatchaIndex >=
                _randomGatchaOptions.Count)
            {
                _randomGatchaIndex =
                    0;
            }
        }

        private void LoadRandomCheckpointConfigFromIni(
            bool applyRuntimeChanges)
        {
            string iniPath =
                "scripts/MoonEffectConfig.ini";

            if (!File.Exists(iniPath))
                return;

            bool previousEnabled =
                _randomCheckpointEnabled;

            int previousPerLeg =
                _randomCheckpointPerLeg;

            int previousDirectCount =
                _randomCheckpointDirectCount;

            bool parsedEnabled =
                _randomCheckpointEnabled;

            int parsedPerLeg =
                _randomCheckpointPerLeg;

            int parsedDirectCount =
                _randomCheckpointDirectCount;

            int parsedCountdownSeconds =
                _randomCheckpointCountdownSeconds;

            int parsedGachaDurationMs =
                _randomCheckpointGachaDurationMs;

            int parsedSwitchIntervalMs =
                _randomCheckpointGachaSwitchIntervalMs;

            int parsedLockedDisplayMs =
                _randomCheckpointLockedDisplayMs;

            Dictionary<int, string> parsedEffects =
                new Dictionary<int, string>();

            Dictionary<int, int> parsedEffectDurations =
                new Dictionary<int, int>();

            try
            {
                string currentSection =
                    string.Empty;

                foreach (
                    string line in
                    File.ReadAllLines(iniPath))
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

                    if (!trimmed.Contains("="))
                    {
                        continue;
                    }

                    string[] parts =
                        trimmed.Split(
                            new char[] { '=' },
                            2
                        );

                    string key =
                        parts[0].Trim();

                    string value =
                        parts[1].Trim();

                    if (currentSection.Equals(
                            "MISSION",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (key.Equals(
                                "RandomCheckpointEnabled",
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            if (bool.TryParse(
                                    value,
                                    out bool enabled
                                ))
                            {
                                parsedEnabled =
                                    enabled;
                            }
                        }

                        continue;
                    }

                    if (!currentSection.Equals(
                            "RANDOM_CHECKPOINT",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        continue;
                    }

                    if (key.Equals(
                            "CheckpointPerLeg",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int count
                            ))
                        {
                            parsedPerLeg =
                                Math.Max(
                                    1,
                                    Math.Min(
                                        20,
                                        count
                                    )
                                );
                        }

                        continue;
                    }

                    if (key.Equals(
                            "DirectCheckpointCount",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int count
                            ))
                        {
                            parsedDirectCount =
                                Math.Max(
                                    1,
                                    Math.Min(
                                        50,
                                        count
                                    )
                                );
                        }

                        continue;
                    }

                    if (key.Equals(
                            "CountdownSeconds",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int seconds
                            ))
                        {
                            parsedCountdownSeconds =
                                Math.Max(
                                    1,
                                    Math.Min(
                                        60,
                                        seconds
                                    )
                                );
                        }

                        continue;
                    }

                    if (key.Equals(
                            "GachaDurationMs",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int durationMs
                            ))
                        {
                            parsedGachaDurationMs =
                                Math.Max(
                                    250,
                                    Math.Min(
                                        30000,
                                        durationMs
                                    )
                                );
                        }

                        continue;
                    }

                    if (key.Equals(
                            "GachaSwitchIntervalMs",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int intervalMs
                            ))
                        {
                            parsedSwitchIntervalMs =
                                Math.Max(
                                    30,
                                    Math.Min(
                                        2000,
                                        intervalMs
                                    )
                                );
                        }

                        continue;
                    }

                    if (key.Equals(
                            "LockedDisplayMs",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int lockedMs
                            ))
                        {
                            parsedLockedDisplayMs =
                                Math.Max(
                                    0,
                                    Math.Min(
                                        10000,
                                        lockedMs
                                    )
                                );
                        }

                        continue;
                    }

                    if (key.StartsWith(
                            "Effect",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        string indexText =
                            key.Substring(
                                "Effect".Length
                            );

                        bool isDuration =
                            indexText.EndsWith(
                                "Duration",
                                StringComparison.OrdinalIgnoreCase
                            );

                        if (isDuration)
                        {
                            indexText =
                                indexText.Substring(
                                    0,
                                    indexText.Length -
                                    "Duration".Length
                                );
                        }

                        if (int.TryParse(
                                indexText,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int effectIndex
                            ) &&
                            effectIndex > 0)
                        {
                            if (isDuration)
                            {
                                if (int.TryParse(
                                        value,
                                        NumberStyles.Integer,
                                        CultureInfo.InvariantCulture,
                                        out int seconds
                                    ) &&
                                    seconds > 0)
                                {
                                    seconds =
                                        Math.Min(
                                            seconds,
                                            24 * 60 * 60
                                        );

                                    parsedEffectDurations[
                                        effectIndex
                                    ] =
                                        seconds * 1000;
                                }
                            }
                            else
                            {
                                string normalizedEffect =
                                    NormalizeRandomCheckpointEffectKey(
                                        value
                                    );

                                if (!string.IsNullOrEmpty(
                                        normalizedEffect
                                    ))
                                {
                                    parsedEffects[
                                        effectIndex
                                    ] =
                                        normalizedEffect;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "RANDOM CHECKPOINT CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[RANDOM CHECKPOINT CONFIG ERROR]~w~ " +
                    ex.Message,
                    false
                );

                return;
            }

            _randomCheckpointEnabled =
                _selectedDifficultyMode != MoonDifficultyMode.None
                    ? DifficultyEnablesRandomCheckpoint()
                    : parsedEnabled;

            _randomCheckpointPerLeg =
                parsedPerLeg;

            _randomCheckpointDirectCount =
                parsedDirectCount;

            _randomCheckpointCountdownSeconds =
                parsedCountdownSeconds;

            _randomCheckpointGachaDurationMs =
                parsedGachaDurationMs;

            _randomCheckpointGachaSwitchIntervalMs =
                parsedSwitchIntervalMs;

            _randomCheckpointLockedDisplayMs =
                parsedLockedDisplayMs;

            if (parsedEffects.Count > 0)
            {
                _randomCheckpointEffectPool.Clear();
                _randomCheckpointEffectDurationsMs.Clear();

                foreach (
                    int effectIndex in
                    parsedEffects.Keys.OrderBy(
                        index => index
                    ))
                {
                    string effectKey =
                        parsedEffects[
                            effectIndex
                        ];

                    _randomCheckpointEffectPool.Add(
                        effectKey
                    );

                    int durationMs =
                        0;

                    if (parsedEffectDurations.TryGetValue(
                            effectIndex,
                            out int configuredDurationMs
                        ))
                    {
                        durationMs =
                            configuredDurationMs;
                    }

                    durationMs =
                        ResolveConfiguredEffectDurationMs(
                            effectKey,
                            durationMs
                        );

                    if (durationMs > 0)
                    {
                        _randomCheckpointEffectDurationsMs[
                            effectKey
                        ] =
                            durationMs;
                    }
                }
            }

            // Fail-safe: pool tidak boleh kosong.
            if (_randomCheckpointEffectPool.Count == 0)
            {
                _randomCheckpointEffectPool.Add(
                    "get_towed"
                );
                _randomCheckpointEffectPool.Add(
                    "traffic_magnet"
                );
                _randomCheckpointEffectPool.Add(
                    "reckless_traffic"
                );
                _randomCheckpointEffectPool.Add(
                    "vehicle_fire_timer"
                );
                _randomCheckpointEffectPool.Add(
                    "police_roadblock"
                );
                _randomCheckpointEffectPool.Add(
                    "random_explosion_nearby"
                );
                _randomCheckpointEffectPool.Add(
                    "wanted_5_star"
                );
            }

            foreach (
                string effectKey in
                _randomCheckpointEffectPool)
            {
                if (!_randomCheckpointEffectDurationsMs.ContainsKey(
                        effectKey
                    ))
                {
                    int fallbackDurationMs =
                        GetDefaultEffectDurationMs(
                            effectKey
                        );

                    if (fallbackDurationMs > 0)
                    {
                        _randomCheckpointEffectDurationsMs[
                            effectKey
                        ] =
                            fallbackDurationMs;
                    }
                }
            }

            if (!applyRuntimeChanges)
                return;

            // Perubahan jumlah marker harus langsung membangun ulang marker
            // leg aktif. Marker yang sudah berada di belakang player ditandai
            // consumed agar edit INI tidak memicu checkpoint lama sekaligus.
            if (previousPerLeg !=
                    _randomCheckpointPerLeg ||
                previousDirectCount !=
                    _randomCheckpointDirectCount)
            {
                ResetRandomCheckpointProgressTracking();
            }

            if (previousEnabled &&
                !_randomCheckpointEnabled)
            {
                CancelRandomCheckpointSequence(
                    clearPending: true
                );

                ResetActiveRandomCheckpointEffects();
                ResetRandomCheckpointProgressTracking();

                ShowHudNotification(
                    "~y~[RANDOM CHECKPOINT] ~w~DISABLED FROM INI"
                );
            }
            else if (!previousEnabled &&
                     _randomCheckpointEnabled)
            {
                ResetRandomCheckpointProgressTracking();

                ShowHudNotification(
                    "~g~[RANDOM CHECKPOINT] ~w~ENABLED FROM INI"
                );
            }
        }

        private DateTime GetConfigLastWriteTimeUtc(
            string path)
        {
            try
            {
                if (!File.Exists(path))
                    return DateTime.MinValue;

                return File.GetLastWriteTimeUtc(path);
            }
            catch (Exception ex)
            {
                LogError(
                    "CONFIG TIMESTAMP ERROR",
                    ex
                );

                return DateTime.MinValue;
            }
        }

        private DateTime GetGameplayConfigCompositeLastWriteTimeUtc()
        {
            return new[]
            {
                GetConfigLastWriteTimeUtc("scripts/GoToMountainConfig.ini"),
                GetConfigLastWriteTimeUtc("scripts/MoonEffectConfig.ini"),
                GetConfigLastWriteTimeUtc("scripts/MoonModConfig.ini"),
                GetConfigLastWriteTimeUtc("scripts/SurvivalConfig.ini")
            }.Max();
        }

        private void ProcessRuntimeGameplayConfig(
            int currentTime)
        {
            if (currentTime <
                _nextGameplayConfigReloadTime)
            {
                return;
            }

            _nextGameplayConfigReloadTime =
                currentTime +
                GAMEPLAY_CONFIG_RELOAD_INTERVAL_MS;

            // One composite timestamp watches every gameplay/effect/global INI.
            DateTime gameplayWriteTimeUtc =
                GetGameplayConfigCompositeLastWriteTimeUtc();

            if (gameplayWriteTimeUtc !=
                _lastGameplayIniWriteTimeUtc)
            {
                bool previousUnderwaterEnabled =
                    _underwaterSystemEnabled;

                // Semua section gameplay yang dibaca saat startup juga
                // harus ikut hot-reload. Jangan ada setting yang terlihat
                // editable tetapi baru aktif setelah script restart.
                LoadMoonModConfigFromIni();
                LoadSurvivalConfigFromIni();
                LoadGiftTimerConfigFromIni();
                LoadRandomTeleportConfigFromIni();
                LoadGameplayConfigFromIni(
                    applyRuntimeChanges: true
                );
                LoadRandomGatchaConfigFromIni();
                LoadRandomCheckpointConfigFromIni(
                    applyRuntimeChanges: true
                );
                LoadMissionConfigFromIni(
                    applyRuntimeChanges: true
                );
                LoadEntityLimitsConfigFromIni(
                    applyRuntimeChanges: true
                );
                LoadPlayerAndHealthBarConfig();
                LoadUnderwaterConfigFromIni();
                LoadHitByVehicleConfigFromIni();

                // Pilihan startup tetap authoritative walaupun INI di-hot-reload.
                ApplySelectedGameplaySettingsToLegacyFlags();
                ApplySelectedDifficultySettingsToRuntimeFlags();

                // [LOCATIONS] boleh diubah live. Jangan mengganti pool saat
                // roulette sedang aktif karena index/lock bisa menunjuk item
                // berbeda. Tunda sampai roulette selesai.
                if (_isTeleportGachaActive)
                {
                    _teleportGachaPoolReloadPending = true;
                }
                else
                {
                    InitializeTeleportGachaPool();
                    _teleportGachaPoolReloadPending = false;
                }

                // Kalau underwater dimatikan saat timer/cooldown aktif,
                // batalkan semua state tertunda.
                if (previousUnderwaterEnabled &&
                    !_underwaterSystemEnabled)
                {
                    _isUnderwaterTimerActive = false;
                    _underwaterStartTime = 0;
                    _underwaterCooldownEndTime = 0;
                }

                // Update timestamp SETELAH rangkaian reload selesai.
                _lastGameplayIniWriteTimeUtc =
                    gameplayWriteTimeUtc;
            }

            // =========================================================
            // Entity.ini
            // =========================================================
            string entityIniPath =
                "scripts/Entity.ini";

            DateTime entityWriteTimeUtc =
                GetConfigLastWriteTimeUtc(
                    entityIniPath
                );

            if (entityWriteTimeUtc !=
                _lastEntityIniWriteTimeUtc)
            {
                // Loader database entity memakai snapshot swap sehingga
                // listener webhook tidak pernah membaca Dictionary yang
                // sedang di-Clear/diisi ulang.
                LoadNpcConfigFromIni();
                LoadBodyguardConfigFromIni();
                LoadAnimalConfigFromIni();
                LoadGiveVehicleConfigFromIni();
                LoadFallingVehiclesConfigFromIni();

                _lastEntityIniWriteTimeUtc =
                    entityWriteTimeUtc;
            }

            // =========================================================
            // GameplayHUD.ini + HUD preset
            // =========================================================
            string designIniPath =
                GetActiveGameplayHudIniPath();

            string hudLayoutIniPath =
                GetActiveHudLayoutIniPath();

            string hudPresetIniPath =
                GetActiveHudPresetIniPath();

            DateTime designWriteTimeUtc =
                GetConfigLastWriteTimeUtc(
                    designIniPath
                );

            DateTime hudLayoutWriteTimeUtc =
                GetConfigLastWriteTimeUtc(
                    hudLayoutIniPath
                );

            DateTime hudPresetWriteTimeUtc =
                GetConfigLastWriteTimeUtc(
                    hudPresetIniPath
                );

            bool hudLayoutNameChanged =
                !string.Equals(
                    _lastLoadedHudLayoutName,
                    _hudLayoutName,
                    StringComparison.OrdinalIgnoreCase
                );

            bool hudPresetNameChanged =
                !string.Equals(
                    _lastLoadedHudPresetName,
                    _hudPresetName,
                    StringComparison.OrdinalIgnoreCase
                );

            if (designWriteTimeUtc !=
                    _lastDesignIniWriteTimeUtc ||
                hudLayoutWriteTimeUtc !=
                    _lastHudLayoutIniWriteTimeUtc ||
                hudPresetWriteTimeUtc !=
                    _lastHudPresetIniWriteTimeUtc ||
                hudLayoutNameChanged ||
                hudPresetNameChanged)
            {
                LoadDesignConfigFromIni();

                _lastDesignIniWriteTimeUtc =
                    designWriteTimeUtc;

                _lastHudLayoutIniWriteTimeUtc =
                    hudLayoutWriteTimeUtc;

                _lastHudPresetIniWriteTimeUtc =
                    hudPresetWriteTimeUtc;

                _lastLoadedHudLayoutName =
                    _hudLayoutName;

                _lastLoadedHudPresetName =
                    _hudPresetName;
            }

            ApplySelectedGameplaySettingsToLegacyFlags();
        }

        private void LoadEntityLimitsConfigFromIni(
            bool applyRuntimeChanges)
        {
            string iniPath =
                "scripts/Entity.ini";

            if (!File.Exists(iniPath))
                return;

            ScriptSettings config =
                ScriptSettings.Load(iniPath);

            int newMaxEnemyUnits =
                ClampEntityLimit(
                    config.GetValue<int>(
                        "ENTITY_LIMITS",
                        "MaxEnemies",
                        25
                    ),
                    25
                );

            int newMaxAnimals =
                ClampEntityLimit(
                    config.GetValue<int>(
                        "ENTITY_LIMITS",
                        "MaxAnimals",
                        15
                    ),
                    15
                );

            int newMaxBodyguards =
                ClampEntityLimit(
                    config.GetValue<int>(
                        "ENTITY_LIMITS",
                        "MaxBodyguards",
                        15
                    ),
                    15
                );

            bool changed =
                newMaxEnemyUnits != _maxEnemyUnits ||
                newMaxAnimals != _maxAnimals ||
                newMaxBodyguards != _maxBodyguards;

            _maxEnemyUnits =
                newMaxEnemyUnits;

            _maxAnimals =
                newMaxAnimals;

            _maxBodyguards =
                newMaxBodyguards;

            if (!applyRuntimeChanges)
                return;

            // =====================================================
            // Kalau limit diturunkan saat game berjalan,
            // langsung pangkas entity paling lama sampai sesuai.
            // =====================================================

            while (
                GetActiveEnemyUnitCount() >
                    _maxEnemyUnits &&
                _activeBosses.Count > 0
            )
            {
                RemoveOldestEnemyUnit();
            }

            _activeBodyguards.RemoveAll(
                b =>
                    b == null ||
                    b.BodyguardPed == null ||
                    !b.BodyguardPed.Exists()
            );

            while (
                _activeBodyguards.Count >
                    _maxBodyguards
            )
            {
                ActiveBodyguardTracker oldest =
                    _activeBodyguards[0];

                if (oldest.BodyguardPed != null &&
                    oldest.BodyguardPed.Exists())
                {
                    oldest.BodyguardPed.Delete();
                }

                _activeBodyguards.RemoveAt(0);
            }

            _activeAnimals.RemoveAll(
                a =>
                    a == null ||
                    a.AnimalPed == null ||
                    !a.AnimalPed.Exists()
            );

            while (
                _activeAnimals.Count >
                    _maxAnimals
            )
            {
                ActiveAnimalTracker oldest =
                    _activeAnimals[0];

                if (oldest.AnimalPed != null &&
                    oldest.AnimalPed.Exists())
                {
                    oldest.AnimalPed.Delete();
                }

                _activeAnimals.RemoveAt(0);
            }

            if (changed)
            {
                ShowHudNotification(
                    "~b~[ENTITY LIMITS] ~w~" +
                    "ENEMY " +
                    _maxEnemyUnits +
                    " | ANIMAL " +
                    _maxAnimals +
                    " | BODYGUARD " +
                    _maxBodyguards
                );
            }
        }

        private int ParseGiftSecondsToMilliseconds(
            string value,
            int fallbackMs)
        {
            if (!int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int seconds
                ) ||
                seconds <= 0)
            {
                return fallbackMs;
            }

            // Safety: maksimal 24 jam per gift.
            seconds =
                Math.Min(
                    seconds,
                    24 * 60 * 60
                );

            return
                seconds * 1000;
        }

        private void LoadRandomTeleportConfigFromIni()
        {
            string iniPath =
                "scripts/MoonEffectConfig.ini";

            if (!File.Exists(iniPath))
                return;

            try
            {
                string currentSection =
                    string.Empty;

                foreach (
                    string line
                    in File.ReadAllLines(iniPath))
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

                    if (!currentSection.Equals(
                            "RANDOM_TELEPORT",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        !trimmed.Contains("="))
                    {
                        continue;
                    }

                    string[] parts =
                        trimmed.Split(
                            new char[] { '=' },
                            2
                        );

                    string key =
                        parts[0].Trim();

                    string value =
                        parts[1].Trim();

                    if (key.Equals(
                            "UseGachaCards",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        key.Equals(
                            "GachaCards",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        bool parsedValue;

                        if (bool.TryParse(
                                value,
                                out parsedValue
                            ))
                        {
                            _randomTeleportUseGachaCards =
                                parsedValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "RANDOM TELEPORT CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[RANDOM TELEPORT CONFIG ERROR]~w~ " +
                    ex.Message,
                    false
                );
            }
        }

        private void LoadGiftTimerConfigFromIni()
        {
            string iniPath =
                "scripts/MoonEffectConfig.ini";

            if (!File.Exists(iniPath))
                return;

            try
            {
                string currentSection =
                    string.Empty;

                foreach (
                    string line
                    in File.ReadAllLines(iniPath))
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

                    bool isTimeDefaultSection =
                        currentSection.Equals(
                            "TIME_DEFAULT",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        currentSection.Equals(
                            "GIFT_TIMERS",
                            StringComparison.OrdinalIgnoreCase
                        );

                    if (!isTimeDefaultSection ||
                        !trimmed.Contains("="))
                    {
                        continue;
                    }

                    string[] parts =
                        trimmed.Split(
                            new char[] { '=' },
                            2
                        );

                    string key =
                        parts[0].Trim();

                    string value =
                        parts[1].Trim();

                    if (key.Equals(
                            "CageSeconds",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        _giftCageDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftCageDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "DisarmSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftDisarmDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftDisarmDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "SleepSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftSleepDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftSleepDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "BallRainSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftRockRainDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftRockRainDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "FallingVehiclesSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftFallingVehiclesDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftFallingVehiclesDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "SuperSpeedSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftSuperSpeedDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftSuperSpeedDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "SuperSpeedVehicleInitialBoost",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (float.TryParse(
                                value,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out float parsedVehicleBoost
                            ))
                        {
                            _superSpeedVehicleInitialBoost =
                                Math.Max(0.0f, Math.Min(60.0f, parsedVehicleBoost));
                        }
                    }
                    else if (key.Equals(
                                 "SuperSpeedBicycleInitialBoost",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (float.TryParse(
                                value,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out float parsedBicycleBoost
                            ))
                        {
                            _superSpeedBicycleInitialBoost =
                                Math.Max(0.0f, Math.Min(28.0f, parsedBicycleBoost));
                        }
                    }
                    else if (key.Equals(
                                 "InvincibleSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftInvincibleDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftInvincibleDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "AnimalTransformSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftAnimalTransformDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftAnimalTransformDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "BlackholeSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ) ||
                             key.Equals(
                                 "BlackholeInitialSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        // BlackholeInitialSeconds tetap diterima sebagai legacy,
                        // tetapi format baru hanya memakai BlackholeSeconds.
                        _giftBlackholeDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftBlackholeDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "RandomTeleportRouletteSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftRandomTeleportRouletteDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftRandomTeleportRouletteDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "RandomTeleportLockSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftRandomTeleportLockDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftRandomTeleportLockDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "WantedFiveStarSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftWantedFiveStarDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftWantedFiveStarDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "EarthquakeSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _giftEarthquakeDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _giftEarthquakeDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "GetTowedSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _getTowedDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _getTowedDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "TrafficMagnetSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _trafficMagnetDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _trafficMagnetDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "RecklessTrafficSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _recklessTrafficDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _recklessTrafficDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "VehicleFireTimerSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _vehicleFireTimerDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _vehicleFireTimerDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "BurningCountdownSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _burningCountdownDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _burningCountdownDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "BurningSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _burningDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _burningDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "PoliceRoadblockSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _policeRoadblockDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _policeRoadblockDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "RandomExplosionNearbySeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        _randomExplosionDurationMs =
                            ParseGiftSecondsToMilliseconds(
                                value,
                                _randomExplosionDurationMs
                            );
                    }
                    else if (key.Equals(
                                 "RandomExplosionNearbyTotal",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int totalExplosions
                            ) &&
                            totalExplosions > 0)
                        {
                            _randomExplosionTotal =
                                Math.Min(
                                    totalExplosions,
                                    5000
                                );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "GIFT TIMER ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[GIFT TIMER ERROR]~w~ " +
                    ex.Message,
                    false
                );
            }
        }

        private void LoadMissionConfigFromIni(
            bool applyRuntimeChanges)
        {
            string iniPath =
                "scripts/GoToMountainConfig.ini";

            if (!File.Exists(iniPath))
                return;

            bool previousEnabled =
                _missionGoToMountainEnabled;

            bool previousGotoMountainPos =
                _missionGoToMountainPosEnabled;

            Vector3 previousStart =
                _missionStartPosition;

            Vector3 previousFinish =
                _missionFinishPosition;

            bool parsedEnabled =
                _missionGoToMountainEnabled;

            bool parsedGotoMountainPos =
                _missionGoToMountainPosEnabled;

            Vector3 parsedStart =
                _missionStartPosition;

            Vector3 parsedFinish =
                _missionFinishPosition;

            bool parsedFallbackToDirect =
                _goToMountainFallbackToDirectOnRouteFail;

            int parsedRouteBuildTimeoutMs =
                _goToMountainRouteBuildTimeoutMs;

            try
            {
                string currentSection =
                    string.Empty;

                foreach (
                    string line in
                    File.ReadAllLines(iniPath))
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

                    if (!currentSection.Equals(
                            "MISSION",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        !trimmed.Contains("="))
                    {
                        continue;
                    }

                    string[] parts =
                        trimmed.Split(
                            new char[] { '=' },
                            2
                        );

                    string key =
                        parts[0].Trim();

                    string value =
                        parts[1].Trim();

                    if (key.Equals(
                            "GoToMountain",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (bool.TryParse(
                                value,
                                out bool enabled
                            ))
                        {
                            parsedEnabled =
                                enabled;
                        }
                    }
                    else if (key.Equals(
                                 "GotoMountainPos",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (bool.TryParse(
                                value,
                                out bool enabled
                            ))
                        {
                            parsedGotoMountainPos =
                                enabled;
                        }
                    }
                    else if (key.Equals(
                                 "FallbackToDirectOnRouteFail",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (bool.TryParse(
                                value,
                                out bool enabled
                            ))
                        {
                            parsedFallbackToDirect =
                                enabled;
                        }
                    }
                    else if (key.Equals(
                                 "RouteBuildTimeoutSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int seconds
                            ))
                        {
                            seconds =
                                Math.Max(
                                    3,
                                    Math.Min(
                                        120,
                                        seconds
                                    )
                                );

                            parsedRouteBuildTimeoutMs =
                                seconds * 1000;
                        }
                    }
                    else if (key.Equals(
                                 "START_POS",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        parsedStart =
                            ParseVector3(
                                value,
                                parsedStart
                            );
                    }
                    else if (key.Equals(
                                 "FINISH_POS",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        parsedFinish =
                            ParseVector3(
                                value,
                                parsedFinish
                            );
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "MISSION CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[MISSION CONFIG ERROR]~w~ " +
                    ex.Message,
                    false
                );

                return;
            }

            _goToMountainFallbackToDirectOnRouteFail =
                parsedFallbackToDirect;

            _goToMountainRouteBuildTimeoutMs =
                parsedRouteBuildTimeoutMs;

            // Setelah user mengunci pilihan di UI startup, pilihan itu menjadi
            // sumber kebenaran untuk MODE. Reload config tetap boleh mengubah
            // START_POS / FINISH_POS, tetapi tidak boleh memindahkan mode gameplay.
            if (_gameplaySelectionLocked)
            {
                parsedEnabled =
                    _selectedGameplayMode == MoonGameplayMode.GoToMountain;

                parsedGotoMountainPos =
                    parsedEnabled && !_selectedGoToMountainDirect;
            }

            _missionGoToMountainEnabled =
                parsedEnabled;

            // GotoMountainPos hanya berlaku saat GoToMountain=true.
            // Saat GoToMountain=false, route global WAJIB memakai POS.
            _missionGoToMountainPosEnabled =
                parsedEnabled
                    ? parsedGotoMountainPos
                    : true;

            _missionStartPosition =
                parsedStart;

            _missionFinishPosition =
                parsedFinish;

            if (!applyRuntimeChanges)
                return;

            bool missionChanged =
                previousEnabled !=
                    _missionGoToMountainEnabled ||
                previousGotoMountainPos !=
                    _missionGoToMountainPosEnabled ||
                previousStart.DistanceTo(
                    _missionStartPosition
                ) > 0.01f ||
                previousFinish.DistanceTo(
                    _missionFinishPosition
                ) > 0.01f;

            if (!missionChanged)
                return;

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                player.IsDead)
            {
                // Perubahan config tetap sudah tersimpan di state.
                // Route akan dibentuk saat player valid.
                _initialRouteSetupPending =
                    true;

                return;
            }

            _isInsideCheckpoint =
                false;

            _checkpointTouchStartTime =
                0;

            CleanupGiftCage();

            if (_missionGoToMountainEnabled)
            {
                // Saat streamer menyalakan GoToMountain dari INI,
                // mulai ulang dari START mission tetap.
                _startPosition =
                    _missionStartPosition;

                Entity target =
                    player.IsInVehicle()
                        ? (Entity)player.CurrentVehicle
                        : player;

                Function.Call(
                    Hash.REQUEST_COLLISION_AT_COORD,
                    _missionStartPosition.X,
                    _missionStartPosition.Y,
                    _missionStartPosition.Z
                );

                target.Position =
                    _missionStartPosition;

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

                ScheduleRouteGeneration(
                    ROUTE_GENERATION_STREAM_DELAY_MS
                );

                ShowHudNotification(
                    _missionGoToMountainPosEnabled
                        ? "~g~[MISSION] GO TO MOUNTAIN ON ~w~START -> POS -> FINISH"
                        : "~g~[MISSION] GO TO MOUNTAIN ON ~w~START -> FINISH"
                );
            }
            else
            {
                // Balik ke sistem global lama.
                // Posisi player saat ini menjadi START seperti behavior normal.
                _startPosition =
                    player.Position;

                ScheduleRouteGeneration(
                    ROUTE_GENERATION_RETRY_MS
                );

                ShowHudNotification(
                    "~y~[MISSION] GO TO MOUNTAIN OFF ~w~Global random route aktif lagi."
                );
            }
        }

        private void LoadPlayerAndHealthBarConfig()
        {
            string iniPath = "scripts/MoonModConfig.ini";
            if (!File.Exists(iniPath))
                return;

            try
            {
                string[] lines = File.ReadAllLines(iniPath);
                string currentSection = string.Empty;

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) ||
                        trimmed.StartsWith(";") ||
                        trimmed.StartsWith("#"))
                    {
                        continue;
                    }

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection =
                            trimmed.Substring(1, trimmed.Length - 2)
                                   .Trim()
                                   .ToUpperInvariant();
                        continue;
                    }

                    if (!trimmed.Contains("="))
                        continue;

                    string[] parts = trimmed.Split(new char[] { '=' }, 2);
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (currentSection != "PLAYER")
                        continue;

                    if (key.Equals("MaxHealth", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(value, out int maxHealth))
                    {
                        _playerMaxHealth = maxHealth;
                    }




                    else if (key.Equals("HealthRegenAmount", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(value, out int healthRegenAmount))
                    {
                        _healthRegenAmount = healthRegenAmount;
                    }
                    else if (key.Equals("HealthRegenDelay", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(value, out int healthRegenDelay))
                    {
                        _healthRegenDelay = healthRegenDelay;
                    }
                    else if (key.Equals("HealthRegenInterval", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(value, out int healthRegenInterval))
                    {
                        _healthRegenInterval = Math.Max(1, healthRegenInterval);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "PLAYER/HUD CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[PLAYER/HUD CONFIG ERROR]~w~ " +
                    ex.Message
                );
            }
        }

        private void WriteDiagnosticLog(
            string category,
            string details)
        {
            try
            {
                string scriptsDirectory =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "scripts"
                    );

                Directory.CreateDirectory(
                    scriptsDirectory
                );

                string logPath =
                    Path.Combine(
                        scriptsDirectory,
                        "MoonMod.log"
                    );

                string safeCategory =
                    string.IsNullOrWhiteSpace(category)
                        ? "ERROR"
                        : category.Trim();

                string safeDetails =
                    details ?? string.Empty;

                string logEntry =
                    "[" +
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss.fff",
                        CultureInfo.InvariantCulture
                    ) +
                    "] [" +
                    safeCategory +
                    "]" +
                    Environment.NewLine +
                    safeDetails +
                    Environment.NewLine +
                    "============================================================" +
                    Environment.NewLine;

                lock (_errorLogLock)
                {
                    File.AppendAllText(
                        logPath,
                        logEntry,
                        Encoding.UTF8
                    );
                }
            }
            catch
            {
                // Logger tidak boleh membuat script ikut crash.
            }
        }

        private void LogError(
            string category,
            Exception ex)
        {
            if (ex == null)
            {
                WriteDiagnosticLog(
                    category,
                    "Unknown exception."
                );
                return;
            }

            WriteDiagnosticLog(
                category,
                ex.ToString()
            );
        }

        private void LoadNpcConfigFromIni()
        {
            Dictionary<string, CustomNpcConfig> loadedDatabase =
                new Dictionary<string, CustomNpcConfig>(
                    StringComparer.OrdinalIgnoreCase
                );


            string iniPath =
                "scripts/Entity.ini";

            if (!File.Exists(iniPath))
                return;

            ScriptSettings config =
                ScriptSettings.Load(iniPath);

            // Jangan berhenti hanya karena satu nomor section kosong.
            // Ini membuat Npc1, Npc2, Npc4 tetap bisa dibaca walaupun Npc3
            // sengaja dihapus. Batas 200 mengikuti batas entity config.
            for (int index = 1; index <= 200; index++)
            {
                string section =
                    $"Npc{index}";

                string prefix =
                    $"Npc{index}";

                string webhook =
                    config.GetValue<string>(
                        section,
                        $"{prefix}WebhookName",
                        null
                    );

                if (string.IsNullOrWhiteSpace(webhook))
                    continue;

                CustomNpcConfig npc =
                    new CustomNpcConfig
                    {
                        // =====================================================
                        // BASIC
                        // =====================================================
                        WebhookName =
                            webhook.Trim().ToLowerInvariant(),

                        DefaultName =
                            config.GetValue<string>(
                                section,
                                $"{prefix}DefaultName",
                                "Enemy"
                            ),

                        // =====================================================
                        // =====================================================
                        Health =
                            config.GetValue<int>(
                                section,
                                $"{prefix}Health",
                                200
                            ),


                        // =====================================================
                        // WEAPON
                        // =====================================================
                        Accuracy =
                            config.GetValue<int>(
                                section,
                                $"{prefix}Accuracy",
                                100
                            ),

                        WeaponNoReload =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}WeaponNoReload",
                                false
                            ),

                        ShootRate =
                            config.GetValue<int>(
                                section,
                                $"{prefix}ShootRate",
                                1000
                            ),

                        // =====================================================
                        // DAMAGE / PROTECTION
                        // =====================================================
                        OnlyDamagedByPlayer =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}OnlyDamagedByPlayer",
                                false
                            ),

                        BulletProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}BulletProof",
                                false
                            ),

                        FireProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}FireProof",
                                false
                            ),

                        ExplosionProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ExplosionProof",
                                false
                            ),

                        // DEFAULT TRUE mengikuti behavior Enemy lama
                        CollisionProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CollisionProof",
                                true
                            ),

                        MeleeProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}MeleeProof",
                                false
                            ),

                        // =====================================================
                        // CRITICAL / RAGDOLL
                        // =====================================================
                        CanSufferCriticalHits =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CanSufferCriticalHits",
                                false
                            ),

                        CanRagdoll =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CanRagdoll",
                                true
                            ),

                        RagdollFromPlayerImpact =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}RagdollFromPlayerImpact",
                                false
                            ),

                        // =====================================================
                        // COMBAT AI
                        // =====================================================
                        CombatAbility =
                            config.GetValue<int>(
                                section,
                                $"{prefix}CombatAbility",
                                2
                            ),

                        CombatRange =
                            config.GetValue<int>(
                                section,
                                $"{prefix}CombatRange",
                                1
                            ),

                        CombatMovement =
                            config.GetValue<int>(
                                section,
                                $"{prefix}CombatMovement",
                                2
                            ),

                        SeeingRange =
                            config.GetValue<float>(
                                section,
                                $"{prefix}SeeingRange",
                                0.0f
                            ),

                        HearingRange =
                            config.GetValue<float>(
                                section,
                                $"{prefix}HearingRange",
                                0.0f
                            ),

                        AttackPlayer =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}AttackPlayer",
                                true
                            ),

                        CanFlee =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CanFlee",
                                false
                            ),

                        // =====================================================
                        // SPAWN / RESPAWN
                        // =====================================================
                        SpawnDistance =
                            config.GetValue<float>(
                                section,
                                $"{prefix}SpawnDistance",
                                5.0f
                            ),

                        RespawnDistance =
                            config.GetValue<float>(
                                section,
                                $"{prefix}RespawnDistance",
                                120.0f
                            ),
                        // =====================================================
                        // UI
                        // =====================================================
                        ShowName =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ShowName",
                                true
                            ),

                        ShowHealthBar =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ShowHealthBar",
                                true
                            ),

                        // =====================================================
                        // BLIP
                        // =====================================================
                        UseBlip =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}UseBlip",
                                false
                            ),

                        BlipColorId =
                            config.GetValue<int>(
                                section,
                                $"{prefix}BlipColorId",
                                1
                            ),

                        BlipScale =
                            config.GetValue<float>(
                                section,
                                $"{prefix}BlipScale",
                                1.0f
                            ),

                        // =====================================================
                        // VEHICLE
                        // =====================================================
                        VehicleModel =
                            config.GetValue<string>(
                                section,
                                $"{prefix}VehicleModel",
                                null
                            ),

                        ModelDriver =
                            config.GetValue<string>(
                                section,
                                $"{prefix}ModelDriver",
                                null
                            ),

                        ModelPassenger =
                            config.GetValue<string>(
                                section,
                                $"{prefix}ModelPassenger",
                                null
                            ),

                        ModelRightRear =
                            config.GetValue<string>(
                                section,
                                $"{prefix}ModelRightRear",
                                null
                            ),

                        ModelLeftRear =
                            config.GetValue<string>(
                                section,
                                $"{prefix}ModelLeftRear",
                                null
                            )
                    };

                // =========================================================
                // MODEL1 / MODEL2 / MODEL3...
                // + DISPLAY NAME MODEL
                // =========================================================
                int mIndex = 1;

                while (true)
                {
                    string modelVal =
                        config.GetValue<string>(
                            section,
                            $"{prefix}Model{mIndex}",
                            null
                        );

                    if (string.IsNullOrEmpty(modelVal))
                        break;

                    string displayName =
                        config.GetValue<string>(
                            section,
                            $"{prefix}DefaultNameModel{mIndex}",
                            null
                        );

                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName =
                            npc.DefaultName;
                    }

                    npc.PedModels.Add(
                        modelVal
                    );

                    npc.PedModelDisplayNames.Add(
                        displayName
                    );

                    mIndex++;
                }

                // =========================================================
                // FORMAT LAMA
                // Npc1Model=xxxx
                // =========================================================
                if (npc.PedModels.Count == 0)
                {
                    string singleModel =
                        config.GetValue<string>(
                            section,
                            $"{prefix}Model",
                            null
                        );

                    if (!string.IsNullOrEmpty(singleModel))
                    {
                        npc.PedModels.Add(
                            singleModel
                        );

                        npc.PedModelDisplayNames.Add(
                            npc.DefaultName
                        );
                    }
                }

                // =========================================================
                // WEAPON
                //
                // AMMO TIDAK DIBACA DARI INI.
                // =========================================================
                npc.Weapon =
                    ParseWeaponHash(
                        config.GetValue<string>(
                            section,
                            $"{prefix}Weapon",
                            "WEAPON_PISTOL"
                        )
                    );
                loadedDatabase[
                    webhook.Trim().ToLowerInvariant()
                ] = npc;
            }


            // Atomic reference swap: webhook thread selalu melihat snapshot
            // lama yang lengkap atau snapshot baru yang lengkap.
            _npcDatabase = loadedDatabase;
        }

        private void LoadBodyguardConfigFromIni()
        {
            Dictionary<string, CustomNpcConfig> loadedDatabase =
                new Dictionary<string, CustomNpcConfig>(
                    StringComparer.OrdinalIgnoreCase
                );

            string iniPath =
                "scripts/Entity.ini";

            if (!File.Exists(iniPath))
                return;

            ScriptSettings config =
                ScriptSettings.Load(iniPath);

            // Jangan berhenti hanya karena satu nomor section kosong.
            // Ini membuat Npc1, Npc2, Npc4 tetap bisa dibaca walaupun Npc3
            // sengaja dihapus. Batas 200 mengikuti batas entity config.
            for (int index = 1; index <= 200; index++)
            {
                string section =
                    $"Bodyguard{index}";

                string prefix =
                    $"Bodyguard{index}";

                string webhook =
                    config.GetValue<string>(
                        section,
                        $"{prefix}WebhookName",
                        null
                    );

                if (string.IsNullOrWhiteSpace(webhook))
                    continue;

                CustomNpcConfig bg =
                    new CustomNpcConfig
                    {
                        // =====================================================
                        // BASIC
                        // =====================================================
                        WebhookName =
                            webhook.Trim().ToLowerInvariant(),

                        DefaultName =
                            config.GetValue<string>(
                                section,
                                $"{prefix}DefaultName",
                                "Bodyguard"
                            ),

                        Health =
                            config.GetValue<int>(
                                section,
                                $"{prefix}Health",
                                500
                            ),


                        // =====================================================
                        // WEAPON
                        // =====================================================
                        Accuracy =
                            config.GetValue<int>(
                                section,
                                $"{prefix}Accuracy",
                                100
                            ),

                        WeaponAmmo =
                            config.GetValue<int>(
                                section,
                                $"{prefix}WeaponAmmo",
                                9999
                            ),

                        WeaponNoReload =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}WeaponNoReload",
                                true
                            ),

                        ShootRate =
                            config.GetValue<int>(
                                section,
                                $"{prefix}ShootRate",
                                1000
                            ),

                        // =====================================================
                        // DAMAGE / PROTECTION
                        // =====================================================
                        OnlyDamagedByPlayer =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}OnlyDamagedByPlayer",
                                false
                            ),

                        BulletProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}BulletProof",
                                false
                            ),

                        FireProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}FireProof",
                                false
                            ),

                        ExplosionProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ExplosionProof",
                                false
                            ),

                        CollisionProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CollisionProof",
                                false
                            ),

                        MeleeProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}MeleeProof",
                                false
                            ),

                        // =====================================================
                        // CRITICAL HIT / RAGDOLL
                        // =====================================================
                        CanSufferCriticalHits =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CanSufferCriticalHits",
                                false
                            ),

                        CanRagdoll =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CanRagdoll",
                                true
                            ),

                        RagdollFromPlayerImpact =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}RagdollFromPlayerImpact",
                                false
                            ),

                        // =====================================================
                        // COMBAT AI
                        // =====================================================
                        CombatAbility =
                            config.GetValue<int>(
                                section,
                                $"{prefix}CombatAbility",
                                2
                            ),

                        CombatRange =
                            config.GetValue<int>(
                                section,
                                $"{prefix}CombatRange",
                                2
                            ),

                        CombatMovement =
                            config.GetValue<int>(
                                section,
                                $"{prefix}CombatMovement",
                                2
                            ),

                        SeeingRange =
                            config.GetValue<float>(
                                section,
                                $"{prefix}SeeingRange",
                                0.0f
                            ),

                        HearingRange =
                            config.GetValue<float>(
                                section,
                                $"{prefix}HearingRange",
                                0.0f
                            ),

                        TargetSearchRadius =
                            config.GetValue<float>(
                                section,
                                $"{prefix}TargetSearchRadius",
                                150.0f
                            ),

                        // =====================================================
                        // FOLLOW PLAYER
                        // =====================================================
                        FollowPlayer =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}FollowPlayer",
                                true
                            ),

                        NeverLeavePlayerGroup =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}NeverLeavePlayerGroup",
                                true
                            ),

                        // =====================================================
                        // SPAWN / RESPAWN
                        // =====================================================
                        SpawnDistance =
                            config.GetValue<float>(
                                section,
                                $"{prefix}SpawnDistance",
                                3.0f
                            ),

                        RespawnDistance =
                            config.GetValue<float>(
                                section,
                                $"{prefix}RespawnDistance",
                                120.0f
                            ),

                        // =====================================================
                        // UI
                        // =====================================================
                        ShowName =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ShowName",
                                true
                            ),

                        ShowHealthBar =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ShowHealthBar",
                                true
                            ),

                        // =====================================================
                        // BLIP
                        // =====================================================
                        UseBlip =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}UseBlip",
                                true
                            ),

                        BlipColorId =
                            config.GetValue<int>(
                                section,
                                $"{prefix}BlipColorId",
                                3
                            ),

                        BlipScale =
                            config.GetValue<float>(
                                section,
                                $"{prefix}BlipScale",
                                0.8f
                            ),

                        // Tetap pertahankan compatibility lama
                        VehicleModel =
                            config.GetValue<string>(
                                section,
                                $"{prefix}VehicleModel",
                                null
                            ),

                        ModelDriver =
                            config.GetValue<string>(
                                section,
                                $"{prefix}ModelDriver",
                                null
                            ),

                        ModelPassenger =
                            config.GetValue<string>(
                                section,
                                $"{prefix}ModelPassenger",
                                null
                            ),

                        ModelRightRear =
                            config.GetValue<string>(
                                section,
                                $"{prefix}ModelRightRear",
                                null
                            ),

                        ModelLeftRear =
                            config.GetValue<string>(
                                section,
                                $"{prefix}ModelLeftRear",
                                null
                            )
                    };

                // =========================================================
                // MODEL1, MODEL2, MODEL3...
                // =========================================================
                int mIndex = 1;

                while (true)
                {
                    string modelVal =
                        config.GetValue<string>(
                            section,
                            $"{prefix}Model{mIndex}",
                            null
                        );

                    if (string.IsNullOrEmpty(modelVal))
                        break;

                    bg.PedModels.Add(
                        modelVal
                    );

                    mIndex++;
                }

                // Format lama:
                // Bodyguard1Model=xxxx
                if (bg.PedModels.Count == 0)
                {
                    string singleModel =
                        config.GetValue<string>(
                            section,
                            $"{prefix}Model",
                            null
                        );

                    if (!string.IsNullOrEmpty(singleModel))
                    {
                        bg.PedModels.Add(
                            singleModel
                        );
                    }
                }

                bg.Weapon =
                    ParseWeaponHash(
                        config.GetValue<string>(
                            section,
                            $"{prefix}Weapon",
                            "WEAPON_PISTOL"
                        )
                    );
                loadedDatabase[
                    webhook.Trim().ToLowerInvariant()
                ] = bg;
            }


            // Atomic reference swap: webhook thread selalu melihat snapshot
            // lama yang lengkap atau snapshot baru yang lengkap.
            _bodyguardDatabase = loadedDatabase;
        }

        private void LoadAnimalConfigFromIni()
        {
            Dictionary<string, CustomNpcConfig> loadedDatabase =
                new Dictionary<string, CustomNpcConfig>(
                    StringComparer.OrdinalIgnoreCase
                );

            string iniPath =
                "scripts/Entity.ini";

            if (!File.Exists(iniPath))
                return;

            ScriptSettings config =
                ScriptSettings.Load(iniPath);

            // Jangan berhenti hanya karena satu nomor section kosong.
            // Ini membuat Npc1, Npc2, Npc4 tetap bisa dibaca walaupun Npc3
            // sengaja dihapus. Batas 200 mengikuti batas entity config.
            for (int index = 1; index <= 200; index++)
            {
                string section =
                    $"Animal{index}";

                string prefix =
                    $"Animal{index}";

                string webhook =
                    config.GetValue<string>(
                        section,
                        $"{prefix}WebhookName",
                        null
                    );

                if (string.IsNullOrWhiteSpace(webhook))
                    continue;

                CustomNpcConfig animal =
                    new CustomNpcConfig
                    {
                        // =====================================================
                        // BASIC
                        // =====================================================
                        WebhookName =
                            webhook.Trim().ToLowerInvariant(),

                        DefaultName =
                            config.GetValue<string>(
                                section,
                                $"{prefix}DefaultName",
                                "Wild Animal"
                            ),

                        // =====================================================
                        // =====================================================
                        Health =
                            config.GetValue<int>(
                                section,
                                $"{prefix}Health",
                                300
                            ),


                        // =====================================================
                        // DAMAGE / PROTECTION
                        // =====================================================
                        OnlyDamagedByPlayer =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}OnlyDamagedByPlayer",
                                false
                            ),

                        BulletProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}BulletProof",
                                false
                            ),

                        FireProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}FireProof",
                                false
                            ),

                        ExplosionProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ExplosionProof",
                                false
                            ),

                        CollisionProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CollisionProof",
                                false
                            ),

                        MeleeProof =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}MeleeProof",
                                false
                            ),

                        // =====================================================
                        // CRITICAL / RAGDOLL
                        // =====================================================
                        CanSufferCriticalHits =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CanSufferCriticalHits",
                                true
                            ),

                        CanRagdoll =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CanRagdoll",
                                true
                            ),

                        RagdollFromPlayerImpact =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}RagdollFromPlayerImpact",
                                true
                            ),

                        // =====================================================
                        // COMBAT
                        // =====================================================
                        CombatRange =
                            config.GetValue<int>(
                                section,
                                $"{prefix}CombatRange",
                                0
                            ),

                        CombatMovement =
                            config.GetValue<int>(
                                section,
                                $"{prefix}CombatMovement",
                                3
                            ),

                        SeeingRange =
                            config.GetValue<float>(
                                section,
                                $"{prefix}SeeingRange",
                                0.0f
                            ),

                        HearingRange =
                            config.GetValue<float>(
                                section,
                                $"{prefix}HearingRange",
                                0.0f
                            ),

                        AttackPlayer =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}AttackPlayer",
                                true
                            ),

                        CanFlee =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}CanFlee",
                                false
                            ),

                        MoveRate =
                            config.GetValue<float>(
                                section,
                                $"{prefix}MoveRate",
                                1.0f
                            ),

                        // =====================================================
                        // SPAWN / RESPAWN
                        // =====================================================
                        SpawnDistance =
                            config.GetValue<float>(
                                section,
                                $"{prefix}SpawnDistance",
                                3.0f
                            ),

                        RespawnDistance =
                            config.GetValue<float>(
                                section,
                                $"{prefix}RespawnDistance",
                                100.0f
                            ),

                        // =====================================================
                        // UI
                        // =====================================================
                        ShowName =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ShowName",
                                true
                            ),

                        ShowHealthBar =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}ShowHealthBar",
                                true
                            ),

                        // =====================================================
                        // BLIP
                        // =====================================================
                        UseBlip =
                            config.GetValue<bool>(
                                section,
                                $"{prefix}UseBlip",
                                true
                            ),

                        BlipColorId =
                            config.GetValue<int>(
                                section,
                                $"{prefix}BlipColorId",
                                75
                            ),

                        BlipScale =
                            config.GetValue<float>(
                                section,
                                $"{prefix}BlipScale",
                                0.1f
                            )
                    };

                // =========================================================
                // RANDOM MODEL + NAMA MASING-MASING
                // =========================================================
                int mIndex = 1;

                while (true)
                {
                    string modelVal =
                        config.GetValue<string>(
                            section,
                            $"{prefix}Model{mIndex}",
                            null
                        );

                    if (string.IsNullOrEmpty(modelVal))
                        break;

                    string displayName =
                        config.GetValue<string>(
                            section,
                            $"{prefix}DefaultNameModel{mIndex}",
                            null
                        );

                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName =
                            animal.DefaultName;
                    }

                    animal.PedModels.Add(
                        modelVal
                    );

                    animal.PedModelDisplayNames.Add(
                        displayName
                    );

                    mIndex++;
                }

                // =========================================================
                // FORMAT LAMA
                // Animal1Model=...
                // =========================================================
                if (animal.PedModels.Count == 0)
                {
                    string singleModel =
                        config.GetValue<string>(
                            section,
                            $"{prefix}Model",
                            null
                        );

                    if (!string.IsNullOrEmpty(singleModel))
                    {
                        animal.PedModels.Add(
                            singleModel
                        );

                        animal.PedModelDisplayNames.Add(
                            animal.DefaultName
                        );
                    }
                }
                loadedDatabase[
                    webhook.Trim().ToLowerInvariant()
                ] = animal;
            }


            // Atomic reference swap: webhook thread selalu melihat snapshot
            // lama yang lengkap atau snapshot baru yang lengkap.
            _animalDatabase = loadedDatabase;
        }

        private void LoadGiveVehicleConfigFromIni()
        {
            _giftVehicleModelNames.Clear();
            _giftVehicleDisplayNames.Clear();

            string iniPath =
                "scripts/Entity.ini";

            if (!File.Exists(iniPath))
                return;

            string currentSection =
                string.Empty;

            foreach (string line in File.ReadAllLines(iniPath))
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
                        ).Trim();

                    continue;
                }

                if (!currentSection.Equals(
                        "GiveVehicles",
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    !trimmed.Contains("="))
                {
                    continue;
                }

                string[] parts =
                    trimmed.Split(
                        new char[] { '=' },
                        2
                    );

                string key =
                    parts[0].Trim();

                string modelName =
                    parts[1].Trim();

                if (string.IsNullOrEmpty(modelName))
                    continue;

                // Hanya terima key Vehicle1, Vehicle2, Vehicle3, dst.
                // Key lain seperti Note= / Enabled= / Description= diabaikan
                // agar tidak pernah dianggap sebagai nama model kendaraan.
                const string vehicleKeyPrefix =
                    "Vehicle";

                if (!key.StartsWith(
                        vehicleKeyPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string vehicleNumberText =
                    key.Substring(
                        vehicleKeyPrefix.Length
                    );

                if (string.IsNullOrEmpty(vehicleNumberText) ||
                    !int.TryParse(
                        vehicleNumberText,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int vehicleNumber) ||
                    vehicleNumber < 1)
                {
                    continue;
                }

                // Nama yang tampil di UI diambil dari model kendaraan.
                string displayName =
                    modelName
                        .Replace("_", " ")
                        .ToUpperInvariant();

                _giftVehicleModelNames.Add(
                    modelName
                );

                _giftVehicleDisplayNames.Add(
                    displayName
                );
            }
        }

        private void LoadHitByVehicleConfigFromIni()
        {
            // Reload pool setiap kali webhook dipanggil supaya perubahan INI
            // langsung ikut terbaca tanpa restart script.
            _hitByVehicleModelNames.Clear();

            string iniPath =
                "scripts/MoonEffectConfig.ini";

            if (!File.Exists(iniPath))
            {
                _hitByVehicleModelNames.Add(
                    "stockade"
                );
                return;
            }

            string currentSection =
                string.Empty;

            foreach (
                string line in
                File.ReadAllLines(iniPath))
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

                if (!currentSection.Equals(
                        "HIT_BY_VEHICLE",
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    !trimmed.Contains("="))
                {
                    continue;
                }

                string[] parts =
                    trimmed.Split(
                        new char[] { '=' },
                        2
                    );

                string key =
                    parts[0].Trim();

                string value =
                    parts[1].Trim();

                if (string.IsNullOrEmpty(key) ||
                    string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (key.Equals(
                        "SpawnDistance",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out float spawnDistance))
                    {
                        _hitByVehicleSpawnDistance =
                            Math.Max(
                                8.0f,
                                Math.Min(
                                    30.0f,
                                    spawnDistance
                                )
                            );
                    }

                    continue;
                }

                if (key.Equals(
                        "Speed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out float speed))
                    {
                        _hitByVehicleSpeed =
                            Math.Max(
                                5.0f,
                                Math.Min(
                                    60.0f,
                                    speed
                                )
                            );
                    }

                    continue;
                }

                if (key.Equals(
                        "ForceLaunchSeconds",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out float forceLaunchSeconds) &&
                        forceLaunchSeconds > 0.0f)
                    {
                        forceLaunchSeconds =
                            Math.Max(
                                0.25f,
                                Math.Min(
                                    10.0f,
                                    forceLaunchSeconds
                                )
                            );

                        _hitByVehicleForceLaunchMs =
                            (int)(
                                forceLaunchSeconds *
                                1000.0f
                            );
                    }

                    continue;
                }

                if (key.Equals(
                        "hitby",
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Semua baris hitby= dimasukkan ke pool.
                    // Duplicate diabaikan supaya peluang tiap model tetap seimbang.
                    if (!_hitByVehicleModelNames.Any(
                            modelName =>
                                modelName.Equals(
                                    value,
                                    StringComparison.OrdinalIgnoreCase
                                )))
                    {
                        _hitByVehicleModelNames.Add(
                            value
                        );
                    }
                }
            }

            // Fallback lama jika section ada tetapi tidak berisi hitby valid.
            if (_hitByVehicleModelNames.Count == 0)
            {
                _hitByVehicleModelNames.Add(
                    "stockade"
                );
            }
        }

        private void LoadFallingVehiclesConfigFromIni()
        {
            _fallingVehicleHashes.Clear();

            string iniPath =
                "scripts/Entity.ini";

            if (!File.Exists(iniPath))
                return;

            string currentSection =
                string.Empty;

            foreach (string line in File.ReadAllLines(iniPath))
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
                        ).Trim();

                    continue;
                }

                if (!currentSection.Equals(
                        "FallingVehicles",
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    !trimmed.Contains("="))
                {
                    continue;
                }

                string[] parts =
                    trimmed.Split(
                        new char[] { '=' },
                        2
                    );

                string key =
                    parts[0].Trim();

                string modelName =
                    parts[1].Trim();

                if (string.IsNullOrEmpty(modelName))
                    continue;

                // Hanya terima key FallingVehicles1, FallingVehicles2, dst.
                // Key asing di section ini diabaikan agar tidak ikut masuk
                // ke pool random Falling Vehicles.
                const string fallingVehicleKeyPrefix =
                    "FallingVehicles";

                if (!key.StartsWith(
                        fallingVehicleKeyPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fallingVehicleNumberText =
                    key.Substring(
                        fallingVehicleKeyPrefix.Length
                    );

                if (string.IsNullOrEmpty(fallingVehicleNumberText) ||
                    !int.TryParse(
                        fallingVehicleNumberText,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int fallingVehicleNumber) ||
                    fallingVehicleNumber < 1)
                {
                    continue;
                }

                _fallingVehicleHashes.Add(
                    (uint)GenerateHashCompat(
                        modelName
                    )
                );
            }
        }

        private string GetDesignString(
            Dictionary<string, Dictionary<string, string>> design,
            string section,
            string key,
            string fallback)
        {
            if (design == null)
                return fallback;

            Dictionary<string, string> sectionData;

            if (!design.TryGetValue(
                    section,
                    out sectionData
                ))
            {
                return fallback;
            }

            string value;

            if (!sectionData.TryGetValue(
                    key,
                    out value
                ))
            {
                return fallback;
            }

            return
                string.IsNullOrEmpty(
                    value
                )
                    ? fallback
                    : value;
        }

        private int GetDesignInt(
            Dictionary<string, Dictionary<string, string>> design,
            string section,
            string key,
            int fallback)
        {
            string raw =
                GetDesignString(
                    design,
                    section,
                    key,
                    null
                );

            if (string.IsNullOrEmpty(
                    raw
                ))
            {
                return fallback;
            }

            int parsed;

            if (!int.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out parsed
                ))
            {
                return fallback;
            }

            return parsed;
        }

        private float GetDesignFloat(
            Dictionary<string, Dictionary<string, string>> design,
            string section,
            string key,
            float fallback)
        {
            string raw =
                GetDesignString(
                    design,
                    section,
                    key,
                    null
                );

            if (string.IsNullOrEmpty(
                    raw
                ))
            {
                return fallback;
            }

            float parsed;

            if (!float.TryParse(
                    raw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out parsed
                ))
            {
                return fallback;
            }

            return parsed;
        }

        private bool GetDesignBool(
            Dictionary<string, Dictionary<string, string>> design,
            string section,
            string key,
            bool fallback)
        {
            string raw =
                GetDesignString(
                    design,
                    section,
                    key,
                    null
                );

            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            bool parsedBool;
            if (bool.TryParse(raw, out parsedBool))
                return parsedBool;

            int parsedInt;
            if (int.TryParse(
                    raw,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out parsedInt
                ))
            {
                return parsedInt != 0;
            }

            return fallback;
        }

        private Vector3 ParseVector3(string input, Vector3 fallback)
        {
            if (string.IsNullOrWhiteSpace(input)) return fallback;
            string[] parts = input.Split(',');
            if (parts.Length == 3 &&
                float.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out float z))
            {
                return new Vector3(x, y, z);
            }
            return fallback;
        }

        private WeaponHash ParseWeaponHash(string rawWeapon)
        {
            rawWeapon = rawWeapon.Trim();
            string cleanHex = rawWeapon.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? rawWeapon.Substring(2) : rawWeapon;

            if (uint.TryParse(cleanHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsedHex))
                return (WeaponHash)(int)parsedHex;

            if (Enum.TryParse(rawWeapon, true, out WeaponHash parsedEnum))
                return parsedEnum;

            string lower = rawWeapon.ToLower();
            if (!lower.StartsWith("weapon_")) lower = "weapon_" + lower;
            return (WeaponHash)GenerateHashCompat(lower);
        }

        private void LoadGiveHealthConfigFromIni()
        {
            string iniPath =
                "scripts/MoonEffectConfig.ini";

            if (!File.Exists(
                    iniPath
                ))
            {
                return;
            }

            try
            {
                string currentSection =
                    string.Empty;

                foreach (
                    string line
                    in File.ReadAllLines(
                        iniPath
                    ))
                {
                    string trimmed =
                        line.Trim();

                    if (string.IsNullOrEmpty(
                            trimmed
                        ) ||
                        trimmed.StartsWith(
                            ";"
                        ) ||
                        trimmed.StartsWith(
                            "#"
                        ))
                    {
                        continue;
                    }

                    if (trimmed.StartsWith(
                            "["
                        ) &&
                        trimmed.EndsWith(
                            "]"
                        ))
                    {
                        currentSection =
                            trimmed.Substring(
                                1,
                                trimmed.Length - 2
                            )
                            .Trim();

                        continue;
                    }

                    if (!currentSection.Equals(
                            "GIVE_HEALTH",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        !trimmed.Contains(
                            "="
                        ))
                    {
                        continue;
                    }

                    string[] parts =
                        trimmed.Split(
                            new char[] { '=' },
                            2
                        );

                    string key =
                        parts[0].Trim();

                    string value =
                        parts[1].Trim();

                    if (key.Equals(
                            "HealthAmount",
                            StringComparison.OrdinalIgnoreCase
                        ) &&
                        int.TryParse(
                            value,
                            out int healthAmount
                        ))
                    {
                        _giveHealthAmount =
                            Math.Max(
                                0,
                                healthAmount
                            );
                    }

                }
            }
            catch (Exception ex)
            {
                LogError(
                    "GIVE HEALTH CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[GIVE HEALTH CONFIG ERROR]~w~ " +
                    ex.Message,
                    false
                );
            }
        }

        private void LoadUnderwaterConfigFromIni()
        {
            string iniPath =
                "scripts/MoonModConfig.ini";

            if (!File.Exists(iniPath))
                return;

            try
            {
                string currentSection =
                    string.Empty;

                foreach (
                    string line in
                    File.ReadAllLines(iniPath))
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

                    if (!currentSection.Equals(
                            "UNDERWATER",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        !trimmed.Contains("="))
                    {
                        continue;
                    }

                    string[] parts =
                        trimmed.Split(
                            new char[] { '=' },
                            2
                        );

                    string key =
                        parts[0].Trim();

                    string value =
                        parts[1].Trim();

                    if (key.Equals(
                            "Enabled",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        if (bool.TryParse(
                                value,
                                out bool enabled
                            ))
                        {
                            _underwaterSystemEnabled =
                                enabled;
                        }
                    }
                    else if (key.Equals(
                                 "UnderwaterSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int seconds
                            ) &&
                            seconds > 0)
                        {
                            _underwaterTriggerDurationMs =
                                Math.Min(
                                    seconds,
                                    24 * 60 * 60
                                ) *
                                1000;
                        }
                    }
                    else if (key.Equals(
                                 "TeleportWarningSeconds",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int seconds
                            ) &&
                            seconds > 0)
                        {
                            _underwaterTeleportWarningDurationMs =
                                Math.Min(
                                    seconds,
                                    60 * 60
                                ) *
                                1000;
                        }
                    }
                    else if (key.Equals(
                                 "SearchLandMaxDistance",
                                 StringComparison.OrdinalIgnoreCase
                             ))
                    {
                        if (float.TryParse(
                                value,
                                NumberStyles.Float,
                                CultureInfo.InvariantCulture,
                                out float distance
                            ) &&
                            distance > 0.0f)
                        {
                            _underwaterSearchLandMaxDistance =
                                Math.Max(
                                    10.0f,
                                    Math.Min(
                                        1000.0f,
                                        distance
                                    )
                                );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "UNDERWATER CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[UNDERWATER CONFIG ERROR]~w~ " +
                    ex.Message,
                    false
                );
            }
        }

        private bool TryParseGiftTimerSeconds(string text, out int totalSeconds)
        {
            totalSeconds = 0;
            string value = (text ?? string.Empty).Trim();
            int colon = value.IndexOf(':');
            if (colon >= 0)
            {
                int minutes, seconds;
                if (!int.TryParse(value.Substring(0, colon), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out minutes) ||
                    !int.TryParse(value.Substring(colon + 1), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out seconds) ||
                    minutes < 0 || seconds < 0 || seconds >= 60)
                    return false;
                long combined = (long)minutes * 60L + seconds;
                if (combined > int.MaxValue) return false;
                totalSeconds = (int)combined;
                return true;
            }
            return int.TryParse(value.TrimEnd('s', 'S'), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out totalSeconds) && totalSeconds >= 0;
        }

        private void LoadMoonModConfigFromIni()
        {
            const string path = "scripts/MoonModConfig.ini";
            if (!File.Exists(path)) return;

            // HUD preset ikut dibaca dari file global yang sama.
            LoadHudPresetSelectionFromIni();

            try
            {
                string section = string.Empty;

                // Dibangun sebagai snapshot baru supaya hot-reload tidak
                // meninggalkan weapon lama dari config sebelumnya.
                List<WeaponHash> parsedPlayerWeapons =
                    new List<WeaponHash>();

                bool playerWeaponsSectionFound =
                    false;

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();

                    if (string.IsNullOrEmpty(line) ||
                        line.StartsWith(";") ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") &&
                        line.EndsWith("]"))
                    {
                        section =
                            line.Substring(
                                1,
                                line.Length - 2
                            ).Trim();

                        if (section.Equals(
                                "PLAYER_WEAPONS",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            playerWeaponsSectionFound =
                                true;
                        }

                        continue;
                    }

                    int eq =
                        line.IndexOf('=');

                    if (eq <= 0)
                        continue;

                    string key =
                        line.Substring(0, eq).Trim();

                    string value =
                        line.Substring(eq + 1).Trim();

                    if (section.Equals(
                            "MOONMOD",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        if (key.Equals(
                                "WebhookPort",
                                StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(
                                value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int port))
                        {
                            _webhookPort =
                                Math.Max(
                                    1024,
                                    Math.Min(65535, port)
                                );
                        }
                    }
                    else if (section.Equals(
                                 "PLAYER_WEAPONS",
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        // Weapon1, Weapon2, Weapon3, dst.
                        // Nomor hanya untuk memudahkan pembacaan INI.
                        if (!key.StartsWith(
                                "Weapon",
                                StringComparison.OrdinalIgnoreCase) ||
                            string.IsNullOrWhiteSpace(value))
                        {
                            continue;
                        }

                        WeaponHash weapon =
                            ParseWeaponHash(value);

                        if (weapon == WeaponHash.Unarmed)
                            continue;

                        if (!parsedPlayerWeapons.Contains(weapon))
                        {
                            parsedPlayerWeapons.Add(weapon);
                        }
                    }
                    else if (section.Equals(
                                 "SCORE",
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        // Gameplay score rules tetap dikunci oleh script.
                        continue;
                    }
                }

                // Section ada tetapi tanpa WeaponN = mode tanpa senjata default.
                if (playerWeaponsSectionFound)
                {
                    _defaultPlayerWeapons.Clear();
                    _defaultPlayerWeapons.AddRange(
                        parsedPlayerWeapons
                    );
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "MOONMOD CONFIG ERROR",
                    ex
                );
            }
        }

    }
}
