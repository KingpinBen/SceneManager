using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif


public class ManagedSceneObject : MonoBehaviour
{
    [SerializeField]
    private string _sceneName;
    [SerializeField]
    private Transform _sceneTraversables;

    private List<Renderer> _traversableRenderers;
    private SceneData _data;

    private void Awake()
    {
        if (!_sceneTraversables)
            throw new UnityException("MANAGEDSCENEOBJECT::AWAKE::SCENETRAVERSABLES_CHILD_IS_NULL");

        _traversableRenderers = new List<Renderer>();

        for(int i = 0; i < _sceneTraversables.childCount; i++)
        {
            var child = _sceneTraversables.GetChild(i);
            if (child.GetComponent<SceneTraversable>())
                _traversableRenderers.Add(child.GetComponent<Renderer>());
        }
    }

    private void Start()
    {
        _data = SceneManager.instance.RegisterSceneObject(this);
    }

    public void TryTransferTraversedObjects()
    {
        for(int i = _traversableRenderers.Count - 1; i >= 0; i--)
        {
            if (!_data.rectangle.Contains(_traversableRenderers[i].bounds) &&
                SceneManager.instance.TryTransferTraversable(_traversableRenderers[i]))
                _traversableRenderers.RemoveAt(i);
        }
    }

    public void ReceiveTraversableObject(Renderer renderer)
    {
        if (_traversableRenderers.Contains(renderer))
            throw new UnityException("MANAGEDSCENEOBJECT::RECEIVETRAVERSABLEOBJECT::ALREADY_TRACKED_IN_OBJECT");

        renderer.transform.parent = _sceneTraversables;
        _traversableRenderers.Add(renderer);
    }

    public string sceneName
    {
        get { return _sceneName; }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        _sceneName = 
            Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
    }
#endif
}

public static partial class Extensions
{
    public static bool Contains(this Rect rect, Bounds bounds)
    {
        //  Bottom left is bounds.min
        //  Top right is bounds.max

        //  Bounds left < rect.right
        //  Bounds right > rect.left
        //  Bounds top > rect.bottom
        //  Bounds bottom < rect.top

        if (bounds.min.x < rect.xMax &&
            bounds.max.x > rect.x &&
            bounds.max.y > rect.y - rect.height &&
            bounds.min.y < rect.y)
            return true;

        return false;
    }
}
