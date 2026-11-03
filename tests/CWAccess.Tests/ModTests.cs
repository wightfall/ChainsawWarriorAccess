using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CWAccess.Audio;
using HarmonyLib;
using Mono.Cecil;
using Xunit;

namespace CWAccess.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void SaveAndLoadRoundTrip()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                Settings.Load(dir);
                Settings.AnnounceDice.Value = false;
                Settings.TimeEveryMinutes.Value = 15;
                Settings.CueVolumes[Cue.Enemy].Value = 40;
                Settings.Language.Value = "th";
                string ini = File.ReadAllText(Path.Combine(dir, "CWAccess.ini"));
                Assert.Contains("AnnounceDice = False", ini);
                Assert.Contains("Cue.Enemy = 40", ini);

                Settings.ResetAll();
                Assert.True(Settings.AnnounceDice.Value);
                File.WriteAllText(Path.Combine(dir, "CWAccess.ini"), ini);
                Settings.Load(dir);
                Assert.False(Settings.AnnounceDice.Value);
                Assert.Equal(15, Settings.TimeEveryMinutes.Value);
                Assert.Equal(40, Settings.CueVolumes[Cue.Enemy].Value);
                Assert.Equal("th", Settings.Language.Value);
            }
            finally
            {
                Settings.ResetAll();
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void BadValuesKeepTheDefault()
        {
            string dir = TestPaths.NewTempDir();
            try
            {
                File.WriteAllText(Path.Combine(dir, "CWAccess.ini"), "TimeEveryMinutes = lots\nSoundCues = maybe\n");
                Settings.ResetAll();
                Settings.Load(dir);
                Assert.Equal(10, Settings.TimeEveryMinutes.Value);
                Assert.True(Settings.SoundCues.Value);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void EveryCueHasAVolumeSetting()
        {
            foreach (Cue c in Enum.GetValues(typeof(Cue))) Assert.True(Settings.CueVolumes.ContainsKey(c));
            Assert.Equal(Settings.All.Count, Settings.All.Select(s => s.Key).Distinct().Count());
        }
    }

    public class SoundCueTests
    {
        [Fact]
        public void EveryCueIsShortAudibleAndClipped()
        {
            foreach (Cue c in Enum.GetValues(typeof(Cue)))
            {
                float[] data = Synth.Make(c, 22050);
                Assert.True(data.Length > 200, c + " is too short");
                Assert.True(data.Length < 22050, c + " is longer than a second");
                Assert.True(data.Max(Math.Abs) > 0.05f, c + " is silent");
                Assert.True(data.All(v => v >= -1f && v <= 1f), c + " clips");
            }
        }

        [Fact]
        public void EveryCueHasANameAndMeaning()
        {
            foreach (Cue c in Enum.GetValues(typeof(Cue)))
            {
                Assert.NotNull(Loc.TryGet("cue_" + c));
                Assert.NotNull(Loc.TryGet("cuedesc_" + c));
            }
        }
    }

    /// <summary>
    /// The mod reaches into the game by name (private fields, Harmony targets). These tests read the game's own
    /// Assembly-CSharp.dll and fail when a name the mod relies on is missing (a typo, or a game update).
    /// </summary>
    public class GameCompatibilityTests
    {
        private static readonly Lazy<ModuleDefinition> Game = new Lazy<ModuleDefinition>(() =>
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(TestPaths.Managed);
            return ModuleDefinition.ReadModule(TestPaths.OriginalAssembly, new ReaderParameters { AssemblyResolver = resolver });
        });

        private static IEnumerable<string> MemberNames(ModuleDefinition m)
        {
            foreach (TypeDefinition t in m.GetTypes())
            {
                foreach (FieldDefinition f in t.Fields) yield return f.Name;
                foreach (MethodDefinition me in t.Methods) yield return me.Name;
                foreach (PropertyDefinition p in t.Properties) yield return p.Name;
            }
        }

        /// <summary>I on a choice of the game board explains it: every kind of choice the game has needs a text.</summary>
        [Fact]
        public void EveryChoiceOfTheGameHasAnExplanation()
        {
            TypeDefinition actions = Game.Value.GetType("EActionType");
            Assert.NotNull(actions);
            var missing = actions.Fields.Where(f => f.IsStatic && f.Name != "AT_None" && f.Name != "AT_NumActions")
                .Select(f => f.Name).Where(n => Loc.TryGet("actionhelp_" + n) == null).ToList();
            Assert.True(missing.Count == 0, "No explanation for: " + string.Join(", ", missing));
        }

        [Fact]
        public void EveryReflectedMemberExistsInTheGame()
        {
            var names = new HashSet<string>(MemberNames(Game.Value));
            var unity = new HashSet<string>(typeof(UnityEngine.RaycastHit).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Select(f => f.Name));
            var missing = new SortedSet<string>();
            foreach (string path in Directory.GetFiles(TestPaths.ModSource, "*.cs", SearchOption.AllDirectories))
            {
                if (path.Contains("\\obj\\") || path.Contains("\\bin\\")) continue;
                string src = File.ReadAllText(path);
                foreach (Match m in Regex.Matches(src, "R\\.(?:Get|GetStatic|Set|Call)(?:<[^>]+>)?\\([^,]+,\\s*\"([A-Za-z_][A-Za-z0-9_]*)\""))
                {
                    string n = m.Groups[1].Value;
                    if (!names.Contains(n) && !unity.Contains(n)) missing.Add(n + " (" + Path.GetFileName(path) + ")");
                }
            }
            Assert.True(missing.Count == 0, "Not found in the game: " + string.Join(", ", missing));
        }

        [Fact]
        public void EveryHarmonyPatchTargetExists()
        {
            var problems = new List<string>();
            foreach (Type t in typeof(Loc).Assembly.GetTypes())
            {
                foreach (HarmonyPatch attr in t.GetCustomAttributes(typeof(HarmonyPatch), false))
                {
                    HarmonyMethod info = attr.info;
                    if (info.declaringType == null || info.methodName == null) continue;
                    // Plain reflection (HarmonyX's AccessTools needs the old Cecil the game folder ships).
                    Type[] args = info.argumentTypes?.Select(x => x.IsByRef ? x.GetElementType() : x).ToArray();
                    bool found = info.declaringType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(m =>
                        m.Name == info.methodName && (args == null || m.GetParameters().Select(p => p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType).SequenceEqual(args)));
                    if (!found) problems.Add(t.Name + " -> " + info.declaringType.Name + "." + info.methodName);
                }
            }
            Assert.True(problems.Count == 0, "Harmony targets not found: " + string.Join(", ", problems));
        }

        [Fact]
        public void TheLoaderHookExists()
        {
            TypeDefinition ui = Game.Value.GetType("CUIManager");
            Assert.NotNull(ui);
            Assert.Contains(ui.Methods, m => m.Name == "Initialise" && m.IsStatic && m.Parameters.Count == 1);
        }

        [Fact]
        public void TheGameScreensTheModKnowsAreRegistered()
        {
            // The names CUIManager.Initialise registers: the mod switches on them in ScreenTracker.
            MethodDefinition init = Game.Value.GetType("CUIManager").Methods.First(m => m.Name == "Initialise");
            var strings = new HashSet<string>(init.Body.Instructions.Where(i => i.Operand is string).Select(i => (string)i.Operand));
            foreach (string screen in new[] { "Game", "Equipment", "EquipmentSelection", "CardSelect", "EndGame", "Achievement", "MotionComic", "LoadScreen", "Options", "Pause", "MainMenu", "Difficulty", "Confirm" })
            {
                Assert.Contains(screen, strings);
            }
        }
    }
}
