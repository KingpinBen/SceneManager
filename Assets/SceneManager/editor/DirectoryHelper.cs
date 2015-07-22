using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class DirectoryHelper
{
    public static List<string> GetAllDirectories(string parent)
    {
        List<string> result = new List<string>();
        GetAllDirectories(parent, result);
        return result;
    }

    public static void GetAllDirectories(string parent, List<string> directories)
    {
        var currentFoldersChildren = Directory.GetDirectories(parent);
        directories.AddRange(currentFoldersChildren);

        for (int i = 0; i < currentFoldersChildren.Length; i++)
            GetAllDirectories(currentFoldersChildren[i], directories);
    }

    public static bool DirectoryContainsFile(string nameWithExtension, string directory)
    {
        var files = Directory.GetFiles(directory);

        for(int i = 0; i < files.Length; i++)
        {
            if (Path.GetFileName(files[i]) == nameWithExtension)
                return true;
        }

        return false;
    }

    public static string GetDirectoryContainingFile(string nameWithExtension, List<string> directories)
    {
        for (int i = 0; i < directories.Count; i++)
        {
            if (DirectoryContainsFile(nameWithExtension, directories[i]))
                return directories[i];
        }

        //   Wasn't found
        return null;
    }

    public static string GetDirectoryContainingFile(string nameWithExtension, string parentDirectory)
    {
        return GetDirectoryContainingFile(nameWithExtension, GetAllDirectories(parentDirectory));
    }
}
