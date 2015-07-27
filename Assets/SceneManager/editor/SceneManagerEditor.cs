using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SceneManager))]
public class SceneManagerEditor : Editor
{
    private SceneManager _target;

    void OnEnable()
    {
        _target = target as SceneManager;
    }

    void OnSceneGUI()
    {
        if (!EditorApplication.isPlaying)
            return;

        Debug.Log("Drawing");
        Handles.CircleCap(0, _target.targetPositionAsVec2, Quaternion.identity, SceneManager.cBufferArea *.5f);
    }
}
