using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonLog.Data;
using DungeonLog.Character;
using DungeonLog.StatusEffects;

namespace DungeonLog.Combat
{
    /// <summary>
    /// 스킬 실행 결과를 담는 DTO 클래스입니다.
    /// </summary>
    public class SkillExecutionResult
    {
        /// <summary>실행 성공 여부</summary>
        public bool IsSuccess { get; set; }

        /// <summary>사용한 캐릭터</summary>
        public DungeonLog.Character.Character User { get; set; }

        /// <summary>사용한 스킬</summary>
        public SkillData Skill { get; set; }

        /// <summary>대상 캐릭터 리스트</summary>
        public List<DungeonLog.Character.Character> Targets { get; set; } = new List<DungeonLog.Character.Character>();

        /// <summary>데미지 딕셔너리 (대상 → 데미지)</summary>
        public Dictionary<DungeonLog.Character.Character, int> Damages { get; set; } = new Dictionary<DungeonLog.Character.Character, int>();

        /// <summary>치명타 여부 딕셔너리 (대상 → 치명타)</summary>
        public Dictionary<DungeonLog.Character.Character, bool> Criticals { get; set; } = new Dictionary<DungeonLog.Character.Character, bool>();

        /// <summary>에러 메시지</summary>
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            if (!IsSuccess)
            {
                return $"[SkillExecutor] 스킬 실행 실패: {ErrorMessage}";
            }

            string targetInfo = string.Join(", ", Targets.ConvertAll(t => t.CharacterName));
            return $"[SkillExecutor] {User?.CharacterName} → {targetInfo} ({Skill?.DisplayName})";
        }
    }

    /// <summary>
    /// 스킬 실행을 조율하는 클래스입니다.
    /// 데미지 계산, 타겟팅, 이펙트 적용 등 스킬 사용의 전체 흐름을 관리합니다.
    /// </summary>
    public class SkillExecutor
    {
        // ========================================================================
        // 의존성 참조
        // ========================================================================

        private BattleManager battleManager;

        // ========================================================================
        // 생성자
        // ========================================================================

        public SkillExecutor(BattleManager manager)
        {
            battleManager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        // ========================================================================
        // 스킬 실행 메인 메서드
        // ========================================================================

        /// <summary>
        /// 스킬을 실행합니다.
        /// </summary>
        /// <param name="user">스킬을 사용하는 캐릭터</param>
        /// <param name="skill">사용할 스킬</param>
        /// <param name="targets">대상 캐릭터 리스트</param>
        /// <returns>실행 결과</returns>
        public SkillExecutionResult ExecuteSkill(DungeonLog.Character.Character user, SkillData skill, List<DungeonLog.Character.Character> targets)
        {
            var result = new SkillExecutionResult
            {
                User = user,
                Skill = skill,
                Targets = targets
            };

            // 전제 조건 검증
            if (!ValidatePreconditions(user, skill, targets, result))
            {
                return result;
            }

            // AP 소비
            if (!ConsumeAP(skill, result))
            {
                return result;
            }

            // 타겟 유효성 검증
            targets = GetValidTargets(user, skill, targets);
            result.Targets = targets;

            if (targets.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "유효한 대상이 없습니다.";
                return result;
            }

            // 스킬 타입별 실행
            ExecuteSkillByType(user, skill, targets, result);

            // 전투 종료 조건 확인
            CheckBattleEndAfterSkill(result);

            return result;
        }

        // ========================================================================
        // 전제 조건 검증
        // ========================================================================

        /// <summary>
        /// 스킬 사용 전제 조건을 검증합니다.
        /// </summary>
        private bool ValidatePreconditions(DungeonLog.Character.Character user, SkillData skill, List<DungeonLog.Character.Character> targets, SkillExecutionResult result)
        {
            if (user == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "사용자가 null입니다.";
                return false;
            }

            if (skill == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "스킬이 null입니다.";
                return false;
            }

            if (user.IsDead)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"{user.CharacterName}은(는) 이미 사망했습니다.";
                return false;
            }

            if (targets == null || targets.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "대상이 지정되지 않았습니다.";
                return false;
            }

            result.IsSuccess = true;
            return true;
        }

        // ========================================================================
        // AP 소비
        // ========================================================================

        /// <summary>
        /// 스킬 AP를 소비합니다.
        /// Null 안전성을 강화했습니다.
        /// </summary>
        private bool ConsumeAP(SkillData skill, SkillExecutionResult result)
        {
            // Null 체크
            if (battleManager == null || battleManager.APSystem == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "전투 시스템이 초기화되지 않았습니다.";
                Debug.LogError("[SkillExecutor] BattleManager 또는 APSystem이 null입니다.");
                return false;
            }

            int apCost = skill.APCost;

            if (!battleManager.APSystem.HasEnoughAP(apCost))
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"AP 부족: 필요 {apCost}, 현재 {battleManager.APSystem.CurrentAP}";
                return false;
            }

            if (!battleManager.APSystem.ConsumeAP(apCost))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "AP 소비 실패";
                return false;
            }

            Debug.Log($"[SkillExecutor] AP {apCost} 소비 완료 (잔여: {battleManager.APSystem.CurrentAP})");
            return true;
        }

        // ========================================================================
        // 타겟팅
        // ========================================================================

        /// <summary>
        /// 유효한 대상만 필터링합니다.
        /// </summary>
        private List<DungeonLog.Character.Character> GetValidTargets(DungeonLog.Character.Character user, SkillData skill, List<DungeonLog.Character.Character> rawTargets)
        {
            var validTargets = new List<DungeonLog.Character.Character>();

            foreach (var target in rawTargets)
            {
                if (target == null) continue;
                if (target.IsDead) continue;

                // TODO: 추가 타겟팅 조건 (사거리, 상태 등)

                validTargets.Add(target);
            }

            return validTargets;
        }

        // ========================================================================
        // 스킬 타입별 실행
        // ========================================================================

        /// <summary>
        /// 스킬 타입에 따라 분기하여 실행합니다.
        /// Null 안전성을 강화했습니다.
        /// </summary>
        private SkillExecutionResult ExecuteSkillByType(DungeonLog.Character.Character user, SkillData skill, List<DungeonLog.Character.Character> targets, SkillExecutionResult result)
        {
            // Null 체크
            if (battleManager == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "BattleManager가 null입니다.";
                Debug.LogError("[SkillExecutor] BattleManager가 null입니다.");
                return result;
            }

            // 이전 상태 저장
            BattleState previousState = battleManager.CurrentState;

            // 스킬 해석 상태로 전환
            BattleEvents.NotifyBattleStateChanged(BattleState.Resolving);

            try
            {
                switch (skill.SkillType)
                {
                    case SkillType.Attack:
                        ExecuteAttackSkill(user, skill, targets, result);
                        break;

                    case SkillType.Buff:
                        ExecuteBuffSkill(user, skill, targets, result);
                        break;

                    case SkillType.Debuff:
                        ExecuteDebuffSkill(user, skill, targets, result);
                        break;

                    case SkillType.Heal:
                        ExecuteHealSkill(user, skill, targets, result);
                        break;

                    default:
                        result.IsSuccess = false;
                        result.ErrorMessage = $"알 수 없는 스킬 타입: {skill.SkillType}";
                        Debug.LogError($"[SkillExecutor] {result.ErrorMessage}");
                        return result; // 조기 반환
                }

                result.IsSuccess = true;
            }
            catch (System.Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"스킬 실행 중 예외 발생: {ex.Message}";
                Debug.LogError($"[SkillExecutor] {result.ErrorMessage}\n{ex.StackTrace}");
            }
            finally
            {
                // 이전 상태로 복귀 (전투가 진행 중인 경우에만)
                if (battleManager != null && battleManager.IsBattleActive)
                {
                    BattleEvents.NotifyBattleStateChanged(previousState);
                }
            }

            return result;
        }

        // ========================================================================
        // 공격 스킬 실행
        // ========================================================================

        /// <summary>
        /// 공격 스킬을 실행합니다.
        /// Phase 6: 상태 이상 부여 연동
        /// </summary>
        private void ExecuteAttackSkill(DungeonLog.Character.Character user, SkillData skill, List<DungeonLog.Character.Character> targets, SkillExecutionResult result)
        {
            Debug.Log($"[SkillExecutor] 공격 스킬 실행: {skill.DisplayName} → {targets.Count}명");

            foreach (var target in targets)
            {
                // 치명타 판정
                bool isCritical = DamageCalculator.RollForCritical(user);

                // 데미지 컨텍스트 생성
                var context = new DamageContext
                {
                    Attacker = user,
                    Target = target,
                    Skill = skill,
                    BaseDamage = skill.BaseDamage,
                    IsCritical = isCritical
                };

                // Phase 5: DamageInfo 생성
                DamageInfo damageInfo = DamageCalculator.CalculateDamageInfo(context);

                // 데미지 적용
                target.Health.TakeDamage(damageInfo.FinalDamage);

                // 결과 저장
                result.Damages[target] = damageInfo.FinalDamage;
                result.Criticals[target] = isCritical;

                // 데미지 이벤트 발생
                DungeonLog.Character.CharacterEvents.NotifyDamaged(target.gameObject, damageInfo.FinalDamage, isCritical);

                // Phase 5: UI 데미지 텍스트 표시를 위한 이벤트 발생
                DungeonLog.Character.CharacterEvents.NotifyDamagedWithInfo(
                    target.transform.position + Vector3.up * 2f, // 캐릭터 위쪽에 표시
                    damageInfo
                );

                // Phase 6: 상태 이상 부여
                ApplyStatusEffectFromSkill(user, skill, target);

                Debug.Log($"[SkillExecutor] {target.CharacterName}에게 {damageInfo.FinalDamage} 데미지! (치명타: {isCritical})");
            }

            result.IsSuccess = true;
        }

        // ========================================================================
        // 버프 스킬 실행
        // ========================================================================

        /// <summary>
        /// 버프 스킬을 실행합니다.
        /// Phase 6: 상태 이상 시스템 연동 완료
        /// </summary>
        private void ExecuteBuffSkill(DungeonLog.Character.Character user, SkillData skill, List<DungeonLog.Character.Character> targets, SkillExecutionResult result)
        {
            Debug.Log($"[SkillExecutor] 버프 스킬 실행: {skill.DisplayName} → {targets.Count}명");

            foreach (var target in targets)
            {
                // Phase 6: StatusEffectManager를 통한 상태 이상 부여
                if (skill.StatusEffect != null)
                {
                    // 스킬에 지정된 상태 이상 데이터가 있는 경우 적용
                    var statusManager = target.GetComponent<StatusEffectManager>();
                    if (statusManager != null)
                    {
                        statusManager.ApplyEffect(skill.StatusEffect, user, skill, skill.StatusEffectIntensity, skill.StatusEffectDuration);
                        Debug.Log($"[SkillExecutor] {target.CharacterName}에게 {skill.StatusEffect.DisplayName} 상태 이상 적용");
                    }
                }
                else
                {
                    // 하위 호환: Phase 5의 기본 StatModifier 방식 지원
                    if (skill.DisplayName.Contains("공격") || skill.ID.Contains("ATK"))
                    {
                        var modifier = new DungeonLog.Character.StatModifier
                        {
                            Value = 20f,
                            Operation = DungeonLog.Character.StatModifierOperation.PercentageBonus,
                            Priority = 0,
                            Source = skill,
                            DurationTurns = 2
                        };

                        target.Stats.AddModifier(DungeonLog.Character.StatType.Attack, modifier);
                        Debug.Log($"[SkillExecutor] {target.CharacterName}에게 AttackUp 버프 적용 (+20% 공격력, 2턴)");
                    }
                    else if (skill.DisplayName.Contains("방어") || skill.ID.Contains("DEF"))
                    {
                        var modifier = new DungeonLog.Character.StatModifier
                        {
                            Value = 30f,
                            Operation = DungeonLog.Character.StatModifierOperation.PercentageBonus,
                            Priority = 0,
                            Source = skill,
                            DurationTurns = 2
                        };

                        target.Stats.AddModifier(DungeonLog.Character.StatType.Defense, modifier);
                        Debug.Log($"[SkillExecutor] {target.CharacterName}에게 DefenseUp 버프 적용 (+30% 방어력, 2턴)");
                    }
                    else
                    {
                        Debug.Log($"[SkillExecutor] {target.CharacterName}에게 버프 적용 (상태 이상 데이터 없음)");
                    }
                }
            }

            result.IsSuccess = true;
        }

        // ========================================================================
        // 디버프 스킬 실행
        // ========================================================================

        /// <summary>
        /// 디버프 스킬을 실행합니다.
        /// Phase 6: 상태 이상 시스템 연동 완료
        /// </summary>
        private void ExecuteDebuffSkill(DungeonLog.Character.Character user, SkillData skill, List<DungeonLog.Character.Character> targets, SkillExecutionResult result)
        {
            Debug.Log($"[SkillExecutor] 디버프 스킬 실행: {skill.DisplayName} → {targets.Count}명");

            foreach (var target in targets)
            {
                // Phase 6: StatusEffectManager를 통한 상태 이상 부여
                if (skill.StatusEffect != null)
                {
                    // 스킬에 지정된 상태 이상 데이터가 있는 경우 적용
                    var statusManager = target.GetComponent<StatusEffectManager>();
                    if (statusManager != null)
                    {
                        statusManager.ApplyEffect(skill.StatusEffect, user, skill, skill.StatusEffectIntensity, skill.StatusEffectDuration);
                        Debug.Log($"[SkillExecutor] {target.CharacterName}에게 {skill.StatusEffect.DisplayName} 상태 이상 적용");
                    }
                }
                else
                {
                    // 하위 호환: Phase 5의 기본 StatModifier 방식 지원
                    if (skill.DisplayName.Contains("약화") || skill.ID.Contains("WEAK"))
                    {
                        var modifier = new DungeonLog.Character.StatModifier
                        {
                            Value = -20f,
                            Operation = DungeonLog.Character.StatModifierOperation.PercentageBonus,
                            Priority = 0,
                            Source = skill,
                            DurationTurns = 2
                        };

                        target.Stats.AddModifier(DungeonLog.Character.StatType.Attack, modifier);
                        Debug.Log($"[SkillExecutor] {target.CharacterName}에게 AttackDown 디버프 적용 (-20% 공격력, 2턴)");
                    }
                    else if (skill.DisplayName.Contains("방어력 저하") || skill.ID.Contains("DEF_DOWN"))
                    {
                        var modifier = new DungeonLog.Character.StatModifier
                        {
                            Value = -30f,
                            Operation = DungeonLog.Character.StatModifierOperation.PercentageBonus,
                            Priority = 0,
                            Source = skill,
                            DurationTurns = 2
                        };

                        target.Stats.AddModifier(DungeonLog.Character.StatType.Defense, modifier);
                        Debug.Log($"[SkillExecutor] {target.CharacterName}에게 DefenseDown 디버프 적용 (-30% 방어력, 2턴)");
                    }
                    else
                    {
                        Debug.Log($"[SkillExecutor] {target.CharacterName}에게 디버프 적용 (상태 이상 데이터 없음)");
                    }
                }
            }

            result.IsSuccess = true;
        }

        // ========================================================================
        // 힐 스킬 실행
        // ========================================================================

        /// <summary>
        /// 힐 스킬을 실행합니다.
        /// Magic 스탯을 사용하여 힐량을 계산합니다.
        /// </summary>
        private void ExecuteHealSkill(DungeonLog.Character.Character user, SkillData skill, List<DungeonLog.Character.Character> targets, SkillExecutionResult result)
        {
            Debug.Log($"[SkillExecutor] 힐 스킬 실행: {skill.DisplayName} → {targets.Count}명");

            foreach (var target in targets)
            {
                // 힐량 계산 (스킬 기본 힐량 + 캐릭터 Magic 스탯 보정)
                int baseHeal = skill.BaseDamage; // BaseDamage를 힐량으로 재사용
                int statBonus = user.Stats.GetFinalStat(DungeonLog.Character.StatType.Magic); // Magic 스탯을 힐량 보정으로 사용
                int healAmount = baseHeal + statBonus;

                // 힐 적용
                target.Health.Heal(healAmount);

                // 힐 이벤트 발생
                DungeonLog.Character.CharacterEvents.NotifyHealed(target.gameObject, healAmount);

                // Phase 5: UI 힐 텍스트 표시를 위한 이벤트 발생
                DungeonLog.Character.CharacterEvents.NotifyHealedWithPosition(
                    target.transform.position + Vector3.up * 2f,
                    healAmount
                );

                Debug.Log($"[SkillExecutor] {target.CharacterName} 힐 +{healAmount}");
            }

            result.IsSuccess = true;
        }

        // ========================================================================
        // 전투 종료 확인
        // ========================================================================

        /// <summary>
        /// 스킬 실행 후 전투 종료 조건을 확인합니다.
        /// </summary>
        private void CheckBattleEndAfterSkill(SkillExecutionResult result)
        {
            // BattleManager에서 자동으로 확인하지만, 스킬 실행 후 즉시 체크가 필요할 수 있음
            // 현재는 BattleManager.CheckBattleEnd()에 의존
        }

        // ========================================================================
        // 상태 이상 부여 헬퍼
        // ========================================================================

        /// <summary>
        /// 스킬에서 상태 이상을 부여합니다.
        /// Phase 6: 스킬 시전 시 상태 이상 부여 연동
        /// </summary>
        private void ApplyStatusEffectFromSkill(DungeonLog.Character.Character user, SkillData skill, DungeonLog.Character.Character target)
        {
            // 스킬에 상태 이상 데이터가 없으면 패스
            if (skill.StatusEffect == null)
            {
                return;
            }

            // 타겟의 StatusEffectManager 가져오기
            var statusManager = target.GetComponent<StatusEffectManager>();
            if (statusManager == null)
            {
                Debug.LogWarning($"[SkillExecutor] {target.CharacterName}에게 StatusEffectManager 컴포넌트가 없습니다.");
                return;
            }

            // 상태 이상 적용
            bool applied = statusManager.ApplyEffect(
                skill.StatusEffect,
                user,
                skill,
                skill.StatusEffectIntensity,
                skill.StatusEffectDuration
            );

            if (applied)
            {
                Debug.Log($"[SkillExecutor] {user.CharacterName}의 스킬로 {target.CharacterName}에게 {skill.StatusEffect.DisplayName} 상태 이상 부여!");
            }
        }
    }
}
