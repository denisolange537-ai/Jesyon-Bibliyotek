using GestionBibliotheque.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GestionBibliotheque.Tests;

public sealed class FoundationServiceRegistrationTests
{
    [Fact]
    public void AddLibraryFoundation_BindsLibrarySettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Library:ApplicationName"] = "Bibliyotèk Tès" })
            .Build();
        var services = new ServiceCollection();

        services.AddLibraryFoundation(configuration);

        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<LibrarySettings>>().Value;
        Assert.Equal("Bibliyotèk Tès", settings.ApplicationName);
    }
}
