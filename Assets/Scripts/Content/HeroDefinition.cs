using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Equipment;
using TaskbarTactics.Core.Models;
using TaskbarTactics.Core.Stats;
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

        [Header("Hero rules")]
        [SerializeField] private HeroClass heroClass;
        [SerializeField, Tooltip("Level-one values before equipment, skills and status effects.")]
        private HeroStats baseStats = HeroStats.CreateDefault();
        [SerializeField, Tooltip("Flat amount added for every level after level one.")]
        private HeroStats growthPerLevel = new HeroStats();
        [SerializeField, Tooltip("Editable weapon, off-hand and armor permissions.")]
        private HeroEquipmentProfile equipmentProfile = new HeroEquipmentProfile();

        [SerializeField] private List<string> tagIds = new List<string>();
        [SerializeField] private List<SkillDefinition> activeSkills = new List<SkillDefinition>();
        [SerializeField] private List<SkillDefinition> passiveSkills = new List<SkillDefinition>();

        public string DisplayNameEs => displayNameEs;
        public string DisplayNameEn => displayNameEn;
        public Color Color => color;
        public Sprite Artwork => artwork;
        public HeroClass HeroClass => heroClass;
        public HeroStats BaseStats => baseStats;
        public HeroStats GrowthPerLevel => growthPerLevel;
        public HeroEquipmentProfile EquipmentProfile => equipmentProfile;
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
            heroClass = data.HeroClass;
            baseStats = data.BaseStats.Clone();
            growthPerLevel = data.GrowthPerLevel.Clone();
            equipmentProfile = data.EquipmentProfile;
            tagIds = new List<string>(data.TagIds);
            activeSkills = actives.ToList();
            passiveSkills = passives.ToList();
        }
    }
}
