using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using Desktop_Gremlin;

public static class MediaManager
{
    private static Dictionary<string, DateTime> LastPlayed = new Dictionary<string, DateTime>();
    private static MediaPlayer player = new MediaPlayer();
    private static double _volumeMultiplier = 1.0;

    public static void SetVolume(double volume)
    {
        _volumeMultiplier = Math.Max(0.0, Math.Min(1.0, volume));
    }

    public static void PlaySound(string fileName, string startChar, double delaySeconds = 0, double volume = 1.0)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", startChar, fileName);
        if (!File.Exists(path)) return;
        if (delaySeconds > 0 &&
            LastPlayed.TryGetValue(fileName, out DateTime lastTime) &&
            (DateTime.Now - lastTime).TotalSeconds < delaySeconds)
        {
            return;
        }
        player.Open(new Uri(path));
        // Use GremlinSettings volume if available, fallback to Settings.VolumeLevel
        double finalVolume = Settings.VolumeLevel * _volumeMultiplier * GremlinSettings.Volume;
        player.Volume = finalVolume;   
        player.Play();
        LastPlayed[fileName] = DateTime.Now;
    }
}


