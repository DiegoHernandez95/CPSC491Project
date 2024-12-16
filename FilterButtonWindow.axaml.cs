using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;

namespace TCGHelper4
{
    public partial class FilterButtonWindow : Window
    {
        public event Action<Dictionary<string, object>>? FilterApplied;

        public FilterButtonWindow()
        {
            InitializeComponent();
        }

        private void ApplyFilterButton_Click(object? sender, RoutedEventArgs e)
        {
            var filterSettings = new Dictionary<string, object>
            {
                { "CardType", CardTypeTextBox.Text },
                { "Keywords", KeywordsTextBox.Text },
                { "Cmc", float.TryParse(CmcTextBox.Text, out float cmcValue) ? cmcValue : (float?)null },
                { "Rarity", (RarityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() },
                { "Set", SetTextBox.Text },
                { "Artist", ArtistTextBox.Text },
                { "Colors", GetSelectedColors() },
                { "FilterMode", ColorFilterModeComboBox.SelectedIndex },
                { "Legalities", GetSelectedLegalities() }
            };

            FilterApplied?.Invoke(filterSettings);

            Close();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        public List<string> GetSelectedColors()
        {
            var selectedColors = new List<string>();

            if (WhiteCheckBox.IsChecked == true) selectedColors.Add("W");
            if (BlueCheckBox.IsChecked == true) selectedColors.Add("U");
            if (BlackCheckBox.IsChecked == true) selectedColors.Add("B");
            if (RedCheckBox.IsChecked == true) selectedColors.Add("R");
            if (GreenCheckBox.IsChecked == true) selectedColors.Add("G");
            if (ColorlessCheckBox.IsChecked == true) selectedColors.Add("C");

            return selectedColors;
        }

        public List<string> GetSelectedLegalities()
        {
            var selectedLegalities = new List<string>();
        
            if (StandardCheckBox.IsChecked == true) selectedLegalities.Add("Standard");
            if (ModernCheckBox.IsChecked == true) selectedLegalities.Add("Modern");
            if (LegacyCheckBox.IsChecked == true) selectedLegalities.Add("Legacy");
            if (VintageCheckBox.IsChecked == true) selectedLegalities.Add("Vintage");
            if (CommanderCheckBox.IsChecked == true) selectedLegalities.Add("Commander");
            if (PioneerCheckBox.IsChecked == true) selectedLegalities.Add("Pioneer");
            if (BrawlCheckBox.IsChecked == true) selectedLegalities.Add("Brawl");
            if (AlchemyCheckBox.IsChecked == true) selectedLegalities.Add("Alchemy");
            if (ExplorerCheckBox.IsChecked == true) selectedLegalities.Add("Explorer");
            if (HistoricCheckBox.IsChecked == true) selectedLegalities.Add("Historic");
            if (TimelessCheckBox.IsChecked == true) selectedLegalities.Add("Timeless");
            if (PauperCheckBox.IsChecked == true) selectedLegalities.Add("Pauper");
            if (PennyCheckBox.IsChecked == true) selectedLegalities.Add("Penny");
            if (OathbreakerCheckBox.IsChecked == true) selectedLegalities.Add("Oathbreaker");
        
            return selectedLegalities;
        }

    }
}
