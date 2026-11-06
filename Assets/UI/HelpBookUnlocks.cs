using System;
using System.Collections.Generic;
using UnityEngine;

// One discovery record for gameplay help, Navigation's book and Shape Closet.
// Scene changes/retries retain it; a new player/Editor Play session clears it.
public static class HelpBookUnlocks
{
    private static readonly HashSet<BodyForm> discovered = new HashSet<BodyForm>();
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLaunch()
    {
        discovered.Clear();
        Changed = null;
    }

    // Blank IDs are reserved for always-available general help (the Star page).
    public static bool IsUnlocked(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return true;
        return Enum.TryParse(id, out BodyForm form) &&
            (form == BodyForm.Star || discovered.Contains(form));
    }

    // Call only after the player has successfully taken control of a body.
    // Completion, opening help and selecting a scene never grant a form group.
    public static void RecordPossession(BodyForm form)
    {
        if (form == BodyForm.Star || !Enum.IsDefined(typeof(BodyForm), form)) return;
        if (discovered.Add(form)) Changed?.Invoke();
    }
}
