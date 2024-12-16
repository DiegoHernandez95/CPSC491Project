using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Layout;
using Avalonia;
using static Card;
using System.IO;
using Avalonia.Threading;

// current functionality:
// card search, sets and individual
// card pricing
// deck creation and deletion
// cards can now be deleted from decks and the ui updates
// card info
// links to secondary market
// filters
// export (kind of implemented this sort of sucks)


// another issue to notice, depending on screen size certain features might not fit the screen
// if you are using a small computer might need to lower the display scale

namespace TCGHelper4;

public class ScryfallApiResponse
{
    [JsonProperty("data")]
    public List<Card.CardIdentifiers> Data { get; set; } = new List<Card.CardIdentifiers>();
}

public partial class MainWindow : Window
{   
    private HttpClient httpClient;
    public Dictionary<string, CardIdentifiers> cardDataByOracleId = new Dictionary<string, CardIdentifiers>();
    public Dictionary<string, string> imageUrlToOracleId = new Dictionary<string, string>();
    public Dictionary<string, string> printingUris = new Dictionary<string, string>();
    private Dictionary<string, List<Card.CardIdentifiers>> cardNameIndex = new();
    private Dictionary<string, List<Card.CardIdentifiers>> setNameIndex = new();
    private bool isIndexBuilt = false;
    private const int CARDS_PER_PAGE = 200;
    private Dictionary<string, Dictionary<int, WrapPanel>> pageCache = new();
    private int currentPage = 0;
    private List<Card.CardIdentifiers> currentSetCards = new();

    public void CacheCardData(List<CardIdentifiers> cards)
    {
        foreach (var card in cards)
        {
            string cardId = card.OracleID ?? card.Name ?? "";
            if (!string.IsNullOrEmpty(cardId))
            {
                if (string.IsNullOrEmpty(card.PrintsSearchUri))
                {
                    Console.WriteLine($"Missing PrintsSearchUri for card: {card.Name}");
                }

                if (!cardDataByOracleId.ContainsKey(cardId))
                {
                    cardDataByOracleId[cardId] = card;
                }

                // Single Faced Cards
                if (card.ImageUris != null)
                {
                    var imageUrlList = new[] { card.ImageUris.Small, card.ImageUris.Normal, card.ImageUris.Large, card.ImageUris.Png };
                    foreach (var url in imageUrlList)
                    {
                        if (!string.IsNullOrEmpty(url) && !imageUrlToOracleId.ContainsKey(url))
                        {
                            imageUrlToOracleId[url] = cardId;
                        }
                    }
                }

                // Cards with two faces
                if (card.CardFaces != null)
                {
                    foreach (var face in card.CardFaces)
                    {
                        if (face.ImageUris != null)
                        {
                            var faceUrlList = new[] { face.ImageUris.Small, face.ImageUris.Normal, face.ImageUris.Large, face.ImageUris.Png };
                            foreach (var url in faceUrlList)
                            {
                                if (!string.IsNullOrEmpty(url) && !imageUrlToOracleId.ContainsKey(url))
                                {
                                    imageUrlToOracleId[url] = cardId;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"Skipped card without any identifier - Name: {card.Name}");
            }
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        httpClient = new HttpClient(); 
        httpClient.DefaultRequestHeaders.Add("User-Agent", "TCGHelper4/1.0");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json;q=0.9,*/*;q=0.8");
        
        InitializationOverlay.IsVisible = true;
        
        Task.Run(async () => 
        {
            await InitializeCards();
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                InitializationOverlay.IsVisible = false;
            });
        });
    }

    // this function is really slow, then again the size of the json file is big so I guess this will be okay for 1.0
    private async Task InitializeCards()
    {
        const string bulkDataUrl = "https://data.scryfall.io/unique-artwork/unique-artwork-20241208100422.json";

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
                InitializationStatus.Text = "Checking cache...");

            if (!OracleCardCache.ShouldUpdateCache())
            {
                await Dispatcher.UIThread.InvokeAsync(() => 
                    InitializationStatus.Text = "Loading from cache...");
                    
                var cachedCards = OracleCardCache.LoadCache(bulkDataUrl);
                if (cachedCards.Count > 0)
                {
                    CacheCardData(cachedCards);
                    return;
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() => 
                InitializationStatus.Text = "Downloading card data...");

            using var client = new HttpClient();
            var oracleData = await client.GetStringAsync(bulkDataUrl);

            if (string.IsNullOrEmpty(oracleData))
            {
                Console.WriteLine("Failed to download cards data.");
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() => 
                InitializationStatus.Text = "Processing card data...");

            var oracleCards = JsonConvert.DeserializeObject<List<Card.CardIdentifiers>>(oracleData);
            if (oracleCards == null || oracleCards.Count == 0)
            {
                Console.WriteLine("Failed to deserialize card data.");
                return;
            }

            OracleCardCache.SaveCache(oracleCards, bulkDataUrl);
            CacheCardData(oracleCards);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
                InitializationStatus.Text = $"Error: {ex.Message}");
            Console.WriteLine($"Error downloading cards: {ex.Message}");
        }
    }

    private void BuildSearchIndex()
    {
        if (isIndexBuilt) return;

        var cachedCards = OracleCardCache.GetCachedCards();
        
        foreach (var card in cachedCards)
        {
            if (!string.IsNullOrEmpty(card.Name))
            {
                string lowercaseName = card.Name.ToLowerInvariant();
                if (!cardNameIndex.ContainsKey(lowercaseName))
                {
                    cardNameIndex[lowercaseName] = new List<Card.CardIdentifiers>();
                }
                cardNameIndex[lowercaseName].Add(card);
            }

            if (!string.IsNullOrEmpty(card.SetName))
            {
                string lowercaseSetName = card.SetName.ToLowerInvariant();
                if (!setNameIndex.ContainsKey(lowercaseSetName))
                {
                    setNameIndex[lowercaseSetName] = new List<Card.CardIdentifiers>();
                }
                setNameIndex[lowercaseSetName].Add(card);
            }
        }

        isIndexBuilt = true;
    }

    private List<string> SearchCachedData(string searchText)
    {
        if (!isIndexBuilt)
        {
            BuildSearchIndex();
        }

        var results = new HashSet<string>();
        searchText = searchText.ToLowerInvariant();

        // The next two for each loops both involve exceptions for secret lair drops, this shouldn't be so finicky
        // look for unique indentifiers for secret lair cards there has to be some way of figuring out each one
        // so the program doesn't automatically assume its the base card
        foreach (var entry in cardNameIndex)
        {
            if (entry.Key.Contains(searchText))
            {
                foreach (var card in entry.Value.GroupBy(c => new { c.Name, c.Set }).Select(g => g.First()))
                {
                    if (card.Set?.ToUpper().StartsWith("SLD") == true)
                    {
                        results.Add($"{card.Name} ({card.SetName})");
                    }
                    else
                    {
                        results.Add(card.Name ?? "");
                    }
                }
            }
        }

        // For some reason Secret Lair cards don't play nice when it comes to their set name
        // this works for some cards but not all of them
        foreach (var entry in setNameIndex)
        {
            if (entry.Key.Contains(searchText))
            {
                var setName = entry.Value.First().SetName;
                if (setName?.Contains("Secret Lair", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var sldCards = entry.Value.Where(card => card.Set?.ToUpper().StartsWith("SLD") == true);
                    results.Add(setName);
                }
                else
                {
                    results.Add(setName ?? "");
                }
            }
        }

        return results.OrderBy(r => r).ToList();
    }

    public async void DisplayCardImages(Card.CardIdentifiers card)
    {
        try
        {
            Console.WriteLine($"Fetching card printings for: {card.Name}");
            CardLoadingProgressBar.IsVisible = true;
            CardLoadingProgressBar.Value = 0;

            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            CardLoadingProgressBar.Value = 0;
            CardLoadingProgressBar.Maximum = 1;

            // single faced cards
            if (card.ImageUris?.Normal != null)
            {
                var frontBitmap = await DownloadImageAsync(card.ImageUris.Normal);
                if (frontBitmap != null)
                {
                    var cardControl = CardControl.CreateCardControl(
                        frontBitmap,
                        card.ImageUris.Normal,
                        null,
                        null,
                        (s, e) => ShowCardInfoWindow(card.OracleID ?? card.Name ?? "", null),
                        card
                    );
                    cardControl.Margin = new Thickness(10);
                    wrapPanel.Children.Add(cardControl);
                }
                CardLoadingProgressBar.Value++;
            }
            // Cards with two faces
            else if (card.CardFaces != null && card.CardFaces.Count > 0)
            {
                var frontImageUrl = card.CardFaces[0].ImageUris?.Normal;
                var backImageUrl = card.CardFaces.Count > 1 ? card.CardFaces[1].ImageUris?.Normal : null;

                if (!string.IsNullOrEmpty(frontImageUrl))
                {
                    var frontBitmap = await DownloadImageAsync(frontImageUrl);
                    Bitmap? backBitmap = null;
                    if (!string.IsNullOrEmpty(backImageUrl))
                    {
                        backBitmap = await DownloadImageAsync(backImageUrl);
                    }

                    if (frontBitmap != null)
                    {
                        var cardControl = CardControl.CreateCardControl(
                            frontBitmap,
                            frontImageUrl,
                            backBitmap,
                            backImageUrl,
                            (s, e) => ShowCardInfoWindow(card.OracleID ?? card.Name ?? "", null),
                            card
                        );
                        cardControl.Margin = new Thickness(10);
                        wrapPanel.Children.Add(cardControl);
                    }
                }
                CardLoadingProgressBar.Value++;
            }

            // getting the card printings
            if (!string.IsNullOrEmpty(card.PrintsSearchUri))
            {
                var response = await httpClient.GetAsync(card.PrintsSearchUri);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var printings = JObject.Parse(jsonResponse);
                    var printingsData = printings["data"] as JArray;
                    
                    if (printingsData != null)
                    {
                        CardLoadingProgressBar.Maximum += printingsData.Count;

                        foreach (var printing in printingsData)
                        {
                            if (printing["id"]?.ToString() == card.OracleID)
                                continue;

                            string? frontImageUrl = printing["image_uris"]?["normal"]?.ToString();
                            string? backImageUrl = null;

                            if (frontImageUrl == null && printing["card_faces"] is JArray cardFaces)
                            {
                                frontImageUrl = cardFaces[0]?["image_uris"]?["normal"]?.ToString();
                                backImageUrl = cardFaces.Count > 1 ? cardFaces[1]?["image_uris"]?["normal"]?.ToString() : null;
                            }

                            if (!string.IsNullOrEmpty(frontImageUrl))
                            {
                                var frontBitmap = await DownloadImageAsync(frontImageUrl);
                                Bitmap? backBitmap = null;
                                if (!string.IsNullOrEmpty(backImageUrl))
                                {
                                    backBitmap = await DownloadImageAsync(backImageUrl);
                                }

                                if (frontBitmap != null)
                                {
                                    var printingCard = new Card.CardIdentifiers
                                    {
                                        Name = card.Name,
                                        OracleID = printing["oracle_id"]?.ToString(),
                                        Set = printing["set"]?.ToString(),
                                        SetName = printing["set_name"]?.ToString(),
                                        Prices = new Card.Prices
                                        {
                                            USD = printing["prices"]?["usd"]?.ToString(),
                                        },
                                        PurchaseUris = printing["purchase_uris"]?.ToObject<Card.PurchaseUris>()
                                    };

                                    var cardControl = CardControl.CreateCardControl(
                                        frontBitmap,
                                        frontImageUrl,
                                        backBitmap,
                                        backImageUrl,
                                        (s, e) => ShowCardInfoWindow(printingCard.OracleID ?? printingCard.Name ?? "", null, 
                                                                   printingCard.Set, printingCard.SetName, printingCard.PurchaseUris),
                                        printingCard
                                    );
                                    cardControl.Margin = new Thickness(10);
                                    wrapPanel.Children.Add(cardControl);
                                }
                            }
                            CardLoadingProgressBar.Value++;
                        }
                    }
                }
            }

            ImageDisplayContent.Content = wrapPanel;
            CardLoadingProgressBar.IsVisible = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error displaying card images: {ex.Message}");
            CardLoadingProgressBar.IsVisible = false;
        }
    }
    // Probably should move all of this card control logic to the actual cardcontrol.cs file at some point
    public async void DisplaySetImages(List<Card.CardIdentifiers> setCards)
    {
        currentSetCards = setCards;
        currentPage = 0;
        
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        var paginationPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10)
        };

        var prevButton = new Button { Content = "Previous", IsEnabled = false, Margin = new Thickness(5) };
        var nextButton = new Button { Content = "Next", Margin = new Thickness(5) };
        var pageInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10) };

        prevButton.Click += async (s, e) => await ChangePage(-1);
        nextButton.Click += async (s, e) => await ChangePage(1);

        paginationPanel.Children.Add(prevButton);
        paginationPanel.Children.Add(pageInfo);
        paginationPanel.Children.Add(nextButton);

        mainPanel.Children.Add(paginationPanel);

        var contentPanel = new Panel();
        mainPanel.Children.Add(contentPanel);

        var bottomPaginationPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10)
        };
        
        var bottomPrevButton = new Button { Content = "Previous", IsEnabled = false, Margin = new Thickness(5) };
        var bottomNextButton = new Button { Content = "Next", Margin = new Thickness(5) };
        var bottomPageInfo = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10) };

        bottomPrevButton.Click += async (s, e) => await ChangePage(-1);
        bottomNextButton.Click += async (s, e) => await ChangePage(1);

        bottomPaginationPanel.Children.Add(bottomPrevButton);
        bottomPaginationPanel.Children.Add(bottomPageInfo);
        bottomPaginationPanel.Children.Add(bottomNextButton);

        mainPanel.Children.Add(bottomPaginationPanel);

        ImageDisplayContent.Content = mainPanel;

        await LoadPage(0, contentPanel, pageInfo, bottomPageInfo, prevButton, nextButton, 
                      bottomPrevButton, bottomNextButton);
    }

    private async Task LoadPage(int page, Panel contentPanel, TextBlock pageInfo, 
    TextBlock bottomPageInfo, Button prevButton, Button nextButton, 
    Button bottomPrevButton, Button bottomNextButton)
    {
        var setName = currentSetCards.FirstOrDefault()?.SetName ?? "Unknown Set";
        var wrapPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        var pageCards = currentSetCards
            .Skip(page * CARDS_PER_PAGE)
            .Take(CARDS_PER_PAGE)
            .ToList();

        CardLoadingProgressBar.IsVisible = true;
        CardLoadingProgressBar.Value = 0;
        CardLoadingProgressBar.Maximum = pageCards.Count;

        var batches = pageCards.Select((card, index) => new { card, index })
                              .GroupBy(x => x.index / 20)
                              .Select(g => g.Select(x => x.card).ToList())
                              .ToList();

        foreach (var batch in batches)
        {
            var tasks = batch.Select(async card =>
            {
                try
                {
                    Bitmap? frontBitmap = null;
                    Bitmap? backBitmap = null;
                    string? frontUrl = null;
                    string? backUrl = null;

                    if (card.ImageUris == null && card.CardFaces != null && card.CardFaces.Count > 0)
                    {
                        frontUrl = card.CardFaces[0].ImageUris?.Normal;
                        backUrl = card.CardFaces.Count > 1 ? card.CardFaces[1].ImageUris?.Normal : null;
                    }
                    else if (card.ImageUris?.Normal != null)
                    {
                        frontUrl = card.ImageUris.Normal;
                    }

                    if (!string.IsNullOrEmpty(frontUrl))
                    {
                        frontBitmap = await DownloadImageAsync(frontUrl);
                        if (!string.IsNullOrEmpty(backUrl))
                        {
                            backBitmap = await DownloadImageAsync(backUrl);
                        }
                    }

                    return (card, frontBitmap, backBitmap, frontUrl, backUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading card {card.Name}: {ex.Message}");
                    return (card, null, null, null, null);
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var result in results)
                {
                    if (result.frontBitmap != null)
                    {
                        var cardControl = CardControl.CreateCardControl(
                            result.frontBitmap,
                            result.frontUrl ?? "",
                            result.backBitmap,
                            result.backUrl,
                            (s, e) => ShowCardInfoWindow(result.card.OracleID ?? "", null),
                            result.card
                        );
                        wrapPanel.Children.Add(cardControl);
                    }
                    CardLoadingProgressBar.Value++;
                }
            });
        }

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            contentPanel.Children.Clear();
            contentPanel.Children.Add(wrapPanel);

            var totalPages = (int)Math.Ceiling(currentSetCards.Count / (double)CARDS_PER_PAGE);
            var pageText = $"Page {page + 1} of {totalPages}";
            pageInfo.Text = pageText;
            bottomPageInfo.Text = pageText;

            prevButton.IsEnabled = bottomPrevButton.IsEnabled = page > 0;
            nextButton.IsEnabled = bottomNextButton.IsEnabled = page < totalPages - 1;

            CardLoadingProgressBar.IsVisible = false;
        });
    }

    private async Task ChangePage(int delta)
    {
        var newPage = currentPage + delta;
        var totalPages = (int)Math.Ceiling(currentSetCards.Count / (double)CARDS_PER_PAGE);
        
        if (newPage >= 0 && newPage < totalPages)
        {
            currentPage = newPage;
            var contentPanel = ((ImageDisplayContent.Content as StackPanel)?.Children[1] as Panel)!;
            var pageInfo = ((ImageDisplayContent.Content as StackPanel)?.Children[0] as StackPanel)?.Children[1] as TextBlock;
            var bottomPageInfo = ((ImageDisplayContent.Content as StackPanel)?.Children[2] as StackPanel)?.Children[1] as TextBlock;
            var prevButton = ((ImageDisplayContent.Content as StackPanel)?.Children[0] as StackPanel)?.Children[0] as Button;
            var nextButton = ((ImageDisplayContent.Content as StackPanel)?.Children[0] as StackPanel)?.Children[2] as Button;
            var bottomPrevButton = ((ImageDisplayContent.Content as StackPanel)?.Children[2] as StackPanel)?.Children[0] as Button;
            var bottomNextButton = ((ImageDisplayContent.Content as StackPanel)?.Children[2] as StackPanel)?.Children[2] as Button;

            await LoadPage(currentPage, contentPanel!, pageInfo!, bottomPageInfo!, 
                          prevButton!, nextButton!, bottomPrevButton!, bottomNextButton!);
        }
    }

    public void StoreCardInfo(string oracleId)
    {
        oracleId = oracleId.Trim();

        Console.WriteLine($"Attempting to find card with Oracle ID: {oracleId}");

        if (cardDataByOracleId.TryGetValue(oracleId, out var card))
        {
            string cardName = card.Name ?? "Unknown";
            string manaCost = card.ManaCost ?? "None";
            string typeLine = card.TypeLine ?? "Unknown";
            string oracleText = card.OracleText ?? "No description available";
            string artist = card.Artist ?? "Unknown Artist";
            Legalities? legalities = card.Legalities;
            string printsSearchUri = card.PrintsSearchUri ?? "No prints available";
            PurchaseUris? purchaseUris = card.PurchaseUris;

            if (purchaseUris != null)
            {
                Console.WriteLine($"Purchase Uris: TCGPlayer - {purchaseUris.TCGPlayer}");
                Console.WriteLine($"Purchase Uris: CardMarket - {purchaseUris.cardmarket}");
                Console.WriteLine($"Purchase Uris: CardHoarder - {purchaseUris.cardhoarder}");
            }
        }
        else
        {
            Console.WriteLine($"Card information not found for the provided Oracle ID: {oracleId}");
        }
    }

    public static async Task<Bitmap?> DownloadImageAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
            {
                Console.WriteLine($"Invalid image URL: {imageUrl}");
                return null;
            }

            using var client = new HttpClient();
            var response = await client.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                return new Bitmap(stream);
            }
            else
            {
                Console.WriteLine($"Failed to download image: {imageUrl}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading image: {ex.Message}");
            return null;
        }
    }

    // as long as you type 2 letters or more then the searchbox will begin looking for cards
    private void SearchBox_TextChanged(object sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            string searchText = textBox.Text.Trim();

            SetListBox.SelectedItem = null;

            if (!string.IsNullOrEmpty(searchText))
            {
                if (searchText.Length >= 2) 
                {
                    var searchResults = SearchCachedData(searchText);

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        SetListBox.ItemsSource = searchResults;
                    });
                }
                else
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        SetListBox.ItemsSource = null;
                    });
                }
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SetListBox.ItemsSource = null;
                });
            }
        }
    }

    // populating the set list box with the suggested names of cards as you type
    private void SetListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string selectedItem)
        {
            var cachedCards = OracleCardCache.GetCachedCards();

            // secret lair cards are so annoying
            if (selectedItem.Contains("(") && selectedItem.Contains(")"))
            {
                var name = selectedItem.Substring(0, selectedItem.IndexOf("(")).Trim();
                var setName = selectedItem.Substring(selectedItem.IndexOf("(") + 1, 
                             selectedItem.IndexOf(")") - selectedItem.IndexOf("(") - 1);

                var secretLairCard = cachedCards.FirstOrDefault(card => 
                    card.Name == name && card.SetName == setName);

                if (secretLairCard != null)
                {
                    DisplayCardImages(secretLairCard);
                    return;
                }
            }

            var selectedCard = cachedCards.FirstOrDefault(card => card.Name == selectedItem);
            if (selectedCard != null)
            {
                DisplayCardImages(selectedCard);
            }
            else
            {
                // Check if the selection is a set
                var setCards = cachedCards.Where(card => card.SetName == selectedItem).ToList();
                if (setCards.Count > 0)
                {
                    DisplaySetImages(setCards);
                }
                else
                {
                    Console.WriteLine($"No cards or sets found for selection: {selectedItem}");
                }
            }
        }
    }
    private async void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        var filterWindow = new FilterButtonWindow();
        filterWindow.FilterApplied += filterSettings =>
        {
            ApplyFilters(filterSettings);
        };

        await filterWindow.ShowDialog(this); 
    }
    private void CreateDeckButton_Click(object sender, RoutedEventArgs e)
    {
        CreateDeckWindow createDeckWindow = new CreateDeckWindow();
        createDeckWindow.Show();
    }

    private void ViewDecksButton_Click(object sender, RoutedEventArgs e)
    {
        var viewDecksWindow = new ViewDecksWindow();
        viewDecksWindow.DeckSelected += LoadSelectedDeck;
        viewDecksWindow.Show();
    }

    public CardIdentifiers? GetCardInfo(string oracleId)
    {
        oracleId = oracleId.Trim();
        Console.WriteLine($"Attempting to find card with Oracle ID: {oracleId}");

        if (cardDataByOracleId.TryGetValue(oracleId, out var card))
        {
            return card;
        }
        else
        {
            Console.WriteLine($"Card information not found for the provided Oracle ID: {oracleId}");
            return null;
        }
    }

    // pulling all the information from the JSON file about the card using the setters and getters from card.cs
    // might add more depending on what information is really important, maybe cmc?
    public async void ShowCardInfoWindow(string oracleId, string? printingUri, string? set = null, string? setName = null, Card.PurchaseUris? purchaseUris = null)
    {
        try
        {
            Card.CardIdentifiers? cardDetails = null;
            if (cardDataByOracleId.TryGetValue(oracleId, out var cachedCard))
            {
                cardDetails = cachedCard;
                
                if (!string.IsNullOrEmpty(set)) cardDetails.Set = set;
                if (!string.IsNullOrEmpty(setName)) cardDetails.SetName = setName;
                if (purchaseUris != null) cardDetails.PurchaseUris = purchaseUris;
                
                if (!string.IsNullOrEmpty(printingUri))
                {
                    var response = await httpClient.GetAsync(printingUri);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var printingDetails = JsonConvert.DeserializeObject<Card.CardIdentifiers>(jsonResponse);
                        
                        if (printingDetails != null)
                        {
                            cardDetails.Prices = printingDetails.Prices;
                            cardDetails.Set = printingDetails.Set ?? set ?? cardDetails.Set;
                            cardDetails.SetName = printingDetails.SetName ?? setName ?? cardDetails.SetName;
                            cardDetails.PurchaseUris = printingDetails.PurchaseUris ?? purchaseUris ?? cardDetails.PurchaseUris;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"Card not found in cache for Oracle ID: {oracleId}");
                return;
            }

            var infoWindow = new InfoWindow(cardDetails);
            infoWindow.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error displaying card info: {ex.Message}");
        }
    }

    private async void LoadSelectedDeck(string selectedDeck)
    {
        ImageDisplayContent.Tag = selectedDeck;
        string selectedDeckPath = Path.Combine(Environment.CurrentDirectory, "Decks", selectedDeck);
        var cardFiles = Directory.GetFiles(selectedDeckPath, "*.json");

        var wrapPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        ImageDisplayContent.Content = null;

        foreach (var cardFile in cardFiles)
        {
            var cardData = JObject.Parse(File.ReadAllText(cardFile));
            
            var cardIdentifiers = new Card.CardIdentifiers
            {
                Name = cardData["Name"]?.ToString(),
                OracleID = cardData["OracleID"]?.ToString(),
                Set = cardData["Set"]?.ToString(),
                SetName = cardData["SetName"]?.ToString(),
                ManaCost = cardData["ManaCost"]?.ToString(),
                TypeLine = cardData["TypeLine"]?.ToString(),
                OracleText = cardData["OracleText"]?.ToString(),
                Artist = cardData["Artist"]?.ToString(),
                Prices = cardData["Prices"]?.ToObject<Card.Prices>(),
                PurchaseUris = cardData["PurchaseUris"]?.ToObject<Card.PurchaseUris>(),
                Legalities = cardData["Legalities"]?.ToObject<Card.Legalities>()
            };

            var frontUrl = cardData["Url"]?.ToString();
            var backUrl = cardData["BackUrl"]?.ToString();

            Bitmap? frontBitmap = null;
            Bitmap? backBitmap = null;

            if (!string.IsNullOrEmpty(frontUrl))
            {
                frontBitmap = await DownloadImageAsync(frontUrl);
            }

            if (!string.IsNullOrEmpty(backUrl))
            {
                backBitmap = await DownloadImageAsync(backUrl);
            }

            if (frontBitmap != null)
            {
                var cardControl = CardControl.CreateCardControl(
                    frontBitmap,
                    frontUrl ?? "",
                    backBitmap,
                    backUrl,
                    (s, e) => ShowCardInfoWindow(cardIdentifiers.OracleID ?? "", null),
                    cardIdentifiers,
                    true
                );

                wrapPanel.Children.Add(cardControl);
            }
        }

        ImageDisplayContent.Content = wrapPanel;
    }

    // I wonder if there is a simpler way to refresh the screen instead of this method
    public async void RefreshDeckDisplay()
    {
        if (ImageDisplayContent.Tag is string selectedDeck)
        {
            string selectedDeckPath = Path.Combine(Environment.CurrentDirectory, "Decks", selectedDeck);
            var cardFiles = Directory.GetFiles(selectedDeckPath, "*.json");

            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            foreach (var cardFile in cardFiles)
            {
                var cardData = JObject.Parse(File.ReadAllText(cardFile));

                var cardIdentifiers = cardData.ToObject<Card.CardIdentifiers>();
                if (cardIdentifiers == null)
                {
                    Console.WriteLine($"Failed to parse card data from {cardFile}");
                    continue;
                }

                var frontUrl = cardData["Url"]?.ToString();
                var backUrl = cardData["BackUrl"]?.ToString();

                Bitmap? frontBitmap = null;
                Bitmap? backBitmap = null;

                if (!string.IsNullOrEmpty(frontUrl) && Uri.TryCreate(frontUrl, UriKind.Absolute, out _))
                {
                    frontBitmap = await DownloadImageAsync(frontUrl);
                }

                if (!string.IsNullOrEmpty(backUrl) && Uri.TryCreate(backUrl, UriKind.Absolute, out _))
                {
                    backBitmap = await DownloadImageAsync(backUrl);
                }

                if (frontBitmap != null)
                {
                    var cardControl = CardControl.CreateCardControl(
                        frontBitmap,
                        frontUrl ?? "",
                        backBitmap,
                        backUrl,
                        (s, e) => ShowCardInfoWindow(cardIdentifiers.OracleID ?? "", null),
                        cardIdentifiers,
                        true
                    );

                    wrapPanel.Children.Add(cardControl);
                }
            }

            ImageDisplayContent.Content = wrapPanel;
        }
    }

    public void ApplyFilters(Dictionary<string, object> filterSettings)
    {
        var cachedCards = OracleCardCache.GetCachedCards();

        var filteredCards = cachedCards.Where(card =>
        {
            foreach (var filter in filterSettings)
            {
                switch (filter.Key)
                {
                    case "CardType" when !string.IsNullOrEmpty(filter.Value?.ToString()):
                        var requiredTypes = filter.Value.ToString()!
                            .Split(',')
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrEmpty(t));
                        if (!requiredTypes.All(type => 
                            card.TypeLine?.Contains(type, StringComparison.OrdinalIgnoreCase) == true))
                        {
                            return false;
                        }
                        break;

                    case "Keywords" when !string.IsNullOrEmpty(filter.Value?.ToString()):
                        var requiredKeywords = filter.Value.ToString()!
                            .Split(',')
                            .Select(k => k.Trim())
                            .Where(k => !string.IsNullOrEmpty(k));
                        if (!requiredKeywords.All(keyword =>
                            card.Keywords.Any(cardKeyword => 
                                cardKeyword.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
                        {
                            return false;
                        }
                        break;

                    case "Cmc" when filter.Value is float cmcValue:
                        if (!card.CMC.HasValue || card.CMC != cmcValue)
                        {
                            return false;
                        }
                        break;

                    case "Rarity" when !string.IsNullOrEmpty(filter.Value?.ToString()):
                        if (!string.Equals(card.Rarity, filter.Value.ToString(), 
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                        break;

                    case "Set" when !string.IsNullOrEmpty(filter.Value?.ToString()):
                        if (!string.Equals(card.SetName, filter.Value.ToString(), 
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                        break;

                    case "Colors" when filter.Value is List<string> selectedColors:
                        if (filterSettings.TryGetValue("FilterMode", out var filterMode) && 
                            filterMode is int modeIndex)
                        {
                            if (!ApplyColorFilter(card.Colors, selectedColors, modeIndex))
                            {
                                return false;
                            }
                        }
                        break;

                    case "Artist" when !string.IsNullOrEmpty(filter.Value?.ToString()):
                        if (!card.Artist?.Contains(filter.Value.ToString(), 
                            StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return false;
                        }
                        break;

                    case "Legalities" when filter.Value is List<string> selectedLegalities:
                        if (selectedLegalities.Any())
                        {
                            foreach (var format in selectedLegalities)
                            {
                                var propertyInfo = card.Legalities?.GetType().GetProperty(format);
                                var legalityValue = propertyInfo?.GetValue(card.Legalities)?.ToString();
                                Console.WriteLine($"Card: {card.Name}, Format: {format}, Value: {legalityValue}"); // Debug line
                                if (legalityValue != "legal")
                                {
                                    return false;
                                }
                            }
                        }
                        break;
                }
            }
            return true;
        }).ToList();

        DisplaySetImages(filteredCards);
    }

    private bool ApplyColorFilter(List<string> cardColors, List<string> selectedColors, int filterMode)
    {
        switch (filterMode)
        {
            case 0: // Exactly these colors
                return cardColors.Count == selectedColors.Count &&
                       selectedColors.All(color => cardColors.Contains(color));

            case 1: // Including these colors
                return selectedColors.All(color => cardColors.Contains(color));

            case 2: // Not these colors
                return !selectedColors.Any(color => cardColors.Contains(color));

            default:
                return true;
        }
    }

    private void OnImageClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Clicked");
    }

}