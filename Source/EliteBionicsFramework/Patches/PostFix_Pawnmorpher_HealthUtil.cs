﻿using EBF.Util;
using HarmonyLib;
using System.Reflection;

namespace EBF.Patches
{
    [HarmonyPatch]
    public class PostFix_Pawnmorpher_HealthUtil
    {
        public static bool Prepare()
        {
            return ModDetector.PawnmorpherIsLoaded;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("Pawnmorph.BodyUtilities:GetPartMaxHealth");
        }

        [HarmonyPrefix]
        public static void PreFix()
        {
            // the flow has changed
            // we approve of Pawnmorpher reading the original values, and then we modify their value to become an EBF-accepted value
            PostFix_BodyPart_GetMaxHealth.SuppressNextWarning();
        }
    }
}
