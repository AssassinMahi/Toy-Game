using UnityEngine;

/// <summary>Stores and formats the player's best chapter completion time.</summary>
public static class GameScore
{
    private const string BestTimeKey = "ToyDriveBestCompletionTime";

    public static bool HasBestTime => PlayerPrefs.HasKey(BestTimeKey);

    public static float BestTime => PlayerPrefs.GetFloat(BestTimeKey, 0f);

    public static float RecordCompletionTime(float completionTime)
    {
        if (!HasBestTime || completionTime < BestTime)
        {
            PlayerPrefs.SetFloat(BestTimeKey, completionTime);
            PlayerPrefs.Save();
        }

        return BestTime;
    }

    public static string FormatTime(float timeInSeconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeInSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
