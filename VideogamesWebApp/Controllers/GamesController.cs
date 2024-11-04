﻿using GamesDataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideogamesWebApp.Models;

public class GamesController : Controller
{
    private readonly DatabaseContext _dbContext;

    public GamesController(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IActionResult Index(string searchQuery)
    {
        var userId = GetUserId();
        var username = GetUsername();


        var transactionsQuery = from transaction in _dbContext.GameTransactions
                                join game in _dbContext.Games on transaction.GameId equals game.GameId
                                join store in _dbContext.Stores on transaction.StoreId equals store.StoreId
                                join platform in _dbContext.Platforms on transaction.PlatformId equals platform.PlatformId
                                join launcher in _dbContext.Launchers on transaction.LauncherId equals launcher.LauncherId
                                where transaction.UserId == userId
                                select new GameTransactionsViewModel
                                {
                                    GameId = transaction.GameId,
                                    GameName = game.GameName,
                                    PurchaseDate = transaction.PurchaseDate,
                                    Price = transaction.Price,
                                    IsVirtual = transaction.IsVirtual,
                                    StoreName = store.StoreName,
                                    PlatformName = platform.PlatformName,
                                    LauncherName = launcher.LauncherName,
                                    MainGameId = game.MainGameId
                                };

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            searchQuery = searchQuery.ToLower();
            transactionsQuery = transactionsQuery.Where(t =>
                t.GameName.ToLower().Contains(searchQuery) ||
                t.StoreName.ToLower().Contains(searchQuery) ||
                t.PlatformName.ToLower().Contains(searchQuery) ||
                t.LauncherName.ToLower().Contains(searchQuery) ||
                (t.MainGameId != null && t.MainGameId.ToLower().Contains(searchQuery))
            );
        }

        var transactions = transactionsQuery.ToList();
        ViewData["Username"] = username;


        //if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //{
        //    return Json(transactions);
        //}

        ViewData["searchQuery"] = searchQuery;

        ViewData["AvailableGames"] = _dbContext.Games
                                      .Select(g => new { g.GameId, g.GameName })
                                      .ToList();

        ViewData["AvailableStores"] = _dbContext.Stores
                                         .Select(s => new { s.StoreId, s.StoreName })
                                         .ToList();
        ViewData["AvailablePlatforms"] = _dbContext.Platforms
                                                   .Select(p => new { p.PlatformId, p.PlatformName })
                                                   .ToList();
        ViewData["AvailableLaunchers"] = _dbContext.Launchers
                                                   .Select(l => new { l.LauncherId, l.LauncherName })
                                                   .ToList();

        return View("~/Views/Home/Index.cshtml", transactions);
    }

    [HttpPost]
    public IActionResult BuyGame(GamePurchaseViewModel model)
    {
        if (ModelState.IsValid)
        {
            var newTransaction = new GameTransactions
            {
                GameId = model.GameId,
                StoreId = model.StoreId,
                PlatformId = model.PlatformId,
                LauncherId = model.LauncherId,
                PurchaseDate = model.PurchaseDate,
                Price = model.Price,
                IsVirtual = model.IsVirtual,
                UserId = GetUserId(),
                Notes = string.IsNullOrEmpty(model.Notes) ? null : model.Notes

            };

            _dbContext.GameTransactions.Add(newTransaction);
            _dbContext.SaveChanges();

            return RedirectToAction("Index");
        }


        ViewData["AvailableGames"] = _dbContext.Games.Select(g => new { g.GameId, g.GameName }).ToList();
        ViewData["AvailableStores"] = _dbContext.Stores.Select(s => new { s.StoreId, s.StoreName }).ToList();
        ViewData["AvailablePlatforms"] = _dbContext.Platforms.Select(p => new { p.PlatformId, p.PlatformName }).ToList();
        ViewData["AvailableLaunchers"] = _dbContext.Launchers.Select(l => new { l.LauncherId, l.LauncherName }).ToList();

        return View("Index", model);
    }



    public IActionResult ViewAllGames()
    {
        var username = GetUsername();

        var allGamesQuery = from game in _dbContext.Games
                            select new GameViewModel
                            {
                                GameId = game.GameId,
                                GameName = game.GameName,
                                GameDescription = game.GameDescription,
                                MainGameId = game.MainGameId
                            };

        var allGames = allGamesQuery.ToList();
        ViewData["Username"] = username;
        return View("~/Views/Home/ViewAllGames.cshtml", allGames);
    }

    private string GetUsername()
    {
        return HttpContext.Session.GetString("Username") ?? "Guest";
    }

    private int GetUserId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    [HttpPost]
    public async Task<IActionResult> AddGame(string gameName, string gameDescription, string mainGameId)
    {
        if (!string.IsNullOrWhiteSpace(gameName))
        {
            var game = new Game
            {
                GameName = gameName,
                GameDescription = gameDescription,
                MainGameId = string.IsNullOrWhiteSpace(mainGameId) ? "" : mainGameId
            };

            _dbContext.Games.Add(game);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("ViewAllGames");
        }

        return RedirectToAction("ViewAllGames");
    }

    [HttpPost]
    public IActionResult AddStore(string storeName, string storeDescription, string storeLink)
    {
        if (!string.IsNullOrWhiteSpace(storeName))
        {
            var newStore = new Stores
            {
                StoreName = storeName,
                StoreDescription = storeDescription,
                StoreLink = storeLink
            };

            _dbContext.Stores.Add(newStore);
            _dbContext.SaveChanges();
            TempData["SuccessMessage"] = "Store created successfully";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult AddPlatform(string platformName, string platformDescription, string platformLink)
    {
        if (!string.IsNullOrWhiteSpace(platformName))
        {
            var newPlatform = new Platforms
            {
                PlatformName = platformName,
                PlatformDescription = platformDescription,
            };

            _dbContext.Platforms.Add(newPlatform);
            _dbContext.SaveChanges();
            TempData["SuccessMessage"] = "Platform  created successfully";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult AddLauncher(string launcherName, string launcherDescription, string link)
    {
        if (!string.IsNullOrWhiteSpace(launcherName))
        {
            var newLauncher = new Launcher
            {
                LauncherName = launcherName,
                LauncherDescription = launcherDescription,
                Link = link
            };

            _dbContext.Launchers.Add(newLauncher);
            _dbContext.SaveChanges();
            TempData["SuccessMessage"] = "Launcher  created successfully";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteGame(int gameId)
    {
        var transaction = _dbContext.GameTransactions.FirstOrDefault(t => t.GameId == gameId && t.UserId == GetUserId());
        if (transaction != null)
        {
            _dbContext.GameTransactions.Remove(transaction);
            _dbContext.SaveChanges();
            TempData["SuccessMessage"] = "Game deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Game not found.";
        }

        return RedirectToAction("Index");
    }



    [HttpGet]
    public IActionResult SearchGames(string query)
    {
        var queryWords = query.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var games = _dbContext.Games
            .Where(g => queryWords.All(word => g.GameName.ToLower().Contains(word)))
            .AsEnumerable()
            .Where(g => queryWords.All(word =>
                g.GameName.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(gameName => gameName.StartsWith(word))))
            .Select(g => new { gameId = g.GameId, gameName = g.GameName })
            .Take(10)
            .ToList();

        return Json(games);
    }
    [HttpGet]
    public IActionResult SearchStores(string query)
    {
        var queryWords = query.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var stores = _dbContext.Stores
            .Where(s => queryWords.All(word => s.StoreName.ToLower().Contains(word)))
            .AsEnumerable()
            .Where(s => queryWords.All(word =>
                s.StoreName.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(storeName => storeName.StartsWith(word))))
            .Select(s => new { storeId = s.StoreId, storeName = s.StoreName })
            .Take(10)
            .ToList();

        return Json(stores);
    }

    [HttpGet]
    public IActionResult SearchPlatforms(string query)
    {
        var queryWords = query.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var platforms = _dbContext.Platforms
            .Where(p => queryWords.All(word => p.PlatformName.ToLower().Contains(word)))
            .AsEnumerable()
            .Where(p => queryWords.All(word =>
                p.PlatformName.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(platformName => platformName.StartsWith(word))))
            .Select(p => new { platformId = p.PlatformId, platformName = p.PlatformName })
            .Take(10)
            .ToList();

        return Json(platforms);
    }

    [HttpGet]
    public IActionResult SearchLaunchers(string query)
    {
        var queryWords = query.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var launchers = _dbContext.Launchers
            .Where(l => queryWords.All(word => l.LauncherName.ToLower().Contains(word)))
            .AsEnumerable()
            .Where(l => queryWords.All(word =>
                l.LauncherName.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(launcherName => launcherName.StartsWith(word))))
            .Select(l => new { launcherId = l.LauncherId, launcherName = l.LauncherName })
            .Take(10)
            .ToList();

        return Json(launchers);
    }

  
}

