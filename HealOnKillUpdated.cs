using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;
using TaleWorlds.ObjectSystem;

namespace HealOnKillUpdated {

    public class HealOnKillMissionBehavior : MissionLogic {

        private readonly List<CharacterObject> _characterCache;
        private readonly List<MBGUID> _nullCharacterCache;
        private HoKUSettings hokInstance;

        public HealOnKillMissionBehavior() {
            _characterCache = new List<CharacterObject>();
            _nullCharacterCache = new List<MBGUID>();

            hokInstance = HoKUSettings.Instance;
        }

        // For healing performed after killing another agent (flat amount).
        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow) {
            /* ---- Base cases ---- */
            
            // Base objects
            if (affectedAgent == null || affectorAgent == null) {
                return;
            }
            // Object references (avoids crash if a condition is checked out of order (or something strange).
            if (affectorAgent.Team == null || affectedAgent.IsMount || (affectedAgent.State != AgentState.Killed && affectedAgent.State != AgentState.Unconscious) || !affectedAgent.IsEnemyOf(affectorAgent)) {
                return;
            }
            // Check whether ranged healing is allowed and return if it is not and the weapon used is a missle.
            if (!hokInstance.allowRangedHealing && killingBlow.IsMissile) {
                return;
            }


            bool isAlly = (affectorAgent.Team.IsPlayerTeam || affectorAgent.Team.IsPlayerAlly);
            bool isPlayer = (affectorAgent.IsMainAgent || affectorAgent.IsPlayerControlled);
            bool isHero = (affectorAgent.Character != null && affectorAgent.Character.IsHero);
            bool getsXP = (!hokInstance.medicineXPPlayerOnly || (hokInstance.medicineXPPlayerOnly && isPlayer));
            bool logging = false;

            
            float min_heal = (float)hokInstance.minHealing;
            float finalHealAmount;

            int healAmount = 0;
            int actualHealing;


            // Player heal
            if (hokInstance.playerHealing > 0 && isPlayer) {
                healAmount = hokInstance.playerHealing;
                logging = hokInstance.logPlayerHealingToChat;
            }
            // Ally Hero heal
            else if (hokInstance.friendlyAIHeroHealing > 0 && isAlly && !isPlayer && isHero) {
                healAmount = hokInstance.friendlyAIHeroHealing;
                logging = hokInstance.logHeroHealingToChat;
            }
            // Enemy Hero heal
            else if (hokInstance.enemyAIHeroHealing > 0 && !isAlly && !isPlayer && isHero) {
                healAmount = hokInstance.enemyAIHeroHealing;
                logging = hokInstance.logHeroHealingToChat;
            }
            // Ally Troop heal
            else if (hokInstance.friendlyAITroopHealing > 0 && !isHero && isAlly && !isPlayer) {
                healAmount = hokInstance.friendlyAITroopHealing;
                logging = hokInstance.logTroopHealingToChat;
            }
            // Enemy Troop heal
            else if (hokInstance.enemyAITroopHealing > 0 && !isHero && !isAlly && !isPlayer) {
                healAmount = hokInstance.enemyAITroopHealing;
                logging = hokInstance.logTroopHealingToChat;
            }


            if (healAmount > 0) {

                finalHealAmount = (float)healAmount;

                // Heal ranged attacks based on the percentage set in MCM
                if (hokInstance.allowRangedHealing && killingBlow.IsMissile) {
                    finalHealAmount = finalHealAmount * hokInstance.rangeHealAmount;
                    min_heal = min_heal * hokInstance.rangeHealAmount;

                }
                
                actualHealing = HealAgent(affectorAgent, finalHealAmount, min_heal);

                // Heal your horse or camel.
                if (hokInstance.healHorsesToo && affectorAgent.MountAgent != null) {
                    HealAgent(affectorAgent.MountAgent, finalHealAmount * hokInstance.mountHealAmount, min_heal * hokInstance.mountHealAmount);
                }

                if (hokInstance.enableMedicineSkillGain && getsXP) {
                    DoMedicineSkillup(affectorAgent, finalHealAmount * hokInstance.medicineXPAmount);
                }

                if (logging && actualHealing > 0) {
                    TextObject text = new TextObject("{=HOK5z9gzZlpT}[HoKU] {ATTACKER} was healed {AMOUNT} HP from killing {VICTIM}.");
                    text.SetTextVariable("ATTACKER", affectorAgent.Name);
                    text.SetTextVariable("AMOUNT", actualHealing.ToString());
                    text.SetTextVariable("VICTIM", affectedAgent.Name.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(text.ToString(), isAlly ? Color.FromUint(4282569842U) : Colors.Red));
                }
            }

            return;
        }



        // For healing performed per strike (percentage of damage dealt).
        public override void OnRegisterBlow(Agent attacker, Agent victim, WeakGameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon) {
            /* ---- Base cases ---- */
            
            // Base objects
            if (attacker == null || victim == null) {
                return;
            }
            // Object references (avoids crash if a condition is checked out of order (or something strange).
            if (attacker.Team == null || collisionData.AttackBlockedWithShield || b.SelfInflictedDamage > 0 || !attacker.IsEnemyOf(victim) || victim.IsMount || b.InflictedDamage <= 0) {
                return;
            }
            // Check whether ranged healing is allowed and return if it is not and the weapon used is a missle.
            if (!hokInstance.allowRangedHealing && b.IsMissile) {
                return;
            }
            

            bool isAlly = (attacker.Team.IsPlayerTeam || attacker.Team.IsPlayerAlly);
            bool isPlayer = (attacker.IsMainAgent || attacker.IsPlayerControlled);
            bool isHero = (attacker.Character != null && attacker.Character.IsHero);
            bool getsXP = (!hokInstance.medicineXPPlayerOnly || (hokInstance.medicineXPPlayerOnly && isPlayer));
            bool logging = false;

            float inflictedDamage = (float)b.InflictedDamage;
            float min_heal = (float)hokInstance.minHealing;
            float healAmount = 0f;
             
            int actualHealing;


            // Player lifesteal
            if (hokInstance.playerLifeLeechPercent > 0f && isPlayer) {
                healAmount = inflictedDamage * hokInstance.playerLifeLeechPercent;
                logging = hokInstance.logPlayerHealingToChat;
            }
            // Ally Hero lifesteal
            else if (hokInstance.friendlyAIHeroLifeLeechPercent > 0f && isAlly && !isPlayer && isHero) {
                healAmount = inflictedDamage * hokInstance.friendlyAIHeroLifeLeechPercent;
                logging = hokInstance.logHeroHealingToChat;
            }
            // Enemy Hero lifesteal  
            else if (hokInstance.enemyAIHeroLifeLeechPercent > 0f && !isAlly && !isPlayer && isHero) {
                healAmount = inflictedDamage * hokInstance.enemyAIHeroLifeLeechPercent;
                logging = hokInstance.logHeroHealingToChat;
            }
            // Ally Troop lifesteal
            else if (hokInstance.friendlyAITroopLifeLeechPercent > 0f && !isHero && isAlly && !isPlayer) {
                healAmount = inflictedDamage * hokInstance.friendlyAITroopLifeLeechPercent;
                logging = hokInstance.logTroopHealingToChat;
            }
            // Enemy Troop lifesteal
            else if (hokInstance.enemyAITroopLifeLeechPercent > 0f && !isHero && !isAlly && !isPlayer) {
                healAmount = inflictedDamage * hokInstance.enemyAITroopLifeLeechPercent;
                logging = hokInstance.logTroopHealingToChat;
            }


            if (healAmount > 0f) {

                // Heal ranged attacks based on the percentage set in MCM
                if (hokInstance.allowRangedHealing && b.IsMissile) {
                    healAmount = healAmount * hokInstance.rangeHealAmount;
                    min_heal = min_heal * hokInstance.rangeHealAmount;

                }
                
                actualHealing = HealAgent(attacker, healAmount, min_heal);

                // Heal your horse or camel.
                if (hokInstance.healHorsesToo && attacker.MountAgent != null) {
                    HealAgent(attacker.MountAgent, healAmount * hokInstance.mountHealAmount, min_heal * hokInstance.mountHealAmount);
                }


                if (hokInstance.enableMedicineSkillGain && getsXP) {
                    DoMedicineSkillup(attacker, healAmount * hokInstance.medicineXPAmount);
                }


                if (logging && actualHealing > 0) {
                    TextObject text = new TextObject("{=HOKMa0v4HCAT}[HoKU] {ATTACKER} was healed {AMOUNT} HP from attacking {VICTIM}.");
                    text.SetTextVariable("ATTACKER", attacker.Name);
                    text.SetTextVariable("AMOUNT", actualHealing.ToString());
                    text.SetTextVariable("VICTIM", victim.Name);
                    InformationManager.DisplayMessage(new InformationMessage(text.ToString(), isAlly ? Color.FromUint(4282569842U) : Colors.Red));
                }
            }

            return;
        }



        private int HealAgent(Agent a, float amount, float min_heal) {
            
            float temp = (amount < min_heal ? min_heal : amount);
            amount = (temp < 1f ? 1f : temp);
            
            float health = a.Health;
            float maxHitPoints = a.HealthLimit;
            a.Health = a.Health + amount < maxHitPoints ? a.Health + amount : maxHitPoints;
            
            return (int)(a.Health - health);
        }



        private void DoMedicineSkillup(Agent a, float amount) {

            if (a.Character != null) {
                if (a.IsHero) {
                    CharacterObject character = LookupCharacter(a.Character.Id);
                    if (character != null) {
                        character.HeroObject.AddSkillXp(DefaultSkills.Medicine, amount);
                    }
                }
                else {
                    Agent general = a.Team?.GeneralAgent;
                    if (general?.Character != null) {
                        CharacterObject character = LookupCharacter(general.Character.Id);
                        if (character != null) {
                            float manCountReduction = a.Team.ActiveAgents.Count * 0.2f;
                            character.HeroObject.AddSkillXp(DefaultSkills.Medicine, (amount / manCountReduction));
                        }
                    }
                }
            }
        }



        private CharacterObject LookupCharacter(MBGUID id) {
            if (_nullCharacterCache.Contains(id)) {
                return null;
            }

            CharacterObject character = _characterCache.FirstOrDefault<CharacterObject>((Func<CharacterObject, bool>)(x => x.Id.Equals(id)));

            if (character == null && Campaign.Current?.Characters != null) {
                character = Campaign.Current.Characters.FirstOrDefault<CharacterObject>((Func<CharacterObject, bool>)(x => x.Id.Equals(id)));
                _characterCache.Add(character);

                if (character == null) {
                    _nullCharacterCache.Add(id);
                }
            }

            return character;
        }
    }
}

