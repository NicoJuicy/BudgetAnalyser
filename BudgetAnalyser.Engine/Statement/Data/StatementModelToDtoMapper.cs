﻿using System;
using System.Linq;
using JetBrains.Annotations;
using Rees.TangyFruitMapper;

namespace BudgetAnalyser.Engine.Statement.Data
{
    [AutoRegisterWithIoC]
    internal partial class Mapper_TransactionSetDto_StatementModel
    {
        private readonly ILogger logger;
        private readonly IDtoMapper<TransactionDto, Transaction> transactionMapper;

        public Mapper_TransactionSetDto_StatementModel([NotNull] ILogger logger, [NotNull] IDtoMapper<TransactionDto, Transaction> transactionMapper)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            if (transactionMapper is null) throw new ArgumentNullException(nameof(transactionMapper));
            this.logger = logger;
            this.transactionMapper = transactionMapper;
        }

        // ReSharper disable once RedundantAssignment
        partial void ModelFactory(TransactionSetDto dto, ref StatementModel model)
        {
            model = new StatementModel(this.logger);
        }

        partial void ToDtoPostprocessing(ref TransactionSetDto dto, StatementModel model)
        {
            var transactions10 = model.AllTransactions.Select(this.transactionMapper.ToDto).ToList();
            dto.Transactions = transactions10;
        }

        partial void ToModelPostprocessing(TransactionSetDto dto, ref StatementModel model)
        {
            model.LoadTransactions(dto.Transactions.Select(t => this.transactionMapper.ToModel(t)));
        }
    }
}