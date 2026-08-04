using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Hero")]
    public sealed class HeroDefinition : StableDefinition
    {
        [SerializeField] private string displayNameEs;
        [SerializeField] private string displayNameEn;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Tooltip("Character artwork used by roster and presentation views.")]
        private Sprite artwork;
        [SerializeField] private int maxHealth;
        [SerializeField] private int power;
        [SerializeField] private int defense;
        [SerializeField] private int range;
        [SerializeField] private List<string> tagIds = new List<string>();
        [SerializeField] private List<SkillDefinition> activeSkills = new List<SkillDefinition>();
        [SerializeField] private List<SkillDefinition> passiveSkills = new List<SkillDefinition>();

        public string DisplayNameEs => displayNameEs;
        public string DisplayNameEn => displayNameEn;
        public Color Color => color;
        public Sprite Artwork => artwork;
        public int MaxHealth => maxHealth;
        public int Power => power;
        public int Defense => defense;
        public int Range => range;
        public IReadOnlyList<string> TagIds => tagIds;
        public IReadOnlyList<SkillDefinition> ActiveSkills => activeSkills;
        public IReadOnlyList<SkillDefinition> PassiveSkills => passiveSkills;

        public void Configure(
            HeroBlueprint data,
            IEnumerable<SkillDefinition> actives,
            IEnumerable<SkillDefinition> passives)
        {
            SetId(data.Id);
            displayNameEs = data.NameEs;
            displayNameEn = data.NameEn;
            ColorUtility.TryParseHtmlString(data.ColorHex, out color);
            maxHealth = data.MaxHealth;
            power = data.Power;
            defense = data.Defense;
            range = data.Range;
            tagIds = new List<string>(data.TagIds);
            activeSkills = actives.ToList();
            passiveSkills = passives.ToList();
        }
    }
}
