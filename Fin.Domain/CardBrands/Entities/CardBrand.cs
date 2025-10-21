using Fin.Domain.CardBrands.Dtos;
using Fin.Domain.CreditCards.Entities;
using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.CardBrands.Entities;

public class CardBrand: IAuditedEntity
{
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }

    public virtual ICollection<CreditCard> CreditCards { get; set; }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public CardBrand()
    {
    }

    public CardBrand(CardBrandInput input)
    {
        Name = input.Name;
        Icon = input.Icon;
        Color = input.Color;
    }

    public void Update(CardBrandInput input)
    {
        Name = input.Name;
        Icon = input.Icon;
        Color = input.Color;
        
    }
}
