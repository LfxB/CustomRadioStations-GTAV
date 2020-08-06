using GTA;
using GTA.Native;
using System;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GTA.Math;
using SelectorWheel;
using GTAVFunctions;
using EventHelper;

namespace CustomRadioStations
{
    public class MainScript : Script
    {
        public const string mainPath = @"scripts\Custom Radio Stations\";

        public static string iconBgPath = @"scripts\Custom Radio Stations\iconbg.png";

        public static string iconhighlightPath = @"scripts\Custom Radio Stations\iconhl.png";

        bool canResumeCustomStation;

        bool lastPlayedOnFoot;

        int lastVanillaStationPlayed = 0;

        bool lastRadioWasCustom;

        DateTime? inputTimer = null;

        DateTime? loadDelayTimer = null;

        ActionOptions ActionQueued;

        bool loaded;

        enum ActionOptions
        {
            DoNothing,
            PlayQueued,
            StopCurrent
        }

        public MainScript()
        {
            // Moved to OnTick, after player control is allowed
            Config.SetupSystemCulture();
            Config.LoadINI();
            Logger.Init();
            //SetupRadio();
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Aborted += OnAbort;

            Interval = 10;
        }

        private void OnAbort(object sender, EventArgs e)
        {
            Game.TimeScale = 1f;
            SoundFile.DisposeSoundEngine();
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Function.Call(Hash.SET_AUDIO_FLAG, "DisableFlightMusic", false);
            Function.Call(Hash.SET_AUDIO_FLAG, "DisableWantedMusic", false);
            if (Function.Call<bool>(Hash.IS_AUDIO_SCENE_ACTIVE, "DEATH_SCENE"))
            {
                Function.Call(Hash.STOP_AUDIO_SCENE, "DEATH_SCENE");
                Function.Call(Hash.STOP_AUDIO_SCENE, "FADE_OUT_WORLD_250MS_SCENE");
            }
            if (Function.Call<bool>(Hash.IS_AUDIO_SCENE_ACTIVE, "MP_JOB_CHANGE_RADIO_MUTE"))
            {
                Function.Call(Hash.STOP_AUDIO_SCENE, "MP_JOB_CHANGE_RADIO_MUTE");
            }
        }

        public void SetupRadio()
        {
            // Get folders in script's main folder "Custom Radio Stations"
            string[] wheelDirectories = Directory.GetDirectories(mainPath, "*", SearchOption.TopDirectoryOnly);

            foreach (var wheelDir in wheelDirectories)
            {
                Logger.Log("Loading " + wheelDir);

                Logger.Log("Checking if " + wheelDir + "\\settings.ini exists");

                var wheelIni = Config.LoadWheelINI(wheelDir);

                // Create wheel obj
                Wheel radioWheel = new Wheel("Radio Wheel", wheelDir, 0, 0, new System.Drawing.Size(wheelIni.iconX, wheelIni.iconY), 200, wheelIni.wheelRadius);

                // Get folders in script's main folder "Custom Radio Stations"
                string[] stationDirectories = Directory.GetDirectories(wheelDir, "*", SearchOption.TopDirectoryOnly);

                Logger.Log("Number of stations: " + stationDirectories.Count());

                // Specify file extensions to search for in the next step
                var extensions = new List<string> { ".mp3", ".wav", ".flac", ".lnk" };

                // Keep count of the number of station folders with actual music files
                int populatedStationCount = 0;

                // Generate wheel categories for each folder, which will be our individual stations on the wheel
                foreach (var stationDir in stationDirectories)
                {
                    Logger.Log("Loading " + stationDir);

                    // Get all files that have the above-mentioned extensions.
                    var musicFilePaths = Directory.GetFiles(stationDir, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(x => extensions.Contains(Path.GetExtension(x)));

                    // Don't make a station out of an empty folder
                    if (musicFilePaths.Count() == 0)
                    {
                        Logger.Log("Skipping " + Path.GetFileName(stationDir) + " as there are no music files.");
                        continue;
                    }

                    // Increase count
                    populatedStationCount++;

                    WheelCategory stationCat = new WheelCategory(Path.GetFileName(stationDir));
                    radioWheel.AddCategory(stationCat);
                    WheelCategoryItem stationItem = new WheelCategoryItem(stationCat.Name);
                    stationCat.AddItem(stationItem);

                    RadioStation station = new RadioStation(stationCat, musicFilePaths);

                    // Add wheel category-station combo to a station list
                    StationWheelPair pair = new StationWheelPair(radioWheel, stationCat, station);
                    StationWheelPair.List.Add(pair);

                    // Get description
                    pair.LoadStationINI(Path.Combine(stationDir, "station.ini"));

                    radioWheel.OnCategoryChange += (sender, selectedCategory, selectedItem, wheelJustOpened) =>
                    {
                        // HandleRadioWheelToggle() handles what happens when the wheel is opened.
                        // So we will only use this anonymous method for when the station is actually changed.
                        //if (wheelJustOpened) return;

                        if (selectedCategory == stationCat)
                        {
                            // If there is no input for a short amount of time, set the selected station as next to play
                            ActionQueued = ActionOptions.PlayQueued;

                            RadioStation.NextQueuedStation = station;

                            // If radio is still being decided, add delay before station changes
                            SetActionDelay(Config.WheelActionDelay);

                            lastRadioWasCustom = true;
                        }
                    };

                    radioWheel.OnItemChange += (sender, selectedCategory, selectedItem, wheelJustOpened, goTo) =>
                    {
                        if (wheelJustOpened) return;
                        
                        if (radioWheel.Visible && radioWheel == WheelVars.CurrentRadioWheel && WheelVars.NextQueuedWheel == null)
                        {
                            if (goTo == GoTo.Next)
                            {
                                radioWheel.Visible = false;
                                WheelVars.NextQueuedWheel = WheelVars.RadioWheels.GetNext(radioWheel);
                            }
                            else if (goTo == GoTo.Prev)
                            {
                                radioWheel.Visible = false;
                                WheelVars.NextQueuedWheel = WheelVars.RadioWheels.GetPrevious(radioWheel);
                            }
                        }
                    };
                }
                
                if (populatedStationCount > 0)
                {
                    WheelVars.RadioWheels.Add(radioWheel);
                    radioWheel.Origin = new Vector2(0.5f, 0.45f);
                    radioWheel.SetCategoryBackgroundIcons(iconBgPath, Config.IconBG, Config.IconBgSizeMultiple, iconhighlightPath, Config.IconHL, Config.IconHlSizeMultiple);
                    radioWheel.CalculateCategoryPlacement();
                }

                Logger.Log(@"/\/\/\/\/\/\/\/\/\/\/\/\/\/\");
            }
            
            if (WheelVars.RadioWheels.Count > 0)
            {
                WheelVars.CurrentRadioWheel = WheelVars.RadioWheels[0];
            }
            else
            {
                UI.ShowSubtitle("No music found in Custom Radio Stations. " +
                    "Please add music and reload script with the INS key.");
                Logger.Log("ERROR: No music found in any station directory. Aborting script...");
                Tick -= OnTick;
            }
        }

        void SetupEvents()
        {
            GeneralEvents.OnPlayerEnteredVehicle += (veh) =>
            {
                bool vehWasEngineRunning = veh.EngineRunning;

                // Make vanilla radio silent
                RadioNativeFunctions.VanillaRadioFadedOut(true);

                DateTime enteredTime = DateTime.Now;

                while (!veh.EngineRunning || DateTime.Now > enteredTime.AddSeconds(10))
                {
                    //UI.ShowSubtitle((enteredTime.AddSeconds(10) - DateTime.Now).TotalMilliseconds.ToString());
                    vehWasEngineRunning = false;
                    Yield();
                }

                // In case the timeout above caused the loop to break,
                // We will not continue because the vehicle is dead.
                if (!veh.EngineRunning) return;

                if (UsedVehiclesManager.IsUsedVehicle(veh))
                {
                    canResumeCustomStation = false;

                    if (UsedVehiclesManager.GetVehicleStationInfo(veh) == null)
                    {
                        // Make vanilla radio audible
                        RadioNativeFunctions.VanillaRadioFadedOut(false);

                        canResumeCustomStation = false;
                        lastRadioWasCustom = false;
                        return;
                    }

                    ActionQueued = ActionOptions.PlayQueued;

                    UsedVehiclesManager.SetLastStationNow(veh);

                    SetActionDelay(Config.WheelActionDelay + 300);

                    lastRadioWasCustom = true;

                    //UI.ShowSubtitle("Started playback");
                }
                else
                {
                    // If the engine was running, don't mess with it.
                    // Since I can't figure out how to see if a vehicle
                    // was emitting a station, I'll just not mess with it.
                    if (vehWasEngineRunning)
                    {
                        //UI.ShowSubtitle("RADIO IS ENABLED: " + RadioNativeFunctions.GET_PLAYER_RADIO_STATION_INDEX().ToString());

                        // Make vanilla radio audible
                        RadioNativeFunctions.VanillaRadioFadedOut(false);

                        canResumeCustomStation = false;
                        lastRadioWasCustom = false;
                        return;
                    }

                    int chooseRandom = RadioStation.random.Next(10);
                    //UI.ShowSubtitle("RANDOM: " + chooseRandom.ToString());
                    // 70% chance to play a custom station.
                    if (chooseRandom >= 3)
                    {
                        ActionQueued = ActionOptions.PlayQueued;

                        // Set the queued radio station randomly, chosen from stationPairList.
                        chooseRandom = RadioStation.random.Next(StationWheelPair.List.Count);
                        
                        UsedVehiclesManager.UpdateVehicleWithStationInfo(veh,
                            StationWheelPair.List[chooseRandom]);

                        UsedVehiclesManager.SetLastStationNow(veh);

                        SetActionDelay(Config.WheelActionDelay + 300);

                        canResumeCustomStation = false;
                        lastRadioWasCustom = true;
                    }
                    else
                    {
                        UsedVehiclesManager.UpdateVehicleWithStationInfo(veh, null);

                        // Make vanilla radio audible
                        RadioNativeFunctions.VanillaRadioFadedOut(false);

                        canResumeCustomStation = false;
                        lastRadioWasCustom = false;
                    }
                }
            };

            GeneralEvents.OnPlayerExitedVehicle += (veh) =>
            {
                if (veh == null) return;

                /*if (!IsMobileRadioEnabled())
                {
                    lastRadioWasCustom = IsCurrentCustomStationPlaying() ? true : false;
                }*/

                // Make vanilla radio audible
                RadioNativeFunctions.VanillaRadioFadedOut(false);

                UsedVehiclesManager.UpdateVehicleWithStationInfo(veh,
                    lastRadioWasCustom ? StationWheelPair.List.Find(x => x.Category == WheelVars.CurrentRadioWheel.SelectedCategory)
                    : null);
            };

            /*GeneralEvents.OnPlayerVehicleEngineTurnedOn += (veh) =>
            {
                if (IsCurrentCustomStationPlaying()) return;

                if (lastRadioWasCustom && canResumeCustomStation)
                {
                    ActionQueued = ActionOptions.PlayQueued;

                    // Set the queued radio station based on the current category selected, using stationPairList.
                    RadioStation.NextQueuedStation = StationWheelPair.List.Find(x => x.Category == WheelVars.CurrentRadioWheel.SelectedCategory).Station;

                    SetActionDelay(Config.WheelActionDelay);

                    canResumeCustomStation = false;
                }
            };*/
        }
        
        void OnTick(object sender, EventArgs e)
        {
            if (!loaded)
            {
                if (!Game.Player.CanControlCharacter || Game.IsLoading) return;

                if (loadDelayTimer == null) loadDelayTimer = DateTime.Now.AddMilliseconds(Config.LoadStartDelay);

                Decorators.DEntity = Game.Player.Character;

                if (loadDelayTimer < DateTime.Now || Decorators.ScriptHasLoadedOnce)
                {
                    if (Config.DisplayHelpText)
                        UI.ShowSubtitle("Loading Custom Radios...");

                    SetupRadio();
                    SetupEvents();

                    // Allow playing MP audio sounds and scenes
                    Function.Call(Hash.SET_AUDIO_FLAG, "LoadMPData", true);

                    RadioNativeFunctions.DashboardScaleform = new ScaleformHelper.Scaleform("dashboard", true);

                    if (!Decorators.ScriptHasLoadedOnce) { Decorators.Init(Game.Player.Character); }

                    loaded = true;

                    if (Config.DisplayHelpText)
                        UI.ShowSubtitle("Custom Radios Loaded");

                    if (Config.CustomWheelAsDefault && WheelVars.RadioWheels.Count > 0)
                    {
                        lastRadioWasCustom = true;
                        canResumeCustomStation = true;
                    }
                }

                return; // Return if loaded is still not true
            }

            if (WheelVars.RadioWheels.Count == 0) return;

            if (GTAFunction.HasCheatStringJustBeenEntered("radio_reload"))
            {
                Config.LoadINI();
                Config.UpdateWheelsVisuals();
                Config.ReloadStationINIs();
                Config.RescanForTracklists();
                UI.ShowSubtitle("Custom Radio INIs reloaded:\n- settings.ini\n- station.ini files\n- Scanned for tracklists");
                Wait(150);
            }

            if (VanillaOrCustomRadioWheelIsVisible())
            {
                if (GTAFunction.UsingGamepad() && Game.IsControlJustPressed(2, Config.GP_Toggle))
                {
                    HandleRadioWheelToggle();
                }
                
                if (lastRadioWasCustom)
                {
                    WheelVars.CurrentRadioWheel.Visible = true;
                }

                lastPlayedOnFoot = Game.Player.Character.IsInVehicle() ? false : true;
            }

            if (Game.IsControlJustReleased(2, GTA.Control.VehicleRadioWheel))
            {
                if (WheelVars.CurrentRadioWheel.Visible)
                {
                    WheelVars.CurrentRadioWheel.Visible = false;
                }
            }

            Wheel.ControlTransitions(Config.EnableWheelSlowmotion);
            WheelVars.RadioWheels.ForEach(w => w.ProcessSelectorWheel());
            HandleRadioWheelQueue();
            SoundFile.ManageSoundEngine();
            RadioStation.ManageStations();
            HandleRadioWheelExtraControls();
            HandleQueuedStationActions();
            HandleEnterExitVehicles();
            UpdateDashboardInfo();
            HandleGamePause();
            GeneralEvents.Update();
        }
        
        public void HandleRadioWheelQueue()
        {
            if (WheelVars.NextQueuedWheel != null)
            {
                WheelVars.CurrentRadioWheel = WheelVars.NextQueuedWheel;
                WheelVars.CurrentRadioWheel.Visible = true;
                WheelVars.NextQueuedWheel = null;

                /*ActionQueued = ActionOptions.PlayQueued;

                // Set the queued radio station based on the current category selected, using stationPairList.
                RadioStation.NextQueuedStation = StationWheelPair.List.Find(x => x.Category == currentRadioWheel.SelectedCategory).Station;

                SetActionDelay(Config.WheelActionDelay);*/
            }
        }

        GTA.Control ControlSkipTrack;
        GTA.Control ControlVolumeUp;
        GTA.Control ControlVolumeDown;
        GTA.Control ControlNextWheel;
        GTA.Control ControlPrevWheel;
        public void HandleRadioWheelExtraControls()
        {
            if (WheelVars.CurrentRadioWheel.Visible)
            {
                if (RadioStation.CurrentPlaying != null)
                {
                    ControlSkipTrack = GTAFunction.UsingGamepad() ? Config.GP_Skip_Track : Config.KB_Skip_Track;
                    ControlVolumeUp = GTAFunction.UsingGamepad() ? Config.GP_Volume_Up : Config.KB_Volume_Up;
                    ControlVolumeDown = GTAFunction.UsingGamepad() ? Config.GP_Volume_Down : Config.KB_Volume_Down;
                    ControlNextWheel = GTAFunction.UsingGamepad() ? GTA.Control.VehicleAccelerate : GTA.Control.WeaponWheelPrev;
                    ControlPrevWheel = GTAFunction.UsingGamepad() ? GTA.Control.VehicleBrake : GTA.Control.WeaponWheelNext;
                    
                    if (Config.DisplayHelpText)
                    {
                        GTAFunction.DisplayHelpTextThisFrame(
                            GTAFunction.InputString(ControlSkipTrack) +
                            " : Skip Track\n" +
                            GTAFunction.InputString(ControlVolumeUp) + " " +
                            GTAFunction.InputString(ControlVolumeDown) +
                            " : Volume: " +
                            Math.Round(SoundFile.SoundEngine.SoundVolume * 100, 0) + "%\n" +
                            GTAFunction.InputString(ControlNextWheel) + " " +
                            GTAFunction.InputString(ControlPrevWheel) +
                            " : Next / Prev Wheel\n", false, false);
                    }
                    
                    if (Game.IsControlJustPressed(2, ControlSkipTrack))
                    {
                        RadioStation.CurrentPlaying.PlayNextSong();
                    }
                    else if (Game.IsControlJustPressed(2, ControlVolumeUp))
                    {
                        SoundFile.StepVolume(0.05f, 2);
                    }
                    else if (Game.IsControlJustPressed(2, ControlVolumeDown))
                    {
                        SoundFile.StepVolume(-0.05f, 2);
                    }
                }
            }
            if (RadioStation.CurrentPlaying != null)
            {
                Game.DisableControlThisFrame(2, GTA.Control.VehicleNextRadio);
                Game.DisableControlThisFrame(2, GTA.Control.VehicleNextRadioTrack);
                Game.DisableControlThisFrame(2, GTA.Control.VehiclePrevRadio);
                Game.DisableControlThisFrame(2, GTA.Control.VehiclePrevRadioTrack);

                RadioNativeFunctions.SetVanillaRadioOff();
            }
        }
        
        public void HandleEnterExitVehicles()
        {
            if (!lastPlayedOnFoot && !RadioNativeFunctions._IS_PLAYER_VEHICLE_RADIO_ENABLED())
            {
                if (RadioStation.CurrentPlaying != null)
                {
                    RadioStation.CurrentPlaying.Stop();
                    RadioStation.CurrentPlaying = null;

                    if (WheelVars.CurrentRadioWheel != null)
                    {
                        WheelVars.CurrentRadioWheel.Visible = false;
                    }

                    // Make vanilla radio audible
                    RadioNativeFunctions.VanillaRadioFadedOut(false);

                    canResumeCustomStation = true;
                }
            }

            /*if (Game.Player.Character.CurrentVehicle != null
                && Game.Player.Character.CurrentVehicle.EngineRunning
                && lastRadioWasCustom
                && canResumeCustomStation
                && !IsCurrentCustomStationPlaying())
            {
                ActionQueued = ActionOptions.PlayQueued;

                // Set the queued radio station based on the current category selected, using stationPairList.
                RadioStation.NextQueuedStation = StationWheelPair.List.Find(x => x.Category == WheelVars.CurrentRadioWheel.SelectedCategory).Station;

                SetActionDelay(Config.WheelActionDelay);

                canResumeCustomStation = false;
            }*/
        }

        bool doUnpauseNextFrame;
        public void HandleGamePause()
        {
            if (Game.IsControlJustPressed(2, GTAFunction.UsingGamepad() ? GTA.Control.FrontendPause : GTA.Control.FrontendPauseAlternate))
            {
                if (RadioStation.CurrentPlaying != null)
                {
                    RadioStation.CurrentPlaying.CurrentSoundIsPaused = true;
                    doUnpauseNextFrame = true;
                    Wait(500);
                }
            }

            if (doUnpauseNextFrame)
            {
                RadioStation.CurrentPlaying.CurrentSoundIsPaused = false;
                doUnpauseNextFrame = false;
            }
        }

        void HandleQueuedStationActions()
        {
            if (CanDoQueuedAction())
            {
                if (ActionQueued == ActionOptions.StopCurrent)
                {
                    if (RadioStation.CurrentPlaying != null)
                    {
                        // Turn off custom radio
                        RadioStation.CurrentPlaying.Stop();

                        // Enable last played vanilla radio
                        RadioNativeFunctions.SET_RADIO_TO_STATION_INDEX(lastVanillaStationPlayed);

                        // Make vanilla radio audible
                        RadioNativeFunctions.VanillaRadioFadedOut(false);


                        RadioStation.CurrentPlaying = null;
                        lastRadioWasCustom = false;
                    }
                }
                else if (ActionQueued == ActionOptions.PlayQueued)
                {
                    if (RadioStation.NextQueuedStation != null
                        && RadioStation.NextQueuedStation != RadioStation.CurrentPlaying)
                    {
                        // Enable custom radio
                        if (RadioStation.CurrentPlaying != null)
                            RadioStation.CurrentPlaying.Stop();

                        RadioStation.CurrentPlaying = RadioStation.NextQueuedStation;
                        RadioStation.CurrentPlaying.Play();
                        RadioStation.NextQueuedStation = null;

                        // Set vanilla radio to Off but save what station was playing beforehand
                        lastVanillaStationPlayed = RadioNativeFunctions.GET_PLAYER_RADIO_STATION_INDEX();
                        RadioNativeFunctions.SetVanillaRadioOff();

                        // Make vanilla radio audible
                        RadioNativeFunctions.VanillaRadioFadedOut(false);

                        lastRadioWasCustom = true;
                    }
                }

                // Set to DoNothing since queued action is completed
                ActionQueued = ActionOptions.DoNothing;
            }
        }

        bool CanDoQueuedAction()
        {
            if (inputTimer == null) return false;

            if (inputTimer < DateTime.Now)
            {
                inputTimer = null;
                return true;
            }
            return false;
        }

        void SetActionDelay(int ms = 500)
        {
            inputTimer = DateTime.Now.AddMilliseconds(ms);
        }

        void HandleRadioWheelToggle()
        {
            if (WheelVars.CurrentRadioWheel.Visible)
            {
                ActionQueued = ActionOptions.StopCurrent;

                RadioStation.NextQueuedStation = null;

                SetActionDelay(Config.WheelActionDelay);

                // Switch to vanilla radio
                WheelVars.CurrentRadioWheel.Visible = false;
                lastRadioWasCustom = false;
            }
            else
            {
                // Switch to custom radio
                WheelVars.CurrentRadioWheel.Visible = true;
                lastRadioWasCustom = true;
            }
        }

        void UpdateDashboardInfo()
        {
            if (RadioStation.CurrentPlaying != null)
            {
                RadioStation.CurrentPlaying.UpdateDashboardInfo();
            }
        }

        bool VanillaOrCustomRadioWheelIsVisible()
        {
            //return /*_IS_PLAYER_VEHICLE_RADIO_ENABLED() &&*/ Game.IsControlPressed(2, GTA.Control.VehicleRadioWheel) && Game.Player.CanControlCharacter;
            if (Game.IsControlPressed(2, GTA.Control.VehicleRadioWheel) && Game.Player.CanControlCharacter)
            {
                if (Game.Player.Character.IsInVehicle() && RadioNativeFunctions._IS_PLAYER_VEHICLE_RADIO_ENABLED())
                {
                    return true;
                }
                else if (Game.Player.Character.IsOnFoot)
                {
                    if (RadioNativeFunctions.IsRadioHudComponentVisible() || WheelVars.CurrentRadioWheel.Visible || IsCurrentCustomStationPlaying())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool IsMobileRadioEnabled()
        {
            return RadioNativeFunctions.IS_MOBILE_PHONE_RADIO_ACTIVE() || IsCurrentCustomStationPlaying();
        }

        bool IsCurrentCustomStationPlaying()
        {
            return RadioStation.CurrentPlaying != null && RadioStation.CurrentPlaying.IsPlaying && !RadioStation.CurrentPlaying.CurrentSoundIsPaused;
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (VanillaOrCustomRadioWheelIsVisible() && e.KeyCode == Config.KB_Toggle)
            {
                if (!loaded)
                {
                    UI.ShowSubtitle("Custom Radio not loaded yet, please try again later!");
                    return;
                }

                HandleRadioWheelToggle();
            }
        }
    }
}