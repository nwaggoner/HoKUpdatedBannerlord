using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;


namespace HealOnKillUpdated {
    public class HoKUSettings : AttributeGlobalSettings<HoKUSettings> {
        
        private bool _useStandardOptionScreen = false;

        public override string Id => "HoKUpdated_v1";
        public override string DisplayName => "Heal on Kill Updated";
        public override string FolderName => "HoKUpdated";
        public override string FormatType => "json2";


        [SettingPropertyInteger("{=HOKQHudaXBC2}Player Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOK2u0NsZmvY}Amount to heal the player upon killing an enemy.", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("Heal on Kill", GroupOrder = 1)]
        public int playerHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOKcdYICCJ1c}Friendly AI Hero Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOKv3ISUPLCw}Amount to heal friendly AI heroes (companions/family/friendly lords) upon their killing an enemy.", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("Heal on Kill", GroupOrder = 1)]
        public int friendlyAIHeroHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOK9riNbUGxH}Enemy AI Hero Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOKbpOuFvNWY}Amount to heal enemy AI heroes (enemy lords) upon their killing an enemy.", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("Heal on Kill", GroupOrder = 1)]
        public int enemyAIHeroHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOKcSryc2957}Friendly AI Troop Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOKdkXBUtCXU}Amount to heal friendly troops upon their killing an enemy.", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Heal on Kill", GroupOrder = 1)]
        public int friendlyAITroopHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOKAMcW5G1nb}Enemy AI Troop Healing Amount", 0, 100, "{=HOKqvBf5KFso}0 Health", HintText = "{=HOKh1va9W1Qn}Amount to heal enemy troops upon their killing an enemy.", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("Heal on Kill", GroupOrder = 1)]
        public int enemyAITroopHealing { get; set; } = 0;

        [SettingPropertyInteger("{=HOKLHZ5h4wt0}Minimum Heal", 1, 100, HintText = "{=HOKZaBFyKiQZ}The minimum amount that someone can be healed per kill (default is 1).", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup("Heal on Kill", GroupOrder = 1)]
        public int minHealOnKill { get; set; } = 1;



        [SettingPropertyFloatingInteger("{=HOKqrGwnJy3M}Player Life Leech", 0f, 2f, "#0%", HintText = "{=HOKt1UZbPrEb}Heals the player by percentage of damage of done.", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("Health on Strike", GroupOrder = 2)]
        public float playerLifeLeechPercent { get; set; } = 0.15f;

        [SettingPropertyFloatingInteger("{=HOKzqH9tLiTH}Friendly AI Hero Life Leech", 0f, 2f, "#0%", HintText = "{=HOKWbg0KQXmU}Heals friendly AI heroes (companions/family/friendly lords) by percentage of damage done.", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("Health on Strike", GroupOrder = 2)]
        public float friendlyAIHeroLifeLeechPercent { get; set; } = 0.00f;

        [SettingPropertyFloatingInteger("{=HOKlkkghJF1z}Enemy AI Hero Life Leech", 0f, 2f, "#0%", HintText = "{=HOKjmEYyVf0s}Heals enemy AI heroes (enemy lords) by percentage of damage done.", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("Health on Strike", GroupOrder = 2)]
        public float enemyAIHeroLifeLeechPercent { get; set; } = 0.00f;

        [SettingPropertyFloatingInteger("{=HOKtxiCNy3zI}Friendly AI Troop Life Leech", 0f, 2f, "#0%", HintText = "{=HOKfqSNv6jN4}Heals friendly troops by percentage of damage done.", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Health on Strike", GroupOrder = 2)]
        public float friendlyAITroopLifeLeechPercent { get; set; } = 0.00f;

        [SettingPropertyFloatingInteger("{=HOKoBDhrhWb1}Enemy AI Troop Life Leech", 0f, 2f, "#0%", HintText = "{=HOKxDKdnJiEZ}Heals enemy troops by percentage of damage done.", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("Health on Strike", GroupOrder = 2)]
        public float enemyAITroopLifeLeechPercent { get; set; } = 0.00f;

        [SettingPropertyInteger("{=HOKLHZ5h4wt0}Minimum Heal", 1, 100, HintText = "{=HOKQqz8yHgZJ}The minimum amount that someone can be healed per strike (default is 1).", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup("Health on Strike", GroupOrder = 2)]
        public int minHealLifeLeech { get; set; } = 1;



        [SettingPropertyBool("{=HOKxaE9F7AUy}Allow Horse Healing", HintText = "{=HOKgABdvxkl0}For mounted troops (and you) all healing done is also applied to the mount.", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("Misc.", GroupOrder = 3)]
        public bool healHorsesToo { get; set; } = true;

        [SettingPropertyFloatingInteger("{=HOK1by8uIITs}Horse Healing Multiplier", 0f, 2f, "#0%", HintText = "{=HOKBtzD2leY7}Control how much healing your mount gets (default is 100%).", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("Misc.", GroupOrder = 3)]
        public float mountHealAmount { get; set; } = 1.00f;

        [SettingPropertyBool("{=HOKNjCLqQi5A}Allow Ranged Healing", HintText = "{=HOKWw2fllAec}Allow healing on ranged damage", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("Misc.", GroupOrder = 3)]
        public bool allowRangedHealing { get; set; } = true;

        [SettingPropertyFloatingInteger("{=HOK1by8uIITs}Ranged Healing Multiplier", 0f, 1f, "#0%", HintText = "{=HOKeohKUFxa3}Control how much healing ranged attacks get (default is 100%).", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Misc.", GroupOrder = 3)]
        public float rangeHealAmount { get; set; } = 1.00f;

        [SettingPropertyBool("{=HOKHvyPZeDlG}Enable Medicine Skill Gain", HintText = "{=HOKJnU6UxTaU}Give medicine skill for healing.", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("Misc.", GroupOrder = 3)]
        public bool enableMedicineSkillGain { get; set; } = false;

        [SettingPropertyBool("{=HOKUVNUH20Hc}Medicine Skill for Player Only", HintText = "{=HOKjp5qUqQ1N}Limits the ability to gain medicine skill to the player character only (instead of all NPC heroes).", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup("Misc.", GroupOrder = 3)]
        public bool medicineXPPlayerOnly { get; set; } = false;

        [SettingPropertyFloatingInteger("{=HOK9VBkMzG3f}Medicine Skill Multiplier", 1f, 10f, "#0%", HintText = "{=HOKQEuU23FDS}Control how much medicine skill you gain (default is 100%).", Order = 7, RequireRestart = false)]
        [SettingPropertyGroup("Misc.", GroupOrder = 3)]
        public float medicineXPAmount { get; set; } = 1.00f;



        [SettingPropertyBool("{=HOKMuUyjaika}Log Player Healing to Chat", HintText = "", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("Debug", GroupOrder = 4)]
        public bool logPlayerHealingToChat { get; set; } = true;

        [SettingPropertyBool("{=HOKfaxNnIOFf}Log Player XP Gain to Chat", HintText = "", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("Debug", GroupOrder = 4)]
        public bool logPlayerXPToChat { get; set; } = false;

        [SettingPropertyBool("{=HOKBtXbn11mL}Log AI Hero Healing to Chat", HintText = "", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("Debug", GroupOrder = 4)]
        public bool logHeroHealingToChat { get; set; } = false;

        [SettingPropertyBool("{=HOKVYdsuVDv3}Log AI Hero XP Gain to Chat", HintText = "", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Debug", GroupOrder = 4)]
        public bool logHeroXPToChat { get; set; } = false;

        [SettingPropertyBool("{=HOKxe6PdaTxL}Log Troop Healing to Chat", HintText = "{=HOKr4xGiDQeC}(Warning: this is just for debugging, don't enable this unless you like spam.)", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("Debug", GroupOrder = 4)]
        public bool logTroopHealingToChat { get; set; } = false;

       



        public override IEnumerable<ISettingsPreset> GetBuiltInPresets() {

            yield return (ISettingsPreset)new MemorySettingsPreset(((BaseSettings)this).Id, "default", new TextObject("{={HOKht6OXHM05}Default", (Dictionary<string, object>)null).ToString(), (Func<BaseSettings>)(() => (BaseSettings)new HoKUSettings()));
            
            yield return (ISettingsPreset)new MemorySettingsPreset(((BaseSettings)this).Id, "ol_reliable", new TextObject("{=HOKY5yyFyqPP}Ol' Reliable", (Dictionary<string, object>)null).ToString(), (Func<BaseSettings>)(() => (BaseSettings)new HoKUSettings() {
                playerHealing = 0,
                friendlyAIHeroHealing = 0, 
                enemyAIHeroHealing = 0,
                friendlyAITroopHealing = 0,
                enemyAITroopHealing = 0,
                playerLifeLeechPercent = 0.2f,
                friendlyAIHeroLifeLeechPercent = 0.3f,
                enemyAIHeroLifeLeechPercent = 0.3f,
                friendlyAITroopLifeLeechPercent = 0f,
                enemyAITroopLifeLeechPercent = 0f,
                minHealOnKill = 1,
                minHealLifeLeech = 1,
                healHorsesToo = true,
                mountHealAmount = 1f,
                allowRangedHealing = true,
                rangeHealAmount = 1f,
                enableMedicineSkillGain = true,
                medicineXPPlayerOnly = false,
                medicineXPAmount = 1f,
                logPlayerHealingToChat = false,
                logPlayerXPToChat = false,
                logHeroHealingToChat = false,
                logHeroXPToChat = false,
                logTroopHealingToChat = false
            }));

            yield return (ISettingsPreset)new MemorySettingsPreset(((BaseSettings)this).Id, "longer_battles", new TextObject("{=HOKSAwLhUcvV}Longer Battles", (Dictionary<string, object>)null).ToString(), (Func<BaseSettings>)(() => (BaseSettings)new HoKUSettings() {
                playerHealing = 0,
                friendlyAIHeroHealing = 0,
                enemyAIHeroHealing = 0,
                friendlyAITroopHealing = 0,
                enemyAITroopHealing = 0,
                playerLifeLeechPercent = 0.2f,
                friendlyAIHeroLifeLeechPercent = 0.4f,
                enemyAIHeroLifeLeechPercent = 0.4f,
                friendlyAITroopLifeLeechPercent = 0.20f,
                enemyAITroopLifeLeechPercent = 0.20f,
                minHealOnKill = 1,
                minHealLifeLeech = 12,
                healHorsesToo = true,
                mountHealAmount = 0.5f,
                allowRangedHealing = true,
                rangeHealAmount = 0.33f,
                enableMedicineSkillGain = true,
                medicineXPPlayerOnly = true,
                medicineXPAmount = 1f,
                logPlayerHealingToChat = false,
                logPlayerXPToChat = false,
                logHeroHealingToChat = false,
                logHeroXPToChat = false,    
                logTroopHealingToChat = false
            }));
        }


        public bool UseStandardOptionScreen {
            get => this._useStandardOptionScreen;
            set {
                if (this._useStandardOptionScreen == value)
                    return;
                this._useStandardOptionScreen = value;
                this.OnPropertyChanged(nameof(UseStandardOptionScreen));
            }
        }
    }
}
