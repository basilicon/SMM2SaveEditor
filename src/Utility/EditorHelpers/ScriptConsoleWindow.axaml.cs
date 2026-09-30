using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using SMM2SaveEditor.Entities;
using SMM2SaveEditor.Utility;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SMM2SaveEditor.Utility.EditorHelpers
{
    public class ScriptGlobals
    {
        public Level level { get; set; } = null!;
        public Map overworld { get; set; } = null!;
        public Map subworld { get; set; } = null!;
        public Action<object?> print { get; set; } = null!;
        public Action refresh { get; set; } = null!;
    }

    public partial class ScriptConsoleWindow : Window
    {
        private readonly Level currentLevel;
        private readonly Action refreshCallback;

        private TextBox codeEditor;
        private TextBox outputConsole;
        private TextBlock statusText;
        private TextBlock executionTimeText;
        private ComboBox presetDropdown;
        private Button runButton;

        public ScriptConsoleWindow() : this(MainWindow.Instance?.CurrentLevel ?? new Level(), () => {})
        {
        }

        public ScriptConsoleWindow(Level level, Action onRefresh)
        {
            InitializeComponent();

            currentLevel = level;
            refreshCallback = onRefresh;

            codeEditor = this.FindControl<TextBox>("CodeEditor")!;
            outputConsole = this.FindControl<TextBox>("OutputConsole")!;
            statusText = this.FindControl<TextBlock>("StatusText")!;
            executionTimeText = this.FindControl<TextBlock>("ExecutionTimeText")!;
            presetDropdown = this.FindControl<ComboBox>("PresetDropdown")!;
            runButton = this.FindControl<Button>("RunButton")!;

            presetDropdown.SelectionChanged += OnPresetChanged;
            LoadPreset(0);

            KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.F5)
                {
                    OnRunScriptClick(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            };
        }

        private void OnPresetChanged(object? sender, SelectionChangedEventArgs e)
        {
            LoadPreset(presetDropdown.SelectedIndex);
        }

        private void LoadPreset(int index)
        {
            switch (index)
            {
                case 0: // Binary Search: Remove Second Half
                    codeEditor.Text = @"// --- Binary Search: Remove Second Half of Tracks ---
var list = overworld.tracks.Count > 0 ? overworld.tracks : subworld.tracks;
string target = overworld.tracks.Count > 0 ? ""Overworld"" : ""Subworld"";
int total = list.Count;
int keep = total / 2;

print($""[{target}] Total tracks: {total}. Keeping first {keep}, removing {total - keep}."");

while (list.Count > keep) {
    list.RemoveAt(list.Count - 1);
}

print($""[{target}] Remaining tracks: {list.Count}."");
refresh();
";
                    break;

                case 1: // Binary Search: Remove First Half
                    codeEditor.Text = @"// --- Binary Search: Remove First Half of Tracks ---
var list = overworld.tracks.Count > 0 ? overworld.tracks : subworld.tracks;
string target = overworld.tracks.Count > 0 ? ""Overworld"" : ""Subworld"";
int total = list.Count;
int removeCount = total / 2;

print($""[{target}] Total tracks: {total}. Removing first {removeCount}, keeping remaining {total - removeCount}."");

for (int i = 0; i < removeCount && list.Count > 0; i++) {
    list.RemoveAt(0);
}

print($""[{target}] Remaining tracks: {list.Count}."");
refresh();
";
                    break;

                case 2: // List All Track Details
                    codeEditor.Text = @"// --- Preset: List All Track Details ---
var list = overworld.tracks.Count > 0 ? overworld.tracks : subworld.tracks;
string target = overworld.tracks.Count > 0 ? ""Overworld"" : ""Subworld"";

print($""Listing {list.Count} tracks in {target}:"");
int idx = 0;
foreach (var t in list) {
    print($""#{idx:D3} @ ({t.x,3}, {t.y,3}) Type={(int)t.type,-2} ({t.type}) u2=0x{t.unknown2:X4} u3=0x{t.unknown3:X4}"");
    idx++;
}
";
                    break;

                case 3: // Apply 7 Rules (Clean Cut Tracks)
                    codeEditor.Text = @"// --- Preset: Auto-Apply 7 Track Rules ---
// Enforces cut-track detection when an endpoint points back into the track body
int modified = 0;
var list = overworld.tracks.Count > 0 ? overworld.tracks : subworld.tracks;

foreach (var t in list) {
    byte u2_lo = (byte)(t.unknown2 & 0xFF);
    byte u3_lo = (byte)(t.unknown3 & 0xFF);
    int s1 = u2_lo & 0x0F;
    int s2 = u3_lo & 0x0F;

    // Check inward-pointing endpoints
    if (t.type == TrackType.horizontal) {
        if (s1 == 0) { t.unknown2 = 0x104; modified++; } // West points East -> cut
        if (s2 == 1) { t.unknown3 = 0x104; modified++; } // East points West -> cut
    } else if (t.type == TrackType.vertical) {
        if (s1 == 3) { t.unknown2 = 0x104; modified++; } // North points South -> cut
        if (s2 == 2) { t.unknown3 = 0x104; modified++; } // South points North -> cut
    }
}

print($""Cleaned {modified} inward cut endpoints."");
refresh();
";
                    break;

                case 4: // Batch Add Ground Walkway
                    codeEditor.Text = @"// --- Preset: Add Solid Ground Walkway ---
for (int gx = 0; gx < 240; gx++) {
    for (int gy = 0; gy < 3; gy++) {
        overworld.ground.Add(new Ground { x = (byte)gx, y = (byte)gy });
    }
}
print($""Overworld now has {overworld.ground.Count} ground blocks."");
refresh();
";
                    break;

                case 5: // Custom Script Template
                    codeEditor.Text = @"// --- Custom Script ---
// Globals available:
// - level     : Active Level instance
// - overworld : level.overworld (Map)
// - subworld  : level.subworld (Map)
// - print(msg): Output message to this console
// - refresh() : Re-render canvas immediately

print($""Current Overworld: {overworld.tracks.Count} tracks, {overworld.objects.Count} objects."");
print($""Current Subworld:  {subworld.tracks.Count} tracks, {subworld.objects.Count} objects."");
";
                    break;
            }
        }

        private async void OnRunScriptClick(object? sender, RoutedEventArgs e)
        {
            string code = codeEditor.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code)) return;

            runButton.IsEnabled = false;
            statusText.Text = "Compiling & executing...";
            statusText.Foreground = Avalonia.Media.Brushes.Orange;

            var sb = new StringBuilder();
            var sw = Stopwatch.StartNew();

            Action<object?> log = (msg) =>
            {
                string line = msg?.ToString() ?? "null";
                sb.AppendLine(line);
            };

            Action doRefresh = () =>
            {
                currentLevel.overworld?.RebuildCanvas();
                currentLevel.subworld?.RebuildCanvas();
                refreshCallback();
            };

            var globals = new ScriptGlobals
            {
                level = currentLevel,
                overworld = currentLevel.overworld,
                subworld = currentLevel.subworld,
                print = log,
                refresh = () =>
                {
                    if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
                    {
                        doRefresh();
                    }
                    else
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(doRefresh);
                    }
                }
            };

            try
            {
                var options = ScriptOptions.Default
                    .AddImports("System", "System.Collections.Generic", "System.Linq", "SMM2SaveEditor", "SMM2SaveEditor.Entities", "SMM2SaveEditor.Utility")
                    .AddReferences(typeof(Level).Assembly, typeof(System.Linq.Enumerable).Assembly);

                await CSharpScript.RunAsync(code, options, globals);

                sw.Stop();
                statusText.Text = "Success";
                statusText.Foreground = Avalonia.Media.Brushes.LightGreen;
                executionTimeText.Text = $"Completed in {sw.ElapsedMilliseconds} ms";
                outputConsole.Text = sb.ToString();

                // Trigger UI refresh
                globals.refresh();
            }
            catch (CompilationErrorException cex)
            {
                sw.Stop();
                statusText.Text = "Compilation Error";
                statusText.Foreground = Avalonia.Media.Brushes.Salmon;
                executionTimeText.Text = $"Failed after {sw.ElapsedMilliseconds} ms";
                outputConsole.Text = "=== COMPILATION ERROR ===\n" + string.Join("\n", cex.Diagnostics);
            }
            catch (Exception ex)
            {
                sw.Stop();
                statusText.Text = "Runtime Exception";
                statusText.Foreground = Avalonia.Media.Brushes.Salmon;
                executionTimeText.Text = $"Failed after {sw.ElapsedMilliseconds} ms";
                outputConsole.Text = $"=== RUNTIME EXCEPTION ===\n{ex.Message}\n{ex.StackTrace}";
            }
            finally
            {
                runButton.IsEnabled = true;
            }
        }

        private void OnClearLogClick(object? sender, RoutedEventArgs e)
        {
            outputConsole.Text = string.Empty;
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
