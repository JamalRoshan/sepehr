using System;
using System.Collections.Generic;
using System.Linq;

namespace Sepehr.Domain
{
    public enum HierarchyResult { Valid, UnknownParent, SelfParent, Cycle, TooDeep }

    public static class GroupHierarchy
    {
        public const int DefaultMaxDepth = 3;

        /// <param name="parents">نگاشت همه گروه‌های موجود به والدشان (ریشه: null).</param>
        /// <param name="groupId">گروه موجود برای جابه‌جایی، یا null برای گروه جدید.</param>
        public static HierarchyResult ValidateParent(
            IReadOnlyDictionary<long, long?> parents, long? groupId, long? newParentId,
            int maxDepth = DefaultMaxDepth)
        {
            if (newParentId == null)
                return SubtreeHeight(parents, groupId) <= maxDepth ? HierarchyResult.Valid : HierarchyResult.TooDeep;

            if (newParentId == groupId) return HierarchyResult.SelfParent;
            if (!parents.ContainsKey(newParentId.Value)) return HierarchyResult.UnknownParent;

            var parentDepth = 0;
            var visited = new HashSet<long>();
            long? current = newParentId;
            while (current != null)
            {
                if (current == groupId || !visited.Add(current.Value)) return HierarchyResult.Cycle;
                parentDepth++;
                if (!parents.TryGetValue(current.Value, out current)) return HierarchyResult.UnknownParent;
            }

            return parentDepth + SubtreeHeight(parents, groupId) <= maxDepth
                ? HierarchyResult.Valid
                : HierarchyResult.TooDeep;
        }

        private static int SubtreeHeight(IReadOnlyDictionary<long, long?> parents, long? rootId)
        {
            if (rootId == null) return 1;
            return Height(parents, rootId.Value, new HashSet<long>());
        }

        private static int Height(IReadOnlyDictionary<long, long?> parents, long id, HashSet<long> visited)
        {
            if (!visited.Add(id)) throw new InvalidOperationException("Existing group hierarchy contains a cycle.");

            var deepestChild = parents
                .Where(pair => pair.Value == id)
                .Select(pair => Height(parents, pair.Key, visited))
                .DefaultIfEmpty(0)
                .Max();

            return 1 + deepestChild;
        }
    }
}
