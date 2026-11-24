using System;
using UnityEngine;

public sealed class ElementaryCompletionUI : SchoolCompletionCard
{
    public void Initialize(Sprite cleverStar, Action enterLevel2)
    {
        InitializeCard(cleverStar, enterLevel2, "Elementary Complete!",
            "More to learn? Bring it on!", "Enter Level 2  \u2192",
            ideaFlashPosition: new Vector2(105f, -94f));
    }
}
