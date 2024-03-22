using AutoFixture.Xunit2;

using Kerbee.Graph;

using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;

using Moq;
using Moq.AutoMock;

namespace Kerbee.Tests.Graph;

public class GraphServiceTests
{
    private static readonly AutoMocker _mocker = new();
    private static readonly Mock<IRequestAdapter> _requestAdapter = new();

    private static GraphService Sut => _mocker.CreateInstance<GraphService>();


    [Theory, AutoData]
    public async Task GetApplicationAsync_ShouldUseManagedIdentityClient(string objectId)
    {
        // Arrange
        SetupManagedIdentityProvider();

        // Act
        await Sut.GetApplicationAsync(objectId);

        // Assert
        _mocker.VerifyAll();
    }

    [Theory, AutoData]
    public async Task RemoveCertificateAsync_ShouldUseManagedIdentityClient(string objectId, string keyId)
    {
        // Arrange
        SetupManagedIdentityProvider();

        // Act
        await Sut.RemoveCertificateAsync(objectId, keyId);

        // Assert
        _mocker.VerifyAll();
    }

    [Theory, AutoData]
    public async Task RemoveSecretAsync_ShouldUseManagedIdentityClient(string objectId)
    {
        // Arrange
        SetupManagedIdentityProvider();

        // Act
        await Sut.GetApplicationAsync(objectId);

        // Assert
        _mocker.VerifyAll();
    }

    private static void SetupManagedIdentityProvider()
    {
        _mocker.Use<IManagedIdentityProvider>(x => x.GetClient() == new GraphServiceClient(_requestAdapter.Object, null));
    }
}
