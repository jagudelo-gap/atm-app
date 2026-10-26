using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Atm.Api.Tests;

public sealed class SpaHostingTests : IDisposable
{
    private const string IndexHtml = "<html><body>stub-spa</body></html>";
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), $"atm-web-{Guid.NewGuid():N}");
    private readonly AtmApiFactory _factory = new();

    public SpaHostingTests()
    {
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), IndexHtml);
        File.WriteAllText(Path.Combine(_webRoot, "app.js"), "console.log('app');");
    }

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseWebRoot(_webRoot)).CreateClient();

    [Fact]
    public async Task Root_returns_index_html_when_wwwroot_has_it()
    {
        var response = await CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(IndexHtml, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Static_asset_is_served()
    {
        var response = await CreateClient().GetAsync("/app.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("console.log", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_client_route_falls_back_to_index()
    {
        var response = await CreateClient().GetAsync("/accounts/some/deep/route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(IndexHtml, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/nope")]
    [InlineData("/api/users/not-a-route/x/y")]
    public async Task Unknown_api_route_returns_404_not_index(string path)
    {
        var response = await CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("stub-spa", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_is_not_swallowed_by_fallback()
    {
        var response = await CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("stub-spa", await response.Content.ReadAsStringAsync());
    }

    public void Dispose()
    {
        _factory.Dispose();
        try { Directory.Delete(_webRoot, recursive: true); } catch (IOException) { /* best effort */ }
    }
}
