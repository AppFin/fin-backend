using Fin.Domain.Menus.Entities;
using Fin.Domain.Menus.Enums;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.Seeders.interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Seeders.Seeders;

public class DefaultMenusSeeder(
    IRepository<Menu> menusRepository,
    ILogger<DefaultMenusSeeder> logger
) : ISeeder
{
    public async Task SeedAsync()
    {
        logger.LogInformation("Seeding default menus");

        var defaultMenus = new List<Menu>
        {
            new()
            {
                Id = Guid.Parse("E517345A-837D-42B8-8281-FD8DC32F9150"),
                FrontRoute = "/wallets",
                Name = "finCore.features.wallet.title",
                Color = "#fdc570",
                Icon = "wallet",
                OnlyForAdmin = false,
                Position = MenuPosition.LeftTop,
                KeyWords = "wallets, carteiras, conta, account, billetera, cuenta"
            },
            new()
            {
                Id = Guid.Parse("0199b289-82a7-7069-9230-05250b55fd47"),
                FrontRoute = "/title-categories",
                Name = "finCore.features.titleCategory.title",
                Color = "#fdc570",
                Icon = "icons",
                OnlyForAdmin = false,
                Position = MenuPosition.LeftTop,
                KeyWords = "title category, categória do título, categoria"
            },
            new()
            {
                Id = Guid.Parse("01994133-6669-7fcd-b6db-19a9b0c06f20"),
                FrontRoute = "/admin/menus",
                Name = "finCore.features.menus.title",
                Color = "#ff6666",
                Icon = "list",
                OnlyForAdmin = true,
                Position = MenuPosition.LeftTop,
                KeyWords = "Menu"
            },
            new()
            {
                Id = Guid.Parse("01994133-6669-7fcd-b6db-19a9b0c06f21"),
                FrontRoute = "/admin/financial-institutions",
                Name = "finCore.features.financialInstitutions.title",
                Color = "#4CAF50",
                Icon = "building-columns",
                OnlyForAdmin = true,
                Position = MenuPosition.LeftTop,
                KeyWords = "Financial Institution"
            },
            new()
            {
                Id = Guid.Parse("01999256-1baa-76bb-be49-6e209249c827"),
                FrontRoute = "/admin/notifications",
                Name = "finCore.features.notifications.title",
                Color = "#ff6666",
                Icon = "comment",
                OnlyForAdmin = true,
                Position = MenuPosition.LeftTop,
                KeyWords = "notifications, notificação, notificações"
            },
            new()
            {
                Id = Guid.Parse("7826C06C-7F7D-4D92-BAFD-68B5D5F247A9"),
                FrontRoute = "/admin/card-brand",
                Name = "finCore.features.cardBrand.title",
                Color = "#6d28d9",
                Icon = "credit-card",
                OnlyForAdmin = true,
                Position = MenuPosition.LeftTop,
                KeyWords = "card brand, bandeira, cartao"
            },
            new()
            {
                Id = Guid.Parse("090183AC-2FBC-4DCE-BA22-CDD46B2C7494"),
                FrontRoute = "/credit-cards",
                Name = "finCore.features.creditCard.title",
                Color = "#6d28d9",
                Icon = "credit-card",
                OnlyForAdmin = false,
                Position = MenuPosition.LeftTop,
                KeyWords = "credit card, cartao de crédito, cartão de credito, cartao"
            },
            new()
            {
                Id = Guid.Parse("019a27f7-c052-7e62-b344-2112b0737691"),
                FrontRoute = "/titles",
                Name = "finCore.features.title.title",
                Color = "#6d28d9",
                Icon = "coins",
                OnlyForAdmin = false,
                Position = MenuPosition.LeftTop,
                KeyWords = "titles, títulos, lançamentos, gostos, recebidos"
            },
            new()
            {
                Id = Guid.Parse("019aa9aa-55c4-72e5-931e-eb9a973670c8"),
                FrontRoute = "/people",
                Name = "finCore.features.person.title",
                Color = "#fdc570",
                Icon = "user",
                OnlyForAdmin = false,
                Position = MenuPosition.LeftTop,
                KeyWords = "person, people, pessoas"
            }
        };
        var defaultMenusIds = defaultMenus.Select(x => x.Id).ToList();
        var menusIdsAlreadyCreated = await menusRepository.AsNoTracking()
            .Where(x => defaultMenusIds.Contains(x.Id))
            .Select(x => x.Id).ToListAsync();
        var menusToCreate = defaultMenus.Where(x => !menusIdsAlreadyCreated.Contains(x.Id)).ToList();

        if (menusToCreate.Any())
        {
            await menusRepository.AddRangeAsync(menusToCreate, true);
        }
        
        logger.LogInformation("Default menus created");
    }
}