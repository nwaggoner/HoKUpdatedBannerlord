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
    }
}
