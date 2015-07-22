using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public sealed class SceneManager : MonoBehaviour
{
    [SerializeField]
    private SceneManagerAsset _sceneManagerAsset;
    private Dictionary<int, GameObject> _sceneBaseObjects;
    private HashSet<int> _activeScenes;
    private float _sqrBufferArea;

    public const float cBufferArea = 50.0f;

    private void Awake()
    {
        _sceneBaseObjects = new Dictionary<int, GameObject>();
        _activeScenes = new HashSet<int>();
        _sqrBufferArea = Mathf.Pow(cBufferArea, 2);
    }

    private void Start()
    {
        //  Setup the correct scenes
        var trackPos2D = targetPositionAsVec2;
        for (int i = 0; i < _sceneManagerAsset.Count; i++)
            CheckSceneState(ref i, ref trackPos2D);

        StartCoroutine(CalculateScenesStatesPeriodically());
    }

    private void LoadScene(int index)
    {
        if (_activeScenes.Contains(index))
            throw new UnityException("SCENEMANAGER::LOADSCENE::SCENE_ALREADY_ACTIVE");

#if UNITY_PRO_LICENCE
        Application.LoadLevelAdditiveAsync(_sceneManagerAsset[index].name);
#else
        Application.LoadLevelAdditive(_sceneManagerAsset[index].name);
#endif

        _activeScenes.Add(index);
    }

    private void UnloadScene(int sceneIndexInAsset)
    {
        GameObject go;

        if (_sceneBaseObjects.TryGetValue(sceneIndexInAsset, out go))
        {
            Destroy(go);
            _sceneBaseObjects.Remove(sceneIndexInAsset);
            _activeScenes.Remove(sceneIndexInAsset);
        }
        else
        {
            throw new UnityException("SCENEMANAGER::UNLOADSCENE::SCENE_ISNT_LOADED");
        }
    }

    public int ActiveSceneCount
    {
        get { return _activeScenes.Count; }
    }

    private IEnumerator CalculateScenesStatesPeriodically()
    {
        int i = 0;

        Vector2 trackPos2D;
        while(true)
        {
            trackPos2D = targetPositionAsVec2;
            CheckSceneState(ref i, ref trackPos2D);

            i = (i + 1) % _sceneManagerAsset.Count;
            if (i % 15 == 0)    //  Only check 15 at a time (loosely)
                yield return new WaitForEndOfFrame();
        }
    }

    private void CheckSceneState(ref int index, ref Vector2 trackedObjectPos)
    {
        var data = _sceneManagerAsset[index];
        var sqrRadius = Mathf.Pow(Mathf.Max(data.rectangle.width, data.rectangle.height) * .5f, 2);

        if (_activeScenes.Contains(index))
        {
            if ((data.rectangle.center - trackedObjectPos).sqrMagnitude > sqrRadius + _sqrBufferArea)
                UnloadScene(index);
        }
        else
        {
            if ((data.rectangle.center - trackedObjectPos).sqrMagnitude < sqrRadius + _sqrBufferArea)
                LoadScene(index);
        }
    }

    /// <summary>
    /// The tracked objects Vec2 position using their X,Y.
    /// If making a 3D game and using the system, you may want to use X,Z instead
    /// </summary>
    public Vector2 targetPositionAsVec2
    {
        get { return new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.y); }
    }

#if UNITY_EDITOR
    public SceneManagerAsset Asset
    {
        get { return _sceneManagerAsset; }
        set { _sceneManagerAsset = value; }
    }
#endif
}
