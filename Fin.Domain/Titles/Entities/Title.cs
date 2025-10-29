using System.Collections.ObjectModel;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Wallets.Entities;

namespace Fin.Domain.Titles.Entities;

public class Title: IAuditedTenantEntity
{
    public decimal Value { get; set; }
    public TitleType Type { get; set; }
    
    public string Description { get; set; }
    public decimal PreviousBalance { get; set; }
    public DateTime Date { get; set; }
    public Guid WalletId { get; set; }
    
    
    public decimal ResultingBalance => PreviousBalance + EffectiveValue;
    public decimal EffectiveValue => (Value * (Type == TitleType.Expense ? -1 : 1));
    
    public virtual Wallet Wallet { get; set; }
    public ICollection<TitleCategory> TitleCategories { get; set; } = [];
    public ICollection<TitleTitleCategory> TitleTitleCategories { get; set; } = [];
    
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }

    public Title()
    {
    }

    public 
        Title(TitleInput input, decimal previousBalance)
    {
        Id = Guid.NewGuid();
        
        UpdateBasicProperties(input, previousBalance);

        TitleTitleCategories = new Collection<TitleTitleCategory>(
            input.TitleCategoriesIds
                .Distinct()
                .Select(categoryId => new TitleTitleCategory(categoryId, Id))
                .ToList()
            );
    } 
    
    public List<TitleTitleCategory> UpdateAndReturnCategoriesToRemove(TitleInput input, decimal previousBalance)
    {
        UpdateBasicProperties(input, previousBalance);
        return SyncCategories(input.TitleCategoriesIds);
    }

    public bool MustReprocess(TitleInput input)
    {
        return input.Date != Date
               || input.Type != Type
               || input.Value != Value
               || input.WalletId != WalletId;
    }
    
    private void UpdateBasicProperties(TitleInput input, decimal previousBalance)
    {
        Value = input.Value;
        Type = input.Type;
        Description = input.Description.Trim();
        Date = input.Date;
        WalletId = input.WalletId;
        PreviousBalance = previousBalance;
    }
    
    private List<TitleTitleCategory> SyncCategories(List<Guid> newCategoryIds)
    {
        var updatedCategories = newCategoryIds.Select(userId => new TitleTitleCategory(userId, Id)).ToList();
        
        var categoriesToDelete = new List<TitleTitleCategory>();
        foreach (var currentDelivery in TitleTitleCategories)
        {
            var index = updatedCategories.FindIndex(c => c.TitleCategoryId == currentDelivery.TitleCategoryId);
            if (index != -1) continue;
            categoriesToDelete.Add(currentDelivery);
        }

        foreach (var currentDelivery in categoriesToDelete)
        {
            TitleTitleCategories.Remove(currentDelivery);
        }

        foreach (var updatedDelivery in updatedCategories)
        {
            var index = TitleTitleCategories.ToList().FindIndex(c => c.TitleCategoryId == updatedDelivery.TitleCategoryId);
            if (index != -1) continue;
            TitleTitleCategories.Add(updatedDelivery);
        }

        return categoriesToDelete;
    }
}