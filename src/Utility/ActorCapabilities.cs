using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia.Platform;
using SMM2SaveEditor.Entities;

namespace SMM2SaveEditor.Utility
{
    public class ActorCapabilityInfo
    {
        public string Name { get; set; } = "";
        public bool CanEquipWing { get; set; }
        public bool CanEquipPara { get; set; }
        public bool CanBeBig { get; set; }
        public bool CanLinkPipe { get; set; }
        public bool CanLinkTrack { get; set; }
        public bool CanLinkClown { get; set; }
        public bool CanLinkCloud { get; set; }
        public bool CanLinkClaw { get; set; }
        public bool CanStack { get; set; }
        public bool CanHoldContents { get; set; }
        public bool CanHaveKey { get; set; }
        public bool CanHaveAltForm { get; set; }
    }

    public static class ActorCapabilities
    {
        private static Dictionary<int, ActorCapabilityInfo>? capabilities = null;

        public static void Initialize()
        {
            if (capabilities != null) return;

            try
            {
                string? filePath = AssetHelper.GetAssetFilePath("Assets/actor_capabilities.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    capabilities = JsonSerializer.Deserialize<Dictionary<int, ActorCapabilityInfo>>(json);
                    if (capabilities != null && capabilities.Count > 0) return;
                }

                // Fallback to embedded resource
                var uri = new Uri("avares://SMM2SaveEditor/Assets/actor_capabilities.json");
                if (AssetLoader.Exists(uri))
                {
                    using var stream = AssetLoader.Open(uri);
                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    capabilities = JsonSerializer.Deserialize<Dictionary<int, ActorCapabilityInfo>>(json);
                    if (capabilities != null && capabilities.Count > 0) return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load actor_capabilities.json: {ex.Message}");
            }

            capabilities = new Dictionary<int, ActorCapabilityInfo>();
        }

        public static ActorCapabilityInfo? GetCapabilities(Obj.ObjId id)
        {
            Initialize();
            if (capabilities != null && capabilities.TryGetValue((int)id, out var info))
            {
                return info;
            }
            return null;
        }

        /// <summary>
        /// Validates an object's bitflags against SMM2 engine capability rules.
        /// Returns a list of error violation descriptions, or empty if valid.
        /// </summary>
        public static List<string> ValidateObjectFlags(Obj.ObjId id, uint flag, uint cflag, int objIndex, string areaName)
        {
            var errors = new List<string>();
            var caps = GetCapabilities(id);

            // 1. General Mutually Exclusive Flag combinations (engine crashes/undefined states)
            if ((flag & 0x8002) == 0x8002) // Wings + Parachute
            {
                errors.Add($"{areaName} Obj #{objIndex} ({id}): Illegal Wings (0x2) + Parachute (0x8000) combination.");
            }

            if ((flag & 0x8400) == 0x8400) // Parachute + On Track
            {
                errors.Add($"{areaName} Obj #{objIndex} ({id}): Illegal Parachute (0x8000) on a Track (0x400).");
            }

            if ((flag & 0x8001) == 0x8001) // Parachute + In Pipe
            {
                errors.Add($"{areaName} Obj #{objIndex} ({id}): Illegal Parachute (0x8000) inside a Pipe (0x1).");
            }

            // 2. Per-Actor Capability Masks (from SMM2 EditDB_common)
            if (caps != null)
            {
                if ((flag & 0x2) != 0 && !caps.CanEquipWing)
                {
                    errors.Add($"{areaName} Obj #{objIndex} ({id}): Has Wings (0x2) but this actor does not support wings.");
                }

                if ((flag & 0x8000) != 0 && !caps.CanEquipPara)
                {
                    errors.Add($"{areaName} Obj #{objIndex} ({id}): Has Parachute (0x8000) but this actor does not support parachutes.");
                }

                if ((flag & 0x4000) != 0 && !caps.CanBeBig)
                {
                    errors.Add($"{areaName} Obj #{objIndex} ({id}): Has Big Size (0x4000) but this actor cannot react to Super Mushrooms.");
                }

                if ((flag & 0x1) != 0 && !caps.CanLinkPipe)
                {
                    errors.Add($"{areaName} Obj #{objIndex} ({id}): Has In-Pipe flag (0x1) but this actor cannot enter pipes.");
                }

                if ((flag & 0x400) != 0 && !caps.CanLinkTrack)
                {
                    errors.Add($"{areaName} Obj #{objIndex} ({id}): Has Track flag (0x400) but this actor cannot be attached to tracks.");
                }

                if ((flag & 0x200) != 0 && !caps.CanLinkClown)
                {
                    errors.Add($"{areaName} Obj #{objIndex} ({id}): Has Clown Car flag (0x200) but this actor cannot ride Clown Cars.");
                }

                if ((flag & 0x80) != 0 && !caps.CanHoldContents)
                {
                    errors.Add($"{areaName} Obj #{objIndex} ({id}): Has Container flag (0x80) but this actor cannot hold contents.");
                }

                if ((flag & 0x1000) != 0 && !caps.CanStack)
                {
                    errors.Add($"{areaName} Obj #{objIndex} ({id}): Has Stack flag (0x1000) but this actor cannot be stacked.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Sanitizes illegal flags on an object by clearing bits that violate capability masks or mutual exclusion rules.
        /// </summary>
        public static uint SanitizeFlags(Obj.ObjId id, uint flag)
        {
            var caps = GetCapabilities(id);
            uint sanitized = flag;

            // Resolve mutual exclusion: keep Parachute if both set, or Wings
            if ((sanitized & 0x8002) == 0x8002)
            {
                sanitized &= ~0x8000u; // Remove parachute if wings present
            }

            if ((sanitized & 0x8400) == 0x8400)
            {
                sanitized &= ~0x8000u; // Clear parachute on track
            }

            if ((sanitized & 0x8001) == 0x8001)
            {
                sanitized &= ~0x8000u; // Clear parachute in pipe
            }

            // Capability mask enforcement
            if (caps != null)
            {
                if (!caps.CanEquipWing) sanitized &= ~0x2u;
                if (!caps.CanEquipPara) sanitized &= ~0x8000u;
                if (!caps.CanBeBig) sanitized &= ~0x4000u;
                if (!caps.CanLinkPipe) sanitized &= ~0x1u;
                if (!caps.CanLinkTrack) sanitized &= ~0x400u;
                if (!caps.CanLinkClown) sanitized &= ~0x200u;
                if (!caps.CanHoldContents) sanitized &= ~0x80u;
                if (!caps.CanStack) sanitized &= ~0x1000u;
            }

            return sanitized;
        }
    }
}
