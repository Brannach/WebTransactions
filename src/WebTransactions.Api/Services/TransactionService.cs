using Microsoft.EntityFrameworkCore;
using WebTransactions.Api.Data;
using WebTransactions.Api.DTO;
using WebTransactions.Api.Models;

namespace WebTransactions.Api.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _dbContext;
    private readonly IExchangeRateService _exchangeRateService;

    public TransactionService(AppDbContext dbContext, IExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<Guid> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        Transaction transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            Amount = Math.Round(request.Amount, 2)
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }

    public async Task<TransactionResponse?> GetTransactionAsync(Guid id, string currency, CancellationToken cancellationToken = default)
    {
        Transaction? transaction = await _dbContext.Transactions.FindAsync(id, cancellationToken);
        if (transaction is null)
            return null;

        decimal? exchangeRate = await _exchangeRateService.GetExchangeRateAsync(currency, transaction.TransactionDate, cancellationToken);
        if (exchangeRate is null)
            return null;

        return new TransactionResponse
        {
            Id = transaction.Id,
            Description = transaction.Description,
            TransactionDate = transaction.TransactionDate,
            OriginalAmountUsd = transaction.Amount,
            ExchangeRate = exchangeRate.Value,
            ConvertedAmount = Math.Round(transaction.Amount * exchangeRate.Value, 2)
        };
    }
    
    public async Task<List<Transaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions.ToListAsync(cancellationToken);
    }
}