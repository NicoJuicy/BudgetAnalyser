﻿using BudgetAnalyser.Engine.BankAccount;
using BudgetAnalyser.Engine.Ledger;
using BudgetAnalyser.Engine.Ledger.Data;
using NSubstitute;
using Shouldly;

namespace BudgetAnalyser.Engine.XUnit.Ledger;

public class MapperBankBalanceAdjustmentToDto_ToModel_Test
{
    private const string AccountName = "CHEQUE";
    private static readonly Guid TransactionId = new("7F921750-4467-4EA4-81E6-3EFD466341A6");
    private readonly Account account;
    private readonly MapperBankBalanceAdjustmentToDto2 subject;
    private readonly LedgerTransactionDto testData;
    private BankBalanceAdjustmentTransaction? result;

    public MapperBankBalanceAdjustmentToDto_ToModel_Test()
    {
        this.testData = new LedgerTransactionDto
        (
            Id: TransactionId,
            Amount: -123.99M,
            Narrative: "Foo bar.",
            TransactionType: typeof(BankBalanceAdjustmentTransaction).FullName,
            Account: AccountName,
            AutoMatchingReference: null,
            Date: new DateOnly(2023, 10, 1));
        this.account = new ChequeAccount(AccountName);
        var accountRepo = Substitute.For<IAccountTypeRepository>();
        accountRepo.GetByKey(AccountName).Returns(this.account);
        this.subject = new MapperBankBalanceAdjustmentToDto2(accountRepo);
    }

    [Fact]
    public void ShouldMapAccount()
    {
        Act();
        this.result!.BankAccount.ShouldBeEquivalentTo(this.account);
    }

    [Fact]
    public void ShouldMapAmount()
    {
        Act();
        this.result!.Amount.ShouldBe(-123.99M);
    }

    [Fact]
    public void ShouldMapId()
    {
        Act();
        this.result!.Id.ShouldBe(TransactionId);
    }

    [Fact]
    public void ShouldMapNarrative()
    {
        Act();
        this.result!.Narrative.ShouldBe("Foo bar.");
    }

    [Fact]
    public void ShouldMapTransactionType()
    {
        Act();
        this.result.ShouldBeOfType<BankBalanceAdjustmentTransaction>();
    }

    private void Act()
    {
        this.result = this.subject.ToModel(this.testData);
    }
}
