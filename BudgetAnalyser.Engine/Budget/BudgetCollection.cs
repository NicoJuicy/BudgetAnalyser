﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BudgetAnalyser.Engine.Budget;

/// <summary>
///     A collection of budgets.  The collection is always sorted in descending order on the
///     <see cref="BudgetModel.EffectiveFrom" /> date. Ie: Future budgets are on top, then the current budget then archived
///     budgets.
/// </summary>
public class BudgetCollection : IEnumerable<BudgetModel>, IModelValidate
{
    private readonly SortedList<DateOnly, BudgetModel> budgetStorage;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BudgetCollection" /> class.
    /// </summary>
    public BudgetCollection() : this([])
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BudgetCollection" /> class.
    /// </summary>
    /// <param name="initialBudgets">The initial budgets.</param>
    public BudgetCollection(IEnumerable<BudgetModel> initialBudgets)
    {
        this.budgetStorage =
            new SortedList<DateOnly, BudgetModel>(
                initialBudgets.OrderByDescending(b => b.EffectiveFrom)
                    .ToDictionary(model => model.EffectiveFrom),
                new DateOnlyDescendingOrder());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BudgetCollection" /> class.
    /// </summary>
    /// <param name="initialBudgets">The initial budgets.</param>
    public BudgetCollection(params BudgetModel[] initialBudgets) : this(initialBudgets.ToList())
    {
    }

    /// <summary>
    ///     Gets the count of budgets stored in this collection.
    /// </summary>
    public int Count => this.budgetStorage.Count;

    /// <summary>
    ///     Gets the current active budget.
    /// </summary>
    public BudgetModel? CurrentActiveBudget => this.OrderByDescending(b => b.EffectiveFrom)
        .FirstOrDefault(b => b.EffectiveFrom <= DateOnlyExt.Today());

    internal BudgetModel this[int index] => this.budgetStorage.ElementAt(index).Value;

    /// <summary>
    ///     Gets or sets the storage key.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    ///     An enumerator that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<BudgetModel> GetEnumerator()
    {
        return this.budgetStorage.Select(kvp => kvp.Value).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Validate the instance and populate any warnings and errors into the <paramref name="validationMessages" /> string
    ///     builder.
    /// </summary>
    /// <param name="validationMessages">A non-null string builder that will be appended to for any messages.</param>
    /// <returns>
    ///     If the instance is in an invalid state it will return false, otherwise it returns true.
    /// </returns>
    public bool Validate(StringBuilder validationMessages)
    {
        var allValid = this.All(budget => budget.Validate(validationMessages));
        return allValid;
    }

    /// <summary>
    ///     Adds the specified item.
    /// </summary>
    public void Add(BudgetModel item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var key = item.EffectiveFrom;
        while (this.budgetStorage.ContainsKey(key))
        {
            // Arbitrarily change the effective from date to ensure no overlap between budgets.
            key = key.AddDays(1);
        }

        item.EffectiveFrom = key;
        this.budgetStorage.Add(item.EffectiveFrom, item);
    }

    /// <summary>
    ///     Retrieves the applicable budget for the specified date.
    /// </summary>
    public BudgetModel? ForDate(DateOnly date)
    {
        return this.OrderByDescending(b => b.EffectiveFrom).FirstOrDefault(b => b.EffectiveFrom <= date);
    }

    /// <summary>
    ///     Retrieves the applicable budgets for the specified date range.
    /// </summary>
    /// <exception cref="BudgetException">
    ///     The period covered by the dates given overlaps a period where no budgets are
    ///     available.
    /// </exception>
    public IEnumerable<BudgetModel> ForDates(DateOnly beginInclusive, DateOnly endInclusive)
    {
        var budgets = new List<BudgetModel>();
        var firstEffectiveBudget = ForDate(beginInclusive) ?? throw new BudgetException(
            "The period covered by the dates given overlaps a period where no budgets are available.");
        budgets.Add(firstEffectiveBudget);
        budgets.AddRange(this.Where(b => b.EffectiveFrom >= beginInclusive && b.EffectiveFrom <= endInclusive));
        if (budgets.Count(b => b == firstEffectiveBudget) > 1)
        {
            var index = budgets.FindIndex(1, b => b == firstEffectiveBudget);
            budgets.RemoveAt(index);
        }

        return budgets;
    }

    /// <summary>
    ///     Determines whether the provided budget is archived.
    /// </summary>
    public bool IsArchivedBudget(BudgetModel budget)
    {
        if (IsFutureBudget(budget))
        {
            return false;
        }

        return !IsCurrentBudget(budget) && this.Any(b => b.EffectiveFrom <= budget.EffectiveFrom);
    }

    /// <summary>
    ///     Determines whether the provided budget is the current budget.
    /// </summary>
    public bool IsCurrentBudget(BudgetModel budget)
    {
        return CurrentActiveBudget == budget;
    }

    /// <summary>
    ///     Determines whether [is future budget] [the specified budget].
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Better for consistency with other methods here")]
    public bool IsFutureBudget(BudgetModel budget)
    {
        return budget is null ? throw new ArgumentNullException(nameof(budget)) : budget.EffectiveFrom > DateOnlyExt.Today();
    }

    internal virtual int IndexOf(BudgetModel budget)
    {
        return this.budgetStorage.IndexOfValue(budget);
    }
}
