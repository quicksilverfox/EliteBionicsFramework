﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace EBF.Transpilations
{
    [StaticConstructorOnStartup]
    public static class Transpiler_DamageWorker_Blunt_SpecialEffects
    {
        /* 
         * Fix transpiler conflict with Humanoid Alien Races
         * HAR's HarmonyPatches.BodyReferenceTranspiler method patches DamageWorker_Blunt.ApplySpecialeffectsToPart method also.
         * Fortunately both transpilers will work if EBF's transpiler is applied first, but this requires that EBF is loaded
         * before HAR, and that EBF's harmony patch is done in the static constructor because HAR's harmony patch is done
         * in a static constructor.
         * 
         * Special thanks to GitHub user RocketDelivery for this fix
         * 
         * ------
         * 
         * Update 2022-Nov:
         * 
         * The fix by RocketDelivery works when used with HAR, but we eventually discovered Yayo Animations doing their patches super-early,
         * and therefore summoned Humanoid Alien Races too early without our consent. This therefore breaks our compatiility with HAR.
         * To maintain parity against Yayo Animations, we must therefore call this static constructor in the constructor of a child class of Verse.Mod.
         * I have made a new fitting child class that, innu, touches this class, so that this static constructor is called.
         * And obviously, the [StaticConstructorOnStartup] becomes a decoration, but I am keeping that annotation to avoid confusion.
         */
        static Transpiler_DamageWorker_Blunt_SpecialEffects()
		{
            Harmony harmony = new Harmony("rimworld.vectorial1024.ebf.damageworker_blunt");
            harmony.Patch(
                AccessTools.Method(typeof(DamageWorker_Blunt), "ApplySpecialEffectsToPart"), 
                null,
                null,
                new HarmonyMethod(typeof(Transpiler_DamageWorker_Blunt_SpecialEffects), nameof(TranspileTheTarget)));
		}

        public static IEnumerable<CodeInstruction> TranspileTheTarget(IEnumerable<CodeInstruction> instructions)
        {
            if (instructions == null)
            {
                // we are simply touching this; don't actually do it
                // we trust Harmony to never give us null
                // check the method usage to see where we are touching this.
                return instructions;
            }

            /*
             * A total of 1 GetMaxHealth occurences detected;
             * Patch with CodeMatcher
             */

            /*
             * This is to patch for "get core part max health" in the function ApplySpecialEffectsToPart(...)
             * for class DamageWorker_Blunt.
             * From what we know, RW generates a lot of nested compiler-generated class,
             * and among them, there is one under DamageWorker_Blunt that stores info related to the blunt-stun effects.
             * 
             * This generated class may have different names, but from what we know, this class:
             * - has a field named "pawn"
             * - such field has type Pawn (this should be less important, since we can't have two fields with the same name but with different types)
             * 
             * I am assuming there is only one such class among all the nested classes under DamageWorker_Blunt.
             */
            Type typeSelfAnon = typeof(DamageWorker_Blunt).GetNestedTypes(BindingFlags.NonPublic).Where((Type type) => type.GetField("pawn") != null).First();
            if (typeSelfAnon == null)
            {
                EliteBionicsFrameworkMain.LogError("Patch failed: blunt special effects, failed to find relevant self-anon type!");
                // In the unlikely case of failing the search, modify nothing and return.
                return instructions;
            }

            // Search successful.
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BodyPartDef), nameof(BodyPartDef.GetMaxHealth)))
                ) // find the only occurence of .GetMaxHealth()
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeSelfAnon, "pawn")),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "def")),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "race")),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RaceProperties), "body")),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BodyDef), "corePart")),
                    new CodeInstruction(OpCodes.Call, VanillaExtender.ReflectionGetMaxHealth())
                ) // insert extra code so that we use VanillaExtender.GetMaxHealth(); we do this out of convenience
                .Set(OpCodes.Nop, null) // and ignore the original instruction
                .InstructionEnumeration();
        }
    }
}
