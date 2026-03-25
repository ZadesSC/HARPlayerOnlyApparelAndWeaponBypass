using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlienRace;
using RimWorld;
using UnityEngine;
using Verse;

namespace Zades.HARPlayerOnlyApparelAndWeaponBypass
{
    public sealed class HARPlayerOnlyApparelAndWeaponBypassMod : Mod
    {
        internal static HARPlayerOnlyApparelAndWeaponBypassSettings Settings;

        public HARPlayerOnlyApparelAndWeaponBypassMod(ModContentPack content)
            : base(content)
        {
            Settings = GetSettings<HARPlayerOnlyApparelAndWeaponBypassSettings>();
        }

        public override string SettingsCategory()
        {
            return "HAR Player-Only Apparel and Weapon Bypass";
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

    public sealed class HARPlayerOnlyApparelAndWeaponBypassSettings : ModSettings
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

                object harmony = CreateHarmonyInstance(harmonyType, "zades.harplayeronlyapparelandweaponbypass");
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

                MethodInfo harApparelScoreMethod = GetRequiredNamedMethod(
                    typeof(HarmonyPatches),
                    "ApparelScoreGainPostFix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
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
                MethodInfo harPawnCanWearPostfixMethod = GetRequiredNamedMethod(
                    typeof(HarmonyPatches),
                    "PawnCanWearPostfix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo ourHarPawnCanWearPostfixPrefix = GetRequiredMethod(
                    patchType,
                    "HarPawnCanWearPostfixPrefix",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    typeof(Pawn));
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
                    harPawnCanWearPostfixMethod,
                    prefix: harmonyMethodCtor.Invoke(new object[] { ourHarPawnCanWearPostfixPrefix }),
                    postfix: null,
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
                    pawnGetGizmosMethod,
                    prefix: null,
                    postfix: harmonyMethodCtor.Invoke(new object[] { ourPawnGetGizmosPostfix }),
                    transpiler: null,
                    finalizer: null);
                Log.Message("[HARPlayerOnlyApparelAndWeaponBypass] Applied patches.");
            }
            catch (Exception ex)
            {
                Log.Error("[HARPlayerOnlyApparelAndWeaponBypass] Failed to apply patches: " + ex);
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
            MethodInfo method = type.GetMethod(name, flags, null, parameterTypes, null);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, name);
            }

            return method;
        }

        private static MethodInfo GetRequiredNamedMethod(Type type, string name, BindingFlags flags)
        {
            MethodInfo[] methods = type.GetMethods(flags)
                .Where(method => string.Equals(method.Name, name, StringComparison.Ordinal))
                .ToArray();
            if (methods.Length == 0)
            {
                throw new MissingMethodException(type.FullName, name);
            }
            if (methods.Length > 1)
            {
                throw new AmbiguousMatchException(type.FullName + "." + name);
            }

            return methods[0];
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

        public static bool HarPawnCanWearPostfixPrefix(Pawn pawn)
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

        public static void PawnGetGizmosPostfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!Helpers.IsEligiblePawn(__instance) || __instance.Spawned == false)
            {
                return;
            }

            if (Helpers.ShouldShowApparelGizmo())
            {
                __result = Helpers.AppendGizmo(__result, Helpers.MakeApparelBypassGizmo(__instance));
            }

            if (Helpers.ShouldShowWeaponGizmo())
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
                return HARPlayerOnlyApparelAndWeaponBypassMod.Settings == null || HARPlayerOnlyApparelAndWeaponBypassMod.Settings.defaultApparelBypassEnabled;
            }
        }

        internal static bool DefaultWeaponBypassEnabled
        {
            get
            {
                return HARPlayerOnlyApparelAndWeaponBypassMod.Settings != null && HARPlayerOnlyApparelAndWeaponBypassMod.Settings.defaultWeaponBypassEnabled;
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

        internal static bool ShouldShowApparelGizmo()
        {
            return HARPlayerOnlyApparelAndWeaponBypassMod.Settings == null || HARPlayerOnlyApparelAndWeaponBypassMod.Settings.showApparelGizmo;
        }

        internal static bool ShouldShowWeaponGizmo()
        {
            return HARPlayerOnlyApparelAndWeaponBypassMod.Settings == null || HARPlayerOnlyApparelAndWeaponBypassMod.Settings.showWeaponGizmo;
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

    }
}
