namespace VideoGamesStore.ViewModels;

public class CartItemViewModel
{
    public int GameId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    public string? CoverImageUrl { get; set; }

    public decimal Total => Price * Quantity;
}
