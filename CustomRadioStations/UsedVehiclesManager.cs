using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;

namespace CustomRadioStations
{
    static class UsedVehiclesManager
    {
        public static List<UsedVehicle> Vehicles = new List<UsedVehicle>();

        public static bool IsUsedVehicle(Vehicle vehicle)
        {
            return Vehicles.Exists(x => x.Handle == vehicle.Handle);
        }

        private static UsedVehicle GetFromList(Vehicle vehicle)
        {
            return Vehicles.FirstOrDefault(x => x.Handle == vehicle.Handle);
        }

        private static void AddVehicle(Vehicle vehicle, StationWheelPair pair)
        {
            if (Vehicles.Count == 20)
                Vehicles.RemoveAt(0);

            Vehicles.Add(new UsedVehicle(vehicle, pair));
        }

        public static void UpdateVehicleWithStationInfo(Vehicle vehicle, StationWheelPair pair)
        {
            if (IsUsedVehicle(vehicle))
            {
                var item = GetFromList(vehicle);

                if (pair == null)
                {
                    //Vehicles.Remove(item);
                    item.radioInfo = null;
                }
                else
                {
                    item.radioInfo = pair;
                }
            }
            else
            {
                AddVehicle(vehicle, pair);
            }
        }

        public static void SetLastStationNow(Vehicle vehicle)
        {
            if (IsUsedVehicle(vehicle))
            {
                var item = GetFromList(vehicle);

                if (item.radioInfo == null) return;

                //StationWheelPair pair = StationWheelPair.List.Find(x => x.Station == item.radioInfo.Station);
                StationWheelPair pair = StationWheelPair.List.Find(x => x.Equals(item.radioInfo));

                WheelVars.CurrentRadioWheel = pair.Wheel;
                WheelVars.CurrentRadioWheel.SelectedCategory = pair.Category;
                RadioStation.NextQueuedStation = pair.Station;
            }
        }

        public static StationWheelPair GetVehicleStationInfo(Vehicle vehicle)
        {
            if (IsUsedVehicle(vehicle))
            {
                return GetFromList(vehicle).radioInfo;
            }
            else
            {
                return null;
            }
        }
    }

    class UsedVehicle
    {
        public int Handle = 0;
        public StationWheelPair radioInfo = null;

        public UsedVehicle(Vehicle vehicle, StationWheelPair pair)
        {
            Handle = vehicle.Handle;
            radioInfo = pair;
        }
    }
}
