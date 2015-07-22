using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;

[InitializeOnLoad]
public static class SceneChange
{
    public static Action<string> OnSceneChange;

    private static string _lastKnownScene;

    static SceneChange()
    {
        _lastKnownScene = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
        EditorApplication.hierarchyWindowChanged += HeirarchyWindowChanged;
    }

    private static void HeirarchyWindowChanged()
    {
        var current = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
        if (current == _lastKnownScene)
            return;

        Debug.Log(string.Format("Scene changed from {0} to {1}", _lastKnownScene, current));

        if (OnSceneChange != null)
            OnSceneChange.Invoke(current);

        _lastKnownScene = current;
    }

    public static string Current
    {
        get { return _lastKnownScene; }
    }
}
