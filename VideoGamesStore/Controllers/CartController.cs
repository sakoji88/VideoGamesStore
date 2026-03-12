using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoGamesStore.Extensions;
using VideoGamesStore.Models;
using VideoGamesStore.ViewModels;

namespace VideoGamesStore.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "Cart";
    private readonly VideoGamesStoreContext _context;

    public CartController(VideoGamesStoreContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await BuildCartViewModelAsync();
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int gameId)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.IsActive);
        if (game == null)
        {
            return NotFound();
        }

        var cartItems = GetCartItems();
        var existing = cartItems.FirstOrDefault(i => i.GameId == gameId);

        if (existing == null)
        {
            cartItems.Add(new CartSessionItem { GameId = gameId, Quantity = 1 });
        }
        else if (existing.Quantity < game.Stock)
        {
            existing.Quantity++;
        }

        SaveCartItems(cartItems);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateQuantity(int gameId, int quantity)
    {
        var cartItems = GetCartItems();
        var item = cartItems.FirstOrDefault(i => i.GameId == gameId);

        if (item == null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (quantity <= 0)
        {
            cartItems.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
        }

        SaveCartItems(cartItems);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int gameId)
    {
        var cartItems = GetCartItems();
        var item = cartItems.FirstOrDefault(i => i.GameId == gameId);

        if (item != null)
        {
            cartItems.Remove(item);
            SaveCartItems(cartItems);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartSessionKey);
        return RedirectToAction(nameof(Index));
    }

    private async Task<CartViewModel> BuildCartViewModelAsync()
    {
        var cartItems = GetCartItems();
        var gameIds = cartItems.Select(c => c.GameId).ToList();

        var games = await _context.Games
            .Where(g => gameIds.Contains(g.Id) && g.IsActive)
            .ToDictionaryAsync(g => g.Id);

        var items = new List<CartItemViewModel>();

        foreach (var cartItem in cartItems)
        {
            if (!games.TryGetValue(cartItem.GameId, out var game))
            {
                continue;
            }

            var quantity = Math.Min(cartItem.Quantity, game.Stock);
            if (quantity <= 0)
            {
                continue;
            }

            items.Add(new CartItemViewModel
            {
                GameId = game.Id,
                Title = game.Title,
                Price = game.Price,
                Quantity = quantity,
                Stock = game.Stock,
                CoverImageUrl = game.CoverImageUrl
            });
        }

        return new CartViewModel { Items = items };
    }

    private List<CartSessionItem> GetCartItems()
    {
        return HttpContext.Session.GetObject<List<CartSessionItem>>(CartSessionKey) ?? new List<CartSessionItem>();
    }

    private void SaveCartItems(List<CartSessionItem> items)
    {
        HttpContext.Session.SetObject(CartSessionKey, items);
    }
}
