using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GTA;
using GTA.Native;

namespace EventHelper
{
    public delegate void PlayerEnteredVehicle(Vehicle vehicle);
    public delegate void PlayerExitedVehicle(Vehicle vehicle);
    public delegate void PlayerVehicleEngineTurnedOn(Vehicle vehicle);

    public static class GeneralEvents
    {
        private static Ped Player;
        private static Vehicle PlayerVehicle;

        #region OnPlayerEnteredVehicle and OnPlayerExitedVehicle

        private static int enteredVehicleOldHandle = -1;
        private static int enteredVehicleNewHandle = -1;
        public static event PlayerEnteredVehicle OnPlayerEnteredVehicle;
        public static event PlayerExitedVehicle OnPlayerExitedVehicle;

        public static void PlayerEnteredVehicle(Vehicle vehicle)
        {
            OnPlayerEnteredVehicle?.Invoke(vehicle);
        }

        public static void PlayerExitedVehicle(Vehicle vehicle)
        {
            OnPlayerExitedVehicle?.Invoke(vehicle);
        }

        private static void UpdatePlayerEnteredExitedVehicle()
        {
            if (Player.IsInVehicle())
            {
                enteredVehicleNewHandle = Player.CurrentVehicle.Handle;

                if (enteredVehicleNewHandle != enteredVehicleOldHandle)
                {
                    if (enteredVehicleOldHandle != -1) // Handles warping between vehicles
                        PlayerExitedVehicle(Player.LastVehicle);

                    PlayerEnteredVehicle(Player.CurrentVehicle);

                    enteredVehicleOldHandle = enteredVehicleNewHandle;
                }
            }
            else
            {
                enteredVehicleNewHandle = -1;
                if (enteredVehicleNewHandle != enteredVehicleOldHandle)
                {
                    PlayerExitedVehicle(Player.LastVehicle);
                    enteredVehicleOldHandle = -1;
                }
            }
        }

        #endregion

        #region OnPlayerVehicleEngineTurnedOn

        private static bool engineTurnedOnToggle = false;
        public static event PlayerVehicleEngineTurnedOn OnPlayerVehicleEngineTurnedOn;

        public static void PlayerVehicleEngineTurnedOn(Vehicle vehicle)
        {
            OnPlayerVehicleEngineTurnedOn?.Invoke(vehicle);
        }

        private static void UpdatePlayerVehicleEngineTurnedOn()
        {
            if (PlayerVehicle == null) return;

            if (PlayerVehicle.EngineRunning)
            {
                if (!engineTurnedOnToggle)
                {
                    PlayerVehicleEngineTurnedOn(PlayerVehicle);
                    engineTurnedOnToggle = true;
                }
            }
            else
            {
                engineTurnedOnToggle = false;
            }
        }

        #endregion

        public static void Update()
        {
            Player = Game.Player.Character;
            PlayerVehicle = Player.CurrentVehicle;

            UpdatePlayerEnteredExitedVehicle();
            UpdatePlayerVehicleEngineTurnedOn();
        }
    }
}
