using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GTA;
using GTA.Native;
using GTA.Math;
using SelectorWheel;

namespace CustomRadioStations
{
    class RadioStation
    {

        public static Random random = new Random();

        public string Name { get; set; }

        /// <summary>
        /// In milliseconds
        /// </summary>
        public uint TotalLength { get; private set; } = 0;

        WheelCategory corrWheelCat;
        List<SoundFileTimePair> SoundFileTimePairs;
        SoundFile CurrentSound;

        bool hasPlayedOnce;
        uint stoppedPositionStation;
        DateTime lastPlayedTime;

        int lastPlayedSoundIndex;
        uint stoppedPositionSound;
        bool allSoundsPlayedOnce;

        public RadioStation(WheelCategory correspondingWheelCategory, IEnumerable<string> songFilesPaths)
        {
            corrWheelCat = correspondingWheelCategory;
            Name = corrWheelCat.Name;
            SoundFileTimePairs = new List<SoundFileTimePair>();
            List<Tuple<string, string>> commercials = new List<Tuple<string, string>>();

            foreach (var path in songFilesPaths)
            {
                try
                {
                    //Logger.Log(path.Substring(path.LastIndexOf('\\') + 1));
                    // If file is a shortcut, get the real path first
                    if (Path.GetExtension(path) == ".lnk")
                    {
                        string str = GeneralHelper.GetShortcutTargetFile(path);

                        if (str == string.Empty) continue;

                        if (Path.GetFileNameWithoutExtension(str).Contains("[Commercial]")
                            || Path.GetFileNameWithoutExtension(path).Contains("[Commercial]"))
                        {
                            commercials.Add(Tuple.Create(str, path));
                        }
                        else
                        {
                            SoundFileTimePairs.Add(new SoundFileTimePair(new SoundFile(str, path), 0));
                        }
                    }
                    else
                    {
                        if (Path.GetFileNameWithoutExtension(path).Contains("[Commercial]"))
                        {
                            commercials.Add(Tuple.Create(path, string.Empty));
                        }
                        else
                        {
                            SoundFileTimePairs.Add(new SoundFileTimePair(new SoundFile(path), 0));
                        }
                    }

                    Config.LoadTick();
                }
                catch (Exception ex)
                {
                    Logger.Log("ERROR : " + path.Substring(path.LastIndexOf('\\') + 1) + " : " + ex.Message);
                    Script.Wait(500);
                }
            }

            ShuffleList(); // Do this based on an ini setting? Yes. TODO
            InsertCommercials(commercials);

            // Calculate lengths and stuff for the station
            // Replaced by UpdateRadioLengthWithCurrentSound()
            //foreach (var s in SoundFileTimePairs)
            //{
            //    s.StartTime = TotalLength;
            //    TotalLength += s.SoundFile.Length;
            //}
        }

        /// <summary>
        /// Must be called after CurrentSound.PlaySound();
        /// </summary>
        private void UpdateRadioLengthWithCurrentSound()
        {
            var s = SoundFileTimePairs.Find(x => x.SoundFile == CurrentSound);
            if (!CurrentSound.LengthAdded)
            {
                s.StartTime = TotalLength;
                TotalLength += CurrentSound.Length;
                CurrentSound.LengthAdded = true;
            }
        }

        DateTime trackUpdateTimer = DateTime.Now;
        public void Update()
        {
            if (CurrentSound == null || CurrentSound.Sound == null) return;

            //UI.ShowSubtitle((lastPlayedSoundIndex + 1) + " / " + SoundFileTimePairs.Count);
            
            if (CurrentSound.HasTrackList && trackUpdateTimer < DateTime.Now)
            {
                UpdateWheelInfo();
                UpdateTrackUpdateTimer();
            }
            
            if (CurrentSound.IsFinishedPlaying())
            {
                PlayNextSound();
            }
        }

        private void ShuffleList()
        {
            /*int n = SoundFileTimePairs.Count;

            for (int i = SoundFileTimePairs.Count - 1; i > 1; i--)
            {
                int rnd = random.Next(i + 1);

                SoundFileTimePair value = SoundFileTimePairs[rnd];
                SoundFileTimePairs[rnd] = SoundFileTimePairs[i];
                SoundFileTimePairs[i] = value;
            }*/

            var count = SoundFileTimePairs.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = random.Next(i, count);
                var tmp = SoundFileTimePairs[i];
                SoundFileTimePairs[i] = SoundFileTimePairs[r];
                SoundFileTimePairs[r] = tmp;
            }
        }

        int lastCommIndex = 0;
        private void InsertCommercials(List<Tuple<string, string>> commercials)
        {
            if (commercials.Count > 0)
            {
                int numToInsert = SoundFileTimePairs.Count / 3;
                int lastIndex = -1;
                SoundFileTimePairs.Capacity += numToInsert;

                for (int i = 0; i < numToInsert; i++)
                {
                    lastIndex += 4;

                    lastCommIndex = GetNewRandom(lastCommIndex, commercials.Count);
                    var commercial = commercials[lastCommIndex];
                    var pair = new SoundFileTimePair(
                            string.IsNullOrEmpty(commercial.Item2) ? new SoundFile(commercial.Item1) :
                            new SoundFile(commercial.Item1, commercial.Item2), 0);

                    if (lastIndex >= SoundFileTimePairs.Count)
                    {
                        SoundFileTimePairs.Add(pair);
                    }
                    else
                    {
                        SoundFileTimePairs.Insert(lastIndex, pair);
                    }
                }
            }
        }

        private int GetNewRandom(int input, int max)
        {
            int rnd = random.Next(0, max);
            if (input == rnd && max != 1)
            {
                return GetNewRandom(input, max);
            }
            else
            {
                return rnd;
            }

        }

        internal void RescanSoundsTracklists()
        {
            foreach (var pair in SoundFileTimePairs)
            {
                var soundFile = pair.SoundFile;
                soundFile.HasTrackList = soundFile.TracklistExists(soundFile.FilePath);
            }
            UpdateWheelInfo();
            UpdateTrackUpdateTimer();
        }

        private void UpdateWheelInfo()
        {
            if (CurrentSound == null || CurrentSound.Sound == null) return;

            WheelCategoryItem radioWheelItem = corrWheelCat.ItemList[0];

            radioWheelItem.Name = Name + "\n" + CurrentSound.DisplayName;
        }

        public void UpdateDashboardInfo()
        {
            if (CurrentSound == null || CurrentSound.Sound == null) return;

            if (Game.Player.Character.IsInVehicle())
            {
                string[] info = CurrentSound.DisplayName.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                RadioNativeFunctions.UpdateRadioScaleform(Name, info[0], info[1]);
            }
        }

        private void UpdateTrackUpdateTimer()
        {
            trackUpdateTimer = DateTime.Now.AddMilliseconds(CurrentSound == null || CurrentSound.Sound == null ? 5000 : CurrentSound.TimeUntilNextTrack());
        }

        public void Play()
        {
            if (!hasPlayedOnce)
            {
                CurrentSound = SoundFileTimePairs[0].SoundFile;
                CurrentSound.PlaySound(false, false, true);
                CurrentSound.Sound.PlayPosition = CurrentSound.GetRandomPlayPosition();
                CurrentSound.Sound.Paused = false;
                UpdateRadioLengthWithCurrentSound();
                hasPlayedOnce = true;

                UpdateWheelInfo();
            }
            else
            {
                if (!allSoundsPlayedOnce &&
                    lastPlayedSoundIndex == SoundFileTimePairs.Count - 1)
                {
                    allSoundsPlayedOnce = true;
                }

                ResumeContinuity();
            }

            Function.Call(Hash.SET_AUDIO_FLAG, "DisableFlightMusic", true);
            Function.Call(Hash.SET_AUDIO_FLAG, "DisableWantedMusic", true);
        }

        private void ResumeContinuity()
        {
            uint elapsed = (uint)(DateTime.Now - lastPlayedTime).TotalMilliseconds;

            var lastPlayedSound = SoundFileTimePairs[lastPlayedSoundIndex].SoundFile;

            //UI.ShowSubtitle("allSoundsPlayed: " + allSoundsPlayedOnce +
            //    "\nelapsed ms: " + elapsed +
            //    "\nRemaining playtime: " + (lastPlayedSound.Length - stoppedPositionSound));

            if (allSoundsPlayedOnce)
            {
                uint newPlayPos = GetTimeFromPrevious(stoppedPositionStation, TotalLength, elapsed);
                var stPair = SoundFileTimePairs.LastOrDefault(s => newPlayPos >= s.StartTime);
                CurrentSound = stPair != default(SoundFileTimePair) ? stPair.SoundFile : SoundFileTimePairs[0].SoundFile;
                CurrentSound.PlaySound(true, false, true);
                UpdateRadioLengthWithCurrentSound();
                CurrentSound.Sound.PlayPosition = Math.Max(0, newPlayPos - stPair.StartTime);
                CurrentSoundIsPaused = false;
            }
            else if (elapsed < lastPlayedSound.Length - stoppedPositionSound)
            {
                CurrentSound = SoundFileTimePairs[lastPlayedSoundIndex].SoundFile;
                CurrentSound.PlaySound(true, false, true);
                CurrentSound.Sound.PlayPosition = Math.Min(CurrentSound.Length - 1, stoppedPositionSound + elapsed);
                CurrentSoundIsPaused = false;
                UpdateRadioLengthWithCurrentSound();
            }
            else
            {
                CurrentSound = lastPlayedSoundIndex != SoundFileTimePairs.Count - 1 ?
                    SoundFileTimePairs[lastPlayedSoundIndex + 1].SoundFile : SoundFileTimePairs[0].SoundFile;
                CurrentSound.PlaySound(true);
                UpdateRadioLengthWithCurrentSound();
            }

            UpdateWheelInfo();
            if (CurrentSound.HasTrackList)
                UpdateTrackUpdateTimer();
        }

        private uint GetTimeFromPrevious(uint previous, uint duration, uint elapsed)
        {
            uint adjElapsed = elapsed - (duration - previous);
            uint time = (previous + elapsed) > duration ? adjElapsed % duration : previous + elapsed;
            return time == duration ? 0 : time;
        }

        public void Stop()
        {
            if (CurrentSound == null || CurrentSound.Sound == null) return;

            // Get stopped position
            var stPair = SoundFileTimePairs.Find(s => s.SoundFile == CurrentSound);
            stoppedPositionStation = stPair.StartTime + CurrentSound.PlayPosition();
            lastPlayedTime = DateTime.Now;
            lastPlayedSoundIndex = SoundFileTimePairs.IndexOf(stPair);
            stoppedPositionSound = CurrentSound.PlayPosition();

            // Set name in wheel to just the station name
            WheelCategoryItem radioWheelItem = corrWheelCat.ItemList[0];
            radioWheelItem.Name = Name;

            //CurrentSound.StopSound();
            CurrentSoundIsPaused = true;
            CurrentSound = null;
            
            Function.Call(Hash.SET_AUDIO_FLAG, "DisableFlightMusic", false);
            Function.Call(Hash.SET_AUDIO_FLAG, "DisableWantedMusic", false);
        }
        
        private void PlayNextSound()
        {
            int currentSoundIndex = 0;
            if (CurrentSound != null)
            {
                currentSoundIndex = SoundFileTimePairs.IndexOf(SoundFileTimePairs.Find(s => s.SoundFile == CurrentSound));
                CurrentSound.StopSound();
            }

            // Set next in list
            currentSoundIndex = currentSoundIndex < SoundFileTimePairs.Count - 1 ? currentSoundIndex + 1 : 0;

            CurrentSound = SoundFileTimePairs[currentSoundIndex].SoundFile;
            CurrentSound.PlaySound(true);
            UpdateRadioLengthWithCurrentSound();
            UpdateWheelInfo();
            UpdateTrackUpdateTimer();
        }

        public void PlayNextSong()
        {
            if (CurrentSound != null)
            {
                // If CurrentSound has a tracklist but isn't at the last song, skip to the next song in the tracklist.
                if (CurrentSound.HasTrackList && CurrentSound.GetCurrentTrackIndex() < CurrentSound.Tracklist.Count - 1)
                {
                    CurrentSound.SkipToNextTrack();
                    UpdateWheelInfo();
                    UpdateTrackUpdateTimer();
                }
                // Else, skip to the next SoundFile.
                else
                {
                    PlayNextSound();
                }
            }
        }

        public bool IsPlaying
        {
            get { return CurrentSound != null && CurrentSound.IsPlaying(); }
        }

        public bool CurrentSoundIsPaused
        {
            get
            {
                return CurrentSound.IsPaused;
            }
            set
            {
                CurrentSound.IsPaused = value;
            }
        }

        public static RadioStation CurrentPlaying;
        public static RadioStation NextQueuedStation;

        public static void ManageStations()
        {
            if (CurrentPlaying == null) return;

            CurrentPlaying.Update();
        }
    }

    class SoundFileTimePair
    {
        public SoundFile SoundFile;
        public uint StartTime;

        public SoundFileTimePair(SoundFile sFile, uint time)
        {
            SoundFile = sFile;
            StartTime = time;
        }
    }
}
