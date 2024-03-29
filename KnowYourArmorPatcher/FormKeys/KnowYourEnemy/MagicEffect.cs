// Autogenerated by https://github.com/Mutagen-Modding/Mutagen.Bethesda.FormKeys

using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace Mutagen.Bethesda.FormKeys.SkyrimSE
{
    public static partial class KnowYourEnemy
    {
        public static class MagicEffect
        {
            private static FormLink<IMagicEffectGetter> Construct(uint id) => new FormLink<IMagicEffectGetter>(ModKey.MakeFormKey(id));
            public static FormLink<IMagicEffectGetter> kye_fur_description => Construct(0x887);
            public static FormLink<IMagicEffectGetter> kye_mgef_weakness_poison => Construct(0x818);
            public static FormLink<IMagicEffectGetter> kye_mgef_weakness_disease => Construct(0x819);
            public static FormLink<IMagicEffectGetter> kye_mgef_dummy => Construct(0x848);
            public static FormLink<IMagicEffectGetter> kye_leather_description => Construct(0x888);
            public static FormLink<IMagicEffectGetter> kye_inspect_mgef => Construct(0x885);
            public static FormLink<IMagicEffectGetter> kye_metal_description => Construct(0x889);
            public static FormLink<IMagicEffectGetter> kye_nonconductive_description => Construct(0x88a);
            public static FormLink<IMagicEffectGetter> kye_thick_description => Construct(0x88b);
            public static FormLink<IMagicEffectGetter> kye_layered_description => Construct(0x88c);
            public static FormLink<IMagicEffectGetter> kye_deep_description => Construct(0x88d);
        }
    }
}
