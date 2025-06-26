

namespace GSNodeEditor
{
    public static class NodeEditorPaths
    {
        public static string AssetsFolderName
        {
            get { return "GSNodeEditorAssets"; }
        }


        static string CachedAssetsPath = "";
        public static string GetCachedAssetsPath()
        {
            if (CachedAssetsPath.Length > 0)
                return CachedAssetsPath;
			string curpath = Directory.GetCurrentDirectory();
			int iters = 0;
			string root = Path.GetPathRoot(curpath) ?? "c:\\";
			CachedAssetsPath = root;
			while (Directory.Exists(curpath) && curpath != root && iters++ < 20)
			{
				curpath = Path.GetFullPath(Path.Combine(curpath, ".."));
				string assetpath = Path.Combine(curpath, AssetsFolderName);
                if (Directory.Exists(assetpath)) {
                    CachedAssetsPath = assetpath;
                    break;
                }
			}
            return CachedAssetsPath;
		}

        public static string AssetsPath {
            get { return GetCachedAssetsPath(); }
        }


        public static string Map(string localPath)
        {
            return Path.Combine(AssetsPath, localPath);
        }


        public static IEnumerable<string> Map(IEnumerable<string> localPaths)
        {
            foreach ( string path in localPaths )
            {
                yield return Path.Combine(AssetsPath, path);
            }
        }



    }

}
