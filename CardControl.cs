using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TCGHelper4;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Avalonia.Controls.ApplicationLifetimes;


// there are certain functions in main that create their own card controls
// in the future try to keep all of that in this section that way its not as ugly looking
public static class CardControl
{
    public static Button CreateCardControl(Bitmap frontBitmap, string frontUrl, Bitmap? backBitmap, 
    string? backUrl, EventHandler<RoutedEventArgs> onImageClick, Card.CardIdentifiers cardIdentifiers, bool showDeleteButton = false)
    {
        var image = new Image
        {
            Source = frontBitmap,
            Width = 300,
            Height = 420,
            Stretch = Stretch.UniformToFill
        };

        var grid = new Grid
        {
            Width = 300,
            Height = 420,
            Margin = new Thickness(5)
        };

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });

        var overlay = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10,
            IsVisible = false
        };

        var addButton = new Button
        {
            Content = "  +  ",
            Padding = new Thickness(5),
            Background = Brushes.LightGreen,
            BorderBrush = Brushes.Transparent,
            Margin = new Thickness(0, 25, 0, 0)
        };

        addButton.Click += async (s, e) =>
        {
            e.Handled = true; 
            string deckFolder = Path.Combine(Environment.CurrentDirectory, "Decks");
            if (!Directory.Exists(deckFolder))
            {
                Console.WriteLine("No decks exist. Create a deck first.");
                return;
            }

            var viewDecksWindow = new ViewDecksWindow();
            string? selectedDeck = null;

            viewDecksWindow.DeckSelected += deck =>
            {
                selectedDeck = deck;
                viewDecksWindow.Close();
            };

            viewDecksWindow.Show();

            while (selectedDeck == null)
            {
                await Task.Delay(100); 
            }

            var quantityDialog = new AddCardQuantityWindow(cardIdentifiers, selectedDeck);
            var result = await quantityDialog.ShowDialog<bool>((Window)TopLevel.GetTopLevel(s as Visual)!);
            
            if (result)
            {
                // Hacky fix: BackUrl replaces front with back because for some reason it saves the back urls
                // with the front urls
                string fixedBackUrl = backUrl?.Replace("front", "back");

                string cardFilePath = Path.Combine(deckFolder, selectedDeck, $"{frontUrl.GetHashCode()}.json");

                var cardData = new
                {
                    Name = cardIdentifiers.Name ?? "Unknown Card",
                    Url = frontUrl,
                    BackUrl = fixedBackUrl,
                    cardIdentifiers.Prices,
                    cardIdentifiers.Set,
                    cardIdentifiers.SetName,
                    cardIdentifiers.CollectorNumber,
                    OracleID = cardIdentifiers.OracleID,
                    cardIdentifiers.PurchaseUris,
                    cardIdentifiers.ManaCost,
                    cardIdentifiers.TypeLine,
                    cardIdentifiers.OracleText,
                    cardIdentifiers.Artist,
                    cardIdentifiers.Legalities
                };

                string jsonContent = JsonConvert.SerializeObject(cardData, Formatting.Indented);
                await File.WriteAllTextAsync(cardFilePath, jsonContent);

                string deckFile = Path.Combine(deckFolder, selectedDeck, "deck.txt");
                string cardEntry = $"{quantityDialog.SelectedQuantity} {cardIdentifiers.Name} [{cardIdentifiers.Set?.ToUpper()}] {cardIdentifiers.CollectorNumber}";

                if (File.Exists(deckFile))
                {
                    var lines = await File.ReadAllLinesAsync(deckFile);
                    var existingCard = lines.FirstOrDefault(l => 
                        l.Contains($"{cardIdentifiers.Name} [{cardIdentifiers.Set?.ToUpper()}] {cardIdentifiers.CollectorNumber}"));

                    if (existingCard != null)
                    {
                        var currentQuantity = int.Parse(existingCard.Split(' ')[0]);
                        var newQuantity = currentQuantity + quantityDialog.SelectedQuantity;
                        
                        var updatedLines = lines.Select(l =>
                            l.Contains($"{cardIdentifiers.Name} [{cardIdentifiers.Set?.ToUpper()}] {cardIdentifiers.CollectorNumber}")
                                ? $"{newQuantity} {cardIdentifiers.Name} [{cardIdentifiers.Set?.ToUpper()}] {cardIdentifiers.CollectorNumber}"
                                : l);
                        await File.WriteAllLinesAsync(deckFile, updatedLines);
                    }
                    else
                    {
                        await File.AppendAllTextAsync(deckFile, cardEntry + Environment.NewLine);
                    }
                }
                else
                {
                    await File.WriteAllTextAsync(deckFile, cardEntry + Environment.NewLine);
                }
            }
        };


        var priceText = new TextBlock
        {
            Text = cardIdentifiers.Prices != null && !string.IsNullOrEmpty(cardIdentifiers.Prices.USD) ? $"${cardIdentifiers.Prices.USD}" : "Price N/A",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 25, 0, 0)
        };

        

        if (backBitmap != null)
        {
            var flipButton = new Button
            {
                Content = "Flip",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 25, 0, 0),
                Padding = new Thickness(5),
                Background = Brushes.LightGray,
                BorderBrush = Brushes.Transparent,
            };

            flipButton.Click += (s, e) =>
            {
                e.Handled = true; 
                if (image.Source == frontBitmap)
                {
                    image.Source = backBitmap;
                }
                else
                {
                    image.Source = frontBitmap;
                }
            };

            overlay.Children.Add(flipButton);
        }

        overlay.Children.Add(addButton);
        overlay.Children.Add(priceText);

        if (showDeleteButton)
        {
            var deleteButton = new Button
            {
                Content = "Delete",
                Padding = new Thickness(5),
                Background = Brushes.IndianRed,
                BorderBrush = Brushes.Transparent,
                Margin = new Thickness(0, 25, 0, 0)
            };

            deleteButton.Click += (s, e) =>
            {
                e.Handled = true; 
                string decksFolder = Path.Combine(Environment.CurrentDirectory, "Decks");
                var cardFiles = Directory.GetFiles(decksFolder, "*.json", SearchOption.AllDirectories);

                foreach (var cardFile in cardFiles)
                {
                    var cardData = JObject.Parse(File.ReadAllText(cardFile));
                    if (cardData["Url"]?.ToString() == frontUrl)
                    {
                        File.Delete(cardFile);
                        Console.WriteLine($"Card deleted: {frontUrl}");

                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                        {
                            if (desktop.MainWindow is MainWindow mainWindow)
                            {
                                mainWindow.RefreshDeckDisplay(); 
                            }
                        }
                        break;
                    }
                }
            };
            overlay.Children.Add(deleteButton);
        }

        Grid.SetRow(image, 0);
        Grid.SetRow(overlay, 1);
        grid.Children.Add(image);
        grid.Children.Add(overlay);

        grid.PointerMoved += (s, e) => overlay.IsVisible = true;
        grid.PointerExited += (s, e) => overlay.IsVisible = false;

        var buttonWrapper = new Button
        {
            Content = grid,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Padding = new Thickness(0),
            Margin = new Thickness(5),
            Tag = frontUrl
        };

        buttonWrapper.Click += (s, e) =>
        {
            onImageClick(s, e); 
        };

        return buttonWrapper;
    }

    }