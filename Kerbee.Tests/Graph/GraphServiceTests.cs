using AutoFixture.Xunit2;

using Kerbee.Graph;

using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;

using Moq;
using Moq.AutoMock;

namespace Kerbee.Tests.Graph;

public class GraphServiceTests
{
    [Theory, AutoData]
    public async Task GetApplicationAsync_ShouldUseManagedIdentityClient(string objectId)
    {
        // Arrange
        var mocker = new AutoMocker();

        var requestAdapter = mocker.Get<Mock<IRequestAdapter>>();

        mocker.Use<IManagedIdentityProvider>(x => x.GetClient() == new GraphServiceClient(requestAdapter.Object, null));

        // Act
        var sut = mocker.CreateInstance<GraphService>();

        await sut.GetApplicationAsync(objectId);

        // Assert
        mocker.VerifyAll();
    }
}
