using System;
using System.Collections.Generic;

using System.Reflection;
using System.Reflection.Emit;

using RimWorld;

using Verse;

#if DEBUG
#warning Compiling in Debug mode
#endif

namespace NoNutrientIngredients;

[StaticConstructorOnStartup]
public static class NoNutrientIngredients
{
    static NoNutrientIngredients()
    {
#if v1_5
        const string GAME_VERSION = "v1.5";
#elif v1_6
        const string GAME_VERSION = "v1.6";
#else
#error No version defined
        const string GAME_VERSION = "UNDEFINED";
#endif

#if DEBUG
        const string BUILD = "Debug";
#else
        const string BUILD = "Release";
#endif
        Log.Message(
            $"[NoNutrientIngredients] Running Version {Assembly.GetAssembly(typeof(NoNutrientIngredients)).GetName().Version} {BUILD} compiled for RimWorld version {GAME_VERSION}"
                + BUILD
        );
        HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("dev.tobot.rimworld.nonutrientingredients");
#if DEBUG
        HarmonyLib.Harmony.DEBUG = true;
#endif
        harmony.PatchAll();
        // get VNPE.Building_NutrientGrinder.TryProducePaste if present
        Type grinderType = Type.GetType("VNPE.Building_NutrientGrinder, VNPE");
        if (grinderType != null)
        {
            Log.Message("[NoNutrientIngredients] VNPE Detected, applying patch");
            harmony.Patch(grinderType.GetMethod("TryProducePaste", BindingFlags.NonPublic | BindingFlags.Instance), transpiler: new HarmonyLib.HarmonyMethod(typeof(NoNutrientIngredients), nameof(VNPE_Grinder_TryProducePastePatch)));
        }
    }

    [HarmonyLib.HarmonyPatch(
        typeof(Building_NutrientPasteDispenser),
        nameof(Building_NutrientPasteDispenser.TryDispenseFood)
    )]
    public static class NutrientPatch
    {
        public static void Postfix(ref Thing __result) => __result.TryGetComp<CompIngredients>()?.ingredients.Clear();
    }

    /// <summary>
    /// Harmony Transpiler Patch for Building_NutrientGrinder.TryProducePaste to clear ingredients instead
    /// </summary>
    public static IEnumerable<HarmonyLib.CodeInstruction> VNPE_Grinder_TryProducePastePatch(IEnumerable<HarmonyLib.CodeInstruction> instructions)
        {
            HarmonyLib.CodeMatcher codeMatcher = new HarmonyLib.CodeMatcher(instructions);
            _ = codeMatcher.MatchStartForward(new HarmonyLib.CodeMatch(new HarmonyLib.CodeInstruction(
                OpCodes.Callvirt,
                HarmonyLib.AccessTools.Method(typeof(CompIngredients), "RegisterIngredient")
            )))
            .Advance(-2)
            .RemoveInstructions(3)
            .Insert(
                new HarmonyLib.CodeInstruction(
                    OpCodes.Ldfld,
                    HarmonyLib.AccessTools.Field(typeof(CompIngredients), "ingredients")
                ),
                new HarmonyLib.CodeInstruction(
                    OpCodes.Callvirt,
                    HarmonyLib.AccessTools.Method(typeof(List<ThingDef>), "Clear")
                )
            );

            return codeMatcher.Instructions();
        }
}
