using System;
using System.Collections.Generic;
using BudgetAnalyser.Engine.Annotations;
using BudgetAnalyser.Engine.Budget;
using BudgetAnalyser.Engine.Statement;

namespace BudgetAnalyser.Engine.Ledger
{
    public interface IReconciliationManager
    {
        /// <summary>
        ///     Creates a new LedgerEntryLine for a <see cref="LedgerBook" /> to begin reconciliation.
        /// </summary>
        /// <param name="ledgerBook">The ledger book that this reconciliation belongs to</param>
        /// <param name="reconciliationDate">
        ///     The startDate for the <see cref="LedgerEntryLine" />. This is usually the previous Month's "Reconciliation-Date",
        ///     as this month's reconciliation starts with this date and includes transactions
        ///     from that date. This date is different to the "Reconciliation-Date" that appears next to the resulting
        ///     reconciliation which is the end date for the period.
        /// </param>
        /// <param name="currentBankBalances">
        ///     The bank balances as at the reconciliation date to include in this new single line of the
        ///     ledger book.
        /// </param>
        /// <param name="budgetContext">The current budget.</param>
        /// <param name="statement">The currently loaded statement.</param>
        /// <param name="ignoreWarnings">Ignores validation warnings if true, otherwise <see cref="ValidationWarningException" />.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="budgetContext" /> is in an invalid state, ie the
        ///     supplied budget is not active.
        /// </exception>
        /// <exception cref="ValidationWarningException">
        ///     Thrown when there are outstanding validation errors in the
        ///     <paramref name="statement" />, <paramref name="budgetContext" />, or <paramref name="ledgerBook" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the supplied dates are invalid or not consistent with the
        ///     <paramref name="ledgerBook" />.
        /// </exception>
        ReconciliationResult MonthEndReconciliation(
            [NotNull] LedgerBook ledgerBook,
            DateTime reconciliationDate,
            [NotNull] IEnumerable<BankBalance> currentBankBalances,
            [NotNull] IBudgetCurrencyContext budgetContext,
            [NotNull] StatementModel statement,
            bool ignoreWarnings = false);

        /// <summary>
        ///     Performs a funds transfer for the given ledger entry line.
        /// </summary>
        void TransferFunds([NotNull] TransferFundsCommand transferDetails, [NotNull] LedgerEntryLine ledgerEntryLine);
    }
}