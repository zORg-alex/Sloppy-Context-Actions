using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SloppyContextActions.Editor
{
    /// <summary>Registration API for editor integrations owned by other assets.</summary>
    public static class AssetContextActions
    {
        /// <summary>
        /// Registers or replaces an action by unique id. Predicates run while drawing;
        /// keep them cheap. Click handlers run on the next editor update. A null menu
        /// builder gives right-click a single item using the button tooltip or text.
        /// </summary>
        public static void Register(
            string id,
            Type assetType,
            GUIContent button,
            Action<Object> execute,
            Predicate<Object> isAvailable = null,
            Action<Object, GenericMenu> populateMenu = null,
            int order = 0)
        {
            if (assetType == null || !typeof(Object).IsAssignableFrom(assetType))
                throw new ArgumentException("An asset type derived from UnityEngine.Object is required.", nameof(assetType));
            if (button == null) throw new ArgumentNullException(nameof(button));
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            GUIContent content = new(button);

            ProjectContextActionHost.Register(id, item =>
            {
                if (item.IsFolder || string.IsNullOrEmpty(item.Path)) return;
                Object asset = item.Asset;
                if (asset == null || !assetType.IsInstanceOfType(asset)) return;
                using (ContextActionPerformance.Measure(
                           "Integration availability: " + id,
                           "Cache expensive integration predicates; do not scan the project during drawing."))
                {
                    if (isAvailable != null && !isAvailable(asset)) return;
                }

                ProjectContextButtonClick click =
                    ProjectContextButton.Draw(item.ReserveButtonRect(), content);
                if (click == ProjectContextButtonClick.None) return;
                EditorApplication.delayCall += () =>
                {
                    if (asset == null || (isAvailable != null && !isAvailable(asset))) return;
                    if (click == ProjectContextButtonClick.Left)
                    {
                        execute(asset);
                        return;
                    }

                    GenericMenu menu = new();
                    if (populateMenu != null) populateMenu(asset, menu);
                    else
                    {
                        string label = string.IsNullOrEmpty(content.tooltip)
                            ? content.text : content.tooltip;
                        menu.AddItem(new GUIContent(label), false, () =>
                        {
                            if (asset != null && (isAvailable == null || isAvailable(asset)))
                                execute(asset);
                        });
                    }
                    menu.ShowAsContext();
                };
            }, order);
        }

        public static void Unregister(string id) => ProjectContextActionHost.Unregister(id);
    }
}
