using HarmonyLib;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Global;
using TaleWorlds.MountAndBlade;

namespace HealOnKillUpdated {
    public class SubModule : MBSubModuleBase {
        
        public override void OnMissionBehaviorInitialize(Mission mission) {
            if (mission == null) {
                return;
            }
            base.OnMissionBehaviorInitialize(mission);

            mission.AddMissionBehavior(new HealOnKillMissionBehavior());
        }

        protected override void OnSubModuleLoad() {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("com.heal_on_kill");
            harmony.PatchAll();
        }

    }
}
