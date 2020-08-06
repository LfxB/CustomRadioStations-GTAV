using GTA;
using GTA.Native;
using System;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GTAVFunctions;

namespace CustomRadioStations
{
    public class NativeWheelOrganizerScript : Script
    {
        const string logPath = "scripts\\Custom Radio Stations\\NativeStations.log";
        const string orgList = "scripts\\Custom Radio Stations\\NativeWheels.cfg";

        NativeWheel currentWheel;

        List<string> validStationNames;

        int maxStationCount;

        bool Event_JUST_OPENED_OnNextOpen = true;

        bool loaded;

        public NativeWheelOrganizerScript()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAbort;

            Interval = 10;
        }

        private void OnAbort(object sender, EventArgs e)
        {
            UnhideAllStations();
        }

        void UnhideAllStations()
        {
            for (int i = 0; i < maxStationCount; i++)
            {
                RadioNativeFunctions._LOCK_RADIO_STATION(RadioNativeFunctions.GET_RADIO_STATION_NAME(i), false);
            }
        }

        void LogAllStations()
        {
            Logger.Init(logPath);

            Logger.Log("Game version: " + Game.Version.ToString(), logPath);
            if ((int)Game.Version < (int)GameVersion.VER_1_0_1493_0_STEAM)
                Logger.Log("WARNING: Can't use the native radio wheel organizer on this game version. " +
                    "Please update to 1.0.1493.0 or higher.");

            Logger.Log("Checking all native and add-on radios...", logPath);

            maxStationCount = RadioNativeFunctions._MAX_RADIO_STATION_INDEX();

            validStationNames = new List<string>();

            for (int i = 0; i < maxStationCount; i++)
            {
                string stationName = RadioNativeFunctions.GET_RADIO_STATION_NAME(i);
                validStationNames.Add(stationName);
                string s = "Name: " + stationName + " || Proper name: " + RadioNativeFunctions.GetRadioStationProperName(i);
                Logger.Log(s, logPath);
            }

            Logger.Log("Please use the 'Name' name for your wheel organization lists (NativeWheels.cfg)! 'Proper name' is only for display purposes.", logPath);
        }

        void GetOrganizationLists()
        {
            if (!File.Exists(orgList)) return;
            
            string[] lines = File.ReadAllLines(orgList);

            bool lastLineWasWheelName = false;

            foreach (string line in lines)
            {
                if (String.IsNullOrWhiteSpace(line)) continue;

                string l = line.Trim();

                if (l.Contains("[") && l.Contains("]"))
                {
                    if (lastLineWasWheelName)
                    {
                        NativeWheel.WheelList.Remove(NativeWheel.WheelList.Last());
                    }

                    var wheel = new NativeWheel(l.Substring(1, l.Length - 2));
                    NativeWheel.WheelList.Add(wheel);
                    lastLineWasWheelName = true;
                    continue;
                }

                if (WheelListIsPopulated() && validStationNames.Any(s => s.Equals(l)))
                {
                    NativeWheel.WheelList.Last().stationList.Add(l);
                    lastLineWasWheelName = false;
                }
            }

            if (WheelListIsPopulated())
            {
                currentWheel = NativeWheel.WheelList[0];

                //foreach (var w in NativeWheel.WheelList)
                //{
                //    w.stationList.ForEach(x => Logger.Log(x, logPath));
                //}
            }
        }

        bool WheelListIsPopulated()
        {
            return NativeWheel.WheelList.Count > 0;
        }

        void OnTick(object sender, EventArgs e)
        {
            if (GTAFunction.HasCheatStringJustBeenEntered("radio_reload"))
            {
                UnhideAllStations();
                NativeWheel.WheelList = null;
                currentWheel = null;
                GetOrganizationLists();
                loaded = true;
                Wait(150);
            }

            if (RadioNativeFunctions.IsRadioHudComponentVisible())
            {
                if (!loaded && Game.Player.CanControlCharacter)
                {
                    LogAllStations();
                    GetOrganizationLists();
                    loaded = true;
                }

                ShowHelpTexts();

                ControlWheelChange();

                if (Event_JUST_OPENED_OnNextOpen)
                {
                    OnJustOpened();
                }

                DisableNativeScrollRadioControls();

                Event_JUST_OPENED_OnNextOpen = false;
            }
            else
            {
                if (!loaded) return;

                if (!Event_JUST_OPENED_OnNextOpen)
                {
                    OnJustClosed();
                    Event_JUST_OPENED_OnNextOpen = true;
                }
            }
        }

        GTA.Control ControlNextWheel;
        GTA.Control ControlPrevWheel;
        void ShowHelpTexts()
        {
            ControlNextWheel = GTAFunction.UsingGamepad() ? GTA.Control.VehicleAccelerate : GTA.Control.WeaponWheelPrev;
            ControlPrevWheel = GTAFunction.UsingGamepad() ? GTA.Control.VehicleBrake : GTA.Control.WeaponWheelNext;

            if (!Config.DisplayHelpText) return;

            string nativeWheelText = (int)Game.Version < (int)GameVersion.VER_1_0_1493_0_STEAM ?
                "" :
                (WheelListIsPopulated() ?
                "\n" +
                GTAFunction.InputString(ControlNextWheel) + " " +
                GTAFunction.InputString(ControlPrevWheel) +
                " : Next / Prev Wheel\n" +
                "Wheel: " + currentWheel.Name
                : "");

            GTAFunction.DisplayHelpTextThisFrame(
                GTAFunction.InputString(Config.KB_Toggle, Config.GP_Toggle) +
                " : Switch to Custom Wheels" +
                nativeWheelText
                , false, false
                );
        }

        void DisableNativeScrollRadioControls()
        {
            Game.DisableControlThisFrame(2, GTA.Control.VehicleNextRadio);
            Game.DisableControlThisFrame(2, GTA.Control.VehiclePrevRadio);
        }

        void ControlWheelChange()
        {
            if (!WheelListIsPopulated()) return;

            if (Game.IsControlJustPressed(2, ControlNextWheel))
            {
                currentWheel = NativeWheel.WheelList.GetNext(currentWheel);
                UpdateWheelThisFrame();
            }
            else if (Game.IsControlJustPressed(2, ControlPrevWheel))
            {
                currentWheel = NativeWheel.WheelList.GetPrevious(currentWheel);
                UpdateWheelThisFrame();
            }
        }

        void UpdateWheelThisFrame()
        {
            if (!WheelListIsPopulated()) return;

            // Unhide all listed radios
            foreach (var station in currentWheel.stationList)
            {
                RadioNativeFunctions._LOCK_RADIO_STATION(station, false);
            }

            // Hide any valid station name that isn't in the current wheel station list
            foreach (var station in validStationNames)
            {
                if (!currentWheel.stationList.Any(s => s.Equals(station)))
                {
                    RadioNativeFunctions._LOCK_RADIO_STATION(station, true);
                }
            }
        }

        void OnJustOpened()
        {
            //UI.ShowSubtitle("Just Opened");
            UpdateWheelThisFrame();
        }

        void OnJustClosed()
        {
            //UI.ShowSubtitle("Just Closed");
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
        }
    }

    class NativeWheel
    {
        public string Name;
        public List<string> stationList = new List<string>();

        public NativeWheel(string name)
        {
            Name = name;
        }
        
        public static List<NativeWheel> WheelList = new List<NativeWheel>();
    }
}
