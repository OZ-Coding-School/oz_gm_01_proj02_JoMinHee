using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonLog.Data;
using DungeonLog.Character;
using DungeonLog.StatusEffects;

namespace DungeonLog.Combat
{
    /// <summary>
    /// 데미지 계산 컨텍스트입니다. 공격자, 대상, 스킬 등의 정보를 담습니다.
    /// </summary>
    public class DamageContext
    {
        public DungeonLog.Character.Character Attacker { get; set; }
        public DungeonLog.Character.Character Target { get; set; }
        public SkillData Skill { get; set; }
        public int BaseDamage { get; set; }
        public bool IsCritical { get; set; }

        /// <summary>
        /// 계산 과정을 추적하기 위한 딕셔너리 (디버깅용)
        /// </summary>
        public Dictionary<string, int> CalculationSteps { get; set; } = new Dictionary<string, int>();

        public override string ToString()
        {
            return $"[{Attacker?.CharacterName} → {Target?.CharacterName}] Base: {BaseDamage}, Skill: {Skill?.DisplayName}";
        }
    }

    /// <summary>
    /// 독립적 데미지 계산 시스템입니다.
    /// 설계서 2-2, 2-3 구현. 8단계 독립적 계산 로직.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// 최종 데미지를 계산합니다.
        /// </summary>
        /// <param name="context">데미지 계산 컨텍스트</param>
        /// <returns>계산된 데미지 (최소 1)</returns>
        public static int CalculateFinalDamage(DamageContext context)
        {
            if (context == null)
            {
                Debug.LogError("[DamageCalculator] 컨텍스트가 null입니다.");
                return 0;
            }

            int damage = context.BaseDamage;
            context.CalculationSteps.Clear();
            context.CalculationSteps["Base"] = damage;

            // 1. 캐릭터 스탯 보정
            damage = ApplyCharacterStat(damage, context);
            context.CalculationSteps["AfterStat"] = damage;

            // 2. 스킬 계수
            damage = ApplySkillMultiplier(damage, context);
            context.CalculationSteps["AfterSkill"] = damage;

            // 3. 아이템 보정 (추후 구현)
            damage = ApplyItemModifier(damage, context);
            context.CalculationSteps["AfterItem"] = damage;

            // 4. 유물 보정 (추후 구현)
            damage = ApplyRelicModifier(damage, context);
            context.CalculationSteps["AfterRelic"] = damage;

            // 5. 버프/디버프
            damage = ApplyStatusEffect(damage, context);
            context.CalculationSteps["AfterStatus"] = damage;

            // 6. 치명타 보정 (방어력 이전에 적용)
            damage = ApplyCritical(damage, context);
            context.CalculationSteps["AfterCritical"] = damage;

            // 7. 방어력 감산 (치명타 후에 적용)
            damage = ApplyDefense(damage, context);
            context.CalculationSteps["AfterDefense"] = damage;

            // 최소 데미지 1 보장
            damage = Mathf.Max(1, damage);
            context.CalculationSteps["Final"] = damage;

            return damage;
        }

        /// <summary>
        /// 캐릭터 스탯을 반영하여 데미지를 보정합니다.
        /// 오버플로우 방지를 위해 checked 블록을 사용합니다.
        /// </summary>
        private static int ApplyCharacterStat(int damage, DamageContext context)
        {
            if (context.Attacker == null) return damage;

            int attack = context.Attacker.Stats.GetFinalStat(StatType.Attack);

            try
            {
                checked
                {
                    int result = damage + attack;
                    // 안전 범위 체크 (최대 100,000 데미지)
                    const int MAX_DAMAGE = 100000;
                    if (result > MAX_DAMAGE)
                    {
                        Debug.LogWarning($"[DamageCalculator] 데미지가 상한값을 초과했습니다: {result}. {MAX_DAMAGE}로 클램핑합니다.");
                        return MAX_DAMAGE;
                    }
                    return result;
                }
            }
            catch (OverflowException)
            {
                Debug.LogError($"[DamageCalculator] 데미지 계산 오버플로우! damage={damage}, attack={attack}");
                return 100000; // 안전한 기본값 반환
            }
        }

        /// <summary>
        /// 스킬 계수를 적용합니다.
        /// </summary>
        private static int ApplySkillMultiplier(int damage, DamageContext context)
        {
            if (context.Skill == null) return damage;

            float multiplier = context.Skill.DamageMultiplier;
            return Mathf.RoundToInt(damage * multiplier);
        }

        /// <summary>
        /// 아이템 보정을 적용합니다. (추후 구현)
        /// </summary>
        private static int ApplyItemModifier(int damage, DamageContext context)
        {
            // TODO: Phase 6에서 구현
            return damage;
        }

        /// <summary>
        /// 유물 보정을 적용합니다. (추후 구현)
        /// </summary>
        private static int ApplyRelicModifier(int damage, DamageContext context)
        {
            // TODO: Phase 10에서 구현
            return damage;
        }

        /// <summary>
        /// 상태 이상에 의한 데미지 보정을 적용합니다.
        /// 공격자의 버프와 대상의 디버프를 모두 고려합니다.
        /// </summary>
        private static int ApplyStatusEffect(int damage, DamageContext context)
        {
            // 1. 공격자의 버프 적용 (공격력 증가 등)
            if (context.Attacker != null)
            {
                var attackerStatus = context.Attacker.GetComponent<StatusEffectManager>();
                if (attackerStatus != null)
                {
                    damage = attackerStatus.ModifyOutgoingDamage(damage, context);
                }
            }

            // 2. 대상의 디버프 적용 (방어력 감소, 받는 데미지 증가 등)
            if (context.Target != null)
            {
                var targetStatus = context.Target.GetComponent<StatusEffectManager>();
                if (targetStatus != null)
                {
                    damage = targetStatus.ModifyIncomingDamage(damage, context);
                }
            }

            return damage;
        }

        /// <summary>
        /// 치명타 보정을 적용합니다.
        /// </summary>
        private static int ApplyCritical(int damage, DamageContext context)
        {
            if (!context.IsCritical) return damage;

            // 치명타 배율 (기본 2배, 추후 스탯으로 수정 가능)
            const float CRITICAL_MULTIPLIER = 2.0f;
            return Mathf.RoundToInt(damage * CRITICAL_MULTIPLIER);
        }

        /// <summary>
        /// 방어력을 감산합니다.
        /// </summary>
        private static int ApplyDefense(int damage, DamageContext context)
        {
            if (context.Target == null) return damage;

            int defense = context.Target.Stats.GetFinalStat(StatType.Defense);

            // 방어력 감산 (단순 감산, 추후 복잡한 공식으로 변경 가능)
            int reducedDamage = Mathf.Max(1, damage - defense);
            return reducedDamage;
        }

        /// <summary>
        /// 치명타 여부를 판정합니다.
        /// </summary>
        public static bool RollForCritical(DungeonLog.Character.Character attacker)
        {
            if (attacker == null) return false;

            float critChance = attacker.Stats.GetFinalStat(StatType.CriticalChance);
            float roll = UnityEngine.Random.value;
            return roll < critChance;
        }

        /// <summary>
        /// DamageInfo DTO를 생성하여 반환합니다.
        /// Phase 5: UI 표시, 로그 기록용
        /// </summary>
        public static DamageInfo CalculateDamageInfo(DamageContext context)
        {
            if (context == null)
            {
                Debug.LogError("[DamageCalculator] 컨텍스트가 null입니다.");
                return new DamageInfo { FinalDamage = 0, IsCritical = false };
            }

            // 기존 계산 로직 재사용
            int finalDamage = CalculateFinalDamage(context);

            // DamageInfo 생성
            var damageInfo = new DamageInfo
            {
                FinalDamage = finalDamage,
                IsCritical = context.IsCritical,
                IsBlocked = false,
                Attacker = context.Attacker,
                Target = context.Target,
                SourceSkill = context.Skill,
                CalculationSteps = new Dictionary<string, int>(context.CalculationSteps)
            };

            // 계산 추적 정보 설정
            if (context.CalculationSteps.TryGetValue("Base", out int baseDmg))
                damageInfo.BaseDamage = baseDmg;

            if (context.CalculationSteps.TryGetValue("AfterStat", out int afterStat))
                damageInfo.StatBonus = afterStat - baseDmg;

            if (context.CalculationSteps.TryGetValue("AfterSkill", out int afterSkill))
                damageInfo.SkillMultiplier = afterSkill;

            // 방어력 감소량: 방어력 적용 전후 차이 계산
            if (context.CalculationSteps.TryGetValue("AfterCritical", out int beforeDefense))
            {
                if (context.CalculationSteps.TryGetValue("AfterDefense", out int afterDefense))
                    damageInfo.DefenseReduction = beforeDefense - afterDefense;
            }

            return damageInfo;
        }
    }
}
