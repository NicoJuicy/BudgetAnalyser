﻿using BudgetAnalyser.Engine.Mobile;
using BudgetAnalyser.Engine.Persistence;

namespace BudgetAnalyser.Engine.Ledger.Data;

internal class MapperMobileSettingsToDto2 : IDtoMapper<MobileStorageSettingsDto, MobileStorageSettings>
{
    public MobileStorageSettingsDto ToDto(MobileStorageSettings model)
    {
        var dto = new MobileStorageSettingsDto(model.AccessKeyId, model.AccessKeySecret, model.AmazonS3Region);
        return dto;
    }

    public MobileStorageSettings ToModel(MobileStorageSettingsDto dto)
    {
        var model = new MobileStorageSettings { AccessKeyId = dto.AccessKeyId, AccessKeySecret = dto.AccessKeySecret, AmazonS3Region = dto.AmazonS3Region };
        return model;
    }
}
