using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TCGHelper4
{
    public partial class AddCardQuantityWindow : Window
    {
        public int SelectedQuantity { get; private set; }

        public AddCardQuantityWindow(Card.CardIdentifiers cardIdentifiers, string deckName)
        {
            InitializeComponent();
        }

        private void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            SelectedQuantity = (int)QuantityUpDown.Value;
            Close(true);
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
} 