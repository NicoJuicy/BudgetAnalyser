﻿using System;
using JetBrains.Annotations;

namespace BudgetAnalyser.Engine.Budget.Data
{
    [AutoRegisterWithIoC]
    internal partial class Mapper_IncomeDto_Income
    {
        private readonly IBudgetBucketRepository bucketRepo;

        public Mapper_IncomeDto_Income([NotNull] IBudgetBucketRepository bucketRepo)
        {
            if (bucketRepo is null) throw new ArgumentNullException(nameof(bucketRepo));
            this.bucketRepo = bucketRepo;
        }

        partial void ToDtoPostprocessing(ref IncomeDto dto, Income model)
        {
            dto.BudgetBucketCode = model.Bucket.Code;
        }

        partial void ToModelPostprocessing(IncomeDto dto, ref Income model)
        {
            model.Bucket = this.bucketRepo.GetByCode(dto.BudgetBucketCode);
        }
    }
}