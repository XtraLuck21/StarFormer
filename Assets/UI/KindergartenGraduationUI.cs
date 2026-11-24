using System;
using UnityEngine;

public sealed class KindergartenGraduationUI : SchoolCompletionCard
{
    public void Initialize(Sprite graduate, Action continueToNextLevel)
    {
        InitializeCard(graduate, continueToNextLevel, "Kindergarten Complete!",
            "You've graduated!\nReady for a new challenge?", "Next Level  \u2192");
    }
}
