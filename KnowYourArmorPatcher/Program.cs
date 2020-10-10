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
using System.Text;
using System.Globalization;

namespace KnowYourArmorPatcher
{
    public class Program
    {
        static ModKey KnowYourEnemy = ModKey.FromNameAndExtension("know_your_enemy.esp");
        static ModKey elementalDestruction = ModKey.FromNameAndExtension("Elemental Destruction.esp");
        static ModKey knowYourElements = ModKey.FromNameAndExtension("Know Your Elements.esp");
        static ModKey shadowSpellPackage = ModKey.FromNameAndExtension("ShadowSpellPackage.esp");
        static ModKey kyeLightAndShadow = ModKey.FromNameAndExtension("KYE Light and Shadow.esp");

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
            return jObject.ContainsKey(key) ? jObject[key]!.Select(x => (string?)x).Where(x => x != null).Select(x => x!).ToList() : new List<string>();
        }

        private static readonly (string Key, uint Id)[] armorKeywordsTuple =
        {
            ("full", 0x0B6D03),
            ("warm", 0x0B6D04),
            ("leathery", 0x0B6D05),
            ("brittle", 0x0B6D06),
            ("nonconductive", 0x0B6D07),
            ("thick", 0x0B6D08),
            ("metal", 0x0B6D09),
            ("layered", 0x0B6D0A),
            ("deep", 0x0B6D0B),
        };

        private static void QuickAppend(StringBuilder description, string name, float num)
        {
            // The en-US is to make the whole numbers and decimals split with a . instead of a ,
            if (num != 1) description.Append(" " + name + " x" + Math.Round(num, 2).ToString(new CultureInfo("en-US")) + ",");
        }
        private static string GenerateDescription(SynthesisState<ISkyrimMod, ISkyrimModGetter> state, string recordEDID, JObject armorRulesJson, float effectIntensity)
        {
            StringBuilder description = new StringBuilder();
            if (armorRulesJson[recordEDID] != null)
            {
                if (armorRulesJson[recordEDID]!["material"] != null)
                {
                    description.Append("Material: " + armorRulesJson[recordEDID]!["material"]!.ToString());
                }
                if (armorRulesJson[recordEDID]!["construction"] != null)
                {
                    if (description.Length != 0) description.Append("; ");
                    description.Append("Construction: " + armorRulesJson[recordEDID]!["construction"]);
                }
                if (description.Length != 0) description.Append(".");

                if (armorRulesJson[recordEDID]!["keywords"] != null)
                {
                    float fire = 1,
                        frost = 1,
                        shock = 1,
                        blade = 1,
                        axe = 1,
                        blunt = 1,
                        arrows = 1,
                        earth = 1,
                        water = 1,
                        wind = 1;

                    string[] keywords = ((JArray)(armorRulesJson[recordEDID]!["keywords"]!)).ToObject<string[]>()!;
                    if (keywords.Contains("warm"))
                    {
                        arrows *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        frost *= AdjustEffectMagnitude(0.5f, effectIntensity);
                        water *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        wind *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }

                    if (keywords.Contains("leathery"))
                    {
                        arrows *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        fire *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        wind *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        water *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    if (keywords.Contains("brittle"))
                    {
                        blunt *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        water *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        earth *= AdjustEffectMagnitude(1.25f, effectIntensity);
                    }
                    if (keywords.Contains("nonconductive"))
                    {
                        shock *= AdjustEffectMagnitude(0.25f, effectIntensity);
                        fire *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        frost *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        water *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    if (keywords.Contains("thick"))
                    {
                        arrows *= AdjustEffectMagnitude(0.5f, effectIntensity);
                        blade *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        wind *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    if (keywords.Contains("metal"))
                    {
                        arrows *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        blade *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        shock *= AdjustEffectMagnitude(1.25f, effectIntensity);
                        earth *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        water *= AdjustEffectMagnitude(1.25f, effectIntensity);
                    }
                    if (keywords.Contains("layered"))
                    {
                        arrows *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        wind *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    if (keywords.Contains("deep"))
                    {
                        blunt *= AdjustEffectMagnitude(0.5f, effectIntensity);
                        axe *= AdjustEffectMagnitude(0.75f, effectIntensity);
                        earth *= AdjustEffectMagnitude(0.75f, effectIntensity);
                    }
                    QuickAppend(description, "Fire", fire);
                    QuickAppend(description, "Frost", frost);
                    QuickAppend(description, "Shock", shock);
                    QuickAppend(description, "Blade", blade);
                    QuickAppend(description, "Axe", axe);
                    QuickAppend(description, "Blunt", blunt);
                    QuickAppend(description, "Arrows", arrows);

                    // If load order contains Know Your Elements, write descriptions for water + wind + earth
                    if (state.LoadOrder.ContainsKey(ModKey.FromNameAndExtension("Know Your Elements.esp")))
                    {
                        QuickAppend(description, "Water", water);
                        QuickAppend(description, "Wind", wind);
                        QuickAppend(description, "Earth", earth);
                    }

                    // Remove last char if ending with ,
                    if (description[description.Length - 1] == ',') description[description.Length - 1] = '.';
                }
            }
            return description.ToString();
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!state.LoadOrder.ContainsKey(KnowYourEnemy))
                throw new Exception("ERROR: Know Your Enemy not detected in load order. You need to install KYE prior to running this patcher!");

            if (state.LoadOrder.ContainsKey(elementalDestruction) && !state.LoadOrder.ContainsKey(knowYourElements))
                Console.WriteLine("WARNING: Elemental Destruction Magic detected. For full compatibility with Know Your Enemy please install Know Your Elements!");

            if (!state.LoadOrder.ContainsKey(elementalDestruction) && state.LoadOrder.ContainsKey(knowYourElements))
                Console.WriteLine("WARNING: Know Your Elements detected, but Elemental Destruction Magic was not found!");

            if (state.LoadOrder.ContainsKey(shadowSpellPackage) && !state.LoadOrder.ContainsKey(kyeLightAndShadow))
                Console.WriteLine("WARNING: Shadow Spells Package detected. For full compatibility with Know Your Enemy please install Know Your Enemy Light and Shadow!");

            if (!state.LoadOrder.ContainsKey(shadowSpellPackage) && state.LoadOrder.ContainsKey(kyeLightAndShadow))
                Console.WriteLine("WARNING: Know Your Enemy Light and Shadow detected, but Shadow Spells Package was not found!");




            string[] requiredFiles = { "armor_rules.json", "misc.json", "settings.json"};
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
            bool patchArmorDescriptions = (bool)settingsJson["patch_armor_descriptions"]!;

            Dictionary<string, FormKey> armorKeywords = armorKeywordsTuple
                .Select(t =>
                {
                    if (state.LinkCache.TryLookup<IKeywordGetter>(KnowYourEnemy.MakeFormKey(t.Id), out var keyword))
                    {
                        return (t.Key, Keyword: keyword.FormKey);
                    }
                    else
                    {
                        throw new Exception($"Failed to find perk with key: {t.Key} and id {t.Id}");
                    }
                })
                .ToDictionary(x => x.Key, x => x.Keyword, StringComparer.OrdinalIgnoreCase);

            var perkForm = KnowYourEnemy.MakeFormKey(0x0B6D0D);
            if (!state.LinkCache.TryLookup<IPerkGetter>(perkForm, out var perkLink))
                throw new Exception($"Unable to find required perk: {perkForm}");

            // Returns all keywords from an armor that are found in armor rules json 
            List<string> GetRecognizedKeywords(IArmorGetter armor)
            {
                List<string> foundEDIDs = new List<string>();
                if (armor.Keywords == null) return foundEDIDs;
                foreach (var keyword in armor.Keywords)
                {
                    if (keyword.TryResolve(state.LinkCache, out var kw))
                    {
                        if (kw.EditorID != null && armorRulesJson![kw.EditorID] != null)
                        {
                            // Make sure ArmorMaterialIron comes first - fixes weird edge case generating descriptions when ArmorMaterialIronBanded is also in there
                            if (kw.FormKey == Skyrim.Keyword.ArmorMaterialIronBanded)
                            {
                                foundEDIDs.Insert(0, kw.EditorID);
                            }
                            else
                            {
                                foundEDIDs.Add(kw.EditorID);
                            }
                        }
                    }
                }
                return foundEDIDs;
            }

            // Part 1
            // Add the armor perk to all relevant NPCs
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList)) continue;

                if (npc.Keywords != null && npc.Keywords.Contains(Skyrim.Keyword.ActorTypeGhost)) continue;

                if (npc.Race.TryResolve(state.LinkCache, out var race) && race.EditorID != null && armorRaces.Contains(race.EditorID))
                {
                    var npcCopy = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                    if (npcCopy.Perks == null) npcCopy.Perks = new ExtendedList<PerkPlacement>();
                    PerkPlacement p = new PerkPlacement
                    {
                        Rank = 1,
                        Perk = perkLink.FormKey
                    };
                    npcCopy.Perks.Add(p);
                }
            }

            // Part 2
            // Adjust the magnitude of KYE's effects according to the effectIntensity settings

            if (!effectIntensity.EqualsWithin(1))
            {
                var perk = state.PatchMod.Perks.GetOrAddAsOverride(perkLink);
                foreach (var eff in perk.Effects)
                {
                    if (!(eff is PerkEntryPointModifyValue epValue)) continue;
                    if (epValue.EntryPoint != APerkEntryPointEffect.EntryType.ModIncomingDamage || epValue.EntryPoint != APerkEntryPointEffect.EntryType.ModIncomingSpellMagnitude) continue;

                    epValue.Value = AdjustEffectMagnitude(epValue.Value, effectIntensity);
                }
            }

            // Part 3
            // Add the keywords to each armor (and optionally add descriptions)
            foreach (var armor in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorGetter>())
            {
                if (armor.EditorID == null || ignoredArmors.Contains(armor.EditorID)) continue;
                if (armor.Keywords == null || !armor.Keywords.Contains(Skyrim.Keyword.ArmorCuirass)) continue;
                if (!armor.TemplateArmor.IsNull) continue;

                List<string> foundEDIDs = GetRecognizedKeywords(armor);
                if (armorRulesJson[armor.EditorID] == null && !foundEDIDs.Any()) continue;

                List<string> armorKeywordsToAdd = new List<string>();

                var armorCopy = state.PatchMod.Armors.GetOrAddAsOverride(armor);

                foreach (string foundEDID in foundEDIDs)
                {
                    // Get KYE keywords connected to recognized armor keyword
                    foreach (string keywordToAdd in ((JArray)armorRulesJson[foundEDID]!["keywords"]!).ToObject<string[]>()!)
                    {
                        if (!armorKeywordsToAdd.Contains(keywordToAdd))
                            armorKeywordsToAdd.Add(keywordToAdd);
                    }
                    if (patchArmorDescriptions)
                    {
                        string desc = GenerateDescription(state, foundEDID, armorRulesJson, effectIntensity);
                        if (!String.IsNullOrEmpty(desc)) armorCopy.Description = desc;
                    }
                }

                if (armorRulesJson[armor.EditorID] != null)
                {
                    foreach (string? keywordToAdd in ((JArray)armorRulesJson[armor.EditorID]!["keywords"]!).ToObject<string[]>()!)
                    {
                        if (keywordToAdd != null && !armorKeywordsToAdd.Contains(keywordToAdd))
                        {
                            armorKeywordsToAdd.Add(keywordToAdd);
                        }

                    }
                    if (patchArmorDescriptions) armorCopy.Description = GenerateDescription(state, armor.EditorID, armorRulesJson, effectIntensity);
                }

                // Add keywords that are to be added to armor
                foreach (var keyword in armorKeywordsToAdd)
                {
                    armorCopy.Keywords!.Add(armorKeywords[keyword]);
                }
            }
        }
    }
}
