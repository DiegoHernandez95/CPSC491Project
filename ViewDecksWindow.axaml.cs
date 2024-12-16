using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Linq;

namespace TCGHelper4
{
    public partial class ViewDecksWindow : Window
    {
        public event Action<string>? DeckSelected;

        public ViewDecksWindow()
        {
            InitializeComponent();
            LoadDecks();
        }

        private void LoadDecks()
        {
            string decksFolder = Path.Combine(Environment.CurrentDirectory, "Decks");
            if (!Directory.Exists(decksFolder))
            {
                Directory.CreateDirectory(decksFolder);
            }

            var deckFolders = Directory.GetDirectories(decksFolder).Select(Path.GetFileName).ToList();
            DecksListBox.ItemsSource = deckFolders;
        }

        private void OpenDeckButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DecksListBox.SelectedItem is string selectedDeck)
            {
                DeckSelected?.Invoke(selectedDeck);
                Close();
            }
        }

        private void DeleteDeckButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DecksListBox.SelectedItem is string selectedDeck)
            {
                try
                {
                    string deckPath = Path.Combine(Environment.CurrentDirectory, "Decks", selectedDeck);
                    if (Directory.Exists(deckPath))
                    {
                        Directory.Delete(deckPath, true);
                        Console.WriteLine($"Deleted deck: {selectedDeck}");
                        
                        LoadDecks();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting deck: {ex.Message}");
                }
            }
        }
    }
}
