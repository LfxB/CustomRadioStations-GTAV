using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using GTA;
using Control = GTA.Control;
using ScriptSettings = Settings.ScriptSettings;

namespace CustomRadioStations
{
    public static class Config
    {
        private const string iniPath = @"scripts\Custom Radio Stations\settings.ini";

        private const string INI_SECTION_GENERAL = "GENERAL",
                                INI_SECTION_GRAPHICS = "GRAPHICS",
                                INI_SECTION_KEYBOARD_CONTROLS = "KEYBOARD_CONTROLS",
                                INI_SECTION_GAMEPAD_CONTROLS = "GAMEPAD_CONTROLS";

        public static bool CustomWheelAsDefault;
        public static int WheelActionDelay;
        public static int LoadMS;
        public static int LoadStartDelay;
        public static bool DisplayHelpText;
        public static bool EnableWheelSlowmotion;

        public static int IconX;
        public static int IconY;
        public static float WheelRadius;
        public static Color IconBG;
        public static Color IconHL;
        public static double IconBgSizeMultiple;
        public static double IconHlSizeMultiple;

        public static Keys KB_Toggle;
        public static Control KB_Skip_Track;
        public static Control KB_Volume_Up;
        public static Control KB_Volume_Down;

        public static Control GP_Toggle;
        public static Control GP_Skip_Track;
        public static Control GP_Volume_Up;
        public static Control GP_Volume_Down;

        public static void SaveINI()
        {
            ForceDecimal();

            ScriptSettings config = ScriptSettings.Load(iniPath);

            var comment = ";Type 'radio_reload' into the cheat textbox (press ` to access) to reload settings.ini, NativeWheels.cfg, and all station.ini files.";
            config.SetValue<float>(INI_SECTION_GENERAL, "DEFAULT VOLUME (0 to 1.0)", SoundFile.SoundEngine.SoundVolume, comment);
            config.SetValue<bool>(INI_SECTION_GENERAL, "First Custom Wheel Is Default On Startup", CustomWheelAsDefault);
            config.SetValue<int>(INI_SECTION_GENERAL, "WHEEL ACTION DELAY", WheelActionDelay);
            config.SetValue<int>(INI_SECTION_GENERAL, "Load milliseconds (Higher number > increased load time but more stable)", LoadMS);
            config.SetValue<int>(INI_SECTION_GENERAL, "Load Start Delay (Milliseconds)", LoadStartDelay);
            config.SetValue<bool>(INI_SECTION_GENERAL, "Display Help Text and Subtitles", DisplayHelpText);
            config.SetValue<bool>(INI_SECTION_GENERAL, "Enable Wheel Slowmotion", EnableWheelSlowmotion);

            config.SetValue<int>(INI_SECTION_GRAPHICS, "ICON X SIZE", IconX);
            config.SetValue<int>(INI_SECTION_GRAPHICS, "ICON Y SIZE", IconY);
            config.SetValue<float>(INI_SECTION_GRAPHICS, "WHEEL RADIUS", WheelRadius);
            config.SetValue<string>(INI_SECTION_GRAPHICS, "ICON BACKGROUND COLOR", GeneralHelper.ColorToHex(IconBG));
            config.SetValue<string>(INI_SECTION_GRAPHICS, "ICON HIGHLIGHT COLOR", GeneralHelper.ColorToHex(IconHL));
            comment = ";Size multiples - basically sets the background and highlight sizes to a" +
                " percentage of ICON X SIZE by ICON Y SIZE.";
            config.SetValue<double>(INI_SECTION_GRAPHICS, "BACKGROUND ICON SIZE MULTIPLE", IconBgSizeMultiple, comment);
            config.SetValue<double>(INI_SECTION_GRAPHICS, "HIGHLIGHT ICON SIZE MULTIPLE", IconHlSizeMultiple);

            comment = ";The keyboard toggle key control uses generic windows keys: https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=netframework-4.7.2";
            config.SetValue<Keys>(INI_SECTION_KEYBOARD_CONTROLS, "Toggle: Q +", KB_Toggle, comment);
            comment = ";The rest are GTA controls. Here's a list: https://raw.githubusercontent.com/crosire/scripthookvdotnet/dev_v2/source/scripting/Controls.hpp";
            config.SetValue<Control>(INI_SECTION_KEYBOARD_CONTROLS, "Skip Track", KB_Skip_Track, comment);
            config.SetValue<Control>(INI_SECTION_KEYBOARD_CONTROLS, "Volume Up", KB_Volume_Up);
            config.SetValue<Control>(INI_SECTION_KEYBOARD_CONTROLS, "Volume Down", KB_Volume_Down);

            config.SetValue<Control>(INI_SECTION_GAMEPAD_CONTROLS, "Toggle: D-Pad Left +", GP_Toggle);
            config.SetValue<Control>(INI_SECTION_GAMEPAD_CONTROLS, "Skip Track", Control.VehicleHandbrake);
            config.SetValue<Control>(INI_SECTION_GAMEPAD_CONTROLS, "Volume Up", GP_Skip_Track);
            config.SetValue<Control>(INI_SECTION_GAMEPAD_CONTROLS, "Volume Down", GP_Volume_Down);

            config.Save();
        }

        public static void LoadINI()
        {
            ForceDecimal();

            ScriptSettings config = ScriptSettings.Load(iniPath);

            SoundFile.SoundEngine.SoundVolume = config.GetValue<float>(INI_SECTION_GENERAL, "DEFAULT VOLUME (0 to 1.0)", 0.3f);
            CustomWheelAsDefault = config.GetValue<bool>(INI_SECTION_GENERAL, "First Custom Wheel Is Default On Startup", true);
            WheelActionDelay = config.GetValue<int>(INI_SECTION_GENERAL, "WHEEL ACTION DELAY", 500);
            LoadMS = config.GetValue<int>(INI_SECTION_GENERAL, "Load milliseconds (Higher number > increased load time but more stable)", 1);
            LoadStartDelay = config.GetValue<int>(INI_SECTION_GENERAL, "Load Start Delay (Milliseconds)", 30000);
            DisplayHelpText = config.GetValue<bool>(INI_SECTION_GENERAL, "Display Help Text and Subtitles", true);
            EnableWheelSlowmotion = config.GetValue<bool>(INI_SECTION_GENERAL, "Enable Wheel Slowmotion", true);

            IconX = config.GetValue<int>(INI_SECTION_GRAPHICS, "ICON X SIZE", 30);
            IconY = config.GetValue<int>(INI_SECTION_GRAPHICS, "ICON Y SIZE", 30);
            WheelRadius = config.GetValue<float>(INI_SECTION_GRAPHICS, "WHEEL RADIUS", 300f);
            IconBG = GeneralHelper.HexToColor(config.GetValue<string>(INI_SECTION_GRAPHICS, "ICON BACKGROUND COLOR", "#CC000000"));
            IconHL = GeneralHelper.HexToColor(config.GetValue<string>(INI_SECTION_GRAPHICS, "ICON HIGHLIGHT COLOR", "#FF00CFEE"));
            IconBgSizeMultiple = config.GetValue<double>(INI_SECTION_GRAPHICS, "BACKGROUND ICON SIZE MULTIPLE", 1.35);
            IconHlSizeMultiple = config.GetValue<double>(INI_SECTION_GRAPHICS, "HIGHLIGHT ICON SIZE MULTIPLE", 1.45);

            KB_Toggle = config.GetValue<Keys>(INI_SECTION_KEYBOARD_CONTROLS, "Toggle: Q +", Keys.E);
            KB_Skip_Track = config.GetValue<Control>(INI_SECTION_KEYBOARD_CONTROLS, "Skip Track", Control.PhoneRight);
            KB_Volume_Up = config.GetValue<Control>(INI_SECTION_KEYBOARD_CONTROLS, "Volume Up", Control.PhoneUp);
            KB_Volume_Down = config.GetValue<Control>(INI_SECTION_KEYBOARD_CONTROLS, "Volume Down", Control.PhoneDown);

            GP_Toggle = config.GetValue<Control>(INI_SECTION_GAMEPAD_CONTROLS, "Toggle: D-Pad Left +", Control.VehicleDuck);
            GP_Skip_Track = config.GetValue<Control>(INI_SECTION_GAMEPAD_CONTROLS, "Skip Track", Control.VehicleHandbrake);
            GP_Volume_Up = config.GetValue<Control>(INI_SECTION_GAMEPAD_CONTROLS, "Volume Up", Control.MoveUpOnly);
            GP_Volume_Down = config.GetValue<Control>(INI_SECTION_GAMEPAD_CONTROLS, "Volume Down", Control.MoveDownOnly);

            SaveINI();
        }

        public static (int iconX, int iconY, float wheelRadius) LoadWheelINI(string directory)
        {
            ForceDecimal();

            ScriptSettings config = ScriptSettings.Load(directory + "\\settings.ini");

            int iconX = config.GetValue<int>(INI_SECTION_GRAPHICS, "ICON X SIZE", IconX);
            int iconY = config.GetValue<int>(INI_SECTION_GRAPHICS, "ICON Y SIZE", IconY);
            float wheelRadius = config.GetValue<float>(INI_SECTION_GRAPHICS, "WHEEL RADIUS", WheelRadius);
            return (iconX, iconY, wheelRadius);
        }

        public static void UpdateWheelsVisuals()
        {
            foreach (var pair in StationWheelPair.List)
            {
                // Go up two levels from pair.IniPath to get wheel settings directory
                string path = pair.IniPath;
                for (int i = 0; i < 2; i++)
                {
                    path = System.IO.Path.GetDirectoryName(path);
                }

                var wheelIni = LoadWheelINI(path);

                pair.Wheel.TextureSize = new Size(wheelIni.iconX, wheelIni.iconY);
                pair.Wheel.Radius = wheelIni.wheelRadius;
                pair.Wheel.SetCategoryBackgroundIcons(MainScript.iconBgPath, IconBG, IconBgSizeMultiple, MainScript.iconhighlightPath, IconHL, IconHlSizeMultiple);
            }
        }

        public static void ReloadStationINIs()
        {
            StationWheelPair.List.ForEach(x => x.LoadStationINI(x.IniPath));
        }

        public static void RescanForTracklists()
        {
            StationWheelPair.List.ForEach(x => x.RescanStationTracklists());
        }

        public static CultureInfo culture;
        public static void SetupSystemCulture()
        {
            culture = new CultureInfo(System.Threading.Thread.CurrentThread.CurrentCulture.Name, true);
            culture.NumberFormat.NumberDecimalSeparator = ".";
            ForceDecimal();
        }

        public static void ForceDecimal()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        }

        public static int loadCounter = 0;      // Count how many audio files are loaded
        public static int loadInterval = 10;    // Add a Wait() every 10 loaded audio files
        public static void LoadTick()
        {
            if (loadCounter % loadInterval == 0)
            {
                Script.Wait(LoadMS);
            }

            loadCounter++;
        }
    }
}
