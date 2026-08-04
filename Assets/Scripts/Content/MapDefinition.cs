using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTactics.Core.Models;
using UnityEngine;

namespace TaskbarTactics.Content
{
    [Serializable]
    public sealed class MapNodeDefinition
    {
        public string Id;
        public MapNodeType Type;
        public int Difficulty;
        public List<string> NextNodeIds = new List<string>();
    }

    [CreateAssetMenu(menuName = "Taskbar Tactics/Map")]
    public sealed class MapDefinition : StableDefinition
    {
        [SerializeField] private List<MapNodeDefinition> nodes = new List<MapNodeDefinition>();

        public IReadOnlyList<MapNodeDefinition> Nodes => nodes;

        public void Configure(string mapId, IEnumerable<MapNodeBlueprint> source)
        {
            SetId(mapId);
            nodes = source.Select(item => new MapNodeDefinition
            {
                Id = item.Id,
                Type = item.Type,
                Difficulty = item.Difficulty,
                NextNodeIds = new List<string>(item.NextNodeIds)
            }).ToList();
        }

        public MapNodeDefinition FindNode(string nodeId)
        {
            return nodes.Find(node => node.Id == nodeId);
        }
    }
}
