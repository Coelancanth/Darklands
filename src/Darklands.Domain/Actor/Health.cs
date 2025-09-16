using System;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Actor
{
    /// <summary>
    /// Represents an actor's health state with current and maximum values.
    /// Immutable value object that provides safe health operations for tactical combat.
    /// </summary>
    public readonly record struct Health
    {
        /// <summary>
        /// Current health points remaining.
        /// </summary>
        public int Current { get; }

        /// <summary>
        /// Maximum health points possible.
        /// </summary>
        public int Maximum { get; }

        /// <summary>
        /// Indicates whether the actor is considered dead (Current <= 0).
        /// </summary>
        public bool IsDead => Current <= 0;

        /// <summary>
        /// Indicates whether the actor is at full health.
        /// </summary>
        public bool IsFullHealth => Current >= Maximum;

        /// <summary>
        /// Gets the percentage of health remaining (0.0 to 1.0).
        /// </summary>
        public double HealthPercentage => Maximum > 0 ? (double)Current / Maximum : 0.0;

        /// <summary>
        /// Private constructor to enforce factory method usage.
        /// </summary>
        private Health(int current, int maximum)
        {
            Current = current;
            Maximum = maximum;
        }

        /// <summary>
        /// Creates a new Health instance with validation.
        /// </summary>
        /// <param name="current">Current health points</param>
        /// <param name="maximum">Maximum health points</param>
        /// <returns>Valid Health instance or validation error</returns>
        public static Fin<Health> Create(int current, int maximum)
        {
            if (maximum <= 0)
                return Error.New("INVALID_HEALTH: Maximum health must be greater than 0");

            if (current < 0)
                return Error.New("INVALID_HEALTH: Current health cannot be negative");

            if (current > maximum)
                return Error.New("INVALID_HEALTH: Current health cannot exceed maximum");

            return new Health(current, maximum);
        }

        /// <summary>
        /// Creates a new Health instance at full health.
        /// </summary>
        /// <param name="maximum">Maximum health points</param>
        /// <returns>Health instance at full health or validation error</returns>
        public static Fin<Health> CreateAtFullHealth(int maximum) =>
            Create(maximum, maximum);

        /// <summary>
        /// Creates a dead Health instance (0 current health).
        /// </summary>
        /// <param name="maximum">Maximum health points</param>
        /// <returns>Dead Health instance or validation error</returns>
        public static Fin<Health> CreateDead(int maximum) =>
            Create(0, maximum);

        /// <summary>
        /// Applies damage to current health, ensuring it doesn't go below 0.
        /// </summary>
        /// <param name="damage">Amount of damage to apply (must be non-negative)</param>
        /// <returns>New Health instance with damage applied or validation error</returns>
        public Fin<Health> TakeDamage(int damage)
        {
            if (damage < 0)
                return Error.New("INVALID_DAMAGE: Damage amount cannot be negative");

            var newCurrent = Math.Max(0, Current - damage);
            return Create(newCurrent, Maximum);
        }

        /// <summary>
        /// Applies healing to current health, ensuring it doesn't exceed maximum.
        /// </summary>
        /// <param name="healAmount">Amount of healing to apply (must be non-negative)</param>
        /// <returns>New Health instance with healing applied or validation error</returns>
        public Fin<Health> Heal(int healAmount)
        {
            if (healAmount < 0)
                return Error.New("INVALID_HEAL: Heal amount cannot be negative");

            var newCurrent = Math.Min(Maximum, Current + healAmount);
            return Create(newCurrent, Maximum);
        }

        /// <summary>
        /// Sets health to maximum (full heal).
        /// </summary>
        /// <returns>Health instance at full health</returns>
        public Health RestoreToFull() => new(Maximum, Maximum);

        /// <summary>
        /// Sets health to zero (instant death).
        /// </summary>
        /// <returns>Dead Health instance</returns>
        public Health SetToDead() => new(0, Maximum);

        /// <summary>
        /// Common health presets for testing and common scenarios.
        /// </summary>
        public static class Presets
        {
            public static readonly Fin<Health> WarriorFullHealth = CreateAtFullHealth(100);
            public static readonly Fin<Health> MageFullHealth = CreateAtFullHealth(60);
            public static readonly Fin<Health> RogueFullHealth = CreateAtFullHealth(80);
            public static readonly Fin<Health> DeadWarrior = CreateDead(100);
        }

        public override string ToString() => $"Health({Current}/{Maximum})";
    }
}
