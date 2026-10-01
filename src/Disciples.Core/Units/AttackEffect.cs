namespace Disciples.Core.Units
{
    /// <summary>Extra effect of a successful hit.</summary>
    public enum AttackEffect
    {
        None,
        Drain,
        Poison,
        Paralysis,
        Petrification,
        /// <summary>The target fights weakened and without armor for a few turns.</summary>
        Polymorph,
        /// <summary>The target leaves the battle when its next turn comes.</summary>
        Fear
    }
}
