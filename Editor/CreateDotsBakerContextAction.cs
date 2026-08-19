using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ContextActionsSlop.Editor
{
    [InitializeOnLoad]
    internal static class CreateDotsBakerContextAction
    {
        private const string RegistrationId = "context-actions-slop.create-dots-baker";

        static CreateDotsBakerContextAction()
        {
            ProjectContextActionHost.Register(RegistrationId, Draw, order: -70);
        }

        private static void Draw(ProjectContextItem item)
        {
            if (item.IsFolder || item.Asset is not MonoScript script) return;
            if (FindType("Unity.Entities.Baker`1") == null) return;

            Type authoringType = script.GetClass();
            if (authoringType == null || authoringType.IsAbstract ||
                !typeof(MonoBehaviour).IsAssignableFrom(authoringType))
            {
                return;
            }

            Rect buttonRect = item.ReserveButtonRect();
            GUIContent content = new()
            {
                image = ContextActionIcons.AddScript,
                tooltip = "Create DOTS Baker"
            };

            ProjectContextButtonClick click = ProjectContextButton.Draw(buttonRect, content);
            if (click != ProjectContextButtonClick.None)
                DotsBakerPickerWindow.Show(buttonRect, script, authoringType);
        }

        internal static Type FindType(string fullName)
        {
            return ContextActionTypeCache.Find(fullName);
        }
    }

    internal sealed class DotsBakerPickerWindow : EditorWindow
    {
        [Flags]
        private enum TransformUsage
        {
            None = 0,
            Renderable = 1,
            Dynamic = 1 << 1,
            WorldSpace = 1 << 2,
            NonUniformScale = 1 << 3,
            ManualOverride = 1 << 4
        }

        private enum ComponentKind
        {
            Component,
            Buffer,
            SharedComponent
        }

        private sealed class ComponentEntry
        {
            public Type Type { get; }
            public ComponentKind Kind { get; }
            public string DisplayName { get; }
            public string SearchText { get; }

            public ComponentEntry(Type type, ComponentKind kind)
            {
                Type = type;
                Kind = kind;
                DisplayName = string.IsNullOrEmpty(type.Namespace)
                    ? type.Name
                    : type.Namespace + "." + type.Name;
                SearchText = DisplayName + " " + kind;
            }
        }

        private const float Width = 460f;
        private const float Height = 520f;
        private readonly List<ComponentEntry> _components = new();
        private readonly HashSet<Type> _selected = new();
        private MonoScript _authoringScript;
        private Type _authoringType;
        private string _search = string.Empty;
        private Vector2 _scroll;
        private TransformUsage _transformUsage = TransformUsage.Dynamic;
        private bool _focusSearch = true;

        internal static void Show(Rect anchor, MonoScript script, Type authoringType)
        {
            DotsBakerPickerWindow window = CreateInstance<DotsBakerPickerWindow>();
            window.titleContent = new GUIContent("Create Baker");
            window._authoringScript = script;
            window._authoringType = authoringType;
            window.LoadComponents();

            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(
                new Vector2(anchor.xMin, anchor.yMax));
            window.ShowAsDropDown(new Rect(screenPoint, Vector2.zero), new Vector2(Width, Height));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Baker for " + _authoringType.Name,
                EditorStyles.boldLabel);
            EditorGUILayout.Space(3f);

            _transformUsage = (TransformUsage)EditorGUILayout.EnumFlagsField(
                new GUIContent("Transform Usage"),
                _transformUsage);

            EditorGUILayout.Space(4f);
            GUI.SetNextControlName("ComponentSearch");
            _search = EditorGUILayout.TextField(
                _search,
                GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Components ({_selected.Count} selected)",
                EditorStyles.miniBoldLabel);
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(48f)))
                _selected.Clear();
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, EditorStyles.helpBox);
            foreach (ComponentEntry component in GetFilteredComponents())
            {
                bool selected = _selected.Contains(component.Type);
                EditorGUILayout.BeginHorizontal();
                bool next = EditorGUILayout.Toggle(selected, GUILayout.Width(18f));
                if (next != selected)
                {
                    if (next) _selected.Add(component.Type);
                    else _selected.Remove(component.Type);
                }

                EditorGUILayout.LabelField(component.DisplayName);
                GUILayout.FlexibleSpace();
                GUILayout.Label(GetKindLabel(component.Kind), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel")) Close();

            EditorGUI.BeginDisabledGroup(_selected.Count == 0);
            if (GUILayout.Button("Create Baker")) CreateBaker();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (_focusSearch && Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl("ComponentSearch");
                _focusSearch = false;
            }
        }

        private void LoadComponents()
        {
            using (ContextActionPerformance.Measure(
                       "DOTS Baker component discovery",
                       "The lookup enumerates loadable types from project script assemblies and classifies DOTS component interfaces when the Baker picker opens."))
            {
                Type componentInterface = CreateDotsBakerContextAction.FindType(
                    "Unity.Entities.IComponentData");
                Type bufferInterface = CreateDotsBakerContextAction.FindType(
                    "Unity.Entities.IBufferElementData");
                Type sharedInterface = CreateDotsBakerContextAction.FindType(
                    "Unity.Entities.ISharedComponentData");
                if (componentInterface == null) return;

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!IsProjectAssembly(assembly)) continue;

                    foreach (Type type in GetLoadableTypes(assembly))
                    {
                        if (type == null || type.IsAbstract || type.IsGenericTypeDefinition ||
                            !type.IsValueType)
                        {
                            continue;
                        }

                        ComponentKind? kind = null;
                        if (bufferInterface?.IsAssignableFrom(type) == true)
                            kind = ComponentKind.Buffer;
                        else if (sharedInterface?.IsAssignableFrom(type) == true)
                            kind = ComponentKind.SharedComponent;
                        else if (componentInterface.IsAssignableFrom(type))
                            kind = ComponentKind.Component;

                        if (kind.HasValue)
                            _components.Add(new ComponentEntry(type, kind.Value));
                    }
                }

                _components.Sort((left, right) =>
                    string.Compare(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        private IEnumerable<ComponentEntry> GetFilteredComponents()
        {
            if (string.IsNullOrWhiteSpace(_search)) return _components;

            return _components
                .Select(component => new
                {
                    Component = component,
                    Score = FuzzyScore(component.SearchText, _search)
                })
                .Where(result => result.Score >= 0)
                .OrderByDescending(result => result.Score)
                .ThenBy(result => result.Component.DisplayName)
                .Select(result => result.Component);
        }

        private static int FuzzyScore(string candidate, string query)
        {
            candidate = candidate.ToLowerInvariant();
            query = query.Trim().ToLowerInvariant();
            int queryIndex = 0;
            int score = 0;
            int consecutive = 0;

            for (int i = 0; i < candidate.Length && queryIndex < query.Length; i++)
            {
                if (candidate[i] != query[queryIndex])
                {
                    consecutive = 0;
                    continue;
                }

                consecutive++;
                score += 1 + consecutive * 3;
                if (i == 0 || candidate[i - 1] is '.' or '_' or ' ') score += 8;
                queryIndex++;
            }

            return queryIndex == query.Length ? score : -1;
        }

        private void CreateBaker()
        {
            string sourcePath = AssetDatabase.GetAssetPath(_authoringScript);
            string directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory)) return;

            string className = GetSimpleTypeName(_authoringType) + "Baker";
            string destinationPath = $"{directory}/{className}.cs";
            if (File.Exists(destinationPath) &&
                !EditorUtility.DisplayDialog(
                    "Create DOTS Baker",
                    $"{destinationPath} already exists.",
                    "Replace",
                    "Cancel"))
            {
                return;
            }

            File.WriteAllText(
                destinationPath,
                BuildBakerSource(className),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);

            Object createdAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(destinationPath);
            Selection.activeObject = createdAsset;
            EditorGUIUtility.PingObject(createdAsset);
            Close();
        }

        private string BuildBakerSource(string className)
        {
            StringBuilder source = new();
            source.AppendLine("using Unity.Entities;");
            source.AppendLine();

            string targetNamespace = _authoringType.Namespace;
            if (!string.IsNullOrEmpty(targetNamespace))
            {
                source.Append("namespace ").Append(targetNamespace).AppendLine();
                source.AppendLine("{");
            }

            string indent = string.IsNullOrEmpty(targetNamespace) ? string.Empty : "    ";
            string authoringName = GetCSharpTypeName(_authoringType);
            source.Append(indent).Append("public sealed class ").Append(className)
                .Append(" : Baker<").Append(authoringName).AppendLine(">");
            source.Append(indent).AppendLine("{");
            source.Append(indent).Append("    public override void Bake(")
                .Append(authoringName).AppendLine(" authoring)");
            source.Append(indent).AppendLine("    {");
            source.Append(indent).Append("        Entity entity = GetEntity(")
                .Append(GetTransformUsageSource()).AppendLine(");");

            foreach (ComponentEntry component in _components.Where(entry => _selected.Contains(entry.Type)))
            {
                string componentName = GetCSharpTypeName(component.Type);
                switch (component.Kind)
                {
                    case ComponentKind.Buffer:
                        source.Append(indent).Append("        AddBuffer<")
                            .Append(componentName).AppendLine(">(entity);");
                        break;
                    case ComponentKind.SharedComponent:
                        source.Append(indent).Append("        AddSharedComponentManaged(entity, default(")
                            .Append(componentName).AppendLine("));");
                        break;
                    default:
                        source.Append(indent).Append("        AddComponent<")
                            .Append(componentName).AppendLine(">(entity);");
                        break;
                }
            }

            source.Append(indent).AppendLine("    }");
            source.Append(indent).AppendLine("}");
            if (!string.IsNullOrEmpty(targetNamespace)) source.AppendLine("}");
            return source.ToString();
        }

        private string GetTransformUsageSource()
        {
            if (_transformUsage == TransformUsage.None) return "TransformUsageFlags.None";

            List<string> flags = new();
            foreach (TransformUsage value in Enum.GetValues(typeof(TransformUsage)))
            {
                if (value == TransformUsage.None || !_transformUsage.HasFlag(value)) continue;
                flags.Add("TransformUsageFlags." + value);
            }

            return flags.Count == 0 ? "TransformUsageFlags.None" : string.Join(" | ", flags);
        }

        private static bool IsProjectAssembly(Assembly assembly)
        {
            string location;
            try
            {
                location = assembly.Location.Replace('\\', '/');
            }
            catch (NotSupportedException)
            {
                return false;
            }

            return location.IndexOf(
                "/Library/ScriptAssemblies/",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        private static string GetKindLabel(ComponentKind kind)
        {
            return kind switch
            {
                ComponentKind.Buffer => "Buffer",
                ComponentKind.SharedComponent => "Shared",
                _ => "Component"
            };
        }

        private static string GetSimpleTypeName(Type type)
        {
            int genericMarker = type.Name.IndexOf('`');
            return genericMarker < 0 ? type.Name : type.Name.Substring(0, genericMarker);
        }

        private static string GetCSharpTypeName(Type type)
        {
            return "global::" + (type.FullName ?? type.Name).Replace('+', '.');
        }
    }
}
