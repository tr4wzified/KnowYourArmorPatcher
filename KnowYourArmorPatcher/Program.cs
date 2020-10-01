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
using Alphaleonis.Win32.Filesystem;

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
            return 1 / AdjustDamageMod(1 / magnitude, scale);
        }

        private static IEnumerable<string> GetFromJson(string key, JObject jObject)
        {
            return jObject.ContainsKey(key) ? jObject[key]!.Select(x => (string?) x).Where(x => x != null).Select(x => x!).ToList() : new List<string>();
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!File.Exists("armor_rules.json"))
                throw new Exception("Required file armor_rules.json does not exist!");
            if (!File.Exists("misc.json"))
                throw new Exception("Required file misc.json does not exist!");
            if (!File.Exists("settings.json"))
                throw new Exception("Required file settings.json does not exist!");

            var armorRules = JObject.Parse(File.ReadAllText("armor_rules.json"));
            var misc = JObject.Parse(File.ReadAllText("misc.json"));
            var settings = JObject.Parse(File.ReadAllText("settings.json"));
            var armorRaces = GetFromJson("armor_races", misc).ToList();

            float effectIntensity = (float)settings["effect_intensity"];

            if (!state.LoadOrder.ContainsKey(ModKey.FromNameAndExtension("know_your_enemy.esp")))
            {
                Console.WriteLine("ERROR: Know Your Enemy not detected in load order. You need to install KYE prior to running this patcher!");
                return;
            }

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
            foreach (var armor in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorGetter>())
            {
                if (misc["ignored_armors"].Contains(armor.EditorID)) continue;
                if (armor.Keywords != null && armor.Keywords.Contains(Skyrim.Keyword.ArmorCuirass)) continue;
                //TODO: if (!xelib.HasElement(record, 'TNAM')) return true;
                //TODO: check if template TNAM is empty

                if (!armorRules.Contains(armor.EditorID)) continue;
                var armorTraits = armorRules[armor.EditorID];
                                                
            }
        }
    }
}
