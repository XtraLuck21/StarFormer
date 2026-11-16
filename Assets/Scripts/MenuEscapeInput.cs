using UnityEngine;
using UnityEngine.InputSystem;

internal static class MenuEscapeInput
{
    private static int handledFrame = -1;
#if UNITY_EDITOR
    private static UnityEditor.EditorWindow viewToRestore;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        handledFrame = -1;
#if UNITY_EDITOR
        viewToRestore = null;
#endif
    }

    public static bool TryConsume()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || handledFrame == Time.frameCount)
            return false;

        var escape = keyboard.escapeKey;
#if UNITY_EDITOR
        // Restoring the layout changes keyboard focus. Finish the press first so
        // the restored Game view cannot receive the held key as another press.
        if (viewToRestore == null && escape.wasPressedThisFrame)
        {
            var view = UnityEditor.EditorWindow.focusedWindow;
            // Unity can temporarily report no focused window during a layout change.
            if (view == null)
            {
                foreach (var window in Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
                {
                    if (window.maximized && window.GetType().Name == "GameView")
                    {
                        view = window;
                        break;
                    }
                }
            }
            if (view != null && view.maximized && view.GetType().Name == "GameView")
                viewToRestore = view;
        }

        if (viewToRestore != null)
        {
            handledFrame = Time.frameCount;
            if (!escape.isPressed)
            {
                var view = viewToRestore;
                viewToRestore = null;
                if (view.maximized) view.maximized = false;
            }
            return false;
        }
#endif
        if (!escape.wasPressedThisFrame) return false;
        handledFrame = Time.frameCount;
        return true;
    }
}
