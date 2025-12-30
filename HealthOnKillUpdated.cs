using System;
using System.Collections.Generic;
using System.Linq;

namespace HealthOnKillUpdated {
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

            float healAmount = 0.0f;
            bool isAlly = (affectorAgent.Team.IsPlayerTeam || affectorAgent.Team.IsPlayerAlly);
            bool isPlayer = (affectorAgent.IsMainAgent || affectorAgent.IsPlayerControlled);
            bool isHero = (affectorAgent.Character != null && affectorAgent.Character.IsHero);
            bool logging = false;

            // Player heal
            if (HoKUSettings.Instance.playerHealing > 0 && isPlayer) {
                healAmount = (float)HoKUSettings.Instance.playerHealing;
                logging = HoKUSettings.Instance.logPlayerHealingToChat;
            }
            // Ally Hero heal
            else if (!isPlayer && isAlly && isHero && HoKUSettings.Instance.friendlyAIHeroHealing > 0) {
                healAmount = (float)HoKUSettings.Instance.friendlyAIHeroHealing;
                logging = HoKUSettings.Instance.logHeroHealingToChat;
            }
            // Enemy Hero heal
            else if (HoKUSettings.Instance.enemyAIHeroHealing > 0 && isHero && !isAlly && !isPlayer) {
                healAmount = (float)HoKUSettings.Instance.enemyAIHeroHealing;
                logging = HoKUSettings.Instance.logHeroHealingToChat;
            }
            // Ally Troop heal
            else if (isAlly && !isHero && HoKUSettings.Instance.friendlyAITroopHealing > 0) {
                healAmount = (float)HoKUSettings.Instance.friendlyAITroopHealing;
                logging = HoKUSettings.Instance.logTroopHealingToChat;
            }
            // Enemy Troop heal
            else if (HoKUSettings.Instance.enemyAITroopHealing > 0 && !isHero && !isAlly) {
                healAmount = (float)HoKUSettings.Instance.enemyAITroopHealing;
                logging = HoKUSettings.Instance.logTroopHealingToChat;
            }

            if ((double)healAmount > 0.0) {

                int actualHealing = HealAgent(affectorAgent, healAmount);

                // Heal your horse or camel.
                if (HoKUSettings.Instance.healHorsesToo && affectorAgent.MountAgent != null) {
                    HealAgent(affectorAgent.MountAgent, healAmount);
                }

                DoMedicineSkillup(affectorAgent, healAmount);

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

            float healAmount = 0.0f;
            bool isAlly = (attacker.Team.IsPlayerTeam || attacker.Team.IsPlayerAlly);
            bool isPlayer = (attacker.IsMainAgent || attacker.IsPlayerControlled);
            bool isHero = (attacker.Character != null && attacker.Character.IsHero);
            bool logging = false;


            // Player lifesteal
            if (isPlayer && (double)HoKUSettings.Instance.playerLifeLeechPercent > 0.0) {
                healAmount = (float)b.InflictedDamage * (float)HoKUSettings.Instance.playerLifeLeechPercent;
                logging = HoKUSettings.Instance.logPlayerHealingToChat;
            }
            // Ally Hero lifesteal
            else if (!isPlayer && isAlly && isHero && (double)HoKUSettings.Instance.friendlyAIHeroLifeLeechPercent > 0.0) {
                healAmount = (float)b.InflictedDamage * HoKUSettings.Instance.friendlyAIHeroLifeLeechPercent;
                logging = HoKUSettings.Instance.logHeroHealingToChat;
            }
            // Enemy Hero lifesteal  
            else if (!isPlayer && !isAlly && isHero && (double)HoKUSettings.Instance.enemyAIHeroLifeLeechPercent > 0.0) {
                healAmount = (float)b.InflictedDamage * HoKUSettings.Instance.enemyAIHeroLifeLeechPercent;
                logging = HoKUSettings.Instance.logHeroHealingToChat;
            }
            // Ally Troop lifesteal
            else if (!isPlayer && isAlly && !isHero && (double)HoKUSettings.Instance.friendlyAITroopLifeLeechPercent > 0.0) {
                healAmount = (float)b.InflictedDamage * HoKUSettings.Instance.friendlyAITroopLifeLeechPercent;
                logging = HoKUSettings.Instance.logTroopHealingToChat;
            }
            // Enemy Troop lifesteal
            else if (!isPlayer && !isAlly && !isHero && (double)HoKUSettings.Instance.enemyAITroopLifeLeechPercent > 0.0) {
                healAmount = (float)b.InflictedDamage * HoKUSettings.Instance.enemyAITroopLifeLeechPercent;
                logging = HoKUSettings.Instance.logTroopHealingToChat;
            }


            if ((double)healAmount > 0.0) {

                int actualHealing = HealAgent(attacker, healAmount);

                // Heal your horse or camel.
                if (HoKUSettings.Instance.healHorsesToo && attacker.MountAgent != null) {
                    HealAgent(attacker.MountAgent, healAmount);
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
            amount = (int)(amount < 1f ? 1f : amount);
            float health = a.Health;
            float maxHitPoints = a.HealthLimit;
            a.Health = a.Health + amount < maxHitPoints ? a.Health + amount : maxHitPoints;
            return (int)((double)a.Health - (double)health);
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
