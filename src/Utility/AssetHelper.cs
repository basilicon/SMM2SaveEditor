using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SMM2SaveEditor.Utility
{
    public static class AssetHelper
    {
        public static Bitmap? LoadBitmap(string relativeAssetPath)
        {
            // Normalize path separators
            relativeAssetPath = relativeAssetPath.Replace('\\', '/').TrimStart('/');

            // 1. Try from AppContext.BaseDirectory / exe location
            string localPath = Path.Combine(AppContext.BaseDirectory, relativeAssetPath);
            if (File.Exists(localPath))
            {
                try
                {
                    return new Bitmap(localPath);
                }
                catch { }
            }

            // 2. Try relative from current working directory
            if (File.Exists(relativeAssetPath))
            {
                try
                {
                    return new Bitmap(relativeAssetPath);
                }
                catch { }
            }

            // 3. Try searching parent directories (up to 4 levels for IDE debug runs)
            string current = AppContext.BaseDirectory;
            for (int i = 0; i < 4; i++)
            {
                string candidate = Path.Combine(current, relativeAssetPath);
                if (File.Exists(candidate))
                {
                    try
                    {
                        return new Bitmap(candidate);
                    }
                    catch { }
                }
                DirectoryInfo? parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            // 4. Try loading from Avalonia embedded resource
            try
            {
                var uri = new Uri($"avares://SMM2SaveEditor/{relativeAssetPath}");
                if (AssetLoader.Exists(uri))
                {
                    using var stream = AssetLoader.Open(uri);
                    return new Bitmap(stream);
                }
            }
            catch { }

            return null;
        }

        public static string? GetAssetFilePath(string relativeAssetPath)
        {
            relativeAssetPath = relativeAssetPath.Replace('\\', '/').TrimStart('/');

            string localPath = Path.Combine(AppContext.BaseDirectory, relativeAssetPath);
            if (File.Exists(localPath)) return localPath;

            if (File.Exists(relativeAssetPath)) return Path.GetFullPath(relativeAssetPath);

            string current = AppContext.BaseDirectory;
            for (int i = 0; i < 4; i++)
            {
                string candidate = Path.Combine(current, relativeAssetPath);
                if (File.Exists(candidate)) return candidate;
                DirectoryInfo? parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            return null;
        }
    }
}
