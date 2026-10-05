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
        // =========================================================
        // STARTUP FLOW - MoonModConfig.ini
        // =========================================================
        // [STARTUP]
        // SkipProfileSelection=true/false
        // GameplayMode=full/gotomountain/survival
        //
        // full         = alur lama: PROFILE -> GAMEPLAY -> DIFFICULTY
        // gotomountain = GAMEPLAY menu dilewati, route default dibaca dari
        //                GoToMountainConfig.ini [MISSION] DefaultPosCount
        // survival     = GAMEPLAY menu dilewati, route default dibaca dari
        //                SurvivalConfig.ini [SURVIVAL] DefaultPosCount
        //
        // DefaultPosCount=0 berarti DIRECT / tanpa POS.
        // =========================================================
        private bool _startupSkipProfileSelection = false;
        private string _startupGameplayModeConfig = "full";
        private int _goToMountainDefaultPosCount = 0;
        private int _survivalDefaultPosCount = 10;

        private void LoadStartupFlowConfigFromIni()
        {
            const string path = "scripts/MoonModConfig.ini";

            _startupSkipProfileSelection = false;
            _startupGameplayModeConfig = "full";

            if (!File.Exists(path))
                return;

            try
            {
                string section = string.Empty;

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();

                    if (string.IsNullOrEmpty(line) ||
                        line.StartsWith(";") ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }

                    if (!section.Equals("STARTUP", StringComparison.OrdinalIgnoreCase))
                        continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (key.Equals("SkipProfileSelection", StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(value, out bool skipProfile))
                            _startupSkipProfileSelection = skipProfile;
                    }
                    else if (key.Equals("GameplayMode", StringComparison.OrdinalIgnoreCase))
                    {
                        string normalized =
                            (value ?? string.Empty)
                                .Trim()
                                .ToLowerInvariant()
                                .Replace("_", string.Empty)
                                .Replace("-", string.Empty)
                                .Replace(" ", string.Empty);

                        if (normalized == "gotomountain")
                            _startupGameplayModeConfig = "gotomountain";
                        else if (normalized == "survival")
                            _startupGameplayModeConfig = "survival";
                        else
                            _startupGameplayModeConfig = "full";
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("STARTUP FLOW CONFIG ERROR", ex);
                _startupSkipProfileSelection = false;
                _startupGameplayModeConfig = "full";
            }
        }

        private int ReadStartupDefaultPosCount(
            string path,
            string sectionName,
            int fallback)
        {
            if (!File.Exists(path))
                return fallback;

            try
            {
                string section = string.Empty;

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();

                    if (string.IsNullOrEmpty(line) ||
                        line.StartsWith(";") ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }

                    if (!section.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (!key.Equals("DefaultPosCount", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (int.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int parsed))
                    {
                        return Math.Max(0, Math.Min(MAX_ROUTE_CHECKPOINT_COUNT, parsed));
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("DEFAULT POS CONFIG ERROR", ex);
            }

            return fallback;
        }

        private int NormalizeConfiguredDefaultPosCount(
            int value,
            int fallback,
            List<int> options)
        {
            // 0 = DIRECT, intentionally valid even though it is not in [POS_OPTIONS].
            if (value <= 0)
                return 0;

            if (options != null && options.Count > 0)
            {
                if (options.Contains(value))
                    return value;

                if (fallback > 0 && options.Contains(fallback))
                    return fallback;

                return options[0];
            }

            return Math.Max(
                1,
                Math.Min(
                    MAX_ROUTE_CHECKPOINT_COUNT,
                    value
                )
            );
        }

        private void InitializeNoHostSessionForSkippedProfile()
        {
            // Skip profile = benar-benar session TANPA HOST.
            // Tidak menunjuk HOST1/HOST2/dll dan tidak ada statistik yang
            // ditulis kembali ke ProfileHost.ini.
            _selectedHostProfileIndex = -1;
            _startupPlayerName = string.Empty;
            _startupNameDraft = string.Empty;
            _startupNameDropdownOpen = false;

            // Statistik session tetap boleh berubah selama gameplay, tetapi
            // semuanya dimulai dari nol dan tidak mempunyai profile tujuan.
            _score = 0;
            _winCount = 0;
            _deathCount = 0;
            _enemyKillCount = 0;

            _profileHostDirty = false;
            _nextProfileHostSaveTime = 0;
        }

        private void ApplyConfiguredGameplayModeForStartup()
        {
            _selectedDifficultyMode = MoonDifficultyMode.None;
            _gameplaySelectionLocked = false;

            if (_startupGameplayModeConfig == "gotomountain")
            {
                _selectedGameplayMode = MoonGameplayMode.GoToMountain;
                _selectedSurvivalDirectFinish = false;

                int defaultPos = NormalizeConfiguredDefaultPosCount(
                    _goToMountainDefaultPosCount,
                    0,
                    _goToMountainPosOptions);

                if (defaultPos <= 0)
                {
                    _selectedGameplayPosCount = 0;
                    _selectedGoToMountainDirect = true;
                }
                else
                {
                    _selectedGameplayPosCount = defaultPos;
                    _selectedGoToMountainDirect = false;
                }

                return;
            }

            if (_startupGameplayModeConfig == "survival")
            {
                _selectedGameplayMode = MoonGameplayMode.Survival;
                _selectedGoToMountainDirect = false;

                int fallback =
                    _survivalPosOptions.Count > 0
                        ? _survivalPosOptions[0]
                        : 5;

                int defaultPos = NormalizeConfiguredDefaultPosCount(
                    _survivalDefaultPosCount,
                    fallback,
                    _survivalPosOptions);

                if (defaultPos <= 0)
                {
                    _selectedGameplayPosCount = 0;
                    _selectedSurvivalDirectFinish = true;
                }
                else
                {
                    _selectedGameplayPosCount = defaultPos;
                    _selectedSurvivalDirectFinish = false;
                    _survivalPosCount = defaultPos;
                }

                return;
            }

            // FULL = behavior lama. Tidak ada mode/route yang dipilih otomatis.
            _startupGameplayModeConfig = "full";
            _selectedGameplayMode = MoonGameplayMode.None;
            _selectedGameplayPosCount = 0;
            _selectedGoToMountainDirect = false;
            _selectedSurvivalDirectFinish = false;
        }

        private void InitializeStartupFlowFromConfig()
        {
            ApplyConfiguredGameplayModeForStartup();

            if (_startupSkipProfileSelection)
            {
                // Skip profile = session tanpa host dan tanpa persistence profile.
                InitializeNoHostSessionForSkippedProfile();

                if (_startupGameplayModeConfig == "full")
                {
                    // FULL tetap memakai menu gameplay + difficulty.
                    _startupUiStage =
                        StartupUiStage.Gameplay;
                }
                else
                {
                    // GameplayMode=gotomountain/survival berarti mode + route
                    // sudah ditentukan dari config. Tidak perlu Gameplay/DIFFICULTY UI.
                    //
                    // Difficulty dibiarkan None agar RandomCheckpoint/RandomChaos
                    // mengikuti nilai config mode/mission, bukan preset EASY/NORMAL/HARD.
                    _selectedDifficultyMode =
                        MoonDifficultyMode.None;

                    _gameplaySelectionLocked =
                        true;

                    _startupNameConfirmed =
                        true;
                }
            }
            else
            {
                // Profile tetap dipilih. Untuk fixed gameplay mode,
                // tombol profile akan berubah menjadi START.
                _startupUiStage =
                    StartupUiStage.Profile;
            }
        }

        private void AdvanceStartupAfterProfileSelection()
        {
            if (_startupGameplayModeConfig == "full")
            {
                _startupUiStage =
                    StartupUiStage.Gameplay;

                return;
            }

            // Fixed mode: PROFILE -> START -> GAMEPLAY.
            // Tidak ada layar Easy / Normal / Hard.
            _selectedDifficultyMode =
                MoonDifficultyMode.None;

            _gameplaySelectionLocked =
                true;

            _startupNameConfirmed =
                true;
        }

        private bool IsFixedGameplayModeFromConfig()
        {
            return
                _startupGameplayModeConfig == "gotomountain" ||
                _startupGameplayModeConfig == "survival";
        }

        // =========================================================
        // STARTUP POS OPTIONS - PER GAMEPLAY MODE
        // Dibaca dari [POS_OPTIONS] Options=... pada config masing-masing.
        // Maksimal 4 pilihan agar layout startup tetap rapi.
        // =========================================================
        private readonly List<int> _goToMountainPosOptions =
            new List<int> { 5, 10, 15, 20 };

        private readonly List<int> _survivalPosOptions =
            new List<int> { 5, 10, 15, 20 };

        private void LoadStartupPosOptionsFromIni()
        {
            LoadPosOptionsFromIniFile(
                "scripts/GoToMountainConfig.ini",
                _goToMountainPosOptions);

            LoadPosOptionsFromIniFile(
                "scripts/SurvivalConfig.ini",
                _survivalPosOptions);

            _goToMountainDefaultPosCount =
                NormalizeConfiguredDefaultPosCount(
                    ReadStartupDefaultPosCount(
                        "scripts/GoToMountainConfig.ini",
                        "MISSION",
                        0),
                    0,
                    _goToMountainPosOptions);

            int survivalFallback =
                _survivalPosOptions.Count > 0
                    ? _survivalPosOptions[0]
                    : 5;

            _survivalDefaultPosCount =
                NormalizeConfiguredDefaultPosCount(
                    ReadStartupDefaultPosCount(
                        "scripts/SurvivalConfig.ini",
                        "SURVIVAL",
                        10),
                    survivalFallback,
                    _survivalPosOptions);

            if (_survivalDefaultPosCount > 0)
            {
                _survivalPosCount = _survivalDefaultPosCount;
            }
            else
            {
                _survivalPosCount = NormalizeSurvivalPosCount(
                    _survivalPosCount,
                    survivalFallback);
            }
        }

        private void LoadPosOptionsFromIniFile(
            string path,
            List<int> destination)
        {
            if (destination == null || !File.Exists(path))
                return;

            try
            {
                string section = string.Empty;
                string rawOptions = null;

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (string.IsNullOrEmpty(line) ||
                        line.StartsWith(";") ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }

                    if (!section.Equals("POS_OPTIONS", StringComparison.OrdinalIgnoreCase))
                        continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (key.Equals("Options", StringComparison.OrdinalIgnoreCase))
                    {
                        rawOptions = value;
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(rawOptions))
                    return;

                List<int> parsed = new List<int>();
                string[] parts = rawOptions.Split(',');

                foreach (string part in parts)
                {
                    if (!int.TryParse(part.Trim(), out int count))
                        continue;

                    count = Math.Max(1, Math.Min(MAX_ROUTE_CHECKPOINT_COUNT, count));
                    if (!parsed.Contains(count))
                        parsed.Add(count);

                    if (parsed.Count >= 4)
                        break;
                }

                if (parsed.Count == 0)
                    return;

                destination.Clear();
                destination.AddRange(parsed);
            }
            catch (Exception ex)
            {
                LogError("POS OPTIONS CONFIG ERROR", ex);
            }
        }

        private List<int> GetPosOptionsForGameplayMode(MoonGameplayMode mode)
        {
            return mode == MoonGameplayMode.Survival
                ? _survivalPosOptions
                : _goToMountainPosOptions;
        }

        private int NormalizePosCountAgainstOptions(
            int value,
            int fallback,
            List<int> options)
        {
            if (options != null && options.Count > 0)
            {
                if (options.Contains(value))
                    return value;
                if (options.Contains(fallback))
                    return fallback;
                return options[0];
            }

            return Math.Max(1, Math.Min(MAX_ROUTE_CHECKPOINT_COUNT, fallback));
        }

        private int NormalizeSurvivalPosCount(int value, int fallback)
        {
            return NormalizePosCountAgainstOptions(value, fallback, _survivalPosOptions);
        }

        private int NormalizeGoToMountainPosCount(int value, int fallback)
        {
            return NormalizePosCountAgainstOptions(value, fallback, _goToMountainPosOptions);
        }

        private int NormalizeSelectedGameplayPosCount(int value)
        {
            List<int> options = GetPosOptionsForGameplayMode(_selectedGameplayMode);
            int fallback = options != null && options.Count > 0 ? options[0] : 5;
            return NormalizePosCountAgainstOptions(value, fallback, options);
        }

        private List<Tuple<string, int, bool>> BuildStartupGameplayRouteOptions()
        {
            List<Tuple<string, int, bool>> options =
                new List<Tuple<string, int, bool>>();

            options.Add(Tuple.Create(
                _selectedGameplayMode == MoonGameplayMode.GoToMountain
                    ? "DIRECT TO MOUNTAIN"
                    : "DIRECT FINISH LINE",
                0,
                true));

            foreach (int count in GetPosOptionsForGameplayMode(_selectedGameplayMode))
            {
                options.Add(Tuple.Create(count + " POS", count, false));
            }

            return options;
        }

        private int GetSurvivalPosOptionIndex(int count)
        {
            int normalized = NormalizeSurvivalPosCount(
                count,
                _survivalPosOptions.Count > 0 ? _survivalPosOptions[0] : 5);

            int index = _survivalPosOptions.IndexOf(normalized);
            if (index < 0)
                index = 0;
            return Math.Max(0, Math.Min(3, index));
        }

        private string GetActiveGameplayHudIniPath()
        {
            // Survival dan GoToMountain memakai satu layout gameplay bersama.
            return "scripts/GameplayHUD.ini";
        }

        private bool IsSelectedDirectRouteMode()
        {
            if (_selectedGameplayMode == MoonGameplayMode.GoToMountain)
                return _selectedGoToMountainDirect;

            if (_selectedGameplayMode == MoonGameplayMode.Survival)
                return _selectedSurvivalDirectFinish;

            return _missionGoToMountainEnabled && !_missionGoToMountainPosEnabled;
        }

        private bool DifficultyEnablesRandomCheckpoint()
        {
            return _selectedDifficultyMode == MoonDifficultyMode.Normal ||
                   _selectedDifficultyMode == MoonDifficultyMode.Hard;
        }

        private bool DifficultyEnablesRandomChaos()
        {
            return _selectedDifficultyMode == MoonDifficultyMode.Hard;
        }

        private void ApplySelectedDifficultySettingsToRuntimeFlags()
        {
            if (_selectedDifficultyMode == MoonDifficultyMode.None)
                return;

            _randomCheckpointEnabled = DifficultyEnablesRandomCheckpoint();
            _randomChaosEnabled = DifficultyEnablesRandomChaos();

            if (!_randomCheckpointEnabled)
            {
                CancelRandomCheckpointSequence(clearPending: true);
                ResetActiveRandomCheckpointEffects();
                ResetRandomCheckpointProgressTracking();
            }

            if (!_randomChaosEnabled)
            {
                CancelChaosMode();
                _isCountdownActive = false;
                _countdownRemainingMs = 0;
            }
        }

        private void ApplySelectedGameplaySettingsToLegacyFlags()
        {
            if (!_gameplaySelectionLocked && _selectedGameplayMode == MoonGameplayMode.None)
                return;

            if (_selectedGameplayMode == MoonGameplayMode.Survival)
            {
                _missionGoToMountainEnabled = false;
                _missionGoToMountainPosEnabled = !_selectedSurvivalDirectFinish;

                if (_selectedSurvivalDirectFinish)
                {
                    _routeCheckpointCount = 0;
                }
                else
                {
                    _survivalPosCount = NormalizeSurvivalPosCount(_selectedGameplayPosCount, _survivalPosCount);
                    _routeCheckpointCount = _survivalPosCount;
                }
            }
            else if (_selectedGameplayMode == MoonGameplayMode.GoToMountain)
            {
                _missionGoToMountainEnabled = true;
                _missionGoToMountainPosEnabled = !_selectedGoToMountainDirect;
                if (!_selectedGoToMountainDirect)
                {
                    _routeCheckpointCount = NormalizeGoToMountainPosCount(_selectedGameplayPosCount, _routeCheckpointCount);
                }
                else
                {
                    _routeCheckpointCount = 0;
                }
            }
        }

        private void LoadSurvivalConfigFromIni()
        {
            const string path = "scripts/SurvivalConfig.ini";
            if (!File.Exists(path)) return;

            try
            {
                string section = string.Empty;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }
                    if (!section.Equals("SURVIVAL", StringComparison.OrdinalIgnoreCase)) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (key.Equals("NoVehicle", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool noVehicle))
                        _survivalNoVehicle = noVehicle;
                    else if (key.Equals("RandomStart", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool randomStart))
                        _survivalRandomStart = randomStart;
                    else if (key.Equals("DefaultPosCount", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int posCount))
                        _survivalPosCount = NormalizeSurvivalPosCount(posCount, _survivalPosCount);
                    else if (key.Equals("DirectMinDistance", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float directMin))
                        _survivalDirectMinDistance = Math.Max(500.0f, directMin);
                    else if (key.Equals("DirectMaxDistance", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float directMax))
                        _survivalDirectMaxDistance = Math.Max(_survivalDirectMinDistance + 100.0f, directMax);
                    else if ((key.Equals("MinDistanceOption1", StringComparison.OrdinalIgnoreCase) || key.Equals("MinDistancePos5", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float d5))
                        _survivalMinDistance5 = Math.Max(150.0f, d5);
                    else if ((key.Equals("MaxDistanceOption1", StringComparison.OrdinalIgnoreCase) || key.Equals("MaxDistancePos5", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float d5Max))
                        _survivalMaxDistance5 = Math.Max(_survivalMinDistance5 + 50.0f, d5Max);
                    else if ((key.Equals("MinDistanceOption2", StringComparison.OrdinalIgnoreCase) || key.Equals("MinDistancePos10", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float d10))
                        _survivalMinDistance10 = Math.Max(150.0f, d10);
                    else if ((key.Equals("MaxDistanceOption2", StringComparison.OrdinalIgnoreCase) || key.Equals("MaxDistancePos10", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float d10Max))
                        _survivalMaxDistance10 = Math.Max(_survivalMinDistance10 + 50.0f, d10Max);
                    else if ((key.Equals("MinDistanceOption3", StringComparison.OrdinalIgnoreCase) || key.Equals("MinDistancePos15", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float d15))
                        _survivalMinDistance15 = Math.Max(120.0f, d15);
                    else if ((key.Equals("MaxDistanceOption3", StringComparison.OrdinalIgnoreCase) || key.Equals("MaxDistancePos15", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float d15Max))
                        _survivalMaxDistance15 = Math.Max(_survivalMinDistance15 + 50.0f, d15Max);
                    else if ((key.Equals("MinDistanceOption4", StringComparison.OrdinalIgnoreCase) || key.Equals("MinDistancePos20", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float d20))
                        _survivalMinDistance20 = Math.Max(100.0f, d20);
                    else if ((key.Equals("MaxDistanceOption4", StringComparison.OrdinalIgnoreCase) || key.Equals("MaxDistancePos20", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float d20Max))
                        _survivalMaxDistance20 = Math.Max(_survivalMinDistance20 + 50.0f, d20Max);
                    else if (key.Equals("RoutePointAttempts", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int attempts))
                        _survivalRoutePointAttempts = Math.Max(20, Math.Min(500, attempts));
                }
            }
            catch (Exception ex)
            {
                LogError("SURVIVAL CONFIG ERROR", ex);
            }
        }

        private float GetSurvivalMinimumLegDistance()
        {
            if (_selectedSurvivalDirectFinish)
                return _survivalDirectMinDistance;

            switch (GetSurvivalPosOptionIndex(_survivalPosCount))
            {
                case 0: return _survivalMinDistance5;
                case 1: return _survivalMinDistance10;
                case 2: return _survivalMinDistance15;
                default: return _survivalMinDistance20;
            }
        }

        private float GetSurvivalMaximumLegDistance()
        {
            if (_selectedSurvivalDirectFinish)
                return Math.Max(_survivalDirectMinDistance + 100.0f, _survivalDirectMaxDistance);

            switch (GetSurvivalPosOptionIndex(_survivalPosCount))
            {
                case 0: return Math.Max(_survivalMinDistance5 + 50.0f, _survivalMaxDistance5);
                case 1: return Math.Max(_survivalMinDistance10 + 50.0f, _survivalMaxDistance10);
                case 2: return Math.Max(_survivalMinDistance15 + 50.0f, _survivalMaxDistance15);
                default: return Math.Max(_survivalMinDistance20 + 50.0f, _survivalMaxDistance20);
            }
        }

        private Vector3 GetSurvivalRandomRoutePosition(Vector3 fromPos,
            List<Vector3> usedPoints, float minLegDistance, float maxLegDistance)
        {
            for (int attempt = 0; attempt < _survivalRoutePointAttempts; attempt++)
            {
                float randomX;
                float randomY;

                if (fromPos != Vector3.Zero && maxLegDistance > minLegDistance)
                {
                    // Sample inside an annulus around the previous POS.
                    // This is the key rule: more POS = a smaller annulus = closer checkpoints.
                    double angle = _random.NextDouble() * Math.PI * 2.0;
                    float radius = minLegDistance +
                        ((float)_random.NextDouble() * (maxLegDistance - minLegDistance));
                    randomX = fromPos.X + ((float)Math.Cos(angle) * radius);
                    randomY = fromPos.Y + ((float)Math.Sin(angle) * radius);

                    if (randomX < GLOBAL_ROUTE_MIN_X || randomX > GLOBAL_ROUTE_MAX_X ||
                        randomY < GLOBAL_ROUTE_MIN_Y || randomY > GLOBAL_ROUTE_MAX_Y)
                    {
                        continue;
                    }
                }
                else
                {
                    // Random START may still be anywhere in the allowed mainland area.
                    randomX = GLOBAL_ROUTE_MIN_X +
                        ((float)_random.NextDouble() * (GLOBAL_ROUTE_MAX_X - GLOBAL_ROUTE_MIN_X));
                    randomY = GLOBAL_ROUTE_MIN_Y +
                        ((float)_random.NextDouble() * (GLOBAL_ROUTE_MAX_Y - GLOBAL_ROUTE_MIN_Y));
                }

                Vector3 roadPosition = GetRoadRoutePosition(new Vector3(randomX, randomY, 500.0f));
                if (roadPosition == Vector3.Zero) continue;
                if (IsRandomRouteRestrictedArea(roadPosition)) continue;

                if (fromPos != Vector3.Zero)
                {
                    float legDistance = roadPosition.DistanceTo(fromPos);
                    if (legDistance < minLegDistance || legDistance > maxLegDistance) continue;
                }

                if (!IsRoutePointFarEnough(roadPosition, usedPoints,
                    Math.Max(120.0f, minLegDistance * 0.30f))) continue;
                return roadPosition;
            }
            return Vector3.Zero;
        }

        private bool GenerateSurvivalRoute()
        {
            int totalCheckpoints = _selectedSurvivalDirectFinish
                ? 0
                : NormalizeSurvivalPosCount(_survivalPosCount, _survivalPosOptions.Count > 0 ? _survivalPosOptions[0] : 5);
            float minLegDistance = GetSurvivalMinimumLegDistance();
            float maxLegDistance = GetSurvivalMaximumLegDistance();

            for (int routeAttempt = 0; routeAttempt < 8; routeAttempt++)
            {
                List<Vector3> checkpoints = new List<Vector3>();
                List<Vector3> used = new List<Vector3>();
                if (_startPosition != Vector3.Zero) used.Add(_startPosition);
                Vector3 current = _startPosition;
                bool valid = current != Vector3.Zero;

                for (int i = 0; valid && i < totalCheckpoints; i++)
                {
                    Vector3 next = GetSurvivalRandomRoutePosition(current, used, minLegDistance, maxLegDistance);
                    if (next == Vector3.Zero) { valid = false; break; }
                    checkpoints.Add(next);
                    used.Add(next);
                    current = next;
                }

                if (!valid) continue;

                Vector3 finish = GetSurvivalRandomRoutePosition(current, used, minLegDistance, maxLegDistance);
                if (finish == Vector3.Zero) continue;

                _activePosList.Clear();
                _activePosList.AddRange(checkpoints);
                _activeFinishPosition = finish;
                _currentPosIndex = 0;
                _isInsideCheckpoint = false;
                _checkpointTouchStartTime = 0;
                UpdateCheckpointBlip();
                return true;
            }

            _activePosList.Clear();
            _activeFinishPosition = Vector3.Zero;
            _currentPosIndex = 0;
            return false;
        }

        private bool PrepareSurvivalRandomStart(Ped player)
        {
            if (player == null || !player.Exists()) return false;

            List<Vector3> used = new List<Vector3>();
            if (_lastSurvivalStartPosition != Vector3.Zero)
                used.Add(_lastSurvivalStartPosition);

            Vector3 start = GetSurvivalRandomRoutePosition(Vector3.Zero, used, 0.0f, 0.0f);
            if (start == Vector3.Zero) return false;

            _startPosition = start;
            _lastSurvivalStartPosition = start;
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, start.X, start.Y, start.Z);

            if (player.IsInVehicle())
            {
                Vehicle vehicle = player.CurrentVehicle;
                if (vehicle != null && vehicle.Exists())
                {
                    Function.Call(Hash.TASK_LEAVE_VEHICLE, player.Handle, vehicle.Handle, 16);
                }
            }

            player.Position = start + new Vector3(0.0f, 0.0f, 0.8f);
            return true;
        }

        private void RestartSurvivalIncrementalAttempt()
        {
            _pendingRouteBuildCheckpoints.Clear();
            _pendingRouteBuildUsedPoints.Clear();
            _pendingRouteBuildReference = _startPosition;
            _pendingRouteBuildCheckpointIndex = 0;
            _pendingRouteBuildCandidateAttempts = 0;
            _pendingRouteBuildWholeAttempts++;

            if (_startPosition != Vector3.Zero)
                _pendingRouteBuildUsedPoints.Add(_startPosition);
        }

        private void InitializeSurvivalIncrementalRouteBuild()
        {
            _pendingRouteBuildMode = PendingRouteBuildMode.Survival;
            _pendingRouteBuildInitialized = true;
            _pendingRouteBuildTotalCheckpoints =
                _selectedSurvivalDirectFinish
                    ? 0
                    : NormalizeSurvivalPosCount(_survivalPosCount, _survivalPosOptions.Count > 0 ? _survivalPosOptions[0] : 5);
            _pendingRouteBuildMinLegDistance =
                GetSurvivalMinimumLegDistance();
            _pendingRouteBuildMaxLegDistance =
                GetSurvivalMaximumLegDistance();
            _pendingRouteBuildFinish = Vector3.Zero;
            _pendingRouteBuildWholeAttempts = 0;
            RestartSurvivalIncrementalAttempt();
        }

        private bool TrySampleSurvivalIncrementalPoint(
            out Vector3 candidate)
        {
            candidate = Vector3.Zero;

            float randomX;
            float randomY;

            if (_pendingRouteBuildReference != Vector3.Zero &&
                _pendingRouteBuildMaxLegDistance > _pendingRouteBuildMinLegDistance)
            {
                double angle = _random.NextDouble() * Math.PI * 2.0;
                float radius = _pendingRouteBuildMinLegDistance +
                    ((float)_random.NextDouble() *
                     (_pendingRouteBuildMaxLegDistance - _pendingRouteBuildMinLegDistance));

                randomX = _pendingRouteBuildReference.X + ((float)Math.Cos(angle) * radius);
                randomY = _pendingRouteBuildReference.Y + ((float)Math.Sin(angle) * radius);

                if (randomX < GLOBAL_ROUTE_MIN_X || randomX > GLOBAL_ROUTE_MAX_X ||
                    randomY < GLOBAL_ROUTE_MIN_Y || randomY > GLOBAL_ROUTE_MAX_Y)
                {
                    return false;
                }
            }
            else
            {
                randomX = GLOBAL_ROUTE_MIN_X +
                    ((float)_random.NextDouble() *
                     (GLOBAL_ROUTE_MAX_X - GLOBAL_ROUTE_MIN_X));
                randomY = GLOBAL_ROUTE_MIN_Y +
                    ((float)_random.NextDouble() *
                     (GLOBAL_ROUTE_MAX_Y - GLOBAL_ROUTE_MIN_Y));
            }

            Vector3 roadPosition = GetRoadRoutePosition(
                new Vector3(randomX, randomY, 500.0f));

            if (roadPosition == Vector3.Zero)
                return false;
            if (IsRandomRouteRestrictedArea(roadPosition))
                return false;

            if (_pendingRouteBuildReference != Vector3.Zero)
            {
                float legDistance = roadPosition.DistanceTo(_pendingRouteBuildReference);
                if (legDistance < _pendingRouteBuildMinLegDistance ||
                    legDistance > _pendingRouteBuildMaxLegDistance)
                {
                    return false;
                }
            }

            if (!IsRoutePointFarEnough(
                    roadPosition,
                    _pendingRouteBuildUsedPoints,
                    Math.Max(120.0f,
                        _pendingRouteBuildMinLegDistance * 0.30f)))
            {
                return false;
            }

            candidate = roadPosition;
            return true;
        }

        private bool ProcessSurvivalIncrementalRouteBuild(
            int currentTime)
        {
            if (_startPosition == Vector3.Zero)
            {
                _nextRouteGenerationAttemptTime =
                    currentTime + ROUTE_GENERATION_RETRY_MS;
                return false;
            }

            if (!_pendingRouteBuildInitialized ||
                _pendingRouteBuildMode != PendingRouteBuildMode.Survival)
            {
                InitializeSurvivalIncrementalRouteBuild();
            }

            for (int work = 0;
                 work < ROUTE_BUILD_CANDIDATES_PER_TICK;
                 work++)
            {
                _pendingRouteBuildCandidateAttempts++;

                if (TrySampleSurvivalIncrementalPoint(
                        out Vector3 candidate))
                {
                    _pendingRouteBuildCandidateAttempts = 0;

                    // Setelah semua POS didapat, kandidat berikutnya menjadi FINISH.
                    if (_pendingRouteBuildCheckpointIndex >=
                        _pendingRouteBuildTotalCheckpoints)
                    {
                        _pendingRouteBuildFinish = candidate;

                        _activePosList.Clear();
                        _activePosList.AddRange(_pendingRouteBuildCheckpoints);
                        _activeFinishPosition = _pendingRouteBuildFinish;
                        _currentPosIndex = 0;
                        _isInsideCheckpoint = false;
                        _checkpointTouchStartTime = 0;
                        UpdateCheckpointBlip();
                        return true;
                    }

                    _pendingRouteBuildCheckpoints.Add(candidate);
                    _pendingRouteBuildUsedPoints.Add(candidate);
                    _pendingRouteBuildReference = candidate;
                    _pendingRouteBuildCheckpointIndex++;
                    continue;
                }

                if (_pendingRouteBuildCandidateAttempts <
                    ROUTE_BUILD_MAX_POINT_ATTEMPTS)
                {
                    continue;
                }

                if (_pendingRouteBuildWholeAttempts >=
                    ROUTE_BUILD_MAX_WHOLE_ATTEMPTS)
                {
                    ResetIncrementalRouteBuildState();
                    _nextRouteGenerationAttemptTime =
                        currentTime + ROUTE_GENERATION_RETRY_MS;
                    return false;
                }

                RestartSurvivalIncrementalAttempt();
            }

            return false;
        }

        private void BeginNextSurvivalRound(Ped player)
        {
            _startPosition = Vector3.Zero;
            _activePosList.Clear();
            _activeFinishPosition = Vector3.Zero;
            _currentPosIndex = 0;
            ClearRouteBlipsOnly();

            if (!PrepareSurvivalRandomStart(player))
            {
                ScheduleRouteGeneration(ROUTE_GENERATION_RETRY_MS);
                return;
            }

            // Random START baru memerlukan streaming/collision warm-up.
            // Route dibangun incremental agar tidak membuat satu Tick berat.
            ScheduleRouteGeneration(ROUTE_GENERATION_STREAM_DELAY_MS);
            ShowHudNotification("~g~[SURVIVAL] ~w~New random START. Loading route...");
        }

        private void TriggerSurvivalNoVehicleWarning()
        {
            if (!_survivalNoVehicleWarningEnabled) return;

            int now = Game.GameTime;
            if (now < _survivalNoVehicleWarningNextTriggerTime) return;

            _survivalNoVehicleWarningUntilTime =
                now + Math.Max(250, _survivalNoVehicleWarningDurationMs);
            _survivalNoVehicleWarningNextTriggerTime =
                now + Math.Max(100, _survivalNoVehicleWarningCooldownMs);
        }

        private void DrawSurvivalNoVehicleWarning()
        {
            if (!_survivalNoVehicle ||
                !_survivalNoVehicleWarningEnabled ||
                _selectedGameplayMode != MoonGameplayMode.Survival ||
                IsNormalHudSuppressed() ||
                Game.GameTime >= _survivalNoVehicleWarningUntilTime)
            {
                return;
            }

            float x = _survivalNoVehicleWarningX;
            float y = _survivalNoVehicleWarningY;
            float width = Math.Max(0.05f, _survivalNoVehicleWarningWidth);
            float height = Math.Max(0.02f, _survivalNoVehicleWarningHeight);
            float border = Math.Max(0.0002f, _survivalNoVehicleWarningBorderSize);

            Function.Call(Hash.DRAW_RECT, x + 0.0025f, y + 0.0040f,
                width, height, 0, 0, 0, 120);
            Function.Call(Hash.DRAW_RECT, x, y, width, height,
                _survivalNoVehicleWarningBgR,
                _survivalNoVehicleWarningBgG,
                _survivalNoVehicleWarningBgB,
                _survivalNoVehicleWarningBgA);

            Function.Call(Hash.DRAW_RECT, x, y - (height * 0.5f), width, border,
                _survivalNoVehicleWarningBorderR, _survivalNoVehicleWarningBorderG,
                _survivalNoVehicleWarningBorderB, _survivalNoVehicleWarningBorderA);
            Function.Call(Hash.DRAW_RECT, x, y + (height * 0.5f), width, border,
                _survivalNoVehicleWarningBorderR, _survivalNoVehicleWarningBorderG,
                _survivalNoVehicleWarningBorderB, _survivalNoVehicleWarningBorderA);
            Function.Call(Hash.DRAW_RECT, x - (width * 0.5f), y, border, height,
                _survivalNoVehicleWarningBorderR, _survivalNoVehicleWarningBorderG,
                _survivalNoVehicleWarningBorderB, _survivalNoVehicleWarningBorderA);
            Function.Call(Hash.DRAW_RECT, x + (width * 0.5f), y, border, height,
                _survivalNoVehicleWarningBorderR, _survivalNoVehicleWarningBorderG,
                _survivalNoVehicleWarningBorderB, _survivalNoVehicleWarningBorderA);

            DrawBoldTextOnScreen(
                _survivalNoVehicleWarningText,
                x,
                y + _survivalNoVehicleWarningTextOffsetY,
                _survivalNoVehicleWarningTextScale,
                _survivalNoVehicleWarningTextR,
                _survivalNoVehicleWarningTextG,
                _survivalNoVehicleWarningTextB,
                _survivalNoVehicleWarningTextA,
                _survivalNoVehicleWarningTextFont
            );
        }

        private void EnforceSurvivalNoVehicle()
        {
            if (!_survivalNoVehicle) return;

            // Block vehicle entry every frame. Because ENTER is disabled, read it
            // with IS_DISABLED_CONTROL_JUST_PRESSED so the warning still triggers.
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 23, true);  // ENTER VEHICLE

            bool enterAttempted =
                Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 23);

            if (enterAttempted)
            {
                TriggerSurvivalNoVehicleWarning();
            }

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;
            if (!player.IsInVehicle()) return;

            // Safety fallback for gifts/external mods that put the player in a car.
            // EXIT is intentionally NOT disabled, so the player can never be trapped.
            TriggerSurvivalNoVehicleWarning();

            Vehicle vehicle = player.CurrentVehicle;
            if (vehicle != null && vehicle.Exists())
            {
                Function.Call(Hash.TASK_LEAVE_VEHICLE, player.Handle, vehicle.Handle, 16);
            }
        }

    }
}
