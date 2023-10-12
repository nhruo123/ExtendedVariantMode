using Celeste;
using Celeste.Mod;
using ExtendedVariants.Module;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace ExtendedVariants.Variants
{
    public class RestoreCustomDashStateOnRespawn : AbstractExtendedVariant
    {
        private static List<ILHook> ilHooks = new List<ILHook>();

        public override Type GetVariantType()
        {
            return typeof(bool);
        }

        public override object GetDefaultVariantValue()
        {
            return false;
        }

        public override object ConvertLegacyVariantValue(int value)
        {
            return value != 0;
        }

        public override void Load()
        {
            ilHooks.Add(new ILHook(typeof(Cassette).GetMethod("CollectRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), updateDashCountOnRespawnPointChange));
            ilHooks.Add(new ILHook(typeof(Level).GetMethod("orig_TransitionRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(), updateDashCountOnRespawnPointChange));

            IL.Celeste.SummitCheckpoint.Awake += updateDashCountOnRespawnPointChange;
            IL.Celeste.SummitCheckpoint.Update += updateDashCountOnRespawnPointChange;
            IL.Celeste.ChangeRespawnTrigger.OnEnter += updateDashCountOnRespawnPointChange;
            IL.Celeste.Level.TeleportTo += updateDashCountOnRespawnPointChange;

            On.Celeste.Player.Added += onPlayerSpawn;
        }

        public override void Unload()
        {
            foreach (ILHook hook in ilHooks)
            {
                hook.Dispose();
            }
            ilHooks.Clear();

            IL.Celeste.SummitCheckpoint.Awake -= updateDashCountOnRespawnPointChange;
            IL.Celeste.SummitCheckpoint.Update -= updateDashCountOnRespawnPointChange;
            IL.Celeste.ChangeRespawnTrigger.OnEnter -= updateDashCountOnRespawnPointChange;
            IL.Celeste.Level.TeleportTo -= updateDashCountOnRespawnPointChange;

            On.Celeste.Player.Added -= onPlayerSpawn;
        }

        private void updateDashCountOnRespawnPointChange(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(instr => instr.MatchStfld<Session>("RespawnPoint")))
            {
                cursor.EmitDelegate<Action>(() =>
                {
                    Player player = Engine.Scene.Tracker.GetEntity<Player>();
                    if (player != null)
                    {
                        StoreAllCustomDashStates();
                    }
                });

                cursor.Index++;
            }
        }

        private void onPlayerSpawn(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            orig(self, scene);

            if (GetVariantValue<bool>(Variant.RestoreCustomDashStateOnRespawn) && ExtendedVariantsModule.Session.CustomDashStateOnLatestRespawn.Count > 0)
            {
                SetAllCustomDashStates();
            }
            else if (ExtendedVariantsModule.Session.CustomDashStateOnLatestRespawn.Count > 0)
            {
                StoreAllCustomDashStates();
            }
        }

        private void StoreAllCustomDashStates()
        {
            ExtendedVariantsModule.Session.CustomDashStateOnLatestRespawn.Clear();
            foreach (KeyValuePair<string, Func<bool>> nameAndGetter in CustomDashState.getCustomStateFunctions)
            {
                ExtendedVariantsModule.Session.CustomDashStateOnLatestRespawn[nameAndGetter.Key] = nameAndGetter.Value.Invoke();
            }
        }

        private void SetAllCustomDashStates()
        {
            foreach (KeyValuePair<string, Action<bool>> nameAndSetter in CustomDashState.setCustomStateFunctions)
            {
                nameAndSetter.Value(ExtendedVariantsModule.Session.CustomDashStateOnLatestRespawn[nameAndSetter.Key]);
            }
        }
    }
}
