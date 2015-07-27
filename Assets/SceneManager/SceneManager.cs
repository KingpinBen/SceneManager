using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public sealed class SceneManager : MonoBehaviour
{
    [SerializeField]
    private SceneManagerAsset _sceneManagerAsset;
    private Dictionary<int, ManagedSceneObject> _sceneBaseObjects;
    private HashSet<int> _activeScenes;
    private float _sqrBufferArea;
    private float _sqrActiveArea;

    public const float cBufferArea = 80.0f;
    public const float cActiveArea = 40.0f;

    private void Awake()
    {
        if (_instance)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(this);

            _sceneBaseObjects = new Dictionary<int, ManagedSceneObject>();
            _activeScenes = new HashSet<int>();

            _sqrBufferArea = Mathf.Pow(cBufferArea, 2);
            _sqrActiveArea = Mathf.Pow(cActiveArea, 2);

            _instance = this;
        }
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
        ManagedSceneObject obj;

        if (_sceneBaseObjects.TryGetValue(sceneIndexInAsset, out obj))
        {
            Destroy(obj.gameObject);
            _sceneBaseObjects.Remove(sceneIndexInAsset);
            _activeScenes.Remove(sceneIndexInAsset);
        }
    }

    public SceneData RegisterSceneObject(ManagedSceneObject obj)
    {
        if (obj == null)
            throw new UnityException("SCENEMANAGER::REGISTERSCENEOBJECT::MANAGEDSCENEOBJECT_IS_NULL");

        int index;
        if (!_sceneManagerAsset.TryFindSceneIndex(obj.sceneName, out index))
            throw new UnityException("SCENEMANAGER::REGISTERSCENEOBJECT::SCENEMANAGERASSET_DOESNT_CONTAIN_VALUE");

        _sceneBaseObjects[index] = obj;
        return _sceneManagerAsset[index];
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
        var closestPoint = data.rectangle.ClosestPoint(trackedObjectPos);

        if (_activeScenes.Contains(index))
        {
            //  Scene hasn't yet fully registered.
            if (!_sceneBaseObjects.ContainsKey(index))
                return;

            if (_sceneBaseObjects[index].gameObject.activeSelf)
            {
                if ((closestPoint - trackedObjectPos).sqrMagnitude > _sqrActiveArea)
                {
                    _sceneBaseObjects[index].TryTransferTraversedObjects();
                    _sceneBaseObjects[index].gameObject.SetActive(false);
                }
            }
            else
            {
                if ((closestPoint - trackedObjectPos).sqrMagnitude < _sqrActiveArea)
                {
                    _sceneBaseObjects[index].gameObject.SetActive(true);
                }
                else
                {
                    if ((closestPoint - trackedObjectPos).sqrMagnitude > _sqrBufferArea)
                    {
                        _sceneBaseObjects[index].TryTransferTraversedObjects();
                        UnloadScene(index);
                    }
                }
            }
        }
        else
        {
            if ((closestPoint - trackedObjectPos).sqrMagnitude < _sqrBufferArea)
                LoadScene(index);
        }
    }

    public bool TryTransferTraversable(Renderer traversableRenderer)
    {
        for(int i = 0; i <_activeScenes.Count; i++)
        {
            Debug.Log(string.Format("{0}, {1} | {2}, {3}",
                _sceneManagerAsset[i].x,
                _sceneManagerAsset[i].y,
                _sceneManagerAsset[i].xMax,
                _sceneManagerAsset[i].yMax));

            if (_sceneManagerAsset[i].rectangle.Contains(traversableRenderer.bounds))
            {
                _sceneBaseObjects[i].ReceiveTraversableObject(traversableRenderer);
                Debug.Log(string.Format("Transferring '{0}' to scene: '{1}'", traversableRenderer.name, _sceneManagerAsset[i].name));
                return true;
            }
        }

        return false;
    }

    public int ActiveSceneCount
    {
        get { return _activeScenes.Count; }
    }

    private static SceneManager _instance;
    public static SceneManager instance
    {
        get { return _instance; }
        private set
        {
            _instance = value;
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

#if DEBUG_SCREENMANAGER && UNITY_EDITOR
    private void OnDrawGizmos()
    {
        for (int i = 0; i < _sceneManagerAsset.Count; i++)
        {
            Gizmos.DrawLine(targetPositionAsVec2, ClosestPointToRect(ref _sceneManagerAsset[i].rectangle, targetPositionAsVec2));
            Gizmos.DrawWireCube(_sceneManagerAsset[i].alteredCenter, new Vector3(_sceneManagerAsset[i].rectangle.width, _sceneManagerAsset[i].rectangle.height));
        }
    }
#endif
}
