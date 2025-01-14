﻿using Kx.Core.Common.Exceptions;
using Persistence.Kx.Availability.Data.Mongo.Abstractions;

namespace Application.Helpers;

public static class DataAccessHelper
{
    public static IDataAccessAggregation ParseAggregationDataAccess(IDataAccess dataAccess)
    {
        return dataAccess as IDataAccessAggregation ?? throw new UnprocessableEntityException();
    }
}