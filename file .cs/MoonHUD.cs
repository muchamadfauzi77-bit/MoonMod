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
        private void LoadStartupPlayerSetupDesignFromIni()
        {
            string iniPath =
                "scripts/MoonHUD.ini";

            if (!File.Exists(iniPath))
                return;

            try
            {
                Dictionary<string, Dictionary<string, string>> design =
                    ReadHudDesignWithSafePreset(iniPath);

                const string section =
                    "HUD_PLAYER_SETUP";

                _startupSetupEnabled = GetDesignBool(design, section, "Enabled", _startupSetupEnabled);
                _startupNameMaxLength = GetDesignInt(design, section, "MaxLength", _startupNameMaxLength);
                _startupNameMaxWords = GetDesignInt(design, section, "MaxWords", _startupNameMaxWords);

                if (_startupNameMaxLength < 1)
                    _startupNameMaxLength = 1;

                if (_startupNameMaxLength > 120)
                    _startupNameMaxLength = 120;

                if (_startupNameMaxWords < 1)
                    _startupNameMaxWords = 1;

                if (_startupNameMaxWords > 20)
                    _startupNameMaxWords = 20;

                _startupOverlayR = GetDesignInt(design, section, "OverlayR", _startupOverlayR);
                _startupOverlayG = GetDesignInt(design, section, "OverlayG", _startupOverlayG);
                _startupOverlayB = GetDesignInt(design, section, "OverlayB", _startupOverlayB);
                _startupOverlayA = GetDesignInt(design, section, "OverlayA", _startupOverlayA);

                _startupNamePanelX = GetDesignFloat(design, section, "PosX", _startupNamePanelX);
                _startupNamePanelY = GetDesignFloat(design, section, "PosY", _startupNamePanelY);
                _startupNamePanelWidth = GetDesignFloat(design, section, "Width", _startupNamePanelWidth);
                _startupNamePanelHeight = GetDesignFloat(design, section, "Height", _startupNamePanelHeight);

                _startupPanelR = GetDesignInt(design, section, "BackgroundR", _startupPanelR);
                _startupPanelG = GetDesignInt(design, section, "BackgroundG", _startupPanelG);
                _startupPanelB = GetDesignInt(design, section, "BackgroundB", _startupPanelB);
                _startupPanelA = GetDesignInt(design, section, "BackgroundA", _startupPanelA);

                _startupPanelBorderR = GetDesignInt(design, section, "BorderR", _startupPanelBorderR);
                _startupPanelBorderG = GetDesignInt(design, section, "BorderG", _startupPanelBorderG);
                _startupPanelBorderB = GetDesignInt(design, section, "BorderB", _startupPanelBorderB);
                _startupPanelBorderA = GetDesignInt(design, section, "BorderA", _startupPanelBorderA);

                _startupAccentR = GetDesignInt(design, section, "AccentR", _startupAccentR);
                _startupAccentG = GetDesignInt(design, section, "AccentG", _startupAccentG);
                _startupAccentB = GetDesignInt(design, section, "AccentB", _startupAccentB);
                _startupAccentA = GetDesignInt(design, section, "AccentA", _startupAccentA);
                _startupAccentHeight = GetDesignFloat(design, section, "AccentHeight", _startupAccentHeight);

                _startupInstructionText = GetDesignString(design, section, "Instruction", _startupInstructionText);
                _startupInstructionOffsetY = GetDesignFloat(design, section, "InstructionOffsetY", _startupInstructionOffsetY);
                _startupInstructionScale = GetDesignFloat(design, section, "InstructionScale", _startupInstructionScale);
                _startupInstructionFont = GetDesignInt(design, section, "InstructionFont", _startupInstructionFont);
                _startupInstructionR = GetDesignInt(design, section, "InstructionR", _startupInstructionR);
                _startupInstructionG = GetDesignInt(design, section, "InstructionG", _startupInstructionG);
                _startupInstructionB = GetDesignInt(design, section, "InstructionB", _startupInstructionB);
                _startupInstructionA = GetDesignInt(design, section, "InstructionA", _startupInstructionA);

                _startupInputTitle = GetDesignString(design, section, "InputTitle", _startupInputTitle);
                _startupInputLabelEnabled = GetDesignBool(design, section, "InputLabelEnabled", _startupInputLabelEnabled);
                _startupInputLabelX = GetDesignFloat(design, section, "InputLabelPosX", _startupInputLabelX);
                _startupInputLabelY = GetDesignFloat(design, section, "InputLabelPosY", _startupInputLabelY);
                _startupInputLabelWidth = GetDesignFloat(design, section, "InputLabelWidth", _startupInputLabelWidth);
                _startupInputLabelHeight = GetDesignFloat(design, section, "InputLabelHeight", _startupInputLabelHeight);
                _startupInputLabelTextPaddingX = GetDesignFloat(design, section, "InputLabelTextPaddingX", _startupInputLabelTextPaddingX);
                _startupInputLabelTextOffsetY = GetDesignFloat(design, section, "InputLabelTextOffsetY", _startupInputLabelTextOffsetY);
                _startupInputLabelTextScale = GetDesignFloat(design, section, "InputLabelTextScale", _startupInputLabelTextScale);
                _startupInputLabelTextFont = GetDesignInt(design, section, "InputLabelTextFont", _startupInputLabelTextFont);
                _startupInputLabelTextR = GetDesignInt(design, section, "InputLabelTextR", _startupInputLabelTextR);
                _startupInputLabelTextG = GetDesignInt(design, section, "InputLabelTextG", _startupInputLabelTextG);
                _startupInputLabelTextB = GetDesignInt(design, section, "InputLabelTextB", _startupInputLabelTextB);
                _startupInputLabelTextA = GetDesignInt(design, section, "InputLabelTextA", _startupInputLabelTextA);
                _startupInputLabelBgR = GetDesignInt(design, section, "InputLabelBackgroundR", _startupInputLabelBgR);
                _startupInputLabelBgG = GetDesignInt(design, section, "InputLabelBackgroundG", _startupInputLabelBgG);
                _startupInputLabelBgB = GetDesignInt(design, section, "InputLabelBackgroundB", _startupInputLabelBgB);
                _startupInputLabelBgA = GetDesignInt(design, section, "InputLabelBackgroundA", _startupInputLabelBgA);

                _startupInputHint = GetDesignString(design, section, "InputHint", _startupInputHint);
                _startupInputPlaceholder = GetDesignString(design, section, "InputPlaceholder", _startupInputPlaceholder);
                _startupInputHintOffsetY = GetDesignFloat(design, section, "InputHintOffsetY", _startupInputHintOffsetY);
                _startupInputHintScale = GetDesignFloat(design, section, "InputHintScale", _startupInputHintScale);
                _startupInputHintR = GetDesignInt(design, section, "InputHintR", _startupInputHintR);
                _startupInputHintG = GetDesignInt(design, section, "InputHintG", _startupInputHintG);
                _startupInputHintB = GetDesignInt(design, section, "InputHintB", _startupInputHintB);
                _startupInputHintA = GetDesignInt(design, section, "InputHintA", _startupInputHintA);

                _startupInputBoxOffsetY = GetDesignFloat(design, section, "InputBoxOffsetY", _startupInputBoxOffsetY);
                _startupInputBoxWidth = GetDesignFloat(design, section, "InputBoxWidth", _startupInputBoxWidth);
                _startupInputBoxHeight = GetDesignFloat(design, section, "InputBoxHeight", _startupInputBoxHeight);
                _startupInputBoxR = GetDesignInt(design, section, "InputBoxR", _startupInputBoxR);
                _startupInputBoxG = GetDesignInt(design, section, "InputBoxG", _startupInputBoxG);
                _startupInputBoxB = GetDesignInt(design, section, "InputBoxB", _startupInputBoxB);
                _startupInputBoxA = GetDesignInt(design, section, "InputBoxA", _startupInputBoxA);
                _startupInputBoxBorderR = GetDesignInt(design, section, "InputBoxBorderR", _startupInputBoxBorderR);
                _startupInputBoxBorderG = GetDesignInt(design, section, "InputBoxBorderG", _startupInputBoxBorderG);
                _startupInputBoxBorderB = GetDesignInt(design, section, "InputBoxBorderB", _startupInputBoxBorderB);
                _startupInputBoxBorderA = GetDesignInt(design, section, "InputBoxBorderA", _startupInputBoxBorderA);
                _startupInputTextScale = GetDesignFloat(design, section, "InputTextScale", _startupInputTextScale);
                _startupInputTextFont = GetDesignInt(design, section, "InputTextFont", _startupInputTextFont);
                _startupInputTextR = GetDesignInt(design, section, "InputTextR", _startupInputTextR);
                _startupInputTextG = GetDesignInt(design, section, "InputTextG", _startupInputTextG);
                _startupInputTextB = GetDesignInt(design, section, "InputTextB", _startupInputTextB);
                _startupInputTextA = GetDesignInt(design, section, "InputTextA", _startupInputTextA);
                _startupInputPlaceholderR = GetDesignInt(design, section, "InputPlaceholderR", _startupInputPlaceholderR);
                _startupInputPlaceholderG = GetDesignInt(design, section, "InputPlaceholderG", _startupInputPlaceholderG);
                _startupInputPlaceholderB = GetDesignInt(design, section, "InputPlaceholderB", _startupInputPlaceholderB);
                _startupInputPlaceholderA = GetDesignInt(design, section, "InputPlaceholderA", _startupInputPlaceholderA);

                // Nama dropdown TIDAK lagi berasal dari file design.
                // Daftar host + data statistik dibaca dari ProfileHost.ini.

                _startupNameDropdownMaxVisible = Math.Max(
                    1,
                    Math.Min(
                        12,
                        GetDesignInt(
                            design,
                            section,
                            "DropdownMaxVisible",
                            _startupNameDropdownMaxVisible
                        )
                    )
                );

                _startupNameDropdownRowHeight = GetDesignFloat(
                    design, section, "DropdownRowHeight", _startupNameDropdownRowHeight);
                _startupNameDropdownGapY = GetDesignFloat(
                    design, section, "DropdownGapY", _startupNameDropdownGapY);
                _startupNameDropdownArrow = GetDesignString(
                    design, section, "DropdownArrow", _startupNameDropdownArrow);

                _startupStartButtonText = GetDesignString(
                    design, section, "StartButtonText", _startupStartButtonText);
                _startupStartButtonX = GetDesignFloat(
                    design, section, "StartButtonPosX", _startupStartButtonX);
                _startupStartButtonY = GetDesignFloat(
                    design, section, "StartButtonPosY", _startupStartButtonY);
                _startupStartButtonWidth = GetDesignFloat(
                    design, section, "StartButtonWidth", _startupStartButtonWidth);
                _startupStartButtonHeight = GetDesignFloat(
                    design, section, "StartButtonHeight", _startupStartButtonHeight);
                _startupStartButtonGap = GetDesignFloat(
                    design, section, "StartButtonGap", _startupStartButtonGap);
                _startupStartButtonTextScale = GetDesignFloat(
                    design, section, "StartButtonTextScale", _startupStartButtonTextScale);
                _startupStartButtonR = GetDesignInt(
                    design, section, "StartButtonR", _startupStartButtonR);
                _startupStartButtonG = GetDesignInt(
                    design, section, "StartButtonG", _startupStartButtonG);
                _startupStartButtonB = GetDesignInt(
                    design, section, "StartButtonB", _startupStartButtonB);
                _startupStartButtonA = GetDesignInt(
                    design, section, "StartButtonA", _startupStartButtonA);
                _startupStartButtonDisabledR = GetDesignInt(
                    design, section, "StartButtonDisabledR", _startupStartButtonDisabledR);
                _startupStartButtonDisabledG = GetDesignInt(
                    design, section, "StartButtonDisabledG", _startupStartButtonDisabledG);
                _startupStartButtonDisabledB = GetDesignInt(
                    design, section, "StartButtonDisabledB", _startupStartButtonDisabledB);
                _startupStartButtonDisabledA = GetDesignInt(
                    design, section, "StartButtonDisabledA", _startupStartButtonDisabledA);
                _startupStartButtonTextR = GetDesignInt(
                    design, section, "StartButtonTextR", _startupStartButtonTextR);
                _startupStartButtonTextG = GetDesignInt(
                    design, section, "StartButtonTextG", _startupStartButtonTextG);
                _startupStartButtonTextB = GetDesignInt(
                    design, section, "StartButtonTextB", _startupStartButtonTextB);
                _startupStartButtonTextA = GetDesignInt(
                    design, section, "StartButtonTextA", _startupStartButtonTextA);

                _startupProfileTitle = GetDesignString(
                    design, section, "ProfileTitle", _startupProfileTitle);
                _startupProfilePanelX = GetDesignFloat(
                    design, section, "ProfilePosX", _startupProfilePanelX);
                _startupProfilePanelY = GetDesignFloat(
                    design, section, "ProfilePosY", _startupProfilePanelY);
                _startupProfilePanelWidth = GetDesignFloat(
                    design, section, "ProfileWidth", _startupProfilePanelWidth);
                _startupProfilePanelHeight = GetDesignFloat(
                    design, section, "ProfileHeight", _startupProfilePanelHeight);
                _startupProfileTitleOffsetY = GetDesignFloat(
                    design, section, "ProfileTitleOffsetY", _startupProfileTitleOffsetY);
                _startupProfileFirstRowOffsetY = GetDesignFloat(
                    design, section, "ProfileFirstRowOffsetY", _startupProfileFirstRowOffsetY);
                _startupProfileRowSpacing = GetDesignFloat(
                    design, section, "ProfileRowSpacing", _startupProfileRowSpacing);
                _startupProfileLabelPaddingX = GetDesignFloat(
                    design, section, "ProfileLabelPaddingX", _startupProfileLabelPaddingX);
                _startupProfileValueOffsetX = GetDesignFloat(
                    design, section, "ProfileValueOffsetX", _startupProfileValueOffsetX);
                _startupProfileLabelScale = GetDesignFloat(
                    design, section, "ProfileLabelScale", _startupProfileLabelScale);
                _startupProfileValueScale = GetDesignFloat(
                    design, section, "ProfileValueScale", _startupProfileValueScale);
                _startupProfileFont = GetDesignInt(
                    design, section, "ProfileFont", _startupProfileFont);

                _startupProfileLabelR = GetDesignInt(design, section, "ProfileLabelR", _startupProfileLabelR);
                _startupProfileLabelG = GetDesignInt(design, section, "ProfileLabelG", _startupProfileLabelG);
                _startupProfileLabelB = GetDesignInt(design, section, "ProfileLabelB", _startupProfileLabelB);
                _startupProfileLabelA = GetDesignInt(design, section, "ProfileLabelA", _startupProfileLabelA);
                _startupProfileNameR = GetDesignInt(design, section, "ProfileNameR", _startupProfileNameR);
                _startupProfileNameG = GetDesignInt(design, section, "ProfileNameG", _startupProfileNameG);
                _startupProfileNameB = GetDesignInt(design, section, "ProfileNameB", _startupProfileNameB);
                _startupProfileNameA = GetDesignInt(design, section, "ProfileNameA", _startupProfileNameA);
                _startupProfilePositiveR = GetDesignInt(design, section, "ProfilePositiveR", _startupProfilePositiveR);
                _startupProfilePositiveG = GetDesignInt(design, section, "ProfilePositiveG", _startupProfilePositiveG);
                _startupProfilePositiveB = GetDesignInt(design, section, "ProfilePositiveB", _startupProfilePositiveB);
                _startupProfilePositiveA = GetDesignInt(design, section, "ProfilePositiveA", _startupProfilePositiveA);
                _startupProfileNegativeR = GetDesignInt(design, section, "ProfileNegativeR", _startupProfileNegativeR);
                _startupProfileNegativeG = GetDesignInt(design, section, "ProfileNegativeG", _startupProfileNegativeG);
                _startupProfileNegativeB = GetDesignInt(design, section, "ProfileNegativeB", _startupProfileNegativeB);
                _startupProfileNegativeA = GetDesignInt(design, section, "ProfileNegativeA", _startupProfileNegativeA);
                _startupProfileNeutralR = GetDesignInt(design, section, "ProfileNeutralR", _startupProfileNeutralR);
                _startupProfileNeutralG = GetDesignInt(design, section, "ProfileNeutralG", _startupProfileNeutralG);
                _startupProfileNeutralB = GetDesignInt(design, section, "ProfileNeutralB", _startupProfileNeutralB);
                _startupProfileNeutralA = GetDesignInt(design, section, "ProfileNeutralA", _startupProfileNeutralA);
                _startupProfileKillR = GetDesignInt(design, section, "ProfileKillR", _startupProfileKillR);
                _startupProfileKillG = GetDesignInt(design, section, "ProfileKillG", _startupProfileKillG);
                _startupProfileKillB = GetDesignInt(design, section, "ProfileKillB", _startupProfileKillB);
                _startupProfileKillA = GetDesignInt(design, section, "ProfileKillA", _startupProfileKillA);

                _startupInputFooterText = GetDesignString(design, section, "InputFooterText", _startupInputFooterText);
                _startupInputFooterOffsetY = GetDesignFloat(design, section, "InputFooterOffsetY", _startupInputFooterOffsetY);
                _startupInputFooterScale = GetDesignFloat(design, section, "InputFooterScale", _startupInputFooterScale);
                _startupInputFooterR = GetDesignInt(design, section, "InputFooterR", _startupInputFooterR);
                _startupInputFooterG = GetDesignInt(design, section, "InputFooterG", _startupInputFooterG);
                _startupInputFooterB = GetDesignInt(design, section, "InputFooterB", _startupInputFooterB);
                _startupInputFooterA = GetDesignInt(design, section, "InputFooterA", _startupInputFooterA);

                _startupInputCounterEnabled = GetDesignBool(design, section, "InputCounterEnabled", _startupInputCounterEnabled);
                _startupInputCounterOffsetY = GetDesignFloat(design, section, "InputCounterOffsetY", _startupInputCounterOffsetY);
                _startupInputCounterScale = GetDesignFloat(design, section, "InputCounterScale", _startupInputCounterScale);
                _startupInputCounterR = GetDesignInt(design, section, "InputCounterR", _startupInputCounterR);
                _startupInputCounterG = GetDesignInt(design, section, "InputCounterG", _startupInputCounterG);
                _startupInputCounterB = GetDesignInt(design, section, "InputCounterB", _startupInputCounterB);
                _startupInputCounterA = GetDesignInt(design, section, "InputCounterA", _startupInputCounterA);

                _startupInputWarningText = GetDesignString(design, section, "InputWarningText", _startupInputWarningText);
                _startupInputWarningDurationMs = GetDesignInt(design, section, "InputWarningDurationMs", _startupInputWarningDurationMs);
                _startupInputWarningOffsetY = GetDesignFloat(design, section, "InputWarningOffsetY", _startupInputWarningOffsetY);
                _startupInputWarningScale = GetDesignFloat(design, section, "InputWarningScale", _startupInputWarningScale);
                _startupInputWarningR = GetDesignInt(design, section, "InputWarningR", _startupInputWarningR);
                _startupInputWarningG = GetDesignInt(design, section, "InputWarningG", _startupInputWarningG);
                _startupInputWarningB = GetDesignInt(design, section, "InputWarningB", _startupInputWarningB);
                _startupInputWarningA = GetDesignInt(design, section, "InputWarningA", _startupInputWarningA);
                _startupInputWarningFlashField = GetDesignBool(design, section, "InputWarningFlashField", _startupInputWarningFlashField);

                _startupInputActiveBarWidth = GetDesignFloat(design, section, "InputActiveBarWidth", _startupInputActiveBarWidth);
                _startupInputActiveUnderlineHeight = GetDesignFloat(design, section, "InputActiveUnderlineHeight", _startupInputActiveUnderlineHeight);
                _startupInputShadowOffsetX = GetDesignFloat(design, section, "InputShadowOffsetX", _startupInputShadowOffsetX);
                _startupInputShadowOffsetY = GetDesignFloat(design, section, "InputShadowOffsetY", _startupInputShadowOffsetY);

                _playerNameTagEnabled = GetDesignBool(design, section, "NameTagEnabled", _playerNameTagEnabled);
                _playerNameTagHeadOffsetZ = GetDesignFloat(design, section, "NameTagHeadOffsetZ", _playerNameTagHeadOffsetZ);
                _playerNameTagScale = GetDesignFloat(design, section, "NameTagScale", _playerNameTagScale);
                _playerNameTagFont = GetDesignInt(design, section, "NameTagFont", _playerNameTagFont);
                _playerNameTagMaxDistance = GetDesignFloat(design, section, "NameTagMaxDistance", _playerNameTagMaxDistance);

                _playerNameTagR = GetDesignInt(design, section, "NameTagTextR", _playerNameTagR);
                _playerNameTagG = GetDesignInt(design, section, "NameTagTextG", _playerNameTagG);
                _playerNameTagB = GetDesignInt(design, section, "NameTagTextB", _playerNameTagB);
                _playerNameTagA = GetDesignInt(design, section, "NameTagTextA", _playerNameTagA);

                _playerNameTagBgR = GetDesignInt(design, section, "NameTagBackgroundR", _playerNameTagBgR);
                _playerNameTagBgG = GetDesignInt(design, section, "NameTagBackgroundG", _playerNameTagBgG);
                _playerNameTagBgB = GetDesignInt(design, section, "NameTagBackgroundB", _playerNameTagBgB);
                _playerNameTagBgA = GetDesignInt(design, section, "NameTagBackgroundA", _playerNameTagBgA);

                _playerNameTagAccentR = GetDesignInt(design, section, "NameTagAccentR", _playerNameTagAccentR);
                _playerNameTagAccentG = GetDesignInt(design, section, "NameTagAccentG", _playerNameTagAccentG);
                _playerNameTagAccentB = GetDesignInt(design, section, "NameTagAccentB", _playerNameTagAccentB);
                _playerNameTagAccentA = GetDesignInt(design, section, "NameTagAccentA", _playerNameTagAccentA);
            }
            catch (Exception ex)
            {
                LogError(
                    "PLAYER SETUP DESIGN ERROR",
                    ex
                );
            }
        }

        private void ProcessStartupPlayerNameInput()
        {
            // Safe UI pause: TimeScale=0 keeps the world frozen while ScriptHookVDotNet
            // can still Tick and draw the mouse UI. A native hard pause can stop script input.
            Function.Call(Hash.SET_TIME_SCALE, 0.0f);

            Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 1);
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 2);

            // GTA cursor while controls are disabled.
            Function.Call(unchecked((Hash)0xAAE7CE1D63167423UL));

            // Dark full-screen background.
            Function.Call(
                Hash.DRAW_RECT,
                0.5f,
                0.5f,
                1.0f,
                1.0f,
                _startupOverlayR,
                _startupOverlayG,
                _startupOverlayB,
                Math.Max(0, Math.Min(255, _startupOverlayA))
            );

            if (!_startupSetupEnabled)
                return;

            if (_startupUiStage == StartupUiStage.Profile)
            {
                HandleStartupNameMouseInput();
                DrawStartupNamePanel();
                DrawStartupCustomInputBox();
            }
            else if (_startupUiStage == StartupUiStage.Gameplay)
            {
                HandleStartupGameplayMouseInput();
                DrawStartupGameplayModePanel();
            }
            else
            {
                HandleStartupDifficultyMouseInput();
                DrawStartupDifficultyPanel();
            }
        }

        private void ProcessStartupLoadingScreen()
        {
            // IMPORTANT: selama LOADING world harus tetap berjalan agar collision,
            // navmesh, road-node, dan Game.GameTime dapat maju. Gameplay tetap tidak
            // terlihat karena black overlay 255 + semua control dimatikan.
            Function.Call(Hash.SET_TIME_SCALE, 1.0f);
            Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 1);
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 2);

            // Black screen full opaque. Background gameplay GTA tidak boleh tembus.
            Function.Call(
                Hash.DRAW_RECT,
                0.5f, 0.5f, 1.0f, 1.0f,
                _startupLoadingBgR,
                _startupLoadingBgG,
                _startupLoadingBgB,
                Math.Max(0, Math.Min(255, _startupLoadingBgA))
            );

            if (!_startupLoadingTextEnabled)
                return;

            string loadingText = _startupLoadingText ?? "LOADING";
            if (_startupLoadingAnimatedDots)
            {
                int dotCount = ((Game.GameTime / 350) % 3) + 1;
                loadingText += new string('.', dotCount);
            }

            DrawStartupText(
                loadingText,
                _startupLoadingTextX,
                _startupLoadingTextY,
                _startupLoadingTextScale,
                _startupLoadingTextR,
                _startupLoadingTextG,
                _startupLoadingTextB,
                _startupLoadingTextA,
                _startupLoadingTextFont
            );

            string modeName =
                _selectedGameplayMode == MoonGameplayMode.Survival
                    ? "SURVIVAL"
                    : "GO TO MOUNTAIN";

            string subtitle = _startupLoadingSubtitle ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                subtitle = subtitle.Replace("{MODE}", modeName);
                DrawStartupText(
                    subtitle,
                    0.500f,
                    _startupLoadingSubtitleY,
                    _startupLoadingSubtitleScale,
                    _startupLoadingSubtitleR,
                    _startupLoadingSubtitleG,
                    _startupLoadingSubtitleB,
                    _startupLoadingSubtitleA,
                    _startupLoadingTextFont
                );
            }
        }

        private float GetStartupMouseX()
        {
            return Math.Max(
                0.0f,
                Math.Min(
                    1.0f,
                    Function.Call<float>(
                        Hash.GET_DISABLED_CONTROL_NORMAL,
                        0,
                        239 // INPUT_CURSOR_X
                    )
                )
            );
        }

        private float GetStartupMouseY()
        {
            return Math.Max(
                0.0f,
                Math.Min(
                    1.0f,
                    Function.Call<float>(
                        Hash.GET_DISABLED_CONTROL_NORMAL,
                        0,
                        240 // INPUT_CURSOR_Y
                    )
                )
            );
        }

        private bool IsStartupMouseClick()
        {
            return Function.Call<bool>(
                Hash.IS_DISABLED_CONTROL_JUST_PRESSED,
                0,
                237 // INPUT_CURSOR_ACCEPT / left mouse
            );
        }

        private bool IsStartupPointInside(
            float px,
            float py,
            float centerX,
            float centerY,
            float width,
            float height)
        {
            return
                px >= centerX - (width * 0.5f) &&
                px <= centerX + (width * 0.5f) &&
                py >= centerY - (height * 0.5f) &&
                py <= centerY + (height * 0.5f);
        }

        private void GetStartupNameControlLayout(
            out float dropdownX,
            out float dropdownY,
            out float startX,
            out float startY)
        {
            // Dropdown berdiri sendiri di panel kiri.
            dropdownX =
                _startupInputLabelX;

            dropdownY =
                _startupInputLabelY +
                _startupInputBoxOffsetY;

            // START berdiri sendiri di area kanan bawah.
            // Posisi tidak lagi dipaksa satu baris dengan dropdown.
            startX =
                _startupStartButtonX;

            startY =
                _startupStartButtonY;
        }

        private void HandleStartupNameMouseInput()
        {
            if (_startupUiStage != StartupUiStage.Profile)
                return;

            float mouseX = GetStartupMouseX();
            float mouseY = GetStartupMouseY();

            float dropdownX;
            float dropdownY;
            float nextX;
            float nextY;

            GetStartupNameControlLayout(
                out dropdownX,
                out dropdownY,
                out nextX,
                out nextY
            );

            int visibleCount = Math.Min(
                _startupNameDropdownMaxVisible,
                _startupNameOptions.Count
            );

            int maxScrollOffset = Math.Max(
                0,
                _startupNameOptions.Count - visibleCount
            );

            if (_startupNameDropdownOpen && maxScrollOffset > 0)
            {
                bool scrollUp = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 241);
                bool scrollDown = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 242);

                if (scrollUp)
                    _startupNameDropdownScrollOffset = Math.Max(0, _startupNameDropdownScrollOffset - 1);
                if (scrollDown)
                    _startupNameDropdownScrollOffset = Math.Min(maxScrollOffset, _startupNameDropdownScrollOffset + 1);
            }

            if (!IsStartupMouseClick())
                return;

            if (_startupNameDropdownOpen)
            {
                float firstRowY =
                    dropdownY +
                    (_startupInputBoxHeight * 0.5f) +
                    _startupNameDropdownGapY +
                    (_startupNameDropdownRowHeight * 0.5f);

                for (int row = 0; row < visibleCount; row++)
                {
                    int optionIndex = _startupNameDropdownScrollOffset + row;
                    if (optionIndex >= _startupNameOptions.Count)
                        break;

                    float rowY = firstRowY + (row * _startupNameDropdownRowHeight);
                    if (IsStartupPointInside(mouseX, mouseY, dropdownX, rowY,
                        _startupInputBoxWidth, _startupNameDropdownRowHeight))
                    {
                        _startupNameDraft = _startupNameOptions[optionIndex];
                        _selectedHostProfileIndex = optionIndex;
                        _startupNameDropdownOpen = false;
                        return;
                    }
                }
            }

            if (IsStartupPointInside(mouseX, mouseY, dropdownX, dropdownY,
                _startupInputBoxWidth, _startupInputBoxHeight))
            {
                _startupNameDropdownOpen = !_startupNameDropdownOpen;
                return;
            }

            // FULL mode: PROFILE -> NEXT.
            // Fixed gotomountain/survival: PROFILE -> START langsung gameplay.
            if (GetSelectedHostProfile() != null &&
                IsStartupPointInside(mouseX, mouseY, nextX, nextY,
                    _startupStartButtonWidth, _startupStartButtonHeight))
            {
                string cleanName = NormalizeStartupPlayerName(_startupNameDraft);
                if (!string.IsNullOrWhiteSpace(cleanName))
                {
                    _startupNameDropdownOpen = false;
                    AdvanceStartupAfterProfileSelection();
                }
                return;
            }

            _startupNameDropdownOpen = false;
        }

        private int CountStartupNameWords(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            return value
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Length;
        }

        private string NormalizeStartupPlayerName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(
                " ",
                value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            ).Trim();
        }

        private void ShowStartupNameWordLimitWarning()
        {
            _startupInputWarningEndTime =
                Game.GameTime + Math.Max(250, _startupInputWarningDurationMs);
        }

        private void OnStartupNameKeyDown(object sender, KeyEventArgs e)
        {
            if (_startupNameConfirmed || !_startupSetupEnabled)
            {
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                // IMPORTANT: validasi SELURUH draft lebih dulu.
                // Jangan truncate sebelum menghitung kata, karena nama 9+ kata
                // bisa terpotong menjadi <= 8 kata lalu lolos ENTER.
                string cleanName = NormalizeStartupPlayerName(_startupNameDraft);
                int cleanWordCount = CountStartupNameWords(cleanName);

                if (cleanName.Length <= 0)
                {
                    ShowNotificationCompat("~r~NAMA TIDAK BOLEH KOSONG");
                    return;
                }

                // HARD BLOCK: lebih dari MaxWords TIDAK BOLEH lanjut gameplay.
                if (cleanWordCount > _startupNameMaxWords)
                {
                    ShowStartupNameWordLimitWarning();
                    return;
                }

                // Safety character limit. Keyboard normal sudah dibatasi saat mengetik,
                // tetapi check ini tetap dipertahankan sebagai validasi terakhir.
                if (cleanName.Length > _startupNameMaxLength)
                {
                    return;
                }

                _startupPlayerName = cleanName;
                _startupNameConfirmed = true;

                ShowNotificationCompat(
                    "~b~PLAYER NAME~w~ : " + _startupPlayerName
                );

                return;
            }

            if (e.KeyCode == Keys.Back)
            {
                if (!string.IsNullOrEmpty(_startupNameDraft))
                {
                    _startupNameDraft = _startupNameDraft.Substring(
                        0,
                        _startupNameDraft.Length - 1
                    );

                    if (CountStartupNameWords(_startupNameDraft) <= _startupNameMaxWords)
                    {
                        _startupInputWarningEndTime = 0;
                    }
                }

                return;
            }

            // ESC hanya membersihkan input, tidak menutup profile setup.
            if (e.KeyCode == Keys.Escape)
            {
                _startupNameDraft = string.Empty;
                _startupInputWarningEndTime = 0;
                return;
            }

            if (_startupNameDraft.Length >= _startupNameMaxLength)
            {
                return;
            }

            char? typedCharacter = GetStartupNameCharacter(e);

            if (typedCharacter.HasValue)
            {
                char c = typedCharacter.Value;

                // Rapikan spasi: tidak boleh diawali spasi atau memakai spasi ganda.
                if (c == ' ')
                {
                    if (string.IsNullOrEmpty(_startupNameDraft) ||
                        _startupNameDraft[_startupNameDraft.Length - 1] == ' ')
                    {
                        return;
                    }

                    // Spasi tetap dirapikan, tetapi kata ke-9 sengaja dibiarkan
                    // terlihat di field supaya user mendapat state INVALID yang jelas.
                }

                string candidate = _startupNameDraft + c;
                _startupNameDraft = candidate;

                // Jika karakter ini membuat nama menjadi kata ke-9 atau lebih,
                // jangan hapus inputnya. Tandai INVALID dan blok ENTER sampai
                // user menghapus kembali ke <= MaxWords.
                if (CountStartupNameWords(_startupNameDraft) > _startupNameMaxWords)
                {
                    ShowStartupNameWordLimitWarning();
                }
            }
        }

        private char? GetStartupNameCharacter(KeyEventArgs e)
        {
            int keyCode = (int)e.KeyCode;

            if (keyCode >= (int)Keys.A && keyCode <= (int)Keys.Z)
            {
                char c = (char)('A' + (keyCode - (int)Keys.A));
                bool upper = e.Shift ^ System.Windows.Forms.Control.IsKeyLocked(Keys.CapsLock);
                return upper ? c : char.ToLowerInvariant(c);
            }

            if (keyCode >= (int)Keys.D0 && keyCode <= (int)Keys.D9)
            {
                int digit = keyCode - (int)Keys.D0;

                if (!e.Shift)
                {
                    return (char)('0' + digit);
                }

                // Shift + angka yang umum dipakai pada nama/profile.
                string shifted = ")!@#$%^&*(";
                return shifted[digit];
            }

            if (keyCode >= (int)Keys.NumPad0 && keyCode <= (int)Keys.NumPad9)
            {
                return (char)('0' + (keyCode - (int)Keys.NumPad0));
            }

            if (e.KeyCode == Keys.Space)
                return ' ';

            if (e.KeyCode == Keys.OemMinus)
                return e.Shift ? '_' : '-';

            if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Decimal)
                return '.';

            return null;
        }

        private void DrawStartupNamePanel()
        {
            float x =
                _startupNamePanelX;

            float y =
                _startupNamePanelY;

            float width =
                _startupNamePanelWidth;

            float height =
                _startupNamePanelHeight;

            // Shadow.
            Function.Call(
                Hash.DRAW_RECT,
                x + 0.0025f,
                y + 0.0035f,
                width,
                height,
                0,
                0,
                0,
                120
            );

            // Panel utama: sengaja pendek dan sejajar dengan kotak input native.
            Function.Call(
                Hash.DRAW_RECT,
                x,
                y,
                width,
                height,
                _startupPanelR,
                _startupPanelG,
                _startupPanelB,
                _startupPanelA
            );

            // Border tipis.
            float borderSize =
                0.0015f;

            Function.Call(
                Hash.DRAW_RECT,
                x,
                y - (height * 0.5f),
                width,
                borderSize,
                _startupPanelBorderR,
                _startupPanelBorderG,
                _startupPanelBorderB,
                _startupPanelBorderA
            );

            Function.Call(
                Hash.DRAW_RECT,
                x,
                y + (height * 0.5f),
                width,
                borderSize,
                _startupPanelBorderR,
                _startupPanelBorderG,
                _startupPanelBorderB,
                _startupPanelBorderA
            );

            Function.Call(
                Hash.DRAW_RECT,
                x - (width * 0.5f),
                y,
                borderSize,
                height,
                _startupPanelBorderR,
                _startupPanelBorderG,
                _startupPanelBorderB,
                _startupPanelBorderA
            );

            Function.Call(
                Hash.DRAW_RECT,
                x + (width * 0.5f),
                y,
                borderSize,
                height,
                _startupPanelBorderR,
                _startupPanelBorderG,
                _startupPanelBorderB,
                _startupPanelBorderA
            );

            // Accent divider di bawah judul PLAYER PROFILE.
            Function.Call(
                Hash.DRAW_RECT,
                x,
                y - (height * 0.5f) + 0.155f,
                Math.Max(0.05f, width - 0.060f),
                _startupAccentHeight,
                _startupAccentR,
                _startupAccentG,
                _startupAccentB,
                _startupAccentA
            );

            DrawStartupText(
                _startupInstructionText,
                x,
                y + _startupInstructionOffsetY,
                _startupInstructionScale,
                _startupInstructionR,
                _startupInstructionG,
                _startupInstructionB,
                _startupInstructionA,
                _startupInstructionFont
            );
        }

        private void DrawStartupCustomInputBox()
        {
            if (!_startupInputLabelEnabled)
                return;

            float x = _startupInputLabelX;
            float y = _startupInputLabelY;
            float width = _startupInputLabelWidth;
            float height = _startupInputLabelHeight;

            float leftEdge = x - (width * 0.5f);
            float rightEdge = x + (width * 0.5f);
            float topEdge = y - (height * 0.5f);
            float bottomEdge = y + (height * 0.5f);

            // Box selector host besar di bawah judul.
            Function.Call(
                Hash.DRAW_RECT,
                x + _startupInputShadowOffsetX,
                y + _startupInputShadowOffsetY,
                width,
                height,
                0, 0, 0, 105
            );

            Function.Call(
                Hash.DRAW_RECT,
                x,
                y,
                width,
                height,
                _startupInputLabelBgR,
                _startupInputLabelBgG,
                _startupInputLabelBgB,
                _startupInputLabelBgA
            );

            Function.Call(
                Hash.DRAW_RECT,
                x,
                topEdge + (_startupAccentHeight * 0.5f),
                width,
                _startupAccentHeight,
                _startupAccentR,
                _startupAccentG,
                _startupAccentB,
                _startupAccentA
            );

            const float outerBorder = 0.0012f;
            Function.Call(Hash.DRAW_RECT, x, topEdge, width, outerBorder,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 150);
            Function.Call(Hash.DRAW_RECT, x, bottomEdge, width, outerBorder,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 150);
            Function.Call(Hash.DRAW_RECT, leftEdge, y, outerBorder, height,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 150);
            Function.Call(Hash.DRAW_RECT, rightEdge, y, outerBorder, height,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 150);

            float leftX = leftEdge + _startupInputLabelTextPaddingX;

            if (!string.IsNullOrWhiteSpace(_startupInputTitle))
            {
                DrawStartupTextLeft(
                    _startupInputTitle,
                    leftX,
                    y + _startupInputLabelTextOffsetY,
                    _startupInputLabelTextScale,
                    _startupInputLabelTextR,
                    _startupInputLabelTextG,
                    _startupInputLabelTextB,
                    _startupInputLabelTextA,
                    _startupInputLabelTextFont
                );
            }

            if (!string.IsNullOrWhiteSpace(_startupInputHint))
            {
                DrawStartupTextLeft(
                    _startupInputHint,
                    leftX,
                    y + _startupInputHintOffsetY,
                    _startupInputHintScale,
                    _startupInputHintR,
                    _startupInputHintG,
                    _startupInputHintB,
                    _startupInputHintA,
                    _startupInputLabelTextFont
                );
            }

            float dropdownX;
            float dropdownY;
            float startX;
            float startY;

            GetStartupNameControlLayout(
                out dropdownX,
                out dropdownY,
                out startX,
                out startY
            );

            float mouseX = GetStartupMouseX();
            float mouseY = GetStartupMouseY();

            bool dropdownHovered = IsStartupPointInside(
                mouseX,
                mouseY,
                dropdownX,
                dropdownY,
                _startupInputBoxWidth,
                _startupInputBoxHeight
            );

            bool hasSelection = GetSelectedHostProfile() != null;

            bool startHovered =
                hasSelection &&
                IsStartupPointInside(
                    mouseX,
                    mouseY,
                    startX,
                    startY,
                    _startupStartButtonWidth,
                    _startupStartButtonHeight
                );

            // DROPDOWN
            Function.Call(
                Hash.DRAW_RECT,
                dropdownX + 0.0015f,
                dropdownY + 0.0025f,
                _startupInputBoxWidth + 0.0040f,
                _startupInputBoxHeight + 0.0050f,
                0, 0, 0, 120
            );

            Function.Call(
                Hash.DRAW_RECT,
                dropdownX,
                dropdownY,
                _startupInputBoxWidth + 0.0025f,
                _startupInputBoxHeight + 0.0035f,
                dropdownHovered || _startupNameDropdownOpen ? _startupAccentR : _startupInputBoxBorderR,
                dropdownHovered || _startupNameDropdownOpen ? _startupAccentG : _startupInputBoxBorderG,
                dropdownHovered || _startupNameDropdownOpen ? _startupAccentB : _startupInputBoxBorderB,
                _startupInputBoxBorderA
            );

            Function.Call(
                Hash.DRAW_RECT,
                dropdownX,
                dropdownY,
                _startupInputBoxWidth,
                _startupInputBoxHeight,
                _startupInputBoxR,
                _startupInputBoxG,
                _startupInputBoxB,
                _startupInputBoxA
            );

            float dropdownLeft = dropdownX - (_startupInputBoxWidth * 0.5f);
            string visibleName = hasSelection
                ? GetSelectedHostProfile().Name
                : _startupInputPlaceholder;

            DrawStartupTextLeft(
                visibleName,
                dropdownLeft + 0.016f,
                dropdownY - 0.0155f,
                _startupInputTextScale,
                hasSelection ? _startupProfileNameR : _startupInputPlaceholderR,
                hasSelection ? _startupProfileNameG : _startupInputPlaceholderG,
                hasSelection ? _startupProfileNameB : _startupInputPlaceholderB,
                hasSelection ? _startupProfileNameA : _startupInputPlaceholderA,
                _startupInputTextFont
            );

            DrawStartupText(
                _startupNameDropdownOpen ? "^" : _startupNameDropdownArrow,
                dropdownX + (_startupInputBoxWidth * 0.5f) - 0.028f,
                dropdownY - 0.0155f,
                _startupInputTextScale * 0.90f,
                _startupInputTextR,
                _startupInputTextG,
                _startupInputTextB,
                _startupInputTextA,
                _startupInputTextFont
            );

            // PROFILE INFORMATION di kolom kanan digambar dulu,
            // lalu tombol START digambar di atas panel agar selalu terlihat.
            DrawStartupProfileInformation();

            // START BUTTON di kanan bawah.
            int startR = hasSelection ? _startupStartButtonR : _startupStartButtonDisabledR;
            int startG = hasSelection ? _startupStartButtonG : _startupStartButtonDisabledG;
            int startB = hasSelection ? _startupStartButtonB : _startupStartButtonDisabledB;
            int startA = hasSelection ? _startupStartButtonA : _startupStartButtonDisabledA;

            if (startHovered)
            {
                startR = Math.Min(255, startR + 18);
                startG = Math.Min(255, startG + 18);
                startB = Math.Min(255, startB + 18);
            }

            Function.Call(
                Hash.DRAW_RECT,
                startX + 0.0015f,
                startY + 0.0025f,
                _startupStartButtonWidth + 0.0040f,
                _startupStartButtonHeight + 0.0050f,
                0, 0, 0, 120
            );

            Function.Call(
                Hash.DRAW_RECT,
                startX,
                startY,
                _startupStartButtonWidth,
                _startupStartButtonHeight,
                startR,
                startG,
                startB,
                startA
            );

            string profileActionText =
                IsFixedGameplayModeFromConfig()
                    ? "START"
                    : _startupStartButtonText;

            DrawStartupText(
                profileActionText,
                startX,
                startY - 0.0215f,
                _startupStartButtonTextScale,
                hasSelection ? _startupStartButtonTextR : _startupInputPlaceholderR,
                hasSelection ? _startupStartButtonTextG : _startupInputPlaceholderG,
                hasSelection ? _startupStartButtonTextB : _startupInputPlaceholderB,
                hasSelection ? _startupStartButtonTextA : _startupInputPlaceholderA,
                _startupInputTextFont
            );

            // Dropdown list digambar TERAKHIR supaya selalu berada di atas panel lain.
            if (_startupNameDropdownOpen)
            {
                int visibleCount = Math.Min(
                    _startupNameDropdownMaxVisible,
                    _startupNameOptions.Count
                );

                int maxOffset = Math.Max(
                    0,
                    _startupNameOptions.Count - visibleCount
                );

                _startupNameDropdownScrollOffset = Math.Max(
                    0,
                    Math.Min(maxOffset, _startupNameDropdownScrollOffset)
                );

                float firstRowY =
                    dropdownY +
                    (_startupInputBoxHeight * 0.5f) +
                    _startupNameDropdownGapY +
                    (_startupNameDropdownRowHeight * 0.5f);

                for (int row = 0; row < visibleCount; row++)
                {
                    int optionIndex = _startupNameDropdownScrollOffset + row;
                    if (optionIndex >= _startupNameOptions.Count)
                        break;

                    float rowY = firstRowY + (row * _startupNameDropdownRowHeight);
                    bool rowHovered = IsStartupPointInside(
                        mouseX,
                        mouseY,
                        dropdownX,
                        rowY,
                        _startupInputBoxWidth,
                        _startupNameDropdownRowHeight
                    );

                    bool rowSelected =
                        optionIndex == _selectedHostProfileIndex;

                    Function.Call(
                        Hash.DRAW_RECT,
                        dropdownX,
                        rowY,
                        _startupInputBoxWidth,
                        _startupNameDropdownRowHeight - 0.001f,
                        rowHovered || rowSelected ? 24 : _startupInputBoxR,
                        rowHovered || rowSelected ? 47 : _startupInputBoxG,
                        rowHovered || rowSelected ? 61 : _startupInputBoxB,
                        252
                    );

                    if (rowHovered || rowSelected)
                    {
                        Function.Call(
                            Hash.DRAW_RECT,
                            dropdownX - (_startupInputBoxWidth * 0.5f) + 0.0025f,
                            rowY,
                            0.005f,
                            _startupNameDropdownRowHeight - 0.004f,
                            _startupAccentR,
                            _startupAccentG,
                            _startupAccentB,
                            255
                        );
                    }

                    DrawStartupTextLeft(
                        _startupNameOptions[optionIndex],
                        dropdownLeft + 0.016f,
                        rowY - 0.0155f,
                        _startupInputTextScale,
                        rowSelected ? _startupProfileNameR : _startupInputTextR,
                        rowSelected ? _startupProfileNameG : _startupInputTextG,
                        rowSelected ? _startupProfileNameB : _startupInputTextB,
                        _startupInputTextA,
                        _startupInputTextFont
                    );
                }
            }
        }

        private void DrawStartupProfileInformation()
        {
            float x = _startupProfilePanelX;
            float y = _startupProfilePanelY;
            float width = _startupProfilePanelWidth;
            float height = _startupProfilePanelHeight;
            float left = x - (width * 0.5f);
            float top = y - (height * 0.5f);
            float bottom = y + (height * 0.5f);

            Function.Call(
                Hash.DRAW_RECT,
                x + 0.003f,
                y + 0.005f,
                width,
                height,
                0, 0, 0, 105
            );

            Function.Call(
                Hash.DRAW_RECT,
                x,
                y,
                width,
                height,
                _startupInputLabelBgR,
                _startupInputLabelBgG,
                _startupInputLabelBgB,
                _startupInputLabelBgA
            );

            Function.Call(
                Hash.DRAW_RECT,
                x,
                top + (_startupAccentHeight * 0.5f),
                width,
                _startupAccentHeight,
                _startupAccentR,
                _startupAccentG,
                _startupAccentB,
                _startupAccentA
            );

            const float border = 0.0012f;
            Function.Call(Hash.DRAW_RECT, x, top, width, border,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 170);
            Function.Call(Hash.DRAW_RECT, x, bottom, width, border,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 170);
            Function.Call(Hash.DRAW_RECT, left, y, border, height,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 170);
            Function.Call(Hash.DRAW_RECT, left + width, y, border, height,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 170);

            DrawStartupTextLeft(
                _startupProfileTitle,
                left + _startupProfileLabelPaddingX,
                y + _startupProfileTitleOffsetY,
                _startupProfileLabelScale * 1.10f,
                _startupInputLabelTextR,
                _startupInputLabelTextG,
                _startupInputLabelTextB,
                _startupInputLabelTextA,
                _startupProfileFont
            );

            // divider di bawah judul PROFILE.
            Function.Call(
                Hash.DRAW_RECT,
                x,
                y + _startupProfileTitleOffsetY + 0.043f,
                width - (_startupProfileLabelPaddingX * 2.0f),
                0.0012f,
                _startupPanelBorderR,
                _startupPanelBorderG,
                _startupPanelBorderB,
                150
            );

            HostProfile profile = GetSelectedHostProfile();

            string nameText = profile != null ? profile.Name : "-";
            string scoreText = profile != null
                ? profile.Score.ToString(CultureInfo.InvariantCulture)
                : "-";
            string winsText = profile != null
                ? profile.Wins.ToString(CultureInfo.InvariantCulture)
                : "-";
            string deathsText = profile != null
                ? profile.Deaths.ToString(CultureInfo.InvariantCulture)
                : "-";
            string killsText = profile != null
                ? profile.EnemyKills.ToString(CultureInfo.InvariantCulture)
                : "-";

            int scoreR = _startupProfileNeutralR;
            int scoreG = _startupProfileNeutralG;
            int scoreB = _startupProfileNeutralB;
            int scoreA = _startupProfileNeutralA;

            if (profile != null && profile.Score > 0)
            {
                scoreR = _startupProfilePositiveR;
                scoreG = _startupProfilePositiveG;
                scoreB = _startupProfilePositiveB;
                scoreA = _startupProfilePositiveA;
            }
            else if (profile != null && profile.Score < 0)
            {
                scoreR = _startupProfileNegativeR;
                scoreG = _startupProfileNegativeG;
                scoreB = _startupProfileNegativeB;
                scoreA = _startupProfileNegativeA;
            }

            float rowY = y + _startupProfileFirstRowOffsetY;

            DrawStartupProfileRow("NAMA HOST :", nameText, rowY,
                _startupProfileNameR, _startupProfileNameG, _startupProfileNameB, _startupProfileNameA);

            rowY += _startupProfileRowSpacing;
            DrawStartupProfileRow("SCORE :", scoreText, rowY,
                scoreR, scoreG, scoreB, scoreA);

            rowY += _startupProfileRowSpacing;
            DrawStartupProfileRow("TOTAL MENANG :", winsText, rowY,
                _startupProfilePositiveR, _startupProfilePositiveG, _startupProfilePositiveB, _startupProfilePositiveA);

            rowY += _startupProfileRowSpacing;
            DrawStartupProfileRow("TOTAL MATI :", deathsText, rowY,
                _startupProfileNegativeR, _startupProfileNegativeG, _startupProfileNegativeB, _startupProfileNegativeA);

            rowY += _startupProfileRowSpacing;
            DrawStartupProfileRow("TOTAL KILL ENEMY :", killsText, rowY,
                _startupProfileKillR, _startupProfileKillG, _startupProfileKillB, _startupProfileKillA);
        }

        private void DrawStartupProfileRow(
            string label,
            string value,
            float rowY,
            int valueR,
            int valueG,
            int valueB,
            int valueA)
        {
            float left =
                _startupProfilePanelX -
                (_startupProfilePanelWidth * 0.5f);

            float rowWidth =
                _startupProfilePanelWidth -
                (_startupProfileLabelPaddingX * 2.0f);

            float rowHeight =
                Math.Max(
                    0.046f,
                    _startupProfileRowSpacing - 0.010f
                );

            float rowCenterX =
                left +
                _startupProfileLabelPaddingX +
                (rowWidth * 0.5f);

            int rowBgR = Math.Max(0, _startupInputBoxR);
            int rowBgG = Math.Max(0, _startupInputBoxG + 6);
            int rowBgB = Math.Max(0, _startupInputBoxB + 10);
            int rowBgA = Math.Min(255, _startupInputBoxA - 20);

            // Shadow tipis untuk tiap baris statistik.
            Function.Call(
                Hash.DRAW_RECT,
                rowCenterX + 0.0015f,
                rowY + 0.0025f,
                rowWidth,
                rowHeight,
                0, 0, 0, 75
            );

            // Kartu baris utama.
            Function.Call(
                Hash.DRAW_RECT,
                rowCenterX,
                rowY,
                rowWidth,
                rowHeight,
                rowBgR,
                rowBgG,
                rowBgB,
                rowBgA
            );

            // Border luar tipis.
            const float rowBorder = 0.0010f;
            float rowTop = rowY - (rowHeight * 0.5f);
            float rowBottom = rowY + (rowHeight * 0.5f);
            float rowLeft = rowCenterX - (rowWidth * 0.5f);
            float rowRight = rowCenterX + (rowWidth * 0.5f);

            Function.Call(Hash.DRAW_RECT, rowCenterX, rowTop, rowWidth, rowBorder,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 115);
            Function.Call(Hash.DRAW_RECT, rowCenterX, rowBottom, rowWidth, rowBorder,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 115);
            Function.Call(Hash.DRAW_RECT, rowLeft, rowY, rowBorder, rowHeight,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 115);
            Function.Call(Hash.DRAW_RECT, rowRight, rowY, rowBorder, rowHeight,
                _startupPanelBorderR, _startupPanelBorderG, _startupPanelBorderB, 115);

            float labelX = left + _startupProfileLabelPaddingX + 0.014f;
            float colonX = left + _startupProfileValueOffsetX - 0.020f;
            float valueX = left + _startupProfileValueOffsetX + 0.026f;
            float textY = rowY - 0.0155f;

            DrawStartupTextLeft(
                label,
                labelX,
                textY,
                _startupProfileLabelScale,
                _startupProfileLabelR,
                _startupProfileLabelG,
                _startupProfileLabelB,
                _startupProfileLabelA,
                _startupProfileFont
            );

            DrawStartupText(
                ":",
                colonX,
                textY,
                _startupProfileLabelScale,
                _startupProfileLabelR,
                _startupProfileLabelG,
                _startupProfileLabelB,
                _startupProfileLabelA,
                _startupProfileFont
            );

            DrawStartupTextLeft(
                value,
                valueX,
                textY,
                _startupProfileValueScale,
                valueR,
                valueG,
                valueB,
                valueA,
                _startupProfileFont
            );
        }

        private void DrawStartupTextLeft(
            string text,
            float x,
            float y,
            float scale,
            int r,
            int g,
            int b,
            int a,
            int font)
        {
            Function.Call(Hash.SET_TEXT_FONT, font);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, a);
            Function.Call(Hash.SET_TEXT_WRAP, 0.0f, 1.0f);
            Function.Call(Hash.SET_TEXT_CENTRE, false);
            Function.Call(Hash.SET_TEXT_DROPSHADOW, 2, 0, 0, 0, 220);
            Function.Call(Hash.SET_TEXT_EDGE, 1, 0, 0, 0, 220);
            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
        }

        private void DrawStartupText(
            string text,
            float x,
            float y,
            float scale,
            int r,
            int g,
            int b,
            int a,
            int font)
        {
            Function.Call(
                Hash.SET_TEXT_FONT,
                font
            );

            Function.Call(
                Hash.SET_TEXT_SCALE,
                scale,
                scale
            );

            Function.Call(
                Hash.SET_TEXT_COLOUR,
                r,
                g,
                b,
                a
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
                Hash.SET_TEXT_DROPSHADOW,
                2,
                0,
                0,
                0,
                220
            );

            Function.Call(
                Hash.SET_TEXT_EDGE,
                1,
                0,
                0,
                0,
                230
            );

            Function.Call(
                Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT,
                "STRING"
            );

            Function.Call(
                Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                text
            );

            Function.Call(
                Hash.END_TEXT_COMMAND_DISPLAY_TEXT,
                x,
                y
            );
        }

        private void DrawEnteredPlayerNameAboveHead()
        {
            if (!_playerNameTagEnabled ||
                !_startupNameConfirmed ||
                string.IsNullOrWhiteSpace(
                    _startupPlayerName
                ))
            {
                return;
            }

            Ped player =
                Game.Player.Character;

            if (player == null ||
                !player.Exists() ||
                !player.IsAlive)
            {
                return;
            }

            Vector3 headPosition =
                Function.Call<Vector3>(
                    Hash.GET_PED_BONE_COORDS,
                    player.Handle,
                    31086,
                    0.0f,
                    0.0f,
                    0.0f
                ) +
                new Vector3(
                    0.0f,
                    0.0f,
                    _playerNameTagHeadOffsetZ
                );

            if (GameplayCamera.Position.DistanceTo(
                    headPosition
                ) >
                _playerNameTagMaxDistance)
            {
                return;
            }

            OutputArgument screenX =
                new OutputArgument();

            OutputArgument screenY =
                new OutputArgument();

            bool onScreen =
                Function.Call<bool>(
                    Hash.GET_SCREEN_COORD_FROM_WORLD_COORD,
                    headPosition.X,
                    headPosition.Y,
                    headPosition.Z,
                    screenX,
                    screenY
                );

            if (!onScreen)
            {
                return;
            }

            float x =
                screenX.GetResult<float>();

            float y =
                screenY.GetResult<float>();

            // Background name-tag menyesuaikan panjang nama.
            float tagWidth =
                0.050f +
                Math.Min(
                    _startupNameMaxLength,
                    _startupPlayerName.Length
                ) *
                0.0050f;

            tagWidth =
                Math.Min(
                    tagWidth,
                    0.170f
                );

            float tagHeight =
                0.034f;

            // Shadow.
            Function.Call(
                Hash.DRAW_RECT,
                x + 0.002f,
                y + 0.003f,
                tagWidth,
                tagHeight,
                0,
                0,
                0,
                115
            );

            // Main tag.
            Function.Call(
                Hash.DRAW_RECT,
                x,
                y,
                tagWidth,
                tagHeight,
                _playerNameTagBgR,
                _playerNameTagBgG,
                _playerNameTagBgB,
                _playerNameTagBgA
            );

            // Accent line.
            Function.Call(
                Hash.DRAW_RECT,
                x,
                y - (tagHeight * 0.5f),
                tagWidth,
                0.0035f,
                _playerNameTagAccentR,
                _playerNameTagAccentG,
                _playerNameTagAccentB,
                _playerNameTagAccentA
            );

            DrawStartupText(
                _startupPlayerName,
                x,
                y - 0.0125f,
                _playerNameTagScale,
                _playerNameTagR,
                _playerNameTagG,
                _playerNameTagB,
                _playerNameTagA,
                _playerNameTagFont
            );
        }

        private void DrawCustomDeathBlackOverlay(
            int currentTime,
            bool showRespawnText,
            int remainingSeconds)
        {
            if (!_hudCustomDeathEnabled)
                return;

            int overlayAlpha =
                Math.Max(
                    0,
                    Math.Min(
                        255,
                        _deathOverlayMaxA
                    )
                );

            // =========================================================
            // FADE TO BLACK MANUAL
            // =========================================================
            int fadeOutStartTime =
                _customDeathStartTime +
                CUSTOM_DEATH_FADE_DELAY_MS;

            int fadeOutEndTime =
                fadeOutStartTime +
                CUSTOM_DEATH_FADE_DURATION_MS;

            if (_customDeathActive &&
                currentTime <
                    fadeOutEndTime)
            {
                float fadeProgress =
                    Math.Max(
                        0.0f,
                        Math.Min(
                            1.0f,
                            (currentTime - fadeOutStartTime) /
                            (float)CUSTOM_DEATH_FADE_DURATION_MS
                        )
                    );

                overlayAlpha =
                    (int)(
                        Math.Max(
                            0,
                            Math.Min(
                                255,
                                _deathOverlayMaxA
                            )
                        ) *
                        fadeProgress
                    );
            }
            // =========================================================
            // FADE FROM BLACK MANUAL
            // =========================================================
            else if (!_customDeathActive &&
                     _customDeathOverlayFadeInEndTime > 0 &&
                     currentTime <
                         _customDeathOverlayFadeInEndTime)
            {
                float fadeInProgress =
                    Math.Max(
                        0.0f,
                        Math.Min(
                            1.0f,
                            (currentTime -
                             _customDeathOverlayFadeInStartTime) /
                            (float)Math.Max(
                                1,
                                _customDeathOverlayFadeInEndTime -
                                _customDeathOverlayFadeInStartTime
                            )
                        )
                    );

                overlayAlpha =
                    (int)(
                        Math.Max(
                            0,
                            Math.Min(
                                255,
                                _deathOverlayMaxA
                            )
                        ) *
                        (1.0f - fadeInProgress)
                    );
            }

            if (overlayAlpha <= 0)
                return;

            // Sembunyikan HUD/radar bawaan GTA selama layar death.
            Function.Call(
                Hash.HIDE_HUD_AND_RADAR_THIS_FRAME
            );

            // Full-screen black rectangle.
            // Karena text digambar SETELAH rectangle ini,
            // text tidak lagi ketimpa layar hitam.
            Function.Call(
                Hash.DRAW_RECT,
                _deathOverlayX,
                _deathOverlayY,
                _deathOverlayWidth,
                _deathOverlayHeight,
                _deathOverlayR,
                _deathOverlayG,
                _deathOverlayB,
                overlayAlpha
            );

            if (showRespawnText &&
                remainingSeconds > 0)
            {
                DrawBoldTextOnScreen(
                    FormatHudText(
                        _deathRespawnTextFormat,
                        remainingSeconds
                    ),
                    _deathRespawnTextX,
                    _deathRespawnTextY,
                    _deathRespawnTextScale,
                    _deathRespawnTextR,
                    _deathRespawnTextG,
                    _deathRespawnTextB,
                    _deathRespawnTextA,
                    _deathRespawnTextFont
                );
            }
        }

        private string CleanHudMessageForLog(
            string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            return message
                .Replace("~r~", string.Empty)
                .Replace("~g~", string.Empty)
                .Replace("~b~", string.Empty)
                .Replace("~y~", string.Empty)
                .Replace("~o~", string.Empty)
                .Replace("~p~", string.Empty)
                .Replace("~c~", string.Empty)
                .Replace("~m~", string.Empty)
                .Replace("~u~", string.Empty)
                .Replace("~n~", string.Empty)
                .Replace("~s~", string.Empty)
                .Replace("~h~", string.Empty)
                .Replace("~w~", string.Empty);
        }

        private void ShowHudNotification(
            string message,
            bool logDiagnosticError = true)
        {
            // Error kondisi (model invalid, spawn gagal, dll.) bukan selalu
            // exception. Karena itu pesan ERROR tetap dicatat walaupun HUD off.
            if (
                logDiagnosticError &&
                !string.IsNullOrEmpty(message) &&
                message.IndexOf(
                    "ERROR",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                WriteDiagnosticLog(
                    "HUD ERROR",
                    CleanHudMessageForLog(message)
                );
            }

            if (!_hudNotificationEnabled)
                return;

            ShowNotificationCompat(
                message
            );
        }

        private bool IsNormalHudSuppressed()
        {
            return
                _customDeathActive ||
                Game.GameTime <
                    _customDeathHudResumeTime;
        }

        // DrawPlayerHealthBar dipindahkan ke file layout HUD.


        private void DrawPlayerVitalCard(float x, string label, string value, float fraction,
            float textOffsetY, float textScale, int tr, int tg, int tb, int ta,
            int fr, int fg, int fb, int fa)
        {
            float width = Math.Max(0.06f, _hpBarWidth);
            float height = Math.Max(0.04f, _hpBarHeight);
            float left = x - width / 2.0f;
            float inset = 0.008f;
            Function.Call(Hash.DRAW_RECT, x, _hpBarY, width, height, _hpBgR, _hpBgG, _hpBgB, _hpBgA);
            DrawGiftOutline(x, _hpBarY, width, height, _healthBorderSize,
                _healthBorderR, _healthBorderG, _healthBorderB, _healthBorderA);
            float valueWidth = (width - 2.0f * inset) * Math.Max(0.35f, Math.Min(0.70f, _healthValueColumnRatio));
            float dividerX = left + width - inset - valueWidth;
            float labelWidth = dividerX - left - inset;
            float textY = _hpBarY + textOffsetY;
            Function.Call(Hash.DRAW_RECT, dividerX, _hpBarY - height * 0.18f,
                0.0008f, height * 0.34f, _healthBorderR, _healthBorderG, _healthBorderB, 190);
            DrawReferenceProgressText(label, left + inset, textY, textScale,
                Math.Max(0.01f, labelWidth - 0.006f), tr, tg, tb, ta, _hpTextFont, false);
            DrawReferenceProgressText(value, dividerX + valueWidth / 2.0f, textY, textScale,
                Math.Max(0.01f, valueWidth - 0.010f), tr, tg, tb, ta, _hpTextFont, true);
            float trackWidth = width - 2.0f * inset;
            float trackHeight = Math.Max(0.002f, Math.Min(height * 0.30f, _healthTrackHeight));
            float trackY = _hpBarY + Math.Max(-height / 2.0f + trackHeight,
                Math.Min(height / 2.0f - trackHeight, _healthTrackOffsetY));
            Function.Call(Hash.DRAW_RECT, x, trackY, trackWidth, trackHeight, 29, 39, 48, 255);
            float fillWidth = trackWidth * Math.Max(0.0f, Math.Min(1.0f, fraction));
            if (fillWidth > 0.0f)
                Function.Call(Hash.DRAW_RECT, left + inset + fillWidth / 2.0f, trackY,
                    fillWidth, trackHeight, fr, fg, fb, fa);
            DrawGiftOutline(x, trackY, trackWidth, trackHeight, 0.0007f,
                _healthBorderR, _healthBorderG, _healthBorderB, 190);
        }

        private void DrawBoldTextOnScreen(
            string text,
            float x,
            float y,
            float scale,
            int r,
            int g,
            int b,
            int a,
            int font)
        {
            Function.Call(Hash.SET_TEXT_FONT, font);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, a);
            Function.Call(Hash.SET_TEXT_WRAP, 0.0f, 1.0f);
            Function.Call(Hash.SET_TEXT_CENTRE, true);

            Function.Call(
                Hash.SET_TEXT_DROPSHADOW,
                _boldTextShadowEnabled
                    ? Math.Max(0, _boldTextShadowDistance)
                    : 0,
                _boldTextShadowR,
                _boldTextShadowG,
                _boldTextShadowB,
                _boldTextShadowEnabled
                    ? _boldTextShadowA
                    : 0
            );

            Function.Call(
                Hash.SET_TEXT_EDGE,
                _boldTextEdgeEnabled
                    ? Math.Max(0, _boldTextEdgeSize)
                    : 0,
                _boldTextEdgeR,
                _boldTextEdgeG,
                _boldTextEdgeB,
                _boldTextEdgeEnabled
                    ? _boldTextEdgeA
                    : 0
            );

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
        }

        private void DrawBoldWrappedTextOnScreen(
            string text,
            float x,
            float y,
            float scale,
            float maxWidth,
            int r,
            int g,
            int b,
            int a,
            int font)
        {
            Function.Call(
                Hash.SET_TEXT_FONT,
                font
            );

            Function.Call(
                Hash.SET_TEXT_SCALE,
                scale,
                scale
            );

            Function.Call(
                Hash.SET_TEXT_COLOUR,
                r,
                g,
                b,
                a
            );

            float left =
                x -
                (maxWidth / 2.0f);

            float right =
                x +
                (maxWidth / 2.0f);

            Function.Call(
                Hash.SET_TEXT_WRAP,
                left,
                right
            );

            Function.Call(
                Hash.SET_TEXT_CENTRE,
                true
            );

            Function.Call(
                Hash.SET_TEXT_DROPSHADOW,
                _boldTextShadowEnabled
                    ? Math.Max(0, _boldTextShadowDistance)
                    : 0,
                _boldTextShadowR,
                _boldTextShadowG,
                _boldTextShadowB,
                _boldTextShadowEnabled
                    ? _boldTextShadowA
                    : 0
            );

            Function.Call(
                Hash.SET_TEXT_EDGE,
                _boldTextEdgeEnabled
                    ? Math.Max(0, _boldTextEdgeSize)
                    : 0,
                _boldTextEdgeR,
                _boldTextEdgeG,
                _boldTextEdgeB,
                _boldTextEdgeEnabled
                    ? _boldTextEdgeA
                    : 0
            );

            Function.Call(
                Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT,
                "STRING"
            );

            Function.Call(
                Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                text
            );

            Function.Call(
                Hash.END_TEXT_COMMAND_DISPLAY_TEXT,
                x,
                y
            );
        }

        private string FormatHudText(
            string format,
            params object[] args)
        {
            if (string.IsNullOrEmpty(format))
                return string.Empty;

            try
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    format,
                    args
                );
            }
            catch
            {
                return format;
            }
        }

        private void LoadDesignConfigFromIni()
        {
            string iniPath =
                GetActiveGameplayHudIniPath();

            if (!File.Exists(iniPath))
                return;

            try
            {
                Dictionary<string, Dictionary<string, string>> design =
                    ReadHudDesignWithSafePreset(iniPath);

                string previousRouteBlipVisualKey =
                    GetRouteBlipVisualKey();

                // =====================================================
                // SURVIVAL - NO VEHICLE WARNING
                // Section tetap berada di GameplayHUD.ini tetapi hanya digambar di Survival.
                // =====================================================
                _survivalNoVehicleWarningEnabled = GetDesignBool(design, "HUD_NO_VEHICLE_WARNING", "Enabled", _survivalNoVehicleWarningEnabled);
                _survivalNoVehicleWarningText = GetDesignString(design, "HUD_NO_VEHICLE_WARNING", "Text", _survivalNoVehicleWarningText);
                _survivalNoVehicleWarningDurationMs = Math.Max(250, GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "DurationMs", _survivalNoVehicleWarningDurationMs));
                _survivalNoVehicleWarningCooldownMs = Math.Max(100, GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "CooldownMs", _survivalNoVehicleWarningCooldownMs));
                _survivalNoVehicleWarningX = GetDesignFloat(design, "HUD_NO_VEHICLE_WARNING", "PosX", _survivalNoVehicleWarningX);
                _survivalNoVehicleWarningY = GetDesignFloat(design, "HUD_NO_VEHICLE_WARNING", "PosY", _survivalNoVehicleWarningY);
                _survivalNoVehicleWarningWidth = Math.Max(0.05f, GetDesignFloat(design, "HUD_NO_VEHICLE_WARNING", "Width", _survivalNoVehicleWarningWidth));
                _survivalNoVehicleWarningHeight = Math.Max(0.02f, GetDesignFloat(design, "HUD_NO_VEHICLE_WARNING", "Height", _survivalNoVehicleWarningHeight));
                _survivalNoVehicleWarningBgR = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "BackgroundR", _survivalNoVehicleWarningBgR);
                _survivalNoVehicleWarningBgG = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "BackgroundG", _survivalNoVehicleWarningBgG);
                _survivalNoVehicleWarningBgB = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "BackgroundB", _survivalNoVehicleWarningBgB);
                _survivalNoVehicleWarningBgA = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "BackgroundA", _survivalNoVehicleWarningBgA);
                _survivalNoVehicleWarningBorderR = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "BorderR", _survivalNoVehicleWarningBorderR);
                _survivalNoVehicleWarningBorderG = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "BorderG", _survivalNoVehicleWarningBorderG);
                _survivalNoVehicleWarningBorderB = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "BorderB", _survivalNoVehicleWarningBorderB);
                _survivalNoVehicleWarningBorderA = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "BorderA", _survivalNoVehicleWarningBorderA);
                _survivalNoVehicleWarningBorderSize = Math.Max(0.0002f, GetDesignFloat(design, "HUD_NO_VEHICLE_WARNING", "BorderSize", _survivalNoVehicleWarningBorderSize));
                _survivalNoVehicleWarningTextScale = Math.Max(0.20f, GetDesignFloat(design, "HUD_NO_VEHICLE_WARNING", "TextScale", _survivalNoVehicleWarningTextScale));
                _survivalNoVehicleWarningTextFont = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "TextFont", _survivalNoVehicleWarningTextFont);
                _survivalNoVehicleWarningTextR = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "TextR", _survivalNoVehicleWarningTextR);
                _survivalNoVehicleWarningTextG = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "TextG", _survivalNoVehicleWarningTextG);
                _survivalNoVehicleWarningTextB = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "TextB", _survivalNoVehicleWarningTextB);
                _survivalNoVehicleWarningTextA = GetDesignInt(design, "HUD_NO_VEHICLE_WARNING", "TextA", _survivalNoVehicleWarningTextA);
                _survivalNoVehicleWarningTextOffsetY = GetDesignFloat(design, "HUD_NO_VEHICLE_WARNING", "TextOffsetY", _survivalNoVehicleWarningTextOffsetY);

                // =====================================================
                // PLAYER SETUP / NAME INPUT
                // =====================================================
                _startupSetupEnabled = GetDesignBool(design, "HUD_PLAYER_SETUP", "Enabled", _startupSetupEnabled);
                _startupNameMaxLength = GetDesignInt(design, "HUD_PLAYER_SETUP", "MaxLength", _startupNameMaxLength);
                _startupNameMaxWords = GetDesignInt(design, "HUD_PLAYER_SETUP", "MaxWords", _startupNameMaxWords);

                if (_startupNameMaxLength < 1)
                    _startupNameMaxLength = 1;

                if (_startupNameMaxLength > 120)
                    _startupNameMaxLength = 120;

                if (_startupNameMaxWords < 1)
                    _startupNameMaxWords = 1;

                if (_startupNameMaxWords > 20)
                    _startupNameMaxWords = 20;

                _startupOverlayR = GetDesignInt(design, "HUD_PLAYER_SETUP", "OverlayR", _startupOverlayR);
                _startupOverlayG = GetDesignInt(design, "HUD_PLAYER_SETUP", "OverlayG", _startupOverlayG);
                _startupOverlayB = GetDesignInt(design, "HUD_PLAYER_SETUP", "OverlayB", _startupOverlayB);
                _startupOverlayA = GetDesignInt(design, "HUD_PLAYER_SETUP", "OverlayA", _startupOverlayA);

                _startupNamePanelX = GetDesignFloat(design, "HUD_PLAYER_SETUP", "PosX", _startupNamePanelX);
                _startupNamePanelY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "PosY", _startupNamePanelY);
                _startupNamePanelWidth = GetDesignFloat(design, "HUD_PLAYER_SETUP", "Width", _startupNamePanelWidth);
                _startupNamePanelHeight = GetDesignFloat(design, "HUD_PLAYER_SETUP", "Height", _startupNamePanelHeight);

                _startupPanelR = GetDesignInt(design, "HUD_PLAYER_SETUP", "BackgroundR", _startupPanelR);
                _startupPanelG = GetDesignInt(design, "HUD_PLAYER_SETUP", "BackgroundG", _startupPanelG);
                _startupPanelB = GetDesignInt(design, "HUD_PLAYER_SETUP", "BackgroundB", _startupPanelB);
                _startupPanelA = GetDesignInt(design, "HUD_PLAYER_SETUP", "BackgroundA", _startupPanelA);

                _startupPanelBorderR = GetDesignInt(design, "HUD_PLAYER_SETUP", "BorderR", _startupPanelBorderR);
                _startupPanelBorderG = GetDesignInt(design, "HUD_PLAYER_SETUP", "BorderG", _startupPanelBorderG);
                _startupPanelBorderB = GetDesignInt(design, "HUD_PLAYER_SETUP", "BorderB", _startupPanelBorderB);
                _startupPanelBorderA = GetDesignInt(design, "HUD_PLAYER_SETUP", "BorderA", _startupPanelBorderA);

                _startupAccentR = GetDesignInt(design, "HUD_PLAYER_SETUP", "AccentR", _startupAccentR);
                _startupAccentG = GetDesignInt(design, "HUD_PLAYER_SETUP", "AccentG", _startupAccentG);
                _startupAccentB = GetDesignInt(design, "HUD_PLAYER_SETUP", "AccentB", _startupAccentB);
                _startupAccentA = GetDesignInt(design, "HUD_PLAYER_SETUP", "AccentA", _startupAccentA);
                _startupAccentHeight = GetDesignFloat(design, "HUD_PLAYER_SETUP", "AccentHeight", _startupAccentHeight);

                _startupInstructionText = GetDesignString(design, "HUD_PLAYER_SETUP", "Instruction", _startupInstructionText);
                _startupInstructionOffsetY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InstructionOffsetY", _startupInstructionOffsetY);
                _startupInstructionScale = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InstructionScale", _startupInstructionScale);
                _startupInstructionFont = GetDesignInt(design, "HUD_PLAYER_SETUP", "InstructionFont", _startupInstructionFont);
                _startupInstructionR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InstructionR", _startupInstructionR);
                _startupInstructionG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InstructionG", _startupInstructionG);
                _startupInstructionB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InstructionB", _startupInstructionB);
                _startupInstructionA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InstructionA", _startupInstructionA);

                _startupInputTitle = GetDesignString(design, "HUD_PLAYER_SETUP", "InputTitle", _startupInputTitle);
                _startupInputLabelEnabled = GetDesignBool(design, "HUD_PLAYER_SETUP", "InputLabelEnabled", _startupInputLabelEnabled);
                _startupInputLabelX = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputLabelPosX", _startupInputLabelX);
                _startupInputLabelY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputLabelPosY", _startupInputLabelY);
                _startupInputLabelWidth = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputLabelWidth", _startupInputLabelWidth);
                _startupInputLabelHeight = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputLabelHeight", _startupInputLabelHeight);
                _startupInputLabelTextPaddingX = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputLabelTextPaddingX", _startupInputLabelTextPaddingX);
                _startupInputLabelTextOffsetY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputLabelTextOffsetY", _startupInputLabelTextOffsetY);
                _startupInputLabelTextScale = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputLabelTextScale", _startupInputLabelTextScale);
                _startupInputLabelTextFont = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelTextFont", _startupInputLabelTextFont);
                _startupInputLabelTextR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelTextR", _startupInputLabelTextR);
                _startupInputLabelTextG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelTextG", _startupInputLabelTextG);
                _startupInputLabelTextB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelTextB", _startupInputLabelTextB);
                _startupInputLabelTextA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelTextA", _startupInputLabelTextA);
                _startupInputLabelBgR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelBackgroundR", _startupInputLabelBgR);
                _startupInputLabelBgG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelBackgroundG", _startupInputLabelBgG);
                _startupInputLabelBgB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelBackgroundB", _startupInputLabelBgB);
                _startupInputLabelBgA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputLabelBackgroundA", _startupInputLabelBgA);

                _startupInputHint = GetDesignString(design, "HUD_PLAYER_SETUP", "InputHint", _startupInputHint);
                _startupInputPlaceholder = GetDesignString(design, "HUD_PLAYER_SETUP", "InputPlaceholder", _startupInputPlaceholder);
                _startupInputHintOffsetY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputHintOffsetY", _startupInputHintOffsetY);
                _startupInputHintScale = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputHintScale", _startupInputHintScale);
                _startupInputHintR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputHintR", _startupInputHintR);
                _startupInputHintG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputHintG", _startupInputHintG);
                _startupInputHintB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputHintB", _startupInputHintB);
                _startupInputHintA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputHintA", _startupInputHintA);

                _startupInputBoxOffsetY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputBoxOffsetY", _startupInputBoxOffsetY);
                _startupInputBoxWidth = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputBoxWidth", _startupInputBoxWidth);
                _startupInputBoxHeight = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputBoxHeight", _startupInputBoxHeight);
                _startupInputBoxR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputBoxR", _startupInputBoxR);
                _startupInputBoxG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputBoxG", _startupInputBoxG);
                _startupInputBoxB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputBoxB", _startupInputBoxB);
                _startupInputBoxA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputBoxA", _startupInputBoxA);
                _startupInputBoxBorderR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputBoxBorderR", _startupInputBoxBorderR);
                _startupInputBoxBorderG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputBoxBorderG", _startupInputBoxBorderG);
                _startupInputBoxBorderB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputBoxBorderB", _startupInputBoxBorderB);
                _startupInputBoxBorderA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputBoxBorderA", _startupInputBoxBorderA);
                _startupInputTextScale = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputTextScale", _startupInputTextScale);
                _startupInputTextFont = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputTextFont", _startupInputTextFont);
                _startupInputTextR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputTextR", _startupInputTextR);
                _startupInputTextG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputTextG", _startupInputTextG);
                _startupInputTextB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputTextB", _startupInputTextB);
                _startupInputTextA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputTextA", _startupInputTextA);
                _startupInputPlaceholderR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputPlaceholderR", _startupInputPlaceholderR);
                _startupInputPlaceholderG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputPlaceholderG", _startupInputPlaceholderG);
                _startupInputPlaceholderB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputPlaceholderB", _startupInputPlaceholderB);
                _startupInputPlaceholderA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputPlaceholderA", _startupInputPlaceholderA);

                _startupInputFooterText = GetDesignString(design, "HUD_PLAYER_SETUP", "InputFooterText", _startupInputFooterText);
                _startupInputFooterOffsetY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputFooterOffsetY", _startupInputFooterOffsetY);
                _startupInputFooterScale = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputFooterScale", _startupInputFooterScale);
                _startupInputFooterR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputFooterR", _startupInputFooterR);
                _startupInputFooterG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputFooterG", _startupInputFooterG);
                _startupInputFooterB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputFooterB", _startupInputFooterB);
                _startupInputFooterA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputFooterA", _startupInputFooterA);

                _startupInputCounterEnabled = GetDesignBool(design, "HUD_PLAYER_SETUP", "InputCounterEnabled", _startupInputCounterEnabled);
                _startupInputCounterOffsetY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputCounterOffsetY", _startupInputCounterOffsetY);
                _startupInputCounterScale = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputCounterScale", _startupInputCounterScale);
                _startupInputCounterR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputCounterR", _startupInputCounterR);
                _startupInputCounterG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputCounterG", _startupInputCounterG);
                _startupInputCounterB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputCounterB", _startupInputCounterB);
                _startupInputCounterA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputCounterA", _startupInputCounterA);

                _startupInputWarningText = GetDesignString(design, "HUD_PLAYER_SETUP", "InputWarningText", _startupInputWarningText);
                _startupInputWarningDurationMs = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputWarningDurationMs", _startupInputWarningDurationMs);
                _startupInputWarningOffsetY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputWarningOffsetY", _startupInputWarningOffsetY);
                _startupInputWarningScale = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputWarningScale", _startupInputWarningScale);
                _startupInputWarningR = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputWarningR", _startupInputWarningR);
                _startupInputWarningG = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputWarningG", _startupInputWarningG);
                _startupInputWarningB = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputWarningB", _startupInputWarningB);
                _startupInputWarningA = GetDesignInt(design, "HUD_PLAYER_SETUP", "InputWarningA", _startupInputWarningA);
                _startupInputWarningFlashField = GetDesignBool(design, "HUD_PLAYER_SETUP", "InputWarningFlashField", _startupInputWarningFlashField);

                _startupInputActiveBarWidth = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputActiveBarWidth", _startupInputActiveBarWidth);
                _startupInputActiveUnderlineHeight = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputActiveUnderlineHeight", _startupInputActiveUnderlineHeight);
                _startupInputShadowOffsetX = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputShadowOffsetX", _startupInputShadowOffsetX);
                _startupInputShadowOffsetY = GetDesignFloat(design, "HUD_PLAYER_SETUP", "InputShadowOffsetY", _startupInputShadowOffsetY);

                _playerNameTagEnabled = GetDesignBool(design, "HUD_PLAYER_SETUP", "NameTagEnabled", _playerNameTagEnabled);
                _playerNameTagHeadOffsetZ = GetDesignFloat(design, "HUD_PLAYER_SETUP", "NameTagHeadOffsetZ", _playerNameTagHeadOffsetZ);
                _playerNameTagScale = GetDesignFloat(design, "HUD_PLAYER_SETUP", "NameTagScale", _playerNameTagScale);
                _playerNameTagFont = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagFont", _playerNameTagFont);
                _playerNameTagMaxDistance = GetDesignFloat(design, "HUD_PLAYER_SETUP", "NameTagMaxDistance", _playerNameTagMaxDistance);

                _playerNameTagR = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagTextR", _playerNameTagR);
                _playerNameTagG = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagTextG", _playerNameTagG);
                _playerNameTagB = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagTextB", _playerNameTagB);
                _playerNameTagA = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagTextA", _playerNameTagA);

                _playerNameTagBgR = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagBackgroundR", _playerNameTagBgR);
                _playerNameTagBgG = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagBackgroundG", _playerNameTagBgG);
                _playerNameTagBgB = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagBackgroundB", _playerNameTagBgB);
                _playerNameTagBgA = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagBackgroundA", _playerNameTagBgA);

                _playerNameTagAccentR = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagAccentR", _playerNameTagAccentR);
                _playerNameTagAccentG = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagAccentG", _playerNameTagAccentG);
                _playerNameTagAccentB = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagAccentB", _playerNameTagAccentB);
                _playerNameTagAccentA = GetDesignInt(design, "HUD_PLAYER_SETUP", "NameTagAccentA", _playerNameTagAccentA);

                // =====================================================
                // GLOBAL HUD TEXT STYLE
                // =====================================================
                _normalTextShadowEnabled = GetDesignBool(design, "HUD_TEXT_STYLE", "NormalShadowEnabled", _normalTextShadowEnabled);
                _normalTextShadowDistance = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalShadowDistance", _normalTextShadowDistance);
                _normalTextShadowR = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalShadowR", _normalTextShadowR);
                _normalTextShadowG = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalShadowG", _normalTextShadowG);
                _normalTextShadowB = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalShadowB", _normalTextShadowB);
                _normalTextShadowA = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalShadowA", _normalTextShadowA);

                _normalTextEdgeEnabled = GetDesignBool(design, "HUD_TEXT_STYLE", "NormalEdgeEnabled", _normalTextEdgeEnabled);
                _normalTextEdgeSize = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalEdgeSize", _normalTextEdgeSize);
                _normalTextEdgeR = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalEdgeR", _normalTextEdgeR);
                _normalTextEdgeG = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalEdgeG", _normalTextEdgeG);
                _normalTextEdgeB = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalEdgeB", _normalTextEdgeB);
                _normalTextEdgeA = GetDesignInt(design, "HUD_TEXT_STYLE", "NormalEdgeA", _normalTextEdgeA);

                _boldTextShadowEnabled = GetDesignBool(design, "HUD_TEXT_STYLE", "BoldShadowEnabled", _boldTextShadowEnabled);
                _boldTextShadowDistance = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldShadowDistance", _boldTextShadowDistance);
                _boldTextShadowR = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldShadowR", _boldTextShadowR);
                _boldTextShadowG = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldShadowG", _boldTextShadowG);
                _boldTextShadowB = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldShadowB", _boldTextShadowB);
                _boldTextShadowA = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldShadowA", _boldTextShadowA);

                _boldTextEdgeEnabled = GetDesignBool(design, "HUD_TEXT_STYLE", "BoldEdgeEnabled", _boldTextEdgeEnabled);
                _boldTextEdgeSize = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldEdgeSize", _boldTextEdgeSize);
                _boldTextEdgeR = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldEdgeR", _boldTextEdgeR);
                _boldTextEdgeG = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldEdgeG", _boldTextEdgeG);
                _boldTextEdgeB = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldEdgeB", _boldTextEdgeB);
                _boldTextEdgeA = GetDesignInt(design, "HUD_TEXT_STYLE", "BoldEdgeA", _boldTextEdgeA);

                // =====================================================
                // GTA SCRIPT NOTIFICATIONS
                // =====================================================
                _hudNotificationEnabled = GetDesignBool(design, "HUD_NOTIFICATION", "Enabled", _hudNotificationEnabled);

                // =====================================================
                // CHECKPOINT COUNTDOWN
                // =====================================================
                _hudCheckpointCountdownEnabled = GetDesignBool(design, "HUD_CHECKPOINT_COUNTDOWN", "Enabled", _hudCheckpointCountdownEnabled);
                _checkpointCountdownX = GetDesignFloat(design, "HUD_CHECKPOINT_COUNTDOWN", "PosX", _checkpointCountdownX);
                _checkpointCountdownY = GetDesignFloat(design, "HUD_CHECKPOINT_COUNTDOWN", "PosY", _checkpointCountdownY);
                _checkpointCountdownScale = GetDesignFloat(design, "HUD_CHECKPOINT_COUNTDOWN", "TextScale", _checkpointCountdownScale);
                _checkpointCountdownFont = GetDesignInt(design, "HUD_CHECKPOINT_COUNTDOWN", "TextFont", _checkpointCountdownFont);
                _checkpointCountdownR = GetDesignInt(design, "HUD_CHECKPOINT_COUNTDOWN", "TextR", _checkpointCountdownR);
                _checkpointCountdownG = GetDesignInt(design, "HUD_CHECKPOINT_COUNTDOWN", "TextG", _checkpointCountdownG);
                _checkpointCountdownB = GetDesignInt(design, "HUD_CHECKPOINT_COUNTDOWN", "TextB", _checkpointCountdownB);
                _checkpointCountdownA = GetDesignInt(design, "HUD_CHECKPOINT_COUNTDOWN", "TextA", _checkpointCountdownA);
                _checkpointCountdownTextFormat = GetDesignString(design, "HUD_CHECKPOINT_COUNTDOWN", "TextFormat", _checkpointCountdownTextFormat);

                // =====================================================
                // FINISH COUNTDOWN
                // =====================================================
                _hudFinishCountdownEnabled = GetDesignBool(design, "HUD_FINISH_COUNTDOWN", "Enabled", _hudFinishCountdownEnabled);
                _finishCountdownX = GetDesignFloat(design, "HUD_FINISH_COUNTDOWN", "PosX", _finishCountdownX);
                _finishCountdownY = GetDesignFloat(design, "HUD_FINISH_COUNTDOWN", "PosY", _finishCountdownY);
                _finishCountdownScale = GetDesignFloat(design, "HUD_FINISH_COUNTDOWN", "TextScale", _finishCountdownScale);
                _finishCountdownFont = GetDesignInt(design, "HUD_FINISH_COUNTDOWN", "TextFont", _finishCountdownFont);
                _finishCountdownR = GetDesignInt(design, "HUD_FINISH_COUNTDOWN", "TextR", _finishCountdownR);
                _finishCountdownG = GetDesignInt(design, "HUD_FINISH_COUNTDOWN", "TextG", _finishCountdownG);
                _finishCountdownB = GetDesignInt(design, "HUD_FINISH_COUNTDOWN", "TextB", _finishCountdownB);
                _finishCountdownA = GetDesignInt(design, "HUD_FINISH_COUNTDOWN", "TextA", _finishCountdownA);
                _finishCountdownTextFormat = GetDesignString(design, "HUD_FINISH_COUNTDOWN", "TextFormat", _finishCountdownTextFormat);

                // =====================================================
                // WORLD ROUTE MARKER
                // =====================================================
                _hudRouteMarkerEnabled = GetDesignBool(design, "HUD_ROUTE_MARKER", "Enabled", _hudRouteMarkerEnabled);
                _routeMarkerRenderDistance = GetDesignFloat(design, "HUD_ROUTE_MARKER", "RenderDistance", _routeMarkerRenderDistance);
                _routeMarkerType = GetDesignEnum(design, "HUD_ROUTE_MARKER", "MarkerType", _routeMarkerType);
                _routeMarkerOffsetZ = GetDesignFloat(design, "HUD_ROUTE_MARKER", "OffsetZ", _routeMarkerOffsetZ);

                _routePosMarkerSizeX = GetDesignFloat(design, "HUD_ROUTE_MARKER", "PosSizeX", _routePosMarkerSizeX);
                _routePosMarkerSizeY = GetDesignFloat(design, "HUD_ROUTE_MARKER", "PosSizeY", _routePosMarkerSizeY);
                _routePosMarkerSizeZ = GetDesignFloat(design, "HUD_ROUTE_MARKER", "PosSizeZ", _routePosMarkerSizeZ);
                _routePosMarkerR = GetDesignInt(design, "HUD_ROUTE_MARKER", "PosR", _routePosMarkerR);
                _routePosMarkerG = GetDesignInt(design, "HUD_ROUTE_MARKER", "PosG", _routePosMarkerG);
                _routePosMarkerB = GetDesignInt(design, "HUD_ROUTE_MARKER", "PosB", _routePosMarkerB);
                _routePosMarkerA = GetDesignInt(design, "HUD_ROUTE_MARKER", "PosA", _routePosMarkerA);

                _routeFinishMarkerSizeX = GetDesignFloat(design, "HUD_ROUTE_MARKER", "FinishSizeX", _routeFinishMarkerSizeX);
                _routeFinishMarkerSizeY = GetDesignFloat(design, "HUD_ROUTE_MARKER", "FinishSizeY", _routeFinishMarkerSizeY);
                _routeFinishMarkerSizeZ = GetDesignFloat(design, "HUD_ROUTE_MARKER", "FinishSizeZ", _routeFinishMarkerSizeZ);
                _routeFinishMarkerR = GetDesignInt(design, "HUD_ROUTE_MARKER", "FinishR", _routeFinishMarkerR);
                _routeFinishMarkerG = GetDesignInt(design, "HUD_ROUTE_MARKER", "FinishG", _routeFinishMarkerG);
                _routeFinishMarkerB = GetDesignInt(design, "HUD_ROUTE_MARKER", "FinishB", _routeFinishMarkerB);
                _routeFinishMarkerA = GetDesignInt(design, "HUD_ROUTE_MARKER", "FinishA", _routeFinishMarkerA);

                // =====================================================
                // ROUTE MAP BLIP
                // =====================================================
                _hudRouteBlipEnabled = GetDesignBool(design, "HUD_ROUTE_BLIP", "Enabled", _hudRouteBlipEnabled);
                _routeBlipShowRoute = GetDesignBool(design, "HUD_ROUTE_BLIP", "ShowRoute", _routeBlipShowRoute);

                _routePosBlipEnabled = GetDesignBool(design, "HUD_ROUTE_BLIP", "PosEnabled", _routePosBlipEnabled);
                _routeFinishBlipEnabled = GetDesignBool(design, "HUD_ROUTE_BLIP", "FinishEnabled", _routeFinishBlipEnabled);

                _routePosBlipSprite = GetDesignEnum(design, "HUD_ROUTE_BLIP", "PosSprite", _routePosBlipSprite);
                _routeActivePosBlipColor = GetDesignEnum(design, "HUD_ROUTE_BLIP", "ActivePosColor", _routeActivePosBlipColor);
                _routeActivePosBlipScale = GetDesignFloat(design, "HUD_ROUTE_BLIP", "ActivePosScale", _routeActivePosBlipScale);

                _routeFinishBlipSprite = GetDesignEnum(design, "HUD_ROUTE_BLIP", "FinishSprite", _routeFinishBlipSprite);
                _routeActiveFinishBlipColor = GetDesignEnum(design, "HUD_ROUTE_BLIP", "ActiveFinishColor", _routeActiveFinishBlipColor);
                _routeActiveFinishBlipScale = GetDesignFloat(design, "HUD_ROUTE_BLIP", "ActiveFinishScale", _routeActiveFinishBlipScale);
                _routeFutureFinishBlipColor = GetDesignEnum(design, "HUD_ROUTE_BLIP", "FutureFinishColor", _routeFutureFinishBlipColor);
                _routeFutureFinishBlipScale = GetDesignFloat(design, "HUD_ROUTE_BLIP", "FutureFinishScale", _routeFutureFinishBlipScale);

                _routeDirectMissionFinishName = GetDesignString(design, "HUD_ROUTE_BLIP", "DirectMissionFinishName", _routeDirectMissionFinishName);
                _routeMissionPosNameFormat = GetDesignString(design, "HUD_ROUTE_BLIP", "MissionPosNameFormat", _routeMissionPosNameFormat);
                _routeNormalPosNameFormat = GetDesignString(design, "HUD_ROUTE_BLIP", "NormalPosNameFormat", _routeNormalPosNameFormat);
                _routeMissionFinishName = GetDesignString(design, "HUD_ROUTE_BLIP", "MissionFinishName", _routeMissionFinishName);
                _routeNormalFinishName = GetDesignString(design, "HUD_ROUTE_BLIP", "NormalFinishName", _routeNormalFinishName);
                _routeFutureMissionFinishName = GetDesignString(design, "HUD_ROUTE_BLIP", "FutureMissionFinishName", _routeFutureMissionFinishName);
                _routeFutureNormalFinishName = GetDesignString(design, "HUD_ROUTE_BLIP", "FutureNormalFinishName", _routeFutureNormalFinishName);

                // =====================================================
                // Common: Enabled, PosX, PosY, Width, Height,
                // Background*, Text*
                // =====================================================
                _hudHealthEnabled = GetDesignBool(design, "HUD_HEALTH", "Enabled", _hudHealthEnabled);
                _healthPanelLabel = GetDesignString(design, "HUD_HEALTH", "HealthLabel", _healthPanelLabel);

                _healthTrackHeight = GetDesignFloat(design, "HUD_HEALTH", "TrackHeight", _healthTrackHeight);
                _healthTrackOffsetY = GetDesignFloat(design, "HUD_HEALTH", "TrackOffsetY", _healthTrackOffsetY);
                _healthValueColumnRatio = GetDesignFloat(design, "HUD_HEALTH", "ValueColumnRatio", _healthValueColumnRatio);
                _healthBorderSize = GetDesignFloat(design, "HUD_HEALTH", "BorderSize", _healthBorderSize);
                _healthBorderR = GetDesignInt(design, "HUD_HEALTH", "BorderR", _healthBorderR);
                _healthBorderG = GetDesignInt(design, "HUD_HEALTH", "BorderG", _healthBorderG);
                _healthBorderB = GetDesignInt(design, "HUD_HEALTH", "BorderB", _healthBorderB);
                _healthBorderA = GetDesignInt(design, "HUD_HEALTH", "BorderA", _healthBorderA);
                _hpBarX = GetDesignFloat(design, "HUD_HEALTH", "PosX", _hpBarX);
                _hpBarY = GetDesignFloat(design, "HUD_HEALTH", "PosY", _hpBarY);
                _hpBarWidth = GetDesignFloat(design, "HUD_HEALTH", "Width", _hpBarWidth);
                _hpBarHeight = GetDesignFloat(design, "HUD_HEALTH", "Height", _hpBarHeight);

                _hpBgR = GetDesignInt(design, "HUD_HEALTH", "BackgroundR", _hpBgR);
                _hpBgG = GetDesignInt(design, "HUD_HEALTH", "BackgroundG", _hpBgG);
                _hpBgB = GetDesignInt(design, "HUD_HEALTH", "BackgroundB", _hpBgB);
                _hpBgA = GetDesignInt(design, "HUD_HEALTH", "BackgroundA", _hpBgA);

                _hpFillR = GetDesignInt(design, "HUD_HEALTH", "HealthFillR", _hpFillR);
                _hpFillG = GetDesignInt(design, "HUD_HEALTH", "HealthFillG", _hpFillG);
                _hpFillB = GetDesignInt(design, "HUD_HEALTH", "HealthFillB", _hpFillB);
                _hpFillA = GetDesignInt(design, "HUD_HEALTH", "HealthFillA", _hpFillA);





                _hpTextOffsetY = GetDesignFloat(design, "HUD_HEALTH", "TextOffsetY", _hpTextOffsetY);
                _hpTextScale = GetDesignFloat(design, "HUD_HEALTH", "TextScale", _hpTextScale);
                _hpTextFont = GetDesignInt(design, "HUD_HEALTH", "TextFont", _hpTextFont);
                _hpTextR = GetDesignInt(design, "HUD_HEALTH", "TextR", _hpTextR);
                _hpTextG = GetDesignInt(design, "HUD_HEALTH", "TextG", _hpTextG);
                _hpTextB = GetDesignInt(design, "HUD_HEALTH", "TextB", _hpTextB);
                _hpTextA = GetDesignInt(design, "HUD_HEALTH", "TextA", _hpTextA);
                _hpTextFormat = GetDesignString(design, "HUD_HEALTH", "HealthTextFormat", _hpTextFormat);










                // =====================================================
                // WIN
                // =====================================================
                _winLabel = GetDesignString(design, "HUD_WIN", "Label", _winLabel);
                _winSubtitle = GetDesignString(design, "HUD_WIN", "Subtitle", _winSubtitle);
                _winSubtitleScale = GetDesignFloat(design, "HUD_WIN", "SubtitleScale", _winSubtitleScale);
                _winSubtitleR = GetDesignInt(design, "HUD_WIN", "SubtitleR", _winSubtitleR);
                _winSubtitleG = GetDesignInt(design, "HUD_WIN", "SubtitleG", _winSubtitleG);
                _winSubtitleB = GetDesignInt(design, "HUD_WIN", "SubtitleB", _winSubtitleB);
                _winSubtitleA = GetDesignInt(design, "HUD_WIN", "SubtitleA", _winSubtitleA);
                _winPosX = GetDesignFloat(design, "HUD_WIN", "PosX", _winPosX);
                _winPosY = GetDesignFloat(design, "HUD_WIN", "PosY", _winPosY);
                _winTextOffsetX = GetDesignFloat(design, "HUD_WIN", "TextOffsetX", _winTextOffsetX);
                _winTextOffsetY = GetDesignFloat(design, "HUD_WIN", "TextOffsetY", _winTextOffsetY);
                _winValueTextOffsetY = GetDesignFloat(design, "HUD_WIN", "ValueTextOffsetY", _winValueTextOffsetY);
                _winBgWidth = GetDesignFloat(design, "HUD_WIN", "Width", _winBgWidth);
                _winBgHeight = GetDesignFloat(design, "HUD_WIN", "Height", _winBgHeight);
                _winScale = GetDesignFloat(design, "HUD_WIN", "TextScale", _winScale);
                _winFont = GetDesignInt(design, "HUD_WIN", "TextFont", _winFont);
                _winR = GetDesignInt(design, "HUD_WIN", "TextR", _winR);
                _winG = GetDesignInt(design, "HUD_WIN", "TextG", _winG);
                _winB = GetDesignInt(design, "HUD_WIN", "TextB", _winB);
                _winA = GetDesignInt(design, "HUD_WIN", "TextA", _winA);
                _winBgR = GetDesignInt(design, "HUD_WIN", "BackgroundR", _winBgR);
                _winBgG = GetDesignInt(design, "HUD_WIN", "BackgroundG", _winBgG);
                _winBgB = GetDesignInt(design, "HUD_WIN", "BackgroundB", _winBgB);
                _winBgA = GetDesignInt(design, "HUD_WIN", "BackgroundA", _winBgA);

                // =====================================================
                // DEATH
                // =====================================================
                _deathLabel = GetDesignString(design, "HUD_DEATH", "Label", _deathLabel);
                _deathSubtitle = GetDesignString(design, "HUD_DEATH", "Subtitle", _deathSubtitle);
                _deathSubtitleScale = GetDesignFloat(design, "HUD_DEATH", "SubtitleScale", _deathSubtitleScale);
                _deathSubtitleR = GetDesignInt(design, "HUD_DEATH", "SubtitleR", _deathSubtitleR);
                _deathSubtitleG = GetDesignInt(design, "HUD_DEATH", "SubtitleG", _deathSubtitleG);
                _deathSubtitleB = GetDesignInt(design, "HUD_DEATH", "SubtitleB", _deathSubtitleB);
                _deathSubtitleA = GetDesignInt(design, "HUD_DEATH", "SubtitleA", _deathSubtitleA);
                _deathPosX = GetDesignFloat(design, "HUD_DEATH", "PosX", _deathPosX);
                _deathPosY = GetDesignFloat(design, "HUD_DEATH", "PosY", _deathPosY);
                _deathTextOffsetX = GetDesignFloat(design, "HUD_DEATH", "TextOffsetX", _deathTextOffsetX);
                _deathTextOffsetY = GetDesignFloat(design, "HUD_DEATH", "TextOffsetY", _deathTextOffsetY);
                _deathValueTextOffsetY = GetDesignFloat(design, "HUD_DEATH", "ValueTextOffsetY", _deathValueTextOffsetY);
                _deathBgWidth = GetDesignFloat(design, "HUD_DEATH", "Width", _deathBgWidth);
                _deathBgHeight = GetDesignFloat(design, "HUD_DEATH", "Height", _deathBgHeight);
                _deathScale = GetDesignFloat(design, "HUD_DEATH", "TextScale", _deathScale);
                _deathFont = GetDesignInt(design, "HUD_DEATH", "TextFont", _deathFont);
                _deathR = GetDesignInt(design, "HUD_DEATH", "TextR", _deathR);
                _deathG = GetDesignInt(design, "HUD_DEATH", "TextG", _deathG);
                _deathB = GetDesignInt(design, "HUD_DEATH", "TextB", _deathB);
                _deathA = GetDesignInt(design, "HUD_DEATH", "TextA", _deathA);
                _deathBgR = GetDesignInt(design, "HUD_DEATH", "BackgroundR", _deathBgR);
                _deathBgG = GetDesignInt(design, "HUD_DEATH", "BackgroundG", _deathBgG);
                _deathBgB = GetDesignInt(design, "HUD_DEATH", "BackgroundB", _deathBgB);
                _deathBgA = GetDesignInt(design, "HUD_DEATH", "BackgroundA", _deathBgA);

                // =====================================================
                // SCORE MODE - master switch untuk SCORE vs WINS/DEATHS
                // =====================================================
                _scoreModeEnabled = GetDesignBool(design, "HUD_SCORE", "Score", _scoreModeEnabled);
                _scorePosX = GetDesignFloat(design, "HUD_SCORE", "PosX", _scorePosX);
                _scorePosY = GetDesignFloat(design, "HUD_SCORE", "PosY", _scorePosY);
                _scoreMinWidth = GetDesignFloat(design, "HUD_SCORE", "Width", _scoreMinWidth);
                _scoreMaxWidth = GetDesignFloat(design, "HUD_SCORE", "MaxWidth", _scoreMaxWidth);
                _scoreHeight = GetDesignFloat(design, "HUD_SCORE", "Height", _scoreHeight);
                _scorePaddingX = GetDesignFloat(design, "HUD_SCORE", "PaddingX", _scorePaddingX);
                _scoreTextOffsetY = GetDesignFloat(design, "HUD_SCORE", "TextOffsetY", _scoreTextOffsetY);
                _scoreTextScale = GetDesignFloat(design, "HUD_SCORE", "TextScale", _scoreTextScale);
                _scoreTextFont = GetDesignInt(design, "HUD_SCORE", "TextFont", _scoreTextFont);

                _scoreLabelR = GetDesignInt(design, "HUD_SCORE", "LabelR", _scoreLabelR);
                _scoreLabelG = GetDesignInt(design, "HUD_SCORE", "LabelG", _scoreLabelG);
                _scoreLabelB = GetDesignInt(design, "HUD_SCORE", "LabelB", _scoreLabelB);
                _scoreLabelA = GetDesignInt(design, "HUD_SCORE", "LabelA", _scoreLabelA);

                _scorePositiveR = GetDesignInt(design, "HUD_SCORE", "PositiveR", _scorePositiveR);
                _scorePositiveG = GetDesignInt(design, "HUD_SCORE", "PositiveG", _scorePositiveG);
                _scorePositiveB = GetDesignInt(design, "HUD_SCORE", "PositiveB", _scorePositiveB);
                _scorePositiveA = GetDesignInt(design, "HUD_SCORE", "PositiveA", _scorePositiveA);

                _scoreNegativeR = GetDesignInt(design, "HUD_SCORE", "NegativeR", _scoreNegativeR);
                _scoreNegativeG = GetDesignInt(design, "HUD_SCORE", "NegativeG", _scoreNegativeG);
                _scoreNegativeB = GetDesignInt(design, "HUD_SCORE", "NegativeB", _scoreNegativeB);
                _scoreNegativeA = GetDesignInt(design, "HUD_SCORE", "NegativeA", _scoreNegativeA);

                _scoreZeroR = GetDesignInt(design, "HUD_SCORE", "ZeroR", _scoreZeroR);
                _scoreZeroG = GetDesignInt(design, "HUD_SCORE", "ZeroG", _scoreZeroG);
                _scoreZeroB = GetDesignInt(design, "HUD_SCORE", "ZeroB", _scoreZeroB);
                _scoreZeroA = GetDesignInt(design, "HUD_SCORE", "ZeroA", _scoreZeroA);

                _scoreBgR = GetDesignInt(design, "HUD_SCORE", "BackgroundR", _scoreBgR);
                _scoreBgG = GetDesignInt(design, "HUD_SCORE", "BackgroundG", _scoreBgG);
                _scoreBgB = GetDesignInt(design, "HUD_SCORE", "BackgroundB", _scoreBgB);
                _scoreBgA = GetDesignInt(design, "HUD_SCORE", "BackgroundA", _scoreBgA);

                // =====================================================
                // RANDOM CHAOS COUNTDOWN / ACTIVE CHAOS TIMER
                // =====================================================
                _hudApocalypseEnabled = GetDesignBool(design, "HUD_APOCALYPSE", "Enabled", _hudApocalypseEnabled);
                _apocalypsePosX = GetDesignFloat(design, "HUD_APOCALYPSE", "PosX", _apocalypsePosX);
                _apocalypsePosY = GetDesignFloat(design, "HUD_APOCALYPSE", "PosY", _apocalypsePosY);
                _apocalypseTextOffsetX = GetDesignFloat(design, "HUD_APOCALYPSE", "TextOffsetX", _apocalypseTextOffsetX);
                _apocalypseBgWidth = GetDesignFloat(design, "HUD_APOCALYPSE", "Width", _apocalypseBgWidth);
                _apocalypseBgHeight = GetDesignFloat(design, "HUD_APOCALYPSE", "Height", _apocalypseBgHeight);
                _apocalypseScale = GetDesignFloat(design, "HUD_APOCALYPSE", "TextScale", _apocalypseScale);
                _apocalypseFont = GetDesignInt(design, "HUD_APOCALYPSE", "TextFont", _apocalypseFont);
                _apocalypseR = GetDesignInt(design, "HUD_APOCALYPSE", "TextR", _apocalypseR);
                _apocalypseG = GetDesignInt(design, "HUD_APOCALYPSE", "TextG", _apocalypseG);
                _apocalypseB = GetDesignInt(design, "HUD_APOCALYPSE", "TextB", _apocalypseB);
                _apocalypseA = GetDesignInt(design, "HUD_APOCALYPSE", "TextA", _apocalypseA);
                _apocalypseBgR = GetDesignInt(design, "HUD_APOCALYPSE", "BackgroundR", _apocalypseBgR);
                _apocalypseBgG = GetDesignInt(design, "HUD_APOCALYPSE", "BackgroundG", _apocalypseBgG);
                _apocalypseBgB = GetDesignInt(design, "HUD_APOCALYPSE", "BackgroundB", _apocalypseBgB);
                _apocalypseBgA = GetDesignInt(design, "HUD_APOCALYPSE", "BackgroundA", _apocalypseBgA);
                _apocalypseCountdownLine1Format = GetDesignString(design, "HUD_APOCALYPSE", "CountdownLine1Format", _apocalypseCountdownLine1Format);
                _apocalypseCountdownLine2 = GetDesignString(design, "HUD_APOCALYPSE", "CountdownLine2", _apocalypseCountdownLine2);
                _apocalypseChaosLine1Format = GetDesignString(design, "HUD_APOCALYPSE", "ChaosLine1Format", _apocalypseChaosLine1Format);
                _apocalypseChaosLine2Format = GetDesignString(design, "HUD_APOCALYPSE", "ChaosLine2Format", _apocalypseChaosLine2Format);

                // =====================================================
                // ROUTE / MISSION PROGRESS
                // =====================================================
                _hudProgressEnabled = GetDesignBool(design, "HUD_PROGRESS", "Enabled", _hudProgressEnabled);
                _barX = GetDesignFloat(design, "HUD_PROGRESS", "PosX", _barX);
                _barY = GetDesignFloat(design, "HUD_PROGRESS", "PosY", _barY);
                _barWidth = GetDesignFloat(design, "HUD_PROGRESS", "Width", _barWidth);
                _barHeight = GetDesignFloat(design, "HUD_PROGRESS", "Height", _barHeight);
                _bgR = GetDesignInt(design, "HUD_PROGRESS", "BackgroundR", _bgR);
                _bgG = GetDesignInt(design, "HUD_PROGRESS", "BackgroundG", _bgG);
                _bgB = GetDesignInt(design, "HUD_PROGRESS", "BackgroundB", _bgB);
                _bgA = GetDesignInt(design, "HUD_PROGRESS", "BackgroundA", _bgA);
                _fillR = GetDesignInt(design, "HUD_PROGRESS", "FillR", _fillR);
                _fillG = GetDesignInt(design, "HUD_PROGRESS", "FillG", _fillG);
                _fillB = GetDesignInt(design, "HUD_PROGRESS", "FillB", _fillB);
                _fillA = GetDesignInt(design, "HUD_PROGRESS", "FillA", _fillA);
                _textMeterOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "TextOffsetY", _textMeterOffsetY);
                _textMeterScale = GetDesignFloat(design, "HUD_PROGRESS", "TextScale", _textMeterScale);
                _textMeterFont = GetDesignInt(design, "HUD_PROGRESS", "TextFont", _textMeterFont);
                _textMeterR = GetDesignInt(design, "HUD_PROGRESS", "TextR", _textMeterR);
                _textMeterG = GetDesignInt(design, "HUD_PROGRESS", "TextG", _textMeterG);
                _textMeterB = GetDesignInt(design, "HUD_PROGRESS", "TextB", _textMeterB);
                _textMeterA = GetDesignInt(design, "HUD_PROGRESS", "TextA", _textMeterA);
                _progressPinOuterWidth = GetDesignFloat(design, "HUD_PROGRESS", "PinOuterWidth", _progressPinOuterWidth);
                _progressPinOuterExtraHeight = GetDesignFloat(design, "HUD_PROGRESS", "PinOuterExtraHeight", _progressPinOuterExtraHeight);
                _progressPinOuterR = GetDesignInt(design, "HUD_PROGRESS", "PinOuterR", _progressPinOuterR);
                _progressPinOuterG = GetDesignInt(design, "HUD_PROGRESS", "PinOuterG", _progressPinOuterG);
                _progressPinOuterB = GetDesignInt(design, "HUD_PROGRESS", "PinOuterB", _progressPinOuterB);
                _progressPinOuterA = GetDesignInt(design, "HUD_PROGRESS", "PinOuterA", _progressPinOuterA);
                _progressPinInnerWidth = GetDesignFloat(design, "HUD_PROGRESS", "PinInnerWidth", _progressPinInnerWidth);
                _progressPinInnerExtraHeight = GetDesignFloat(design, "HUD_PROGRESS", "PinInnerExtraHeight", _progressPinInnerExtraHeight);
                _progressPinInnerR = GetDesignInt(design, "HUD_PROGRESS", "PinInnerR", _progressPinInnerR);
                _progressPinInnerG = GetDesignInt(design, "HUD_PROGRESS", "PinInnerG", _progressPinInnerG);
                _progressPinInnerB = GetDesignInt(design, "HUD_PROGRESS", "PinInnerB", _progressPinInnerB);
                _progressPinInnerA = GetDesignInt(design, "HUD_PROGRESS", "PinInnerA", _progressPinInnerA);
                _missionTextOffsetX = GetDesignFloat(design, "HUD_PROGRESS", "MissionTextOffsetX", _missionTextOffsetX);
                _missionTextOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "MissionTextOffsetY", _missionTextOffsetY);
                _missionTextScale = GetDesignFloat(design, "HUD_PROGRESS", "MissionTextScale", _missionTextScale);
                _missionTextWidthMultiplier = GetDesignFloat(design, "HUD_PROGRESS", "MissionTextWidthMultiplier", _missionTextWidthMultiplier);
                _missionTextR = GetDesignInt(design, "HUD_PROGRESS", "MissionTextR", _missionTextR);
                _missionTextG = GetDesignInt(design, "HUD_PROGRESS", "MissionTextG", _missionTextG);
                _missionTextB = GetDesignInt(design, "HUD_PROGRESS", "MissionTextB", _missionTextB);
                _missionTextA = GetDesignInt(design, "HUD_PROGRESS", "MissionTextA", _missionTextA);
                _missionTextFont = GetDesignInt(design, "HUD_PROGRESS", "MissionTextFont", _missionTextFont);
                _progressDistanceTextFormat = GetDesignString(design, "HUD_PROGRESS", "DistanceTextFormat", _progressDistanceTextFormat);
                _progressMissionGoToMountainText = GetDesignString(design, "HUD_PROGRESS", "MissionGoToMountainText", _progressMissionGoToMountainText);
                _progressMissionSurvivalText = GetDesignString(design, "HUD_PROGRESS", "MissionSurvivalText", _progressMissionSurvivalText);
                _progressMissionNormalText = GetDesignString(design, "HUD_PROGRESS", "MissionNormalText", _progressMissionNormalText);
                _progressPosTextFormat = GetDesignString(design, "HUD_PROGRESS", "PosTextFormat", _progressPosTextFormat);
                _progressMissionWithPosFormat = GetDesignString(design, "HUD_PROGRESS", "MissionWithPosFormat", _progressMissionWithPosFormat);
                _progressMissionTextUppercase = GetDesignBool(design, "HUD_PROGRESS", "MissionTextUppercase", _progressMissionTextUppercase);

                _progressPanelWidth = GetDesignFloat(design, "HUD_PROGRESS", "PanelWidth", _progressPanelWidth);
                _progressPanelHeight = GetDesignFloat(design, "HUD_PROGRESS", "PanelHeight", _progressPanelHeight);
                _progressPanelCornerRadius = GetDesignFloat(design, "HUD_PROGRESS", "PanelCornerRadius", _progressPanelCornerRadius);
                _progressPanelR = GetDesignInt(design, "HUD_PROGRESS", "PanelR", _progressPanelR);
                _progressPanelG = GetDesignInt(design, "HUD_PROGRESS", "PanelG", _progressPanelG);
                _progressPanelB = GetDesignInt(design, "HUD_PROGRESS", "PanelB", _progressPanelB);
                _progressPanelA = GetDesignInt(design, "HUD_PROGRESS", "PanelA", _progressPanelA);

                _progressPanelShadowOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "PanelShadowOffsetY", _progressPanelShadowOffsetY);
                _progressPanelShadowExtraWidth = GetDesignFloat(design, "HUD_PROGRESS", "PanelShadowExtraWidth", _progressPanelShadowExtraWidth);
                _progressPanelShadowExtraHeight = GetDesignFloat(design, "HUD_PROGRESS", "PanelShadowExtraHeight", _progressPanelShadowExtraHeight);
                _progressPanelShadowR = GetDesignInt(design, "HUD_PROGRESS", "PanelShadowR", _progressPanelShadowR);
                _progressPanelShadowG = GetDesignInt(design, "HUD_PROGRESS", "PanelShadowG", _progressPanelShadowG);
                _progressPanelShadowB = GetDesignInt(design, "HUD_PROGRESS", "PanelShadowB", _progressPanelShadowB);
                _progressPanelShadowA = GetDesignInt(design, "HUD_PROGRESS", "PanelShadowA", _progressPanelShadowA);

                _progressPanelDividerEnabled = GetDesignBool(design, "HUD_PROGRESS", "PanelDividerEnabled", _progressPanelDividerEnabled);
                _progressPanelHorizontalDividerOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "PanelHorizontalDividerOffsetY", _progressPanelHorizontalDividerOffsetY);
                _progressPanelDividerThickness = GetDesignFloat(design, "HUD_PROGRESS", "PanelDividerThickness", _progressPanelDividerThickness);
                _progressPanelLeftDividerOffsetX = GetDesignFloat(design, "HUD_PROGRESS", "PanelLeftDividerOffsetX", _progressPanelLeftDividerOffsetX);
                _progressPanelRightDividerOffsetX = GetDesignFloat(design, "HUD_PROGRESS", "PanelRightDividerOffsetX", _progressPanelRightDividerOffsetX);
                _progressPanelDividerR = GetDesignInt(design, "HUD_PROGRESS", "PanelDividerR", _progressPanelDividerR);
                _progressPanelDividerG = GetDesignInt(design, "HUD_PROGRESS", "PanelDividerG", _progressPanelDividerG);
                _progressPanelDividerB = GetDesignInt(design, "HUD_PROGRESS", "PanelDividerB", _progressPanelDividerB);
                _progressPanelDividerA = GetDesignInt(design, "HUD_PROGRESS", "PanelDividerA", _progressPanelDividerA);

                _progressTopTextOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "TopTextOffsetY", _progressTopTextOffsetY);
                _progressTrackOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "TrackOffsetY", _progressTrackOffsetY);

                _progressPercentOffsetX = GetDesignFloat(design, "HUD_PROGRESS", "PercentOffsetX", _progressPercentOffsetX);
                _progressPercentOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "PercentOffsetY", _progressPercentOffsetY);
                _progressPercentScale = GetDesignFloat(design, "HUD_PROGRESS", "PercentScale", _progressPercentScale);
                _progressPercentFont = GetDesignInt(design, "HUD_PROGRESS", "PercentFont", _progressPercentFont);
                _progressPercentR = GetDesignInt(design, "HUD_PROGRESS", "PercentR", _progressPercentR);
                _progressPercentG = GetDesignInt(design, "HUD_PROGRESS", "PercentG", _progressPercentG);
                _progressPercentB = GetDesignInt(design, "HUD_PROGRESS", "PercentB", _progressPercentB);
                _progressPercentA = GetDesignInt(design, "HUD_PROGRESS", "PercentA", _progressPercentA);

                _progressRightOffsetX = GetDesignFloat(design, "HUD_PROGRESS", "RightTextOffsetX", _progressRightOffsetX);
                _progressRightOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "RightTextOffsetY", _progressRightOffsetY);
                _progressRightScale = GetDesignFloat(design, "HUD_PROGRESS", "RightTextScale", _progressRightScale);
                _progressRightFont = GetDesignInt(design, "HUD_PROGRESS", "RightTextFont", _progressRightFont);
                _progressRightR = GetDesignInt(design, "HUD_PROGRESS", "RightTextR", _progressRightR);
                _progressRightG = GetDesignInt(design, "HUD_PROGRESS", "RightTextG", _progressRightG);
                _progressRightB = GetDesignInt(design, "HUD_PROGRESS", "RightTextB", _progressRightB);
                _progressRightA = GetDesignInt(design, "HUD_PROGRESS", "RightTextA", _progressRightA);

                _progressCaptionText = GetDesignString(design, "HUD_PROGRESS", "CaptionText", _progressCaptionText);
                _progressHostNameEnabled = GetDesignBool(design, "HUD_PROGRESS", "HostNameEnabled", _progressHostNameEnabled);
                _progressHostTextScaleRatio = GetDesignFloat(design, "HUD_PROGRESS", "HostTextScaleRatio", _progressHostTextScaleRatio);
                _progressHostNameUppercase = GetDesignBool(design, "HUD_PROGRESS", "HostNameUppercase", _progressHostNameUppercase);
                _progressHostTextR = GetDesignInt(design, "HUD_PROGRESS", "HostTextR", _progressHostTextR);
                _progressHostTextG = GetDesignInt(design, "HUD_PROGRESS", "HostTextG", _progressHostTextG);
                _progressHostTextB = GetDesignInt(design, "HUD_PROGRESS", "HostTextB", _progressHostTextB);
                _progressHostTextA = GetDesignInt(design, "HUD_PROGRESS", "HostTextA", _progressHostTextA);
                _progressDynamicPanelEnabled = GetDesignBool(design, "HUD_PROGRESS", "DynamicPanelEnabled", _progressDynamicPanelEnabled);
                _progressDynamicPanelMaxWidth = GetDesignFloat(design, "HUD_PROGRESS", "DynamicPanelMaxWidth", _progressDynamicPanelMaxWidth);
                _progressDynamicPanelPadding = GetDesignFloat(design, "HUD_PROGRESS", "DynamicPanelPadding", _progressDynamicPanelPadding);
                _progressPosCaptionText = GetDesignString(design, "HUD_PROGRESS", "PosCaptionText", _progressPosCaptionText);
                _progressDirectCaptionText = GetDesignString(design, "HUD_PROGRESS", "DirectCaptionText", _progressDirectCaptionText);
                _progressDirectValueText = GetDesignString(design, "HUD_PROGRESS", "DirectValueText", _progressDirectValueText);
                _progressDirectGoToMountainValueText = GetDesignString(
                    design,
                    "HUD_PROGRESS",
                    "DirectGoToMountainValueText",
                    _progressDirectValueText);
                _progressDirectSurvivalValueText = GetDesignString(
                    design,
                    "HUD_PROGRESS",
                    "DirectSurvivalValueText",
                    "FINISH LINE");

                // Legacy CaptionScale tetap dibaca sebagai fallback.
                _progressCaptionScale = GetDesignFloat(design, "HUD_PROGRESS", "CaptionScale", _progressCaptionScale);

                // Kontrol kiri PROGRESS.
                _progressCaptionOffsetX = GetDesignFloat(design, "HUD_PROGRESS", "ProgressCaptionOffsetX", _progressCaptionOffsetX);
                _progressCaptionOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "ProgressCaptionOffsetY", _progressCaptionOffsetY);
                _progressCaptionScale = GetDesignFloat(design, "HUD_PROGRESS", "ProgressCaptionScale", _progressCaptionScale);

                // Kontrol kanan POS - TIDAK lagi mengikuti PROGRESS.
                _progressPosCaptionOffsetX = GetDesignFloat(design, "HUD_PROGRESS", "PosCaptionOffsetX", _progressPosCaptionOffsetX);
                _progressPosCaptionOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "PosCaptionOffsetY", _progressPosCaptionOffsetY);
                _progressPosCaptionScale = GetDesignFloat(design, "HUD_PROGRESS", "PosCaptionScale", _progressPosCaptionScale);

                // Value kanan (1/7, 3/8, dst) punya posisi Y sendiri pada layout normal.
                _progressRightValueOffsetY = GetDesignFloat(design, "HUD_PROGRESS", "RightValueOffsetY", _progressRightValueOffsetY);

                _progressCaptionR = GetDesignInt(design, "HUD_PROGRESS", "CaptionR", _progressCaptionR);
                _progressCaptionG = GetDesignInt(design, "HUD_PROGRESS", "CaptionG", _progressCaptionG);
                _progressCaptionB = GetDesignInt(design, "HUD_PROGRESS", "CaptionB", _progressCaptionB);
                _progressCaptionA = GetDesignInt(design, "HUD_PROGRESS", "CaptionA", _progressCaptionA);
                _progressMissionAccentR = GetDesignInt(design, "HUD_PROGRESS", "MissionAccentR", _progressMissionAccentR);
                _progressMissionAccentG = GetDesignInt(design, "HUD_PROGRESS", "MissionAccentG", _progressMissionAccentG);
                _progressMissionAccentB = GetDesignInt(design, "HUD_PROGRESS", "MissionAccentB", _progressMissionAccentB);
                _progressMissionAccentA = GetDesignInt(design, "HUD_PROGRESS", "MissionAccentA", _progressMissionAccentA);

                _progressTrackR = GetDesignInt(design, "HUD_PROGRESS", "TrackR", _progressTrackR);
                _progressTrackG = GetDesignInt(design, "HUD_PROGRESS", "TrackG", _progressTrackG);
                _progressTrackB = GetDesignInt(design, "HUD_PROGRESS", "TrackB", _progressTrackB);
                _progressTrackA = GetDesignInt(design, "HUD_PROGRESS", "TrackA", _progressTrackA);
                _progressPinEnabled = GetDesignBool(design, "HUD_PROGRESS", "PinEnabled", _progressPinEnabled);

                // =====================================================
                // RANDOM CHECKPOINT HUD
                // =====================================================
                _hudRandomCheckpointEnabled = GetDesignBool(design, "HUD_RANDOM_CHECKPOINT", "Enabled", _hudRandomCheckpointEnabled);
                _randomCheckpointMarkerWidth = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "MarkerWidth", _randomCheckpointMarkerWidth);
                _randomCheckpointMarkerHeightMultiplier = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "MarkerHeightMultiplier", _randomCheckpointMarkerHeightMultiplier);
                _randomCheckpointMarkerOffsetY = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "MarkerOffsetY", _randomCheckpointMarkerOffsetY);
                _randomCheckpointMarkerR = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "MarkerR", _randomCheckpointMarkerR);
                _randomCheckpointMarkerG = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "MarkerG", _randomCheckpointMarkerG);
                _randomCheckpointMarkerB = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "MarkerB", _randomCheckpointMarkerB);
                _randomCheckpointMarkerA = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "MarkerA", _randomCheckpointMarkerA);
                _randomCheckpointMarkerFlagEnabled = GetDesignBool(design, "HUD_RANDOM_CHECKPOINT", "MarkerFlagEnabled", _randomCheckpointMarkerFlagEnabled);
                _randomCheckpointMarkerFlagWidth = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "MarkerFlagWidth", _randomCheckpointMarkerFlagWidth);
                _randomCheckpointMarkerFlagHeight = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "MarkerFlagHeight", _randomCheckpointMarkerFlagHeight);
                _randomCheckpointMarkerFlagOffsetY = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "MarkerFlagOffsetY", _randomCheckpointMarkerFlagOffsetY);

                _randomCheckpointCountdownBarR = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownBarR", _randomCheckpointCountdownBarR);
                _randomCheckpointCountdownBarG = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownBarG", _randomCheckpointCountdownBarG);
                _randomCheckpointCountdownBarB = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownBarB", _randomCheckpointCountdownBarB);
                _randomCheckpointCountdownBarA = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownBarA", _randomCheckpointCountdownBarA);
                _randomCheckpointGachaBarR = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaBarR", _randomCheckpointGachaBarR);
                _randomCheckpointGachaBarG = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaBarG", _randomCheckpointGachaBarG);
                _randomCheckpointGachaBarB = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaBarB", _randomCheckpointGachaBarB);
                _randomCheckpointGachaBarA = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaBarA", _randomCheckpointGachaBarA);
                _randomCheckpointLockedBarR = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedBarR", _randomCheckpointLockedBarR);
                _randomCheckpointLockedBarG = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedBarG", _randomCheckpointLockedBarG);
                _randomCheckpointLockedBarB = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedBarB", _randomCheckpointLockedBarB);
                _randomCheckpointLockedBarA = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedBarA", _randomCheckpointLockedBarA);

                _randomCheckpointUiX = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "PosX", _randomCheckpointUiX);
                _randomCheckpointUiY = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "PosY", _randomCheckpointUiY);

                _randomCheckpointTitle = GetDesignString(design, "HUD_RANDOM_CHECKPOINT", "Title", _randomCheckpointTitle);
                _randomCheckpointTitleScale = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "TitleScale", _randomCheckpointTitleScale);
                _randomCheckpointTitleFont = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "TitleFont", _randomCheckpointTitleFont);
                _randomCheckpointTitleR = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "TitleR", _randomCheckpointTitleR);
                _randomCheckpointTitleG = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "TitleG", _randomCheckpointTitleG);
                _randomCheckpointTitleB = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "TitleB", _randomCheckpointTitleB);
                _randomCheckpointTitleA = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "TitleA", _randomCheckpointTitleA);

                _randomCheckpointCountdownOffsetY = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "CountdownOffsetY", _randomCheckpointCountdownOffsetY);
                _randomCheckpointCountdownScale = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "CountdownScale", _randomCheckpointCountdownScale);
                _randomCheckpointCountdownFont = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownFont", _randomCheckpointCountdownFont);
                _randomCheckpointCountdownR = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownR", _randomCheckpointCountdownR);
                _randomCheckpointCountdownG = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownG", _randomCheckpointCountdownG);
                _randomCheckpointCountdownB = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownB", _randomCheckpointCountdownB);
                _randomCheckpointCountdownA = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "CountdownA", _randomCheckpointCountdownA);

                _randomCheckpointGachaOffsetY = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "GachaOffsetY", _randomCheckpointGachaOffsetY);
                _randomCheckpointGachaScale = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "GachaScale", _randomCheckpointGachaScale);
                _randomCheckpointGachaFont = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaFont", _randomCheckpointGachaFont);
                _randomCheckpointGachaR = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaR", _randomCheckpointGachaR);
                _randomCheckpointGachaG = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaG", _randomCheckpointGachaG);
                _randomCheckpointGachaB = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaB", _randomCheckpointGachaB);
                _randomCheckpointGachaA = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "GachaA", _randomCheckpointGachaA);

                _randomCheckpointLockedText = GetDesignString(design, "HUD_RANDOM_CHECKPOINT", "LockedText", _randomCheckpointLockedText);
                _randomCheckpointLockedOffsetY = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "LockedOffsetY", _randomCheckpointLockedOffsetY);
                _randomCheckpointLockedScale = GetDesignFloat(design, "HUD_RANDOM_CHECKPOINT", "LockedScale", _randomCheckpointLockedScale);
                _randomCheckpointLockedFont = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedFont", _randomCheckpointLockedFont);
                _randomCheckpointLockedR = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedR", _randomCheckpointLockedR);
                _randomCheckpointLockedG = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedG", _randomCheckpointLockedG);
                _randomCheckpointLockedB = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedB", _randomCheckpointLockedB);
                _randomCheckpointLockedA = GetDesignInt(design, "HUD_RANDOM_CHECKPOINT", "LockedA", _randomCheckpointLockedA);

                // =====================================================
                // INSTANT GIFT HUD
                // =====================================================
                _hudInstantEnabled = GetDesignBool(design, "HUD_INSTANT", "Enabled", _hudInstantEnabled);
                _instantHudX = GetDesignFloat(design, "HUD_INSTANT", "PosX", _instantHudX);
                _instantHudStartY = GetDesignFloat(design, "HUD_INSTANT", "PosY", _instantHudStartY);
                _instantHudBoxSpacing = GetDesignFloat(design, "HUD_INSTANT", "SpacingY", _instantHudBoxSpacing);
                _instantHudBoxWidth = GetDesignFloat(design, "HUD_INSTANT", "Width", _instantHudBoxWidth);
                _instantHudBoxHeight = GetDesignFloat(design, "HUD_INSTANT", "Height", _instantHudBoxHeight);
                _instantHudBorderSize = GetDesignFloat(design, "HUD_INSTANT", "BorderSize", _instantHudBorderSize);
                _instantHudBorderR = GetDesignInt(design, "HUD_INSTANT", "BorderR", _instantHudBorderR);
                _instantHudBorderG = GetDesignInt(design, "HUD_INSTANT", "BorderG", _instantHudBorderG);
                _instantHudBorderB = GetDesignInt(design, "HUD_INSTANT", "BorderB", _instantHudBorderB);
                _instantHudBorderA = GetDesignInt(design, "HUD_INSTANT", "BorderA", _instantHudBorderA);
                _instantGoodBgR = GetDesignInt(design, "HUD_INSTANT", "GoodBackgroundR", _instantGoodBgR);
                _instantGoodBgG = GetDesignInt(design, "HUD_INSTANT", "GoodBackgroundG", _instantGoodBgG);
                _instantGoodBgB = GetDesignInt(design, "HUD_INSTANT", "GoodBackgroundB", _instantGoodBgB);
                _instantBadBgR = GetDesignInt(design, "HUD_INSTANT", "BadBackgroundR", _instantBadBgR);
                _instantBadBgG = GetDesignInt(design, "HUD_INSTANT", "BadBackgroundG", _instantBadBgG);
                _instantBadBgB = GetDesignInt(design, "HUD_INSTANT", "BadBackgroundB", _instantBadBgB);
                _instantHudBgA = GetDesignInt(design, "HUD_INSTANT", "BackgroundA", _instantHudBgA);
                _instantHudTextOffsetY = GetDesignFloat(design, "HUD_INSTANT", "TextOffsetY", _instantHudTextOffsetY);
                _instantHudTextScale = GetDesignFloat(design, "HUD_INSTANT", "TextScale", _instantHudTextScale);
                _instantHudTextFont = GetDesignInt(design, "HUD_INSTANT", "TextFont", _instantHudTextFont);
                _instantHudTextR = GetDesignInt(design, "HUD_INSTANT", "TextR", _instantHudTextR);
                _instantHudTextG = GetDesignInt(design, "HUD_INSTANT", "TextG", _instantHudTextG);
                _instantHudTextB = GetDesignInt(design, "HUD_INSTANT", "TextB", _instantHudTextB);
                _instantHudTextA = GetDesignInt(design, "HUD_INSTANT", "TextA", _instantHudTextA);
                _instantHudDashLength = GetDesignFloat(design, "HUD_INSTANT", "DashLength", _instantHudDashLength);
                _instantHudDashGap = GetDesignFloat(design, "HUD_INSTANT", "DashGap", _instantHudDashGap);
                _instantHudDurationMs = GetDesignInt(design, "HUD_INSTANT", "DurationMs", _instantHudDurationMs);

                // INSTANT selalu satu slot. MaxVisible dari INI sengaja tidak boleh
                // menaikkan jumlah kartu, supaya gift baru langsung me-replace kartu lama.
                _instantPlayerFlipLabel = GetDesignString(design, "HUD_INSTANT", "PlayerFlipLabel", _instantPlayerFlipLabel);
                _instantGiveVehicleLabel = GetDesignString(design, "HUD_INSTANT", "GiveVehicleLabel", _instantGiveVehicleLabel);
                _instantDestroyCarLabel = GetDesignString(design, "HUD_INSTANT", "DestroyCarLabel", _instantDestroyCarLabel);
                _instantRandomTeleportLabel = GetDesignString(design, "HUD_INSTANT", "RandomTeleportLabel", _instantRandomTeleportLabel);
                _instantBackToStartLabel = GetDesignString(design, "HUD_INSTANT", "BackToStartLabel", _instantBackToStartLabel);
                _instantGoToFinishLabel = GetDesignString(design, "HUD_INSTANT", "GoToFinishLabel", _instantGoToFinishLabel);
                _instantTeleportSkyLabel = GetDesignString(design, "HUD_INSTANT", "TeleportSkyLabel", _instantTeleportSkyLabel);
                _instantHitByVehicleLabel = GetDesignString(design, "HUD_INSTANT", "HitByVehicleLabel", _instantHitByVehicleLabel);
                _instantUTurnLabel = GetDesignString(design, "HUD_INSTANT", "UTurnLabel", _instantUTurnLabel);
                _instantEnemyLabel = GetDesignString(design, "HUD_INSTANT", "EnemyLabel", _instantEnemyLabel);
                _instantBodyguardLabel = GetDesignString(design, "HUD_INSTANT", "BodyguardLabel", _instantBodyguardLabel);
                _instantAnimalLabel = GetDesignString(design, "HUD_INSTANT", "AnimalLabel", _instantAnimalLabel);
                _instantCountFormat = GetDesignString(design, "HUD_INSTANT", "CountFormat", _instantCountFormat);
                _instantGiveHealthFormat = GetDesignString(design, "HUD_INSTANT", "GiveHealthFormat", _instantGiveHealthFormat);

                // =====================================================
                // TIMER / ACTIVE EFFECT HUD
                // =====================================================
                _hudTimerEnabled = GetDesignBool(design, "HUD_TIMER", "Enabled", _hudTimerEnabled);
                _activeHudMaxVisible = Math.Max(1, Math.Min(3, GetDesignInt(design, "HUD_TIMER", "MaxVisible", _activeHudMaxVisible)));
                _statusHudX = GetDesignFloat(design, "HUD_TIMER", "PosX", _statusHudX);
                _statusHudStartY = GetDesignFloat(design, "HUD_TIMER", "PosY", _statusHudStartY);
                _statusHudBoxSpacing = GetDesignFloat(design, "HUD_TIMER", "SpacingY", _statusHudBoxSpacing);
                _statusHudBoxWidth = GetDesignFloat(design, "HUD_TIMER", "Width", _statusHudBoxWidth);
                _statusHudBoxHeight = GetDesignFloat(design, "HUD_TIMER", "Height", _statusHudBoxHeight);
                _statusHudBorderSize = GetDesignFloat(design, "HUD_TIMER", "BorderSize", _statusHudBorderSize);
                _statusHudBorderR = GetDesignInt(design, "HUD_TIMER", "BorderR", _statusHudBorderR);
                _statusHudBorderG = GetDesignInt(design, "HUD_TIMER", "BorderG", _statusHudBorderG);
                _statusHudBorderB = GetDesignInt(design, "HUD_TIMER", "BorderB", _statusHudBorderB);
                _statusHudBorderA = GetDesignInt(design, "HUD_TIMER", "BorderA", _statusHudBorderA);
                _timerGoodBgR = GetDesignInt(design, "HUD_TIMER", "GoodBackgroundR", _timerGoodBgR);
                _timerGoodBgG = GetDesignInt(design, "HUD_TIMER", "GoodBackgroundG", _timerGoodBgG);
                _timerGoodBgB = GetDesignInt(design, "HUD_TIMER", "GoodBackgroundB", _timerGoodBgB);
                _timerBadBgR = GetDesignInt(design, "HUD_TIMER", "BadBackgroundR", _timerBadBgR);
                _timerBadBgG = GetDesignInt(design, "HUD_TIMER", "BadBackgroundG", _timerBadBgG);
                _timerBadBgB = GetDesignInt(design, "HUD_TIMER", "BadBackgroundB", _timerBadBgB);
                _statusHudBgA = GetDesignInt(design, "HUD_TIMER", "BackgroundA", _statusHudBgA);
                _statusHudTextOffsetY = GetDesignFloat(design, "HUD_TIMER", "TextOffsetY", _statusHudTextOffsetY);
                _statusHudTextScale = GetDesignFloat(design, "HUD_TIMER", "TextScale", _statusHudTextScale);
                _statusHudTextFont = GetDesignInt(design, "HUD_TIMER", "TextFont", _statusHudTextFont);
                _statusHudTextR = GetDesignInt(design, "HUD_TIMER", "TextR", _statusHudTextR);
                _statusHudTextG = GetDesignInt(design, "HUD_TIMER", "TextG", _statusHudTextG);
                _statusHudTextB = GetDesignInt(design, "HUD_TIMER", "TextB", _statusHudTextB);
                _statusHudTextA = GetDesignInt(design, "HUD_TIMER", "TextA", _statusHudTextA);
                _statusHudDisplayTextFormat = GetDesignString(design, "HUD_TIMER", "DisplayTextFormat", _statusHudDisplayTextFormat);
                _giftTimerTimeScale = GetDesignFloat(design, "HUD_TIMER", "TimeScale", _giftTimerTimeScale);
                _giftTimerTimeOffsetY = GetDesignFloat(design, "HUD_TIMER", "TimeOffsetY", _giftTimerTimeOffsetY);
                _giftTimerColumnRatio = GetDesignFloat(design, "HUD_TIMER", "TimeColumnRatio", _giftTimerColumnRatio);
                _giftTimerBarHeight = GetDesignFloat(design, "HUD_TIMER", "BarHeight", _giftTimerBarHeight);
                _giftTimerBarOffsetY = GetDesignFloat(design, "HUD_TIMER", "BarOffsetY", _giftTimerBarOffsetY);
                _instantAccentGoodR = GetDesignInt(design, "HUD_INSTANT", "GoodAccentR", _instantAccentGoodR);
                _instantAccentGoodG = GetDesignInt(design, "HUD_INSTANT", "GoodAccentG", _instantAccentGoodG);
                _instantAccentGoodB = GetDesignInt(design, "HUD_INSTANT", "GoodAccentB", _instantAccentGoodB);
                _instantAccentBadR = GetDesignInt(design, "HUD_INSTANT", "BadAccentR", _instantAccentBadR);
                _instantAccentBadG = GetDesignInt(design, "HUD_INSTANT", "BadAccentG", _instantAccentBadG);
                _instantAccentBadB = GetDesignInt(design, "HUD_INSTANT", "BadAccentB", _instantAccentBadB);
                _timerAccentGoodR = GetDesignInt(design, "HUD_TIMER", "GoodAccentR", _timerAccentGoodR);
                _timerAccentGoodG = GetDesignInt(design, "HUD_TIMER", "GoodAccentG", _timerAccentGoodG);
                _timerAccentGoodB = GetDesignInt(design, "HUD_TIMER", "GoodAccentB", _timerAccentGoodB);
                _timerAccentBadR = GetDesignInt(design, "HUD_TIMER", "BadAccentR", _timerAccentBadR);
                _timerAccentBadG = GetDesignInt(design, "HUD_TIMER", "BadAccentG", _timerAccentBadG);
                _timerAccentBadB = GetDesignInt(design, "HUD_TIMER", "BadAccentB", _timerAccentBadB);

                _statusSuperSpeedLabel = GetDesignString(design, "HUD_TIMER", "SuperSpeedLabel", _statusSuperSpeedLabel);
                _statusFallingVehiclesLabel = GetDesignString(design, "HUD_TIMER", "FallingVehiclesLabel", _statusFallingVehiclesLabel);
                _statusSleepLabel = GetDesignString(design, "HUD_TIMER", "SleepLabel", _statusSleepLabel);
                _statusBallRainLabel = GetDesignString(design, "HUD_TIMER", "BallRainLabel", _statusBallRainLabel);
                _statusInvincibleLabel = GetDesignString(design, "HUD_TIMER", "InvincibleLabel", _statusInvincibleLabel);
                _statusWantedLabel = GetDesignString(design, "HUD_TIMER", "WantedLabel", _statusWantedLabel);
                _statusDisarmLabel = GetDesignString(design, "HUD_TIMER", "DisarmLabel", _statusDisarmLabel);
                _statusBlackholeLabel = GetDesignString(design, "HUD_TIMER", "BlackholeLabel", _statusBlackholeLabel);
                _statusEarthquakeLabel = GetDesignString(design, "HUD_TIMER", "EarthquakeLabel", _statusEarthquakeLabel);
                _statusBlackholeWaitingLabel = GetDesignString(design, "HUD_TIMER", "BlackholeWaitingLabel", _statusBlackholeWaitingLabel);
                _statusEarthquakeWaitingLabel = GetDesignString(design, "HUD_TIMER", "EarthquakeWaitingLabel", _statusEarthquakeWaitingLabel);
                _statusCageLabel = GetDesignString(design, "HUD_TIMER", "CageLabel", _statusCageLabel);
                _statusTransformingLabel = GetDesignString(design, "HUD_TIMER", "TransformingLabel", _statusTransformingLabel);
                _statusAnimalLabel = GetDesignString(design, "HUD_TIMER", "AnimalLabel", _statusAnimalLabel);
                _statusReturningLabel = GetDesignString(design, "HUD_TIMER", "ReturningLabel", _statusReturningLabel);
                _statusGetTowedLabel = GetDesignString(design, "HUD_TIMER", "GetTowedLabel", _statusGetTowedLabel);
                _statusTrafficMagnetLabel = GetDesignString(design, "HUD_TIMER", "TrafficMagnetLabel", _statusTrafficMagnetLabel);
                _statusRecklessTrafficLabel = GetDesignString(design, "HUD_TIMER", "RecklessTrafficLabel", _statusRecklessTrafficLabel);
                _statusVehicleFireWarningLabel = GetDesignString(design, "HUD_TIMER", "VehicleFireWarningLabel", _statusVehicleFireWarningLabel);
                _statusVehicleBurningLabel = GetDesignString(design, "HUD_TIMER", "VehicleBurningLabel", _statusVehicleBurningLabel);
                _statusBurningWarningLabel = GetDesignString(design, "HUD_TIMER", "BurningWarningLabel", _statusBurningWarningLabel);
                _statusBurningLabel = GetDesignString(design, "HUD_TIMER", "BurningLabel", _statusBurningLabel);
                _statusPoliceRoadblockLabel = GetDesignString(design, "HUD_TIMER", "PoliceRoadblockLabel", _statusPoliceRoadblockLabel);
                _statusRandomExplosionLabel = GetDesignString(design, "HUD_TIMER", "RandomExplosionLabel", _statusRandomExplosionLabel);

                // =====================================================
                // RANDOM CHAOS ROULETTE
                // =====================================================
                _hudChaosRouletteEnabled = GetDesignBool(design, "HUD_CHAOS_ROULETTE", "Enabled", _hudChaosRouletteEnabled);
                _chaosUiX = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "PosX", _chaosUiX);
                _chaosUiY = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "PosY", _chaosUiY);
                _chaosUiWidth = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "Width", _chaosUiWidth);
                _chaosUiHeight = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "Height", _chaosUiHeight);
                _chaosUiBorderSize = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "BorderSize", _chaosUiBorderSize);
                _chaosUiBorderA = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "BorderA", _chaosUiBorderA);
                _chaosUiSelectingR = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "SelectingBorderR", _chaosUiSelectingR);
                _chaosUiSelectingG = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "SelectingBorderG", _chaosUiSelectingG);
                _chaosUiSelectingB = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "SelectingBorderB", _chaosUiSelectingB);
                _chaosUiLockedR = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "LockedBorderR", _chaosUiLockedR);
                _chaosUiLockedG = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "LockedBorderG", _chaosUiLockedG);
                _chaosUiLockedB = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "LockedBorderB", _chaosUiLockedB);
                _chaosUiBgR = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "BackgroundR", _chaosUiBgR);
                _chaosUiBgG = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "BackgroundG", _chaosUiBgG);
                _chaosUiBgB = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "BackgroundB", _chaosUiBgB);
                _chaosUiBgA = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "BackgroundA", _chaosUiBgA);
                _chaosUiSelectingText = GetDesignString(design, "HUD_CHAOS_ROULETTE", "SelectingText", _chaosUiSelectingText);
                _chaosUiLockedText = GetDesignString(design, "HUD_CHAOS_ROULETTE", "LockedText", _chaosUiLockedText);
                _chaosUiStatusOffsetY = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "StatusTextOffsetY", _chaosUiStatusOffsetY);
                _chaosUiStatusScale = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "StatusTextScale", _chaosUiStatusScale);
                _chaosUiStatusFont = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "StatusTextFont", _chaosUiStatusFont);
                _chaosUiStatusR = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "StatusTextR", _chaosUiStatusR);
                _chaosUiStatusG = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "StatusTextG", _chaosUiStatusG);
                _chaosUiStatusB = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "StatusTextB", _chaosUiStatusB);
                _chaosUiStatusA = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "StatusTextA", _chaosUiStatusA);
                _chaosUiResultOffsetY = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "ResultTextOffsetY", _chaosUiResultOffsetY);
                _chaosUiResultScale = GetDesignFloat(design, "HUD_CHAOS_ROULETTE", "ResultTextScale", _chaosUiResultScale);
                _chaosUiResultFont = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "ResultTextFont", _chaosUiResultFont);
                _chaosUiResultR = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "ResultTextR", _chaosUiResultR);
                _chaosUiResultG = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "ResultTextG", _chaosUiResultG);
                _chaosUiResultB = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "ResultTextB", _chaosUiResultB);
                _chaosUiResultA = GetDesignInt(design, "HUD_CHAOS_ROULETTE", "ResultTextA", _chaosUiResultA);

                // =====================================================
                // RANDOM TELEPORT GACHA
                // =====================================================
                _hudTeleportGachaEnabled = GetDesignBool(design, "HUD_TELEPORT_GACHA", "Enabled", _hudTeleportGachaEnabled);
                _teleportUiX = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "PosX", _teleportUiX);
                _teleportUiY = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "PosY", _teleportUiY);
                _teleportUiWidth = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "Width", _teleportUiWidth);
                _teleportUiHeight = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "Height", _teleportUiHeight);
                _teleportUiBorderSize = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "BorderSize", _teleportUiBorderSize);
                _teleportUiBorderA = GetDesignInt(design, "HUD_TELEPORT_GACHA", "BorderA", _teleportUiBorderA);
                _teleportUiSelectingR = GetDesignInt(design, "HUD_TELEPORT_GACHA", "SelectingBorderR", _teleportUiSelectingR);
                _teleportUiSelectingG = GetDesignInt(design, "HUD_TELEPORT_GACHA", "SelectingBorderG", _teleportUiSelectingG);
                _teleportUiSelectingB = GetDesignInt(design, "HUD_TELEPORT_GACHA", "SelectingBorderB", _teleportUiSelectingB);
                _teleportUiLockedR = GetDesignInt(design, "HUD_TELEPORT_GACHA", "LockedBorderR", _teleportUiLockedR);
                _teleportUiLockedG = GetDesignInt(design, "HUD_TELEPORT_GACHA", "LockedBorderG", _teleportUiLockedG);
                _teleportUiLockedB = GetDesignInt(design, "HUD_TELEPORT_GACHA", "LockedBorderB", _teleportUiLockedB);
                _teleportUiBgR = GetDesignInt(design, "HUD_TELEPORT_GACHA", "BackgroundR", _teleportUiBgR);
                _teleportUiBgG = GetDesignInt(design, "HUD_TELEPORT_GACHA", "BackgroundG", _teleportUiBgG);
                _teleportUiBgB = GetDesignInt(design, "HUD_TELEPORT_GACHA", "BackgroundB", _teleportUiBgB);
                _teleportUiBgA = GetDesignInt(design, "HUD_TELEPORT_GACHA", "BackgroundA", _teleportUiBgA);
                _teleportUiSelectingText = GetDesignString(design, "HUD_TELEPORT_GACHA", "SelectingText", _teleportUiSelectingText);
                _teleportUiLockedText = GetDesignString(design, "HUD_TELEPORT_GACHA", "LockedText", _teleportUiLockedText);
                _teleportUiStatusOffsetY = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "StatusTextOffsetY", _teleportUiStatusOffsetY);
                _teleportUiStatusScale = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "StatusTextScale", _teleportUiStatusScale);
                _teleportUiStatusFont = GetDesignInt(design, "HUD_TELEPORT_GACHA", "StatusTextFont", _teleportUiStatusFont);
                _teleportUiStatusR = GetDesignInt(design, "HUD_TELEPORT_GACHA", "StatusTextR", _teleportUiStatusR);
                _teleportUiStatusG = GetDesignInt(design, "HUD_TELEPORT_GACHA", "StatusTextG", _teleportUiStatusG);
                _teleportUiStatusB = GetDesignInt(design, "HUD_TELEPORT_GACHA", "StatusTextB", _teleportUiStatusB);
                _teleportUiStatusA = GetDesignInt(design, "HUD_TELEPORT_GACHA", "StatusTextA", _teleportUiStatusA);
                _teleportUiResultOffsetY = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "ResultTextOffsetY", _teleportUiResultOffsetY);
                _teleportUiResultScale = GetDesignFloat(design, "HUD_TELEPORT_GACHA", "ResultTextScale", _teleportUiResultScale);
                _teleportUiResultFont = GetDesignInt(design, "HUD_TELEPORT_GACHA", "ResultTextFont", _teleportUiResultFont);
                _teleportUiResultR = GetDesignInt(design, "HUD_TELEPORT_GACHA", "ResultTextR", _teleportUiResultR);
                _teleportUiResultG = GetDesignInt(design, "HUD_TELEPORT_GACHA", "ResultTextG", _teleportUiResultG);
                _teleportUiResultB = GetDesignInt(design, "HUD_TELEPORT_GACHA", "ResultTextB", _teleportUiResultB);
                _teleportUiResultA = GetDesignInt(design, "HUD_TELEPORT_GACHA", "ResultTextA", _teleportUiResultA);

                // =====================================================
                // RANDOM GATCHA
                // =====================================================
                _hudRandomGatchaEnabled = GetDesignBool(design, "HUD_RANDOM_GATCHA", "Enabled", _hudRandomGatchaEnabled);
                _randomGatchaUiX = GetDesignFloat(design, "HUD_RANDOM_GATCHA", "PosX", _randomGatchaUiX);
                _randomGatchaUiY = GetDesignFloat(design, "HUD_RANDOM_GATCHA", "PosY", _randomGatchaUiY);
                _randomGatchaUiWidth = GetDesignFloat(design, "HUD_RANDOM_GATCHA", "Width", _randomGatchaUiWidth);
                _randomGatchaUiHeight = GetDesignFloat(design, "HUD_RANDOM_GATCHA", "Height", _randomGatchaUiHeight);
                _randomGatchaAccentRollingR = GetDesignInt(design, "HUD_RANDOM_GATCHA", "RollingAccentR", _randomGatchaAccentRollingR);
                _randomGatchaAccentRollingG = GetDesignInt(design, "HUD_RANDOM_GATCHA", "RollingAccentG", _randomGatchaAccentRollingG);
                _randomGatchaAccentRollingB = GetDesignInt(design, "HUD_RANDOM_GATCHA", "RollingAccentB", _randomGatchaAccentRollingB);
                _randomGatchaAccentLockedR = GetDesignInt(design, "HUD_RANDOM_GATCHA", "LockedAccentR", _randomGatchaAccentLockedR);
                _randomGatchaAccentLockedG = GetDesignInt(design, "HUD_RANDOM_GATCHA", "LockedAccentG", _randomGatchaAccentLockedG);
                _randomGatchaAccentLockedB = GetDesignInt(design, "HUD_RANDOM_GATCHA", "LockedAccentB", _randomGatchaAccentLockedB);

                // =====================================================
                // PED UI - ENEMY / BODYGUARD / ANIMAL
                // MODERN TEXT-ONLY CARD (TANPA ICON)
                // =====================================================
                _hudPedEnabled = GetDesignBool(design, "HUD_PED", "Enabled", _hudPedEnabled);
                _uiRenderDistance = GetDesignFloat(design, "HUD_PED", "RenderDistance", _uiRenderDistance);
                _pedHudHeadOffsetZ = GetDesignFloat(design, "HUD_PED", "HeadOffsetZ", _pedHudHeadOffsetZ);
                _pedHudCameraDistance = GetDesignFloat(design, "HUD_PED", "MaxDistance", _pedHudCameraDistance);

                _pedHudPanelMinWidth = GetDesignFloat(design, "HUD_PED", "PanelMinWidth", _pedHudPanelMinWidth);
                _pedHudPanelMaxWidth = GetDesignFloat(design, "HUD_PED", "PanelMaxWidth", _pedHudPanelMaxWidth);
                _pedHudPanelHeight = GetDesignFloat(design, "HUD_PED", "PanelHeight", _pedHudPanelHeight);
                _pedHudPanelPaddingX = GetDesignFloat(design, "HUD_PED", "PanelPaddingX", _pedHudPanelPaddingX);

                _pedHudPanelR = GetDesignInt(design, "HUD_PED", "PanelR", _pedHudPanelR);
                _pedHudPanelG = GetDesignInt(design, "HUD_PED", "PanelG", _pedHudPanelG);
                _pedHudPanelB = GetDesignInt(design, "HUD_PED", "PanelB", _pedHudPanelB);
                _pedHudPanelA = GetDesignInt(design, "HUD_PED", "PanelA", _pedHudPanelA);

                _pedHudPanelBorderSize = GetDesignFloat(design, "HUD_PED", "PanelBorderSize", _pedHudPanelBorderSize);
                _pedHudPanelBorderR = GetDesignInt(design, "HUD_PED", "PanelBorderR", _pedHudPanelBorderR);
                _pedHudPanelBorderG = GetDesignInt(design, "HUD_PED", "PanelBorderG", _pedHudPanelBorderG);
                _pedHudPanelBorderB = GetDesignInt(design, "HUD_PED", "PanelBorderB", _pedHudPanelBorderB);
                _pedHudPanelBorderA = GetDesignInt(design, "HUD_PED", "PanelBorderA", _pedHudPanelBorderA);

                _pedHudShadowOffsetX = GetDesignFloat(design, "HUD_PED", "ShadowOffsetX", _pedHudShadowOffsetX);
                _pedHudShadowOffsetY = GetDesignFloat(design, "HUD_PED", "ShadowOffsetY", _pedHudShadowOffsetY);
                _pedHudShadowExtraWidth = GetDesignFloat(design, "HUD_PED", "ShadowExtraWidth", _pedHudShadowExtraWidth);
                _pedHudShadowExtraHeight = GetDesignFloat(design, "HUD_PED", "ShadowExtraHeight", _pedHudShadowExtraHeight);
                _pedHudShadowR = GetDesignInt(design, "HUD_PED", "ShadowR", _pedHudShadowR);
                _pedHudShadowG = GetDesignInt(design, "HUD_PED", "ShadowG", _pedHudShadowG);
                _pedHudShadowB = GetDesignInt(design, "HUD_PED", "ShadowB", _pedHudShadowB);
                _pedHudShadowA = GetDesignInt(design, "HUD_PED", "ShadowA", _pedHudShadowA);

                _pedHudNameUppercase = GetDesignBool(design, "HUD_PED", "NameUppercase", _pedHudNameUppercase);
                _pedHudNameOffsetY = GetDesignFloat(design, "HUD_PED", "NameOffsetY", _pedHudNameOffsetY);
                _pedHudNameScale = GetDesignFloat(design, "HUD_PED", "NameTextScale", _pedHudNameScale);
                _pedHudNameFont = GetDesignInt(design, "HUD_PED", "NameTextFont", _pedHudNameFont);
                _pedHudNameR = GetDesignInt(design, "HUD_PED", "NameTextR", _pedHudNameR);
                _pedHudNameG = GetDesignInt(design, "HUD_PED", "NameTextG", _pedHudNameG);
                _pedHudNameB = GetDesignInt(design, "HUD_PED", "NameTextB", _pedHudNameB);
                _pedHudNameA = GetDesignInt(design, "HUD_PED", "NameTextA", _pedHudNameA);

                _pedHudHealthOffsetY = GetDesignFloat(design, "HUD_PED", "HealthOffsetY", _pedHudHealthOffsetY);
                _pedHudHealthHorizontalPadding = GetDesignFloat(design, "HUD_PED", "HealthHorizontalPadding", _pedHudHealthHorizontalPadding);
                _pedHudHealthHeight = GetDesignFloat(design, "HUD_PED", "HealthHeight", _pedHudHealthHeight);
                _pedHudHealthBorderSize = GetDesignFloat(design, "HUD_PED", "HealthBorderSize", _pedHudHealthBorderSize);
                _pedHudHealthBorderR = GetDesignInt(design, "HUD_PED", "HealthBorderR", _pedHudHealthBorderR);
                _pedHudHealthBorderG = GetDesignInt(design, "HUD_PED", "HealthBorderG", _pedHudHealthBorderG);
                _pedHudHealthBorderB = GetDesignInt(design, "HUD_PED", "HealthBorderB", _pedHudHealthBorderB);
                _pedHudHealthBorderA = GetDesignInt(design, "HUD_PED", "HealthBorderA", _pedHudHealthBorderA);
                _pedHudHealthTrackR = GetDesignInt(design, "HUD_PED", "HealthTrackR", _pedHudHealthTrackR);
                _pedHudHealthTrackG = GetDesignInt(design, "HUD_PED", "HealthTrackG", _pedHudHealthTrackG);
                _pedHudHealthTrackB = GetDesignInt(design, "HUD_PED", "HealthTrackB", _pedHudHealthTrackB);
                _pedHudHealthTrackA = GetDesignInt(design, "HUD_PED", "HealthTrackA", _pedHudHealthTrackA);

                _pedHudEnemyR = GetDesignInt(design, "HUD_PED", "EnemyHealthR", _pedHudEnemyR);
                _pedHudEnemyG = GetDesignInt(design, "HUD_PED", "EnemyHealthG", _pedHudEnemyG);
                _pedHudEnemyB = GetDesignInt(design, "HUD_PED", "EnemyHealthB", _pedHudEnemyB);
                _pedHudBodyguardR = GetDesignInt(design, "HUD_PED", "BodyguardHealthR", _pedHudBodyguardR);
                _pedHudBodyguardG = GetDesignInt(design, "HUD_PED", "BodyguardHealthG", _pedHudBodyguardG);
                _pedHudBodyguardB = GetDesignInt(design, "HUD_PED", "BodyguardHealthB", _pedHudBodyguardB);
                _pedHudAnimalR = GetDesignInt(design, "HUD_PED", "AnimalHealthR", _pedHudAnimalR);
                _pedHudAnimalG = GetDesignInt(design, "HUD_PED", "AnimalHealthG", _pedHudAnimalG);
                _pedHudAnimalB = GetDesignInt(design, "HUD_PED", "AnimalHealthB", _pedHudAnimalB);
                _pedHudHealthFillA = GetDesignInt(design, "HUD_PED", "HealthFillA", _pedHudHealthFillA);

                // =====================================================
                // CUSTOM DEATH / RESPAWN SCREEN
                // =====================================================
                _hudCustomDeathEnabled = GetDesignBool(design, "HUD_CUSTOM_DEATH", "Enabled", _hudCustomDeathEnabled);
                _deathOverlayX = GetDesignFloat(design, "HUD_CUSTOM_DEATH", "OverlayPosX", _deathOverlayX);
                _deathOverlayY = GetDesignFloat(design, "HUD_CUSTOM_DEATH", "OverlayPosY", _deathOverlayY);
                _deathOverlayWidth = GetDesignFloat(design, "HUD_CUSTOM_DEATH", "OverlayWidth", _deathOverlayWidth);
                _deathOverlayHeight = GetDesignFloat(design, "HUD_CUSTOM_DEATH", "OverlayHeight", _deathOverlayHeight);
                _deathOverlayR = GetDesignInt(design, "HUD_CUSTOM_DEATH", "OverlayBackgroundR", _deathOverlayR);
                _deathOverlayG = GetDesignInt(design, "HUD_CUSTOM_DEATH", "OverlayBackgroundG", _deathOverlayG);
                _deathOverlayB = GetDesignInt(design, "HUD_CUSTOM_DEATH", "OverlayBackgroundB", _deathOverlayB);
                _deathOverlayMaxA = GetDesignInt(design, "HUD_CUSTOM_DEATH", "OverlayBackgroundA", _deathOverlayMaxA);
                _deathRespawnTextX = GetDesignFloat(design, "HUD_CUSTOM_DEATH", "TextPosX", _deathRespawnTextX);
                _deathRespawnTextY = GetDesignFloat(design, "HUD_CUSTOM_DEATH", "TextPosY", _deathRespawnTextY);
                _deathRespawnTextScale = GetDesignFloat(design, "HUD_CUSTOM_DEATH", "TextScale", _deathRespawnTextScale);
                _deathRespawnTextFont = GetDesignInt(design, "HUD_CUSTOM_DEATH", "TextFont", _deathRespawnTextFont);
                _deathRespawnTextR = GetDesignInt(design, "HUD_CUSTOM_DEATH", "TextR", _deathRespawnTextR);
                _deathRespawnTextG = GetDesignInt(design, "HUD_CUSTOM_DEATH", "TextG", _deathRespawnTextG);
                _deathRespawnTextB = GetDesignInt(design, "HUD_CUSTOM_DEATH", "TextB", _deathRespawnTextB);
                _deathRespawnTextA = GetDesignInt(design, "HUD_CUSTOM_DEATH", "TextA", _deathRespawnTextA);
                _deathRespawnTextFormat = GetDesignString(design, "HUD_CUSTOM_DEATH", "TextFormat", _deathRespawnTextFormat);

                // Apply route blip visual changes immediately after a live reload.
                if (!string.Equals(
                        previousRouteBlipVisualKey,
                        GetRouteBlipVisualKey(),
                        StringComparison.Ordinal
                    ))
                {
                    UpdateCheckpointBlip();
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "DESIGN CONFIG ERROR",
                    ex
                );

                ShowHudNotification(
                    "~r~[DESIGN CONFIG ERROR]~w~ " +
                    ex.Message,
                    false
                );
            }
        }

        // DrawCountdownUI dipindahkan ke file layout HUD.


        private void DrawChaosHudTrack(float left, float y, float width, float fraction, int r, int g, int b)
        {
            Function.Call(Hash.DRAW_RECT, left + width / 2f, y, width, 0.005f, 29, 39, 48, 255);
            float fill = width * Math.Max(0f, Math.Min(1f, fraction));
            if (fill > 0f) Function.Call(Hash.DRAW_RECT, left + fill / 2f, y, fill, 0.005f, r, g, b, 255);
            DrawGiftOutline(left + width / 2f, y, width, 0.005f, 0.0006f, 92, 113, 134, 185);
        }

        // DrawRandomCheckpointMarkers dipindahkan ke file layout HUD.


        // DrawRandomCheckpointUI dipindahkan ke file layout HUD.


        private void DrawRoundedRectApprox(
            float centerX,
            float centerY,
            float width,
            float height,
            float cornerRadiusY,
            int r,
            int g,
            int b,
            int a)
        {
            width = Math.Max(0.001f, width);
            height = Math.Max(0.001f, height);

            float radiusY =
                Math.Max(
                    0.0f,
                    Math.Min(
                        cornerRadiusY,
                        height / 2.0f
                    )
                );

            if (radiusY <= 0.0001f)
            {
                Function.Call(
                    Hash.DRAW_RECT,
                    centerX,
                    centerY,
                    width,
                    height,
                    r,
                    g,
                    b,
                    a
                );
                return;
            }

            // Karena koordinat X/Y GTA dinormalisasi terhadap ukuran layar,
            // radius X diperkecil agar sudut terlihat lebih bulat di 16:9.
            float radiusX =
                radiusY * 0.5625f;

            const int slices = 18;
            float sliceHeight =
                height /
                slices;

            float top =
                centerY -
                (height / 2.0f);

            for (
                int i = 0;
                i < slices;
                i++)
            {
                float y =
                    top +
                    ((i + 0.5f) * sliceHeight);

                float widthAtY =
                    width;

                float localFromTop =
                    y - top;

                float localFromBottom =
                    (top + height) - y;

                if (localFromTop < radiusY)
                {
                    float dy =
                        radiusY -
                        localFromTop;

                    float ratio =
                        Math.Max(
                            0.0f,
                            1.0f -
                            ((dy * dy) /
                             (radiusY * radiusY))
                        );

                    float extension =
                        radiusX *
                        (float)Math.Sqrt(ratio);

                    widthAtY =
                        width -
                        (2.0f *
                         (radiusX - extension));
                }
                else if (localFromBottom < radiusY)
                {
                    float dy =
                        radiusY -
                        localFromBottom;

                    float ratio =
                        Math.Max(
                            0.0f,
                            1.0f -
                            ((dy * dy) /
                             (radiusY * radiusY))
                        );

                    float extension =
                        radiusX *
                        (float)Math.Sqrt(ratio);

                    widthAtY =
                        width -
                        (2.0f *
                         (radiusX - extension));
                }

                Function.Call(
                    Hash.DRAW_RECT,
                    centerX,
                    y,
                    Math.Max(0.001f, widthAtY),
                    sliceHeight + 0.0004f,
                    r,
                    g,
                    b,
                    a
                );
            }
        }

        private void DrawCleanProgressText(
            string text,
            float x,
            float y,
            float scale,
            int r,
            int g,
            int b,
            int a,
            int font)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Function.Call(Hash.SET_TEXT_FONT, font);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, a);
            Function.Call(Hash.SET_TEXT_WRAP, 0.0f, 1.0f);
            Function.Call(Hash.SET_TEXT_CENTRE, true);

            // Panel putih modern: sengaja tanpa outline / shadow text GTA.
            Function.Call(
                Hash.SET_TEXT_DROPSHADOW,
                0,
                0,
                0,
                0,
                0
            );

            Function.Call(
                Hash.SET_TEXT_EDGE,
                0,
                0,
                0,
                0,
                0
            );

            Function.Call(
                Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT,
                "STRING"
            );

            Function.Call(
                Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                text
            );

            Function.Call(
                Hash.END_TEXT_COMMAND_DISPLAY_TEXT,
                x,
                y
            );
        }

        private void DrawModernHudPanel(
            float left,
            float top,
            float width,
            float height,
            int panelR,
            int panelG,
            int panelB,
            int panelA)
        {
            float centerX =
                left +
                (width / 2.0f);

            float centerY =
                top +
                (height / 2.0f);

            // Versi clean tanpa rounded-slice agar tidak muncul garis-garis
            // horizontal di dalam panel saat dirender di GTA.
            Function.Call(
                Hash.DRAW_RECT,
                centerX,
                centerY + 0.0020f,
                width + 0.0040f,
                height + 0.0060f,
                0,
                0,
                0,
                95
            );

            Function.Call(
                Hash.DRAW_RECT,
                centerX,
                centerY,
                width + 0.0022f,
                height + 0.0032f,
                115,
                128,
                145,
                185
            );

            Function.Call(
                Hash.DRAW_RECT,
                centerX,
                centerY,
                width,
                height,
                panelR,
                panelG,
                panelB,
                panelA
            );
        }

        private void DrawModernCounterRow(
            float left,
            float top,
            float width,
            float height,
            float textOffsetY,
            float valueTextOffsetY,
            string labelFormat,
            string fallbackLabel,
            string subtitle,
            int value,
            float textScale,
            int font,
            int accentR,
            int accentG,
            int accentB,
            int accentA,
            float subtitleScale,
            int subtitleR,
            int subtitleG,
            int subtitleB,
            int subtitleA,
            int panelR,
            int panelG,
            int panelB,
            int panelA)
        {
            // Match the progress panel: square steel frame, dark fill, side accents.
            float cx = left + width / 2.0f;
            float cy = top + height / 2.0f;
            Function.Call(Hash.DRAW_RECT, cx, cy, width + 0.0024f, height + 0.0034f,
                92, 108, 126, 205);
            Function.Call(Hash.DRAW_RECT, cx, cy, width, height,
                panelR, panelG, panelB, panelA);
            Function.Call(Hash.DRAW_RECT, left + 0.0025f, cy, 0.0015f,
                Math.Max(0.001f, height - 0.008f), 132, 170, 199, 230);
            Function.Call(Hash.DRAW_RECT, left + width - 0.0025f, cy, 0.0015f,
                Math.Max(0.001f, height - 0.008f), 132, 170, 199, 230);
            float dividerX = left + width * 0.72f;
            Function.Call(Hash.DRAW_RECT, dividerX, cy, 0.0008f,
                Math.Max(0.001f, height - 0.010f), 92, 113, 134, 185);

            string label = GetCounterLabelPrefix(labelFormat, fallbackLabel);
            float mainY = top + textOffsetY;
            DrawReferenceProgressText(label, left + 0.011f, mainY, textScale,
                width * 0.72f - 0.019f, 248, 250, 253, 255, font, false);
            DrawReferenceProgressText(value.ToString(CultureInfo.InvariantCulture),
                left + width * 0.855f, top + valueTextOffsetY, textScale,
                width * 0.28f - 0.012f, accentR, accentG, accentB, accentA, font, true);
        }

        // DrawScoreCounter dipindahkan ke file layout HUD.


        // DrawDeathCounter dipindahkan ke file layout HUD.


        private void DrawReferenceProgressText(string text, float x, float y, float scale,
            float maxWidth, int r, int g, int b, int a, int font, bool centered)
        {
            if (string.IsNullOrEmpty(text)) return;
            scale = Math.Max(0.01f, scale);
            float width = MeasureReferenceProgressText(text, scale, font);
            if (width > maxWidth && width > 0.0f)
            {
                scale *= maxWidth / width;
                width = MeasureReferenceProgressText(text, scale, font);
            }
            DrawCleanProgressText(text, centered ? x : x + width / 2.0f,
                y, scale, r, g, b, a, font);
        }

        // DrawRouteLoadingProgressPanel dipindahkan ke file layout HUD.


        // DrawProgressBar dipindahkan ke file layout HUD.


        // DrawRandomGatchaUI dipindahkan ke file layout HUD.


        // DrawTeleportGachaCardsUI dipindahkan ke file layout HUD.


        private void DrawPedHud(
            Ped ped,
            string name,
            PedHudType hudType,
            bool showName,
            bool showHealthBar)
        {
            if (!_hudPedEnabled ||
                IsNormalHudSuppressed() ||
                (!showName && !showHealthBar))
                return;

            if (ped == null ||
                !ped.Exists() ||
                !ped.IsAlive)
                return;

            Vector3 headPos =
                Function.Call<Vector3>(
                    Hash.GET_PED_BONE_COORDS,
                    ped.Handle,
                    31086,
                    0.0f,
                    0.0f,
                    0.0f
                ) +
                new Vector3(
                    0.0f,
                    0.0f,
                    _pedHudHeadOffsetZ
                );

            if (GameplayCamera.Position.DistanceTo(headPos) >
                _pedHudCameraDistance)
                return;

            OutputArgument sX = new OutputArgument();
            OutputArgument sY = new OutputArgument();

            if (!Function.Call<bool>(
                    Hash.GET_SCREEN_COORD_FROM_WORLD_COORD,
                    headPos.X,
                    headPos.Y,
                    headPos.Z,
                    sX,
                    sY))
                return;

            float screenX = sX.GetResult<float>();
            float screenY = sY.GetResult<float>();

            string displayName =
                string.IsNullOrWhiteSpace(name)
                    ? "UNKNOWN"
                    : name.Trim();

            if (_pedHudNameUppercase)
            {
                displayName =
                    displayName.ToUpperInvariant();
            }

            float nameScale =
                Math.Max(
                    0.10f,
                    _pedHudNameScale
                );

            float minWidth =
                Math.Max(
                    0.040f,
                    _pedHudPanelMinWidth
                );

            float maxWidth =
                Math.Max(
                    minWidth,
                    _pedHudPanelMaxWidth
                );

            // Lebar card mengikuti nama, tetapi tetap memiliki batas aman.
            float measuredNameWidth =
                showName
                    ? MeasureReferenceProgressText(
                        displayName,
                        nameScale,
                        _pedHudNameFont
                    )
                    : 0.0f;

            float desiredPanelWidth =
                measuredNameWidth +
                (_pedHudPanelPaddingX * 2.0f);

            float panelWidth =
                Math.Max(
                    minWidth,
                    Math.Min(
                        maxWidth,
                        desiredPanelWidth
                    )
                );

            // Kalau nama terlalu panjang untuk PanelMaxWidth,
            // text diperkecil secukupnya agar tidak terpotong.
            float nameContentWidth =
                Math.Max(
                    0.010f,
                    panelWidth -
                    (_pedHudPanelPaddingX * 2.0f)
                );

            if (showName &&
                measuredNameWidth > nameContentWidth &&
                measuredNameWidth > 0.0f)
            {
                nameScale *=
                    nameContentWidth /
                    measuredNameWidth;

                nameScale =
                    Math.Max(
                        0.20f,
                        nameScale
                    );
            }

            float panelHeight =
                Math.Max(
                    0.020f,
                    _pedHudPanelHeight
                );

            // Jika salah satu komponen dimatikan lewat config NPC,
            // card mengecil otomatis agar tidak menyisakan ruang kosong berlebihan.
            if (showName && !showHealthBar)
            {
                panelHeight *= 0.58f;
            }
            else if (!showName && showHealthBar)
            {
                panelHeight *= 0.48f;
            }

            // =====================================================
            // DROP SHADOW
            // =====================================================
            Function.Call(
                Hash.DRAW_RECT,
                screenX + _pedHudShadowOffsetX,
                screenY + _pedHudShadowOffsetY,
                panelWidth + _pedHudShadowExtraWidth,
                panelHeight + _pedHudShadowExtraHeight,
                _pedHudShadowR,
                _pedHudShadowG,
                _pedHudShadowB,
                _pedHudShadowA
            );

            // =====================================================
            // PANEL BORDER + BACKGROUND
            // =====================================================
            float panelBorderSize =
                Math.Max(
                    0.0f,
                    _pedHudPanelBorderSize
                );

            if (panelBorderSize > 0.0f)
            {
                Function.Call(
                    Hash.DRAW_RECT,
                    screenX,
                    screenY,
                    panelWidth + (panelBorderSize * 2.0f),
                    panelHeight + (panelBorderSize * 2.0f),
                    _pedHudPanelBorderR,
                    _pedHudPanelBorderG,
                    _pedHudPanelBorderB,
                    _pedHudPanelBorderA
                );
            }

            Function.Call(
                Hash.DRAW_RECT,
                screenX,
                screenY,
                panelWidth,
                panelHeight,
                _pedHudPanelR,
                _pedHudPanelG,
                _pedHudPanelB,
                _pedHudPanelA
            );

            // =====================================================
            // NAME - TEXT ONLY / TANPA ICON
            // =====================================================
            if (showName)
            {
                float nameY =
                    screenY +
                    (
                        showHealthBar
                            ? _pedHudNameOffsetY
                            : -0.010f
                    );

                DrawTextOnScreen(
                    displayName,
                    screenX,
                    nameY,
                    nameScale,
                    _pedHudNameR,
                    _pedHudNameG,
                    _pedHudNameB,
                    _pedHudNameA,
                    true,
                    _pedHudNameFont
                );
            }

            // =====================================================
            // HEALTH BAR
            // =====================================================
            if (showHealthBar)
            {
                int currentHealth =
                    Math.Max(
                        0,
                        ped.Health - 100
                    );

                int maxHealth =
                    Math.Max(
                        1,
                        ped.MaxHealth - 100
                    );

                float healthPct =
                    Math.Max(
                        0.0f,
                        Math.Min(
                            1.0f,
                            (float)currentHealth /
                            maxHealth
                        )
                    );

                float barWidth =
                    Math.Max(
                        0.015f,
                        panelWidth -
                        (_pedHudHealthHorizontalPadding * 2.0f)
                    );

                float barHeight =
                    Math.Max(
                        0.002f,
                        _pedHudHealthHeight
                    );

                float barY =
                    screenY +
                    (
                        showName
                            ? _pedHudHealthOffsetY
                            : 0.0f
                    );

                float healthBorderSize =
                    Math.Max(
                        0.0f,
                        _pedHudHealthBorderSize
                    );

                if (healthBorderSize > 0.0f)
                {
                    Function.Call(
                        Hash.DRAW_RECT,
                        screenX,
                        barY,
                        barWidth + (healthBorderSize * 2.0f),
                        barHeight + (healthBorderSize * 2.0f),
                        _pedHudHealthBorderR,
                        _pedHudHealthBorderG,
                        _pedHudHealthBorderB,
                        _pedHudHealthBorderA
                    );
                }

                Function.Call(
                    Hash.DRAW_RECT,
                    screenX,
                    barY,
                    barWidth,
                    barHeight,
                    _pedHudHealthTrackR,
                    _pedHudHealthTrackG,
                    _pedHudHealthTrackB,
                    _pedHudHealthTrackA
                );

                if (healthPct > 0.001f)
                {
                    int fillR;
                    int fillG;
                    int fillB;

                    switch (hudType)
                    {
                        case PedHudType.Bodyguard:
                            fillR = _pedHudBodyguardR;
                            fillG = _pedHudBodyguardG;
                            fillB = _pedHudBodyguardB;
                            break;

                        case PedHudType.Animal:
                            fillR = _pedHudAnimalR;
                            fillG = _pedHudAnimalG;
                            fillB = _pedHudAnimalB;
                            break;

                        default:
                            fillR = _pedHudEnemyR;
                            fillG = _pedHudEnemyG;
                            fillB = _pedHudEnemyB;
                            break;
                    }

                    Function.Call(
                        Hash.DRAW_RECT,
                        screenX -
                            (barWidth / 2.0f) +
                            ((barWidth * healthPct) / 2.0f),
                        barY,
                        barWidth * healthPct,
                        barHeight,
                        fillR,
                        fillG,
                        fillB,
                        _pedHudHealthFillA
                    );
                }
            }
        }

        private void DrawDashedRectOutline(
            float centerX,
            float centerY,
            float width,
            float height,
            float thickness,
            float dashLength,
            float dashGap,
            int r,
            int g,
            int b,
            int a)
        {
            float safeThickness =
                Math.Max(
                    0.001f,
                    thickness
                );

            float safeDash =
                Math.Max(
                    0.004f,
                    dashLength
                );

            float safeGap =
                Math.Max(
                    0.002f,
                    dashGap
                );

            float left =
                centerX -
                (
                    width /
                    2.0f
                );

            float right =
                centerX +
                (
                    width /
                    2.0f
                );

            float top =
                centerY -
                (
                    height /
                    2.0f
                );

            float bottom =
                centerY +
                (
                    height /
                    2.0f
                );

            // Horizontal dashed lines: top + bottom.
            float x =
                left;

            while (x < right)
            {
                float remaining =
                    right -
                    x;

                float segmentWidth =
                    Math.Min(
                        safeDash,
                        remaining
                    );

                float segmentCenterX =
                    x +
                    (
                        segmentWidth /
                        2.0f
                    );

                Function.Call(
                    Hash.DRAW_RECT,
                    segmentCenterX,
                    top,
                    segmentWidth,
                    safeThickness,
                    r,
                    g,
                    b,
                    a
                );

                Function.Call(
                    Hash.DRAW_RECT,
                    segmentCenterX,
                    bottom,
                    segmentWidth,
                    safeThickness,
                    r,
                    g,
                    b,
                    a
                );

                x +=
                    safeDash +
                    safeGap;
            }

            // Vertical dashed lines: left + right.
            float y =
                top;

            while (y < bottom)
            {
                float remaining =
                    bottom -
                    y;

                float segmentHeight =
                    Math.Min(
                        safeDash,
                        remaining
                    );

                float segmentCenterY =
                    y +
                    (
                        segmentHeight /
                        2.0f
                    );

                Function.Call(
                    Hash.DRAW_RECT,
                    left,
                    segmentCenterY,
                    safeThickness,
                    segmentHeight,
                    r,
                    g,
                    b,
                    a
                );

                Function.Call(
                    Hash.DRAW_RECT,
                    right,
                    segmentCenterY,
                    safeThickness,
                    segmentHeight,
                    r,
                    g,
                    b,
                    a
                );

                y +=
                    safeDash +
                    safeGap;
            }
        }

        // DrawInstantGiftHUD dipindahkan ke file layout HUD.


        private void DrawGiftOutline(float x, float y, float w, float h, float t,
            int r, int g, int b, int a)
        {
            if (w <= 0.0f || h <= 0.0f) return;
            t = Math.Max(0.0005f, t);
            Function.Call(Hash.DRAW_RECT, x, y - h / 2.0f, w, t, r, g, b, a);
            Function.Call(Hash.DRAW_RECT, x, y + h / 2.0f, w, t, r, g, b, a);
            Function.Call(Hash.DRAW_RECT, x - w / 2.0f, y, t, h, r, g, b, a);
            Function.Call(Hash.DRAW_RECT, x + w / 2.0f, y, t, h, r, g, b, a);
        }

        // DrawActiveStatusHUD dipindahkan ke file layout HUD.


        private void DrawTextOnScreen(
            string text,
            float x,
            float y,
            float scale,
            int r,
            int g,
            int b,
            int a,
            bool center,
            int font)
        {
            Function.Call(Hash.SET_TEXT_FONT, font);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call(Hash.SET_TEXT_COLOUR, r, g, b, a);
            Function.Call(Hash.SET_TEXT_WRAP, 0.0f, 1.0f);
            Function.Call(Hash.SET_TEXT_CENTRE, center);

            Function.Call(
                Hash.SET_TEXT_DROPSHADOW,
                _normalTextShadowEnabled
                    ? Math.Max(0, _normalTextShadowDistance)
                    : 0,
                _normalTextShadowR,
                _normalTextShadowG,
                _normalTextShadowB,
                _normalTextShadowEnabled
                    ? _normalTextShadowA
                    : 0
            );

            Function.Call(
                Hash.SET_TEXT_EDGE,
                _normalTextEdgeEnabled
                    ? Math.Max(0, _normalTextEdgeSize)
                    : 0,
                _normalTextEdgeR,
                _normalTextEdgeG,
                _normalTextEdgeB,
                _normalTextEdgeEnabled
                    ? _normalTextEdgeA
                    : 0
            );

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
        }

        private void LoadStartupLoadingDesignFromIni()
        {
            const string path = "scripts/MoonHUD.ini";
            if (!File.Exists(path))
                return;

            try
            {
                Dictionary<string, Dictionary<string, string>> design = ReadHudDesignWithSafePreset(path);
                const string s = "HUD_STARTUP_LOADING";

                _startupLoadingTextEnabled = GetDesignBool(design, s, "Enabled", _startupLoadingTextEnabled);
                _startupLoadingText = GetDesignString(design, s, "Text", _startupLoadingText);
                _startupLoadingTextX = GetDesignFloat(design, s, "TextPosX", _startupLoadingTextX);
                _startupLoadingTextY = GetDesignFloat(design, s, "TextPosY", _startupLoadingTextY);
                _startupLoadingTextScale = GetDesignFloat(design, s, "TextScale", _startupLoadingTextScale);
                _startupLoadingTextFont = GetDesignInt(design, s, "TextFont", _startupLoadingTextFont);
                _startupLoadingTextR = GetDesignInt(design, s, "TextR", _startupLoadingTextR);
                _startupLoadingTextG = GetDesignInt(design, s, "TextG", _startupLoadingTextG);
                _startupLoadingTextB = GetDesignInt(design, s, "TextB", _startupLoadingTextB);
                _startupLoadingTextA = GetDesignInt(design, s, "TextA", _startupLoadingTextA);

                _startupLoadingSubtitle = GetDesignString(design, s, "Subtitle", _startupLoadingSubtitle);
                _startupLoadingSubtitleY = GetDesignFloat(design, s, "SubtitlePosY", _startupLoadingSubtitleY);
                _startupLoadingSubtitleScale = GetDesignFloat(design, s, "SubtitleScale", _startupLoadingSubtitleScale);
                _startupLoadingSubtitleR = GetDesignInt(design, s, "SubtitleR", _startupLoadingSubtitleR);
                _startupLoadingSubtitleG = GetDesignInt(design, s, "SubtitleG", _startupLoadingSubtitleG);
                _startupLoadingSubtitleB = GetDesignInt(design, s, "SubtitleB", _startupLoadingSubtitleB);
                _startupLoadingSubtitleA = GetDesignInt(design, s, "SubtitleA", _startupLoadingSubtitleA);

                _startupLoadingBgR = GetDesignInt(design, s, "BackgroundR", _startupLoadingBgR);
                _startupLoadingBgG = GetDesignInt(design, s, "BackgroundG", _startupLoadingBgG);
                _startupLoadingBgB = GetDesignInt(design, s, "BackgroundB", _startupLoadingBgB);
                _startupLoadingBgA = GetDesignInt(design, s, "BackgroundA", _startupLoadingBgA);
                _startupLoadingMinDisplayMs = Math.Max(0, GetDesignInt(design, s, "MinDisplayMs", _startupLoadingMinDisplayMs));
                _startupLoadingReadyHoldMs = Math.Max(0, GetDesignInt(design, s, "ReadyHoldMs", _startupLoadingReadyHoldMs));
                _startupLoadingMaxWaitMs = Math.Max(5000, GetDesignInt(design, s, "MaxWaitMs", _startupLoadingMaxWaitMs));
                _startupLoadingAnimatedDots = GetDesignBool(design, s, "AnimatedDots", _startupLoadingAnimatedDots);
            }
            catch (Exception ex)
            {
                LogError("STARTUP LOADING HUD ERROR", ex);
            }
        }

        private void LoadStartupGameplaySelectDesignFromIni()
        {
            const string path = "scripts/MoonHUD.ini";
            if (!File.Exists(path)) return;

            try
            {
                Dictionary<string, Dictionary<string, string>> design = ReadHudDesignWithSafePreset(path);
                const string s = "HUD_GAMEPLAY_SELECT";

                _gameplaySelectEnabled = GetDesignBool(design, s, "Enabled", _gameplaySelectEnabled);
                _gameplaySelectTitle = GetDesignString(design, s, "Title", _gameplaySelectTitle);
                _gameplaySelectHeaderX = GetDesignFloat(design, s, "HeaderPosX", _gameplaySelectHeaderX);
                _gameplaySelectHeaderY = GetDesignFloat(design, s, "HeaderPosY", _gameplaySelectHeaderY);
                _gameplaySelectHeaderWidth = GetDesignFloat(design, s, "HeaderWidth", _gameplaySelectHeaderWidth);
                _gameplaySelectHeaderHeight = GetDesignFloat(design, s, "HeaderHeight", _gameplaySelectHeaderHeight);
                _gameplaySelectTitleScale = GetDesignFloat(design, s, "TitleScale", _gameplaySelectTitleScale);
                _gameplaySelectTitleFont = GetDesignInt(design, s, "TitleFont", _gameplaySelectTitleFont);

                _gameplayModeBoxY = GetDesignFloat(design, s, "ModeBoxPosY", _gameplayModeBoxY);
                _gameplayModeBoxWidth = GetDesignFloat(design, s, "ModeBoxWidth", _gameplayModeBoxWidth);
                _gameplayModeBoxHeight = GetDesignFloat(design, s, "ModeBoxHeight", _gameplayModeBoxHeight);
                _gameplaySurvivalBoxX = GetDesignFloat(design, s, "SurvivalPosX", _gameplaySurvivalBoxX);
                _gameplayMountainBoxX = GetDesignFloat(design, s, "GoToMountainPosX", _gameplayMountainBoxX);
                _gameplayModeTextScale = GetDesignFloat(design, s, "ModeTextScale", _gameplayModeTextScale);

                _gameplayNeutralR = GetDesignInt(design, s, "NeutralR", _gameplayNeutralR);
                _gameplayNeutralG = GetDesignInt(design, s, "NeutralG", _gameplayNeutralG);
                _gameplayNeutralB = GetDesignInt(design, s, "NeutralB", _gameplayNeutralB);
                _gameplayNeutralA = GetDesignInt(design, s, "NeutralA", _gameplayNeutralA);
                _gameplaySelectedR = GetDesignInt(design, s, "SelectedR", _gameplaySelectedR);
                _gameplaySelectedG = GetDesignInt(design, s, "SelectedG", _gameplaySelectedG);
                _gameplaySelectedB = GetDesignInt(design, s, "SelectedB", _gameplaySelectedB);
                _gameplaySelectedA = GetDesignInt(design, s, "SelectedA", _gameplaySelectedA);
                _gameplayBorderR = GetDesignInt(design, s, "BorderR", _gameplayBorderR);
                _gameplayBorderG = GetDesignInt(design, s, "BorderG", _gameplayBorderG);
                _gameplayBorderB = GetDesignInt(design, s, "BorderB", _gameplayBorderB);
                _gameplayBorderA = GetDesignInt(design, s, "BorderA", _gameplayBorderA);
                _gameplayTextR = GetDesignInt(design, s, "TextR", _gameplayTextR);
                _gameplayTextG = GetDesignInt(design, s, "TextG", _gameplayTextG);
                _gameplayTextB = GetDesignInt(design, s, "TextB", _gameplayTextB);
                _gameplayTextA = GetDesignInt(design, s, "TextA", _gameplayTextA);

                _gameplayTotalPosLabel = GetDesignString(design, s, "TotalPosLabel", _gameplayTotalPosLabel);
                _gameplayTotalPosY = GetDesignFloat(design, s, "TotalPosPosY", _gameplayTotalPosY);
                _gameplayTotalPosScale = GetDesignFloat(design, s, "TotalPosScale", _gameplayTotalPosScale);

                _gameplayOptionY = GetDesignFloat(design, s, "OptionPosY", _gameplayOptionY);
                _gameplayOptionWidth = GetDesignFloat(design, s, "OptionWidth", _gameplayOptionWidth);
                _gameplayOptionHeight = GetDesignFloat(design, s, "OptionHeight", _gameplayOptionHeight);
                _gameplayOptionGap = GetDesignFloat(design, s, "OptionGap", _gameplayOptionGap);
                _gameplayOptionTextScale = GetDesignFloat(design, s, "OptionTextScale", _gameplayOptionTextScale);
                _gameplayStartX = GetDesignFloat(design, s, "StartPosX", _gameplayStartX);
                _gameplayStartY = GetDesignFloat(design, s, "StartPosY", _gameplayStartY);
                _gameplayStartWidth = GetDesignFloat(design, s, "StartWidth", _gameplayStartWidth);
                _gameplayStartHeight = GetDesignFloat(design, s, "StartHeight", _gameplayStartHeight);
                _gameplayStartTextScale = GetDesignFloat(design, s, "StartTextScale", _gameplayStartTextScale);
            }
            catch (Exception ex)
            {
                LogError("GAMEPLAY SELECT HUD ERROR", ex);
            }
        }

        private void LoadStartupDifficultySelectDesignFromIni()
        {
            const string path = "scripts/MoonHUD.ini";
            if (!File.Exists(path)) return;

            try
            {
                Dictionary<string, Dictionary<string, string>> design = ReadHudDesignWithSafePreset(path);
                const string d = "HUD_DIFFICULTY_SELECT";

                _difficultySelectEnabled = GetDesignBool(design, d, "Enabled", _difficultySelectEnabled);
                _difficultySelectTitle = GetDesignString(design, d, "Title", _difficultySelectTitle);
                _difficultyHeaderX = GetDesignFloat(design, d, "HeaderPosX", _difficultyHeaderX);
                _difficultyHeaderY = GetDesignFloat(design, d, "HeaderPosY", _difficultyHeaderY);
                _difficultyHeaderWidth = GetDesignFloat(design, d, "HeaderWidth", _difficultyHeaderWidth);
                _difficultyHeaderHeight = GetDesignFloat(design, d, "HeaderHeight", _difficultyHeaderHeight);
                _difficultyTitleScale = GetDesignFloat(design, d, "TitleScale", _difficultyTitleScale);
                _difficultyTitleFont = GetDesignInt(design, d, "TitleFont", _difficultyTitleFont);
                _difficultyBoxY = GetDesignFloat(design, d, "BoxPosY", _difficultyBoxY);
                _difficultyBoxWidth = GetDesignFloat(design, d, "BoxWidth", _difficultyBoxWidth);
                _difficultyBoxHeight = GetDesignFloat(design, d, "BoxHeight", _difficultyBoxHeight);
                _difficultyEasyX = GetDesignFloat(design, d, "EasyPosX", _difficultyEasyX);
                _difficultyNormalX = GetDesignFloat(design, d, "NormalPosX", _difficultyNormalX);
                _difficultyHardX = GetDesignFloat(design, d, "HardPosX", _difficultyHardX);
                _difficultyTextScale = GetDesignFloat(design, d, "TextScale", _difficultyTextScale);
                _difficultyStartX = GetDesignFloat(design, d, "StartPosX", _difficultyStartX);
                _difficultyStartY = GetDesignFloat(design, d, "StartPosY", _difficultyStartY);
                _difficultyStartWidth = GetDesignFloat(design, d, "StartWidth", _difficultyStartWidth);
                _difficultyStartHeight = GetDesignFloat(design, d, "StartHeight", _difficultyStartHeight);
                _difficultyStartTextScale = GetDesignFloat(design, d, "StartTextScale", _difficultyStartTextScale);
                _difficultyNeutralR = GetDesignInt(design, d, "NeutralR", _difficultyNeutralR);
                _difficultyNeutralG = GetDesignInt(design, d, "NeutralG", _difficultyNeutralG);
                _difficultyNeutralB = GetDesignInt(design, d, "NeutralB", _difficultyNeutralB);
                _difficultyNeutralA = GetDesignInt(design, d, "NeutralA", _difficultyNeutralA);
                _difficultySelectedR = GetDesignInt(design, d, "SelectedR", _difficultySelectedR);
                _difficultySelectedG = GetDesignInt(design, d, "SelectedG", _difficultySelectedG);
                _difficultySelectedB = GetDesignInt(design, d, "SelectedB", _difficultySelectedB);
                _difficultySelectedA = GetDesignInt(design, d, "SelectedA", _difficultySelectedA);
                _difficultyBorderR = GetDesignInt(design, d, "BorderR", _difficultyBorderR);
                _difficultyBorderG = GetDesignInt(design, d, "BorderG", _difficultyBorderG);
                _difficultyBorderB = GetDesignInt(design, d, "BorderB", _difficultyBorderB);
                _difficultyBorderA = GetDesignInt(design, d, "BorderA", _difficultyBorderA);
                _difficultyTextR = GetDesignInt(design, d, "TextR", _difficultyTextR);
                _difficultyTextG = GetDesignInt(design, d, "TextG", _difficultyTextG);
                _difficultyTextB = GetDesignInt(design, d, "TextB", _difficultyTextB);
                _difficultyTextA = GetDesignInt(design, d, "TextA", _difficultyTextA);
            }
            catch (Exception ex)
            {
                LogError("DIFFICULTY SELECT HUD ERROR", ex);
            }
        }

        private void HandleStartupGameplayMouseInput()
        {
            if (_startupUiStage != StartupUiStage.Gameplay || !_gameplaySelectEnabled)
                return;

            float mx = GetStartupMouseX();
            float my = GetStartupMouseY();
            if (!IsStartupMouseClick()) return;

            if (IsStartupPointInside(mx, my, _gameplaySurvivalBoxX, _gameplayModeBoxY,
                _gameplayModeBoxWidth, _gameplayModeBoxHeight))
            {
                _selectedGameplayMode = MoonGameplayMode.Survival;
                _selectedGameplayPosCount = 0;
                _selectedGoToMountainDirect = false;
                _selectedSurvivalDirectFinish = false;
                return;
            }

            if (IsStartupPointInside(mx, my, _gameplayMountainBoxX, _gameplayModeBoxY,
                _gameplayModeBoxWidth, _gameplayModeBoxHeight))
            {
                _selectedGameplayMode = MoonGameplayMode.GoToMountain;
                _selectedGameplayPosCount = 0;
                _selectedGoToMountainDirect = false;
                _selectedSurvivalDirectFinish = false;
                return;
            }

            if (_selectedGameplayMode == MoonGameplayMode.None)
                return;

            List<Tuple<string, int, bool>> options = BuildStartupGameplayRouteOptions();

            float totalWidth = (options.Count * _gameplayOptionWidth) +
                ((options.Count - 1) * _gameplayOptionGap);
            float firstX = 0.5f - (totalWidth * 0.5f) + (_gameplayOptionWidth * 0.5f);

            for (int i = 0; i < options.Count; i++)
            {
                float x = firstX + i * (_gameplayOptionWidth + _gameplayOptionGap);
                if (!IsStartupPointInside(mx, my, x, _gameplayOptionY,
                    _gameplayOptionWidth, _gameplayOptionHeight))
                {
                    continue;
                }

                if (options[i].Item3)
                {
                    _selectedGameplayPosCount = 0;
                    _selectedGoToMountainDirect =
                        _selectedGameplayMode == MoonGameplayMode.GoToMountain;
                    _selectedSurvivalDirectFinish =
                        _selectedGameplayMode == MoonGameplayMode.Survival;
                }
                else
                {
                    _selectedGameplayPosCount = options[i].Item2;
                    _selectedGoToMountainDirect = false;
                    _selectedSurvivalDirectFinish = false;
                }
                return;
            }

            bool routeChoiceReady =
                IsSelectedDirectRouteMode() || _selectedGameplayPosCount > 0;

            if (routeChoiceReady &&
                IsStartupPointInside(mx, my, _gameplayStartX, _gameplayStartY,
                    _gameplayStartWidth, _gameplayStartHeight))
            {
                if (!IsSelectedDirectRouteMode())
                {
                    _selectedGameplayPosCount =
                        NormalizeSelectedGameplayPosCount(_selectedGameplayPosCount);
                }

                _selectedDifficultyMode = MoonDifficultyMode.None;
                _startupUiStage = StartupUiStage.Difficulty;
            }
        }

        private void DrawStartupGameplayModePanel()
        {
            if (!_gameplaySelectEnabled) return;

            Function.Call(Hash.DRAW_RECT, _gameplaySelectHeaderX, _gameplaySelectHeaderY,
                _gameplaySelectHeaderWidth, _gameplaySelectHeaderHeight,
                _startupPanelR, _startupPanelG, _startupPanelB, _startupPanelA);
            DrawStartupText(_gameplaySelectTitle, _gameplaySelectHeaderX,
                _gameplaySelectHeaderY - 0.030f, _gameplaySelectTitleScale,
                _gameplayTextR, _gameplayTextG, _gameplayTextB, _gameplayTextA,
                _gameplaySelectTitleFont);

            DrawStartupChoiceBox(_gameplaySurvivalBoxX, _gameplayModeBoxY,
                _gameplayModeBoxWidth, _gameplayModeBoxHeight, "SURVIVAL",
                _selectedGameplayMode == MoonGameplayMode.Survival, _gameplayModeTextScale);

            DrawStartupChoiceBox(_gameplayMountainBoxX, _gameplayModeBoxY,
                _gameplayModeBoxWidth, _gameplayModeBoxHeight, "GO TO MOUNTAIN",
                _selectedGameplayMode == MoonGameplayMode.GoToMountain, _gameplayModeTextScale);

            if (_selectedGameplayMode != MoonGameplayMode.None)
            {
                DrawStartupText(
                    _gameplayTotalPosLabel,
                    0.500f,
                    _gameplayTotalPosY,
                    _gameplayTotalPosScale,
                    _gameplayTextR, _gameplayTextG, _gameplayTextB, _gameplayTextA,
                    _gameplaySelectTitleFont);

                List<Tuple<string, int, bool>> options = BuildStartupGameplayRouteOptions();

                float totalWidth = (options.Count * _gameplayOptionWidth) +
                    ((options.Count - 1) * _gameplayOptionGap);
                float firstX = 0.5f - (totalWidth * 0.5f) + (_gameplayOptionWidth * 0.5f);

                for (int i = 0; i < options.Count; i++)
                {
                    bool selected = options[i].Item3
                        ? IsSelectedDirectRouteMode()
                        : (!IsSelectedDirectRouteMode() &&
                           _selectedGameplayPosCount == options[i].Item2);
                    float x = firstX + i * (_gameplayOptionWidth + _gameplayOptionGap);
                    DrawStartupChoiceBox(x, _gameplayOptionY, _gameplayOptionWidth,
                        _gameplayOptionHeight, options[i].Item1, selected, _gameplayOptionTextScale);
                }
            }

            bool ready =
                _selectedGameplayMode != MoonGameplayMode.None &&
                (IsSelectedDirectRouteMode() || _selectedGameplayPosCount > 0);

            DrawStartupActionButton(_gameplayStartX, _gameplayStartY,
                _gameplayStartWidth, _gameplayStartHeight, "NEXT", ready,
                _gameplayStartTextScale);
        }

        private void HandleStartupDifficultyMouseInput()
        {
            if (_startupUiStage != StartupUiStage.Difficulty || !_difficultySelectEnabled)
                return;

            float mx = GetStartupMouseX();
            float my = GetStartupMouseY();
            if (!IsStartupMouseClick()) return;

            if (IsStartupPointInside(mx, my, _difficultyEasyX, _difficultyBoxY,
                _difficultyBoxWidth, _difficultyBoxHeight))
            {
                _selectedDifficultyMode = MoonDifficultyMode.Easy;
                return;
            }

            if (IsStartupPointInside(mx, my, _difficultyNormalX, _difficultyBoxY,
                _difficultyBoxWidth, _difficultyBoxHeight))
            {
                _selectedDifficultyMode = MoonDifficultyMode.Normal;
                return;
            }

            if (IsStartupPointInside(mx, my, _difficultyHardX, _difficultyBoxY,
                _difficultyBoxWidth, _difficultyBoxHeight))
            {
                _selectedDifficultyMode = MoonDifficultyMode.Hard;
                return;
            }

            if (_selectedDifficultyMode != MoonDifficultyMode.None &&
                IsStartupPointInside(mx, my, _difficultyStartX, _difficultyStartY,
                    _difficultyStartWidth, _difficultyStartHeight))
            {
                _gameplaySelectionLocked = true;
                _startupNameConfirmed = true;
            }
        }

        private void DrawStartupDifficultyPanel()
        {
            if (!_difficultySelectEnabled) return;

            Function.Call(Hash.DRAW_RECT, _difficultyHeaderX, _difficultyHeaderY,
                _difficultyHeaderWidth, _difficultyHeaderHeight,
                _startupPanelR, _startupPanelG, _startupPanelB, _startupPanelA);

            DrawStartupText(_difficultySelectTitle, _difficultyHeaderX,
                _difficultyHeaderY - 0.030f, _difficultyTitleScale,
                _difficultyTextR, _difficultyTextG, _difficultyTextB, _difficultyTextA,
                _difficultyTitleFont);

            DrawStartupDifficultyChoiceBox(_difficultyEasyX, _difficultyBoxY,
                _difficultyBoxWidth, _difficultyBoxHeight, "EASY",
                _selectedDifficultyMode == MoonDifficultyMode.Easy, _difficultyTextScale);

            DrawStartupDifficultyChoiceBox(_difficultyNormalX, _difficultyBoxY,
                _difficultyBoxWidth, _difficultyBoxHeight, "NORMAL",
                _selectedDifficultyMode == MoonDifficultyMode.Normal, _difficultyTextScale);

            DrawStartupDifficultyChoiceBox(_difficultyHardX, _difficultyBoxY,
                _difficultyBoxWidth, _difficultyBoxHeight, "HARD",
                _selectedDifficultyMode == MoonDifficultyMode.Hard, _difficultyTextScale);

            DrawStartupActionButton(_difficultyStartX, _difficultyStartY,
                _difficultyStartWidth, _difficultyStartHeight, "START",
                _selectedDifficultyMode != MoonDifficultyMode.None,
                _difficultyStartTextScale);
        }

        private void DrawStartupDifficultyChoiceBox(float x, float y, float width, float height,
            string text, bool selected, float textScale)
        {
            int r = selected ? _difficultySelectedR : _difficultyNeutralR;
            int g = selected ? _difficultySelectedG : _difficultyNeutralG;
            int b = selected ? _difficultySelectedB : _difficultyNeutralB;
            int aValue = selected ? _difficultySelectedA : _difficultyNeutralA;

            Function.Call(Hash.DRAW_RECT, x + 0.002f, y + 0.004f, width, height, 0, 0, 0, 95);
            Function.Call(Hash.DRAW_RECT, x, y, width, height, r, g, b, aValue);
            const float t = 0.0012f;
            Function.Call(Hash.DRAW_RECT, x, y - height * 0.5f, width, t,
                _difficultyBorderR, _difficultyBorderG, _difficultyBorderB, _difficultyBorderA);
            Function.Call(Hash.DRAW_RECT, x, y + height * 0.5f, width, t,
                _difficultyBorderR, _difficultyBorderG, _difficultyBorderB, _difficultyBorderA);
            Function.Call(Hash.DRAW_RECT, x - width * 0.5f, y, t, height,
                _difficultyBorderR, _difficultyBorderG, _difficultyBorderB, _difficultyBorderA);
            Function.Call(Hash.DRAW_RECT, x + width * 0.5f, y, t, height,
                _difficultyBorderR, _difficultyBorderG, _difficultyBorderB, _difficultyBorderA);

            DrawStartupText(text, x, y - 0.015f, textScale,
                _difficultyTextR, _difficultyTextG, _difficultyTextB, _difficultyTextA, 4);
        }

        private void DrawStartupChoiceBox(float x, float y, float width, float height,
            string text, bool selected, float textScale)
        {
            int r = selected ? _gameplaySelectedR : _gameplayNeutralR;
            int g = selected ? _gameplaySelectedG : _gameplayNeutralG;
            int b = selected ? _gameplaySelectedB : _gameplayNeutralB;
            int a = selected ? _gameplaySelectedA : _gameplayNeutralA;

            Function.Call(Hash.DRAW_RECT, x + 0.002f, y + 0.004f, width, height, 0, 0, 0, 95);
            Function.Call(Hash.DRAW_RECT, x, y, width, height, r, g, b, a);
            const float t = 0.0012f;
            Function.Call(Hash.DRAW_RECT, x, y - height * 0.5f, width, t,
                _gameplayBorderR, _gameplayBorderG, _gameplayBorderB, _gameplayBorderA);
            Function.Call(Hash.DRAW_RECT, x, y + height * 0.5f, width, t,
                _gameplayBorderR, _gameplayBorderG, _gameplayBorderB, _gameplayBorderA);
            Function.Call(Hash.DRAW_RECT, x - width * 0.5f, y, t, height,
                _gameplayBorderR, _gameplayBorderG, _gameplayBorderB, _gameplayBorderA);
            Function.Call(Hash.DRAW_RECT, x + width * 0.5f, y, t, height,
                _gameplayBorderR, _gameplayBorderG, _gameplayBorderB, _gameplayBorderA);

            DrawStartupText(text, x, y - 0.015f, textScale,
                _gameplayTextR, _gameplayTextG, _gameplayTextB, _gameplayTextA, 4);
        }

        private void DrawStartupActionButton(float x, float y, float width, float height,
            string text, bool enabled, float textScale)
        {
            int r = enabled ? _startupStartButtonR : _startupStartButtonDisabledR;
            int g = enabled ? _startupStartButtonG : _startupStartButtonDisabledG;
            int b = enabled ? _startupStartButtonB : _startupStartButtonDisabledB;
            int a = enabled ? _startupStartButtonA : _startupStartButtonDisabledA;
            Function.Call(Hash.DRAW_RECT, x, y, width, height, r, g, b, a);
            DrawStartupText(text, x, y - 0.018f, textScale,
                _startupStartButtonTextR, _startupStartButtonTextG,
                _startupStartButtonTextB, _startupStartButtonTextA, 4);
        }

        private List<ActiveStatusItem> BuildActiveStatusItems(int currentTime)
        {
            List<ActiveStatusItem> activeStatuses = new List<ActiveStatusItem>();

            if (currentTime < _superSpeedEndTime)
            {
                int remainingSeconds = (int)Math.Ceiling((_superSpeedEndTime - currentTime) / 1000.0f);
                activeStatuses.Add(new ActiveStatusItem { Key = "SuperSpeed", Revision = _superSpeedEndTime, Text = $"{_statusSuperSpeedLabel} ({remainingSeconds}s)", IsGood = true });
            }

            // FALLING VEHICLES memakai status stack / design yang sama
            // dengan SUPERSPEED. Timer mengikuti total durasi effect,
            // termasuk tambahan durasi ketika gift masuk lagi.
            if (_fallingVehiclesEffectEndTime > 0 &&
                !HasGameTimeReached(
                    currentTime,
                    _fallingVehiclesEffectEndTime
                ))
            {
                int remainingMs =
                    Math.Max(
                        0,
                        _fallingVehiclesEffectEndTime -
                        currentTime
                    );

                int remainingSeconds =
                    (int)Math.Ceiling(
                        remainingMs /
                        1000.0f
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "FallingVehicles",
                        Revision = _fallingVehiclesEffectEndTime,
                        Text =
                            $"{_statusFallingVehiclesLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            // SLEEP memakai status stack / design yang sama dengan SUPERSPEED.
            if (currentTime < _giftSleepEndTime)
            {
                int remainingSeconds =
                    (int)Math.Ceiling(
                        (_giftSleepEndTime - currentTime) /
                        1000.0f
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Sleep",
                        Revision = _giftSleepEndTime,
                        Text =
                            $"{_statusSleepLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            if (currentTime < _rockRainEndTime)
            {
                int remainingSeconds =
                    (int)Math.Ceiling(
                        (_rockRainEndTime - currentTime) /
                        1000.0f
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "BallRain",
                        Revision = _rockRainEndTime,
                        Text =
                            $"{_statusBallRainLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            if (currentTime < _invincibleEndTime)
            {
                int remainingSeconds = (int)Math.Ceiling((_invincibleEndTime - currentTime) / 1000.0f);
                activeStatuses.Add(new ActiveStatusItem { Key = "Invincible", Revision = _invincibleEndTime, Text = $"{_statusInvincibleLabel} ({remainingSeconds}s)", IsGood = true });
            }

            // =========================================================
            // WANTED 5 STAR
            // Pakai design/status stack yang SAMA dengan SUPERSPEED dll.
            // Otomatis muncul di bawah-tengah dan ikut naik kalau ada
            // beberapa status aktif bersamaan.
            // =========================================================
            if (_wantedFiveGiftActive &&
                _wantedFiveGiftEndTime > currentTime)
            {
                int remainingMs =
                    Math.Max(
                        0,
                        _wantedFiveGiftEndTime -
                        currentTime
                    );

                int totalSeconds =
                    (int)Math.Ceiling(
                        remainingMs /
                        1000.0
                    );

                int minutes =
                    totalSeconds /
                    60;

                int seconds =
                    totalSeconds %
                    60;

                string wantedTimer =
                    minutes.ToString("00") +
                    ":" +
                    seconds.ToString("00");

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Wanted",
                        Revision = _wantedFiveGiftEndTime,
                        Text =
                            $"{_statusWantedLabel} ({wantedTimer})",
                        IsGood = false
                    }
                );
            }

            if (currentTime < _giftDisarmEndTime)
            {
                int remainingSeconds =
                    Math.Max(
                        0,
                        (int)Math.Ceiling(
                            (_giftDisarmEndTime - currentTime) /
                            1000.0f
                        )
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Disarm",
                        Revision = _giftDisarmEndTime,
                        Text = $"{_statusDisarmLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            if (currentTime < _blackholeEndTime)
            {
                int remainingSeconds = (int)Math.Ceiling((_blackholeEndTime - currentTime) / 1000.0f);
                activeStatuses.Add(new ActiveStatusItem { Key = "Blackhole", Revision = _blackholeEndTime, Text = $"{_statusBlackholeLabel} ({remainingSeconds}s)" });

                if (_pendingEarthquakeDurationMs > 0)
                {
                    // Countdown ini menunjukkan BERAPA LAMA LAGI
                    // sampai Blackhole selesai dan Earthquake boleh mulai.
                    activeStatuses.Add(
                        new ActiveStatusItem
                        {
                            Key = "EarthquakeWaiting",
                            Revision = _pendingEarthquakeDurationMs,
                            Text = $"{_statusEarthquakeWaitingLabel} ({remainingSeconds}s)"
                        }
                    );
                }
            }

            // =========================================================
            // EARTHQUAKE
            // =========================================================
            if (currentTime < _earthquakeEndTime)
            {
                int remainingSeconds =
                    Math.Max(
                        0,
                        (int)Math.Ceiling(
                            (_earthquakeEndTime - currentTime) /
                            1000.0f
                        )
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Earthquake",
                        Revision = _earthquakeEndTime,
                        Text =
                            $"{_statusEarthquakeLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );

                if (_pendingBlackholeDurationMs > 0)
                {
                    // Countdown ini menunjukkan BERAPA LAMA LAGI
                    // sampai Earthquake selesai dan Blackhole boleh mulai.
                    activeStatuses.Add(
                        new ActiveStatusItem
                        {
                            Key = "BlackholeWaiting",
                            Revision = _pendingBlackholeDurationMs,
                            Text = $"{_statusBlackholeWaitingLabel} ({remainingSeconds}s)"
                        }
                    );
                }
            }

            // =========================================================
            // GIFT CAGE
            // =========================================================
            if (currentTime < _giftCageEndTime)
            {
                int remainingSeconds =
                    Math.Max(
                        0,
                        (int)Math.Ceiling(
                            (_giftCageEndTime - currentTime) /
                            1000.0f
                        )
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Cage",
                        Revision = _giftCageEndTime,
                        Text = $"{_statusCageLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            // =========================================================
            // STANDALONE WEBHOOK EFFECT STATUS
            // Design/layout sama dengan SUPERSPEED ACTIVE.
            // =========================================================
            if (_getTowedEndTime > currentTime)
            {
                int remainingSeconds =
                    (int)Math.Ceiling(
                        (_getTowedEndTime - currentTime) /
                        1000.0f
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "GetTowed",
                        Revision = _getTowedEndTime,
                        Text = $"{_statusGetTowedLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            if (_trafficMagnetEndTime > currentTime)
            {
                int remainingSeconds =
                    (int)Math.Ceiling(
                        (_trafficMagnetEndTime - currentTime) /
                        1000.0f
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "TrafficMagnet",
                        Revision = _trafficMagnetEndTime,
                        Text = $"{_statusTrafficMagnetLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }



            if (_recklessTrafficEndTime > currentTime)
            {
                int remainingSeconds =
                    (int)Math.Ceiling(
                        (_recklessTrafficEndTime - currentTime) /
                        1000.0f
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "RecklessTraffic",
                        Revision = _recklessTrafficEndTime,
                        Text = $"{_statusRecklessTrafficLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            if (_vehicleFireTarget != null &&
                _vehicleFireTarget.Exists() &&
                _vehicleFireExplodeTime > currentTime)
            {
                int remainingSeconds;

                string fireText;

                if (!_vehicleFireIgnited)
                {
                    remainingSeconds =
                        Math.Max(
                            0,
                            (int)Math.Ceiling(
                                (_vehicleFireIgniteTime - currentTime) /
                                1000.0f
                            )
                        );

                    fireText =
                        $"{_statusVehicleFireWarningLabel} ({remainingSeconds}s)";
                }
                else
                {
                    remainingSeconds =
                        Math.Max(
                            0,
                            (int)Math.Ceiling(
                                (_vehicleFireExplodeTime - currentTime) /
                                1000.0f
                            )
                        );

                    fireText =
                        $"{_statusVehicleBurningLabel} ({remainingSeconds}s)";
                }

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "VehicleFire",
                        Revision = _vehicleFireExplodeTime,
                        Text = fireText,
                        IsGood = false
                    }
                );
            }

            if (_burningEndTime > currentTime)
            {
                int remainingSeconds;
                string burningText;

                if (!_burningIgnited)
                {
                    remainingSeconds =
                        Math.Max(
                            0,
                            (int)Math.Ceiling(
                                (_burningIgniteTime - currentTime) /
                                1000.0f
                            )
                        );

                    burningText =
                        $"{_statusBurningWarningLabel} ({remainingSeconds}s)";
                }
                else
                {
                    remainingSeconds =
                        Math.Max(
                            0,
                            (int)Math.Ceiling(
                                (_burningEndTime - currentTime) /
                                1000.0f
                            )
                        );

                    burningText =
                        $"{_statusBurningLabel} ({remainingSeconds}s)";
                }

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Burning",
                        Revision = _burningEndTime,
                        Text = burningText,
                        IsGood = false
                    }
                );
            }

            if (_policeRoadblockEndTime > currentTime)
            {
                int remainingSeconds =
                    (int)Math.Ceiling(
                        (_policeRoadblockEndTime - currentTime) /
                        1000.0f
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "PoliceRoadblock",
                        Revision = _policeRoadblockEndTime,
                        Text = $"{_statusPoliceRoadblockLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            if (_randomExplosionEndTime > currentTime)
            {
                int remainingSeconds =
                    (int)Math.Ceiling(
                        (_randomExplosionEndTime - currentTime) /
                        1000.0f
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "RandomExplosion",
                        Revision = _randomExplosionEndTime,
                        Text = $"{_statusRandomExplosionLabel} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }

            if (_playerAnimalTransformPhase == 1)
            {
                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Transforming",
                        Revision = _playerAnimalModelRequestStartTime,
                        Text = $"{_statusTransformingLabel}: {_playerAnimalDisplayName}",
                        IsGood = false
                    }
                );
            }
            else if (_playerAnimalTransformPhase == 2)
            {
                int remainingSeconds =
                    Math.Max(
                        0,
                        (int)Math.Ceiling(
                            (_playerAnimalTransformEndTime - currentTime) /
                            1000.0f
                        )
                    );

                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Animal",
                        Revision = _playerAnimalTransformEndTime,
                        Text = $"{_statusAnimalLabel}: {_playerAnimalDisplayName} ({remainingSeconds}s)",
                        IsGood = false
                    }
                );
            }
            else if (_playerAnimalTransformPhase == 3)
            {
                activeStatuses.Add(
                    new ActiveStatusItem
                    {
                        Key = "Returning",
                        Revision = _playerAnimalTransformEndTime,
                        Text = _statusReturningLabel,
                        IsGood = false
                    }
                );
            }

            return activeStatuses;
        }


        // =========================================================================
        // HUD LAYOUT DISPATCH
        // =========================================================================
        private bool IsHudLayoutDesign2()
        {
            return string.Equals(
                _hudLayoutName,
                "design2",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private void DrawPlayerHealthBar()
        {
            if (IsHudLayoutDesign2()) DrawPlayerHealthBarLayoutDesign2();
            else DrawPlayerHealthBarLayoutDefault();
        }

        private void DrawCountdownUI(int currentTime)
        {
            if (IsHudLayoutDesign2()) DrawCountdownUILayoutDesign2(currentTime);
            else DrawCountdownUILayoutDefault(currentTime);
        }

        private void DrawRandomCheckpointMarkers(float barLeftX, float trackY, float activeBarWidth)
        {
            if (IsHudLayoutDesign2()) DrawRandomCheckpointMarkersLayoutDesign2(barLeftX, trackY, activeBarWidth);
            else DrawRandomCheckpointMarkersLayoutDefault(barLeftX, trackY, activeBarWidth);
        }

        private void DrawRandomCheckpointUI(int currentTime)
        {
            if (IsHudLayoutDesign2()) DrawRandomCheckpointUILayoutDesign2(currentTime);
            else DrawRandomCheckpointUILayoutDefault(currentTime);
        }

        private void DrawScoreCounter()
        {
            if (IsHudLayoutDesign2()) DrawScoreCounterLayoutDesign2();
            else DrawScoreCounterLayoutDefault();
        }

        private void DrawDeathCounter()
        {
            if (IsHudLayoutDesign2()) DrawDeathCounterLayoutDesign2();
            else DrawDeathCounterLayoutDefault();
        }

        private void DrawRouteLoadingProgressPanel()
        {
            if (IsHudLayoutDesign2()) DrawRouteLoadingProgressPanelLayoutDesign2();
            else DrawRouteLoadingProgressPanelLayoutDefault();
        }

        private void DrawProgressBar()
        {
            if (IsHudLayoutDesign2()) DrawProgressBarLayoutDesign2();
            else DrawProgressBarLayoutDefault();
        }

        private void DrawRandomGatchaUI(int currentTime)
        {
            if (IsHudLayoutDesign2()) DrawRandomGatchaUILayoutDesign2(currentTime);
            else DrawRandomGatchaUILayoutDefault(currentTime);
        }

        private void DrawTeleportGachaCardsUI(TeleportLocationItem activeLocation, bool isLocked)
        {
            if (IsHudLayoutDesign2()) DrawTeleportGachaCardsUILayoutDesign2(activeLocation, isLocked);
            else DrawTeleportGachaCardsUILayoutDefault(activeLocation, isLocked);
        }

        private void DrawInstantGiftHUD(int currentTime)
        {
            if (IsHudLayoutDesign2()) DrawInstantGiftHUDLayoutDesign2(currentTime);
            else DrawInstantGiftHUDLayoutDefault(currentTime);
        }

        private void DrawActiveStatusHUD(int currentTime)
        {
            if (IsHudLayoutDesign2()) DrawActiveStatusHUDLayoutDesign2(currentTime);
            else DrawActiveStatusHUDLayoutDefault(currentTime);
        }


    }
}
