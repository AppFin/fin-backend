
using Fin.Domain.CardBrands.Entities;

public class CardBrandOutput
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
   

    public CardBrandOutput()
    {
    }

    public CardBrandOutput(CardBrand input)
    {
        Id = input.Id;
        Name = input.Name;
        Icon = input.Icon;
        Color = input.Color;
    }
}