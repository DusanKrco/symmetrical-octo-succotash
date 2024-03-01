using Domain.Abstractions;

namespace Kx.Availability.Tests.Data;

public interface ITestDataAccessFactory
{
    ITestData GetDataAccess();
}