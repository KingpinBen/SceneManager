using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif


public class ManagedSceneObject : MonoBehaviour
{
    [SerializeField]
    private string _sceneName;

    private void Start()
    {
        SceneManager.instance.RegisterSceneObject(this);
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
