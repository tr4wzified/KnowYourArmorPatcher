// Autogenerated by https://github.com/Mutagen-Modding/Mutagen.Bethesda.FormKeys

using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class KnowYourElements
    {
        public static class Spell
        {
            private static FormLink<ISpellGetter> Construct(uint id) => new FormLink<ISpellGetter>(ModKey.MakeFormKey(id));
            public static FormLink<ISpellGetter> kye_ab_water_elemental => Construct(0x5909);
            public static FormLink<ISpellGetter> kye_ab_wind_elemental => Construct(0x5907);
            public static FormLink<ISpellGetter> kye_ab_earth_elemental => Construct(0x5905);
        }
    }
}
