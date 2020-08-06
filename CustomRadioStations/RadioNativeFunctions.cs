using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomRadioStations
{
    public static class RadioNativeFunctions
    {

        public static ScaleformHelper.Scaleform DashboardScaleform;

        public static void UpdateRadioScaleform(string station, string artist, string track)
        {
            try
            {
                // Call function.
                DashboardScaleform.CallFunction("SET_RADIO",
                        "", station,
                        artist, track);
            }
            catch (Exception exception)
            {
                UI.Notify(exception.ToString());
            }
        }

        // Only works for vehicle radio.
        public static bool _IS_PLAYER_VEHICLE_RADIO_ENABLED()
        {
            return Function.Call<bool>((Hash)0x5F43D83FD6738741);
        }

        public static bool IsRadioHudComponentVisible()
        {
            return Function.Call<bool>(Hash.IS_HUD_COMPONENT_ACTIVE, 16);
        }

        public static int GET_PLAYER_RADIO_STATION_INDEX()
        {
            return Function.Call<int>(Hash.GET_PLAYER_RADIO_STATION_INDEX);
        }

        public static void SET_RADIO_TO_STATION_INDEX(int index)
        {
            if (index == 255)
            {
                SetVanillaRadioOff();
            }
            else
            {
                Function.Call(Hash.SET_RADIO_TO_STATION_INDEX, index);
            }
        }

        public static string GET_RADIO_STATION_NAME(int index)
        {
            return Function.Call<string>(Hash.GET_RADIO_STATION_NAME, index);
        }

        public static void SET_RADIO_TO_STATION_NAME(string name)
        {
            Function.Call(Hash.SET_RADIO_TO_STATION_NAME, name);
        }

        public static string GetRadioStationProperName(string name)
        {
            return _GET_LABEL_TEXT(name);
        }

        public static string GetRadioStationProperName(int index)
        {
            return _GET_LABEL_TEXT(GET_RADIO_STATION_NAME(index));
        }

        public static string GetCurrentPlayingArtist()
        {
            int trackId = Function.Call<int>(Hash.GET_AUDIBLE_MUSIC_TRACK_TEXT_ID);
            return _GET_LABEL_TEXT(trackId.ToString() + "A");
        }

        public static string GetCurrentPlayingSongname()
        {
            int trackId = Function.Call<int>(Hash.GET_AUDIBLE_MUSIC_TRACK_TEXT_ID);
            return _GET_LABEL_TEXT(trackId.ToString() + "S");
        }

        /// <summary>
        /// Hides station from wheel and stops playing it if it was playing
        /// </summary>
        /// <param name="stationName">Name returned by GET_RADIO_STATION_NAME. Not the fancy name.</param>
        /// <param name="hide">true = hide or remove from wheel</param>
        public static void _LOCK_RADIO_STATION(string stationName, bool hide)
        {
            if ((int)Game.Version >= (int)GameVersion.VER_1_0_1493_0_STEAM)
                Function.Call((Hash)0x477D9DB48F889591, stationName, hide); // _LOCK_RADIO_STATION
        }

        /// <summary>
        /// Get the number of stations enabled in the radio wheel. 
        /// Call BEFORE using _LOCK_RADIO_STATION if you want to get the default number of stations in the wheel.
        /// </summary>
        /// <returns></returns>
        public static int _MAX_RADIO_STATION_INDEX()
        {
            return Function.Call<int>((Hash)0xF1620ECB50E01DE7); // _MAX_RADIO_STATION_INDEX
        }

        public static void SetVanillaRadioOff()
        {
            if (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.EngineRunning)
            {
                SetVehicleRadioStationOff();
            }
            if (IS_MOBILE_PHONE_RADIO_ACTIVE()) // Doesn't return true if mobile radio is enabled but set to OFF station.
                SET_MOBILE_PHONE_RADIO_STATE(false);
        }

        public static void SetVehicleRadioStationOff()
        {
            Function.Call(Hash.SET_VEH_RADIO_STATION, Game.Player.Character.CurrentVehicle, "OFF");
        }

        public static void VanillaRadioFadedOut(bool fadeOut)
        {
            string scene = "MP_JOB_CHANGE_RADIO_MUTE";

            if (!fadeOut)
            {
                Function.Call(Hash.SET_AUDIO_SCENE_VARIABLE, scene, "apply", 0f);
                return;
            }

            if (fadeOut)
            {
                Function.Call(Hash.START_AUDIO_SCENE, scene);
                Function.Call(Hash.SET_AUDIO_SCENE_VARIABLE, scene, "apply", 1f);
            }
        }

        public static bool IS_MOBILE_PHONE_RADIO_ACTIVE()
        {
            return Function.Call<bool>(Hash.IS_MOBILE_PHONE_RADIO_ACTIVE);
        }

        public static void SET_MOBILE_PHONE_RADIO_STATE(bool on)
        {
            Function.Call(Hash.SET_MOBILE_PHONE_RADIO_STATE, on);
        }

        static string _GET_LABEL_TEXT(string text)
        {
            return Function.Call<string>((Hash)0x7B5280EBA9840C72, text); // _GET_LABEL_TEXT
        }
    }
}
