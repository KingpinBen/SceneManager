using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

public sealed class ScenePreviewCollection
{
    private Dictionary<string, Texture2D> _texturePreviewCache;
    private string _previewFolder;

    private const string cSceneImagesPreviewFolderName = "Scene Previews";

    public ScenePreviewCollection(ProjectSceneCollection sceneCollection)
    {
        _texturePreviewCache = new Dictionary<string, Texture2D>();

        FindPreviewFolder();
    }

    public void Add(string sceneName, Texture2D texture)
    {
        if (_texturePreviewCache.ContainsKey(sceneName))
            Object.DestroyImmediate(_texturePreviewCache[sceneName]);

        _texturePreviewCache[sceneName] = texture;
        var imageLocation = string.Format("{0}\\{1}.png", _previewFolder, sceneName);

        File.WriteAllBytes(imageLocation, texture.EncodeToPNG());
    }

    public void Remove(string sceneName)
    {
        if (!_texturePreviewCache.ContainsKey(sceneName))
            throw new UnityException(
                string.Format("Tried removing key '{0}' but it doesn't exist in the list", sceneName));

        Object.DestroyImmediate(_texturePreviewCache[sceneName]);
        _texturePreviewCache.Remove(sceneName);
    }

    public bool ContainsKey(string sceneName)
    {
        if (!_texturePreviewCache.ContainsKey(sceneName))
            return false;

        if (!_texturePreviewCache[sceneName])
        {
            var tex = new Texture2D(1,1);
            tex.LoadImage(File.ReadAllBytes(string.Format("{0}\\{1}.png",_previewFolder, sceneName)));

            _texturePreviewCache[sceneName] = tex;
        }

        return true;
    }

    private void FindPreviewFolder()
    {
        const string sceneManagerName = "SceneManagerWindow.cs";

        var sceneManagerFileLocation =
            DirectoryHelper.GetDirectoryContainingFile(sceneManagerName, Application.dataPath);

        if (string.IsNullOrEmpty(sceneManagerFileLocation))
            throw new UnityException("SceneManagerWindow class wasn't found in the project somehow");

        var relativeSceneManagerLocation =
            sceneManagerFileLocation.Remove(0, Application.dataPath.Length - ("Assets").Length);
        _previewFolder = string.Format("{0}\\{1}", relativeSceneManagerLocation, cSceneImagesPreviewFolderName);

        if (!AssetDatabase.IsValidFolder(_previewFolder))
            AssetDatabase.CreateFolder(relativeSceneManagerLocation, cSceneImagesPreviewFolderName);
    }

    public string RelativeTexturePreviewDirectory
    {
        get { return _previewFolder; }
    }

    public Texture2D this[string key]
    {
        get { return _texturePreviewCache[key]; }
    }
}
