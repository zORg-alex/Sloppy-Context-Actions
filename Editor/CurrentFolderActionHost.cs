using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ContextActionsSlop.Editor
{
    /// <summary>Draws one action strip for the active folder in each Project window.</summary>
    [InitializeOnLoad]
    public static class CurrentFolderActionHost
    {
        private const string OverlayName = "context-actions-slop-current-folder";
        private const BindingFlags InstanceMembers =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly List<Registration> Registrations = new();
        private static readonly Dictionary<int, FolderOverlay> Overlays = new();
        private static readonly Type ProjectBrowserType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
        private static readonly MethodInfo GetActiveFolderPathMethod =
            ProjectBrowserType?.GetMethod("GetActiveFolderPath", InstanceMembers);
        private static readonly FieldInfo ListAreaRectField =
            ProjectBrowserType?.GetField("m_ListAreaRect", InstanceMembers);

        private static bool _sortRequired;
        private static double _nextOverlayUpdate;

        static CurrentFolderActionHost()
        {
            EditorApplication.update += UpdateOverlays;
        }

        public static void Register(
            string id,
            Action<ProjectContextItem> drawer,
            Func<string, bool> isVisible,
            int order = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A current-folder action id is required.", nameof(id));
            if (drawer == null) throw new ArgumentNullException(nameof(drawer));
            if (isVisible == null) throw new ArgumentNullException(nameof(isVisible));

            int index = Registrations.FindIndex(item => item.Id == id);
            Registration registration = new(id, drawer, isVisible, order);
            if (index >= 0) Registrations[index] = registration;
            else Registrations.Add(registration);

            _sortRequired = true;
            Repaint();
        }

        internal static void Repaint()
        {
            foreach (FolderOverlay overlay in Overlays.Values) overlay.Window.Repaint();
        }

        private static void UpdateOverlays()
        {
            if (ProjectBrowserType == null) return;
            if (EditorApplication.timeSinceStartup < _nextOverlayUpdate) return;
            _nextOverlayUpdate = EditorApplication.timeSinceStartup + 0.1d;

            HashSet<int> liveWindows = new();
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null || window.GetType() != ProjectBrowserType) continue;

                int id = window.GetHashCode();
                liveWindows.Add(id);
                if (!Overlays.TryGetValue(id, out FolderOverlay overlay))
                {
                    VisualElement existing = window.rootVisualElement.Q<VisualElement>(OverlayName);
                    existing?.RemoveFromHierarchy();

                    overlay = new FolderOverlay(window, DrawOverlay);
                    Overlays.Add(id, overlay);
                    window.rootVisualElement.Add(overlay.Container);
                }

                UpdateOverlayLayout(overlay);
            }

            List<int> stale = null;
            foreach (int id in Overlays.Keys)
            {
                if (liveWindows.Contains(id)) continue;
                stale ??= new List<int>();
                stale.Add(id);
            }

            if (stale == null) return;
            foreach (int id in stale) Overlays.Remove(id);
        }

        private static void UpdateOverlayLayout(FolderOverlay overlay)
        {
            string path = GetActiveFolderPath(overlay.Window);
            if (!string.Equals(path, overlay.ActiveFolderPath, StringComparison.Ordinal))
            {
                overlay.ActiveFolderPath = path;
                overlay.Container.MarkDirtyRepaint();
            }

            int visibleCount = CountVisible(path);
            if (visibleCount == 0 || ListAreaRectField == null)
            {
                overlay.Container.style.display = DisplayStyle.None;
                return;
            }

            Rect listAreaRect = (Rect)ListAreaRectField.GetValue(overlay.Window);
            if (listAreaRect.width <= 0f || listAreaRect.height <= 0f)
            {
                overlay.Container.style.display = DisplayStyle.None;
                return;
            }

            float size = ContextActionPreferences.ButtonSize;
            float width = visibleCount * size + (visibleCount - 1) * ProjectContextActionHost.ButtonSpacing;
            overlay.Container.style.display = DisplayStyle.Flex;
            overlay.Container.style.left = listAreaRect.xMin + ProjectContextActionHost.EdgePadding;
            overlay.Container.style.top = listAreaRect.yMin + ProjectContextActionHost.EdgePadding;
            overlay.Container.style.width = width;
            overlay.Container.style.height = Mathf.Min(size, listAreaRect.height);
        }

        private static void DrawOverlay(EditorWindow window)
        {
            SortIfRequired();
            string path = GetActiveFolderPath(window);
            if (string.IsNullOrEmpty(path)) return;

            List<Registration> visible = new();
            foreach (Registration registration in Registrations)
            {
                if (registration.IsVisible(path)) visible.Add(registration);
            }

            if (visible.Count == 0) return;

            float size = ContextActionPreferences.ButtonSize;
            float width = visible.Count * size + (visible.Count - 1) * ProjectContextActionHost.ButtonSpacing;
            string guid = AssetDatabase.AssetPathToGUID(path);
            ProjectContextItem item = new(guid, new Rect(0f, 0f, width, size), layoutFromLeft: true);

            foreach (Registration registration in visible)
            {
                try
                {
                    registration.Drawer(item);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static int CountVisible(string path)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            SortIfRequired();

            int count = 0;
            foreach (Registration registration in Registrations)
            {
                if (registration.IsVisible(path)) count++;
            }

            return count;
        }

        private static string GetActiveFolderPath(EditorWindow window)
        {
            return GetActiveFolderPathMethod?.Invoke(window, null) as string;
        }

        private static void SortIfRequired()
        {
            if (!_sortRequired) return;
            Registrations.Sort((left, right) => left.Order.CompareTo(right.Order));
            _sortRequired = false;
        }

        private sealed class FolderOverlay
        {
            public EditorWindow Window { get; }
            public IMGUIContainer Container { get; }
            public string ActiveFolderPath { get; set; }

            public FolderOverlay(EditorWindow window, Action<EditorWindow> draw)
            {
                Window = window;
                Container = new IMGUIContainer(() => draw(window))
                {
                    name = OverlayName,
                    pickingMode = PickingMode.Position
                };
                Container.style.position = Position.Absolute;
            }
        }

        private readonly struct Registration
        {
            public string Id { get; }
            public Action<ProjectContextItem> Drawer { get; }
            public Func<string, bool> IsVisible { get; }
            public int Order { get; }

            public Registration(
                string id,
                Action<ProjectContextItem> drawer,
                Func<string, bool> isVisible,
                int order)
            {
                Id = id;
                Drawer = drawer;
                IsVisible = isVisible;
                Order = order;
            }
        }
    }
}
