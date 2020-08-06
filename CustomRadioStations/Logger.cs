using System;
using System.IO;

/// <summary>
/// Static logger class that allows direct logging of anything to a text file
/// </summary>
public static class Logger
{
    public static void Log(object message, string path = "scripts\\Custom Radio Stations\\CustomRadioStations.log")
    {
        File.AppendAllText(path, DateTime.Now + " : " + message + Environment.NewLine);
    }

    public static void Init(string path = "scripts\\Custom Radio Stations\\CustomRadioStations.log")
    {
        bool exists = File.Exists(path);
        if (exists)
        {
            File.Delete(path);
        }
    }
}