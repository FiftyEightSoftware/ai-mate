using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace api.Tests;

public class JobsClientsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public JobsClientsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Jobs_CRUD_Basic()
    {
        var client = _factory.CreateClient();
        // Initially empty
        var list = await client.GetFromJsonAsync<List<JobDto>>("/api/jobs");
        Assert.NotNull(list);
        Assert.Empty(list);

        // Create
        var create = new JobDto { Title = "Test", Status = "Upcoming", QuotedPrice = 123.45m };
        var resp = await client.PostAsJsonAsync("/api/jobs", create);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var created = await resp.Content.ReadFromJsonAsync<JobDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id != Guid.Empty);

        // Get list now has 1
        list = await client.GetFromJsonAsync<List<JobDto>>("/api/jobs");
        Assert.Single(list!);

        // Get single
        var singleResp = await client.GetAsync($"/api/jobs/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, singleResp.StatusCode);
    }

    [Fact]
    public async Task Clients_Create_And_List()
    {
        var client = _factory.CreateClient();
        var list = await client.GetFromJsonAsync<List<ClientDto>>("/api/clients");
        Assert.NotNull(list);
        Assert.Empty(list);

        var newClient = new ClientDto { Name = "Acme", Phone = "+1-555-0100" };
        var resp = await client.PostAsJsonAsync("/api/clients", newClient);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var created = await resp.Content.ReadFromJsonAsync<ClientDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id != Guid.Empty);

        list = await client.GetFromJsonAsync<List<ClientDto>>("/api/clients");
        Assert.Single(list!);
    }

    public record JobDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public decimal? QuotedPrice { get; set; }
    }

    public record ClientDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
    }
}
