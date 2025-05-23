﻿using System.Windows.Threading;
using BudgetAnalyser.Engine.Services;
using BudgetAnalyser.Engine.Statement;
using BudgetAnalyser.Filtering;
using CommunityToolkit.Mvvm.Messaging;
using Rees.Wpf;
using Rees.Wpf.Contracts;

namespace BudgetAnalyser.Statement;

public class StatementControllerFileOperations : ControllerBase
{
    private readonly LoadFileController loadFileController;
    private readonly IUserMessageBox messageBox;
    private readonly ITransactionManagerService transactionService;
    private bool doNotUseLoadingData;

    public StatementControllerFileOperations(
        IUiContext uiContext,
        LoadFileController loadFileController,
        IApplicationDatabaseFacade applicationDatabaseService,
        ITransactionManagerService transactionManagerService)
        : base(uiContext.Messenger)
    {
        if (uiContext is null)
        {
            throw new ArgumentNullException(nameof(uiContext));
        }

        if (applicationDatabaseService is null)
        {
            throw new ArgumentNullException(nameof(applicationDatabaseService));
        }

        this.messageBox = uiContext.UserPrompts.MessageBox;
        this.loadFileController = loadFileController ?? throw new ArgumentNullException(nameof(loadFileController));
        this.transactionService = transactionManagerService ?? throw new ArgumentNullException(nameof(transactionManagerService));
        ViewModel = new StatementViewModel(applicationDatabaseService, this.transactionService);
    }

    public bool LoadingData
    {
        get => this.doNotUseLoadingData;
        private set
        {
            if (this.doNotUseLoadingData == value)
            {
                return;
            }

            this.doNotUseLoadingData = value;
            OnPropertyChanged();
        }
    }

    internal StatementViewModel ViewModel { get; }

    internal void Close()
    {
        ViewModel.Statement = null;
        NotifyOfReset();
        ViewModel.TriggerRefreshTotalsRow();
        Messenger.Send(new StatementReadyMessage(null));
    }

    internal async Task MergeInNewTransactions()
    {
        PersistenceOperationCommands.SaveDatabaseCommand.Execute(this);

        var fileName = await GetFileNameFromUser();
        if (string.IsNullOrWhiteSpace(fileName) || this.loadFileController.SelectedExistingAccountName is null)
        {
            // User cancelled
            return;
        }

        try
        {
            var account = this.loadFileController.SelectedExistingAccountName;
            await this.transactionService.ImportAndMergeBankStatementAsync(fileName, account);

            await SyncWithServiceAsync();
            Messenger.Send(new TransactionsChangedMessage());
            OnPropertyChanged(nameof(ViewModel));
            NotifyOfEdit();
            ViewModel.TriggerRefreshTotalsRow();
            Messenger.Send(new StatementReadyMessage(ViewModel.Statement));
        }
        catch (NotSupportedException ex)
        {
            FileCannotBeLoaded(ex);
        }
        catch (KeyNotFoundException ex)
        {
            FileCannotBeLoaded(ex);
        }
        catch (TransactionsAlreadyImportedException ex)
        {
            FileCannotBeLoaded(ex);
        }
        finally
        {
            this.loadFileController.Reset();
        }
    }

    internal void NotifyOfEdit()
    {
        ViewModel.Dirty = true;
        Messenger.Send(new StatementHasBeenModifiedMessage());
    }

    internal async Task SyncWithServiceAsync()
    {
        var statementModel = this.transactionService.StatementModel;
        ViewModel.Statement = null; // Prevent events from firing while updating the model.
        LoadingData = true;
        await Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            () =>
            {
                // Update all UI bound properties.
                var requestCurrentFilterMessage = new RequestFilterMessage(this);
                Messenger.Send(requestCurrentFilterMessage);
                if (requestCurrentFilterMessage.Criteria is not null)
                {
                    this.transactionService.FilterTransactions(requestCurrentFilterMessage.Criteria);
                }

                ViewModel.Statement = statementModel;
                ViewModel.TriggerRefreshTotalsRow();

                Messenger.Send(new StatementReadyMessage(ViewModel.Statement));

                LoadingData = false;
            });
    }

    private void FileCannotBeLoaded(Exception ex)
    {
        this.messageBox.Show("The file was not loaded.\n" + ex.Message);
    }

    /// <summary>
    ///     Prompts the user for a filename and other required parameters to be able to merge the statement file.
    /// </summary>
    /// <returns>
    ///     The user selected filename. All other required parameters are accessible from the
    ///     <see cref="LoadFileController" />.
    /// </returns>
    private async Task<string> GetFileNameFromUser()
    {
        var statement = ViewModel.Statement ?? throw new InvalidOperationException("Statement Model is null, uninitialised or not loaded.");
        await this.loadFileController.RequestUserInputForMerging(statement);

        return this.loadFileController.FileName ?? string.Empty;
    }

    private void NotifyOfReset()
    {
        if (ViewModel is { Dirty: false, Statement: null })
        {
            // No need to notify of reset if the statement is already null. This happens during first load before the statement is loaded
            return;
        }

        ViewModel.Dirty = false;
        Messenger.Send(new StatementHasBeenModifiedMessage());
    }
}
