using FluentAssertions;
using Xunit;

namespace Tianjige.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Projects_ShouldReferenceCoreAndData()
    {
        // Assert Core and Data assemblies can be loaded
        var coreAssembly = typeof(Tianjige.Core.Models.GeneralStaticInfo).Assembly;
        var dataAssembly = typeof(Tianjige.Data.Repositories.StaticRepository).Assembly;

        coreAssembly.Should().NotBeNull();
        dataAssembly.Should().NotBeNull();
    }
}
