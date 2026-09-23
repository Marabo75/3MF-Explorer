using System;
using System.Collections.Generic;

namespace ThreeMFExplorer
{
    public class ThumbnailCacheEntry
    {
        public string PreviewPath { get; set; } =
            string.Empty;

        public DateTime LastWriteTime { get; set; }
    }

    public static class ThumbnailCache
    {
        private static readonly Dictionary<
            string,
            ThumbnailCacheEntry> Cache = new();

        public static bool TryGet(
            string filePath,
            DateTime lastWriteTime,
            out string previewPath)
        {
            previewPath = string.Empty;

            if (!Cache.TryGetValue(
                    filePath,
                    out ThumbnailCacheEntry? entry))
            {
                return false;
            }

            if (entry.LastWriteTime != lastWriteTime)
            {
                Cache.Remove(filePath);

                return false;
            }

            previewPath =
                entry.PreviewPath;

            return true;
        }

        public static void Store(
            string filePath,
            string previewPath,
            DateTime lastWriteTime)
        {
            Cache[filePath] =
                new ThumbnailCacheEntry
                {
                    PreviewPath = previewPath,
                    LastWriteTime = lastWriteTime
                };
        }

        public static void Clear()
        {
            Cache.Clear();
        }
    }
}