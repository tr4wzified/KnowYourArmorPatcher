using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Wabbajack.Common;
using Newtonsoft.Json.Linq;

namespace KnowYourArmorPatcher
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                new UserPreferences
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher
                    {
                        IdentifyingModKey = "know_your_armor_patcher.esp",
                        TargetRelease = GameRelease.SkyrimSE
                    }
                });
        }

        private static float AdjustEffectMagnitude(float magnitude, float scale)
        {
            if (magnitude.EqualsWithin(0))
                return magnitude;
            if (magnitude > 1)
                return (magnitude - 1) * scale + 1;
            return 1 / AdjustEffectMagnitude(1 / magnitude, scale);
        }

        private static IEnumerable<string> GetFromJson(string key, JObject jObject)
        {
            return jObject.ContainsKey(key) ? jObject[key]!.Select(x => (string?) x).Where(x => x != null).Select(x => x!).ToList() : new List<string>();
        }

        private static readonly Tuple<string, uint>[] armorKeywordsTuple =
        {
            new Tuple<string, uint> ("full", 0x0B6D03),
            new Tuple<string, uint> ("warm", 0x0B6D04),
            new Tuple<string, uint> ("leathery", 0x0B6D05),
            new Tuple<string, uint> ("brittle", 0x0B6D06),
            new Tuple<string, uint> ("nonconductive", 0x0B6D07),
            new Tuple<string, uint> ("thick", 0x0B6D08),
            new Tuple<string, uint> ("metal", 0x0B6D09),
            new Tuple<string, uint> ("layered", 0x0B6D0A),
            new Tuple<string, uint> ("deep", 0x0B6D0B),
        };

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        { 
            if (!state.LoadOrder.ContainsKey(ModKey.FromNameAndExtension("know_your_enemy.esp")))
            {
                Console.WriteLine("ERROR: Know Your Enemy not detected in load order. You need to install KYE prior to running this patcher!");
                return;
            }

            string[] requiredFiles = { "armor_rules.json", "misc.json", "settings.json" };
            foreach (string file in requiredFiles)
            {
                if (!File.Exists(file)) throw new Exception("Required file " + file + " does not exist! Make sure to copy all files over when installing the patcher, and don't run it from within an archive.");
            }

            var armorRulesJson = JObject.Parse(File.ReadAllText("armor_rules.json"));
            var miscJson = JObject.Parse(File.ReadAllText("misc.json"));
            var settingsJson = JObject.Parse(File.ReadAllText("settings.json"));

            // Converting to list because .Contains in Newtonsoft.JSON has weird behavior
            List<string> armorRaces = GetFromJson("armor_races", miscJson).ToList();
            List<string> ignoredArmors = GetFromJson("ignored_armors", miscJson).ToList();

            float effectIntensity = (float)settingsJson["effect_intensity"]!;

            Dictionary<string, Keyword> armorKeywords = armorKeywordsTuple.Select(tuple =>
            {
                var (key, id) = tuple;
                state.LinkCache.TryLookup<IKeywordGetter>(new FormKey("know_your_enemy.esp", id), out var keyword);
                if (perk != null) return (key, keyword: keyword.DeepCopy());
                else throw new Exception("Failed to find perk with key: " + key + " and id " + id);
            }).Where(x => x.perk != null)
        .ToDictionary(x => x.key, x => x.perk!, StringComparer.OrdinalIgnoreCase);


            if (!state.LinkCache.TryLookup<IPerkGetter>(new FormKey("know_your_enemy.esp", 0x0B6D0D), out var perkLink))
                throw new Exception("Unable to find required perk know_your_enemy.esp:0x0B6D0D");


            // Part 1
            // Add the armor perk to all relevant NPCs
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList)) continue;

                if (npc.Keywords != null && npc.Keywords.Contains(Skyrim.Keyword.ActorTypeGhost)) continue;

                if (npc.Race.TryResolve(state.LinkCache, out var race) && race.EditorID != null && armorRaces.Contains(race.EditorID.ToString()))
                {
                    var perk = perkLink.DeepCopy();
                    var npcCopy = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                    if (npcCopy.Perks == null) npcCopy.Perks = new ExtendedList<PerkPlacement>();
                    PerkPlacement p = new PerkPlacement
                    {
                        Rank = 1,
                        Perk = perk
                    };
                    npcCopy.Perks.Add(p);
                }
            }

            // Part 2
            // Adjust the magnitude of KYE's effects according to the effectIntensity settings

            if (!effectIntensity.EqualsWithin(1))
            {
                Perk perk = perkLink.DeepCopy();
                foreach (var eff in perk.Effects)
                {
                    if (!(eff is PerkEntryPointModifyValue epValue)) continue;
                    if (epValue.EntryPoint != APerkEntryPointEffect.EntryType.ModIncomingDamage || epValue.EntryPoint != APerkEntryPointEffect.EntryType.ModIncomingSpellMagnitude) continue;

                    epValue.Value = AdjustEffectMagnitude(epValue.Value, effectIntensity);
                }
                state.PatchMod.Perks.GetOrAddAsOverride(perk);
            }

            // Part 3
            // Add the keywords to each armor
            foreach (var armor in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorGetter>())
            {
                if (armor.EditorID == null) continue;
                if (ignoredArmors.Contains(armor.EditorID)) continue;
                if (armor.Keywords != null && !armor.Keywords.Contains(Skyrim.Keyword.ArmorCuirass)) continue;
                if (!armor.TemplateArmor.IsNull) continue;
                if (armorRulesJson[armor.EditorID.ToString()] == null) continue;

                Armor armorCopy = armor.DeepCopy();
                JArray keywords = (JArray)armorRulesJson[armor.EditorID.ToString()]!["keywords"]!;

                foreach(string? keyword in keywords)
                {
                    if (keyword != null) armorCopy!.Keywords!.Add(armorKeywords[keyword]);
                }
            }
        }
    }
}
