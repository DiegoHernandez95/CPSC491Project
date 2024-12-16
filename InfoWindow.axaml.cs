using Avalonia.Controls;
using static Card;

namespace TCGHelper4;

// need to showcase the name, mana cost, type, oracle text, artist name, legalities, prints, and links to secondary markets

public partial class InfoWindow : Window
{
    private CardIdentifiers card;

    public InfoWindow(CardIdentifiers card)
    {
        InitializeComponent();
        this.card = card;
        SetCardDetails(); 
    }

    private void SetCardDetails()
    {
        this.FindControl<TextBlock>("CardNameTextBlock").Text = card.Name ?? "Unknown";
        this.FindControl<TextBlock>("ManaCostTextBlock").Text = card.ManaCost ?? "None";
        this.FindControl<TextBlock>("TypeLineTextBlock").Text = card.TypeLine ?? "Unknown";
        this.FindControl<TextBlock>("OracleTextTextBlock").Text = card.OracleText ?? "No description available";
        this.FindControl<TextBlock>("ArtistTextBlock").Text = card.Artist ?? "Unknown Artist";
        this.FindControl<TextBlock>("SetTextBlock").Text = card.Set ?? "Unknown Set";
        this.FindControl<TextBlock>("SetNameTextBlock").Text = card.SetName ?? "Unknown Set Name";
        
        if (card.Legalities != null)
        {
            var legalitiesText = "";

            var legalitiesProperties = card.Legalities.GetType().GetProperties();
            foreach (var property in legalitiesProperties)
            {
                var value = property.GetValue(card.Legalities)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    legalitiesText += $"{property.Name}: {value}\n";
                }
            }

            this.FindControl<TextBlock>("LegalitiesTextBlock").Text = legalitiesText;
        }

        if (card.PurchaseUris != null)
        {
            var tcgPlayerPanel = this.FindControl<ContentControl>("TCGPlayerPanel");
            var tcgPlayerButton = this.FindControl<Button>("TCGPlayerButton");
            if (!string.IsNullOrEmpty(card.PurchaseUris.TCGPlayer))
            {
                tcgPlayerPanel.IsVisible = true;
                tcgPlayerButton.Tag = card.PurchaseUris.TCGPlayer;
            }
        
            var cardMarketPanel = this.FindControl<ContentControl>("CardMarketPanel");
            var cardMarketButton = this.FindControl<Button>("CardMarketButton");
            if (!string.IsNullOrEmpty(card.PurchaseUris.cardmarket))
            {
                cardMarketPanel.IsVisible = true;
                cardMarketButton.Tag = card.PurchaseUris.cardmarket;
            }
        
            var cardhoarderPanel = this.FindControl<ContentControl>("CardhoarderPanel");
            var cardhoarderButton = this.FindControl<Button>("CardhoarderButton");
            if (!string.IsNullOrEmpty(card.PurchaseUris.cardhoarder))
            {
                cardhoarderPanel.IsVisible = true;
                cardhoarderButton.Tag = card.PurchaseUris.cardhoarder;
            }
        }
    }

    private void OnPurchaseButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string url)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
