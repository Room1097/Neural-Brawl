using UnityEngine;
using System.IO;

public class StatsLogger : MonoBehaviour
{
    // Define the file name (e.g., "Experiment_Results.csv")
    public string fileName = "Experiment_Results.csv";
    private string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/" + fileName;

        // Write the Header row if the file doesn't exist yet
        if (!File.Exists(filePath))
        {
            // Columns: Episode Number, Winner Name, Match Duration, Winner Health
            File.WriteAllText(filePath, "Episode,Winner,Duration,RemainingHealth\n");
        }
    }

    public void LogMatch(int episodeCount, string winnerName, float duration, int remainingHealth)
    {
        // Format the data as a comma-separated line
        string line = $"{episodeCount},{winnerName},{duration:F2},{remainingHealth}\n";

        // Append to the file
        File.AppendAllText(filePath, line);
        
        // Debug log to confirm it's working
        // Debug.Log($"Stats Saved: {line}");
    }
}