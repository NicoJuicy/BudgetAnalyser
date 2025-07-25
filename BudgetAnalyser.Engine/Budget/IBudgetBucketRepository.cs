﻿using BudgetAnalyser.Engine.Budget.Data;

namespace BudgetAnalyser.Engine.Budget;

/// <summary>
///     A repository for all Budget Buckets of all kinds whether they are used in the budget or not.
///     This repository does not need to be persisted, because it uses the <see cref="BudgetCollection" /> as the source of
///     truth.
/// </summary>
public interface IBudgetBucketRepository
{
    /// <summary>
    ///     Gets all known budget buckets.
    /// </summary>
    IEnumerable<BudgetBucket> Buckets { get; }

    /// <summary>
    ///     Gets the surplus bucket. This is for convenience only, it also exists in the <see cref="Buckets" /> collection
    /// </summary>
    BudgetBucket SurplusBucket { get; }

    /// <summary>
    ///     Creates the new fixed budget project. Returns null if the bucket code already exists.
    /// </summary>
    /// <param name="bucketCode">The bucket code.</param>
    /// <param name="description">The description.</param>
    /// <param name="fixedBudgetAmount">The fixed budget amount.</param>
    FixedBudgetProjectBucket? CreateNewFixedBudgetProject(string bucketCode, string description, decimal fixedBudgetAmount);

    /// <summary>
    ///     Gets a bucket by its code.
    /// </summary>
    /// <param name="code">The code, also used as a key and must be unique.</param>
    /// <returns>The registered bucket or null if the given code doesn't exist.</returns>
    BudgetBucket? GetByCode(string code);

    /// <summary>
    ///     Gets the bucket by its code or creates a new one if not found.
    /// </summary>
    /// <param name="code">The code, also used as a key and must be unique.</param>
    /// <param name="factory">The factory to create the new bucket if not already registered.</param>
    /// <returns>The bucket.</returns>
    BudgetBucket GetOrCreateNew(string code, Func<BudgetBucket> factory);

    /// <summary>
    ///     Initialises the buckets from the provided data.  Used by persistence.
    /// </summary>
    IBudgetBucketRepository Initialise(IEnumerable<BudgetBucketDto> buckets);

    /// <summary>
    ///     Determines whether the bucket code is registered in this repository.
    /// </summary>
    /// <param name="code">The code, also used as a key and must be unique.</param>
    /// <returns>True if found, otherwise false.</returns>
    bool IsValidCode(string code);
}
