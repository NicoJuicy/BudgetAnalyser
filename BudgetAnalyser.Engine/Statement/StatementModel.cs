﻿using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BudgetAnalyser.Engine.Budget;
using JetBrains.Annotations;

namespace BudgetAnalyser.Engine.Statement;

/// <summary>
///     A collection of bank statement transactions that have been imported.
/// </summary>
/// <seealso cref="INotifyPropertyChanged" />
/// <seealso cref="IDataChangeDetection" />
/// <seealso cref="IDisposable" />
[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "There are no native resources to clean up. Unnecessary complexity.")]
public class StatementModel : INotifyPropertyChanged, IDataChangeDetection, IDisposable
{
    private readonly ILogger logger;

    /// <summary>
    ///     A hash to show when critical state of the statement model has changed. Includes child objects ie Transactions.
    ///     The hash does not persist between Application Loads.
    /// </summary>
    private Guid changeHash;

    private GlobalFilterCriteria currentFilter = new();

    // Track whether Dispose has been called.
    private bool disposed;
    private List<Transaction> doNotUseAllTransactions = new();
    private int doNotUseDurationInMonths;
    private IEnumerable<Transaction> doNotUseTransactions = new List<Transaction>();
    private IEnumerable<IGrouping<int, Transaction>>? duplicates;
    private int fullDuration;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StatementModel" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <exception cref="System.ArgumentNullException"></exception>
    [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Reviewed, ok here. Required for binding")]
    public StatementModel(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.changeHash = Guid.NewGuid();
    }

    /// <summary>
    ///     Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     Gets all transactions ignoring any filters.
    /// </summary>
    public IEnumerable<Transaction> AllTransactions
    {
        get => this.doNotUseAllTransactions;

        private set
        {
            this.doNotUseAllTransactions = value.ToList();
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Gets the duration in months of the current <see cref="Transactions" /> collection.
    /// </summary>
    public int DurationInMonths
    {
        get => this.doNotUseDurationInMonths;

        private set
        {
            this.doNotUseDurationInMonths = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Gets a value indicating whether this <see cref="StatementModel" /> has an active filtered.
    /// </summary>
    public bool Filtered { get; private set; }

    /// <summary>
    ///     Gets the last imported date and time.
    /// </summary>
    public DateTime LastImport { get; internal init; }

    /// <summary>
    ///     Gets or sets the storage key.  This could be the filename for the statement's persistence, or a database unique id.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the filtered transactions.
    /// </summary>
    public IEnumerable<Transaction> Transactions
    {
        get => this.doNotUseTransactions;

        private set
        {
            this.doNotUseTransactions = value;
            this.changeHash = Guid.NewGuid();
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Calculates a hash that represents a data state for the current instance.  When the data state changes the hash will change.
    /// </summary>
    public long SignificantDataChangeHash()
    {
        ThrowIfDisposed();
        return BitConverter.ToInt64(this.changeHash.ToByteArray(), 8);
    }

    /// <summary>
    ///     Implement IDisposable. Do not make this method virtual. A derived class should not be able to override this method
    /// </summary>
    public void Dispose()
    {
        Dispose(true);

        // Take this instance off the Finalization queue to prevent finalization code for this object from executing a second time.
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Allows derivatives to customise dispose logic.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        // Check to see if Dispose has already been called.
        if (!this.disposed)
        {
            UnsubscribeToTransactionChangedEvents();
        }

        this.disposed = true;
    }

    internal virtual void Filter(GlobalFilterCriteria? criteria)
    {
        ThrowIfDisposed();
        if (criteria is null)
        {
            this.changeHash = Guid.NewGuid();
            Transactions = AllTransactions.ToList();
            DurationInMonths = this.fullDuration;
            Filtered = false;
            return;
        }

        if (criteria.BeginDate > criteria.EndDate)
        {
            throw new ArgumentException("End date must be after the begin date.");
        }

        this.currentFilter = criteria;

        this.changeHash = Guid.NewGuid();
        if (criteria.Cleared)
        {
            Transactions = AllTransactions.ToList();
            DurationInMonths = this.fullDuration;
            Filtered = false;
            return;
        }

        var query = BaseFilterQuery(criteria);

        Transactions = query.ToList();
        DurationInMonths = StatementCalculations.CalculateDurationInMonths(criteria, Transactions);
        this.duplicates = null;
        Filtered = true;
    }

    /// <summary>
    ///     Used internally by the importers to load transactions into the statement model. This includes data loaded by persistence.
    /// </summary>
    /// <param name="transactions">The transactions to load.</param>
    /// <returns>Returns this instance, to allow chaining.</returns>
    internal StatementModel LoadTransactions(IEnumerable<Transaction> transactions)
    {
        ThrowIfDisposed();
        UnsubscribeToTransactionChangedEvents();
        this.changeHash = Guid.NewGuid();
        var listOfTransactions = transactions.OrderBy(t => t.Date).ToList();
        Transactions = listOfTransactions;
        AllTransactions = Transactions;
        if (listOfTransactions.Any())
        {
            UpdateDuration();
        }

        this.duplicates = null;
        OnPropertyChanged(nameof(Transactions));
        SubscribeToTransactionChangedEvents();
        return this;
    }

    /// <summary>
    ///     Merges the provided model with this one and returns a new combined model. This model or the supplied one are not changed.
    /// </summary>
    [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ok here. This methods creates the instance for use elsewhere.")]
    internal virtual StatementModel Merge(StatementModel additionalModel)
    {
        ThrowIfDisposed();
        if (additionalModel is null)
        {
            throw new ArgumentNullException(nameof(additionalModel));
        }

        var combinedModel = new StatementModel(this.logger) { LastImport = additionalModel.LastImport, StorageKey = StorageKey };

        var mergedTransactions = AllTransactions.ToList().Merge(additionalModel.AllTransactions).ToList();
        combinedModel.LoadTransactions(mergedTransactions);
        return combinedModel;
    }

    internal void ReassignFixedProjectTransactions(FixedBudgetProjectBucket bucket, BudgetBucket reassignmentBucket)
    {
        ThrowIfDisposed();
        if (bucket is null)
        {
            throw new ArgumentNullException(nameof(bucket));
        }

        if (reassignmentBucket is null)
        {
            throw new ArgumentNullException(nameof(reassignmentBucket));
        }

        foreach (var transaction in AllTransactions.Where(t => t.BudgetBucket == bucket))
        {
            transaction.BudgetBucket = reassignmentBucket;
        }
    }

    internal virtual void RemoveTransaction(Transaction transaction)
    {
        ThrowIfDisposed();
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        transaction.PropertyChanged -= OnTransactionPropertyChanged;
        this.changeHash = Guid.NewGuid();
        this.doNotUseAllTransactions.Remove(transaction);
        Filter(this.currentFilter);
    }

    internal virtual void SplitTransaction(
        Transaction originalTransaction,
        decimal splinterAmount1,
        decimal splinterAmount2,
        BudgetBucket splinterBucket1,
        BudgetBucket splinterBucket2)
    {
        ThrowIfDisposed();
        if (originalTransaction is null)
        {
            throw new ArgumentNullException(nameof(originalTransaction));
        }

        if (splinterBucket1 is null)
        {
            throw new ArgumentNullException(nameof(splinterBucket1));
        }

        if (splinterBucket2 is null)
        {
            throw new ArgumentNullException(nameof(splinterBucket2));
        }

        var splinterTransaction1 = originalTransaction.Clone();
        var splinterTransaction2 = originalTransaction.Clone();

        splinterTransaction1.Amount = splinterAmount1;
        splinterTransaction2.Amount = splinterAmount2;

        splinterTransaction1.BudgetBucket = splinterBucket1;
        splinterTransaction2.BudgetBucket = splinterBucket2;

        if (splinterAmount1 + splinterAmount2 != originalTransaction.Amount)
        {
            throw new ArgumentException("The two new amounts do not add up to the original transaction value.");
        }

        RemoveTransaction(originalTransaction);

        this.changeHash = Guid.NewGuid();
        var mergedTransactions = AllTransactions.ToList().Merge([splinterTransaction1, splinterTransaction2]).ToList();
        AllTransactions = mergedTransactions;
        splinterTransaction1.PropertyChanged += OnTransactionPropertyChanged;
        splinterTransaction2.PropertyChanged += OnTransactionPropertyChanged;
        this.duplicates = null;
        UpdateDuration();
        Filter(this.currentFilter);
    }

    internal IEnumerable<IGrouping<int, Transaction>> ValidateAgainstDuplicates(DateOnly? minDate = null, DateOnly? maxDate = null)
    {
        ThrowIfDisposed();
        if (this.duplicates is not null)
        {
            return this.duplicates;
            // Reset by Merging Transactions, Load Transactions, or by reloading the statement model.
        }

        minDate ??= DateOnly.MinValue;
        maxDate ??= DateOnly.MaxValue;
        var query =
            Transactions
                .Where(t => t.Date >= minDate && t.Date <= maxDate)
                .GroupBy(t => t.GetEqualityHashCode(), t => t)
                .Where(group => group.Count() > 1)
                .AsParallel()
                .ToList();
        this.logger.LogWarning(l => l.Format("{0} Duplicates detected.", query.Sum(group => group.Count())));
        query.ForEach(
            duplicate =>
            {
                foreach (var txn in duplicate)
                {
                    txn.IsSuspectedDuplicate = true;
                }
            });
        this.duplicates = query;
        return this.duplicates;
    }

    private IEnumerable<Transaction> BaseFilterQuery(GlobalFilterCriteria criteria)
    {
        if (criteria.Cleared)
        {
            return AllTransactions.ToList();
        }

        var query = AllTransactions;
        if (criteria.BeginDate is not null)
        {
            query = AllTransactions.Where(t => t.Date >= criteria.BeginDate.Value);
        }

        if (criteria.EndDate is not null)
        {
            query = query.Where(t => t.Date <= criteria.EndDate.Value);
        }

        return query;
    }

    /// <summary>
    ///     Called when a property is changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    [NotifyPropertyChangedInvocator]
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        ThrowIfDisposed();
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnTransactionPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        switch (propertyChangedEventArgs.PropertyName)
        {
            case nameof(Transaction.Amount):
            case nameof(Transaction.BudgetBucket):
            case nameof(Transaction.Date):
                this.changeHash = Guid.NewGuid();
                break;
        }
    }

    private void SubscribeToTransactionChangedEvents()
    {
        Parallel.ForEach(AllTransactions, transaction => { transaction.PropertyChanged += OnTransactionPropertyChanged; });
    }

    private void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException(nameof(StatementModel));
        }
    }

    private void UnsubscribeToTransactionChangedEvents()
    {
        if (AllTransactions.None())
        {
            return;
        }

        Parallel.ForEach(AllTransactions, transaction => { transaction.PropertyChanged -= OnTransactionPropertyChanged; });
    }

    private void UpdateDuration()
    {
        this.fullDuration = StatementCalculations.CalculateDurationInMonths(new GlobalFilterCriteria(), AllTransactions);
        DurationInMonths = StatementCalculations.CalculateDurationInMonths(null, Transactions);
    }

    /// <summary>
    ///     Finalizes an instance of the <see cref="StatementModel" /> class. This destructor will run only if the Dispose method does not get called.
    ///     Do not provide destructors in types derived from this class.
    /// </summary>
    ~StatementModel()
    {
        // Do not re-create Dispose clean-up code here.
        Dispose(false);
    }
}
