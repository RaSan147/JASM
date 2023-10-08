﻿namespace GIMI_ModManager.Core.Helpers;

public static class ModFolderHelpers
{
    public const string DISABLED_PREFIX = "DISABLED_";
    public const string ALT_DISABLED_PREFIX = "DISABLED";


    /// <summary>
    /// Gets the folder name without the disabled prefix. If the folder does not have the disabled prefix, it returns the same string.
    /// </summary>
    /// <param name="folderName"></param>
    /// <returns></returns>
    public static string GetFolderNameWithoutDisabledPrefix(string folderName)
    {
        if (folderName.StartsWith(DISABLED_PREFIX, StringComparison.CurrentCultureIgnoreCase))
            return folderName.Replace(DISABLED_PREFIX, string.Empty);

        if (folderName.StartsWith(ALT_DISABLED_PREFIX, StringComparison.CurrentCultureIgnoreCase))
            return folderName.Replace(ALT_DISABLED_PREFIX, string.Empty);

        return folderName;
    }

    /// <summary>
    /// Gets the folder name with the disabled prefix. If the folder already has the disabled prefix, it returns the same string.
    /// If it has the alternate disabled prefix, it returns it with the normal disabled prefix.
    /// </summary>
    /// <param name="folderName"></param>
    /// <returns></returns>
    public static string GetFolderNameWithDisabledPrefix(string folderName)
    {
        if (folderName.StartsWith(DISABLED_PREFIX, StringComparison.CurrentCultureIgnoreCase))
            return folderName;

        if (folderName.StartsWith(ALT_DISABLED_PREFIX, StringComparison.CurrentCultureIgnoreCase))
            return folderName.Replace(ALT_DISABLED_PREFIX, DISABLED_PREFIX);

        return DISABLED_PREFIX + folderName;
    }

    public static bool FolderNameEquals(string folderName1, string folderName2, bool absolutePaths = false)
    {
        if (absolutePaths)
        {
            folderName1 = new DirectoryInfo(folderName1).Name;
            folderName2 = new DirectoryInfo(folderName2).Name;
        }

        return GetFolderNameWithoutDisabledPrefix(folderName1).Equals(GetFolderNameWithoutDisabledPrefix(folderName2),
            StringComparison.CurrentCultureIgnoreCase);
    }


    public static bool FolderHasDisabledPrefix(string folderName, bool absolutePath = false)
    {
        if (absolutePath)
            folderName = new DirectoryInfo(folderName).Name;

        return folderName.StartsWith(DISABLED_PREFIX, StringComparison.CurrentCultureIgnoreCase) ||
               folderName.StartsWith(ALT_DISABLED_PREFIX, StringComparison.CurrentCultureIgnoreCase);
    }
}