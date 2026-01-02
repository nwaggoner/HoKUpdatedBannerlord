using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Engine;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace HealOnKillUpdated {
    public class HealOnKillMissionBehavior : MissionLogic {
        
        private readonly List<CharacterObject> _characterCache;
        private readonly List<MBGUID> _nullCharacterCache;

        public HealOnKillMissionBehavior() {
            _characterCache = new List<CharacterObject>();
            _nullCharacterCache = new List<MBGUID>();
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
            if (!HoKUSettings.Instance.allowRangedHealing && killingBlow.IsMissile) {
                return;
            }

            int  healAmount = 0;
            bool isAlly = (affectorAgent.Team.IsPlayerTeam || affectorAgent.Team.IsPlayerAlly);
            bool isPlayer = (affectorAgent.IsMainAgent || affectorAgent.IsPlayerControlled);
            bool isHero = (affectorAgent.Character != null && affectorAgent.Character.IsHero);
            bool logging = false;


            HoKUSettings hokInstance = HoKUSettings.Instance;

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
            else if (hokInstance.friendlyAITroopHealing > 0 && !isHero && isAlly) {
                healAmount = hokInstance.friendlyAITroopHealing;
                logging = hokInstance.logTroopHealingToChat;
            }
            // Enemy Troop heal
            else if (hokInstance.enemyAITroopHealing > 0 && !isHero && !isAlly) {
                healAmount = hokInstance.enemyAITroopHealing;
                logging = hokInstance.logTroopHealingToChat;
            }


            // Heal ranged attacks based on the percentage set in MCM
            if (hokInstance.allowRangedHealing && killingBlow.IsMissile) {
                healAmount = healAmount * (int)hokInstance.rangeHealAmount;
            }


            if (healAmount > 0) {

                int actualHealing = HealAgent(affectorAgent, healAmount);

                // Heal your horse or camel.
                if (hokInstance.healHorsesToo && affectorAgent.MountAgent != null) {
                    HealAgent(affectorAgent.MountAgent, (float)healAmount * hokInstance.mountHealAmount);
                }

                DoMedicineSkillup(affectorAgent, (float)healAmount);

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
            if (!HoKUSettings.Instance.allowRangedHealing && b.IsMissile) {
                return;
            }

            float healAmount = 0f;
            bool isAlly = (attacker.Team.IsPlayerTeam || attacker.Team.IsPlayerAlly);
            bool isPlayer = (attacker.IsMainAgent || attacker.IsPlayerControlled);
            bool isHero = (attacker.Character != null && attacker.Character.IsHero);
            bool logging = false;
            float inflictedDamage = (float)b.InflictedDamage;


            HoKUSettings hokInstance = HoKUSettings.Instance;
           
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
            else if (hokInstance.friendlyAITroopLifeLeechPercent > 0f && !isHero && isAlly) {
                healAmount = inflictedDamage * hokInstance.friendlyAITroopLifeLeechPercent;
                logging = hokInstance.logTroopHealingToChat;
            }
            // Enemy Troop lifesteal
            else if (hokInstance.enemyAITroopLifeLeechPercent > 0f && !isHero && !isAlly) {
                healAmount = inflictedDamage * hokInstance.enemyAITroopLifeLeechPercent;
                logging = hokInstance.logTroopHealingToChat;
            }


            // Heal ranged attacks based on the percentage set in MCM
            if (hokInstance.allowRangedHealing && b.IsMissile) {
                healAmount = healAmount * hokInstance.rangeHealAmount; 
            }


            if (healAmount > 0) {

                int actualHealing = HealAgent(attacker, healAmount);

                // Heal your horse or camel.
                if (hokInstance.healHorsesToo && attacker.MountAgent != null) {
                    HealAgent(attacker.MountAgent, healAmount * hokInstance.mountHealAmount);
                }

                DoMedicineSkillup(attacker, healAmount);

                if (logging && actualHealing > 0) {
                    TextObject text = new TextObject("{=HOKMa0v4HCAT}[HoKU] {ATTACKER} was healed {AMOUNT} HP from attacking {VICTIM}.");
                    text.SetTextVariable("ATTACKER", attacker.Name);
                    text.SetTextVariable("AMOUNT", actualHealing.ToString());
                    text.SetTextVariable("VICTIM", victim.Name);
                    InformationManager.DisplayMessage(new InformationMessage(text.ToString(), isAlly ? Color.FromUint(4282569842U) : Colors.Red));
                }
            }
        }



        private int HealAgent(Agent a, float amount) {
            amount = (amount < 1f ? 1f : amount);
            float health = a.Health;
            float maxHitPoints = a.HealthLimit;
            a.Health = a.Health + amount < maxHitPoints ? a.Health + amount : maxHitPoints;
            return (int)(a.Health - health);
        }


        private void DoMedicineSkillup(Agent a, float amount) {
            if (HoKUSettings.Instance.enableMedicineSkillGain && a.Character != null) {
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
                            character.HeroObject.AddSkillXp(DefaultSkills.Medicine, amount / manCountReduction);
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
