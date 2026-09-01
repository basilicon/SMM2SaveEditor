using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia.Platform;

namespace SMM2SaveEditor.Utility
{
    public static class FlagDatabase
    {
        public class FlagEntry
        {
            public string description { get; set; } = "";
            public string? extended { get; set; }
        }

        public class ObjectFlagData
        {
            public string name { get; set; } = "";
            public Dictionary<string, string> flags { get; set; } = new();
            public string? notes { get; set; }
        }

        public class DatabaseModel
        {
            public Dictionary<string, FlagEntry> general { get; set; } = new();
            public Dictionary<string, ObjectFlagData> objects { get; set; } = new();
        }

        private static DatabaseModel? db = null;

        public static void Initialize()
        {
            if (db != null) return;

            try
            {
                string? filePath = AssetHelper.GetAssetFilePath("Assets/object_flags.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    db = JsonSerializer.Deserialize<DatabaseModel>(json);
                    return;
                }

                // Fallback to embedded Avalonia resource
                var uri = new Uri("avares://SMM2SaveEditor/Assets/object_flags.json");
                if (AssetLoader.Exists(uri))
                {
                    using var stream = AssetLoader.Open(uri);
                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    db = JsonSerializer.Deserialize<DatabaseModel>(json);
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load object_flags.json: {ex.Message}");
            }

            db = new DatabaseModel();
        }

        public static string GetFlagLabel(int? objId, uint flagBit)
        {
            Initialize();

            string hexKey = "0x" + flagBit.ToString("x");
            string hexDisplay = "0x" + flagBit.ToString("X");

            if (db != null)
            {
                // Check object-specific flag description
                if (objId.HasValue && db.objects.TryGetValue(objId.Value.ToString(), out var objData))
                {
                    if (objData.flags.TryGetValue(hexKey, out var desc) && !string.IsNullOrWhiteSpace(desc))
                    {
                        return $"{hexDisplay} - {desc}";
                    }
                }

                // Fallback to general flag description
                if (db.general.TryGetValue(hexKey, out var genEntry) && !string.IsNullOrWhiteSpace(genEntry.description))
                {
                    return $"{hexDisplay} - {genEntry.description}";
                }
            }

            return hexDisplay;
        }

        public static string? GetFlagTooltip(int? objId, uint flagBit)
        {
            Initialize();

            string hexKey = "0x" + flagBit.ToString("x");

            if (db != null)
            {
                // General extended description
                if (db.general.TryGetValue(hexKey, out var genEntry))
                {
                    if (!string.IsNullOrWhiteSpace(genEntry.extended))
                    {
                        return genEntry.extended;
                    }
                }
            }

            return null;
        }

        public static string? GetObjectNotes(int? objId)
        {
            Initialize();

            if (db != null && objId.HasValue && db.objects.TryGetValue(objId.Value.ToString(), out var objData))
            {
                return objData.notes;
            }

            return null;
        }

        public static string? GetObjectName(int? objId)
        {
            Initialize();

            if (db != null && objId.HasValue && db.objects.TryGetValue(objId.Value.ToString(), out var objData))
            {
                return objData.name;
            }

            return null;
        }
    }
}
