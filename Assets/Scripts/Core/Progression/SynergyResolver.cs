using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskbarTactics.Core.Progression
{
    public sealed class TagSource
    {
        public string SourceId;
        public IReadOnlyList<string> TagIds;

        public TagSource(string sourceId, IReadOnlyList<string> tagIds)
        {
            SourceId = sourceId;
            TagIds = tagIds;
        }
    }

    public sealed class ActiveSynergy
    {
        public string TagId;
        public int Tier;
        public int SourceCount;
    }

    public sealed class SynergyResolver
    {
        public IReadOnlyList<ActiveSynergy> Resolve(IReadOnlyList<TagSource> sources)
        {
            Dictionary<string, HashSet<string>> contributors =
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (TagSource source in sources)
            {
                foreach (string tag in source.TagIds.Distinct())
                {
                    if (!contributors.TryGetValue(tag, out HashSet<string> ids))
                    {
                        ids = new HashSet<string>(StringComparer.Ordinal);
                        contributors.Add(tag, ids);
                    }

                    ids.Add(source.SourceId);
                }
            }

            return contributors
                .Where(pair => pair.Value.Count >= 2)
                .Select(pair => new ActiveSynergy
                {
                    TagId = pair.Key,
                    Tier = pair.Value.Count >= 3 ? 2 : 1,
                    SourceCount = pair.Value.Count
                })
                .OrderBy(item => item.TagId)
                .ToList();
        }
    }
}
