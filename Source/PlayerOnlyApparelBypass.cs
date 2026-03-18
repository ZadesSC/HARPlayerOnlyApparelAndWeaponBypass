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
    public sealed class PlayerOnlyApparelBypassMod : Mod
    {
        internal static PlayerOnlyApparelBypassSettings Settings;

        public PlayerOnlyApparelBypassMod(ModContentPack content)
            : base(content)
        {
            Settings = GetSettings<PlayerOnlyApparelBypassSettings>();
        }

        public override string SettingsCategory()
        {
            return "Player-Only Apparel Bypass";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            bool defaultApparelBypassEnabled = Settings != null && Settings.defaultApparelBypassEnabled;
            listing.CheckboxLabeled(
                "New player colonists bypass HAR apparel restrictions by default",
                ref defaultApparelBypassEnabled,
                "This becomes the initial behavior for player-controlled colonists when they are first managed by the mod.");
            Settings.defaultApparelBypassEnabled = defaultApparelBypassEnabled;

            listing.Gap();

            bool showApparelGizmo = Settings == null || Settings.showApparelGizmo;
            listing.CheckboxLabeled(
                "Show per-pawn HAR apparel bypass gizmo",
                ref showApparelGizmo,
                "Adds a toggle gizmo to selected player-controlled colonists so each pawn can individually follow or ignore HAR apparel restrictions.");
            Settings.showApparelGizmo = showApparelGizmo;

            listing.Gap();

            bool defaultWeaponBypassEnabled = Settings != null && Settings.defaultWeaponBypassEnabled;
            listing.CheckboxLabeled(
                "New player colonists bypass HAR weapon restrictions by default",
                ref defaultWeaponBypassEnabled,
                "This becomes the initial weapon behavior for player-controlled colonists when they are first managed by the mod.");
            Settings.defaultWeaponBypassEnabled = defaultWeaponBypassEnabled;

            listing.Gap();

            bool showWeaponGizmo = Settings == null || Settings.showWeaponGizmo;
            listing.CheckboxLabeled(
                "Show per-pawn HAR weapon bypass gizmo",
                ref showWeaponGizmo,
                "Adds a separate toggle gizmo to selected player-controlled colonists so each pawn can individually follow or ignore HAR weapon restrictions.");
            Settings.showWeaponGizmo = showWeaponGizmo;

            listing.End();
        }
    }

    public sealed class PlayerOnlyApparelBypassSettings : ModSettings
    {
        public bool defaultApparelBypassEnabled = true;
        public bool showApparelGizmo = true;
        public bool defaultWeaponBypassEnabled = false;
        public bool showWeaponGizmo = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultApparelBypassEnabled, "defaultBypassEnabled", defaultApparelBypassEnabled);
            Scribe_Values.Look(ref defaultApparelBypassEnabled, "defaultApparelBypassEnabled", defaultApparelBypassEnabled);
            Scribe_Values.Look(ref showApparelGizmo, "showPawnGizmo", showApparelGizmo);
            Scribe_Values.Look(ref showApparelGizmo, "showApparelGizmo", showApparelGizmo);
            Scribe_Values.Look(ref defaultWeaponBypassEnabled, "defaultWeaponBypassEnabled", false);
            Scribe_Values.Look(ref showWeaponGizmo, "showWeaponGizmo", true);
        }
    }

    public sealed class PawnBypassStateComponent : GameComponent
    {
        private Dictionary<int, bool> apparelBypassStates = new Dictionary<int, bool>();
        private Dictionary<int, bool> weaponBypassStates = new Dictionary<int, bool>();
        private List<int> apparelStateKeys;
        private List<bool> apparelStateValues;
        private List<int> weaponStateKeys;
        private List<bool> weaponStateValues;
        private List<int> legacyStateKeys;
        private List<bool> legacyStateValues;

        public PawnBypassStateComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            Dictionary<int, bool> legacyPawnBypassStates = null;
            Scribe_Collections.Look(
                ref legacyPawnBypassStates,
                "pawnBypassStates",
                LookMode.Value,
                LookMode.Value,
                ref legacyStateKeys,
                ref legacyStateValues);
            Scribe_Collections.Look(
                ref apparelBypassStates,
                "apparelBypassStates",
                LookMode.Value,
                LookMode.Value,
                ref apparelStateKeys,
                ref apparelStateValues);
            Scribe_Collections.Look(
                ref weaponBypassStates,
                "weaponBypassStates",
                LookMode.Value,
                LookMode.Value,
                ref weaponStateKeys,
                ref weaponStateValues);

            if (apparelBypassStates == null)
            {
                apparelBypassStates = new Dictionary<int, bool>();
            }
            if (weaponBypassStates == null)
            {
                weaponBypassStates = new Dictionary<int, bool>();
            }

            if ((apparelBypassStates == null || apparelBypassStates.Count == 0) &&
                legacyPawnBypassStates != null &&
                legacyPawnBypassStates.Count > 0)
            {
                apparelBypassStates = new Dictionary<int, bool>(legacyPawnBypassStates);
            }
        }

        public bool IsApparelBypassEnabled(Pawn pawn)
        {
            if (!Helpers.IsEligiblePawn(pawn))
            {
                return false;
            }

            bool enabled;
            if (!apparelBypassStates.TryGetValue(pawn.thingIDNumber, out enabled))
            {
                enabled = Helpers.DefaultApparelBypassEnabled;
                apparelBypassStates[pawn.thingIDNumber] = enabled;
            }

            return enabled;
        }

        public bool IsWeaponBypassEnabled(Pawn pawn)
        {
            if (!Helpers.IsEligiblePawn(pawn))
            {
                return false;
            }

            bool enabled;
            if (!weaponBypassStates.TryGetValue(pawn.thingIDNumber, out enabled))
            {
                enabled = Helpers.DefaultWeaponBypassEnabled;
                weaponBypassStates[pawn.thingIDNumber] = enabled;
            }

            return enabled;
        }

        public void SetApparelBypassEnabled(Pawn pawn, bool enabled)
        {
            if (!Helpers.IsEligiblePawn(pawn))
            {
                return;
            }

            apparelBypassStates[pawn.thingIDNumber] = enabled;
        }

        public void SetWeaponBypassEnabled(Pawn pawn, bool enabled)
        {
            if (!Helpers.IsEligiblePawn(pawn))
            {
                return;
            }

            weaponBypassStates[pawn.thingIDNumber] = enabled;
        }
    }

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
                MethodInfo pawnGetGizmosMethod = GetRequiredMethod(
                    typeof(Pawn),
                    "GetGizmos",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo ourPawnGetGizmosPostfix = GetRequiredMethod(
                    patchType,
                    "PawnGetGizmosPostfix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Pawn),
                    typeof(IEnumerable<Gizmo>).MakeByRefType());

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
                PatchMethod(
                    harmony,
                    patchMethod,
                    pawnGetGizmosMethod,
                    prefix: null,
                    postfix: harmonyMethodCtor.Invoke(new object[] { ourPawnGetGizmosPostfix }),
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
            return !Helpers.IsApparelManagedPawn(pawn);
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
            ThingDef thingDef = thing != null ? thing.def : null;
            bool bypassApparel = Helpers.ShouldBypassApparelChecks(pawn, thingDef);
            bool bypassWeapon = Helpers.ShouldBypassWeaponChecks(pawn, thingDef);
            if (__result || (!bypassApparel && !bypassWeapon))
            {
                return;
            }

            CompBladelinkWeapon bladelinkComp = thing != null ? thing.TryGetComp<CompBladelinkWeapon>() : null;
            if (bladelinkComp != null && bladelinkComp.Biocodable && bladelinkComp.CodedPawn != null && bladelinkComp.CodedPawn != pawn)
            {
                return;
            }

            if (CompBiocodable.IsBiocoded(thing) && !CompBiocodable.IsBiocodedFor(thing, pawn))
            {
                return;
            }

            if (checkBonded && EquipmentUtility.AlreadyBondedToWeapon(thing, pawn))
            {
                return;
            }

            string ignoredReason = null;
            if (EquipmentUtility.RolePreventsFromUsing(pawn, thing, out ignoredReason))
            {
                return;
            }

            if (bypassApparel &&
                thingDef != null &&
                thingDef.apparel != null &&
                !thingDef.apparel.developmentalStageFilter.Has(pawn.DevelopmentalStage))
            {
                return;
            }

            cantReason = null;
            __result = true;
        }

        public static bool TryGiveJobPrefix(JobGiver_OptimizeApparel __instance, Pawn pawn, ref Job __result)
        {
            if (!Helpers.IsApparelManagedPawn(pawn))
            {
                return true;
            }

            __result = ManagedApparelOptimizer.TryGiveManagedJob(pawn);
            return false;
        }

        public static void PawnGetGizmosPostfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!Helpers.IsEligiblePawn(__instance) || __instance.Spawned == false)
            {
                return;
            }

            if (Helpers.ShouldShowApparelGizmo(__instance))
            {
                __result = Helpers.AppendGizmo(__result, Helpers.MakeApparelBypassGizmo(__instance));
            }

            if (Helpers.ShouldShowWeaponGizmo(__instance))
            {
                __result = Helpers.AppendGizmo(__result, Helpers.MakeWeaponBypassGizmo(__instance));
            }
        }
    }

    internal static class Helpers
    {
        internal static bool DefaultApparelBypassEnabled
        {
            get
            {
                return PlayerOnlyApparelBypassMod.Settings == null || PlayerOnlyApparelBypassMod.Settings.defaultApparelBypassEnabled;
            }
        }

        internal static bool DefaultWeaponBypassEnabled
        {
            get
            {
                return PlayerOnlyApparelBypassMod.Settings != null && PlayerOnlyApparelBypassMod.Settings.defaultWeaponBypassEnabled;
            }
        }

        internal static bool IsEligiblePawn(Pawn pawn)
        {
            return pawn != null && pawn.IsColonistPlayerControlled;
        }

        internal static bool IsApparelManagedPawn(Pawn pawn)
        {
            return IsEligiblePawn(pawn) && GetApparelBypassEnabled(pawn);
        }

        internal static bool IsWeaponManagedPawn(Pawn pawn)
        {
            return IsEligiblePawn(pawn) && GetWeaponBypassEnabled(pawn);
        }

        internal static bool ShouldBypassApparelChecks(Pawn pawn, ThingDef thingDef)
        {
            return IsApparelManagedPawn(pawn) && thingDef != null && thingDef.IsApparel;
        }

        internal static bool ShouldBypassWeaponChecks(Pawn pawn, ThingDef thingDef)
        {
            return IsWeaponManagedPawn(pawn) && thingDef != null && thingDef.IsWeapon;
        }

        internal static bool ShouldShowApparelGizmo(Pawn pawn)
        {
            return PlayerOnlyApparelBypassMod.Settings == null || PlayerOnlyApparelBypassMod.Settings.showApparelGizmo;
        }

        internal static bool ShouldShowWeaponGizmo(Pawn pawn)
        {
            return PlayerOnlyApparelBypassMod.Settings == null || PlayerOnlyApparelBypassMod.Settings.showWeaponGizmo;
        }

        internal static bool GetApparelBypassEnabled(Pawn pawn)
        {
            if (!IsEligiblePawn(pawn))
            {
                return false;
            }

            PawnBypassStateComponent stateComponent = Current.Game != null
                ? Current.Game.GetComponent<PawnBypassStateComponent>()
                : null;

            return stateComponent != null
                ? stateComponent.IsApparelBypassEnabled(pawn)
                : DefaultApparelBypassEnabled;
        }

        internal static bool GetWeaponBypassEnabled(Pawn pawn)
        {
            if (!IsEligiblePawn(pawn))
            {
                return false;
            }

            PawnBypassStateComponent stateComponent = Current.Game != null
                ? Current.Game.GetComponent<PawnBypassStateComponent>()
                : null;

            return stateComponent != null
                ? stateComponent.IsWeaponBypassEnabled(pawn)
                : DefaultWeaponBypassEnabled;
        }

        internal static void SetApparelBypassEnabled(Pawn pawn, bool enabled)
        {
            if (!IsEligiblePawn(pawn) || Current.Game == null)
            {
                return;
            }

            PawnBypassStateComponent stateComponent = Current.Game.GetComponent<PawnBypassStateComponent>();
            if (stateComponent != null)
            {
                stateComponent.SetApparelBypassEnabled(pawn, enabled);
            }
        }

        internal static void SetWeaponBypassEnabled(Pawn pawn, bool enabled)
        {
            if (!IsEligiblePawn(pawn) || Current.Game == null)
            {
                return;
            }

            PawnBypassStateComponent stateComponent = Current.Game.GetComponent<PawnBypassStateComponent>();
            if (stateComponent != null)
            {
                stateComponent.SetWeaponBypassEnabled(pawn, enabled);
            }
        }

        internal static IEnumerable<Gizmo> AppendGizmo(IEnumerable<Gizmo> source, Gizmo gizmo)
        {
            if (source != null)
            {
                foreach (Gizmo item in source)
                {
                    yield return item;
                }
            }

            if (gizmo != null)
            {
                yield return gizmo;
            }
        }

        internal static Gizmo MakeApparelBypassGizmo(Pawn pawn)
        {
            return new Command_Toggle
            {
                defaultLabel = "HAR apparel bypass",
                defaultDesc = BuildApparelGizmoDescription(pawn),
                icon = TexCommand.DesirePower,
                isActive = delegate
                {
                    return GetApparelBypassEnabled(pawn);
                },
                toggleAction = delegate
                {
                    bool enabled = !GetApparelBypassEnabled(pawn);
                    SetApparelBypassEnabled(pawn, enabled);
                }
            };
        }

        internal static Gizmo MakeWeaponBypassGizmo(Pawn pawn)
        {
            return new Command_Toggle
            {
                defaultLabel = "HAR weapon bypass",
                defaultDesc = BuildWeaponGizmoDescription(pawn),
                icon = TexCommand.DesirePower,
                isActive = delegate
                {
                    return GetWeaponBypassEnabled(pawn);
                },
                toggleAction = delegate
                {
                    bool enabled = !GetWeaponBypassEnabled(pawn);
                    SetWeaponBypassEnabled(pawn, enabled);
                }
            };
        }

        private static string BuildApparelGizmoDescription(Pawn pawn)
        {
            bool enabled = GetApparelBypassEnabled(pawn);
            string currentState = enabled ? "enabled" : "disabled";
            string defaultState = DefaultApparelBypassEnabled ? "enabled" : "disabled";
            return "Toggle whether this pawn ignores HAR race apparel restrictions. Current state: " +
                currentState + ". Default for newly managed player colonists: " + defaultState + ".";
        }

        private static string BuildWeaponGizmoDescription(Pawn pawn)
        {
            bool enabled = GetWeaponBypassEnabled(pawn);
            string currentState = enabled ? "enabled" : "disabled";
            string defaultState = DefaultWeaponBypassEnabled ? "enabled" : "disabled";
            return "Toggle whether this pawn ignores HAR race weapon restrictions. Current state: " +
                currentState + ". Default for newly managed player colonists: " + defaultState + ".";
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
            return !Helpers.IsApparelManagedPawn(pawn);
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
