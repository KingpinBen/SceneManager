using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public sealed class SceneManagerWindow : EditorWindow
{
    [SerializeField]
    private SceneManagerAsset _asset;

    private SceneCollection _sceneCollection;
    private ScenePreviewCollection _previewCollection;
    private string _scenePreviewImagesFolder;
    private Camera _windowCamera;
    private bool _showStats;
    private bool _showSceneNames;
    private bool _showSurroundingScenesInSceneView;
    private int _selectedSceneIndex = -1;
    private int _hoverSceneIndex = -1;

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

        _sceneCollection.UpdateRelativePaths();

        SceneView.onSceneGUIDelegate += OnSceneGUI;

    }

    private void OnGUI()
    {
        HandleSceneManagerAssetDrop();

        EditorGUILayout.BeginVertical();
        DrawToolbar();

        var lastRect = GUILayoutUtility.GetLastRect();
        var renderArea = new Rect(0, lastRect.yMax, position.width, position.height - lastRect.yMax);

        if (_asset)
        {
            DrawSceneMap(renderArea);
            if (EditorApplication.isPlaying)
                Handles.DrawWireDisc(Camera.main.transform.position + new Vector3(position.width, position.height) *.5f, -Vector3.forward, SceneManager.cBufferArea);
        }

        if (_showStats)
        {
            DrawStatsWindow(renderArea);
        }

        EditorGUILayout.EndVertical();
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

            _showStats = GUILayout.Toggle(_showStats, "Stats", EditorStyles.toolbarButton, new GUILayoutOption[0]);
        }
        else
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Drag a SceneManagerAsset onto the window");
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSceneMap(Rect renderArea)
    {
        SceneData data;
        Rect rectWithOffset = new Rect();

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            _selectedSceneIndex = -1;
        }
        _hoverSceneIndex = -1;

        Handles.BeginGUI();
        for (int i = 0;  i < _asset.Count; i++)
        {
            data = _asset[i];
            rectWithOffset.x = data.rectangle.x + position.width * .5f;
            rectWithOffset.y =  position.height * .5f - data.rectangle.y;
            rectWithOffset.width = data.rectangle.width;
            rectWithOffset.height = data.rectangle.height;

            if (rectWithOffset.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0)
                {
                    if (Event.current.clickCount == 2)
                    {
                        if (_sceneCollection.SceneExists(_asset[i].name) &&
                            EditorUtility.DisplayDialog("Open Scene", "Are you sure you want to open the scene?", "Yes", "No"))
                                EditorApplication.OpenScene(_sceneCollection.GetRelativeScenePath(_asset[i].name));                        
                    }

                    _selectedSceneIndex = i;
                }

                _hoverSceneIndex = i;
            }

            if (Event.current.type == EventType.repaint &&
                !string.IsNullOrEmpty(data.name) && _previewCollection.ContainsKey(data.name))
            {
                if (_previewCollection.ContainsKey(data.name))
                    GUI.DrawTexture(rectWithOffset, _previewCollection[data.name]);
            }

            if (_showSceneNames)
                GUI.Label(rectWithOffset, string.IsNullOrEmpty(data.name) ? "<NO SCENE>" : data.name);

            DrawSceneRect(rectWithOffset, i);
        }
        Handles.EndGUI();
    }

    private void DrawSceneRect(Rect rect, int index)
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

    private void DrawStatsWindow(Rect renderArea)
    {
        using (var scope = new EditorGUILayout.VerticalScope("Box", GUILayout.Width(350)))
        {
            if (_asset)
            {
                GUILayout.Label(string.Format("{0} : [{1} Scenes Total]", _asset.name, _asset.Count));
                _showSceneNames = GUILayout.Toggle(_showSceneNames, "Show Scene Names");
                _showSurroundingScenesInSceneView = GUILayout.Toggle(_showSurroundingScenesInSceneView, "Show Surrounding Scene Images in SceneView");
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
        }

        _windowCamera.enabled = false;
        _windowCamera.orthographic = true;
        _windowCamera.backgroundColor = new Color(49, 77, 121, 1);
        _windowCamera.clearFlags = CameraClearFlags.Skybox;
    }

    private void UpdateTexturePreviewCache()
    {
        if (_asset == null)
            return;

        SetUpCamera();

        var startScene = EditorApplication.currentScene;
        EditorApplication.SaveScene();
        for (int i = 0; i < _sceneCollection.Count; i++)
        {
            var sceneData = _asset.TryFindScene(_sceneCollection[i]);

            if (sceneData == null)
            {
                Debug.Log("sceneData is null");
                continue;
            }

            if (!_sceneCollection.SceneExists(sceneData.name))
                continue;
            
            EditorApplication.OpenScene(_sceneCollection.GetRelativeScenePath(sceneData.name));

            var aspect = sceneData.rectangle.width / sceneData.rectangle.height;

            _windowCamera.aspect = aspect;
            _windowCamera.orthographicSize = sceneData.rectangle.height * .5f;
            _windowCamera.transform.position = new Vector3(sceneData.rectangle.center.x, sceneData.rectangle.y - sceneData.rectangle.height*.5f, -10);

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

        Resources.UnloadUnusedAssets();
        EditorApplication.OpenScene(startScene);
        Repaint();
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
        var data = _asset.TryFindScene(Path.GetFileNameWithoutExtension(EditorApplication.currentScene));
        if (data == null)
        {
            Handles.BeginGUI();
            GUILayout.Box("Scene wasn't found in the SceneManagerAsset. Cannot display boundaries.");
            Handles.EndGUI();

            return;
        }

        var rect = data.rectangle;
        var center = rect.center;
        var oldCol = Handles.color;
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

        if (_showSurroundingScenesInSceneView)
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
}
