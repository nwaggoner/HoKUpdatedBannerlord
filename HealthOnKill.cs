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

namespace HealthOnKill
{
  public class HealOnKillMissionBehavior : MissionLogic
  {
    private readonly List<CharacterObject> _characterCache;
    private readonly List<MBGUID> _nullCharacterCache;

    public HealOnKillMissionBehavior()
    {
      _characterCache = new List<CharacterObject>();
      _nullCharacterCache = new List<MBGUID>();
    }

    public override void OnAgentRemoved(
      Agent affectedAgent,
      Agent affectorAgent,
      AgentState agentState,
      KillingBlow killingBlow)
    {
      if (affectedAgent == null || affectorAgent == null || affectorAgent.Team == null || affectedAgent.IsMount || affectedAgent.State != AgentState.Killed && affectedAgent.State != AgentState.Unconscious || !affectedAgent.IsEnemyOf(affectorAgent))
      {
        return;
      }

      if (!HoKSettings.Instance.allowRangedHealing && killingBlow.IsMissile)
      {
        return;
      }

      float healAmount = 0.0f;
      bool isAlly = affectorAgent.Team.IsPlayerTeam || affectorAgent.Team.IsPlayerAlly;
      bool isPlayer = affectorAgent.IsMainAgent || affectorAgent.IsPlayerControlled;
      bool isHero = (affectorAgent.Character != null && affectorAgent.Character.IsHero);
      bool logging = false;
      if (HoKSettings.Instance.playerHealing > 0 & isPlayer)
      {
        healAmount = (float)HoKSettings.Instance.playerHealing;
        logging = HoKSettings.Instance.logPlayerHealingToChat;
      }
      else if (((HoKSettings.Instance.friendlyAIHeroHealing <= 0 ? 0 : (isHero ? 1 : 0)) & (isAlly ? 1 : 0)) != 0 && !isPlayer)
      {
        healAmount = (float)HoKSettings.Instance.friendlyAIHeroHealing;
        logging = HoKSettings.Instance.logHeroHealingToChat;
      }
      else if (HoKSettings.Instance.enemyAIHeroHealing > 0 && isHero && !isAlly && !isPlayer)
      {
        healAmount = (float)HoKSettings.Instance.enemyAIHeroHealing;
        logging = HoKSettings.Instance.logHeroHealingToChat;
      }
      else if (((HoKSettings.Instance.friendlyAITroopHealing <= 0 ? 0 : (!isHero ? 1 : 0)) & (isAlly ? 1 : 0)) != 0)
      {
        healAmount = (float)HoKSettings.Instance.friendlyAITroopHealing;
        logging = HoKSettings.Instance.logTroopHealingToChat;
      }
      else if (HoKSettings.Instance.enemyAITroopHealing > 0 && !isHero && !isAlly)
      {
        healAmount = (float)HoKSettings.Instance.enemyAITroopHealing;
        logging = HoKSettings.Instance.logTroopHealingToChat;
      }

      if ((double)healAmount <= 0.0)
      {
        return;
      }

      int actualHealing = HealAgent(affectorAgent, healAmount);

      if (HoKSettings.Instance.healHorsesToo && affectorAgent.MountAgent != null)
      {
        HealAgent(affectorAgent.MountAgent, healAmount);
      }

      DoMedicineSkillup(affectorAgent, healAmount);

      if (logging && actualHealing > 0)
      {
        TextObject text = new TextObject("{=HOK5z9gzZlpT}[HoK] {ATTACKER} was healed {AMOUNT} HP from killing {VICTIM}.");
        text.SetTextVariable("ATTACKER", affectorAgent.Name);
        text.SetTextVariable("AMOUNT", actualHealing.ToString());
        text.SetTextVariable("VICTIM", affectedAgent.Name.ToString());
        InformationManager.DisplayMessage(new InformationMessage(text.ToString(), isAlly ? Color.FromUint(4282569842U) : Colors.Red));
      }
    }

    public override void OnRegisterBlow(
      Agent attacker,
      Agent victim,
      GameEntity realHitEntity,
      Blow b,
      ref AttackCollisionData collisionData,
      in MissionWeapon attackerWeapon)
    {
      if (attacker == null || victim == null || attacker.Team == null || collisionData.AttackBlockedWithShield || b.SelfInflictedDamage > 0 || !attacker.IsEnemyOf(victim) || victim.IsMount || b.InflictedDamage <= 0)
      {
        return;
      }

      if (!HoKSettings.Instance.allowRangedHealing && b.IsMissile)
      {
        return;
      }

      float healAmount = 0.0f;
      bool isAlly = attacker.Team.IsPlayerTeam || attacker.Team.IsPlayerAlly;
      bool isPlayer = attacker.IsMainAgent || attacker.IsPlayerControlled;
      bool logging = false;
      bool isHero = (attacker.Character != null && attacker.Character.IsHero);
      if ((double)HoKSettings.Instance.playerLifeLeechPercent > 0.0 & isPlayer)
      {
        healAmount = (float)b.InflictedDamage * HoKSettings.Instance.playerLifeLeechPercent;
        logging = HoKSettings.Instance.logPlayerHealingToChat;
      }
      else if ((((double)HoKSettings.Instance.friendlyAIHeroLifeLeechPercent <= 0.0 ? 0 : (isHero ? 1 : 0)) & (isAlly ? 1 : 0)) != 0 && !isPlayer)
      {
        healAmount = (float)b.InflictedDamage * HoKSettings.Instance.friendlyAIHeroLifeLeechPercent;
        logging = HoKSettings.Instance.logHeroHealingToChat;
      }
      else if ((double)HoKSettings.Instance.enemyAIHeroLifeLeechPercent > 0.0 && isHero && !isAlly && !isPlayer)
      {
        healAmount = (float)b.InflictedDamage * HoKSettings.Instance.enemyAIHeroLifeLeechPercent;
        logging = HoKSettings.Instance.logHeroHealingToChat;
      }
      else if ((((double)HoKSettings.Instance.friendlyAITroopLifeLeechPercent <= 0.0 ? 0 : (!isHero ? 1 : 0)) & (isAlly ? 1 : 0)) != 0)
      {
        healAmount = (float)b.InflictedDamage * HoKSettings.Instance.friendlyAITroopLifeLeechPercent;
        logging = HoKSettings.Instance.logTroopHealingToChat;
      }
      else if ((double)HoKSettings.Instance.enemyAITroopLifeLeechPercent > 0.0 && !isHero && !isAlly)
      {
        healAmount = (float)b.InflictedDamage * HoKSettings.Instance.enemyAITroopLifeLeechPercent;
        logging = HoKSettings.Instance.logTroopHealingToChat;
      }

      if ((double)healAmount <= 0.0)
      {
        return;
      }

      int actualHealing = HealAgent(attacker, healAmount);

      if (HoKSettings.Instance.healHorsesToo && attacker.MountAgent != null)
      {
        HealAgent(attacker.MountAgent, healAmount);
      }

      DoMedicineSkillup(attacker, healAmount);

      if (logging && actualHealing > 0)
      {
        TextObject text = new TextObject("{=HOKMa0v4HCAT}[HoK] {ATTACKER} was healed {AMOUNT} HP from attacking {VICTIM}.");
        text.SetTextVariable("ATTACKER", attacker.Name);
        text.SetTextVariable("AMOUNT", actualHealing.ToString());
        text.SetTextVariable("VICTIM", victim.Name);
        InformationManager.DisplayMessage(new InformationMessage(text.ToString(), isAlly ? Color.FromUint(4282569842U) : Colors.Red));
      }
    }

    private int HealAgent(Agent a, float amount)
    {
      amount = (int)(amount < 1f ? 1f : amount);
      float health = a.Health;
      float maxHitPoints = a.HealthLimit;
      a.Health = a.Health + amount < maxHitPoints ? a.Health + amount : maxHitPoints;
      return (int)((double)a.Health - (double)health);
    }

    private void DoMedicineSkillup(Agent a, float amount)
    {
      if (HoKSettings.Instance.enableMedicineSkillGain && a.Character != null)
      {
        if (a.IsHero)
        {
          CharacterObject character = LookupCharacter(a.Character.Id);
          if (character != null)
          {
            character.HeroObject.AddSkillXp(DefaultSkills.Medicine, amount);
          }
        }
        else
        {
          Agent general = a.Team?.GeneralAgent;
          if (general?.Character != null)
          {
            CharacterObject character = LookupCharacter(general.Character.Id);
            if (character != null)
            {
              float manCountReduction = a.Team.ActiveAgents.Count * 0.2f;
              character.HeroObject.AddSkillXp(DefaultSkills.Medicine, amount / manCountReduction);
            }
          }
        }
      }
    }

    private CharacterObject LookupCharacter(MBGUID id)
    {
      if (_nullCharacterCache.Contains(id))
      {
        return null;
      }

      CharacterObject character = _characterCache.FirstOrDefault<CharacterObject>((Func<CharacterObject, bool>)(x => x.Id.Equals(id)));

      if (character == null && Campaign.Current?.Characters != null)
      {
        character = Campaign.Current.Characters.FirstOrDefault<CharacterObject>((Func<CharacterObject, bool>)(x => x.Id.Equals(id)));
        _characterCache.Add(character);

        if (character == null)
        {
          _nullCharacterCache.Add(id);
        }
      }

      return character;
    }
  }
}
