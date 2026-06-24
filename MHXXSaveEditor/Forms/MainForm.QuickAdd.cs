// Item Box "Quick Add" panel: search items by substring, pick a quantity
// (default 99, capped at 99), and drop the item into the next empty slot with a
// single click / double-click / Enter. Built entirely in code so the generated
// MainForm.Designer.cs stays untouched. Purely additive to the existing
// slot-by-slot editing workflow.

using MHXXSaveEditor.Data;
using MHXXSaveEditor.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MHXXSaveEditor
{
    public partial class MainForm
    {
        private const int QuickAddMaxQty = 99;
        private const string EmptyItemName = "-----";

        private GroupBox groupBoxQuickAdd;
        private Label lblQuickSearch;
        private TextBox txtItemSearch;
        private Label lblQuickQty;
        private NumericUpDown numQuickAddQty;
        private CheckBox chkQuickAddMax;
        private Button btnQuickAdd;
        private ListBox listBoxItemResults;

        // Called from the MainForm constructor after InitializeComponent().
        private void SetupQuickAdd()
        {
            // Make room: grow the window and the tab control so the panel fits
            // below the existing "Edit" group box on the Item Box tab.
            const int extra = 160;
            this.ClientSize = new Size(this.ClientSize.Width, this.ClientSize.Height + extra);
            this.tabControlMain.Size = new Size(this.tabControlMain.Width, this.tabControlMain.Height + extra);

            groupBoxQuickAdd = new GroupBox
            {
                Text = "Quick Add",
                Location = new Point(5, 268),
                Size = new Size(591, 145),
                Enabled = false // enabled once a save is loaded
            };

            lblQuickSearch = new Label { Text = "Search:", AutoSize = true, Location = new Point(8, 25) };

            txtItemSearch = new TextBox { Location = new Point(58, 22), Size = new Size(250, 23) };
            txtItemSearch.TextChanged += (s, e) => RefreshItemResults();
            txtItemSearch.KeyDown += TxtItemSearch_KeyDown;

            lblQuickQty = new Label { Text = "Qty:", AutoSize = true, Location = new Point(325, 25) };

            numQuickAddQty = new NumericUpDown
            {
                Location = new Point(358, 22),
                Size = new Size(55, 23),
                Minimum = 1,
                Maximum = QuickAddMaxQty,
                Value = QuickAddMaxQty
            };

            chkQuickAddMax = new CheckBox
            {
                Text = "Max (99)",
                AutoSize = true,
                Location = new Point(420, 24),
                Checked = true
            };
            chkQuickAddMax.CheckedChanged += ChkQuickAddMax_CheckedChanged;

            btnQuickAdd = new Button { Text = "Add", Location = new Point(505, 19), Size = new Size(75, 27) };
            btnQuickAdd.Click += (s, e) => QuickAddSelectedItem();

            listBoxItemResults = new ListBox { Location = new Point(8, 52), Size = new Size(572, 82) };
            listBoxItemResults.DoubleClick += (s, e) => QuickAddSelectedItem();

            groupBoxQuickAdd.Controls.Add(lblQuickSearch);
            groupBoxQuickAdd.Controls.Add(txtItemSearch);
            groupBoxQuickAdd.Controls.Add(lblQuickQty);
            groupBoxQuickAdd.Controls.Add(numQuickAddQty);
            groupBoxQuickAdd.Controls.Add(chkQuickAddMax);
            groupBoxQuickAdd.Controls.Add(btnQuickAdd);
            groupBoxQuickAdd.Controls.Add(listBoxItemResults);

            this.itemBoxTab.Controls.Add(groupBoxQuickAdd);
        }

        // Enable the panel and populate results. Called from LoadItemBox() after a save loads.
        private void EnableQuickAdd()
        {
            if (groupBoxQuickAdd == null)
                return;
            groupBoxQuickAdd.Enabled = true;
            RefreshItemResults();
        }

        private void RefreshItemResults()
        {
            if (listBoxItemResults == null)
                return;

            List<string> matches = ItemSearch.Filter(GameConstants.ItemNameList, txtItemSearch.Text);
            listBoxItemResults.BeginUpdate();
            listBoxItemResults.Items.Clear();
            foreach (string name in matches)
                listBoxItemResults.Items.Add(name);
            listBoxItemResults.EndUpdate();

            if (listBoxItemResults.Items.Count > 0)
                listBoxItemResults.SelectedIndex = 0;
        }

        private void ChkQuickAddMax_CheckedChanged(object sender, EventArgs e)
        {
            if (chkQuickAddMax.Checked)
                numQuickAddQty.Value = QuickAddMaxQty;
        }

        private void TxtItemSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                QuickAddSelectedItem();
                e.Handled = true;
                e.SuppressKeyPress = true; // suppress the Windows "ding"
            }
        }

        // First slot whose item is the empty placeholder, or -1 if the box is full.
        private int FindNextEmptyItemSlot()
        {
            for (int i = 0; i < listViewItem.Items.Count; i++)
            {
                if (listViewItem.Items[i].SubItems[1].Text == EmptyItemName)
                    return i;
            }
            return -1;
        }

        private void QuickAddSelectedItem()
        {
            if (listViewItem.Items.Count == 0)
            {
                MessageBox.Show("Load a save first.", "Quick Add");
                return;
            }

            if (listBoxItemResults.SelectedItem == null)
            {
                if (listBoxItemResults.Items.Count == 0)
                    return;
                listBoxItemResults.SelectedIndex = 0;
            }

            string name = listBoxItemResults.SelectedItem.ToString();
            int itemId = Array.IndexOf(GameConstants.ItemNameList, name);
            if (itemId < 0)
                return;

            // Always use the next empty slot, even if this item already exists.
            int slot = FindNextEmptyItemSlot();
            if (slot < 0)
            {
                MessageBox.Show("Item box is full — no empty slots.", "Quick Add");
                return;
            }

            int qty = (int)numQuickAddQty.Value;
            listViewItem.Items[slot].SubItems[1].Text = name;
            listViewItem.Items[slot].SubItems[2].Text = qty.ToString();
            player.ItemId[slot] = itemId.ToString();
            player.ItemCount[slot] = qty.ToString();

            listViewItem.EnsureVisible(slot);
        }
    }
}
