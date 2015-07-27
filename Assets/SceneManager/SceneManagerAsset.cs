using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class SceneManagerAsset : ScriptableObject
{
    [SerializeField]
    private SceneData[] _sceneData;

    public int Count
    {
        get
        {
            return _sceneData.Length;
        }
    }

    public SceneData this[int index]
    {
        get { return _sceneData[index]; }
        set { _sceneData[index] = value; }
    }

    public SceneData TryFindScene(string name)
    {
        int index;
        if (TryFindSceneIndex(name, out index))
            return _sceneData[index];

        return null;
    }

    public bool TryFindSceneIndex(string name, out int index)
    {
        index = -1;
        if (string.IsNullOrEmpty(name))
            return false;

        for (int i = 0; i < _sceneData.Length; i++)
        {
            if (_sceneData[i].name == name)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR

    [MenuItem("SceneManager/Create Scene Collection Asset")]
    public static void CreateSceneManagerAsset()
    {
        var asset = CreateInstance<SceneManagerAsset>();
        var pathAndName = AssetDatabase.GenerateUniqueAssetPath(string.Format("Assets/New {0}.asset", typeof(SceneManagerAsset).ToString()));
        AssetDatabase.CreateAsset(asset, pathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    public int AddNewSceneData()
    {
        var index = _sceneData.Length;
        var newData = new SceneData();

        ArrayUtility.Add(ref _sceneData, newData);
        return index;
    }

    public void RemoveAt(int index)
    {
        ArrayUtility.RemoveAt(ref _sceneData, index);
    }
#endif
}

[Serializable]
public class SceneData
{
    public string name;
    [SerializeField]
    private Rect _rectangle;

    public SceneData()
    {
        _rectangle = new Rect(0, 0, 20, 20);
    }

    public Vector2 alteredCenter
    {
        get { return new Vector2(_rectangle.center.x, _rectangle.y - _rectangle.height * .5f); }
    }

    public int x
    {
        get { return (int)_rectangle.x; }
        set { _rectangle.x = value; }
    }

    public int xMax
    {
        get { return (int)_rectangle.xMax; }
        set { _rectangle.xMax = value; }
    }

    public int y
    {
        get { return (int)_rectangle.y; }
        set { _rectangle.y = value; }
    }

    public int yMax
    {
        get { return (int)_rectangle.yMax; }
        set { _rectangle.yMax = value; }
    }

    public int width
    {
        get { return (int)_rectangle.width; }
        set { _rectangle.width = Mathf.Max(Mathf.RoundToInt(value), 10); }
    }

    public int height
    {
        get { return (int)_rectangle.height; }
        set { _rectangle.height = Mathf.Max(Mathf.RoundToInt(value), 10); }
    }

    public Rect rectangle
    {
        get { return _rectangle; }
    }
}
