using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomRadioStations
{
    class Track
    {
        public uint StartTime { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }

        public Track() { }

        public Track(uint startTime, string artist, string title)
        {
            StartTime = startTime;
            Artist = artist;
            Title = title;
        }

        public override string ToString()
        {
            return "Artist: " + Artist + "\n" + "Title: " + Title + "\n" + "Ms: " + StartTime;
        }
    }
}
