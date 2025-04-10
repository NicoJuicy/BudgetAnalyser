﻿using System.Globalization;
using System.Text;
using BudgetAnalyser.Engine.BankAccount;
using JetBrains.Annotations;

namespace BudgetAnalyser.Engine.Ledger;

/// <summary>
///     This represents a reconciliation as at a date in the <see cref="LedgerBook" /> that crosses all
///     <see cref="LedgerBucket" />s
///     for the reconciliation date.  It shows the financial position at a point in time, the reconciliation date.
///     An instance of this class contains many <see cref="LedgerEntry" />s show the financial position of that
///     <see cref="LedgerBucket" /> as at the reconciliation date.
/// </summary>
public class LedgerEntryLine
{
    private List<BankBalanceAdjustmentTransaction> bankBalanceAdjustments = new();
    private List<BankBalance> bankBalancesList = new();
    private List<LedgerEntry> entries = new();

    /// <summary>
    ///     Constructs a new instance of <see cref="LedgerEntryLine" />.
    /// </summary>
    internal LedgerEntryLine()
    {
        IsNew = true;
    }

    /// <summary>
    ///     This constructor is used by persistence to create a new instance of <see cref="LedgerEntryLine" />.
    /// </summary>
    internal LedgerEntryLine(IEnumerable<BankBalanceAdjustmentTransaction> bankBalanceAdjustments, IEnumerable<BankBalance> bankBalances, IEnumerable<LedgerEntry> ledgerEntries)
        : this()
    {
        BankBalanceAdjustments = bankBalanceAdjustments.ToList();
        BankBalances = bankBalances.ToList();
        Entries = ledgerEntries.ToList();
        Lock();
    }

    /// <summary>
    ///     Constructs a new instance of <see cref="LedgerEntryLine" />.
    ///     Use this constructor for adding a new line when reconciling once a month.
    /// </summary>
    /// <param name="reconciliationDate">The date of the line</param>
    /// <param name="bankBalances">The bank balances for this date.</param>
    internal LedgerEntryLine(DateOnly reconciliationDate, IEnumerable<BankBalance> bankBalances)
        : this()
    {
        if (bankBalances is null)
        {
            throw new ArgumentNullException(nameof(bankBalances));
        }

        Date = reconciliationDate;
        this.bankBalancesList = bankBalances.ToList();
        if (this.bankBalancesList.None())
        {
            throw new ArgumentException("There are no bank balances in the collection provided.",
                nameof(bankBalances));
        }
    }

    /// <summary>
    ///     A collection of optional adjustments to the bank balance that can be added during a reconciliation.
    ///     This is to compensate for transactions that may not have been reflected in the bank account at the time of the
    ///     reconciliation.
    ///     Most commonly this is a credit card payment once the user has ascertained how much surplus they have.
    /// </summary>
    public IEnumerable<BankBalanceAdjustmentTransaction> BankBalanceAdjustments
    {
        get => this.bankBalanceAdjustments;
        [UsedImplicitly]
        private set => this.bankBalanceAdjustments = value.ToList();
    }

    /// <summary>
    ///     The bank balances of all the bank accounts being tracked by the ledger book.
    ///     These balances do not include balance adjustments.
    /// </summary>
    public IEnumerable<BankBalance> BankBalances
    {
        get => this.bankBalancesList;
        [UsedImplicitly]
        private set => this.bankBalancesList = value.ToList();
    }

    /// <summary>
    ///     The total surplus as at the given date.  This is the total surplus across all the bank accounts being tracked by
    ///     the ledger book.
    ///     This is the amount of money left over after funds have been allocated to all budget buckets being tracked by the
    ///     ledger entries.
    /// </summary>
    public decimal CalculatedSurplus => LedgerBalance - Entries.Sum(e => e.Balance);

    /// <summary>
    ///     This is the "as-at" date. It is the date of the fixed snapshot in time when this reconciliation line was created.
    ///     It is not editable as it is used to match transactions from the statement.  Changing this date would mean all
    ///     transactions
    ///     now falling outside the date range would need to be removed, thus affected balances.
    /// </summary>
    public DateOnly Date { get; internal set; }

    /// <summary>
    ///     Gets the entries for all the Ledger Buckets.
    /// </summary>
    public IEnumerable<LedgerEntry> Entries
    {
        get => this.entries;
        private set => this.entries = value.ToList();
    }

    /// <summary>
    ///     A variable to keep track if this is a newly created entry for a new reconciliation as opposed to creation from
    ///     loading from file.
    ///     This variable is intentionally not persisted.
    ///     AutoMapper always sets this to false.
    ///     When a LedgerBook is saved the whole book is reloaded which will set this to false.
    /// </summary>
    internal bool IsNew { get; private set; }

    /// <summary>
    ///     Gets the grand total ledger balance. This includes a total of all accounts and all balance adjustments.
    /// </summary>
    public decimal LedgerBalance => TotalBankBalance + TotalBalanceAdjustments;

    /// <summary>
    ///     Gets the user remarks for this reconciliation.
    /// </summary>
    public string Remarks { get; internal set; } = string.Empty;

    /// <summary>
    ///     The individual surplus balance in each bank account being tracked by the Ledger book.  These will add up to the
    ///     <see cref="CalculatedSurplus" />.
    /// </summary>
    public IEnumerable<BankBalance> SurplusBalances
    {
        get
        {
            var adjustedBalances =
                BankBalances.Select(
                    b => new BankBalance(b.Account, b.Balance + TotalBankBalanceAdjustmentForAccount(b.Account)));
            var results = Entries.GroupBy(
                e => e.LedgerBucket.StoredInAccount,
                (accountType, ledgerEntries) => new BankBalance(accountType, ledgerEntries.Sum(e => e.Balance)));
            return
                adjustedBalances.Select(
                    a =>
                        new BankBalance(a.Account,
                            a.Balance - results.Where(r => r.Account == a.Account).Sum(r => r.Balance)));
        }
    }

    /// <summary>
    ///     Gets the calculated total balance adjustments.
    /// </summary>
    public decimal TotalBalanceAdjustments => BankBalanceAdjustments.Sum(a => a.Amount);

    /// <summary>
    ///     Gets the total bank balance across all accounts. Does not include balance adjustments.
    /// </summary>
    public decimal TotalBankBalance => this.bankBalancesList.Sum(b => b.Balance);

    internal BankBalanceAdjustmentTransaction BalanceAdjustment(decimal adjustment, string narrative, Account account, Guid? id = null)
    {
        if (!IsNew)
        {
            throw new InvalidOperationException("Cannot adjust existing ledger lines, only newly added lines can be adjusted.");
        }

        if (adjustment == 0)
        {
            throw new ArgumentException("The balance adjustment amount cannot be zero.", nameof(adjustment));
        }

        var newAdjustment = id is null
            ? new BankBalanceAdjustmentTransaction { Date = Date, Narrative = narrative, Amount = adjustment, BankAccount = account }
            : new BankBalanceAdjustmentTransaction(id.Value) { Date = Date, Narrative = narrative, Amount = adjustment, BankAccount = account };

        this.bankBalanceAdjustments.Add(newAdjustment);
        return newAdjustment;
    }

    internal void CancelBalanceAdjustment(Guid transactionId)
    {
        if (!IsNew)
        {
            throw new InvalidOperationException("Cannot adjust existing ledger lines, only newly added lines can be adjusted.");
        }

        var txn = this.bankBalanceAdjustments.FirstOrDefault(t => t.Id == transactionId);
        if (txn is not null)
        {
            this.bankBalanceAdjustments.Remove(txn);
        }
    }

    internal static decimal FindPreviousEntryClosingBalance(LedgerEntryLine? previousLine, LedgerBucket ledgerBucket)
    {
        if (ledgerBucket is null)
        {
            throw new ArgumentNullException(nameof(ledgerBucket));
        }

        if (previousLine is null)
        {
            return 0;
        }

        var previousEntry = previousLine.Entries.FirstOrDefault(e => e.LedgerBucket.BudgetBucket == ledgerBucket.BudgetBucket);
        return previousEntry?.Balance ?? 0;
    }

    /// <summary>
    ///     Sets the <see cref="LedgerEntry" /> list for this reconciliation. Used when building a new reconciliation and
    ///     populating a new <see cref="LedgerEntryLine" />. Persistence uses private setter on <see cref="LedgerEntryLine.Entries" />.
    /// </summary>
    internal void SetNewLedgerEntries(IEnumerable<LedgerEntry> ledgerEntries)
    {
        Entries = ledgerEntries.ToList();
    }

    internal void Unlock()
    {
        IsNew = true;
        foreach (var entry in Entries)
        {
            entry.Unlock();
        }
    }

    internal void UpdateRemarks(string remarks)
    {
        if (IsNew)
        {
            Remarks = remarks;
        }
    }

    internal bool Validate(StringBuilder validationMessages, LedgerEntryLine? previousLine)
    {
        if (validationMessages is null)
        {
            throw new ArgumentNullException(nameof(validationMessages));
        }

        var result = true;

        if (Entries.None())
        {
            validationMessages.AppendFormat(CultureInfo.CurrentCulture, "The Ledger Entry does not contain any entries, either delete it or add entries.");
            result = false;
        }

        var totalLedgers = Entries.Sum(e => e.Balance);
        if (totalLedgers + CalculatedSurplus - LedgerBalance != 0)
        {
            result = false;
            validationMessages.Append("All ledgers + surplus + balance adjustments does not equal balance.");
        }

        foreach (var ledgerEntry in Entries)
        {
            if (!ledgerEntry.Validate(validationMessages, FindPreviousEntryClosingBalance(previousLine, ledgerEntry.LedgerBucket)))
            {
                validationMessages.AppendFormat(CultureInfo.CurrentCulture, "\nLedger Entry with Balance {0:C} is invalid.", ledgerEntry.Balance);
                result = false;
            }
        }

        return result;
    }

    private void Lock()
    {
        IsNew = false;
        foreach (var entry in Entries)
        {
            entry.Lock();
        }
    }

    private decimal TotalBankBalanceAdjustmentForAccount(Account account)
    {
        return BankBalanceAdjustments.Where(a => a.BankAccount == account).Sum(a => a.Amount);
    }
}
