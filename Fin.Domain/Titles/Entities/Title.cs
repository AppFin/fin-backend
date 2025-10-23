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
    
    
    public decimal ResultingBalance => PreviousBalance + (Value * (Type == TitleType.Expense ? -1 : 1));
    
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

    public Title(TitleInput input, decimal previousBalance)
    {
        Id = Guid.NewGuid();
        
        Value = input.Value;
        Type = input.Type;
        Description = input.Description.Trim();
        Date = input.Date;
        WalletId = input.WalletId;
        
        PreviousBalance = previousBalance;

        TitleTitleCategories = new Collection<TitleTitleCategory>(
            input.TitleCategoriesIds
                .Distinct()
                .Select(categoryId => new TitleTitleCategory(categoryId, Id))
                .ToList()
            );
    } 
    
    public List<TitleTitleCategory> UpdateAndReturnToRemoveTitleCategories(TitleInput input, decimal previousBalance)
    {
        Value = input.Value;
        Type = input.Type;
        Description = input.Description.Trim();
        Date = input.Date;
        WalletId = input.WalletId;
        
        PreviousBalance = previousBalance;
        
        var categoriesToDelete = new List<TitleTitleCategory>();
        foreach (var titleTitleCategory in TitleTitleCategories)
        {
            var index = input.TitleCategoriesIds.FindIndex(ttcId => ttcId == titleTitleCategory.TitleCategoryId);
            if (index != -1) continue;
            categoriesToDelete.Add(titleTitleCategory);
        }

        foreach (var categoryToRemove in categoriesToDelete)
        {
            TitleTitleCategories.Remove(categoryToRemove);
        }

        foreach (var currentTitleCategoryId in input.TitleCategoriesIds)
        {
            var index = TitleTitleCategories.ToList().FindIndex(ttc => ttc.TitleCategoryId == currentTitleCategoryId);
            if (index != -1) continue;
            TitleTitleCategories.Add(new TitleTitleCategory(currentTitleCategoryId, Id));
        }

        return categoriesToDelete;
    } 
}