using UnityEditor;
using UnityEngine;

namespace ContextActionsSlop.Editor
{
    public enum ProjectContextButtonClick
    {
        None,
        Left,
        Right
    }

    /// <summary>Draws a Project-window button with native hover/pressed states and two mouse actions.</summary>
    public static class ProjectContextButton
    {
        private static readonly int ButtonHint = "ContextActionsSlop.ProjectContextButton".GetHashCode();

        public static ProjectContextButtonClick Draw(
            Rect rect,
            GUIContent content,
            GUIStyle style = null)
        {
            style ??= EditorStyles.miniButton;

            int controlId = GUIUtility.GetControlID(ButtonHint, FocusType.Passive, rect);
            Event current = Event.current;
            bool hovered = rect.Contains(current.mousePosition);
            bool ownsMouse = GUIUtility.hotControl == controlId;

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.Repaint:
                    if (content.image == null)
                    {
                        style.Draw(rect, content, controlId, false, hovered);
                    }
                    else
                    {
                        GUIContent tooltipContent = new(string.Empty, content.tooltip);
                        style.Draw(rect, tooltipContent, controlId, false, hovered);
                        GUI.DrawTexture(rect, content.image, ScaleMode.ScaleToFit, true);
                    }
                    break;

                case EventType.MouseDown:
                    if (hovered && (current.button == 0 || current.button == 1))
                    {
                        GUIUtility.hotControl = controlId;
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (ownsMouse) current.Use();
                    break;

                case EventType.MouseUp:
                    if (!ownsMouse) break;

                    GUIUtility.hotControl = 0;
                    int mouseButton = current.button;
                    current.Use();

                    if (!hovered) break;
                    if (mouseButton == 0) return ProjectContextButtonClick.Left;
                    if (mouseButton == 1) return ProjectContextButtonClick.Right;
                    break;
            }

            return ProjectContextButtonClick.None;
        }
    }
}
