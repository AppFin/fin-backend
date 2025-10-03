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
                Id = Guid.Parse("01999256-1baa-76bb-be49-6e209249c827"),
                FrontRoute = "/admin/notifications",
                Name = "finCore.features.notifications.title",
                Color = "#ff6666",
                Icon = "comment",
                OnlyForAdmin = true,
                Position = MenuPosition.LeftTop,
                KeyWords = "notifications, notificação, notificações"
            }
        };
        var defaultMenusIds = defaultMenus.Select(x => x.Id).ToList();
        var menusIdsAlreadyCreated = await menusRepository.Query(false)
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