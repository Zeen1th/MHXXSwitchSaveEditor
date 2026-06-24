# Item Quick-Add — Design

**Date:** 2026-06-25
**Project:** MHXXSwitchSaveEditor fork (Monster Hunter Generations Ultimate / XX save editor)
**Author:** Zeen1th

## Goal

Make adding items to the Item Box fast. Today, adding one item takes three
fiddly steps with a weak search. This feature adds a **Quick Add** panel that
lets the user search items by any substring, choose a quantity (default 99),
and drop the item into the next empty slot with a single click.

This is **purely additive**. The existing slot-by-slot editing workflow
(select a row → pick from `comboBoxItem` → set `numericUpDownItemAmount`)
remains unchanged.

## Current State

On the **Item Box** tab (`MHXXSaveEditor/Forms/MainForm.cs`):

- `listViewItem` holds `Constants.TOTAL_ITEM_SLOTS` (2,300) fixed slots, columns:
  slot # (1-based), item name, count.
- `GameConstants.ItemNameList` is a `string[]` of all item names; index 0 is
  `"-----"` (empty). The item's stored ID is its index in this array.
- Adding an item today:
  1. Select an empty slot row (`ListViewItem_SelectedIndexChanged`, line ~630).
  2. Choose from `comboBoxItem`, a single unsorted combo of every name, prefix
     type-ahead only (`ComboBoxItem_SelectedIndexChanged`, line ~642).
  3. Set quantity via `numericUpDownItemAmount` (`NumericUpDownItemAmount_ValueChanged`, line ~666).
- On save (`SaveToolStripMenuItemSave_Click` path, line ~583), each row's name is
  converted back to its index via `Array.IndexOf(GameConstants.ItemNameList, name)`
  and count is packed: count in 7 bits (PadLeft 7), id in 12 bits (PadLeft 12).
  **Count storage is 7 bits → max 127; in-game stack cap is 99.**

## Requirements

1. **Substring search** — case-insensitive "contains" match against
   `ItemNameList` (excluding the `"-----"` empty entry), filtering live as the
   user types.
2. **Quantity** — a numeric box defaulting to **99**, **capped at 99** (game
   stack cap). A **"Max (99)" checkbox** sets the box to 99 in one tap.
3. **One-click add** — clicking **Add**, double-clicking a result, or pressing
   **Enter** in the search box (when a result is selected) places the item in
   the **next empty slot** at the chosen quantity, then refreshes the list.
4. **Duplicate handling** — **always** use the next empty slot, even if a stack
   of that item already exists (no stacking/merging).
5. **Box full** — if no empty slot exists, show a clear message and add nothing.

## UI

Add a **Quick Add** group box to the Item Box tab, alongside the existing
editing controls (exact placement finalized during implementation to avoid
overlapping `listViewItem` / `comboBoxItem` / `numericUpDownItemAmount`):

- `txtItemSearch` — TextBox; `TextChanged` refilters results.
- `listBoxItemResults` — ListBox showing filtered names; `DoubleClick` adds.
- `numQuickAddQty` — NumericUpDown, Min 1, Max 99, default 99.
- `chkQuickAddMax` — CheckBox "Max (99)"; when checked, sets qty to 99.
- `btnQuickAdd` — Button "Add".

Controls are only meaningful after a save is loaded; they follow the same
enable/disable timing as the rest of the editing UI.

## Architecture / Components

Keep `MainForm.cs` from growing more tangled by isolating reusable logic in a
small helper rather than inlining everything in event handlers.

- **`Util/ItemSearch.cs`** (new) — pure, UI-free, unit-testable:
  - `IEnumerable<string> Filter(string[] names, string query)` — case-insensitive
    substring match, excludes `"-----"`, preserves original order, returns full
    list when query is empty/whitespace.
- **`MainForm` additions** (item-box region):
  - `FindNextEmptyItemSlot()` → first row index whose name is `"-----"`, or `-1`
    if none. (Searches `listViewItem.Items`, the live UI state.)
  - `RefreshItemResults()` — repopulate `listBoxItemResults` from
    `ItemSearch.Filter(GameConstants.ItemNameList, txtItemSearch.Text)`.
  - `QuickAddSelectedItem()` — read selected result name + quantity, find next
    empty slot, write name and count into that row, update `player.ItemId` /
    `player.ItemCount` for that slot (mirroring existing handlers), refresh.
  - Event wiring: `txtItemSearch.TextChanged`, `listBoxItemResults.DoubleClick`,
    `txtItemSearch.KeyDown` (Enter), `btnQuickAdd.Click`, `chkQuickAddMax.CheckedChanged`.

## Data Flow

1. User types in `txtItemSearch` → `RefreshItemResults()` → `ItemSearch.Filter`
   → `listBoxItemResults` repopulated.
2. User sets quantity (or ticks Max → 99).
3. User triggers add (button / double-click / Enter) → `QuickAddSelectedItem()`:
   - `slot = FindNextEmptyItemSlot()`; if `-1`, show "Item box is full" and stop.
   - Set `listViewItem.Items[slot].SubItems[1].Text = name`,
     `SubItems[2].Text = qty`.
   - Set `player.ItemId[slot] = Array.IndexOf(ItemNameList, name).ToString()`,
     `player.ItemCount[slot] = qty.ToString()`.
4. Existing save path serializes rows unchanged — no save-format changes.

## Error Handling

- **No save loaded** — Quick Add controls disabled until a save is loaded
  (consistent with existing UI gating).
- **Empty search / no result selected on add** — no-op (optionally a brief hint);
  never throws.
- **Box full** — `MessageBox` "Item box is full — no empty slots."; nothing added.
- **Quantity bounds** — enforced by NumericUpDown (1–99); no manual validation needed.

## Testing

- **Unit tests for `ItemSearch.Filter`** (the only pure logic): empty query
  returns all (minus `"-----"`); case-insensitive substring; no match → empty;
  order preserved. Added in a lightweight test project (`MHXXSaveEditor.Tests`)
  if a test runner is reasonable on the toolchain; otherwise documented manual
  checks. Toolchain/build modernization is **out of scope** for this change.
- **Manual verification** — load a save, search "potion", add at 99, confirm it
  lands in the first empty slot; fill the box and confirm the full-box message;
  confirm existing slot-edit workflow still works and saves correctly.

## Out of Scope

- Toolchain/.NET modernization, CI, UI theming.
- Editing equipment / palico boxes (item box only).
- Stacking/merging duplicates, category filtering, sorting options.

## Repository / Fork

- Fork `Dawnshifter/MHXXSwitchSaveEditor` into the `Zeen1th` GitHub account (the
  active `gh` login) via `gh repo fork --remote`, set the fork as `origin`, keep
  `upstream` pointing at the original. Work lands on a feature branch and is
  pushed to the fork.
