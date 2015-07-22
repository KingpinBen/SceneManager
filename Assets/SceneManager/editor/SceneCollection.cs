using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public sealed class SceneCollection
{
    private Dictionary<string, string> _relativeScenePaths;
    private List<string> _sceneList;

    private const string cSceneExtension = ".unity";

    public SceneCollection()
    {
        _relativeScenePaths = new Dictionary<string, string>();
        _sceneList = new List<string>();
    }

    public void UpdateRelativePaths()
    {
        _relativeScenePaths.Clear();
        _sceneList.Clear();

        FindScenesinProject();
    }

    private void FindScenesinProject()
    {
        var directories = new List<string>(new[] { Application.dataPath });
        DirectoryHelper.GetAllDirectories(Application.dataPath, directories);

        int j;
        for(int i = 0; i < directories.Count; i++)
        {
            var files = Directory.GetFiles(directories[i]);
            for (j = 0; j < files.Length; j++)
            {
                if(Path.GetExtension(files[j]) == cSceneExtension)
                {
                    var relativePath = files[j].Remove(0, Application.dataPath.Length - 6);
                    var sceneName = Path.GetFileNameWithoutExtension(relativePath);
                    _sceneList.Add(sceneName);

                    if (_relativeScenePaths.ContainsKey(sceneName))
                        _relativeScenePaths[sceneName] = relativePath;
                    else
                        _relativeScenePaths.Add(sceneName, relativePath);

                    continue;
                }
            }
        }
    }

    public bool SceneExists(string sceneName)
    {
        return _sceneList.Contains(sceneName);
    }

    public string GetRelativeScenePath(string sceneName)
    {
        if (!SceneExists(sceneName))
            throw new UnityException(string.Format("Scene '{0}' isn't in the collection", sceneName));

        return _relativeScenePaths[sceneName];
    }

    public int Count
    {
        get { return _sceneList.Count; }
    }

    public string this[int index]
    {
        get { return _sceneList[index]; }
    }
}
