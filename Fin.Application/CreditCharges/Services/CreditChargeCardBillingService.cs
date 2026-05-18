using Fin.Application.Titles.Services;
using Fin.Domain.CreditCards.Entities;
using Fin.Domain.CreditCharges.Entities;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Enums;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.CreditCharges.Services;

public interface ICreditChargeCardBillingService
{
    Task ReprocessCardBillingForCharge(
        CreditCharge charge, 
        CreditCard creditCard,
        bool autoSave = false,
        CancellationToken cancellationToken = default);
    
    Task ReprocessCardBillingAfterUpdate(
        CreditCharge charge,
        CreditCard creditCard,
        bool autoSave = false,
        CancellationToken cancellationToken = default);
    
    Task ReprocessCardBillingAfterDelete(
        CreditCharge charge,
        CreditCard creditCard,
        bool autoSave = false,
        CancellationToken cancellationToken = default);
}

public class CreditChargeCardBillingService(
    IRepository<Installment> installmentRepository,
    IRepository<CardBilling> cardBillingRepository,
    IRepository<CreditCard> creditCardRepository,
    ITitleService titleService
) : ICreditChargeCardBillingService, IAutoTransient
{
    public async Task ReprocessCardBillingForCharge(
        CreditCharge charge,
        CreditCard creditCard,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var installments = GenerateInstallments(charge);
        var cardBillings = new Dictionary<Guid, CardBilling>();
        
        foreach (var installment in installments)
        {
            var period = GetCardBillingPeriod(creditCard, installment.DueDate);
            var cardBilling = await FindOrCreateCardBilling(creditCard, period, cancellationToken);
            cardBillings[cardBilling.Id] = cardBilling;
            
            installment.SetCardBillingId(cardBilling.Id);
        }
        await installmentRepository.AddRangeAsync(installments, autoSave: false, cancellationToken);

        foreach (var cardBilling in cardBillings.Values)
        {
            await UpdateCardBillingPaymentTitle(cardBilling, autoSave: false, cancellationToken);
        }

        if (autoSave)
            await installmentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ReprocessCardBillingAfterUpdate(
        CreditCharge charge,
        CreditCard creditCard,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        if (charge.Installments == null || !charge.Installments.Any())
        {
            await ReprocessCardBillingForCharge(charge, creditCard, autoSave, cancellationToken);
            return;
        }

        var existingInstallments = charge.Installments.ToList();
        var affectedCardBillingIds = existingInstallments.Select(i => i.CardBillingId).Distinct().ToList();
        
        foreach (var installment in existingInstallments)
        {
            await installmentRepository.DeleteAsync(installment, autoSave: false, cancellationToken);
        }
        await ReprocessCardBillingForCharge(charge, creditCard, autoSave: false, cancellationToken);

        var cardBillings = await cardBillingRepository.AsNoTracking()
            .Where(cb => affectedCardBillingIds.Contains(cb.Id))
            .ToListAsync(cancellationToken);

        foreach (var cardBilling in cardBillings)
        {
            await UpdateCardBillingPaymentTitle(cardBilling, autoSave: false, cancellationToken);
        }

        if (autoSave)
            await installmentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ReprocessCardBillingAfterDelete(
        CreditCharge charge,
        CreditCard creditCard,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var installments = charge.Installments.ToList();
        var affectedCardBillingIds = installments.Select(i => i.CardBillingId).Distinct().ToList();
        
        foreach (var installment in installments)
        {
            await installmentRepository.DeleteAsync(installment, autoSave: false, cancellationToken);
        }

        var cardBillings = await cardBillingRepository.AsNoTracking()
            .Where(cb => affectedCardBillingIds.Contains(cb.Id))
            .ToListAsync(cancellationToken);

        foreach (var cardBilling in cardBillings)
        {
            await UpdateCardBillingPaymentTitle(cardBilling, autoSave: false, cancellationToken);
        }

        if (autoSave)
            await installmentRepository.SaveChangesAsync(cancellationToken);
    }

    private List<Installment> GenerateInstallments(CreditCharge charge)
    {
        var installments = new List<Installment>();
        var installmentValue = Math.Round(charge.Value / charge.NumberOfInstallments, 2);
        var remainder = charge.Value - (installmentValue * (charge.NumberOfInstallments - 1));

        var chargeDate = charge.Date;

        for (byte i = 0; i < charge.NumberOfInstallments; i++)
        {
            var dueDate = CalculateInstallmentDueDate(chargeDate, i);
            var value = i == charge.NumberOfInstallments - 1 ? remainder : installmentValue;

            var installment = new Installment(value, dueDate, (byte)(i + 1), charge.Id);
            installments.Add(installment);
        }

        return installments;
    }

    private DateTime CalculateInstallmentDueDate(DateTime chargeDate, byte installmentIndex)
    {
        var dueMonth = chargeDate.AddMonths(installmentIndex);
        
        var targetDay = chargeDate.Day;
        var maxDayInMonth = DateTime.DaysInMonth(dueMonth.Year, dueMonth.Month);
        var finalDay = Math.Min(targetDay, maxDayInMonth);

        var dueDate = new DateTime(dueMonth.Year, dueMonth.Month, finalDay);
        
        return dueDate;
    }

    private (DateOnly PeriodStart, DateOnly PeriodEnd) GetCardBillingPeriod(CreditCard creditCard, DateTime installmentDueDate)
    {
        var closingDay = creditCard.ClosingDay;
        var currentYear = installmentDueDate.Year;
        var currentMonth = installmentDueDate.Month;
        var currentDay = installmentDueDate.Day;

        if (currentDay >= closingDay)
        {
            var periodStart = new DateOnly(currentYear, currentMonth, Math.Min(closingDay, DateTime.DaysInMonth(currentYear, currentMonth)));
            
            var nextMonthDate = currentMonth == 12 
                ? new DateTime(currentYear + 1, 1, 1) 
                : new DateTime(currentYear, currentMonth + 1, 1);
            
            var periodEnd = new DateOnly(
                nextMonthDate.Year,
                nextMonthDate.Month,
                Math.Min(closingDay, DateTime.DaysInMonth(nextMonthDate.Year, nextMonthDate.Month))
            );

            return (periodStart, periodEnd);
        }
        else
        {
            var previousMonthDate = currentMonth == 1
                ? new DateTime(currentYear - 1, 12, 1)
                : new DateTime(currentYear, currentMonth - 1, 1);

            var periodStart = new DateOnly(
                previousMonthDate.Year,
                previousMonthDate.Month,
                Math.Min(closingDay, DateTime.DaysInMonth(previousMonthDate.Year, previousMonthDate.Month))
            );

            var periodEnd = new DateOnly(currentYear, currentMonth, Math.Min(closingDay, DateTime.DaysInMonth(currentYear, currentMonth)));

            return (periodStart, periodEnd);
        }
    }

    private async Task<CardBilling> FindOrCreateCardBilling(
        CreditCard creditCard,
        (DateOnly PeriodStart, DateOnly PeriodEnd) period,
        CancellationToken cancellationToken)
    {
        var existingBilling = await cardBillingRepository.AsNoTracking()
            .Where(cb => cb.CreditCardId == creditCard.Id &&
                         cb.PeriodStart == period.PeriodStart &&
                         cb.PeriodEnd == period.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingBilling != null)
            return existingBilling;

        var paymentDate = period.PeriodEnd.ToDateTime(TimeOnly.MinValue).AddDays(creditCard.DueDay - creditCard.ClosingDay);
        var newBilling = new CardBilling(0, creditCard.Id, Guid.Empty, paymentDate, period.PeriodStart, period.PeriodEnd);

        await cardBillingRepository.AddAsync(newBilling, autoSave: false, cancellationToken);
        return newBilling;
    }

    private async Task UpdateCardBillingPaymentTitle(
        CardBilling cardBilling,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var totalValue = await installmentRepository.AsNoTracking()
            .Where(i => i.CardBillingId == cardBilling.Id)
            .SumAsync(i => i.Value, cancellationToken);

        cardBilling.UpdateValue(totalValue);

        if (cardBilling.PaymentTitleId == Guid.Empty)
        {
            var creditCard = await creditCardRepository.FirstAsync(cc => cc.Id == cardBilling.CreditCardId, cancellationToken);
            
            var titleInput = new TitleInput
            {
                Description = $"Credit Card {creditCard.Name} - {cardBilling.PeriodStart:MMM yyyy}",
                Value = totalValue,
                Type = TitleType.Expense,
                Date = cardBilling.PaymentDate,
                WalletId = creditCard.DebitWalletId
            };

            var titleCreation = await titleService.Create(titleInput, autoSave: false, cancellationToken);

            titleCreation.ThrowIfHasError();
            cardBilling.UpdatePaymentTitle(titleCreation.Data.Id);
        }
        else
        {
            var title = await titleService.Get(cardBilling.PaymentTitleId, cancellationToken);
            var titleUpdate = await titleService.Update(
                cardBilling.PaymentTitleId,
                new TitleInput
                {
                    Description = title.Description,
                    Value = totalValue,
                    Type = title.Type,
                    Date = title.Date,
                    WalletId = title.WalletId
                },
                autoSave: false, cancellationToken
                );
            titleUpdate.ThrowIfHasError();
        }

        await cardBillingRepository.UpdateAsync(cardBilling, autoSave, cancellationToken);
    }
}








