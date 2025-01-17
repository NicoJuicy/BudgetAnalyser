﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BudgetAnalyser.Engine.BankAccount;
using BudgetAnalyser.Engine.Budget;
using BudgetAnalyser.Engine.Ledger;
using BudgetAnalyser.Engine.Ledger.Reconciliation;
using BudgetAnalyser.Engine.Persistence;
using BudgetAnalyser.Engine.Statement;
using JetBrains.Annotations;

namespace BudgetAnalyser.Engine.Services;

[AutoRegisterWithIoC(SingleInstance = true)]
internal class ReconciliationService : IReconciliationService, ISupportsModelPersistence
{
    private readonly IReconciliationCreationManager reconciliationManager;

    public ReconciliationService([NotNull] IReconciliationCreationManager reconciliationManager)
    {
        if (reconciliationManager is null)
        {
            throw new ArgumentNullException(nameof(reconciliationManager));
        }

        this.reconciliationManager = reconciliationManager;
    }

    /// <summary>
    ///     Gets the type of the data the implementation deals with.
    /// </summary>
    public ApplicationDataType DataType => ApplicationDataType.Ledger;

    /// <summary>
    ///     Gets the initialisation sequence number. Set this to a low number for important data that needs to be loaded first.
    ///     Defaults to 50.
    /// </summary>
    public int LoadSequence => 51;

    /// <summary>
    ///     The To Do List loaded from a persistent storage.
    /// </summary>
    public ToDoCollection ReconciliationToDoList { get; private set; }

    /// <summary>
    ///     An optional validation method the UI can call before invoking <see cref="PeriodEndReconciliation" />
    ///     to test for
    ///     validation warnings.
    ///     If validation fails a new <see cref="ValidationWarningException" /> is thrown; otherwise the method returns.
    /// </summary>
    public void BeforeReconciliationValidation(LedgerBook book, StatementModel model)
    {
        this.reconciliationManager.ValidateAgainstOrphanedAutoMatchingTransactions(book, model);
    }

    public void CancelBalanceAdjustment(LedgerEntryLine entryLine, Guid transactionId)
    {
        if (entryLine is null)
        {
            throw new ArgumentNullException(nameof(entryLine));
        }

        entryLine.CancelBalanceAdjustment(transactionId);
    }

    public LedgerTransaction CreateBalanceAdjustment(LedgerEntryLine entryLine, decimal amount, string narrative,
                                                     Account account)
    {
        if (entryLine is null)
        {
            throw new ArgumentNullException(nameof(entryLine));
        }

        if (narrative is null)
        {
            throw new ArgumentNullException(nameof(narrative));
        }

        if (account is null)
        {
            throw new ArgumentNullException(nameof(account));
        }

        var adjustmentTransaction = entryLine.BalanceAdjustment(amount, narrative, account);
        adjustmentTransaction.Date = entryLine.Date;
        return adjustmentTransaction;
    }

    public LedgerTransaction CreateLedgerTransaction(LedgerBook ledgerBook, LedgerEntryLine reconciliation, LedgerEntry ledgerEntry,
                                                     decimal amount, string narrative)
    {
        if (reconciliation is null)
        {
            throw new ArgumentNullException(nameof(reconciliation));
        }

        if (ledgerEntry is null)
        {
            throw new ArgumentNullException(nameof(ledgerEntry));
        }

        if (narrative is null)
        {
            throw new ArgumentNullException(nameof(narrative));
        }

        LedgerTransaction newTransaction = new CreditLedgerTransaction();
        newTransaction.WithAmount(amount).WithNarrative(narrative);
        newTransaction.Date = reconciliation.Date;

        // ledgerEntry.AddTransactionForPersistenceOnly(newTransaction);
        var replacementTxns = ledgerEntry.Transactions.ToList();
        replacementTxns.Add(newTransaction);
        ledgerEntry.SetTransactionsForReconciliation(replacementTxns);
        ledgerEntry.RecalculateClosingBalance(ledgerBook);
        return newTransaction;
    }

    public LedgerEntryLine PeriodEndReconciliation(LedgerBook ledgerBook,
                                                   DateTime reconciliationDate,
                                                   BudgetCollection budgetCollection,
                                                   StatementModel statement,
                                                   bool ignoreWarnings,
                                                   params BankBalance[] balances)
    {
        var reconResult = this.reconciliationManager.PeriodEndReconciliation(ledgerBook, reconciliationDate, budgetCollection, statement, ignoreWarnings, balances);
        ReconciliationToDoList.Clear();
        reconResult.Tasks.ToList().ForEach(ReconciliationToDoList.Add);
        return reconResult.Reconciliation;
    }

    public void RemoveTransaction(LedgerBook ledgerBook, LedgerEntry ledgerEntry, Guid transactionId)
    {
        if (ledgerBook is null)
        {
            throw new ArgumentNullException(nameof(ledgerBook));
        }

        if (ledgerEntry is null)
        {
            throw new ArgumentNullException(nameof(ledgerEntry));
        }

        ledgerEntry.RemoveTransaction(transactionId);

        ledgerEntry.RecalculateClosingBalance(ledgerBook);
    }

    /// <summary>
    ///     Transfer funds from one ledger bucket to another. This is only possible if the current ledger reconciliation is unlocked. This is usually used during reconciliation.
    /// </summary>
    /// <param name="ledgerBook">The parent ledger book.</param>
    /// <param name="reconciliation">
    ///     The reconciliation line that this transfer will be created in.  A transfer can only occur between two ledgers in the same reconciliation.
    /// </param>
    /// <param name="transferDetails">The details of the requested transfer.</param>
    public void TransferFunds(LedgerBook ledgerBook, LedgerEntryLine reconciliation, TransferFundsCommand transferDetails)
    {
        if (reconciliation is null)
        {
            throw new ArgumentNullException(nameof(reconciliation), "There are no reconciliations. Transfer funds can only be used on the most recent reconciliation.");
        }

        this.reconciliationManager.TransferFunds(ledgerBook, transferDetails, reconciliation);
    }

    public LedgerEntryLine UnlockCurrentPeriod(LedgerBook ledgerBook)
    {
        return ledgerBook is null ? throw new ArgumentNullException(nameof(ledgerBook)) : ledgerBook.UnlockMostRecentLine();
    }

    public void UpdateRemarks(LedgerEntryLine entryLine, string remarks)
    {
        if (entryLine is null)
        {
            throw new ArgumentNullException(nameof(entryLine));
        }

        entryLine.UpdateRemarks(remarks);
    }

    /// <summary>
    ///     Closes the currently loaded file.  No warnings will be raised if there is unsaved data.
    /// </summary>
    public void Close()
    {
        ReconciliationToDoList = new ToDoCollection();
    }

    /// <summary>
    ///     Create a new file specific for that service's data.
    /// </summary>
    public Task CreateAsync(ApplicationDatabase applicationDatabase)
    {
        // Nothing needs to be done here.
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Loads a data source with the provided database reference data asynchronously.
    /// </summary>
    public Task LoadAsync(ApplicationDatabase applicationDatabase)
    {
        if (applicationDatabase is null)
        {
            throw new ArgumentNullException(nameof(applicationDatabase));
        }

        // The To Do Collection persistence is managed by the ApplicationDatabaseService.
        ReconciliationToDoList = applicationDatabase.LedgerReconciliationToDoCollection;
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Saves the application database asynchronously. This may be called using a background worker thread.
    /// </summary>
    public Task SaveAsync(ApplicationDatabase applicationDatabase)
    {
        // Nothing needs to be done here.
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called before Save is called. This will be called on the UI Thread. Objects can optionally add some context data that will be passed to the
    ///     <see cref="ISupportsModelPersistence.SaveAsync" /> method call. This can be used to finalise any edits or prompt the user for closing data, ie, a "what-did-you-change" comment; this
    ///     can't be done during save as it may not be called using the UI Thread.
    /// </summary>
    public void SavePreview()
    {
    }

    /// <summary>
    ///     Validates the model owned by the service.
    /// </summary>
    public bool ValidateModel(StringBuilder messages)
    {
        return true;
    }
}
