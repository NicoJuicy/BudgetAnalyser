﻿using BudgetAnalyser.Engine.BankAccount;
using BudgetAnalyser.Engine.Persistence;

namespace BudgetAnalyser.Engine.Ledger.Data;

internal class MapperLedgerEntryToDto2(ILedgerBucketFactory bucketFactory, ILedgerTransactionFactory ledgerTransactionFactory) : IDtoMapper<LedgerEntryDto, LedgerEntry>
{
    private readonly ILedgerBucketFactory bucketFactory = bucketFactory ?? throw new ArgumentNullException(nameof(bucketFactory));

    private readonly IDtoMapper<LedgerTransactionDto, LedgerTransaction> transactionMapper = new MapperLedgerTransactionToDto2(ledgerTransactionFactory);

    public LedgerEntryDto ToDto(LedgerEntry model)
    {
        var dto = new LedgerEntryDto
        (
            StoredInAccount: model.LedgerBucket.StoredInAccount.Name,
            BucketCode: model.LedgerBucket.BudgetBucket.Code,
            Balance: model.Balance,
            Transactions: model.Transactions.Select(this.transactionMapper.ToDto).ToArray()
        );
        return dto;
    }

    public LedgerEntry ToModel(LedgerEntryDto dto)
    {
        var entry = new LedgerEntry(dto.Transactions.Select(t => this.transactionMapper.ToModel(t)))
        {
            Balance = dto.Balance,
            LedgerBucket = this.bucketFactory.Build(dto.BucketCode, string.IsNullOrWhiteSpace(dto.StoredInAccount) ? AccountTypeRepositoryConstants.Cheque : dto.StoredInAccount)
        };

        return entry;
    }
}
