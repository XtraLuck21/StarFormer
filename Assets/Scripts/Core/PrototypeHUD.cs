using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    [SerializeField] private PlayerControlManager manager;

    private GUIStyle instructionStyle;
    private GUIStyle successStyle;

    public void Configure(PlayerControlManager controlManager)
    {
        manager = controlManager;
    }

    private void OnGUI()
    {
        instructionStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = new Color(0.18f, 0.27f, 0.29f) }
        };

        successStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.20f, 0.52f, 0.25f) }
        };

        GUI.Label(
            new Rect(20f, 16f, 900f, 32f),
            "A/D or Arrow Keys: Move    Space: Star Jump    R: Restart",
            instructionStyle
        );

        GUI.Label(
            new Rect(20f, 46f, 900f, 32f),
            "Catch the rolling ball. Your first steering input starts its bounce.",
            instructionStyle
        );

        if (manager != null && manager.IsComplete)
        {
            GUI.Box(
                new Rect(Screen.width * 0.5f - 230f, 80f, 460f, 78f),
                GUIContent.none
            );

            GUI.Label(
                new Rect(Screen.width * 0.5f - 220f, 88f, 440f, 60f),
                "PROTOTYPE COMPLETE!",
                successStyle
            );
        }
    }
}
