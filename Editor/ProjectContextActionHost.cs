using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SloppyContextActions.Editor
{
    /// <summary>
    /// Hosts independently registered controls on hovered Project-window folders and assets.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectContextActionHost
    {
        public const float ButtonSpacing = 2f;
        public const float EdgePadding = 2f;
        private const double HoverRepaintInterval = 1d / 30d;

        private static readonly List<Registration> Registrations = new();
        private static readonly List<Registration> TreeFolderRegistrations = new();
        private static readonly Type ProjectBrowserType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
        private static readonly MethodInfo IsTwoColumnsMethod = ProjectBrowserType?.GetMethod(
            "IsTwoColumns",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo ListAreaRectField = ProjectBrowserType?.GetField(
            "m_ListAreaRect",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static bool _sortRequired;
        private static bool _treeSortRequired;
        private static EditorWindow _projectWindow;
        private static string _hoveredGuid;
        private static double _nextHoverRepaintTime;
        private static bool _mouseWasOverProjectWindow;

        static ProjectContextActionHost()
        {
            EditorApplication.projectWindowItemOnGUI += DrawProjectItem;
            EditorApplication.update += TrackProjectWindow;
        }

        /// <summary>
        /// Registers or replaces a drawer. Lower order values reserve the rightmost slots first.
        /// </summary>
        public static void Register(string id, Action<ProjectContextItem> drawer, int order = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A context action id is required.", nameof(id));
            if (drawer == null)
                throw new ArgumentNullException(nameof(drawer));

            int index = Registrations.FindIndex(item => item.Id == id);
            Registration registration = new(id, drawer, order);

            if (index >= 0) Registrations[index] = registration;
            else Registrations.Add(registration);

            _sortRequired = true;
            RepaintProjectWindow();
        }

        /// <summary>Registers an action shown only on hovered folders in the left Project tree.</summary>
        public static void RegisterTreeFolder(
            string id,
            Action<ProjectContextItem> drawer,
            int order = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A tree-folder action id is required.", nameof(id));
            if (drawer == null) throw new ArgumentNullException(nameof(drawer));

            int index = TreeFolderRegistrations.FindIndex(item => item.Id == id);
            Registration registration = new(id, drawer, order);
            if (index >= 0) TreeFolderRegistrations[index] = registration;
            else TreeFolderRegistrations.Add(registration);

            _treeSortRequired = true;
            RepaintProjectWindow();
        }

        public static void Unregister(string id)
        {
            int removed = Registrations.RemoveAll(item => item.Id == id);
            if (removed > 0) RepaintProjectWindow();
        }

        private static void DrawProjectItem(string guid, Rect itemRect)
        {
            if (Registrations.Count == 0 && TreeFolderRegistrations.Count == 0) return;

            EditorWindow hoveredWindow = EditorWindow.mouseOverWindow;
            if (hoveredWindow != null && hoveredWindow.GetType() == ProjectBrowserType)
                _projectWindow = hoveredWindow;

            ProjectContextItem item = new(guid, itemRect);
            if (!item.IsHovered) return;

            if (_hoveredGuid != guid)
            {
                _hoveredGuid = guid;
                RepaintProjectWindow();
            }

            if (_sortRequired)
            {
                Registrations.Sort((left, right) => left.Order.CompareTo(right.Order));
                _sortRequired = false;
            }

            foreach (Registration registration in Registrations)
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

            if (!item.IsFolder || !IsLeftTreeItem(itemRect)) return;

            if (_treeSortRequired)
            {
                TreeFolderRegistrations.Sort((left, right) => left.Order.CompareTo(right.Order));
                _treeSortRequired = false;
            }

            foreach (Registration registration in TreeFolderRegistrations)
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

        private static bool IsLeftTreeItem(Rect itemRect)
        {
            if (_projectWindow == null || IsTwoColumnsMethod == null || ListAreaRectField == null)
                return false;
            if (!(bool)IsTwoColumnsMethod.Invoke(_projectWindow, null)) return false;

            Rect listAreaRect = (Rect)ListAreaRectField.GetValue(_projectWindow);
            return itemRect.xMax <= listAreaRect.xMin + 1f;
        }

        private static void TrackProjectWindow()
        {
            EditorWindow hoveredWindow = EditorWindow.mouseOverWindow;
            bool isOverProjectWindow =
                hoveredWindow != null && hoveredWindow.GetType().Name == "ProjectBrowser";

            if (!isOverProjectWindow)
            {
                if (_mouseWasOverProjectWindow && _projectWindow != null)
                {
                    _hoveredGuid = null;
                    _projectWindow.Repaint();
                }

                _mouseWasOverProjectWindow = false;
                return;
            }

            _projectWindow = hoveredWindow;
            _mouseWasOverProjectWindow = true;
            if (!_projectWindow.wantsMouseMove) _projectWindow.wantsMouseMove = true;
            if (!_projectWindow.wantsMouseEnterLeaveWindow)
                _projectWindow.wantsMouseEnterLeaveWindow = true;

            double now = EditorApplication.timeSinceStartup;
            if (now < _nextHoverRepaintTime) return;

            _nextHoverRepaintTime = now + HoverRepaintInterval;
            _projectWindow.Repaint();
        }

        internal static void RepaintProjectWindow()
        {
            if (_projectWindow != null)
            {
                _projectWindow.Repaint();
                return;
            }

            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().Name == "ProjectBrowser") window.Repaint();
            }
        }

        private readonly struct Registration
        {
            public string Id { get; }
            public Action<ProjectContextItem> Drawer { get; }
            public int Order { get; }

            public Registration(string id, Action<ProjectContextItem> drawer, int order)
            {
                Id = id;
                Drawer = drawer;
                Order = order;
            }
        }
    }
}
