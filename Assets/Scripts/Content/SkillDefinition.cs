using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Skill")]
    public sealed class SkillDefinition : StableDefinition
    {
        [SerializeField] private string ownerHeroId;
        [SerializeField] private bool isPassive;
        [SerializeField] private int cooldownTicks;
        [SerializeField] private float magnitude;

        public string OwnerHeroId => ownerHeroId;
        public bool IsPassive => isPassive;
        public int CooldownTicks => cooldownTicks;
        public float Magnitude => magnitude;

        public void Configure(SkillBlueprint data)
        {
            SetId(data.Id);
            ownerHeroId = data.HeroId;
            isPassive = data.IsPassive;
            cooldownTicks = data.CooldownTicks;
            magnitude = data.Magnitude;
        }
    }
}
