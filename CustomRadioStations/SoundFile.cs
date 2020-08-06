using GTA;
using GTA.Math;
using IrrKlang;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CustomRadioStations
{
    class SoundFile
    {
        public ISound Sound;
        public ISoundSource Source;
        public ISoundEffectControl SoundEffect;

        public string FileName;
        public string FilePath;

        private string _displayName;
        public string DisplayName
        {
            get
            {
                if (HasTrackList)
                {
                    Track t = GetCurrentTrack();
                    if (t == null) return "";
                    return t.Artist + "\n" + t.Title;
                }
                else
                {
                    return _displayName;
                }
            }
        }

        /// <summary>
        /// Returns sound length in milliseconds
        /// </summary>
        public uint Length { get; private set; } = 0;
        /// <summary>
        /// If Length has been added to this sound's corresponding station
        /// </summary>
        public bool LengthAdded { get; set; }

        public bool HasTrackList;
        public List<Track> Tracklist { get; private set; }

        //public float MaximumDistance = 20f;
        //public float MinimumDistance = 1f;

        private static Random random = new Random();

        public SoundFile(string filepath)
        {
            FilePath = filepath;
            Source = SoundEngine.AddSoundSourceFromFile(filepath, StreamMode.AutoDetect, false);
            if (Source == null)
            {
                Source = SoundEngine.GetSoundSource(filepath);
            }
            FileName = Path.GetFileNameWithoutExtension(filepath);
            _displayName = DisplayNameFromFilename();
            //Length = Source.PlayLength;
            HasTrackList = TracklistExists(filepath);
        }

        public SoundFile(string filepath, string shortcutPath)
        {
            FilePath = shortcutPath;
            Source = SoundEngine.AddSoundSourceFromFile(filepath, StreamMode.AutoDetect, false);
            if (Source == null)
            {
                Source = SoundEngine.GetSoundSource(filepath);
            }
            FileName = Path.GetFileNameWithoutExtension(filepath);
            _displayName = DisplayNameFromFilename();
            //Length = Source.PlayLength;
            HasTrackList = TracklistExists(shortcutPath);
        }

        private string DisplayNameFromFilename()
        {
            string[] sections = FileName.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            string str = "";
            foreach (var s in sections)
            {
                str += s.Trim() + "\n";
            }
            return str;
        }

        internal bool TracklistExists(string filepath)
        {
            string iniPath = Path.ChangeExtension(filepath, ".ini");
            bool tracklistExists = File.Exists(iniPath);
            if (tracklistExists)
            {
                Tracklist = new List<Track>();

                var lines = File.ReadAllLines(iniPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Length > 0)
                        CreateTracklist(lines[i]);
                }
                return true;
            }
            return false;
        }

        private void CreateTracklist(string inputFromINI)
        {
            if (uint.TryParse(inputFromINI.Substring(0, 2), out uint h)
                && uint.TryParse(inputFromINI.Substring(3, 2), out uint m)
                && uint.TryParse(inputFromINI.Substring(6, 2), out uint s))
            {
                // Convert hours:minutes:seconds to milliseconds
                uint startTime = (h * 60 * 60 * 1000)
                    + (m * 60 * 1000)
                    + (s * 1000);

                // Skip all entries that have a timestamp past the length of the entire file
                //if (startTime > Length) return; // Gonna let this slide for now, working on getting length only when sound is loaded...

                if (inputFromINI.Length == 8)
                {
                    Tracklist.Add(new Track(startTime, "", ""));
                    return;
                }

                // Get remainder of string
                string artistTitle = inputFromINI.Substring(8);
                
                if (artistTitle.Contains("||"))
                {
                    // Separate it by the string ||
                    string[] splitTexts = artistTitle.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

                    // Set Artist and Title with beginning and ending whitespaces removed
                    var artist = splitTexts[0].Trim();
                    var title = splitTexts[1].Trim();

                    Tracklist.Add(new Track(startTime, artist, title));
                }
                else
                {
                    Tracklist.Add(new Track(startTime, "", artistTitle));
                }
            }
        }

        public Track GetCurrentTrack()
        {
            if (!HasTrackList) return null;

            //Track trk = Tracklist.FirstOrDefault(t => t.StartTime <= PlayPosition());
            Track trk = Tracklist.LastOrDefault(t => PlayPosition() >= t.StartTime);

            if (trk == default(Track))
            {
                return null;
            }
            else
            {
                return trk;
            }
        }

        public int GetCurrentTrackIndex()
        {
            if (!HasTrackList) return 0;

            return Tracklist.IndexOf(GetCurrentTrack());
        }

        public Track GetNextTrack()
        {
            if (!HasTrackList) return null;

            Track t = GetCurrentTrack();

            if (Tracklist.Last() == t)
            {
                return Tracklist.First();
            }
            else
            {
                return Tracklist[Tracklist.IndexOf(t) + 1];
            }
        }

        public void SkipToNextTrack()
        {
            if (!HasTrackList) return;

            Sound.PlayPosition = GetNextTrack().StartTime;
        }

        public uint TimeUntilNextTrack()
        {
            if (Sound == null || PlayPosition() == -1) return 0;
            if (!HasTrackList) return Length - PlayPosition();

            Track t = GetNextTrack();
            uint pPos = PlayPosition();

            return t.StartTime > pPos ? t.StartTime - pPos + 1 : Length - pPos;
        }

        public uint GetRandomPlayPosition(float percentMinBound = 0.2f, float percentMaxBound = 0.7f)
        {
            if (Sound == null) return 0;
            uint min = (uint)(percentMinBound * Sound.PlayLength);
            uint max = (uint)(percentMaxBound * Sound.PlayLength);

            // Get random uint within bounds
            var buffer = new byte[sizeof(uint)];
            new Random().NextBytes(buffer);
            uint result = BitConverter.ToUInt32(buffer, 0);

            result = (result % (max - min)) + min;
            return result;
        }

        public void PlaySound(/*Vector3 sourcePosition,*/bool resume, bool playLooped = false, bool playPaused = false, bool allowMultipleInstances = false, bool allowSoundEffects = false)
        {
            if (allowMultipleInstances || (!allowMultipleInstances && (Sound == null || Sound != null && (Sound.Finished || IsPaused))))
            {
                // Vector3D sourcePos = SoundHelperIK.Vector3ToVector3D(GameplayCamera.GetOffsetFromWorldCoords(sourcePosition));
                
                if (resume && IsPaused)
                {
                    IsPaused = false;
                    return;
                }

                // Sound = SoundEngine.Play3D(Source, sourcePos.X, sourcePos.Y, sourcePos.Z, playLooped, false, false);
                Sound = SoundEngine.Play2D(Source, playLooped, true, allowSoundEffects);

                if (Sound == null) return;

                // Attempt to avoid popping..
                Sound.Volume = 0f;

                if (!playPaused)
                {
                    Sound.Paused = false;
                }

                Sound.Volume = Source.DefaultVolume;

                if (Length == 0)
                {
                    //Length = Source.PlayLength;
                    Length = Sound.PlayLength;
                }
                if (allowSoundEffects)
                {
                    SoundEffect = Sound.SoundEffectControl;
                }
            }
        }

        public void StopSound()
        {
            if (Sound == null || Sound.Finished) return;
            Sound.Stop();
        }

        public bool IsPaused
        {
            get
            {
                if (Sound == null) return false;
                return Sound.Paused;
            }
            set
            {
                if (Sound == null || Sound.Finished) return;
                Sound.Paused = value;
            }
        }

        public bool IsPlaying()
        {
            return Sound != null && !Sound.Finished;
        }

        public bool IsFinishedPlaying()
        {
            return Sound.Finished;
        }

        /// <summary>
        /// Returns -1 if null, not playing, etc. 
        /// Else returns position in milliseconds.
        /// </summary>
        /// <returns></returns>
        public uint PlayPosition()
        {
            return Sound.PlayPosition;
        }

        // 3D Sound stuff only
        /*public void ProcessSound(Vector3 sourcePosition)
        {
            if (Sound != null && !Sound.Finished)
            {
                Sound.MaxDistance = MaximumDistance;
                Sound.MinDistance = MinimumDistance;
                Vector3D sourcePos = SoundHelperIK.Vector3ToVector3D(GameplayCamera.GetOffsetFromWorldCoords(sourcePosition));
                Sound.Position = sourcePos;
            }
        }

        public void SetDistances(float max, float min)
        {
            MaximumDistance = max;
            MinimumDistance = min;
        }*/

        public void Dispose()
        {
            Sound.Stop();
            Sound.Dispose();
            Source.Dispose();
        }

        public static ISoundEngine SoundEngine = new ISoundEngine();

        public static void ManageSoundEngine()
        {
            //SoundEngine.SetListenerPosition(new Vector3D(0, 0, 0), new Vector3D(0, 0, 1), new Vector3D(0, 0, 0), new Vector3D(0, 1, 0));
            SoundEngine.Update();
        }

        public static void StepVolume(float step, int decimals)
        {
            float temp = (float)Math.Round(SoundEngine.SoundVolume + step, decimals, MidpointRounding.ToEven);
            SoundEngine.SoundVolume = temp.LimitToRange(0f, 1f);
        }

        public static void DisposeSoundEngine()
        {
            SoundEngine.StopAllSounds();
            SoundEngine.Dispose();
        }
    }

    static class SoundHelperIK
    {
        public static Vector3D Vector3ToVector3D(Vector3 vec)
        {
            return new Vector3D(vec.X, vec.Z, vec.Y);
        }
    }
}
