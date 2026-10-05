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
        private void DrawPlayerHealthBarLayoutDefault()
        {
            if (!_hudHealthEnabled ||
                IsNormalHudSuppressed())
                return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;

            // Hapus HIDE_HUD_COMPONENT_THIS_FRAME dari sini!

            int currentHp =
                Math.Max(
                    0,
                    _customHealth
                );

            int maxHp =
                Math.Max(
                    1,
                    _playerMaxHealth
                );

            float hpPercent =
                Math.Min(
                    1.0f,
                    (float)currentHp /
                    maxHp
                );

            DrawPlayerVitalCard(_hpBarX, _healthPanelLabel,
                FormatHudText(_hpTextFormat, currentHp, maxHp), hpPercent,
                _hpTextOffsetY, _hpTextScale, _hpTextR, _hpTextG, _hpTextB, _hpTextA,
                _hpFillR, _hpFillG, _hpFillB, _hpFillA);
        }

        private void DrawCountdownUILayoutDefault(int currentTime)
        {
            if (!_randomChaosEnabled || !_hudApocalypseEnabled || IsNormalHudSuppressed()) return;
            bool rolling = _isChaosRouletteActive && !_chaosRouletteLocked;
            bool locked = _isChaosRouletteActive && _chaosRouletteLocked;
            bool active = _isChaosActive;
            bool pausedNext = rolling || locked || active;
            bool queued = active &&
                ((_activeChaosKey == "blackhole" && _pendingBlackholeDurationMs > 0 && currentTime < _earthquakeEndTime) ||
                 (_activeChaosKey == "earthquake" && _pendingEarthquakeDurationMs > 0 && currentTime < _blackholeEndTime));
            string state = rolling ? "ROLLING" : locked ? "LOCKED" : queued ? "QUEUED" : active ? "ACTIVE" : "COUNTDOWN";
            string effect = active ? _activeChaosName : "NEXT RANDOM EFFECT";
            if (rolling && _hudChaosRouletteEnabled && _randomChaosOptions != null && _randomChaosOptions.Count > 0)
            {
                int index = Math.Max(0, Math.Min(_randomChaosOptions.Count - 1, _currentChaosRouletteIndex));
                effect = _randomChaosOptions[index].DisplayName;
            }
            else if (rolling) effect = "SELECTING EFFECT";
            int nextMs = pausedNext ? Math.Max(0, _countdownDurationMs) : Math.Max(0, _countdownRemainingMs);
            int effectMs = Math.Max(0, _chaosRemainingMs);
            int effectTotal = Math.Max(1, DEFAULT_RANDOM_CHAOS_DURATION_MS);
            if (active && _randomChaosOptions != null && _activeChaosIndex >= 0 && _activeChaosIndex < _randomChaosOptions.Count)
                effectTotal = Math.Max(1, _randomChaosOptions[_activeChaosIndex].DurationMs);
            int phaseMs = rolling ? Math.Max(0, _chaosRouletteDurationMs - (currentTime - _chaosRouletteStartTime)) : effectMs;
            float nextProgress = Math.Max(0f, Math.Min(1f, nextMs / (float)Math.Max(1, _countdownDurationMs)));
            float effectProgress = rolling
                ? Math.Max(0f, Math.Min(1f, phaseMs / (float)Math.Max(1, _chaosRouletteDurationMs)))
                : active ? Math.Max(0f, Math.Min(1f, effectMs / (float)effectTotal)) : 0f;

            // Replacement card: only the currently running phase occupies the fixed frame.
            int shownMs = rolling || active ? phaseMs : nextMs;
            float shownProgress = rolling || active ? effectProgress : nextProgress;
            string shownLabel = rolling || active ? effect : "NEXT CHAOS";
            float left = _apocalypsePosX, top = _apocalypsePosY;
            float width = Math.Max(0.16f, _apocalypseBgWidth), height = Math.Max(0.09f, _apocalypseBgHeight);
            float cx = left + width / 2f, cy = top + height / 2f;
            int ar = locked || active ? _chaosUiLockedR : _apocalypseR;
            int ag = locked || active ? _chaosUiLockedG : _apocalypseG;
            int ab = locked || active ? _chaosUiLockedB : _apocalypseB;
            float pad = 0.009f;
            Function.Call(Hash.DRAW_RECT, cx, cy, width, height, _apocalypseBgR, _apocalypseBgG, _apocalypseBgB, _apocalypseBgA);
            DrawGiftOutline(cx, cy, width, height, 0.001f, 154, 177, 192, 230);
            Function.Call(Hash.DRAW_RECT, left + 0.003f, cy, 0.0015f, height - 0.008f, ar, ag, ab, 230);
            Function.Call(Hash.DRAW_RECT, left + width - 0.003f, cy, 0.0015f, height - 0.008f, ar, ag, ab, 230);
            DrawReferenceProgressText("RANDOM CHAOS", left + pad, top,
                _apocalypseScale, width * 0.65f - pad, 248, 250, 253, 255, _apocalypseFont, false);
            DrawReferenceProgressText(state, left + width * 0.82f, top + 0.003f,
                _chaosUiStatusScale, width * 0.30f, ar, ag, ab, 255, _chaosUiStatusFont, true);
            Function.Call(Hash.DRAW_RECT, cx, top + height * 0.36f, width - 2f * pad, 0.0008f, 92, 113, 134, 185);
            float dividerX = left + width * 0.67f;
            float valueWidth = left + width - pad - dividerX;
            Function.Call(Hash.DRAW_RECT, dividerX, top + height * 0.59f, 0.0008f, height * 0.32f, 92, 113, 134, 185);
            DrawReferenceProgressText(shownLabel, left + pad, top + height * 0.39f,
                _chaosUiResultScale, dividerX - left - pad - 0.006f,
                248, 250, 253, 255, _chaosUiResultFont, false);
            DrawReferenceProgressText(FormatChaosHudClock(shownMs), dividerX + valueWidth / 2f,
                top + height * 0.36f, 0.70f, valueWidth - 0.007f, 248, 250, 253, 255, 4, true);
            DrawChaosHudTrack(left + pad, top + height * 0.87f, width - 2f * pad,
                shownProgress, ar, ag, ab);
        }

        private void DrawRandomCheckpointMarkersLayoutDefault(
            float barLeftX,
            float trackY,
            float activeBarWidth)
        {
            if (!_randomCheckpointEnabled ||
                !_hudRandomCheckpointEnabled ||
                !IsSelectedDirectRouteMode() ||
                !_randomCheckpointProgressInitialized)
            {
                return;
            }

            for (
                int i = 0;
                i < _randomCheckpointThresholds.Count;
                i++)
            {
                // Marker yang sudah tersentuh dihilangkan dari bar.
                if (_randomCheckpointTriggeredIndices.Contains(
                        i
                    ))
                {
                    continue;
                }

                float markerX =
                    barLeftX +
                    (activeBarWidth *
                     _randomCheckpointThresholds[i]);

                float markerHeightMultiplier =
                    Math.Max(
                        0.10f,
                        Math.Min(
                            3.0f,
                            _randomCheckpointMarkerHeightMultiplier
                        )
                    );

                float markerHeight =
                    _barHeight *
                    markerHeightMultiplier;

                // Tiang merah checkpoint.
                Function.Call(
                    Hash.DRAW_RECT,
                    markerX,
                    trackY,
                    _randomCheckpointMarkerWidth,
                    markerHeight,
                    _randomCheckpointMarkerR,
                    _randomCheckpointMarkerG,
                    _randomCheckpointMarkerB,
                    _randomCheckpointMarkerA
                );

                // Flag kecil mengarah ke kanan seperti mockup yang dipilih.
                if (_randomCheckpointMarkerFlagEnabled)
                {
                    float flagCenterX =
                        markerX +
                        (_randomCheckpointMarkerFlagWidth / 2.0f);

                    float flagCenterY =
                        trackY +
                        _randomCheckpointMarkerFlagOffsetY;

                    Function.Call(
                        Hash.DRAW_RECT,
                        flagCenterX,
                        flagCenterY,
                        _randomCheckpointMarkerFlagWidth,
                        _randomCheckpointMarkerFlagHeight,
                        _randomCheckpointMarkerR,
                        _randomCheckpointMarkerG,
                        _randomCheckpointMarkerB,
                        _randomCheckpointMarkerA
                    );
                }
            }
        }

        private void DrawRandomCheckpointUILayoutDefault(
            int currentTime)
        {
            return;
        }

        private void DrawScoreCounterLayoutDefault()
        {
            string playerName =
                string.IsNullOrWhiteSpace(_startupPlayerName)
                    ? "PLAYER"
                    : _startupPlayerName.Trim();

            // Nama SCORE mengikuti style nama host pada HUD_PROGRESS:
            // uppercase, warna, font, dan rasio scale yang sama.
            if (_progressHostNameUppercase)
            {
                playerName =
                    playerName.ToUpperInvariant();
            }

            string prefix =
                "SCORE ";

            string suffix =
                " :";

            string valueText =
                _score.ToString(
                    CultureInfo.InvariantCulture
                );

            float scale =
                Math.Max(
                    0.01f,
                    _scoreTextScale
                );

            // Nama player pada SCORE harus sama besar persis dengan label SCORE.
            // Warna/font/uppercase tetap mengikuti style host HUD_PROGRESS.
            float hostScale =
                scale;

            int hostFont =
                _progressPercentFont;

            float prefixWidth =
                MeasureReferenceProgressText(
                    prefix,
                    scale,
                    _scoreTextFont
                );

            float hostWidth =
                MeasureReferenceProgressText(
                    playerName,
                    hostScale,
                    hostFont
                );

            float suffixWidth =
                MeasureReferenceProgressText(
                    suffix,
                    scale,
                    _scoreTextFont
                );

            float valueWidth =
                MeasureReferenceProgressText(
                    valueText,
                    scale,
                    _scoreTextFont
                );

            // Jarak setelah titik dua dibuat sangat rapat agar isi panel
            // tidak terlihat terlalu menyebar.
            float gap =
                0.002f;

            float contentWidth =
                prefixWidth +
                hostWidth +
                suffixWidth +
                gap +
                valueWidth;

            // Width SCORE dibuat fixed. Jika nama panjang, semua text
            // mengecil proporsional supaya tidak membuat box melebar.
            float panelWidth =
                Math.Max(
                    0.10f,
                    _scoreMinWidth
                );

            float availableWidth =
                Math.Max(
                    0.05f,
                    panelWidth -
                    (_scorePaddingX * 2.0f)
                );

            if (contentWidth >
                    availableWidth &&
                contentWidth > 0.0f)
            {
                float fitRatio =
                    availableWidth /
                    contentWidth;

                scale *=
                    fitRatio;

                hostScale *=
                    fitRatio;

                prefixWidth =
                    MeasureReferenceProgressText(
                        prefix,
                        scale,
                        _scoreTextFont
                    );

                hostWidth =
                    MeasureReferenceProgressText(
                        playerName,
                        hostScale,
                        hostFont
                    );

                suffixWidth =
                    MeasureReferenceProgressText(
                        suffix,
                        scale,
                        _scoreTextFont
                    );

                valueWidth =
                    MeasureReferenceProgressText(
                        valueText,
                        scale,
                        _scoreTextFont
                    );

                gap *=
                    fitRatio;

                contentWidth =
                    prefixWidth +
                    hostWidth +
                    suffixWidth +
                    gap +
                    valueWidth;
            }

            // DrawModernHudPanel(
            //     _scorePosX,
            //     _scorePosY,
            //     panelWidth,
            //     _scoreHeight,
            //     _scoreBgR,
            //     _scoreBgG,
            //     _scoreBgB,
            //     _scoreBgA
            // );

            // Accent kiri/kanan mengikuti panel WINS/DEATHS.
            float centerY =
                _scorePosY +
                (_scoreHeight / 2.0f);

            Function.Call(
                Hash.DRAW_RECT,
                _scorePosX + 0.0025f,
                centerY,
                0.0015f,
                Math.Max(
                    0.001f,
                    _scoreHeight - 0.012f
                ),
                132,
                170,
                199,
                230
            );

            Function.Call(
                Hash.DRAW_RECT,
                _scorePosX + panelWidth - 0.0025f,
                centerY,
                0.0015f,
                Math.Max(
                    0.001f,
                    _scoreHeight - 0.012f
                ),
                132,
                170,
                199,
                230
            );

            int valueR;
            int valueG;
            int valueB;
            int valueA;

            if (_score > 0)
            {
                valueR = _scorePositiveR;
                valueG = _scorePositiveG;
                valueB = _scorePositiveB;
                valueA = _scorePositiveA;
            }
            else if (_score < 0)
            {
                valueR = _scoreNegativeR;
                valueG = _scoreNegativeG;
                valueB = _scoreNegativeB;
                valueA = _scoreNegativeA;
            }
            else
            {
                valueR = _scoreZeroR;
                valueG = _scoreZeroG;
                valueB = _scoreZeroB;
                valueA = _scoreZeroA;
            }

            float contentLeft =
                _scorePosX +
                ((panelWidth - contentWidth) / 2.0f);

            float textY =
                _scorePosY +
                _scoreTextOffsetY;

            float cursorX =
                contentLeft;

            DrawCleanProgressText(
                prefix,
                cursorX +
                    (prefixWidth / 2.0f),
                textY,
                scale,
                _scoreLabelR,
                _scoreLabelG,
                _scoreLabelB,
                _scoreLabelA,
                _scoreTextFont
            );

            cursorX +=
                prefixWidth;

            DrawCleanProgressText(
                playerName,
                cursorX +
                    (hostWidth / 2.0f),
                textY,
                hostScale,
                _progressHostTextR,
                _progressHostTextG,
                _progressHostTextB,
                _progressHostTextA,
                hostFont
            );

            cursorX +=
                hostWidth;

            DrawCleanProgressText(
                suffix,
                cursorX +
                    (suffixWidth / 2.0f),
                textY,
                scale,
                _scoreLabelR,
                _scoreLabelG,
                _scoreLabelB,
                _scoreLabelA,
                _scoreTextFont
            );

            cursorX +=
                suffixWidth +
                gap;

            DrawCleanProgressText(
                valueText,
                cursorX +
                    (valueWidth / 2.0f),
                textY,
                scale,
                valueR,
                valueG,
                valueB,
                valueA,
                _scoreTextFont
            );
        }

        private void DrawDeathCounterLayoutDefault()
        {
            if (IsNormalHudSuppressed())
                return;

            if (_scoreModeEnabled)
            {
                DrawScoreCounter();
                DrawProgressBar();
                return;
            }

            // Score=false adalah satu-satunya kondisi untuk menampilkan
            // HUD WINS + DEATHS. HUD_WIN/HUD_DEATH tidak memiliki
            // toggle Enabled terpisah.
            DrawModernCounterRow(
                _winPosX,
                _winPosY,
                _winBgWidth,
                _winBgHeight,
                _winTextOffsetY,
                _winValueTextOffsetY,
                _winLabel,
                "WINS :",
                _winSubtitle,
                _winCount,
                _winScale,
                _winFont,
                _winR,
                _winG,
                _winB,
                _winA,
                _winSubtitleScale,
                _winSubtitleR,
                _winSubtitleG,
                _winSubtitleB,
                _winSubtitleA,
                _winBgR,
                _winBgG,
                _winBgB,
                _winBgA
            );

            DrawModernCounterRow(
                _deathPosX,
                _deathPosY,
                _deathBgWidth,
                _deathBgHeight,
                _deathTextOffsetY,
                _deathValueTextOffsetY,
                _deathLabel,
                "DEATHS :",
                _deathSubtitle,
                _deathCount,
                _deathScale,
                _deathFont,
                _deathR,
                _deathG,
                _deathB,
                _deathA,
                _deathSubtitleScale,
                _deathSubtitleR,
                _deathSubtitleG,
                _deathSubtitleB,
                _deathSubtitleA,
                _deathBgR,
                _deathBgG,
                _deathBgB,
                _deathBgA
            );

            DrawProgressBar();
        }

        private void DrawRouteLoadingProgressPanelLayoutDefault()
        {
            float width = Math.Max(0.18f, _barWidth);
            float height = Math.Max(0.045f, _barHeight);

            Function.Call(
                Hash.DRAW_RECT,
                _barX,
                _barY,
                width,
                height,
                _bgR,
                _bgG,
                _bgB,
                Math.Max(210, _bgA)
            );

            string modeText =
                _selectedGameplayMode == MoonGameplayMode.Survival
                    ? "SURVIVAL - LOADING ROUTE..."
                    : "GO TO MOUNTAIN - LOADING ROUTE...";

            DrawCleanProgressText(
                modeText,
                _barX,
                _barY - 0.014f,
                Math.Max(0.34f, Math.Min(0.52f, _textMeterScale)),
                _textMeterR,
                _textMeterG,
                _textMeterB,
                _textMeterA,
                _textMeterFont
            );
        }

        private void DrawProgressBarLayoutDefault()
        {
            if (!_hudProgressEnabled)
                return;

            bool routeNeedsPos =
                !IsSelectedDirectRouteMode();

            if (_routeGenerationPending ||
                _activeFinishPosition == Vector3.Zero ||
                (routeNeedsPos &&
                 _activePosList.Count == 0))
            {
                DrawRouteLoadingProgressPanel();
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

            // =============================================================
            // TARGET / LEG AKTIF
            // =============================================================
            bool isGoingToFinish =
                _currentPosIndex >=
                _activePosList.Count;

            Vector3 currentTargetPos =
                (!isGoingToFinish &&
                 _activePosList.Count > _currentPosIndex)
                    ? _activePosList[_currentPosIndex]
                    : _activeFinishPosition;

            Vector3 legStartPos;

            if (isGoingToFinish)
            {
                legStartPos =
                    _activePosList.Count > 0
                        ? _activePosList[
                            _activePosList.Count - 1
                          ]
                        : _startPosition;
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

            float progress;

            if (currentDistance <= targetRadius)
            {
                progress = 1.0f;
            }
            else
            {
                float effectiveTotalDistance =
                    totalDistance -
                    targetRadius;

                if (effectiveTotalDistance <= 0.001f)
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

            // =============================================================
            // BOXY GTA HUD - DESIGN V2 + FLEXIBLE WIDTH
            // Saat nama host panjang, seluruh panel melebar secara horizontal.
            // Divider dan progress track ikut melebar proporsional sehingga
            // nama dapat tampil penuh tanpa dipotong dengan "...".
            // =============================================================
            bool randomSequenceActive =
                _hudRandomCheckpointEnabled &&
                _randomCheckpointSequenceActive;

            float activePanelWidth = _progressPanelWidth;
            float horizontalScale = 1.0f;

            if (_progressDynamicPanelEnabled &&
                !randomSequenceActive &&
                _progressHostNameEnabled)
            {
                string dynamicHostName =
                    (_startupPlayerName ?? string.Empty).Trim();

                if (_progressHostNameUppercase)
                    dynamicHostName = dynamicHostName.ToUpperInvariant();

                if (!string.IsNullOrEmpty(dynamicHostName))
                {
                    float sizingScale =
                        _progressPercentScale *
                        Math.Max(0.10f, _progressHostTextScaleRatio);

                    string sizingSuffix = _progressCaptionText ?? string.Empty;
                    float requiredCaptionWidth =
                        MeasureReferenceProgressText(dynamicHostName, sizingScale, _progressPercentFont);

                    if (!string.IsNullOrEmpty(sizingSuffix))
                    {
                        requiredCaptionWidth +=
                            0.006f +
                            MeasureReferenceProgressText(sizingSuffix, sizingScale, _progressPercentFont);
                    }

                    requiredCaptionWidth +=
                        Math.Max(0.0f, _progressDynamicPanelPadding);

                    float percentWidthForSizing =
                        MeasureReferenceProgressText("100%", _progressPercentScale, _progressPercentFont);

                    float widthCoefficient =
                        ((_progressPanelRightDividerOffsetX -
                          _progressPanelLeftDividerOffsetX) +
                         _barWidth) / 2.0f;

                    float fixedCaptionCost =
                        0.010f +
                        _progressPercentOffsetX +
                        percentWidthForSizing +
                        0.005f +
                        _progressCaptionOffsetX;

                    if (widthCoefficient > 0.001f)
                    {
                        float neededScale =
                            (requiredCaptionWidth + fixedCaptionCost) /
                            widthCoefficient;

                        horizontalScale =
                            Math.Max(1.0f, neededScale);

                        float maxPanelWidth =
                            Math.Max(
                                _progressPanelWidth,
                                Math.Min(0.98f, _progressDynamicPanelMaxWidth)
                            );

                        float maxHorizontalScale =
                            maxPanelWidth /
                            Math.Max(0.001f, _progressPanelWidth);

                        horizontalScale =
                            Math.Min(horizontalScale, maxHorizontalScale);
                    }
                }
            }

            activePanelWidth =
                Math.Min(
                    0.98f,
                    _progressPanelWidth * horizontalScale
                );

            horizontalScale =
                activePanelWidth /
                Math.Max(0.001f, _progressPanelWidth);

            float activeBarWidth =
                _barWidth * horizontalScale;

            float panelHalfWidth =
                activePanelWidth / 2.0f;

            float panelLeftX =
                _barX - panelHalfWidth;

            float panelRightX =
                _barX + panelHalfWidth;

            float leftDividerX =
                _barX +
                (_progressPanelLeftDividerOffsetX * horizontalScale);

            float rightDividerX =
                _barX +
                (_progressPanelRightDividerOffsetX * horizontalScale);

            // Shadow box - tetap kotak, tanpa rounded corner.
            Function.Call(
                Hash.DRAW_RECT,
                _barX,
                _barY + _progressPanelShadowOffsetY,
                activePanelWidth + _progressPanelShadowExtraWidth,
                _progressPanelHeight + _progressPanelShadowExtraHeight,
                _progressPanelShadowR,
                _progressPanelShadowG,
                _progressPanelShadowB,
                _progressPanelShadowA
            );

            // Frame luar tipis seperti panel industrial pada reference kedua.
            Function.Call(
                Hash.DRAW_RECT,
                _barX,
                _barY,
                activePanelWidth + 0.0024f,
                _progressPanelHeight + 0.0034f,
                92,
                108,
                126,
                205
            );

            // Base panel.
            Function.Call(
                Hash.DRAW_RECT,
                _barX,
                _barY,
                activePanelWidth,
                _progressPanelHeight,
                _progressPanelR,
                _progressPanelG,
                _progressPanelB,
                _progressPanelA
            );

            // Left + right module dibuat sedikit berbeda supaya terlihat seperti
            // tiga box terpisah, tetapi tetap satu HUD yang rapih.
            float leftModuleWidth =
                Math.Max(
                    0.001f,
                    leftDividerX - panelLeftX
                );

            float rightModuleWidth =
                Math.Max(
                    0.001f,
                    panelRightX - rightDividerX
                );

            int sidePanelR =
                Math.Max(0, _progressPanelR - 3);
            int sidePanelG =
                Math.Max(0, _progressPanelG - 3);
            int sidePanelB =
                Math.Max(0, _progressPanelB - 2);

            Function.Call(
                Hash.DRAW_RECT,
                panelLeftX + (leftModuleWidth / 2.0f),
                _barY,
                leftModuleWidth,
                _progressPanelHeight,
                sidePanelR,
                sidePanelG,
                sidePanelB,
                _progressPanelA
            );

            Function.Call(
                Hash.DRAW_RECT,
                rightDividerX + (rightModuleWidth / 2.0f),
                _barY,
                rightModuleWidth,
                _progressPanelHeight,
                sidePanelR,
                sidePanelG,
                sidePanelB,
                _progressPanelA
            );

            // Divider vertikal kiri/kanan. Dibuat lurus dan boxy.
            float dividerHeight =
                Math.Max(
                    0.001f,
                    _progressPanelHeight - 0.006f
                );

            float dividerThickness =
                Math.Max(
                    0.0008f,
                    _progressPanelDividerThickness
                );

            if (_progressPanelDividerEnabled)
            {
                Function.Call(
                    Hash.DRAW_RECT,
                    leftDividerX,
                    _barY,
                    dividerThickness,
                    dividerHeight,
                    _progressPanelDividerR,
                    _progressPanelDividerG,
                    _progressPanelDividerB,
                    Math.Max(150, _progressPanelDividerA)
                );

                Function.Call(
                    Hash.DRAW_RECT,
                    rightDividerX,
                    _barY,
                    dividerThickness,
                    dividerHeight,
                    _progressPanelDividerR,
                    _progressPanelDividerG,
                    _progressPanelDividerB,
                    Math.Max(150, _progressPanelDividerA)
                );

            }

            // Accent lurus tipis di sisi paling kiri / kanan seperti design V2.
            float sideAccentWidth = 0.0015f;
            float sideAccentHeight =
                Math.Max(
                    0.001f,
                    _progressPanelHeight - 0.008f
                );

            Function.Call(
                Hash.DRAW_RECT,
                panelLeftX + 0.0025f,
                _barY,
                sideAccentWidth,
                sideAccentHeight,
                _fillR,
                _fillG,
                _fillB,
                230
            );

            Function.Call(
                Hash.DRAW_RECT,
                panelRightX - 0.0025f,
                _barY,
                sideAccentWidth,
                sideAccentHeight,
                _fillR,
                _fillG,
                _fillB,
                230
            );

            float trackY =
                _barY +
                _progressTrackOffsetY;

            float trackCenterX = (leftDividerX + rightDividerX) / 2.0f;
            float barLeftX = trackCenterX - (activeBarWidth / 2.0f);

            // =============================================================
            // TENTUKAN ISI PANEL SESUAI STATE
            // NORMAL -> COUNTDOWN -> ROULETTE -> LOCKED -> NORMAL
            // =============================================================
            string leftText;
            string centerText;
            string rightText;

            float displayProgress =
                progress;

            int fillR = _fillR;
            int fillG = _fillG;
            int fillB = _fillB;
            int fillA = _fillA;

            int leftR = _progressPercentR;
            int leftG = _progressPercentG;
            int leftB = _progressPercentB;
            int leftA = _progressPercentA;

            int centerR = _missionTextR;
            int centerG = _missionTextG;
            int centerB = _missionTextB;
            int centerA = _missionTextA;

            int rightR = _progressRightR;
            int rightG = _progressRightG;
            int rightB = _progressRightB;
            int rightA = _progressRightA;

            float leftScale =
                _progressPercentScale;

            float centerScale =
                _missionTextScale;

            float rightScale =
                _progressRightScale;

            int leftFont =
                _progressPercentFont;

            int centerFont =
                _missionTextFont;

            int rightFont =
                _progressRightFont;

            if (randomSequenceActive)
            {
                leftText =
                    _randomCheckpointTitle;

                // Gunakan scale dari INI secara langsung.
                // Versi lama mengunci maksimal 0.42 sehingga RANDOM CHECKPOINT
                // selalu terlihat kecil walaupun TitleScale dinaikkan.
                leftScale =
                    _randomCheckpointTitleScale;

                leftFont =
                    _randomCheckpointTitleFont;

                leftR =
                    _randomCheckpointTitleR;
                leftG =
                    _randomCheckpointTitleG;
                leftB =
                    _randomCheckpointTitleB;
                leftA =
                    _randomCheckpointTitleA;

                int currentTime =
                    Game.GameTime;

                int elapsedMs =
                    Math.Max(
                        0,
                        currentTime -
                        _randomCheckpointPhaseStartTime
                    );

                if (_randomCheckpointPhase == 1)
                {
                    int totalMs =
                        Math.Max(
                            1,
                            _randomCheckpointCountdownSeconds *
                            1000
                        );

                    int elapsedSeconds =
                        elapsedMs /
                        1000;

                    int remaining =
                        Math.Max(
                            1,
                            _randomCheckpointCountdownSeconds -
                            elapsedSeconds
                        );

                    centerText =
                        "EFFECT IN";

                    // Semua text Random Checkpoint memakai scale khusus dari INI
                    // agar ukurannya sama-sama besar dan terbaca.
                    centerScale =
                        _randomCheckpointGachaScale;
                    centerFont =
                        _randomCheckpointGachaFont;

                    rightText =
                        remaining.ToString(
                            CultureInfo.InvariantCulture
                        ) +
                        " SEC";

                    rightScale =
                        _randomCheckpointCountdownScale;
                    rightFont =
                        _randomCheckpointCountdownFont;

                    displayProgress =
                        1.0f -
                        Math.Max(
                            0.0f,
                            Math.Min(
                                1.0f,
                                elapsedMs /
                                (float)totalMs
                            )
                        );

                    fillR = _randomCheckpointCountdownBarR;
                    fillG = _randomCheckpointCountdownBarG;
                    fillB = _randomCheckpointCountdownBarB;
                    fillA = _randomCheckpointCountdownBarA;

                    rightR = _randomCheckpointCountdownR;
                    rightG = _randomCheckpointCountdownG;
                    rightB = _randomCheckpointCountdownB;
                    rightA = _randomCheckpointCountdownA;
                }
                else if (_randomCheckpointPhase == 2)
                {
                    centerText =
                        GetRandomCheckpointEffectDisplayName(
                            _randomCheckpointCurrentEffectKey
                        );

                    centerScale =
                        GetRandomCheckpointCenterTextScale(
                            centerText
                        );
                    centerFont =
                        _randomCheckpointGachaFont;

                    rightText =
                        "ROLLING";

                    rightScale =
                        _randomCheckpointCountdownScale;
                    rightFont =
                        _randomCheckpointCountdownFont;

                    displayProgress =
                        Math.Max(
                            0.0f,
                            Math.Min(
                                1.0f,
                                elapsedMs /
                                (float)Math.Max(
                                    1,
                                    _randomCheckpointGachaDurationMs
                                )
                            )
                        );

                    fillR = _randomCheckpointGachaBarR;
                    fillG = _randomCheckpointGachaBarG;
                    fillB = _randomCheckpointGachaBarB;
                    fillA = _randomCheckpointGachaBarA;

                    centerR = _randomCheckpointGachaR;
                    centerG = _randomCheckpointGachaG;
                    centerB = _randomCheckpointGachaB;
                    centerA = _randomCheckpointGachaA;

                    rightR = _randomCheckpointGachaBarR;
                    rightG = _randomCheckpointGachaBarG;
                    rightB = _randomCheckpointGachaBarB;
                    rightA = 255;
                }
                else
                {
                    centerText =
                        GetRandomCheckpointEffectDisplayName(
                            _randomCheckpointLockedEffectKey
                        );

                    centerScale =
                        GetRandomCheckpointCenterTextScale(
                            centerText
                        );
                    centerFont =
                        _randomCheckpointGachaFont;

                    rightText =
                        _randomCheckpointLockedText;

                    rightScale =
                        _randomCheckpointLockedScale;
                    rightFont =
                        _randomCheckpointLockedFont;

                    displayProgress =
                        1.0f;

                    fillR = _randomCheckpointLockedBarR;
                    fillG = _randomCheckpointLockedBarG;
                    fillB = _randomCheckpointLockedBarB;
                    fillA = _randomCheckpointLockedBarA;

                    centerR = _randomCheckpointGachaR;
                    centerG = _randomCheckpointGachaG;
                    centerB = _randomCheckpointGachaB;
                    centerA = _randomCheckpointGachaA;

                    rightR = _randomCheckpointLockedR;
                    rightG = _randomCheckpointLockedG;
                    rightB = _randomCheckpointLockedB;
                    rightA = _randomCheckpointLockedA;
                }
            }
            else
            {
                int percent =
                    Math.Max(
                        0,
                        Math.Min(
                            100,
                            (int)Math.Round(
                                progress *
                                100.0f
                            )
                        )
                    );

                leftText =
                    percent.ToString(
                        CultureInfo.InvariantCulture
                    ) +
                    "%";

                if (_selectedGameplayMode == MoonGameplayMode.GoToMountain)
                {
                    centerText =
                        _progressMissionGoToMountainText;
                }
                else if (_selectedGameplayMode == MoonGameplayMode.Survival)
                {
                    centerText =
                        _progressMissionSurvivalText;
                }
                else
                {
                    centerText =
                        _progressMissionNormalText;
                }

                if (_progressMissionTextUppercase &&
                    !string.IsNullOrEmpty(centerText))
                {
                    centerText =
                        centerText.ToUpperInvariant();
                }

                // DIRECT GO TO MOUNTAIN:
                // GotoMountainPos=false berarti route langsung START -> FINISH.
                // Tidak ada nomor POS. Panel kanan khusus menampilkan:
                // TARGET
                // MOUNTAIN
                if (IsSelectedDirectRouteMode())
                {
                    rightText =
                        _selectedGameplayMode == MoonGameplayMode.Survival
                            ? _progressDirectSurvivalValueText
                            : _progressDirectGoToMountainValueText;
                }
                else if (isGoingToFinish ||
                    _activePosList.Count <= 0)
                {
                    rightText =
                        "FINISH";
                }
                else
                {
                    int totalPos =
                        _activePosList.Count;

                    int displayPos =
                        Math.Min(
                            _currentPosIndex + 1,
                            totalPos
                        );

                    rightText =
                        FormatHudText(
                            _progressPosTextFormat,
                            displayPos,
                            totalPos
                        );
                }
            }

            // =============================================================
            // TRACK BOXY + FRAME
            // Bar sengaja kotak penuh: tidak ada rounded / lekukan.
            // =============================================================
            Function.Call(
                Hash.DRAW_RECT,
                trackCenterX,
                trackY,
                activeBarWidth + 0.0030f,
                _barHeight + 0.0050f,
                72,
                88,
                106,
                220
            );

            Function.Call(
                Hash.DRAW_RECT,
                trackCenterX,
                trackY,
                activeBarWidth,
                _barHeight,
                _progressTrackR,
                _progressTrackG,
                _progressTrackB,
                _progressTrackA
            );

            // =============================================================
            // FILL
            // =============================================================
            float currentFillWidth =
                activeBarWidth *
                Math.Max(
                    0.0f,
                    Math.Min(
                        1.0f,
                        displayProgress
                    )
                );

            if (currentFillWidth > 0.001f)
            {
                float fillCenterX =
                    barLeftX +
                    (currentFillWidth / 2.0f);

                Function.Call(
                    Hash.DRAW_RECT,
                    fillCenterX,
                    trackY,
                    currentFillWidth,
                    _barHeight,
                    fillR,
                    fillG,
                    fillB,
                    fillA
                );
            }

            // Marker merah hanya tampil pada state mission normal.
            if (!randomSequenceActive)
            {
                DrawRandomCheckpointMarkers(
                    barLeftX,
                    trackY,
                    activeBarWidth
                );
            }

            // Pin lama tetap tersedia sebagai opsi, default OFF.
            if (_progressPinEnabled &&
                !randomSequenceActive)
            {
                float pinX =
                    barLeftX +
                    currentFillWidth;

                Function.Call(
                    Hash.DRAW_RECT,
                    pinX,
                    trackY,
                    _progressPinOuterWidth,
                    _barHeight +
                        _progressPinOuterExtraHeight,
                    _progressPinOuterR,
                    _progressPinOuterG,
                    _progressPinOuterB,
                    _progressPinOuterA
                );

                Function.Call(
                    Hash.DRAW_RECT,
                    pinX,
                    trackY,
                    _progressPinInnerWidth,
                    _barHeight +
                        _progressPinInnerExtraHeight,
                    _progressPinInnerR,
                    _progressPinInnerG,
                    _progressPinInnerB,
                    _progressPinInnerA
                );
            }

            // =============================================================
            // REFERENCE LAYOUT: MISSION | PERCENT + PROGRESS / BAR | POS.
            // Text is measured using the current GTA font, then fitted to its column.
            float topTextY = _barY + _progressTopTextOffsetY;
            float inset = 0.010f;
            float missionX = panelLeftX + inset + _missionTextOffsetX;
            float missionWidth = Math.Max(0.01f, leftDividerX - missionX - inset);
            float middleX = barLeftX + _progressPercentOffsetX;
            float middleWidth = Math.Max(0.01f, rightDividerX - middleX - inset);
            float rightX = (rightDividerX + panelRightX) / 2.0f + _progressRightOffsetX;
            float rightWidth = Math.Max(0.01f, panelRightX - rightDividerX - 2.0f * inset);

            if (!randomSequenceActive)
            {
                string missionPrefix = "MISSION :";
                string missionTitle = centerText ?? string.Empty;
                int colon = missionTitle.IndexOf(':');
                if (colon >= 0)
                {
                    missionPrefix = missionTitle.Substring(0, colon + 1).Trim();
                    missionTitle = missionTitle.Substring(colon + 1).Trim();
                }
                DrawReferenceProgressText(missionPrefix, missionX, topTextY,
                    _progressCaptionScale, missionWidth, _progressMissionAccentR,
                    _progressMissionAccentG, _progressMissionAccentB, _progressMissionAccentA, centerFont, false);
                DrawReferenceProgressText(missionTitle, missionX, topTextY + _missionTextOffsetY,
                    centerScale, missionWidth, centerR, centerG, centerB, centerA, centerFont, false);

                // Keep percentage size; reserve 100% width to prevent horizontal jumping.
                float percentWidth = MeasureReferenceProgressText("100%", leftScale, leftFont);
                DrawCleanProgressText(leftText,
                    middleX + MeasureReferenceProgressText(leftText, leftScale, leftFont) / 2.0f,
                    topTextY + _progressPercentOffsetY,
                    leftScale, leftR, leftG, leftB, leftA, leftFont);
                float captionX = middleX + percentWidth + 0.005f + _progressCaptionOffsetX;
                float captionWidth = Math.Max(0.01f, rightDividerX - inset - captionX);
                float hostScale = leftScale * Math.Max(0.10f, _progressHostTextScaleRatio);
                string hostName = _progressHostNameEnabled ? (_startupPlayerName ?? string.Empty).Trim() : string.Empty;
                if (_progressHostNameUppercase) hostName = hostName.ToUpperInvariant();
                string suffix = _progressCaptionText ?? string.Empty;

                // Nama dan kata PROGRESS dirender TERPISAH supaya warna nama host
                // bisa berbeda. Tidak ada lagi pemotongan dengan "...". Jika panel
                // sudah mencapai batas layar, scale keduanya turun bersama agar full.
                float hostWidth = MeasureReferenceProgressText(hostName, hostScale, leftFont);
                float suffixWidth = MeasureReferenceProgressText(suffix, hostScale, leftFont);
                float hostSuffixGap = (!string.IsNullOrEmpty(hostName) && !string.IsNullOrEmpty(suffix))
                    ? 0.006f
                    : 0.0f;
                float combinedWidth = hostWidth + hostSuffixGap + suffixWidth;

                if (combinedWidth > captionWidth && combinedWidth > 0.0f)
                {
                    hostScale *= captionWidth / combinedWidth;
                    hostWidth = MeasureReferenceProgressText(hostName, hostScale, leftFont);
                    suffixWidth = MeasureReferenceProgressText(suffix, hostScale, leftFont);
                    hostSuffixGap = (!string.IsNullOrEmpty(hostName) && !string.IsNullOrEmpty(suffix))
                        ? Math.Min(0.006f, 0.006f * (captionWidth / combinedWidth))
                        : 0.0f;
                }

                if (!string.IsNullOrEmpty(hostName))
                {
                    DrawReferenceProgressText(hostName, captionX,
                        topTextY + _progressCaptionOffsetY, hostScale,
                        Math.Max(0.01f, captionWidth),
                        _progressHostTextR, _progressHostTextG, _progressHostTextB, _progressHostTextA,
                        leftFont, false);
                }

                if (!string.IsNullOrEmpty(suffix))
                {
                    float suffixX = captionX + hostWidth + hostSuffixGap;
                    float suffixMaxWidth = Math.Max(0.01f, captionWidth - hostWidth - hostSuffixGap);
                    DrawReferenceProgressText(suffix, suffixX,
                        topTextY + _progressCaptionOffsetY, hostScale, suffixMaxWidth,
                        _progressCaptionR, _progressCaptionG, _progressCaptionB, _progressCaptionA,
                        leftFont, false);
                }

                string rightValue = rightText ?? string.Empty;
                if (rightValue.StartsWith("POS", StringComparison.OrdinalIgnoreCase))
                    rightValue = rightValue.Substring(3).Trim();
                string rightCaption = IsSelectedDirectRouteMode()
                    ? _progressDirectCaptionText : _progressPosCaptionText;
                DrawReferenceProgressText(rightCaption, rightX + _progressPosCaptionOffsetX,
                    topTextY + _progressPosCaptionOffsetY, _progressPosCaptionScale,
                    rightWidth, _progressCaptionR, _progressCaptionG, _progressCaptionB,
                    _progressCaptionA, rightFont, true);
                DrawReferenceProgressText(rightValue, rightX,
                    topTextY + _progressRightValueOffsetY + _progressRightOffsetY,
                    rightScale, rightWidth, rightR, rightG, rightB, rightA, rightFont, true);
            }
            else
            {
                // Countdown, roulette and locked state use the same three compartments.
                DrawReferenceProgressText(leftText, missionX, topTextY + _missionTextOffsetY,
                    leftScale, missionWidth, leftR, leftG, leftB, leftA, leftFont, false);
                DrawReferenceProgressText(centerText, barLeftX, topTextY + 0.012f,
                    centerScale, middleWidth, centerR, centerG, centerB, centerA, centerFont, false);
                DrawReferenceProgressText(rightText, rightX, topTextY + _progressRightValueOffsetY,
                    rightScale, rightWidth, rightR, rightG, rightB, rightA, rightFont, true);
            }
        }

        private void DrawRandomGatchaUILayoutDefault(int currentTime)
        {
            if (!_randomGatchaActive ||
                _randomGatchaOptions == null ||
                _randomGatchaOptions.Count == 0)
            {
                return;
            }

            int safeIndex = Math.Max(
                0,
                Math.Min(
                    _randomGatchaOptions.Count - 1,
                    _randomGatchaIndex
                )
            );

            RandomGatchaOption option =
                _randomGatchaOptions[safeIndex];

            int ar = _randomGatchaLocked
                ? _randomGatchaAccentLockedR
                : _randomGatchaAccentRollingR;
            int ag = _randomGatchaLocked
                ? _randomGatchaAccentLockedG
                : _randomGatchaAccentRollingG;
            int ab = _randomGatchaLocked
                ? _randomGatchaAccentLockedB
                : _randomGatchaAccentRollingB;

            float width = Math.Max(0.30f, _randomGatchaUiWidth);
            float height = Math.Max(0.115f, _randomGatchaUiHeight);
            float left = _randomGatchaUiX - (width * 0.5f);
            float top = _randomGatchaUiY - (height * 0.5f);
            float cx = _randomGatchaUiX;
            float cy = _randomGatchaUiY;
            float pad = 0.012f;

            // Same modern family: dark card, thin outline, side accents.
            Function.Call(
                Hash.DRAW_RECT,
                cx,
                cy,
                width,
                height,
                14,
                18,
                24,
                238
            );

            DrawGiftOutline(
                cx,
                cy,
                width,
                height,
                0.0010f,
                154,
                177,
                192,
                220
            );

            Function.Call(
                Hash.DRAW_RECT,
                left + 0.003f,
                cy,
                0.0020f,
                height - 0.010f,
                ar,
                ag,
                ab,
                245
            );

            // Top header. Layout sengaja berbeda dari Random Teleport.
            DrawReferenceProgressText(
                "RANDOM GATCHA",
                left + pad,
                top + 0.004f,
                0.62f,
                width * 0.60f,
                MODERN_HUD_TEXT_R,
                MODERN_HUD_TEXT_G,
                MODERN_HUD_TEXT_B,
                255,
                4,
                false
            );

            DrawReferenceProgressText(
                _randomGatchaLocked ? "LOCKED" : "ROLLING",
                left + width - pad - (width * 0.16f),
                top + 0.008f,
                0.38f,
                width * 0.16f,
                ar,
                ag,
                ab,
                255,
                4,
                true
            );

            Function.Call(
                Hash.DRAW_RECT,
                cx,
                top + height * 0.32f,
                width - (pad * 2.0f),
                0.0008f,
                92,
                113,
                134,
                185
            );

            // Selected gift sits inside its own center band, making this card
            // visibly different from the teleport destination layout.
            float selectionY = top + height * 0.56f;
            Function.Call(
                Hash.DRAW_RECT,
                cx,
                selectionY,
                width - (pad * 2.0f),
                height * 0.29f,
                20,
                27,
                35,
                245
            );

            Function.Call(
                Hash.DRAW_RECT,
                left + pad + 0.002f,
                selectionY,
                0.0030f,
                height * 0.22f,
                ar,
                ag,
                ab,
                255
            );

            DrawReferenceProgressText(
                option.DisplayName,
                left + pad + 0.012f,
                selectionY - 0.021f,
                0.76f,
                width - (pad * 2.0f) - 0.085f,
                248,
                250,
                253,
                255,
                4,
                false
            );

            int remainingMs = _randomGatchaLocked
                ? Math.Max(0, _randomGatchaLockEndTime - currentTime)
                : Math.Max(0, _randomGatchaRollMs -
                    (currentTime - _randomGatchaStartTime));

            DrawReferenceProgressText(
                FormatChaosHudClock(remainingMs),
                left + width - pad - 0.045f,
                selectionY - 0.018f,
                0.56f,
                0.080f,
                248,
                250,
                253,
                255,
                4,
                true
            );

            // Seven tiny slots show the 7-entry pool.
            float slotsLeft = left + pad;
            float slotsWidth = width - (pad * 2.0f);
            float gap = 0.0035f;
            float slotWidth =
                (slotsWidth - (gap * (_randomGatchaOptions.Count - 1))) /
                _randomGatchaOptions.Count;
            float slotY = top + height * 0.84f;

            for (int i = 0; i < _randomGatchaOptions.Count; i++)
            {
                float sx = slotsLeft +
                    (slotWidth * i) +
                    (gap * i) +
                    (slotWidth * 0.5f);

                bool selected = i == safeIndex;

                Function.Call(
                    Hash.DRAW_RECT,
                    sx,
                    slotY,
                    slotWidth,
                    0.006f,
                    selected ? ar : MODERN_HUD_TRACK_R,
                    selected ? ag : MODERN_HUD_TRACK_G,
                    selected ? ab : MODERN_HUD_TRACK_B,
                    selected ? 255 : 210
                );
            }
        }

        private void DrawTeleportGachaCardsUILayoutDefault(TeleportLocationItem activeLocation, bool isLocked)
        {
            if (!_hudTeleportGachaEnabled ||
                IsNormalHudSuppressed())
                return;

            float width = Math.Max(0.30f, _teleportUiWidth);
            float height = Math.Max(0.100f, _teleportUiHeight);
            float left = _teleportUiX - (width * 0.5f);
            float top = _teleportUiY - (height * 0.5f);
            float cx = _teleportUiX;
            float cy = _teleportUiY;
            float pad = 0.010f;

            // Warna accent tetap membedakan phase, tetapi base card mengikuti
            // tema HUD modern lain: dark graphite + outline tipis + white text.
            int ar = isLocked ? 147 : 70;
            int ag = isLocked ? 112 : 190;
            int ab = isLocked ? 219 : 235;

            string locationName =
                activeLocation?.DisplayName ?? "UNKNOWN";

            string state =
                isLocked ? "LOCKED" : "ROLLING";

            int currentTime = Game.GameTime;
            int remainingMs = isLocked
                ? Math.Max(0, _teleportGachaLockEndTime - currentTime)
                : Math.Max(0, _teleportGachaDuration -
                    (currentTime - _teleportGachaStartTime));

            float progress = isLocked
                ? Math.Max(0.0f, Math.Min(
                    1.0f,
                    remainingMs /
                    (float)Math.Max(1, _giftRandomTeleportLockDurationMs)
                ))
                : Math.Max(0.0f, Math.Min(
                    1.0f,
                    remainingMs /
                    (float)Math.Max(1, _teleportGachaDuration)
                ));

            Function.Call(
                Hash.DRAW_RECT,
                cx,
                cy,
                width,
                height,
                14,
                18,
                24,
                238
            );

            DrawGiftOutline(
                cx,
                cy,
                width,
                height,
                0.0010f,
                154,
                177,
                192,
                220
            );

            // Slim side accents match the current RANDOM CHAOS / mission family.
            Function.Call(
                Hash.DRAW_RECT,
                left + 0.003f,
                cy,
                0.0017f,
                height - 0.009f,
                ar,
                ag,
                ab,
                245
            );

            Function.Call(
                Hash.DRAW_RECT,
                left + width - 0.003f,
                cy,
                0.0017f,
                height - 0.009f,
                ar,
                ag,
                ab,
                245
            );

            DrawReferenceProgressText(
                "RANDOM TELEPORT",
                left + pad,
                top + 0.003f,
                0.61f,
                width * 0.62f,
                MODERN_HUD_TEXT_R,
                MODERN_HUD_TEXT_G,
                MODERN_HUD_TEXT_B,
                255,
                4,
                false
            );

            DrawReferenceProgressText(
                state,
                left + width - pad - (width * 0.16f),
                top + 0.007f,
                0.37f,
                width * 0.16f,
                ar,
                ag,
                ab,
                255,
                4,
                true
            );

            Function.Call(
                Hash.DRAW_RECT,
                cx,
                top + height * 0.36f,
                width - (pad * 2.0f),
                0.0008f,
                92,
                113,
                134,
                185
            );

            float dividerX =
                left + width * 0.72f;

            Function.Call(
                Hash.DRAW_RECT,
                dividerX,
                top + height * 0.61f,
                0.0008f,
                height * 0.31f,
                92,
                113,
                134,
                185
            );

            DrawReferenceProgressText(
                locationName,
                left + pad,
                top + height * 0.42f,
                0.72f,
                dividerX - left - pad - 0.007f,
                248,
                250,
                253,
                255,
                4,
                false
            );

            float timerWidth =
                left + width - pad - dividerX;

            DrawReferenceProgressText(
                FormatChaosHudClock(remainingMs),
                dividerX + (timerWidth * 0.5f),
                top + height * 0.42f,
                0.64f,
                timerWidth - 0.006f,
                248,
                250,
                253,
                255,
                4,
                true
            );

            DrawChaosHudTrack(
                left + pad,
                top + height * 0.88f,
                width - (pad * 2.0f),
                progress,
                ar,
                ag,
                ab
            );
        }

        private void DrawInstantGiftHUDLayoutDefault(
            int currentTime)
        {
            if (!_hudInstantEnabled ||
                IsNormalHudSuppressed())
                return;

            // Karena desain ini hanya satu slot, buang semua item selain item aktif.
            while (_instantGiftHudItems.Count > 1)
            {
                _instantGiftHudItems.RemoveAt(
                    _instantGiftHudItems.Count - 1
                );
            }

            for (
                int i =
                    _instantGiftHudItems.Count - 1;
                i >= 0;
                i--)
            {
                InstantGiftHudItem expiredItem =
                    _instantGiftHudItems[i];

                if (expiredItem == null ||
                    currentTime >=
                        expiredItem.EndTime)
                {
                    _instantGiftHudItems.RemoveAt(
                        i
                    );
                }
            }

            if (_instantGiftHudItems.Count == 0)
                return;

            InstantGiftHudItem activeItem =
                _instantGiftHudItems[0];

            float currentY =
                _instantHudStartY;

            int instantBgR =
                activeItem.IsGood
                    ? _instantGoodBgR
                    : _instantBadBgR;

            int instantBgG =
                activeItem.IsGood
                    ? _instantGoodBgG
                    : _instantBadBgG;

            int instantBgB =
                activeItem.IsGood
                    ? _instantGoodBgB
                    : _instantBadBgB;

            int ar = activeItem.IsGood ? _instantAccentGoodR : _instantAccentBadR;
            int ag = activeItem.IsGood ? _instantAccentGoodG : _instantAccentBadG;
            int ab = activeItem.IsGood ? _instantAccentGoodB : _instantAccentBadB;

            Function.Call(Hash.DRAW_RECT, _instantHudX, currentY,
                _instantHudBoxWidth, _instantHudBoxHeight, instantBgR, instantBgG, instantBgB, _instantHudBgA);
            DrawGiftOutline(_instantHudX, currentY, _instantHudBoxWidth,
                _instantHudBoxHeight, _instantHudBorderSize, _instantHudBorderR,
                _instantHudBorderG, _instantHudBorderB, _instantHudBorderA);

            float left = _instantHudX - _instantHudBoxWidth / 2f;
            Function.Call(Hash.DRAW_RECT, left + 0.002f, currentY, 0.002f,
                _instantHudBoxHeight - 0.006f, ar, ag, ab, 255);

            DrawReferenceProgressText(activeItem.Text, _instantHudX, currentY + _instantHudTextOffsetY,
                _instantHudTextScale, Math.Max(0.01f, _instantHudBoxWidth - 0.025f),
                _instantHudTextR, _instantHudTextG, _instantHudTextB, _instantHudTextA,
                _instantHudTextFont, true);
        }

        private void DrawActiveStatusHUDLayoutDefault(int currentTime)
        {
            if (IsNormalHudSuppressed())
                return;

            List<ActiveStatusItem> activeStatuses =
                BuildActiveStatusItems(currentTime);

            List<ActiveStatusItem> displayedStatuses = SelectActiveHudCards(activeStatuses);
            // Track the real remaining range for all running effects, even evicted cards.
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

            // INSTANT digambar sebagai sistem terpisah.
            DrawInstantGiftHUD(
                currentTime
            );

            if (!_hudTimerEnabled ||
                displayedStatuses.Count == 0)
                return;

            float startY =
                _statusHudStartY;

            float boxSpacing =
                _statusHudBoxSpacing;

            float boxWidth =
                _statusHudBoxWidth;

            float boxHeight =
                _statusHudBoxHeight;

            float borderSize =
                _statusHudBorderSize;

            for (
                int i = 0;
                i < displayedStatuses.Count;
                i++)
            {
                float currentY =
                    startY -
                    (
                        i *
                        boxSpacing
                    );

                ActiveStatusItem status =
                    displayedStatuses[i];

                bool timerIsGood =
                    status.IsGood;

                int timerBgR =
                    timerIsGood
                        ? _timerGoodBgR
                        : _timerBadBgR;

                int timerBgG =
                    timerIsGood
                        ? _timerGoodBgG
                        : _timerBadBgG;

                int timerBgB =
                    timerIsGood
                        ? _timerGoodBgB
                        : _timerBadBgB;

                int ar = timerIsGood ? _timerAccentGoodR : _timerAccentBadR;
                int ag = timerIsGood ? _timerAccentGoodG : _timerAccentBadG;
                int ab = timerIsGood ? _timerAccentGoodB : _timerAccentBadB;
                Function.Call(Hash.DRAW_RECT, _statusHudX, currentY, boxWidth, boxHeight,
                    timerBgR, timerBgG, timerBgB, _statusHudBgA);
                DrawGiftOutline(_statusHudX, currentY, boxWidth, boxHeight, borderSize,
                    _statusHudBorderR, _statusHudBorderG, _statusHudBorderB, _statusHudBorderA);
                float left = _statusHudX - boxWidth / 2f;
                float pad = 0.010f;
                Function.Call(Hash.DRAW_RECT, left + 0.002f, currentY,
                    0.002f, boxHeight - 0.006f, ar, ag, ab, 255);
                string label, timer;
                SplitTimerStatusText(status.Text, out label, out timer);
                int seconds;
                bool hasTimer = TryParseGiftTimerSeconds(timer, out seconds);
                if (hasTimer)
                {
                    float timeWidth = boxWidth * Math.Max(0.18f, Math.Min(0.40f, _giftTimerColumnRatio));
                    float dividerX = left + boxWidth - timeWidth;
                    Function.Call(Hash.DRAW_RECT, dividerX, currentY - 0.002f,
                        0.0006f, boxHeight * 0.60f, 92, 113, 134, 175);
                    DrawReferenceProgressText(label, left + pad, currentY + _statusHudTextOffsetY,
                        _statusHudTextScale, dividerX - left - pad - 0.006f,
                        _statusHudTextR, _statusHudTextG, _statusHudTextB, _statusHudTextA, _statusHudTextFont, false);
                    string clock = (seconds / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                        (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
                    DrawReferenceProgressText(clock, dividerX + timeWidth / 2f,
                        currentY + _giftTimerTimeOffsetY, _giftTimerTimeScale, timeWidth - 0.012f,
                        _statusHudTextR, _statusHudTextG, _statusHudTextB, _statusHudTextA, _statusHudTextFont, true);
                    int peak;
                    if (!_hudTimerPeakSeconds.TryGetValue(status.Key, out peak)) peak = Math.Max(1, seconds);
                    float fraction = Math.Max(0f, Math.Min(1f, seconds / (float)Math.Max(1, peak)));
                    float trackWidth = boxWidth - 2f * pad;
                    float trackHeight = Math.Max(0.002f, Math.Min(boxHeight * 0.12f, _giftTimerBarHeight));
                    float trackY = currentY + Math.Max(-boxHeight / 2f + trackHeight,
                        Math.Min(boxHeight / 2f - trackHeight, _giftTimerBarOffsetY));
                    Function.Call(Hash.DRAW_RECT, _statusHudX, trackY, trackWidth, trackHeight, 29, 39, 48, 255);
                    if (fraction > 0f) Function.Call(Hash.DRAW_RECT, left + pad + trackWidth * fraction / 2f,
                        trackY, trackWidth * fraction, trackHeight, ar, ag, ab, 255);
                }
                else
                {
                    DrawReferenceProgressText(label, left + pad, currentY + _statusHudTextOffsetY,
                        _statusHudTextScale, boxWidth - 2f * pad,
                        _statusHudTextR, _statusHudTextG, _statusHudTextB, _statusHudTextA, _statusHudTextFont, false);
                }
            }
        }
    }
}