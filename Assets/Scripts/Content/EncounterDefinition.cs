using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [CreateAssetMenu(menuName = "Taskbar Tactics/Encounter")]
    public sealed class EncounterDefinition : StableDefinition
    {
        [SerializeField] private int difficulty;
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();

        public int Difficulty => difficulty;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;

        public void Configure(string encounterId, int encounterDifficulty, IEnumerable<EnemyDefinition> units)
        {
            SetId(encounterId);
            difficulty = encounterDifficulty;
            enemies = units.ToList();
        }
    }
}
