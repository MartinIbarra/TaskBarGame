using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Affix")]
    public sealed class AffixDefinition : StableDefinition
    {
        [SerializeField] private int value;

        public int Value => value;

        public void Configure(AffixBlueprint data)
        {
            SetId(data.Id);
            value = data.Value;
        }
    }
}
