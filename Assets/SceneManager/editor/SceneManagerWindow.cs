#define _DEBUG

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;

public sealed class SceneManagerWindow : EditorWindow
{
    [SerializeField]
    private SceneCollectionAsset _asset;

    private ProjectSceneCollection _projectSceneCollection;
    private ScenePreviewCollection _previewCollection;
    private string _scenePreviewImagesFolder;
    private Camera _windowCamera;
    private int _selectedSceneIndex = -1;
    private int _hoverSceneIndex = -1;
    private bool _ignoreSceneChanges = false;
    private WindowsSettingFlags _flags;
    private Vector2 _windowContentsOffset;
    private Rect _scenesMinMaxSize = new Rect();
    private DraggingEdge _dragging;
    private bool _snapping;
    private Vector3[] _vertexCache = new Vector3[4];

    private const int _cSnapDistance = 10;
    private const int _cClickableEdgeDistance = 6;
    private static SceneManagerWindow s_window;

    [MenuItem("SceneManager/Scene Map Window")]
    private static void CreateWindow()
    {
        s_window = GetWindow<SceneManagerWindow>();
        s_window.Show();
    }

    private void OnEnable()
    {
        _projectSceneCollection = new ProjectSceneCollection();
        _previewCollection = new ScenePreviewCollection(_projectSceneCollection);

        titleContent = new GUIContent("Scene Map");

        _projectSceneCollection.UpdateRelativePaths();

        SceneView.onSceneGUIDelegate += OnSceneGUI;
        SceneChange.OnSceneChange += UpdateScenePreviewImage;
        Undo.undoRedoPerformed += Repaint;

        ShowScenePreviews = true;
        wantsMouseMove = true;

        CalculateMinMaxSceneSizes();
        _windowContentsOffset = _scenesMinMaxSize.center;

        if (!s_window)
            s_window = this;
    }

    private void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneChange.OnSceneChange -= UpdateScenePreviewImage;
        Undo.undoRedoPerformed -= Repaint;
    }

    private void OnGUI()
    {
        HandleSceneManagerAssetDrop();

        EditorGUILayout.BeginVertical();

        HandleMouseDrag();

        if (_asset)
        {
            HandleMap();
            HandleSelectedScene();
        }

        if (ShowOptions)
            DrawOptionsPopup();

        DrawToolbar();

        EditorGUILayout.EndVertical();
    }

    private void OnSceneGUI(SceneView sceneview)
    {
        //  Current scene isn't saved so don't try to find it.
        if (string.IsNullOrEmpty(EditorApplication.currentScene) || !_asset)
            return;

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

            _vertexCache[0] = new Vector2(data.x, data.y);
            _vertexCache[1] = new Vector2(data.x + data.width, data.y);
            _vertexCache[2] = new Vector2(data.x + data.width, data.y - data.height);
            _vertexCache[3] = new Vector2(data.x, data.y - data.height);

            Handles.DrawLine(_vertexCache[0], _vertexCache[1]);
            Handles.DrawLine(_vertexCache[1], _vertexCache[2]);
            Handles.DrawLine(_vertexCache[2], _vertexCache[3]);
            Handles.DrawLine(_vertexCache[3], _vertexCache[0]);

            Handles.color = Color.red;

            Handles.DrawDottedLine(_vertexCache[0], _vertexCache[1], 3.0f);
            Handles.DrawDottedLine(_vertexCache[1], _vertexCache[2], 3.0f);
            Handles.DrawDottedLine(_vertexCache[2], _vertexCache[3], 3.0f);
            Handles.DrawDottedLine(_vertexCache[3], _vertexCache[0], 3.0f);
        }

        var rect = new Rect();
        if (ShowSurroundingScenesInSceneView)
        {
            Handles.BeginGUI();

            GUI.color = Color.white * .75f;
            for (int i = 0; i < _asset.Count; i++)
            {
                data = _asset[i];

                if (SceneChange.Current == data.name)
                    continue;

                if (!_previewCollection.ContainsKey(data.name))
                    continue;



                var tl = SceneView.currentDrawingSceneView
                    .camera.WorldToScreenPoint(new Vector3(data.x, data.y, 0));
                var br = SceneView.currentDrawingSceneView
                    .camera.WorldToScreenPoint(new Vector3(data.x + data.width, data.y + data.height, 0));

                rect = new Rect(new Vector3(tl.x, sceneview.camera.pixelHeight - tl.y), new Vector3(br.x - tl.x, br.y - tl.y));

                GUI.DrawTexture(rect, _previewCollection[_asset[i].name]);
            }

            Handles.EndGUI();
        }

        Handles.color = oldCol;
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

                _selectedSceneIndex = _asset.Count - 1;

                EditorUtility.SetDirty(_asset);
            }

            if (GUILayout.Button("Update Previews", EditorStyles.toolbarButton, new GUILayoutOption[0]))
            {
                UpdateTexturePreviewCache();
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("Focus Active Asset", EditorStyles.toolbarButton, new GUILayoutOption[0]))
            {
                Selection.activeObject = _asset;
                EditorUtility.FocusProjectWindow();
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();

            ShowOptions = GUILayout.Toggle(ShowOptions, "Options", EditorStyles.toolbarButton, new GUILayoutOption[0]);
        }
        else
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Drag a SceneCollectionAsset onto the window");
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Handles various event types concerning the cells in the window
    /// containing the map preview and layout. 
    /// </summary>
    private void HandleMap()
    {
        int i;
        Rect rectWithOffset = new Rect();

        //  I chose the method of a switch with loops in each to avoid 
        //  unnecessary Begin/EndGUI() calls

        switch (Event.current.type)
        {
            case EventType.MouseMove:
                _hoverSceneIndex = -1;
                
                for (i = 0; i < _asset.Count; i++)
                {
                    rectWithOffset = GetRectWithScreenOffset(_asset[i].rectangle);

                    if (rectWithOffset.Contains(Event.current.mousePosition))
                    {
                        _hoverSceneIndex = i;
                        Repaint();
                        break;
                    }
                }
                break;
            case EventType.mouseDown:
                //  The left button has been clicked so deselect the current scene if it exists
                if (Event.current.button == 0)
                    _selectedSceneIndex = -1;
                else break;

                for (i = 0; i < _asset.Count; i++)
                {
                    rectWithOffset = GetRectWithScreenOffset(_asset[i].rectangle);

                    if (rectWithOffset.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                        {
                            _selectedSceneIndex = i;
                            _dragging = DraggingEdge.None;
                            Selection.activeObject = _asset;

                            if (Event.current.clickCount == 2)
                            {
                                if (_projectSceneCollection.SceneExists(_asset[i].name) &&
                                    EditorUtility.DisplayDialog("Open Scene",
                                        "Are you sure you want to open the scene?", "Yes", "No"))
                                {
                                    EditorApplication.OpenScene(_projectSceneCollection.GetRelativeScenePath(_asset[i].name));
                                }
                            }
                            else
                            {
                                Repaint();
                            }
                        }
                    }
                }
                break;
            case EventType.repaint:

                if (_hoverSceneIndex >= _asset.Count)
                    _hoverSceneIndex = -1;
                
                Handles.BeginGUI();

                for (i = 0; i < _asset.Count; i++)
                {
                    if (i == _selectedSceneIndex ||
                        i == _hoverSceneIndex)
                        continue;

                    DrawScene(_asset[i], Color.white);
                }

                if (_hoverSceneIndex >= 0)
                    DrawScene(_asset[_hoverSceneIndex], Color.yellow);

                if (_selectedSceneIndex >= 0)
                    DrawScene(_asset[_selectedSceneIndex], Color.red);

                Handles.EndGUI();
                break;

        }
    }

    /// <summary>
    /// Handles code relating to the currently selected scene (if it exists).
    /// 
    /// </summary>
    private void HandleSelectedScene()
    {
        if (_selectedSceneIndex >= _asset.Count)
            _selectedSceneIndex = -1;

        if (_selectedSceneIndex < 0)
            return;

        if (Event.current.type == EventType.repaint || Event.current.type == EventType.layout)
        {
            if (ShowSelectedSceneInfo)
                DrawSceneInfoPopup();
        }

        HandleSelectedSceneDragging();
    }

    private void HandleSelectedSceneDragging()
    {
        switch (Event.current.type)
        {
            #region Handles clicking on the edge to be draggable
            case EventType.mouseUp:
                if (Event.current.button == 0)
                {
                    _dragging = DraggingEdge.None;
                    Repaint();
                }
                break;
            case EventType.mouseDown:
                var mousePos = mousePositionInWorld;
                var dataRect = _asset[_selectedSceneIndex].rectangle;
                if (Mathf.Abs(mousePos.x - dataRect.x) < _cClickableEdgeDistance)
                    _dragging = DraggingEdge.Left;
                else
                {
                    if (Mathf.Abs(mousePos.x - (dataRect.x + dataRect.width)) < _cClickableEdgeDistance)
                        _dragging = DraggingEdge.Right;
                    else
                    {
                        if (Mathf.Abs(mousePos.y - dataRect.y) < _cClickableEdgeDistance)
                            _dragging = DraggingEdge.Top;
                        else
                        {
                            if (Mathf.Abs(mousePos.y - (dataRect.y - dataRect.height)) < _cClickableEdgeDistance)
                                _dragging = DraggingEdge.Bottom;
                            else
                                _dragging = DraggingEdge.None;
                        }
                    }
                }
                
                break;
            #endregion
            #region Draw the edge being dragged (if any)
            case EventType.repaint:
                if (_dragging == DraggingEdge.None)
                    return;

                Rect rect = GetRectWithScreenOffset(_asset[_selectedSceneIndex]);

                switch (_dragging)
                {
                    case DraggingEdge.Left:
                        _vertexCache[0] = new Vector3(rect.x + 1, rect.y + 1);
                        _vertexCache[1] = new Vector3(rect.x + _cClickableEdgeDistance - 1, rect.y + 1);
                        _vertexCache[2] = new Vector3(rect.x + _cClickableEdgeDistance - 1, rect.yMax - 1);
                        _vertexCache[3] = new Vector3(rect.x + 1, rect.yMax - 1);
                        break;
                    case DraggingEdge.Top:
                        _vertexCache[0] = new Vector3(rect.x + 1, rect.y + 1);
                        _vertexCache[1] = new Vector3(rect.x + rect.width, rect.y + 1);
                        _vertexCache[2] = new Vector3(rect.x + rect.width, rect.y + _cClickableEdgeDistance - 1);
                        _vertexCache[3] = new Vector3(rect.x + 1, rect.y + _cClickableEdgeDistance - 1);
                        break;
                    case DraggingEdge.Right:
                        _vertexCache[0] = new Vector3(rect.x + rect.width - _cClickableEdgeDistance + 1, rect.y + 1);
                        _vertexCache[1] = new Vector3(rect.x + rect.width, rect.y + 1);
                        _vertexCache[2] = new Vector3(rect.x + rect.width, rect.yMax - 1);
                        _vertexCache[3] = new Vector3(rect.x + rect.width - _cClickableEdgeDistance + 1, rect.yMax - 1);
                        break;
                    case DraggingEdge.Bottom:
                        _vertexCache[0] = new Vector3(rect.x + 1, rect.yMax - _cClickableEdgeDistance + 1);
                        _vertexCache[1] = new Vector3(rect.xMax, rect.yMax - _cClickableEdgeDistance + 1);
                        _vertexCache[2] = new Vector3(rect.xMax, rect.yMax - 1);
                        _vertexCache[3] = new Vector3(rect.x + 1, rect.yMax - 1);
                        break;

                }

                Handles.DrawSolidRectangleWithOutline(_vertexCache, Color.cyan * .7f, Color.clear);
                break;
                #endregion
        }

        HandleDragSnappingScene();
        HandleResizeDraggingScene();
    }

    private void HandleResizeDraggingScene()
    {
        if (Event.current.type != EventType.mouseDrag)
            return;

        if (_dragging == DraggingEdge.None)
            return;

        if (_snapping)
            return;

        var data = _asset[_selectedSceneIndex];
        Undo.RecordObject(_asset, "Resize");

        if (_dragging == DraggingEdge.Left || _dragging == DraggingEdge.Top)
        {
            float oldValue;
            if (_dragging == DraggingEdge.Left)
            {
                oldValue = data.xMax;

                data.x = (int)mousePositionInWorld.x;
                data.xMax -= (int)(data.xMax - oldValue);
            }
            else
            {
                oldValue = data.y;

                data.y = (int)mousePositionInWorld.y;
                data.height += (int)(data.y - oldValue);
            }
        }
        else
        {
            if (_dragging == DraggingEdge.Right)
                data.width = (int)(mousePositionInWorld.x - data.x);
            else
                data.height = (int)(data.y - mousePositionInWorld.y);
        }

        Repaint();
    }

    private void HandleDragSnappingScene()
    {
        if (_dragging == DraggingEdge.None)
            return;

        _snapping = false;

        if (!(Event.current.modifiers == EventModifiers.Control ||
            Event.current.modifiers == EventModifiers.Command))
            return;

        Rect closestRect;
        Vector2 closestPoint;
        Vector2 mousePos = Event.current.mousePosition;
        var selectedData = _asset[_selectedSceneIndex];
        
        for (int i = 0; i < _asset.Count; i++)
        {
            if (i == _selectedSceneIndex)
                continue;

            closestRect = GetRectWithScreenOffset(_asset[i]);
            
            //  We need to add the height to flip the rect up to get it to the correct 
            //  coords. When we're working with the y again, we need to flip it back
            closestRect.y += closestRect.height;
            closestPoint = closestRect.ClosestPoint(Event.current.mousePosition);

            if ((closestPoint - mousePos).magnitude > _cClickableEdgeDistance)
                continue;

            switch(_dragging)
            {
                case DraggingEdge.Top:
                case DraggingEdge.Bottom:
                    closestRect.y -= closestRect.height;
                    var y = Mathf.Abs(mousePos.y - closestRect.y) < _cSnapDistance;

                    //  Save y bool as we can use it later, second conditional is yMax check. 
                    //  We can just use !y past this if as one of them is true
                    if (!y && !(Mathf.Abs(mousePos.y - closestRect.yMax) < _cSnapDistance))
                        continue;

                    _snapping = true;

                    if (_dragging == DraggingEdge.Top)
                    {
                        int oldValue = selectedData.y;
                        if (y)
                            selectedData.y = _asset[i].y;
                        else
                            selectedData.y = _asset[i].y - _asset[i].height;

                        selectedData.height += (selectedData.y - oldValue);
                        Repaint();
                    }
                    else
                    {
                        if (y)
                            selectedData.height = (selectedData.y - _asset[i].y);
                        else
                            selectedData.height = (selectedData.y - _asset[i].y) + _asset[i].height;

                        Repaint();
                    }
                    break;
                case DraggingEdge.Left:
                case DraggingEdge.Right:
                    var x = Mathf.Abs(mousePos.x - closestRect.x) < _cSnapDistance;
                    if (!x && !(Mathf.Abs(mousePos.x - closestRect.xMax) < _cSnapDistance))
                        continue;

                    if (_dragging == DraggingEdge.Left)
                    {
                        int oldValue = selectedData.x;
                        if (x)
                            selectedData.x = _asset[i].x;
                        else
                            selectedData.x = _asset[i].xMax;

                        selectedData.width += (oldValue - selectedData.x);
                        Repaint();
                    }
                    else
                    {
                        if (x)
                            selectedData.xMax = _asset[i].x;
                        else
                            selectedData.xMax = _asset[i].xMax;

                        Repaint();
                    }

                    break;
            }
        }
    }

    private void DrawScene(SceneData data, Color col)
    {
        var rectWithOffset = GetRectWithScreenOffset(data.rectangle);

        if (ShowScenePreviews && !string.IsNullOrEmpty(data.name) && _previewCollection.ContainsKey(data.name))
        {
            if (_previewCollection.ContainsKey(data.name))
                GUI.DrawTexture(rectWithOffset, _previewCollection[data.name]);
        }

        if (ShowSceneNames)
            GUI.Label(rectWithOffset, string.IsNullOrEmpty(data.name) ? "<NO SCENE>" : data.name);

        DrawSceneOutline(rectWithOffset, ref col);
    }

    private void DrawScene(int index, Color col)
    {
        DrawScene(_asset[index], col);
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

    private void DrawSceneOutline(Rect rect, ref Color color)
    {
        if (!_asset)
            return;

        Color oldCol = Handles.color;
        Handles.color = color;

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


        Handles.color = oldCol;
    }

    private void CalculateMinMaxSceneSizes()
    {
        if (!_asset)
            return;

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
        if (_asset)
        {
            GUILayout.BeginArea(new Rect(position.width - 355, 16f, 350, 16f * 6), GUIContent.none, "Box");
            GUILayout.Label(string.Format("{0} : [{1} Scenes Total]", _asset.name, _asset.Count));
            ShowSceneNames = GUILayout.Toggle(ShowSceneNames, "Show Scene Names");
            ShowScenePreviews = GUILayout.Toggle(ShowScenePreviews, "Show Scene Previews");
            ShowSurroundingScenesInSceneView = GUILayout.Toggle(ShowSurroundingScenesInSceneView, "Show Surrounding Scene Previews in SceneView");
            ShowSelectedSceneInfo = GUILayout.Toggle(ShowSelectedSceneInfo, "Show Selected Scene Info Popup");
            GUILayout.EndArea();
        }
        else
        {
            GUILayout.Label("No asset assigned. Drag one onto the window");
        }
    }

    private void DrawSceneInfoPopup()
    {
        const float width = 120;
        const float height = 20 * 3;
        const float sideOffset = 2.0f;

        var data = _asset[_selectedSceneIndex];

        GUILayout.BeginArea(new Rect(position.width - width - sideOffset, position.height - height - sideOffset, width, height), GUIContent.none, "Box");
        GUILayout.Label(string.IsNullOrEmpty(data.name) ? SceneManagerAssetEditor.cMissingSceneNameContent : new GUIContent(data.name));

        GUILayout.BeginHorizontal();
        GUILayout.Label("Position");
        GUILayout.FlexibleSpace();
        GUILayout.Label(string.Format("[{0}, {1}]", data.x, data.y));
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Size");
        GUILayout.FlexibleSpace();
        GUILayout.Label(string.Format("[{0}, {1}]", data.width, data.height));
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void SetUpCamera()
    {
        if (!_windowCamera)
        {
            var newCamera = new GameObject();
            newCamera.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(newCamera);

            _windowCamera = newCamera.AddComponent<Camera>();
            _windowCamera.backgroundColor = Color.clear;
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
        
        for (int i = 0; i < _projectSceneCollection.Count; i++)
        {
            var sceneData = _asset.TryFindScene(_projectSceneCollection[i]);

            if (sceneData == null)
                continue;

            if (!_projectSceneCollection.SceneExists(sceneData.name))
                continue;

            if (EditorApplication.currentScene != _projectSceneCollection.GetRelativeScenePath(sceneData.name))
                EditorApplication.OpenScene(_projectSceneCollection.GetRelativeScenePath(sceneData.name));

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

        var tempRT = RenderTexture.GetTemporary((int)sceneData.rectangle.width, (int)sceneData.rectangle.height, 16, RenderTextureFormat.ARGB32);
        var final = new Texture2D(tempRT.width, tempRT.height, TextureFormat.ARGB32, false);

        final.hideFlags = HideFlags.HideAndDontSave;
        
        _windowCamera.targetTexture = tempRT;
        RenderTexture.active = tempRT;
        _windowCamera.Render();

        final.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        final.Apply();

        _previewCollection.Add(sceneData.name, final);

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
                DragAndDrop.objectReferences[0] is SceneCollectionAsset;

            DragAndDrop.visualMode = acceptableDrag ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;

            if (acceptableDrag && e.type == EventType.DragPerform)
            {
                SetSceneManagerAsset(DragAndDrop.objectReferences[0] as SceneCollectionAsset);
                DragAndDrop.AcceptDrag();
            }

            e.Use();
        }
    }

    private void SetSceneManagerAsset(SceneCollectionAsset asset)
    {
        _asset = asset;

        Repaint();
    }

    #region Properties and Enums

    private Vector2 mousePositionInWorld
    {
        get
        {
            var mapMousePos = Event.current.mousePosition;
            mapMousePos -= _windowContentsOffset;
            mapMousePos.x -= position.width * .5f;
            mapMousePos.y = -(mapMousePos.y - position.height * .5f);
            return mapMousePos;
        }
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

    private bool ShowSelectedSceneInfo
    {
        get { return (_flags & WindowsSettingFlags.ShowSelectedSceneInfo) != 0; }
        set
        {
            if (value)
                _flags |= WindowsSettingFlags.ShowSelectedSceneInfo;
            else
                _flags &= ~WindowsSettingFlags.ShowSelectedSceneInfo;
        }
    }

    [Flags]
    private enum WindowsSettingFlags
    {
        ShowOptions = 1,
        ShowSceneNames = 2,
        ShowScenePreviews = 4,
        ShowSurroundingScenesInSceneView = 8,
        ShowSelectedSceneInfo = 16,
    }

    private enum DraggingEdge
    {
        None = 0,
        Left = 1,
        Top = 2,
        Right = 3,
        Bottom = 4
    }

    #endregion

    public static SceneManagerWindow instance
    {
        get { return s_window; }
    }
}
