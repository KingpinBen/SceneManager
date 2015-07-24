#define _DEBUG

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

public sealed class SceneManagerWindow : EditorWindow
{
    [SerializeField]
    private SceneManagerAsset _asset;

    private SceneCollection _sceneCollection;
    private ScenePreviewCollection _previewCollection;
    private string _scenePreviewImagesFolder;
    private Camera _windowCamera;
    private int _selectedSceneIndex = -1;
    private int _hoverSceneIndex = -1;
    private bool _ignoreSceneChanges = false;
    private WindowsSettingFlags _flags;
    private Vector2 _windowContentsOffset;
    private Rect _scenesMinMaxSize = new Rect();

    [MenuItem("Window/SceneManager Window")]
    private static void CreateWindow()
    {
        var window = CreateInstance<SceneManagerWindow>();
        window.Show();
    }

    private void OnEnable()
    {
        _sceneCollection = new SceneCollection();
        _previewCollection = new ScenePreviewCollection(_sceneCollection);

        titleContent = new GUIContent("Scene Man");

        _sceneCollection.UpdateRelativePaths();

        SceneView.onSceneGUIDelegate += OnSceneGUI;
        SceneChange.OnSceneChange += UpdateScenePreviewImage;

        ShowScenePreviews = true;

        CalculateMinMaxSceneSizes();
        _windowContentsOffset = _scenesMinMaxSize.center;
    }

    private void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneChange.OnSceneChange -= UpdateScenePreviewImage;
    }

    private void OnGUI()
    {
        HandleSceneManagerAssetDrop();

        EditorGUILayout.BeginVertical();

        HandleMouseDrag();

        if (_asset)
            DrawSceneMap();

        if (ShowOptions)
            DrawOptionsPopup();

        DrawToolbar();

        EditorGUILayout.EndVertical();
    }

    private void HandleMouseDrag()
    {
        var e = Event.current;

        CalculateMinMaxSceneSizes();

        if (e.type == EventType.mouseDrag && e.button > 0)
        {
            _windowContentsOffset.x = Mathf.Clamp(_windowContentsOffset.x + e.delta.x, -position.width*.5f-_scenesMinMaxSize.xMin, position.width * .5f - _scenesMinMaxSize.xMax);
            _windowContentsOffset.y = Mathf.Clamp(_windowContentsOffset.y + e.delta.y, -position.height * .5f + _scenesMinMaxSize.yMin, position.height * .5f - _scenesMinMaxSize.yMax);

            Repaint();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (_asset)
        {
            GUI.enabled = !EditorApplication.isPlaying;
            if (GUILayout.Button("New SceneData", EditorStyles.toolbarButton, new GUILayoutOption[0]))
            {
                _asset.AddNewSceneData();

                EditorGUIUtility.PingObject(_asset);
                Selection.activeObject = _asset;

                EditorUtility.SetDirty(_asset);
            }

            if (GUILayout.Button("Update Previews", EditorStyles.toolbarButton, new GUILayoutOption[0]))
            {
                UpdateTexturePreviewCache();
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();

            ShowOptions = GUILayout.Toggle(ShowOptions, "Options", EditorStyles.toolbarButton, new GUILayoutOption[0]);
        }
        else
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Drag a SceneManagerAsset onto the window");
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSceneMap()
    {
        int i;
        Rect rectWithOffset = new Rect();

        //  I chose the method of a switch with loops in each to avoid 
        //  unnecessary Begin/EndGUI() calls
        switch (Event.current.type)
        {
            case EventType.mouseMove:
                _hoverSceneIndex = -1;
                for (i = 0; i < _asset.Count; i++)
                {
                    rectWithOffset = GetRectWithScreenOffset(_asset[i].rectangle);

                    if (rectWithOffset.Contains(Event.current.mousePosition))
                    {
                        _hoverSceneIndex = i;
                        break;
                    }
                }
                break;
            case EventType.mouseDown:
                //  The left button has been clicked so deselect the current scene if it exists
                if (Event.current.button == 0)
                    _selectedSceneIndex = -1;

                for (i = 0; i < _asset.Count; i++)
                {
                    rectWithOffset = GetRectWithScreenOffset(_asset[i].rectangle);

                    if (rectWithOffset.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                        {
                            _selectedSceneIndex = i;

                            if (Event.current.clickCount == 2)
                            {
                                if (_sceneCollection.SceneExists(_asset[i].name) &&
                                    EditorUtility.DisplayDialog("Open Scene",
                                        "Are you sure you want to open the scene?", "Yes", "No"))
                                {
                                    EditorApplication.OpenScene(_sceneCollection.GetRelativeScenePath(_asset[i].name));
                                }
                            }
                        }
                    }
                }
                break;
            case EventType.repaint:
                SceneData data = null;
                Handles.BeginGUI();
                for (i = 0; i < _asset.Count; i++)
                {
                    data = _asset[i];
                    rectWithOffset = GetRectWithScreenOffset(_asset[i].rectangle);

                    if (ShowScenePreviews && !string.IsNullOrEmpty(data.name) && _previewCollection.ContainsKey(data.name))
                    {
                        if (_previewCollection.ContainsKey(data.name))
                            GUI.DrawTexture(rectWithOffset, _previewCollection[data.name]);
                    }

                    if (ShowSceneNames)
                        GUI.Label(rectWithOffset, string.IsNullOrEmpty(data.name) ? "<NO SCENE>" : data.name);

                    DrawSceneRectOutline(rectWithOffset, i);
                }
                Handles.EndGUI();
                break;

        }
    }

    private Rect GetRectWithScreenOffset(SceneData data)
    {
        return GetRectWithScreenOffset(data.rectangle);
    }

    private Rect GetRectWithScreenOffset(Rect rect)
    {
        var result = new Rect();
        result.x = rect.x + position.width * .5f + _windowContentsOffset.x;
        result.y = position.height * .5f - rect.y + _windowContentsOffset.y;
        result.width  = rect.width;
        result.height = rect.height;

        return result;
    }

    private void DrawSceneRectOutline(Rect rect, int index)
    {
        if (!_asset)
            return;

        Color oldCol = Handles.color;
        Handles.color = 
            _selectedSceneIndex == index ? Color.red : 
            _hoverSceneIndex    == index ? Color.yellow : Color.white;

        for(int i = 0; i < _asset.Count; i++)
        {
            var verts = new[]
        {
            new Vector2(rect.x, rect.y),
            new Vector2(rect.x + rect.width, rect.y),
            new Vector2(rect.x + rect.width, rect.y + rect.height),
            new Vector2(rect.x, rect.y + rect.height)
        };

            Handles.DrawLine(verts[0], verts[1]);
            Handles.DrawLine(verts[1], verts[2]);
            Handles.DrawLine(verts[2], verts[3]);
            Handles.DrawLine(verts[3], verts[0]);
        }

        Handles.color = oldCol;
    }

    private void CalculateMinMaxSceneSizes()
    {
        if (_asset.Count == 0)
            return;

        Rect rect;
        rect = _asset[0].rectangle;

        _scenesMinMaxSize.xMin = rect.x;
        _scenesMinMaxSize.xMax = rect.x + rect.width;
        _scenesMinMaxSize.yMin = rect.y;
        _scenesMinMaxSize.yMax = rect.y + rect.height;

        for (int i = 1; i < _asset.Count; i++)
        {
            rect = _asset[i].rectangle;

            //  Left
            if (rect.xMin < _scenesMinMaxSize.xMin)
                _scenesMinMaxSize.xMin = rect.xMin;
            //  Right
            if (rect.xMax > _scenesMinMaxSize.xMax)
                _scenesMinMaxSize.xMax = rect.xMax;

            //  Top
            if (rect.yMin > _scenesMinMaxSize.yMin)
                _scenesMinMaxSize.yMin = rect.yMin;
            //  Bottom
            var bot = rect.y + rect.height;
            if (bot < _scenesMinMaxSize.yMax)
                _scenesMinMaxSize.yMax = bot;
        }
    }

    private void DrawOptionsPopup()
    {
        using (var scope = new EditorGUILayout.VerticalScope("Box", GUILayout.Width(350)))
        {
            if (_asset)
            {
                GUILayout.Label(string.Format("{0} : [{1} Scenes Total]", _asset.name, _asset.Count));
                ShowSceneNames = GUILayout.Toggle(ShowSceneNames, "Show Scene Names");
                ShowScenePreviews = GUILayout.Toggle(ShowScenePreviews, "Show Scene Previews");
                ShowSurroundingScenesInSceneView = GUILayout.Toggle(ShowSurroundingScenesInSceneView, "Show Surrounding Scene Previews in SceneView");
            }
            else
            {
                GUILayout.Label("No asset assigned. Drag one onto the window");
            }
        }
    }

    private void SetUpCamera()
    {
        if (!_windowCamera)
        {
            var newCamera = new GameObject();
            newCamera.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(newCamera);

            _windowCamera = newCamera.AddComponent<Camera>();
            _windowCamera.backgroundColor = new Color(49, 77, 121, 1);
            _windowCamera.clearFlags = CameraClearFlags.Skybox;
        }

        _windowCamera.enabled = false;
        _windowCamera.orthographic = true;
    }

    private void UpdateTexturePreviewCache()
    {
        if (_asset == null)
            return;

        if (_ignoreSceneChanges)
            return;
        _ignoreSceneChanges = true;

        SetUpCamera();

        EditorApplication.SaveCurrentSceneIfUserWantsTo();
        var startScene = EditorApplication.currentScene;
        
        for (int i = 0; i < _sceneCollection.Count; i++)
        {
            var sceneData = _asset.TryFindScene(_sceneCollection[i]);

            if (sceneData == null)
                continue;

            if (!_sceneCollection.SceneExists(sceneData.name))
                continue;

            if (EditorApplication.currentScene != _sceneCollection.GetRelativeScenePath(sceneData.name))
                EditorApplication.OpenScene(_sceneCollection.GetRelativeScenePath(sceneData.name));

            UpdateScenePreviewImage(sceneData);
        }

        Resources.UnloadUnusedAssets();
        if (EditorApplication.currentScene != startScene)
            EditorApplication.OpenScene(startScene);

        _ignoreSceneChanges = false;
        Repaint();
    }

    private void UpdateScenePreviewImage(string sceneName)
    {
        var scene = _asset.TryFindScene(sceneName);
        if (scene != null)
            UpdateScenePreviewImage(scene);
    }

    private void UpdateScenePreviewImage(SceneData sceneData)
    {
        SetUpCamera();

        _windowCamera.aspect = sceneData.rectangle.width / sceneData.rectangle.height;
        _windowCamera.orthographicSize = sceneData.rectangle.height * .5f;
        _windowCamera.transform.position = new Vector3(sceneData.rectangle.center.x, sceneData.rectangle.y - sceneData.rectangle.height * .5f, -10);

        var tempRT = RenderTexture.GetTemporary((int)sceneData.rectangle.width, (int)sceneData.rectangle.height, 16);
        RenderTexture.active = tempRT;

        _windowCamera.targetTexture = tempRT;
        _windowCamera.Render();

        var image = new Texture2D(tempRT.width, tempRT.height, TextureFormat.ARGB32, false);
        image.hideFlags = HideFlags.HideAndDontSave;
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0, false);
        image.Apply();

        _previewCollection.Add(sceneData.name, image);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempRT);
    }

    private void HandleSceneManagerAssetDrop()
    {
        if (DragAndDrop.objectReferences.Length == 0)
            return;

        Event e = Event.current;

        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            var acceptableDrag = DragAndDrop.objectReferences.Length == 1 &&
                DragAndDrop.objectReferences[0] is SceneManagerAsset;

            DragAndDrop.visualMode = acceptableDrag ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;

            if (acceptableDrag && e.type == EventType.DragPerform)
            {
                SetSceneManagerAsset(DragAndDrop.objectReferences[0] as SceneManagerAsset);
                DragAndDrop.AcceptDrag();
            }

            e.Use();
        }
    }

    private void SetSceneManagerAsset(SceneManagerAsset asset)
    {
        _asset = asset;

        Repaint();
    }

    private void OnSceneGUI(SceneView sceneview)
    {
        Rect rect = new Rect();
        var oldCol = Handles.color;
        var data = _asset.TryFindScene(Path.GetFileNameWithoutExtension(EditorApplication.currentScene));
        if (data == null)
        {
            Handles.BeginGUI();
            GUILayout.Box("Scene wasn't found in the SceneManagerAsset. Cannot display boundaries.");
            Handles.EndGUI();
        }
        else
        {
            rect = data.rectangle;
 
            var verts = new[]
            {
            new Vector2(rect.x, rect.y),
            new Vector2(rect.x + rect.width, rect.y),
            new Vector2(rect.x + rect.width, rect.y - rect.height),
            new Vector2(rect.x, rect.y - rect.height)
        };

            Handles.color = Color.red;
            Handles.DrawDottedLine(verts[0], verts[1], 5.0f);
            Handles.DrawDottedLine(verts[1], verts[2], 5.0f);
            Handles.DrawDottedLine(verts[2], verts[3], 5.0f);
            Handles.DrawDottedLine(verts[3], verts[0], 5.0f);
        }


        if (ShowSurroundingScenesInSceneView)
        {
            Handles.BeginGUI();

            GUI.color = Color.white * .75f;
            for (int i = 0; i < _asset.Count; i++)
            {
                if (SceneChange.Current == _asset[i].name)
                    continue;

                if (!_previewCollection.ContainsKey(_asset[i].name))
                    continue;

                rect = _asset[i].rectangle;

                var tl = SceneView.currentDrawingSceneView
                    .camera.WorldToScreenPoint(new Vector3(rect.x, rect.y, 0));
                var br = SceneView.currentDrawingSceneView
                    .camera.WorldToScreenPoint(new Vector3(rect.x + rect.width, rect.y + rect.height, 0));

                rect = new Rect(new Vector3(tl.x, sceneview.camera.pixelHeight - tl.y), new Vector3(br.x - tl.x, br.y - tl.y));
                
                GUI.DrawTexture(rect, _previewCollection[_asset[i].name]);
            }

            Handles.EndGUI();
        }
        
        Handles.color = oldCol;
    }

    [Flags]
    private enum WindowsSettingFlags
    {
        ShowOptions = 1,
        ShowSceneNames = 2,
        ShowScenePreviews = 4,
        ShowSurroundingScenesInSceneView = 8,
    }

    private bool ShowOptions
    {
        get { return (_flags & WindowsSettingFlags.ShowOptions) != 0; }
        set
        {
            if (value)
                _flags |= WindowsSettingFlags.ShowOptions;
            else
                _flags &= ~WindowsSettingFlags.ShowOptions;
        }
    }

    private bool ShowSceneNames
    {
        get { return (_flags & WindowsSettingFlags.ShowSceneNames) != 0; }
        set
        {
            if (value)
                _flags |= WindowsSettingFlags.ShowSceneNames;
            else
                _flags &= ~WindowsSettingFlags.ShowSceneNames;
        }
    }

    private bool ShowScenePreviews
    {
        get { return (_flags & WindowsSettingFlags.ShowScenePreviews) != 0; }
        set
        {
            if (value)
                _flags |= WindowsSettingFlags.ShowScenePreviews;
            else
                _flags &= ~WindowsSettingFlags.ShowScenePreviews;
        }
    }

    private bool ShowSurroundingScenesInSceneView
    {
        get { return (_flags & WindowsSettingFlags.ShowSurroundingScenesInSceneView) != 0; }
        set
        {
            if (value)
                _flags |= WindowsSettingFlags.ShowSurroundingScenesInSceneView;
            else
                _flags &= ~WindowsSettingFlags.ShowSurroundingScenesInSceneView;
        }
    }
}
