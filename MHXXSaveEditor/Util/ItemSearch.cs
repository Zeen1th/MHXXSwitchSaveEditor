using System;
using System.Collections.Generic;

namespace MHXXSaveEditor.Util
{
    // Pure, UI-free item-name filtering used by the Item Box Quick Add panel.
    public static class ItemSearch
    {
        // Empty placeholder entry in GameConstants.ItemNameList (index 0 == empty slot).
        private const string EmptyName = "-----";

        // Case-insensitive substring match over item names. The empty placeholder is
        // never returned. An empty/whitespace query returns every real item, in order.
        public static List<string> Filter(string[] names, string query)
        {
            var results = new List<string>();
            if (names == null)
                return results;

            string q = (query ?? string.Empty).Trim();
            bool matchAll = q.Length == 0;

            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name) || name == EmptyName)
                    continue;
                if (matchAll || name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    results.Add(name);
            }
            return results;
        }
    }
}
