﻿using System.Diagnostics.CodeAnalysis;
using BudgetAnalyser.Engine.BankAccount;
using JetBrains.Annotations;
using NotNull = JetBrains.Annotations.NotNullAttribute;

namespace BudgetAnalyser.Engine.Statement
{
    /// <summary>
    ///     The Statement File Manager is responsible for loading any kind of statement file, whether it be an existing budget
    ///     analyser statement file or a downloaded bank statement export to merge or load. If the filename is already known
    ///     it is loaded with no prompting, otherwise the user is prompted for a filename.
    ///     It also is responsible for saving  any open statement file into a budget analyser statement file.
    ///     To function it orchestrates across the  <see cref="IVersionedStatementModelRepository" /> and the
    ///     <see cref="IBankStatementImporterRepository" />.
    ///     This implementation is strictly not thread safe and should be single threaded only.  Don't allow mulitple threads
    ///     to use it at the same time.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Used by IoC")]
    [AutoRegisterWithIoC(SingleInstance = true)]
    internal class StatementRepository : IStatementRepository
    {
        private readonly IBankStatementImporterRepository importerRepository;
        private readonly IVersionedStatementModelRepository statementModelRepository;

        public StatementRepository(
            [NotNull] IVersionedStatementModelRepository statementModelRepository,
            [NotNull] IBankStatementImporterRepository importerRepository)
        {
            if (statementModelRepository is null)
            {
                throw new ArgumentNullException(nameof(statementModelRepository));
            }

            if (importerRepository is null)
            {
                throw new ArgumentNullException(nameof(importerRepository));
            }
            this.statementModelRepository = statementModelRepository;
            this.importerRepository = importerRepository;
        }

        public async Task CreateNewAndSaveAsync(string storageKey)
        {
            await this.statementModelRepository.CreateNewAndSaveAsync(storageKey);
        }

        public async Task<StatementModel> ImportBankStatementAsync(
            string storageKey,
            Account account)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                throw new ArgumentNullException(nameof(storageKey));
            }

            return account is null
                ? throw new ArgumentNullException(nameof(account))
                : await this.importerRepository.ImportAsync(storageKey, account);
        }

        public async Task<StatementModel> LoadAsync(string storageKey, bool isEncrypted)
        {
            return storageKey is null
                ? throw new FileNotFoundException("storageKey")
                : await this.statementModelRepository.LoadAsync(storageKey, isEncrypted);
        }

        public async Task SaveAsync([NotNull] StatementModel statementModel, bool isEncrypted)
        {
            if (statementModel is null)
            {
                throw new ArgumentNullException(nameof(statementModel));
            }

            await this.statementModelRepository.SaveAsync(statementModel, statementModel.StorageKey, isEncrypted);
        }
    }
}
