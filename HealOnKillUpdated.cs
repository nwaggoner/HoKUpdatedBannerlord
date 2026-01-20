using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
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

            Agent leader = affectorAgent.Team?.Leader;

            bool isPlayer = (affectorAgent.IsPlayerControlled || affectorAgent.Character.IsPlayerCharacter);
            bool isAlly = (affectorAgent.Team.IsPlayerTeam || affectorAgent.Team.IsPlayerAlly);
            bool isHero = (affectorAgent.Character != null && affectorAgent.Character.IsHero) && !isPlayer;
            bool isTroop = (!isHero && !isPlayer);
            bool playerGetsXP = leader == null ? isPlayer : (isPlayer || (isTroop && isAlly && leader.IsPlayerControlled));

            bool logging = false;
            bool loggingXP = false;

            float min_heal = (float)hokInstance.minHealOnKill;
            float finalHealAmount,
                  xpHealAmount;

            int healAmount = 0;
                

            int actualHealing = 0,
                actualXP = 0;


            // Player heal
            if (hokInstance.playerHealing > 0 && isPlayer) {
                healAmount = hokInstance.playerHealing;
                logging = hokInstance.logPlayerHealingToChat;
                loggingXP = hokInstance.logPlayerXPToChat;
            }
            // Ally Hero heal
            else if (hokInstance.friendlyAIHeroHealing > 0 && isAlly && isHero) {
                healAmount = hokInstance.friendlyAIHeroHealing;
                logging = hokInstance.logHeroHealingToChat;
                loggingXP = hokInstance.medicineXPPlayerOnly ? false : hokInstance.logHeroXPToChat;
            }
            // Enemy Hero heal
            else if (hokInstance.enemyAIHeroHealing > 0 && !isAlly && isHero) {
                healAmount = hokInstance.enemyAIHeroHealing;
                logging = hokInstance.logHeroHealingToChat;
                loggingXP = hokInstance.medicineXPPlayerOnly ? false : hokInstance.logHeroXPToChat;
            }
            // Ally Troop heal
            else if (hokInstance.friendlyAITroopHealing > 0 && isTroop && isAlly) {
                healAmount = hokInstance.friendlyAITroopHealing;
                logging = hokInstance.logTroopHealingToChat;
                loggingXP = hokInstance.medicineXPPlayerOnly ? hokInstance.logPlayerXPToChat : (hokInstance.logPlayerXPToChat || hokInstance.logHeroXPToChat);
            }
            // Enemy Troop heal
            else if (hokInstance.enemyAITroopHealing > 0 && isTroop && !isAlly) {
                healAmount = hokInstance.enemyAITroopHealing;
                logging = hokInstance.logTroopHealingToChat;
                loggingXP = hokInstance.medicineXPPlayerOnly ? false : hokInstance.logHeroXPToChat;
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

                if (hokInstance.enableMedicineSkillGain && actualHealing > 0) {
                    if ((hokInstance.medicineXPPlayerOnly && playerGetsXP) || !hokInstance.medicineXPPlayerOnly) {
                        xpHealAmount = (finalHealAmount < min_heal) ? finalHealAmount : (float)actualHealing;
                        actualXP = DoMedicineSkillup(affectorAgent, xpHealAmount * hokInstance.medicineXPAmount);
                    }
                }

                // Logging
                if (actualHealing > 0 && logging) {

                    TextObject textHeal = new TextObject("{=HOK5z9gzZlpT}[HoKU] {ATTACKER} was healed {AMOUNT} HP from killing {VICTIM}.");
                    textHeal.SetTextVariable("ATTACKER", affectorAgent.Name);
                    textHeal.SetTextVariable("AMOUNT", actualHealing.ToString());
                    textHeal.SetTextVariable("VICTIM", affectedAgent.Name.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(textHeal.ToString(), isAlly ? Colors.Green : Colors.Red));
                }

                if (actualXP > 0 && loggingXP) {

                    TextObject textXP = new TextObject("{=HOKplCWnkotD}[HoKU] {ATTACKER} gained {AMOUNT} Medical XP.");
                    if (leader != null)
                        textXP.SetTextVariable("ATTACKER", isHero ? affectorAgent.Name : leader.Name);
                    else
                        textXP.SetTextVariable("ATTACKER", affectorAgent.Name);
                    textXP.SetTextVariable("AMOUNT", actualXP.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(textXP.ToString(), isAlly ? Color.FromUint(4282569842U) : Colors.Magenta));
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

            Agent leader = attacker.Team?.Leader;

            bool isPlayer = (attacker.IsPlayerControlled || attacker.Character.IsPlayerCharacter);
            bool isAlly = (attacker.Team.IsPlayerTeam || attacker.Team.IsPlayerAlly);
            bool isHero = (attacker.Character != null && attacker.Character.IsHero) && !isPlayer;
            bool isTroop = (!isHero && !isPlayer);
            bool playerGetsXP = leader == null ? isPlayer : isPlayer || (isTroop && isAlly && leader.IsPlayerControlled);

            bool logging = false;
            bool loggingXP = false;

            float inflictedDamage = (float)b.InflictedDamage;
            float min_heal = (float)hokInstance.minHealLifeLeech;
            float healAmount = 0f,
                  xpHealAmount;
             
            int actualHealing = 0,
                actualXP = 0;


            // Player lifesteal
            if (hokInstance.playerLifeLeechPercent > 0f && isPlayer) {
                healAmount = inflictedDamage * hokInstance.playerLifeLeechPercent;
                logging = hokInstance.logPlayerHealingToChat;
                loggingXP = hokInstance.logPlayerXPToChat;
            }
            // Ally Hero lifesteal
            else if (hokInstance.friendlyAIHeroLifeLeechPercent > 0f && isHero && isAlly) {
                healAmount = inflictedDamage * hokInstance.friendlyAIHeroLifeLeechPercent;
                logging = hokInstance.logHeroHealingToChat;
                loggingXP = hokInstance.medicineXPPlayerOnly ? false : hokInstance.logHeroXPToChat;
            }
            // Enemy Hero lifesteal  
            else if (hokInstance.enemyAIHeroLifeLeechPercent > 0f && isHero && !isAlly) {
                healAmount = inflictedDamage * hokInstance.enemyAIHeroLifeLeechPercent;
                logging = hokInstance.logHeroHealingToChat;
                loggingXP = hokInstance.medicineXPPlayerOnly ? false : hokInstance.logHeroXPToChat;
            }
            // Ally Troop lifesteal
            else if (hokInstance.friendlyAITroopLifeLeechPercent > 0f && isTroop && isAlly) {
                healAmount = inflictedDamage * hokInstance.friendlyAITroopLifeLeechPercent;
                logging = hokInstance.logTroopHealingToChat;
                loggingXP = hokInstance.medicineXPPlayerOnly ? hokInstance.logPlayerXPToChat : (hokInstance.logPlayerXPToChat || hokInstance.logHeroXPToChat);
            }
            // Enemy Troop lifesteal
            else if (hokInstance.enemyAITroopLifeLeechPercent > 0f && isTroop && !isAlly) {
                healAmount = inflictedDamage * hokInstance.enemyAITroopLifeLeechPercent;
                logging = hokInstance.logTroopHealingToChat;
                loggingXP = hokInstance.medicineXPPlayerOnly ? false : hokInstance.logHeroXPToChat;
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


                if (hokInstance.enableMedicineSkillGain && actualHealing > 0) {
                    if ((hokInstance.medicineXPPlayerOnly && playerGetsXP) || !hokInstance.medicineXPPlayerOnly) {
                        xpHealAmount = (healAmount < min_heal) ? healAmount : (float)actualHealing;
                        actualXP = DoMedicineSkillup(attacker, xpHealAmount * hokInstance.medicineXPAmount);
                    }
                }

                // Logging
                if (actualHealing > 0 && logging) {

                    TextObject textHeal = new TextObject("{=HOKMa0v4HCAT}[HoKU] {ATTACKER} was healed {AMOUNT} HP from attacking {VICTIM}.");
                    textHeal.SetTextVariable("ATTACKER", attacker.Name);
                    textHeal.SetTextVariable("AMOUNT", actualHealing.ToString());
                    textHeal.SetTextVariable("VICTIM", victim.Name);
                    InformationManager.DisplayMessage(new InformationMessage(textHeal.ToString(), isAlly ? Colors.Green : Colors.Red));
                }

                if (actualXP > 0 && loggingXP) {
                    
                    TextObject textXP = new TextObject("{=HOKplCWnkotD}[HoKU] {ATTACKER} gained {AMOUNT} Medical XP.");
                    if (leader != null)
                        textXP.SetTextVariable("ATTACKER", isHero ? attacker.Name : leader.Name);
                    else
                        textXP.SetTextVariable("ATTACKER", attacker.Name);
                    textXP.SetTextVariable("AMOUNT", actualXP.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(textXP.ToString(), isAlly ? Color.FromUint(4282569842U) : Colors.Magenta));
                }
            }

            return;
        }



        private int HealAgent(Agent a, float amount, float min_heal) {

            float temp = Math.Max(amount, min_heal);
            amount = Math.Max(temp, 1f);
            
            float health = a.Health;
            float maxHitPoints = a.HealthLimit;
            a.Health = a.Health + amount < maxHitPoints ? a.Health + amount : maxHitPoints;
            
            return (int)(a.Health - health);
        }



        private int DoMedicineSkillup(Agent a, float amount) {

            float finalAmount = Math.Max(amount, 1f);
            
            Agent leader = a.Team?.Leader;

            CharacterObject character,
                            leaderCharacter;


            if (a.Character?.IsHero == true) {
           
                character = LookupCharacter(a.Character.Id);

                if (character != null && character.HeroObject != null) {
                    character.HeroObject.AddSkillXp(DefaultSkills.Medicine, finalAmount);
                }

                return (int)finalAmount;
            }    
            else if (leader?.Character != null) 
            {

                leaderCharacter = LookupCharacter(leader.Character.Id);

                if (leaderCharacter != null && leaderCharacter.HeroObject != null) {
              
                    int activeAgents = Math.Max(a.Team.ActiveAgents.Count, 1);
                    float manCountReduction = Math.Max((float)activeAgents * 0.2f, 1f);

                    finalAmount = Math.Max(finalAmount / manCountReduction, 1f);

                    leaderCharacter.HeroObject.AddSkillXp(DefaultSkills.Medicine, finalAmount);

                    return (int)finalAmount;
                }
            }
           
            return 0;
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

