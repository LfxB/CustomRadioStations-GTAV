using System;
using System.Collections.Generic;
using System.Linq;
using GTA;

namespace CustomRadioStations
{
    class StationWheelPair
    {
        public static List<StationWheelPair> List = new List<StationWheelPair>();

        public SelectorWheel.Wheel Wheel;
        public SelectorWheel.WheelCategory Category;
        public RadioStation Station;

        public string IniPath;

        public StationWheelPair(SelectorWheel.Wheel wheel, SelectorWheel.WheelCategory category, RadioStation station)
        {
            Wheel = wheel;
            Category = category;
            Station = station;
        }
        
        public void LoadStationINI(string path)
        {
            IniPath = path;

            Config.ForceDecimal();

            ScriptSettings config = ScriptSettings.Load(path);

            var description = config.GetValue<string>("GENERAL", "DESCRIPTION", null);
            Category.Description = description.Replace("\\n", "\r\n");
        }

        public void RescanStationTracklists()
        {
            Station.RescanSoundsTracklists();
        }
    }
}
