using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Enemy")]
    public sealed class EnemyDefinition : StableDefinition
    {
        [SerializeField] private string displayNameEs;
        [SerializeField] private int maxHealth;
        [SerializeField] private int power;
        [SerializeField] private int defense;
        [SerializeField] private int range;
        [SerializeField] private bool isBoss;
        [SerializeField, Tooltip("Character artwork used by combat presentation views.")]
        private Sprite artwork;

        public string DisplayNameEs => displayNameEs;
        public int MaxHealth => maxHealth;
        public int Power => power;
        public int Defense => defense;
        public int Range => range;
        public bool IsBoss => isBoss;
        public Sprite Artwork => artwork;

        public void Configure(EnemyBlueprint data)
        {
            SetId(data.Id);
            displayNameEs = data.NameEs;
            maxHealth = data.MaxHealth;
            power = data.Power;
            defense = data.Defense;
            range = data.Range;
            isBoss = data.IsBoss;
        }
    }
}
