using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DiscordArchitect.Services;
using FluentAssertions;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

/// <summary>
/// Contains unit tests for the ForumTagService class.
/// </summary>
/// <remarks>These tests verify the behavior of ForumTagService methods, ensuring correct HTTP requests are
/// constructed and sent. The tests use a custom HTTP message handler to capture and inspect outgoing requests without
/// making real network calls.</remarks>
public class ForumTagServiceTests
{
    /// <summary>
    /// Verifies that the PatchAvailableTagsAsync method sends the correct JSON payload and HTTP request when updating
    /// available tags for a channel.
    /// </summary>
    /// <remarks>This test ensures that the HTTP PATCH request is sent to the expected endpoint with the
    /// appropriate JSON structure, including the correct tag names and properties. It also verifies that the method
    /// returns a successful result when the operation completes as expected.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task PatchAvailableTagsAsync_SendsCorrectJson()
    {
        string? capturedContent = null;
        string? capturedUrl = null;
        HttpMethod? capturedMethod = null;

        var handler = new FakeHandler(async (req, ct) =>
        {
            capturedMethod = req.Method;
            capturedUrl = req.RequestUri!.ToString();

            // null-safe content read + ct z xUnit TestContext
            if (req.Content is not null)
            {
                capturedContent = await req.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var http = new HttpClient(handler);
        var sut = new ForumTagService(http);

        var payload = new object[]
        {
            new { name = "Bug",  emoji = new { name = "🐞" }, moderated = false },
            new { name = "Idea", emoji = (object?)null,     moderated = false }
        };

        var ok = await sut.PatchAvailableTagsAsync(123456789012345678UL, payload, CancellationToken.None);

        ok.Should().BeTrue();
        capturedMethod.Should().Be(HttpMethod.Patch);
        capturedUrl.Should().EndWith("/api/v10/channels/123456789012345678");
        capturedContent.Should().NotBeNull();
        capturedContent!.Should().Contain("\"available_tags\"");
        capturedContent.Should().Contain("\"name\":\"Bug\"");
        capturedContent.Should().Contain("\"name\":\"Idea\"");
    }

    /// <summary>
    /// Provides a custom HTTP message handler that delegates request processing to a user-supplied function. Intended
    /// for use in testing scenarios where HTTP responses need to be simulated.
    /// </summary>
    /// <remarks>This handler enables the simulation of HTTP responses by allowing callers to specify the
    /// logic for handling HTTP requests. It is typically used in unit tests to mock HTTP interactions without making
    /// real network calls.</remarks>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _impl;
        public FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> impl) => _impl = impl;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _impl(request, cancellationToken);
    }
}
