using System;

namespace DungeonLog.Character
{
    /// <summary>
    /// 스탯 타입을 정의합니다.
    /// </summary>
    public enum StatType
    {
        MaxHP,
        Attack,
        Magic,       // 마법 공격력/힐량 보정 (Phase 5 추가)
        Defense,
        CriticalChance,
        Speed
    }

    /// <summary>
    /// 스탯 수정자 연산 타입입니다.
    /// 적용 순서: BaseAddition → PercentageBonus → Multiplicative → FinalOverride
    /// </summary>
    public enum StatModifierOperation
    {
        BaseAddition,      // 1순위: 기본값에 더하기 (ex: +10)
        PercentageBonus,   // 2순위: 기본값 퍼센트 증가 (ex: +20%)
        Multiplicative,    // 3순위: 현재값 곱하기 (ex: x1.5)
        FinalOverride      // 4순위: 최종 값 강제 설정
    }

    /// <summary>
    /// 스탯 수정자를 나타내는 구조체입니다.
    /// 버프, 디버프, 아이템 효과 등으로 스탯을 수정할 때 사용합니다.
    /// </summary>
    public struct StatModifier : IEquatable<StatModifier>
    {
        /// <summary>수정 값</summary>
        public float Value;

        /// <summary>연산 타입</summary>
        public StatModifierOperation Operation;

        /// <summary>동일 연산 타입 내에서의 우선순위 (낮을수록 먼저 적용)</summary>
        public int Priority;

        /// <summary>수정자 출처 (스킬, 아이템, 유물 등 추적용)</summary>
        public object Source;

        /// <summary>지속 시간 (턴), 0이면 영구</summary>
        public int DurationTurns;

        /// <summary>
        /// StatModifier 생성자
        /// </summary>
        public StatModifier(float value, StatModifierOperation operation, int priority = 0, object source = null, int durationTurns = 0)
        {
            Value = value;
            Operation = operation;
            Priority = priority;
            Source = source;
            DurationTurns = durationTurns;
        }

        /// <summary>
        /// 기본 스탯에 이 수정자를 적용한 결과를 반환합니다.
        /// </summary>
        public readonly float Apply(float baseValue)
        {
            return Operation switch
            {
                StatModifierOperation.BaseAddition => baseValue + Value,
                StatModifierOperation.PercentageBonus => baseValue + (baseValue * (Value / 100f)),
                StatModifierOperation.Multiplicative => baseValue * Value,
                StatModifierOperation.FinalOverride => Value,
                _ => baseValue
            };
        }

        public readonly bool Equals(StatModifier other)
        {
            return Value == other.Value &&
                   Operation == other.Operation &&
                   Priority == other.Priority &&
                   Equals(Source, other.Source) &&
                   DurationTurns == other.DurationTurns;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is StatModifier other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Value, Operation, Priority, Source, DurationTurns);
        }

        public static bool operator ==(StatModifier left, StatModifier right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatModifier left, StatModifier right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            string sourceStr = Source?.ToString() ?? "None";
            return $"[{Operation}] {Value} (Priority: {Priority}, Source: {sourceStr}, Duration: {DurationTurns})";
        }
    }
}
