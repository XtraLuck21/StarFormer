using System;
using UnityEngine;

// The two schools award independent groups; completing Level02 awards both.
public static class ShapeClosetProgress
{
    private const string ProgressKey = "StarFormer.ShapeCloset.UnlockedGroups.v2";
    private const string LegacyProgressKey = "StarFormer.ShapeCloset.UnlockTier.v1";
    private const int BasicForms = 1;
    private const int AdvancedForms = 2;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSubscribers() => Changed = null;

    private static int ReadUnlockedGroups()
    {
        if (PlayerPrefs.HasKey(ProgressKey)) return PlayerPrefs.GetInt(ProgressKey, 0);

        // Old tiers did not record which route earned the reward. Preserve existing
        // unlocks rather than removing potentially legitimate progress during migration.
        int tier = PlayerPrefs.GetInt(LegacyProgressKey, 0);
        if (tier <= 0) return 0;
        int groups = tier >= 2 ? BasicForms | AdvancedForms : BasicForms;
        PlayerPrefs.SetInt(ProgressKey, groups);
        PlayerPrefs.DeleteKey(LegacyProgressKey);
        PlayerPrefs.Save();
        return groups;
    }

    public static bool IsUnlocked(BodyForm form)
    {
        int groups = ReadUnlockedGroups();
        switch (form)
        {
            case BodyForm.Box:
            case BodyForm.Bubble:
            case BodyForm.BouncyBall:
                return (groups & BasicForms) != 0;
            case BodyForm.Spider:
            case BodyForm.Fan:
                return (groups & AdvancedForms) != 0;
            default:
                return false;
        }
    }

    public static void RecordCompletion(string sceneName)
    {
        int earnedGroups;
        switch (sceneName)
        {
            case "IntroBouncyBall":
            case "Level01":
                earnedGroups = BasicForms;
                break;
            case "IntroFan":
                earnedGroups = AdvancedForms;
                break;
            case "Level02":
                earnedGroups = BasicForms | AdvancedForms;
                break;
            default:
                return;
        }

        int currentGroups = ReadUnlockedGroups();
        int updatedGroups = currentGroups | earnedGroups;
        if (updatedGroups == currentGroups) return;
        PlayerPrefs.SetInt(ProgressKey, updatedGroups);
        // Save before a transition or browser close can discard the earned reward.
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
