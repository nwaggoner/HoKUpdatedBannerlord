namespace HealthOnKillUpdated {
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
