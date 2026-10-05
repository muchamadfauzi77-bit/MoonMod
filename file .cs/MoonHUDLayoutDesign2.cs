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

        // =========================================================================
        // HUD LAYOUT - DESIGN 2
        // =========================================================================
        // Konsep: COMPACT DASHBOARD / RIBBON.
        // Progress + Random Checkpoint menjadi satu ribbon.
        // Health, Score/Win/Death, Chaos, Teleport/Gatcha dan Gift memakai
        // bentuk card yang berbeda dari layout default.
        // =========================================================================

        private void DrawPlayerHealthBarLayoutDesign2()
        {
            if (!_hudHealthEnabled || IsNormalHudSuppressed()) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;

            int currentHp = Math.Max(0, _customHealth);
            int maxHp = Math.Max(1, _playerMaxHealth);
            float fraction = Math.Max(0.0f, Math.Min(1.0f, currentHp / (float)maxHp));

            float width = Math.Max(0.12f, _hpBarWidth);
            float height = Math.Max(0.042f, _hpBarHeight);
            float left = _hpBarX - width / 2.0f;
            float top = _hpBarY - height / 2.0f;

            Function.Call(Hash.DRAW_RECT, _hpBarX, _hpBarY, width, height,
                _hpBgR, _hpBgG, _hpBgB, _hpBgA);

            Function.Call(Hash.DRAW_RECT, left + 0.0032f, _hpBarY, 0.0032f,
                Math.Max(0.010f, height - 0.010f),
                _hpFillR, _hpFillG, _hpFillB, _hpFillA);

            DrawGiftOutline(_hpBarX, _hpBarY, width, height, _healthBorderSize,
                _healthBorderR, _healthBorderG, _healthBorderB, _healthBorderA);

            float pad = 0.010f;

            DrawReferenceProgressText(_healthPanelLabel, left + pad, top + 0.004f,
                Math.Max(0.34f, _hpTextScale * 0.72f), width * 0.36f,
                _hpTextR, _hpTextG, _hpTextB, _hpTextA, _hpTextFont, false);

            DrawReferenceProgressText(
                FormatHudText(_hpTextFormat, currentHp, maxHp),
                left + width - pad - width * 0.24f, top + 0.004f,
                Math.Max(0.40f, _hpTextScale * 0.80f), width * 0.25f,
                _hpTextR, _hpTextG, _hpTextB, _hpTextA, _hpTextFont, true);

            float trackWidth = width - pad * 2.0f;
            float trackHeight = Math.Max(0.003f, Math.Min(0.007f, _healthTrackHeight));
            float trackY = top + height - 0.011f;

            Function.Call(Hash.DRAW_RECT, _hpBarX, trackY, trackWidth, trackHeight,
                _progressTrackR, _progressTrackG, _progressTrackB, _progressTrackA);

            float fillWidth = trackWidth * fraction;
            if (fillWidth > 0.0f)
            {
                Function.Call(Hash.DRAW_RECT, left + pad + fillWidth / 2.0f,
                    trackY, fillWidth, trackHeight,
                    _hpFillR, _hpFillG, _hpFillB, _hpFillA);
            }
        }

        private void DrawScoreCounterLayoutDesign2()
        {
            float width = Math.Max(0.105f, _scoreMinWidth);
            float height = Math.Max(0.050f, _scoreHeight);
            float cx = _scorePosX + width / 2.0f;
            float cy = _scorePosY + height / 2.0f;

            int vr = _score > 0 ? _scorePositiveR : _score < 0 ? _scoreNegativeR : _scoreZeroR;
            int vg = _score > 0 ? _scorePositiveG : _score < 0 ? _scoreNegativeG : _scoreZeroG;
            int vb = _score > 0 ? _scorePositiveB : _score < 0 ? _scoreNegativeB : _scoreZeroB;
            int va = _score > 0 ? _scorePositiveA : _score < 0 ? _scoreNegativeA : _scoreZeroA;

            Function.Call(Hash.DRAW_RECT, cx, cy, width, height,
                _scoreBgR, _scoreBgG, _scoreBgB, _scoreBgA);

            DrawGiftOutline(cx, cy, width, height, 0.0008f,
                _progressPanelDividerR, _progressPanelDividerG,
                _progressPanelDividerB, _progressPanelDividerA);

            Function.Call(Hash.DRAW_RECT, _scorePosX + 0.0030f, cy,
                0.0030f, height - 0.010f, vr, vg, vb, va);

            DrawReferenceProgressText("SCORE", _scorePosX + 0.012f,
                _scorePosY + 0.003f, Math.Max(0.34f, _scoreTextScale * 0.56f),
                width * 0.52f, _scoreLabelR, _scoreLabelG, _scoreLabelB,
                _scoreLabelA, _scoreTextFont, false);

            DrawReferenceProgressText(_score.ToString(CultureInfo.InvariantCulture),
                _scorePosX + width - 0.040f, _scorePosY + 0.004f,
                Math.Max(0.52f, _scoreTextScale * 0.78f), 0.070f,
                vr, vg, vb, va, _scoreTextFont, true);

            string host = (_startupPlayerName ?? string.Empty).Trim();
            if (_progressHostNameUppercase) host = host.ToUpperInvariant();

            if (!string.IsNullOrEmpty(host))
            {
                DrawReferenceProgressText(host, _scorePosX + 0.012f,
                    _scorePosY + height * 0.49f,
                    Math.Max(0.28f, _scoreTextScale * 0.42f),
                    width - 0.024f, _progressHostTextR, _progressHostTextG,
                    _progressHostTextB, _progressHostTextA,
                    _progressPercentFont, false);
            }
        }

        private void DrawDesign2StatCard(
            float left, float top, float width, float height,
            string label, int value,
            int ar, int ag, int ab, int aa,
            int br, int bg, int bb, int ba,
            int font)
        {
            float cx = left + width / 2.0f;
            float cy = top + height / 2.0f;

            Function.Call(Hash.DRAW_RECT, cx, cy, width, height, br, bg, bb, ba);
            DrawGiftOutline(cx, cy, width, height, 0.0008f,
                _progressPanelDividerR, _progressPanelDividerG,
                _progressPanelDividerB, _progressPanelDividerA);

            Function.Call(Hash.DRAW_RECT, left + 0.0025f, cy, 0.0025f,
                Math.Max(0.008f, height - 0.009f), ar, ag, ab, aa);

            DrawReferenceProgressText(label, left + 0.010f, top + 0.004f,
                0.42f, width * 0.62f, 248, 250, 253, 255, font, false);

            DrawReferenceProgressText(value.ToString(CultureInfo.InvariantCulture),
                left + width - 0.034f, top + 0.003f, 0.56f, 0.060f,
                ar, ag, ab, aa, font, true);
        }

        private void DrawDeathCounterLayoutDesign2()
        {
            if (IsNormalHudSuppressed()) return;

            if (_scoreModeEnabled)
            {
                DrawScoreCounter();
                DrawProgressBar();
                return;
            }

            DrawDesign2StatCard(_winPosX, _winPosY, _winBgWidth, _winBgHeight,
                "WINS", _winCount, _winR, _winG, _winB, _winA,
                _winBgR, _winBgG, _winBgB, _winBgA, _winFont);

            DrawDesign2StatCard(_deathPosX, _deathPosY, _deathBgWidth, _deathBgHeight,
                "DEATHS", _deathCount, _deathR, _deathG, _deathB, _deathA,
                _deathBgR, _deathBgG, _deathBgB, _deathBgA, _deathFont);

            DrawProgressBar();
        }

        private void DrawRouteLoadingProgressPanelLayoutDesign2()
        {
            float width = Math.Max(0.24f, _progressPanelWidth);
            float height = Math.Max(0.055f, _progressPanelHeight);
            float left = _barX - width / 2.0f;

            Function.Call(Hash.DRAW_RECT, _barX, _barY, width, height,
                _progressPanelR, _progressPanelG, _progressPanelB, _progressPanelA);

            Function.Call(Hash.DRAW_RECT, left + 0.0030f, _barY, 0.0030f,
                height - 0.010f, _fillR, _fillG, _fillB, _fillA);

            DrawGiftOutline(_barX, _barY, width, height, 0.0008f,
                _progressPanelDividerR, _progressPanelDividerG,
                _progressPanelDividerB, _progressPanelDividerA);

            DrawReferenceProgressText("LOADING ROUTE", left + 0.014f,
                _barY - 0.020f, Math.Max(0.46f, _missionTextScale * 0.78f),
                width * 0.62f, _missionTextR, _missionTextG, _missionTextB,
                _missionTextA, _missionTextFont, false);
        }

        private void DrawProgressBarLayoutDesign2()
        {
            if (!_hudProgressEnabled) return;

            bool routeNeedsPos = !IsSelectedDirectRouteMode();

            if (_routeGenerationPending ||
                _activeFinishPosition == Vector3.Zero ||
                (routeNeedsPos && _activePosList.Count == 0))
            {
                DrawRouteLoadingProgressPanel();
                return;
            }

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;

            bool isGoingToFinish = _currentPosIndex >= _activePosList.Count;

            Vector3 target =
                (!isGoingToFinish && _activePosList.Count > _currentPosIndex)
                    ? _activePosList[_currentPosIndex]
                    : _activeFinishPosition;

            Vector3 legStart;
            if (isGoingToFinish)
                legStart = _activePosList.Count > 0 ? _activePosList[_activePosList.Count - 1] : _startPosition;
            else if (_currentPosIndex == 0)
                legStart = _startPosition;
            else
                legStart = _activePosList[_currentPosIndex - 1];

            float totalDistance = Math.Max(1.0f, legStart.DistanceTo(target));
            float currentDistance = player.Position.DistanceTo(target);
            float targetRadius = isGoingToFinish ? 5.0f : 4.0f;

            float progress =
                currentDistance <= targetRadius
                    ? 1.0f
                    : Math.Max(0.0f, Math.Min(0.99f,
                        (totalDistance - currentDistance) /
                        Math.Max(1.0f, totalDistance - targetRadius)));

            bool randomSequenceActive =
                _hudRandomCheckpointEnabled && _randomCheckpointSequenceActive;

            float width = Math.Max(0.30f, _progressPanelWidth);
            float height = Math.Max(0.060f, _progressPanelHeight);
            float left = _barX - width / 2.0f;
            float right = _barX + width / 2.0f;
            float top = _barY - height / 2.0f;

            int ar = _fillR, ag = _fillG, ab = _fillB, aa = _fillA;
            float shownProgress = progress;
            string missionText;
            string centerText;
            string rightText;

            if (randomSequenceActive)
            {
                missionText = _randomCheckpointTitle;
                int now = Game.GameTime;
                int elapsed = Math.Max(0, now - _randomCheckpointPhaseStartTime);

                if (_randomCheckpointPhase == 1)
                {
                    int totalMs = Math.Max(1, _randomCheckpointCountdownSeconds * 1000);
                    int remaining = Math.Max(1, _randomCheckpointCountdownSeconds - elapsed / 1000);
                    centerText = "EFFECT IN";
                    rightText = remaining.ToString(CultureInfo.InvariantCulture) + " SEC";
                    shownProgress = 1.0f - Math.Max(0.0f, Math.Min(1.0f, elapsed / (float)totalMs));
                    ar = _randomCheckpointCountdownBarR; ag = _randomCheckpointCountdownBarG;
                    ab = _randomCheckpointCountdownBarB; aa = _randomCheckpointCountdownBarA;
                }
                else if (_randomCheckpointPhase == 2)
                {
                    centerText = GetRandomCheckpointEffectDisplayName(_randomCheckpointCurrentEffectKey);
                    rightText = "ROLLING";
                    shownProgress = Math.Max(0.0f, Math.Min(1.0f,
                        elapsed / (float)Math.Max(1, _randomCheckpointGachaDurationMs)));
                    ar = _randomCheckpointGachaBarR; ag = _randomCheckpointGachaBarG;
                    ab = _randomCheckpointGachaBarB; aa = _randomCheckpointGachaBarA;
                }
                else
                {
                    centerText = GetRandomCheckpointEffectDisplayName(_randomCheckpointLockedEffectKey);
                    rightText = _randomCheckpointLockedText;
                    shownProgress = 1.0f;
                    ar = _randomCheckpointLockedBarR; ag = _randomCheckpointLockedBarG;
                    ab = _randomCheckpointLockedBarB; aa = _randomCheckpointLockedBarA;
                }
            }
            else
            {
                missionText =
                    _selectedGameplayMode == MoonGameplayMode.GoToMountain
                        ? "GO TO MOUNTAIN"
                        : _selectedGameplayMode == MoonGameplayMode.Survival
                            ? "SURVIVAL"
                            : "GO TO FINISH";

                centerText =
                    ((int)Math.Round(progress * 100.0f))
                    .ToString(CultureInfo.InvariantCulture) + "%";

                if (IsSelectedDirectRouteMode())
                {
                    rightText =
                        _selectedGameplayMode == MoonGameplayMode.Survival
                            ? _progressDirectSurvivalValueText
                            : _progressDirectGoToMountainValueText;
                }
                else if (isGoingToFinish || _activePosList.Count <= 0)
                {
                    rightText = "FINISH";
                }
                else
                {
                    rightText = "POS " +
                        Math.Min(_currentPosIndex + 1, _activePosList.Count)
                        .ToString(CultureInfo.InvariantCulture) +
                        "/" + _activePosList.Count.ToString(CultureInfo.InvariantCulture);
                }
            }

            Function.Call(Hash.DRAW_RECT, _barX, _barY, width, height,
                _progressPanelR, _progressPanelG, _progressPanelB, _progressPanelA);

            DrawGiftOutline(_barX, _barY, width, height, 0.0008f,
                _progressPanelDividerR, _progressPanelDividerG,
                _progressPanelDividerB, _progressPanelDividerA);

            Function.Call(Hash.DRAW_RECT, _barX, top + 0.0020f,
                width - 0.008f, 0.0030f, ar, ag, ab, aa);

            float leftDivider = left + width * 0.30f;
            float rightDivider = left + width * 0.76f;

            Function.Call(Hash.DRAW_RECT, leftDivider, _barY + 0.004f,
                0.0008f, height - 0.018f,
                _progressPanelDividerR, _progressPanelDividerG,
                _progressPanelDividerB, _progressPanelDividerA);

            Function.Call(Hash.DRAW_RECT, rightDivider, _barY + 0.004f,
                0.0008f, height - 0.018f,
                _progressPanelDividerR, _progressPanelDividerG,
                _progressPanelDividerB, _progressPanelDividerA);

            DrawReferenceProgressText(missionText, left + 0.010f, top + 0.012f,
                Math.Max(0.42f, _missionTextScale * 0.72f),
                leftDivider - left - 0.018f,
                _missionTextR, _missionTextG, _missionTextB, _missionTextA,
                _missionTextFont, false);

            if (!randomSequenceActive)
            {
                string host = (_startupPlayerName ?? string.Empty).Trim();
                if (_progressHostNameUppercase) host = host.ToUpperInvariant();
                if (!string.IsNullOrEmpty(host))
                {
                    DrawReferenceProgressText(host, left + 0.010f, top + 0.033f,
                        0.32f, leftDivider - left - 0.018f,
                        _progressHostTextR, _progressHostTextG, _progressHostTextB,
                        _progressHostTextA, _progressPercentFont, false);
                }
            }

            DrawReferenceProgressText(centerText,
                leftDivider + (rightDivider - leftDivider) / 2.0f,
                top + 0.014f,
                randomSequenceActive ? Math.Max(0.44f, _randomCheckpointGachaScale * 0.70f)
                                     : Math.Max(0.62f, _progressPercentScale * 0.70f),
                rightDivider - leftDivider - 0.018f,
                randomSequenceActive ? _randomCheckpointGachaR : _progressPercentR,
                randomSequenceActive ? _randomCheckpointGachaG : _progressPercentG,
                randomSequenceActive ? _randomCheckpointGachaB : _progressPercentB,
                255,
                randomSequenceActive ? _randomCheckpointGachaFont : _progressPercentFont,
                true);

            DrawReferenceProgressText(rightText,
                rightDivider + (right - rightDivider) / 2.0f,
                top + 0.016f, Math.Max(0.42f, _progressRightScale * 0.62f),
                right - rightDivider - 0.014f,
                _progressRightR, _progressRightG, _progressRightB, _progressRightA,
                _progressRightFont, true);

            float trackLeft = left + 0.010f;
            float trackWidth = width - 0.020f;
            float trackY = top + height - 0.009f;

            Function.Call(Hash.DRAW_RECT, trackLeft + trackWidth / 2.0f,
                trackY, trackWidth, 0.0050f,
                _progressTrackR, _progressTrackG, _progressTrackB, _progressTrackA);

            float fillWidth = trackWidth * Math.Max(0.0f, Math.Min(1.0f, shownProgress));
            if (fillWidth > 0.0f)
            {
                Function.Call(Hash.DRAW_RECT, trackLeft + fillWidth / 2.0f,
                    trackY, fillWidth, 0.0050f, ar, ag, ab, aa);
            }

            if (!randomSequenceActive)
                DrawRandomCheckpointMarkers(trackLeft, trackY, trackWidth);
        }

        private void DrawRandomCheckpointMarkersLayoutDesign2(
            float barLeftX, float trackY, float activeBarWidth)
        {
            if (!_randomCheckpointEnabled ||
                !_hudRandomCheckpointEnabled ||
                !IsSelectedDirectRouteMode() ||
                !_randomCheckpointProgressInitialized)
                return;

            for (int i = 0; i < _randomCheckpointThresholds.Count; i++)
            {
                if (_randomCheckpointTriggeredIndices.Contains(i)) continue;

                float x = barLeftX + activeBarWidth * _randomCheckpointThresholds[i];

                Function.Call(Hash.DRAW_RECT, x, trackY,
                    Math.Max(0.0012f, _randomCheckpointMarkerWidth), 0.013f,
                    _randomCheckpointMarkerR, _randomCheckpointMarkerG,
                    _randomCheckpointMarkerB, _randomCheckpointMarkerA);

                Function.Call(Hash.DRAW_RECT, x, trackY - 0.007f,
                    0.006f, 0.0025f,
                    _randomCheckpointMarkerR, _randomCheckpointMarkerG,
                    _randomCheckpointMarkerB, _randomCheckpointMarkerA);
            }
        }

        private void DrawRandomCheckpointUILayoutDesign2(int currentTime)
        {
            // Design2: Random Checkpoint selalu menyatu dengan progress ribbon.
        }

        private void DrawCountdownUILayoutDesign2(int currentTime)
        {
            if (!_randomChaosEnabled || !_hudApocalypseEnabled || IsNormalHudSuppressed()) return;

            bool rolling = _isChaosRouletteActive && !_chaosRouletteLocked;
            bool locked = _isChaosRouletteActive && _chaosRouletteLocked;
            bool active = _isChaosActive;
            bool queued = active &&
                ((_activeChaosKey == "blackhole" && _pendingBlackholeDurationMs > 0 && currentTime < _earthquakeEndTime) ||
                 (_activeChaosKey == "earthquake" && _pendingEarthquakeDurationMs > 0 && currentTime < _blackholeEndTime));

            string state = rolling ? "ROLLING" : locked ? "LOCKED" : queued ? "QUEUED" : active ? "ACTIVE" : "NEXT";
            string effect = active ? _activeChaosName : "RANDOM CHAOS";

            if (rolling && _randomChaosOptions != null && _randomChaosOptions.Count > 0)
            {
                int idx = Math.Max(0, Math.Min(_randomChaosOptions.Count - 1, _currentChaosRouletteIndex));
                effect = _randomChaosOptions[idx].DisplayName;
            }

            int shownMs;
            float fraction;

            if (rolling)
            {
                shownMs = Math.Max(0, _chaosRouletteDurationMs - (currentTime - _chaosRouletteStartTime));
                fraction = Math.Max(0f, Math.Min(1f, shownMs / (float)Math.Max(1, _chaosRouletteDurationMs)));
            }
            else if (active)
            {
                shownMs = Math.Max(0, _chaosRemainingMs);
                int total = DEFAULT_RANDOM_CHAOS_DURATION_MS;
                if (_randomChaosOptions != null && _activeChaosIndex >= 0 && _activeChaosIndex < _randomChaosOptions.Count)
                    total = Math.Max(1, _randomChaosOptions[_activeChaosIndex].DurationMs);
                fraction = Math.Max(0f, Math.Min(1f, shownMs / (float)Math.Max(1, total)));
            }
            else
            {
                shownMs = Math.Max(0, _countdownRemainingMs);
                fraction = Math.Max(0f, Math.Min(1f, shownMs / (float)Math.Max(1, _countdownDurationMs)));
            }

            int ar = locked || active ? _chaosUiLockedR : _apocalypseR;
            int ag = locked || active ? _chaosUiLockedG : _apocalypseG;
            int ab = locked || active ? _chaosUiLockedB : _apocalypseB;

            float left = _apocalypsePosX;
            float top = _apocalypsePosY;
            float width = Math.Max(0.16f, _apocalypseBgWidth);
            float height = Math.Max(0.078f, _apocalypseBgHeight);
            float cx = left + width / 2f;
            float cy = top + height / 2f;

            Function.Call(Hash.DRAW_RECT, cx, cy, width, height,
                _apocalypseBgR, _apocalypseBgG, _apocalypseBgB, _apocalypseBgA);

            DrawGiftOutline(cx, cy, width, height, 0.0008f,
                _progressPanelDividerR, _progressPanelDividerG,
                _progressPanelDividerB, _progressPanelDividerA);

            Function.Call(Hash.DRAW_RECT, cx, top + 0.0020f,
                width - 0.008f, 0.0030f, ar, ag, ab, 255);

            DrawReferenceProgressText(state, left + 0.010f, top + 0.010f,
                0.34f, width * 0.30f, ar, ag, ab, 255, _chaosUiStatusFont, false);

            DrawReferenceProgressText(effect, left + 0.010f, top + 0.031f,
                Math.Max(0.43f, _chaosUiResultScale * 0.76f),
                width * 0.65f, 248, 250, 253, 255, _chaosUiResultFont, false);

            DrawReferenceProgressText(FormatChaosHudClock(shownMs),
                left + width - 0.045f, top + 0.029f,
                0.56f, 0.080f, 248, 250, 253, 255, 4, true);

            DrawChaosHudTrack(left + 0.010f, top + height - 0.010f,
                width - 0.020f, fraction, ar, ag, ab);
        }

        private void DrawRandomGatchaUILayoutDesign2(int currentTime)
        {
            if (!_hudRandomGatchaEnabled || IsNormalHudSuppressed() ||
                (!_randomGatchaActive && !_randomGatchaLocked) ||
                _randomGatchaOptions == null || _randomGatchaOptions.Count == 0)
                return;

            int safeIndex = Math.Max(0, Math.Min(_randomGatchaOptions.Count - 1, _randomGatchaIndex));
            RandomGatchaOption option = _randomGatchaOptions[safeIndex];

            int ar = _randomGatchaLocked ? _randomGatchaAccentLockedR : _randomGatchaAccentRollingR;
            int ag = _randomGatchaLocked ? _randomGatchaAccentLockedG : _randomGatchaAccentRollingG;
            int ab = _randomGatchaLocked ? _randomGatchaAccentLockedB : _randomGatchaAccentRollingB;

            float width = Math.Max(0.30f, _randomGatchaUiWidth);
            float height = Math.Max(0.100f, _randomGatchaUiHeight);
            float left = _randomGatchaUiX - width / 2f;
            float top = _randomGatchaUiY - height / 2f;

            int remainingMs = _randomGatchaLocked
                ? Math.Max(0, _randomGatchaLockEndTime - currentTime)
                : Math.Max(0, _randomGatchaRollMs - (currentTime - _randomGatchaStartTime));

            Function.Call(Hash.DRAW_RECT, _randomGatchaUiX, _randomGatchaUiY,
                width, height, _progressPanelR, _progressPanelG, _progressPanelB, _progressPanelA);

            DrawGiftOutline(_randomGatchaUiX, _randomGatchaUiY, width, height, 0.0009f,
                _progressPanelDividerR, _progressPanelDividerG,
                _progressPanelDividerB, _progressPanelDividerA);

            Function.Call(Hash.DRAW_RECT, _randomGatchaUiX, top + 0.0020f,
                width - 0.008f, 0.0030f, ar, ag, ab, 255);

            float numberWidth = Math.Min(0.075f, width * 0.18f);

            Function.Call(Hash.DRAW_RECT, left + numberWidth / 2f, _randomGatchaUiY,
                numberWidth, height,
                Math.Max(0, _progressPanelR - 2), Math.Max(0, _progressPanelG - 1),
                _progressPanelB, _progressPanelA);

            DrawReferenceProgressText((safeIndex + 1).ToString(CultureInfo.InvariantCulture),
                left + numberWidth / 2f, top + 0.020f, 0.82f,
                numberWidth - 0.012f, ar, ag, ab, 255, 4, true);

            DrawReferenceProgressText(_randomGatchaLocked ? "LOCKED" : "GATCHA",
                left + numberWidth + 0.012f, top + 0.008f, 0.36f,
                width * 0.25f, ar, ag, ab, 255, 4, false);

            DrawReferenceProgressText(option.DisplayName,
                left + numberWidth + 0.012f, top + 0.035f, 0.70f,
                width - numberWidth - 0.110f,
                248, 250, 253, 255, 4, false);

            DrawReferenceProgressText(FormatChaosHudClock(remainingMs),
                left + width - 0.045f, top + 0.036f, 0.50f,
                0.078f, 248, 250, 253, 255, 4, true);
        }

        private void DrawTeleportGachaCardsUILayoutDesign2(
            TeleportLocationItem activeLocation, bool isLocked)
        {
            if (!_hudTeleportGachaEnabled || IsNormalHudSuppressed()) return;

            float width = Math.Max(0.30f, _teleportUiWidth);
            float height = Math.Max(0.095f, _teleportUiHeight);
            float left = _teleportUiX - width / 2f;
            float top = _teleportUiY - height / 2f;

            int ar = isLocked ? _teleportUiLockedR : _teleportUiSelectingR;
            int ag = isLocked ? _teleportUiLockedG : _teleportUiSelectingG;
            int ab = isLocked ? _teleportUiLockedB : _teleportUiSelectingB;

            string locationName = activeLocation?.DisplayName ?? "UNKNOWN";
            int currentTime = Game.GameTime;

            int remainingMs = isLocked
                ? Math.Max(0, _teleportGachaLockEndTime - currentTime)
                : Math.Max(0, _teleportGachaDuration - (currentTime - _teleportGachaStartTime));

            float progress = isLocked
                ? Math.Max(0f, Math.Min(1f, remainingMs / (float)Math.Max(1, _giftRandomTeleportLockDurationMs)))
                : Math.Max(0f, Math.Min(1f, remainingMs / (float)Math.Max(1, _teleportGachaDuration)));

            Function.Call(Hash.DRAW_RECT, _teleportUiX, _teleportUiY,
                width, height, _teleportUiBgR, _teleportUiBgG, _teleportUiBgB, _teleportUiBgA);

            DrawGiftOutline(_teleportUiX, _teleportUiY, width, height,
                Math.Max(0.0008f, _teleportUiBorderSize * 0.30f),
                ar, ag, ab, _teleportUiBorderA);

            float leftBlockWidth = Math.Min(0.110f, width * 0.25f);

            Function.Call(Hash.DRAW_RECT, left + leftBlockWidth / 2f,
                _teleportUiY, leftBlockWidth, height, ar, ag, ab, 65);

            DrawReferenceProgressText(isLocked ? "LOCKED" : "TELEPORT",
                left + 0.010f, top + 0.012f, 0.38f,
                leftBlockWidth - 0.018f, ar, ag, ab, 255,
                _teleportUiStatusFont, false);

            DrawReferenceProgressText(locationName,
                left + leftBlockWidth + 0.014f, top + 0.016f,
                Math.Max(0.56f, _teleportUiResultScale * 0.82f),
                width - leftBlockWidth - 0.110f,
                _teleportUiResultR, _teleportUiResultG, _teleportUiResultB,
                _teleportUiResultA, _teleportUiResultFont, false);

            DrawReferenceProgressText(FormatChaosHudClock(remainingMs),
                left + width - 0.047f, top + 0.017f,
                0.50f, 0.082f,
                _teleportUiResultR, _teleportUiResultG, _teleportUiResultB,
                _teleportUiResultA, 4, true);

            DrawChaosHudTrack(left + leftBlockWidth + 0.014f,
                top + height - 0.011f,
                width - leftBlockWidth - 0.027f,
                progress, ar, ag, ab);
        }

        private void DrawInstantGiftHUDLayoutDesign2(int currentTime)
        {
            if (!_hudInstantEnabled || IsNormalHudSuppressed()) return;

            while (_instantGiftHudItems.Count > 1)
                _instantGiftHudItems.RemoveAt(_instantGiftHudItems.Count - 1);

            for (int i = _instantGiftHudItems.Count - 1; i >= 0; i--)
            {
                InstantGiftHudItem item = _instantGiftHudItems[i];
                if (item == null || currentTime >= item.EndTime)
                    _instantGiftHudItems.RemoveAt(i);
            }

            if (_instantGiftHudItems.Count == 0) return;

            InstantGiftHudItem activeItem = _instantGiftHudItems[0];

            int bgR = activeItem.IsGood ? _instantGoodBgR : _instantBadBgR;
            int bgG = activeItem.IsGood ? _instantGoodBgG : _instantBadBgG;
            int bgB = activeItem.IsGood ? _instantGoodBgB : _instantBadBgB;
            int ar = activeItem.IsGood ? _instantAccentGoodR : _instantAccentBadR;
            int ag = activeItem.IsGood ? _instantAccentGoodG : _instantAccentBadG;
            int ab = activeItem.IsGood ? _instantAccentGoodB : _instantAccentBadB;

            float width = Math.Max(0.11f, _instantHudBoxWidth);
            float height = Math.Max(0.040f, _instantHudBoxHeight);

            Function.Call(Hash.DRAW_RECT, _instantHudX, _instantHudStartY,
                width, height, bgR, bgG, bgB, _instantHudBgA);

            DrawGiftOutline(_instantHudX, _instantHudStartY, width, height,
                _instantHudBorderSize, _instantHudBorderR, _instantHudBorderG,
                _instantHudBorderB, _instantHudBorderA);

            Function.Call(Hash.DRAW_RECT, _instantHudX,
                _instantHudStartY - height / 2f + 0.0020f,
                width - 0.008f, 0.0030f, ar, ag, ab, 255);

            DrawReferenceProgressText(activeItem.Text, _instantHudX,
                _instantHudStartY + _instantHudTextOffsetY,
                _instantHudTextScale, width - 0.020f,
                _instantHudTextR, _instantHudTextG, _instantHudTextB,
                _instantHudTextA, _instantHudTextFont, true);
        }

        private void DrawActiveStatusHUDLayoutDesign2(int currentTime)
        {
            if (IsNormalHudSuppressed()) return;

            List<ActiveStatusItem> activeStatuses =
                BuildActiveStatusItems(currentTime);

            List<ActiveStatusItem> displayedStatuses =
                SelectActiveHudCards(activeStatuses);

            foreach (ActiveStatusItem item in activeStatuses)
            {
                string eventLabel, eventClock;
                SplitTimerStatusText(item.Text, out eventLabel, out eventClock);
                int remaining, peak;
                if (TryParseGiftTimerSeconds(eventClock, out remaining))
                {
                    if (!_hudTimerPeakSeconds.TryGetValue(item.Key, out peak)) peak = 1;
                    _hudTimerPeakSeconds[item.Key] = Math.Max(peak, remaining);
                }
            }

            DrawInstantGiftHUD(currentTime);

            if (!_hudTimerEnabled || displayedStatuses.Count == 0) return;

            float boxWidth = Math.Max(0.13f, _statusHudBoxWidth);
            float boxHeight = Math.Max(0.042f, _statusHudBoxHeight);

            for (int i = 0; i < displayedStatuses.Count; i++)
            {
                ActiveStatusItem status = displayedStatuses[i];
                float currentY = _statusHudStartY - i * _statusHudBoxSpacing;

                int bgR = status.IsGood ? _timerGoodBgR : _timerBadBgR;
                int bgG = status.IsGood ? _timerGoodBgG : _timerBadBgG;
                int bgB = status.IsGood ? _timerGoodBgB : _timerBadBgB;
                int ar = status.IsGood ? _timerAccentGoodR : _timerAccentBadR;
                int ag = status.IsGood ? _timerAccentGoodG : _timerAccentBadG;
                int ab = status.IsGood ? _timerAccentGoodB : _timerAccentBadB;

                Function.Call(Hash.DRAW_RECT, _statusHudX, currentY,
                    boxWidth, boxHeight, bgR, bgG, bgB, _statusHudBgA);

                DrawGiftOutline(_statusHudX, currentY, boxWidth, boxHeight,
                    _statusHudBorderSize, _statusHudBorderR, _statusHudBorderG,
                    _statusHudBorderB, _statusHudBorderA);

                Function.Call(Hash.DRAW_RECT, _statusHudX,
                    currentY - boxHeight / 2f + 0.0020f,
                    boxWidth - 0.008f, 0.0030f, ar, ag, ab, 255);

                string label, timer;
                SplitTimerStatusText(status.Text, out label, out timer);

                int seconds;
                bool hasTimer = TryParseGiftTimerSeconds(timer, out seconds);

                float left = _statusHudX - boxWidth / 2f;
                float pad = 0.010f;

                if (hasTimer)
                {
                    float timeWidth = Math.Max(0.055f,
                        boxWidth * Math.Max(0.18f, Math.Min(0.36f, _giftTimerColumnRatio)));
                    float dividerX = left + boxWidth - timeWidth;

                    Function.Call(Hash.DRAW_RECT, dividerX, currentY + 0.002f,
                        0.0006f, boxHeight - 0.014f,
                        _progressPanelDividerR, _progressPanelDividerG,
                        _progressPanelDividerB, _progressPanelDividerA);

                    DrawReferenceProgressText(label, left + pad,
                        currentY + _statusHudTextOffsetY, _statusHudTextScale,
                        dividerX - left - pad - 0.006f,
                        _statusHudTextR, _statusHudTextG, _statusHudTextB,
                        _statusHudTextA, _statusHudTextFont, false);

                    string clock =
                        (seconds / 60).ToString("00", CultureInfo.InvariantCulture) +
                        ":" + (seconds % 60).ToString("00", CultureInfo.InvariantCulture);

                    DrawReferenceProgressText(clock, dividerX + timeWidth / 2f,
                        currentY + _giftTimerTimeOffsetY, _giftTimerTimeScale,
                        timeWidth - 0.012f,
                        _statusHudTextR, _statusHudTextG, _statusHudTextB,
                        _statusHudTextA, _statusHudTextFont, true);

                    int peak;
                    if (!_hudTimerPeakSeconds.TryGetValue(status.Key, out peak))
                        peak = Math.Max(1, seconds);

                    float fraction = Math.Max(0f, Math.Min(1f,
                        seconds / (float)Math.Max(1, peak)));

                    float trackWidth = boxWidth - pad * 2f;
                    float trackY = currentY + boxHeight / 2f - 0.008f;
                    float trackHeight = Math.Max(0.002f, _giftTimerBarHeight);

                    Function.Call(Hash.DRAW_RECT, _statusHudX, trackY,
                        trackWidth, trackHeight,
                        _progressTrackR, _progressTrackG,
                        _progressTrackB, _progressTrackA);

                    float fillWidth = trackWidth * fraction;
                    if (fillWidth > 0f)
                    {
                        Function.Call(Hash.DRAW_RECT,
                            left + pad + fillWidth / 2f, trackY,
                            fillWidth, trackHeight, ar, ag, ab, 255);
                    }
                }
                else
                {
                    DrawReferenceProgressText(label, left + pad,
                        currentY + _statusHudTextOffsetY, _statusHudTextScale,
                        boxWidth - pad * 2f,
                        _statusHudTextR, _statusHudTextG, _statusHudTextB,
                        _statusHudTextA, _statusHudTextFont, false);
                }
            }
        }
    }
}
