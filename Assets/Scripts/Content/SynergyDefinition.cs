using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Synergy")]
    public sealed class SynergyDefinition : StableDefinition
    {
        [SerializeField] private int tierOneThreshold = 2;
        [SerializeField] private int tierTwoThreshold = 3;
        [SerializeField] private float tierOneBonus = 0.1f;
        [SerializeField] private float tierTwoBonus = 0.2f;

        public int TierOneThreshold => tierOneThreshold;
        public int TierTwoThreshold => tierTwoThreshold;
        public float TierOneBonus => tierOneBonus;
        public float TierTwoBonus => tierTwoBonus;

        public void Configure(string tagId)
        {
            SetId(tagId);
        }
    }
}
