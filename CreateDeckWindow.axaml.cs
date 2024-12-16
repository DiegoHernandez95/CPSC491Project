using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TCGHelper4;

public partial class CreateDeckWindow : Window
{
    public CreateDeckWindow()
    {
        InitializeComponent();
    }

    public void SubmitButton_Click(object? sender, RoutedEventArgs e)
    {
        string deckName = DeckNameTextBox.Text?.Trim();

        if (string.IsNullOrEmpty(deckName))
        {
            Console.WriteLine("Deck name cannot be empty.");
            return;
        }

        try
        {
            string decksFolder = Path.Combine(Environment.CurrentDirectory, "Decks");

            if (!Directory.Exists(decksFolder))
            {
                Directory.CreateDirectory(decksFolder);
            }
            
            string newDeckFolder = Path.Combine(decksFolder, deckName);
            Directory.CreateDirectory(newDeckFolder);

            Console.WriteLine($"Deck folder '{deckName}' created successfully.");
            this.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}