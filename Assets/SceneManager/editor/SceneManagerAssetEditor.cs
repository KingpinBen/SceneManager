﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SceneManagerAsset))]
public class SceneManagerAssetEditor : Editor
{
    private SceneManagerAsset _target;
    //private SerializedProperty _sceneData;

    private static HashSet<string> _hiddenScenes;
    private static GUIContent _deleteSceneButtonContent = EditorGUIUtility.IconContent("TreeEditor.Trash");
    private static GUIContent s_missingSceneNameContent =
    new GUIContent("NoName", EditorGUIUtility.IconContent("console.warnicon").image);


    private void OnEnable()
    {
        _target = target as SceneManagerAsset;

        if (_hiddenScenes == null)
            _hiddenScenes = new HashSet<string>();
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
        _target[index].rectangle = new Rect(0, 0, 100, 75);

        if (SceneManagerWindow.instance)
            SceneManagerWindow.instance.Repaint();
    }

    private void DrawSceneData(int index)
    {
        var sceneData = _target[index];
        var shown = !_hiddenScenes.Contains(sceneData.name);

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
            if (!_hiddenScenes.Contains(sceneData.name))
                _hiddenScenes.Add(sceneData.name);
        }
        else
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical();
            sceneData.name = EditorGUILayout.TextField("Scene Name", sceneData.name);

            var vec = new Vector2(sceneData.rectangle.x, sceneData.rectangle.y);
            vec = EditorGUILayout.Vector2Field("Position", vec);
            sceneData.rectangle.x = vec.x;
            sceneData.rectangle.y = vec.y;

            vec.x = sceneData.rectangle.width;
            vec.y = sceneData.rectangle.height;
            vec = EditorGUILayout.Vector2Field("Size", vec);
            sceneData.rectangle.width = vec.x;
            sceneData.rectangle.height = vec.y;

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            if (_hiddenScenes.Contains(sceneData.name))
                _hiddenScenes.Remove(sceneData.name);

            if (SceneManagerWindow.instance)
                SceneManagerWindow.instance.Repaint();
        }
    }
}