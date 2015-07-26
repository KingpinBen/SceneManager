using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SceneManagerAsset))]
public class SceneManagerAssetEditor : Editor
{
    private SceneManagerAsset _target;

    private static HashSet<string> _shownScenes;
    private static GUIContent _deleteSceneButtonContent = EditorGUIUtility.IconContent("TreeEditor.Trash");
    private static GUIContent s_missingSceneNameContent =
        new GUIContent("NoName", EditorGUIUtility.IconContent("console.warnicon").image);


    private void OnEnable()
    {
        _target = target as SceneManagerAsset;

        if (_shownScenes == null)
            _shownScenes = new HashSet<string>();
    }

    public override void OnInspectorGUI()
    {
        int i = 0;
        while(i < _target.Count)
        {
            DrawSceneData(i++);
        }

        if (GUILayout.Button("Add New SceneData"))
            AddNewSceneData();
    }

    private void AddNewSceneData()
    {
        int index = _target.AddNewSceneData();

        _target[index].name = null;

        if (SceneManagerWindow.instance)
            SceneManagerWindow.instance.Repaint();
    }

    private void DrawSceneData(int index)
    {
        var sceneData = _target[index];
        var shown = _shownScenes.Contains(sceneData.name);

        EditorGUILayout.BeginHorizontal("toolbar");
        EditorGUI.indentLevel++;

        if (string.IsNullOrEmpty(sceneData.name))
            shown = EditorGUILayout.Foldout(shown, s_missingSceneNameContent);
        else
            shown = EditorGUILayout.Foldout(shown, sceneData.name);

        EditorGUI.indentLevel--;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(_deleteSceneButtonContent, EditorStyles.toolbarButton) &&
            EditorUtility.DisplayDialog("You are about to remove a tracked scene", string.Format("Are you sure you want to remove the scene '{0} from the list?", sceneData.name), "Yes", "Cancel"))
        {
            _target.RemoveAt(index);

            if (SceneManagerWindow.instance)
                SceneManagerWindow.instance.Repaint();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (!shown)
        {
            if (_shownScenes.Contains(sceneData.name))
                _shownScenes.Remove(sceneData.name);
        }
        else
        {
            bool dirty = false;
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical();
            sceneData.name = EditorGUILayout.TextField("Scene Name", sceneData.name);

            var vec = new Vector2(sceneData.x, sceneData.y);
            vec = EditorGUILayout.Vector2Field("Position", vec);

            dirty = dirty || (vec.x != sceneData.x || vec.y != sceneData.y);

            sceneData.x = (int)vec.x;
            sceneData.y = (int)vec.y;

            vec.x = sceneData.width;
            vec.y = sceneData.height;
            vec = EditorGUILayout.Vector2Field("Size", vec);

            vec.x = Mathf.Clamp(vec.x, 10, vec.x);
            vec.y = Mathf.Clamp(vec.y, 10, vec.y);

            dirty = dirty || (vec.x != sceneData.width || vec.y != sceneData.height);

            sceneData.width = vec.x;
            sceneData.height = vec.y;

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            if (!_shownScenes.Contains(sceneData.name))
                _shownScenes.Add(sceneData.name);

            if (dirty && SceneManagerWindow.instance)
            {
                SceneManagerWindow.instance.Repaint();
            }
        }
    }
}
