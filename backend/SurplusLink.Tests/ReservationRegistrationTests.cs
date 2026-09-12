using Microsoft.Extensions.DependencyInjection;
using SurplusLink.Api.Reservations;

namespace SurplusLink.Tests;

public sealed class ReservationRegistrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public void Reservation_service_is_registered_as_scoped_dependency()
    {
        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IReservationService>();

        Assert.IsType<ReservationService>(service);
    }
}
