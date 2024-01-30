using HarmonyLib;
using SRML;
using SRML.Console;
using System.Reflection;
using Challenges;
using SRML.SR;
using UnityEngine;

namespace SomeExtraChallenges
{
    public class Main : ModEntryPoint
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{System.Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";

        public override void PreLoad() {
            HarmonyInstance.PatchAll();
            TranslationPatcher.AddPediaTranslation("c.name.waterproofTarrs", "Waterproof Tarrs");
            TranslationPatcher.AddPediaTranslation("c.desc.waterproofTarrs", "Tarrs are no longer affected by getting wet");
            TranslationPatcher.AddPediaTranslation("c.name.marketCrash", "Market Crash");
            TranslationPatcher.AddPediaTranslation("c.desc.marketCrash", "All plorts sell for 1 newbuck");
            TranslationPatcher.AddPediaTranslation("c.name.empty", "The Empty");
            TranslationPatcher.AddPediaTranslation("c.desc.empty", "No life exists in the open world");
        }

        public override void Load()
        {
            new Challenge(
                "waterproofTarrs",
                "c.name.waterproofTarrs",
                "c.desc.waterproofTarrs",
                SceneContext.Instance.PediaDirector.entryDict[PediaDirector.Id.TARR_SLIME].icon,
                Challenge.ChallengeType.Bad,
                null,
                null
            );
            new Challenge(
                "marketCrash",
                "c.name.marketCrash",
                "c.desc.marketCrash",
                SceneContext.Instance.PediaDirector.entryDict[PediaDirector.Id.PLORT_MARKET].icon,
                Challenge.ChallengeType.Bad,
                null,
                () => Patch_EconomyDirector.Prefix(SceneContext.Instance.EconomyDirector)
            );
            new Challenge(
                "empty",
                "c.name.empty",
                "c.desc.empty",
                SceneContext.Instance.PediaDirector.entryDict[PediaDirector.Id.DESERT].icon,
                Challenge.ChallengeType.Bad,
                null,
                () =>
                {
                    foreach (var s in Resources.FindObjectsOfTypeAll<SpawnResource>())
                    {
                        foreach (var i in s.spawned)
                            Object.DestroyImmediate(i);
                        s.spawned.Clear();
                    }
                }
            );
        }
    }

    [HarmonyPatch(typeof(EconomyDirector),"ResetPrices")]
    class Patch_EconomyDirector
    {
        public static bool Prefix(EconomyDirector __instance)
        {
            if (!Challenge.IsActive("marketCrash"))
                return true;
            foreach (var p in __instance.currValueMap.Values)
            {
                p.prevValue = 1;
                p.currValue = 1;
            }
            __instance.didUpdateDelegate?.Invoke();
            return false;
        }
    }

    [HarmonyPatch(typeof(TarrSterilizeOnWater), "AddLiquid")]
    class Patch_TarrDamageOnWater { static bool Prefix() => !Challenge.IsActive("waterproofTarrs"); }

    [HarmonyPatch(typeof(DirectedActorSpawner), "Spawn")]
    class Patch_SpawnActors { static void Prefix(DirectedActorSpawner __instance, ref int count) { if ((__instance is DirectedSlimeSpawner || __instance is DirectedAnimalSpawner) && Challenge.IsActive("empty")) count = 0; } }

    [HarmonyPatch(typeof(CellDirector), "CanSpawnSlimes")]
    class Patch_CanSpawnSlimes { static void Postfix(ref bool __result) { if (Challenge.IsActive("empty")) __result = false; } }

    [HarmonyPatch(typeof(GameObjectActorModelIdentifiableIndex), "GetSlimeCount")]
    class Patch_GetSlimeCount { static void Postfix(ref int __result) { if (Challenge.IsActive("empty")) __result = 0; } }

    [HarmonyPatch(typeof(GameObjectActorModelIdentifiableIndex), "GetLargoCount")]
    class Patch_GetLargoCount { static void Postfix(ref int __result) { if (Challenge.IsActive("empty")) __result = 0; } }

    [HarmonyPatch(typeof(SpawnResource), "Spawn", typeof(System.Collections.Generic.IEnumerable<SpawnResource.SpawnMetadata>))]
    class Patch_UpdateResourceSpawner { static bool Prefix(SpawnResource __instance) => __instance.landPlot || !Challenge.IsActive("empty"); }
}