using Fin.Domain.CreditCards.Entities;
using Fin.Domain.CreditCharges.Entities;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.People.Entities;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.TitleCategories;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Users.Entities;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fin.Application.Tenants.Services;

public interface ITenantCleanupService
{
    /// Deletes every tenant that has no admin user, along with all of its data.
    /// Used by the portfolio-demo recurring job so that visitor-created data does not pile up.
    Task<int> DeleteNonAdminTenantsAsync(CancellationToken cancellationToken = default);
}

public class TenantCleanupService(
    IRepository<Tenant> tenantRepo,
    IRepository<TenantUser> tenantUserRepo,
    IRepository<User> userRepo,
    IRepository<UserCredential> credentialRepo,
    IRepository<UserNotificationSettings> notificationSettingsRepo,
    IRepository<UserRememberUseSetting> rememberUseSettingRepo,
    IRepository<Wallet> walletRepo,
    IRepository<Title> titleRepo,
    IRepository<TitleCategory> titleCategoryRepo,
    IRepository<TitleTitleCategory> titleTitleCategoryRepo,
    IRepository<CreditCard> creditCardRepo,
    IRepository<CreditCharge> creditChargeRepo,
    IRepository<CreditChargeCategory> creditChargeCategoryRepo,
    IRepository<CardBilling> cardBillingRepo,
    IRepository<Installment> installmentRepo,
    IRepository<Person> personRepo,
    IRepository<CreditChargePerson> creditChargePersonRepo,
    IRepository<TitlePerson> titlePersonRepo,
    IUnitOfWork unitOfWork,
    ILogger<TenantCleanupService> logger
) : ITenantCleanupService, IAutoTransient
{
    public async Task<int> DeleteNonAdminTenantsAsync(CancellationToken cancellationToken = default)
    {
        var nonAdminTenantIds = await tenantRepo
            .Where(t => !t.Users.Any(u => u.IsAdmin))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in nonAdminTenantIds)
        {
            try
            {
                await DeleteTenantAsync(tenantId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to clean up non-admin tenant {TenantId}", tenantId);
            }
        }

        logger.LogInformation("Demo cleanup removed {Count} non-admin tenant(s)", nonAdminTenantIds.Count);
        return nonAdminTenantIds.Count;
    }

    private async Task DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var creditChargeIds = await creditChargeRepo.Where(c => c.TenantId == tenantId)
            .Select(c => c.Id).ToListAsync(cancellationToken);
        var titleIds = await titleRepo.Where(t => t.TenantId == tenantId)
            .Select(t => t.Id).ToListAsync(cancellationToken);

        var creditChargeCategories = await creditChargeCategoryRepo
            .Where(c => creditChargeIds.Contains(c.CreditChargeId)).ToListAsync(cancellationToken);
        var titleTitleCategories = await titleTitleCategoryRepo
            .Where(t => titleIds.Contains(t.TitleId)).ToListAsync(cancellationToken);
        var creditChargePeople = await creditChargePersonRepo
            .Where(c => creditChargeIds.Contains(c.CreditChargeId)).ToListAsync(cancellationToken);
        var titlePeople = await titlePersonRepo
            .Where(t => titleIds.Contains(t.TitleId)).ToListAsync(cancellationToken);

        var installments = await installmentRepo.Where(i => i.TenantId == tenantId).ToListAsync(cancellationToken);
        var cardBillings = await cardBillingRepo.Where(c => c.TenantId == tenantId).ToListAsync(cancellationToken);
        var creditCharges = await creditChargeRepo.Where(c => c.TenantId == tenantId).ToListAsync(cancellationToken);
        var creditCards = await creditCardRepo.Where(c => c.TenantId == tenantId).ToListAsync(cancellationToken);
        var titles = await titleRepo.Where(t => t.TenantId == tenantId).ToListAsync(cancellationToken);
        var people = await personRepo.Where(p => p.TenantId == tenantId).ToListAsync(cancellationToken);
        var titleCategories = await titleCategoryRepo.Where(t => t.TenantId == tenantId).ToListAsync(cancellationToken);
        var wallets = await walletRepo.Where(w => w.TenantId == tenantId).ToListAsync(cancellationToken);

        var notificationSettings = await notificationSettingsRepo.Where(n => n.TenantId == tenantId).ToListAsync(cancellationToken);
        var rememberSettings = await rememberUseSettingRepo.Where(r => r.TenantId == tenantId).ToListAsync(cancellationToken);
        var tenantUsers = await tenantUserRepo.Where(t => t.TenantId == tenantId).ToListAsync(cancellationToken);
        var tenant = await tenantRepo.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        // Users left with no other tenant are removed too, so no PII lingers after the tenant is gone.
        var userIdsToDelete = new List<Guid>();
        foreach (var userId in tenantUsers.Select(t => t.UserId).Distinct())
        {
            var hasOtherTenant = await tenantUserRepo
                .AnyAsync(t => t.UserId == userId && t.TenantId != tenantId, cancellationToken);
            if (!hasOtherTenant)
                userIdsToDelete.Add(userId);
        }

        var credentialsToDelete = await credentialRepo
            .Where(c => userIdsToDelete.Contains(c.UserId)).ToListAsync(cancellationToken);
        var usersToDelete = await userRepo
            .Where(u => userIdsToDelete.Contains(u.Id)).ToListAsync(cancellationToken);

        await using var scope = await unitOfWork.BeginTransactionAsync(cancellationToken);

        foreach (var e in creditChargeCategories) await creditChargeCategoryRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in titleTitleCategories) await titleTitleCategoryRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in creditChargePeople) await creditChargePersonRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in titlePeople) await titlePersonRepo.DeleteAsync(e, cancellationToken);

        foreach (var e in installments) await installmentRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in cardBillings) await cardBillingRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in creditCharges) await creditChargeRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in creditCards) await creditCardRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in titles) await titleRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in people) await personRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in titleCategories) await titleCategoryRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in wallets) await walletRepo.DeleteAsync(e, cancellationToken);

        foreach (var e in notificationSettings) await notificationSettingsRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in rememberSettings) await rememberUseSettingRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in tenantUsers) await tenantUserRepo.DeleteAsync(e, cancellationToken);
        if (tenant != null) await tenantRepo.DeleteAsync(tenant, cancellationToken);

        foreach (var e in credentialsToDelete) await credentialRepo.DeleteAsync(e, cancellationToken);
        foreach (var e in usersToDelete) await userRepo.DeleteAsync(e, cancellationToken);

        await scope.CompleteAsync(cancellationToken);
    }
}
