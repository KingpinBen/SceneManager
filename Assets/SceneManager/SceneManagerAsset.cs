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

    public int AddNewSceneData()
    {
        
        var defaultDimensions = new Vector2(100, 50);
        var index = _sceneData.Length;
        var newData = new SceneData();

        newData.rectangle = new Rect(-defaultDimensions.x * .5f, -defaultDimensions.y * .5f, defaultDimensions.x, defaultDimensions.y);

        ArrayUtility.Add(ref _sceneData, newData);
        return index;
    }
#endif
}

[Serializable]
public class SceneData
{
    public string name;
    public Rect rectangle;

    public Vector2 alteredCenter
    {
        get { return new Vector2(rectangle.center.x, rectangle.y - rectangle.height * .5f); }
    }
}
