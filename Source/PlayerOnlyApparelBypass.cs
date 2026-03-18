using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlienRace;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PlayerOnlyApparelBypass
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            try
            {
                Type patchType = typeof(Bootstrap);
                Assembly harmonyAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "0Harmony", StringComparison.Ordinal));
                if (harmonyAssembly == null)
                {
                    throw new InvalidOperationException("0Harmony assembly is not loaded.");
                }

                Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", false) ??
                    harmonyAssembly.GetType("Harmony.HarmonyInstance", true);
                Type harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod", false) ??
                    harmonyAssembly.GetType("Harmony.HarmonyMethod", true);

                object harmony = CreateHarmonyInstance(harmonyType, "local.playeronlyapparelbypass");
                MethodInfo patchMethod = harmonyType.GetMethod("Patch", BindingFlags.Public | BindingFlags.Instance);
                if (patchMethod == null)
                {
                    throw new MissingMethodException(harmonyType.FullName, "Patch");
                }

                ConstructorInfo harmonyMethodCtor = harmonyMethodType.GetConstructor(new[] { typeof(MethodInfo) });
                if (harmonyMethodCtor == null)
                {
                    throw new MissingMethodException(harmonyMethodType.FullName, ".ctor(MethodInfo)");
                }

                MethodInfo harApparelScoreMethod = GetRequiredMethod(
                    typeof(HarmonyPatches),
                    "ApparelScoreGainPostFix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Pawn),
                    typeof(Apparel),
                    typeof(float).MakeByRefType());
                MethodInfo ourApparelScorePrefix = GetRequiredMethod(
                    patchType,
                    "ApparelScoreGainPostFixPrefix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Pawn));
                MethodInfo hasPartsToWearMethod = GetRequiredMethod(
                    typeof(ApparelUtility),
                    "HasPartsToWear",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Pawn),
                    typeof(ThingDef));
                MethodInfo ourHasPartsToWearPostfix = GetRequiredMethod(
                    patchType,
                    "HasPartsToWearPostfix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Pawn),
                    typeof(ThingDef),
                    typeof(bool).MakeByRefType());
                MethodInfo apparelPawnCanWearMethod = GetRequiredMethod(
                    typeof(Apparel),
                    "PawnCanWear",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    typeof(Pawn),
                    typeof(bool));
                MethodInfo ourApparelPawnCanWearPostfix = GetRequiredMethod(
                    patchType,
                    "ApparelPawnCanWearPostfix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Apparel),
                    typeof(Pawn),
                    typeof(bool),
                    typeof(bool).MakeByRefType());
                MethodInfo canEquipMethod = GetRequiredMethod(
                    typeof(EquipmentUtility),
                    "CanEquip",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Thing),
                    typeof(Pawn),
                    typeof(string).MakeByRefType(),
                    typeof(bool));
                MethodInfo ourCanEquipPostfix = GetRequiredMethod(
                    patchType,
                    "CanEquipPostfix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Thing),
                    typeof(Pawn),
                    typeof(string).MakeByRefType(),
                    typeof(bool),
                    typeof(bool).MakeByRefType());
                MethodInfo tryGiveJobMethod = GetRequiredMethod(
                    typeof(JobGiver_OptimizeApparel),
                    "TryGiveJob",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    typeof(Pawn));
                MethodInfo ourTryGiveJobPrefix = GetRequiredMethod(
                    patchType,
                    "TryGiveJobPrefix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(JobGiver_OptimizeApparel),
                    typeof(Pawn),
                    typeof(Job).MakeByRefType());

                PatchMethod(
                    harmony,
                    patchMethod,
                    harApparelScoreMethod,
                    prefix: harmonyMethodCtor.Invoke(new object[] { ourApparelScorePrefix }),
                    postfix: null,
                    transpiler: null,
                    finalizer: null);
                PatchMethod(
                    harmony,
                    patchMethod,
                    hasPartsToWearMethod,
                    prefix: null,
                    postfix: harmonyMethodCtor.Invoke(new object[] { ourHasPartsToWearPostfix }),
                    transpiler: null,
                    finalizer: null);
                PatchMethod(
                    harmony,
                    patchMethod,
                    apparelPawnCanWearMethod,
                    prefix: null,
                    postfix: harmonyMethodCtor.Invoke(new object[] { ourApparelPawnCanWearPostfix }),
                    transpiler: null,
                    finalizer: null);
                PatchMethod(
                    harmony,
                    patchMethod,
                    canEquipMethod,
                    prefix: null,
                    postfix: harmonyMethodCtor.Invoke(new object[] { ourCanEquipPostfix }),
                    transpiler: null,
                    finalizer: null);
                PatchMethod(
                    harmony,
                    patchMethod,
                    tryGiveJobMethod,
                    prefix: harmonyMethodCtor.Invoke(new object[] { ourTryGiveJobPrefix }),
                    postfix: null,
                    transpiler: null,
                    finalizer: null);
                Log.Message("[PlayerOnlyApparelBypass] Applied patches.");
            }
            catch (Exception ex)
            {
                Log.Error("[PlayerOnlyApparelBypass] Failed to apply patches: " + ex);
            }
        }

        private static object CreateHarmonyInstance(Type harmonyType, string id)
        {
            ConstructorInfo ctor = harmonyType.GetConstructor(new[] { typeof(string) });
            if (ctor != null)
            {
                return ctor.Invoke(new object[] { id });
            }

            MethodInfo createMethod = harmonyType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            if (createMethod != null)
            {
                return createMethod.Invoke(null, new object[] { id });
            }

            throw new MissingMethodException(harmonyType.FullName, ".ctor(string)/Create(string)");
        }

        private static MethodInfo GetRequiredMethod(Type type, string name, BindingFlags flags, params Type[] parameterTypes)
        {
            MethodInfo method = FindMethod(type, name, flags, parameterTypes);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, name);
            }

            return method;
        }

        private static MethodInfo FindMethod(Type type, string name, BindingFlags flags, params Type[] parameterTypes)
        {
            return type.GetMethod(name, flags, null, parameterTypes, null);
        }

        private static void PatchMethod(
            object harmony,
            MethodInfo patchMethod,
            MethodBase original,
            object prefix,
            object postfix,
            object transpiler,
            object finalizer)
        {
            if (original == null)
            {
                throw new ArgumentNullException("original");
            }

            var parameterCount = patchMethod.GetParameters().Length;
            if (parameterCount == 4)
            {
                patchMethod.Invoke(harmony, new[] { original, prefix, postfix, transpiler });
                return;
            }

            if (parameterCount == 5)
            {
                patchMethod.Invoke(harmony, new[] { original, prefix, postfix, transpiler, finalizer });
                return;
            }

            throw new NotSupportedException("Unexpected Harmony.Patch signature with " + parameterCount + " parameters.");
        }

        public static bool ApparelScoreGainPostFixPrefix(Pawn pawn)
        {
            return !Helpers.IsManagedPawn(pawn);
        }

        public static void HasPartsToWearPostfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (!__result && Helpers.ShouldBypassApparelChecks(p, apparel))
            {
                __result = true;
            }
        }

        public static void ApparelPawnCanWearPostfix(Apparel __instance, Pawn pawn, bool ignoreGender, ref bool __result)
        {
            if (!__result && Helpers.ShouldBypassApparelChecks(pawn, __instance != null ? __instance.def : null))
            {
                __result = true;
            }
        }

        public static void CanEquipPostfix(Thing thing, Pawn pawn, ref string cantReason, bool checkBonded, ref bool __result)
        {
            if (__result || !Helpers.ShouldBypassApparelChecks(pawn, thing != null ? thing.def : null))
            {
                return;
            }

            if (CompBiocodable.IsBiocoded(thing) && !CompBiocodable.IsBiocodedFor(thing, pawn))
            {
                return;
            }

            string ignoredReason = null;
            if (EquipmentUtility.RolePreventsFromUsing(pawn, thing, out ignoredReason))
            {
                return;
            }

            cantReason = null;
            __result = true;
        }

        public static bool TryGiveJobPrefix(JobGiver_OptimizeApparel __instance, Pawn pawn, ref Job __result)
        {
            if (!Helpers.IsManagedPawn(pawn))
            {
                return true;
            }

            __result = ManagedApparelOptimizer.TryGiveManagedJob(pawn);
            return false;
        }
    }

    internal static class Helpers
    {
        internal static bool IsManagedPawn(Pawn pawn)
        {
            return pawn != null && pawn.IsColonistPlayerControlled;
        }

        internal static bool ShouldBypassApparelChecks(Pawn pawn, ThingDef thingDef)
        {
            return IsManagedPawn(pawn) && thingDef != null && thingDef.IsApparel;
        }

        internal static FloatMenuOption MakeWearOption(Pawn pawn, Apparel apparel)
        {
            Action action = () =>
            {
                Action takeJob = delegate
                {
                    ForbidUtility.SetForbidden(apparel, false, false);
                    Job job = JobMaker.MakeJob(JobDefOf.Wear, apparel);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);
                };

                var replaced = ApparelUtility.GetApparelReplacedByNewApparel(pawn, apparel);
                if (replaced != null && ModsConfig.BiotechActive &&
                    MechanitorUtility.TryConfirmBandwidthLossFromDroppingThing(pawn, replaced, takeJob))
                {
                    return;
                }

                takeJob();
            };

            var label = "ForceWear".Translate(apparel.LabelShort, apparel) + " [Ignore race restriction]";
            var option = new FloatMenuOption(label, action, MenuOptionPriority.High, null, apparel);
            return FloatMenuUtility.DecoratePrioritizedTask(option, pawn, apparel, "ReservedBy");
        }
    }

    public static class SkipHarOptimizerPenaltyForColonists
    {
        public static bool Prefix(Pawn pawn)
        {
            return !Helpers.IsManagedPawn(pawn);
        }
    }

    public static class ManagedApparelOptimizer
    {
        public static Job TryGiveManagedJob(Pawn pawn)
        {
            if (pawn == null || pawn.outfits == null)
            {
                return null;
            }

            if (pawn.IsMutant && pawn.mutant != null && pawn.mutant.Def != null && pawn.mutant.Def.disableApparel)
            {
                return null;
            }

            if (QuestUtility.IsQuestLodger(pawn))
            {
                return null;
            }

            ApparelPolicy policy = pawn.outfits.CurrentApparelPolicy;
            if (policy == null)
            {
                return null;
            }

            List<Apparel> wornApparel = pawn.apparel != null ? pawn.apparel.WornApparel : null;
            if (wornApparel != null)
            {
                for (int i = wornApparel.Count - 1; i >= 0; i--)
                {
                    Apparel worn = wornApparel[i];
                    bool inVacuum = pawn.MapHeld != null && pawn.MapHeld.Biome != null && pawn.MapHeld.Biome.inVacuum &&
                        VacuumUtility.GetVacuum(pawn.Position, pawn.MapHeld) >= 0.5f &&
                        worn.GetStatValue(StatDefOf.VacuumResistance, true, 60) > 0f;
                    if (inVacuum)
                    {
                        continue;
                    }

                    if (!policy.filter.Allows(worn) &&
                        pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(worn) &&
                        !pawn.apparel.IsLocked(worn))
                    {
                        Job removeJob = JobMaker.MakeJob(JobDefOf.RemoveApparel, worn);
                        removeJob.haulDroppedApparel = true;
                        return removeJob;
                    }
                }
            }

            List<float> wornScores = new List<float>();
            if (wornApparel != null)
            {
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    wornScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, wornApparel[i]));
                }
            }

            Apparel best = null;
            float bestScore = 0f;
            List<Thing> things = pawn.Map != null ? pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel) : null;
            if (things != null)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Apparel apparel = things[i] as Apparel;
                    if (apparel == null)
                    {
                        continue;
                    }

                    if (!policy.filter.Allows(apparel) ||
                        ForbidUtility.IsForbidden(apparel, pawn) ||
                        FireUtility.IsBurning(apparel))
                    {
                        continue;
                    }

                    float score = JobGiver_OptimizeApparel.ApparelScoreGain(pawn, apparel, wornScores);
                    if (score < 0.05f || score < bestScore)
                    {
                        continue;
                    }

                    if (CompBiocodable.IsBiocoded(apparel) && !CompBiocodable.IsBiocodedFor(apparel, pawn))
                    {
                        continue;
                    }

                    if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                    {
                        continue;
                    }

                    LocalTargetInfo target = apparel;
                    IApparelSource source = apparel.ParentHolder as IApparelSource;
                    Thing sourceThing = source as Thing;
                    if (source != null && sourceThing != null)
                    {
                        if (!source.ApparelSourceEnabled)
                        {
                            continue;
                        }

                        target = sourceThing;
                    }

                    if (!ReservationUtility.CanReserveAndReach(
                        pawn,
                        target,
                        PathEndMode.OnCell,
                        DangerUtility.NormalMaxDanger(pawn),
                        1,
                        -1,
                        null,
                        false))
                    {
                        continue;
                    }

                    best = apparel;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                return null;
            }

            return JobMaker.MakeJob(JobDefOf.Wear, best);
        }
    }
}
