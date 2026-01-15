using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonLog.Data;

namespace DungeonLog.AI.Core
{
    /// <summary>
    /// AI의 의사결정 결과를 담는 데이터 전송 객체 (DTO)
    /// Object Pooling을 위한 Clear() 메서드 제공
    /// </summary>
    public class AIAction
    {
        /// <summary>
        /// 행동을 수행하는 캐릭터 (적)
        /// </summary>
        public DungeonLog.Character.Character Actor { get; set; }

        /// <summary>
        /// 선택된 스킬
        /// </summary>
        public SkillData Skill { get; set; }

        /// <summary>
        /// 주요 타겟 (단일 타겟 스킬의 경우)
        /// </summary>
        public DungeonLog.Character.Character PrimaryTarget { get; set; }

        /// <summary>
        /// 모든 타겟 리스트 (AoE 스킬의 경우)
        /// </summary>
        public List<DungeonLog.Character.Character> Targets { get; set; }

        /// <summary>
        /// 이 행동의 유틸리티 점수
        /// </summary>
        public float UtilityScore { get; set; }

        /// <summary>
        /// 점수 계산 상세 정보 (디버깅용)
        /// 키: 점수 요소 이름, 값: 점수
        /// </summary>
        public Dictionary<string, float> ScoringBreakdown { get; set; }

        /// <summary>
        /// 이 행동을 선택한 이유 (디버깅/로그용)
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 유효한 액션인지 확인
        /// </summary>
        public bool IsValid => Actor != null && Skill != null && Targets != null && Targets.Count > 0;

        /// <summary>
        /// Object Pooling을 위한 초기화
        /// </summary>
        public AIAction()
        {
            Targets = new List<DungeonLog.Character.Character>();
            ScoringBreakdown = new Dictionary<string, float>();
        }

        /// <summary>
        /// Object Pooling 반환 전 초기화
        /// </summary>
        public void Clear()
        {
            Actor = null;
            Skill = null;
            PrimaryTarget = null;
            Targets.Clear();
            UtilityScore = 0f;
            ScoringBreakdown.Clear();
            Reason = string.Empty;
        }

        /// <summary>
        /// 디버깅용 문자열 변환
        /// </summary>
        public override string ToString()
        {
            if (!IsValid) return "Invalid AIAction";

            string targetNames = PrimaryTarget != null
                ? PrimaryTarget.CharacterName
                : $"{Targets.Count} targets";

            return $"[{Actor.CharacterName}] → {Skill.DisplayName} on {targetNames} (Score: {UtilityScore:F2})";
        }
    }
}
