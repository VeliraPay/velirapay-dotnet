using VeliraPay.Tests.Support;
using static VeliraPay.Tests.Support.TestSupport;

namespace VeliraPay.Tests;

/// <summary>API keys, members and invitations.</summary>
public sealed class TeamTests
{
    [Fact]
    public async Task AKeyIsCreatedWithTheKeyItselfShownOnce()
    {
        var http = new MockHttp().Json("""
            {"data": {"id": "42", "object": "api_key", "name": "Shop server", "prefix": "vp_live_8Kq2", "key": "vp_live_8Kq2abc", "mode": "live",
                      "permissions": ["charges:read"], "allowed_ips": ["203.0.113.7"], "created_by": "Ada", "last_used_at": null, "created_at": "2026-11-06T09:12:44.000000Z"}}
            """, 201);

        var apiKey = await Client(http).ApiKeys.CreateAsync(
            new ApiKeyCreateOptions { Name = "Shop server", Permissions = [ApiKeyPermission.ChargesRead], AllowedIps = ["203.0.113.7"] },
            new RequestOptions { IdempotencyKey = "new-key" });

        var request = http.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/api-keys", request.Path);
        Assert.Equal("new-key", request.Header("Idempotency-Key"));
        Assert.Equal("""{"name":"Shop server","permissions":["charges:read"],"allowed_ips":["203.0.113.7"]}""", request.Body);
        Assert.Equal("42", apiKey.Id);
        Assert.Equal("Shop server", apiKey.Name);
        Assert.Equal("vp_live_8Kq2", apiKey.Prefix);
        Assert.Equal("vp_live_8Kq2abc", apiKey.Key);
        Assert.Equal(Mode.Live, apiKey.Mode);
        Assert.Equal(["charges:read"], apiKey.Permissions!);
        Assert.Equal(["203.0.113.7"], apiKey.AllowedIps!);
        Assert.Equal("Ada", apiKey.CreatedBy);
        Assert.Null(apiKey.LastUsedAt);
        Assert.Equal(Utc("2026-11-06T09:12:44Z"), apiKey.CreatedAt);
        Assert.False(apiKey.HasFullAccess);
    }

    [Fact]
    public async Task PermissionsAreSentAsTheirValuesWhetherGivenAsConstantsOrStrings()
    {
        var http = new MockHttp().Json("""{"data":{"id":"43","object":"api_key","name":"Books","prefix":"vp_live_9Lr3","key":"vp_live_9Lr3abc","mode":"live","permissions":["payouts:read","account:write"],"allowed_ips":null}}""", 201);

        await Client(http).ApiKeys.CreateAsync(new ApiKeyCreateOptions { Name = "Books", Permissions = [ApiKeyPermission.PayoutsRead, "account:write"] });

        Assert.Equal("""{"name":"Books","permissions":["payouts:read","account:write"]}""", http.LastRequest.Body);
    }

    [Fact]
    public async Task KeysAreListedChangedAndRevoked()
    {
        var http = new MockHttp()
            .Json("""{"data":[{"id":"7","object":"api_key","name":"Old","prefix":"vp_live_aaaa","mode":"live","permissions":null,"allowed_ips":null}],"meta":{"current_page":1,"last_page":1,"per_page":25,"total":1}}""")
            .Json("""{"data":{"id":"7","object":"api_key","name":"Reader","prefix":"vp_live_aaaa","mode":"live","permissions":["charges:read"],"allowed_ips":null}}""")
            .Empty();
        var client = Client(http);

        var listed = Assert.Single(await client.ApiKeys.ListAsync());

        Assert.Equal("/v1/api-keys", http.LastRequest.Path);
        Assert.Null(listed.Key);
        Assert.True(listed.HasFullAccess);
        Assert.Null(listed.AllowedIps);

        var updated = await client.ApiKeys.UpdateAsync("7", new ApiKeyUpdateOptions { Name = "Reader", Permissions = [ApiKeyPermission.ChargesRead], AllowedIps = null });

        Assert.Equal(HttpMethod.Patch, http.LastRequest.Method);
        Assert.Equal("/v1/api-keys/7", http.LastRequest.Path);
        Assert.Equal("""{"name":"Reader","permissions":["charges:read"],"allowed_ips":null}""", http.LastRequest.Body);
        Assert.Equal("Reader", updated.Name);

        await client.ApiKeys.RevokeAsync("7");

        Assert.Equal(HttpMethod.Delete, http.LastRequest.Method);
        Assert.Equal("/v1/api-keys/7", http.LastRequest.Path);
    }

    [Fact]
    public async Task MembersAreListedChangedAndRemoved()
    {
        var http = new MockHttp()
            .Json("""{"data":[{"id":"3","object":"member","name":"Ada","email":"ada@example.com","role":"admin","joined_at":"2026-10-01T08:00:00.000000Z"}],"meta":{"current_page":1,"last_page":1,"per_page":25,"total":1}}""")
            .Json("""{"data":{"id":"3","object":"member","name":"Ada","email":"ada@example.com","role":"finance"}}""")
            .Empty();
        var client = Client(http);

        var member = Assert.Single(await client.Members.ListAsync());

        Assert.Equal("/v1/members", http.LastRequest.Path);
        Assert.Equal("3", member.Id);
        Assert.Equal("Ada", member.Name);
        Assert.Equal("ada@example.com", member.Email);
        Assert.Equal(MemberRole.Admin, member.Role);
        Assert.Equal(Utc("2026-10-01T08:00:00Z"), member.JoinedAt);

        Assert.Equal(MemberRole.Finance, (await client.Members.UpdateAsync("3", new MemberUpdateOptions { Role = MemberRole.Finance })).Role);
        Assert.Equal(HttpMethod.Patch, http.LastRequest.Method);
        Assert.Equal("""{"role":"finance"}""", http.LastRequest.Body);

        await client.Members.RemoveAsync("3");

        Assert.Equal(HttpMethod.Delete, http.LastRequest.Method);
        Assert.Equal("/v1/members/3", http.LastRequest.Path);
    }

    [Fact]
    public void AMemberWithoutARoleReadsAsNull()
    {
        Assert.Null(new Member(Element(Json("""{"id":"3","role":null}"""))).Role);
    }

    [Fact]
    public async Task InvitationsAreSentListedAndCanceled()
    {
        const string invitation = """{"id":"9","object":"invitation","email":"grace@example.com","role":"admin","invited_by":"Ada","expired":false,"expires_at":"2026-11-09T09:00:00.000000Z","created_at":"2026-11-02T09:00:00.000000Z"}""";
        var http = new MockHttp()
            .Json("""{"data":""" + invitation + "}", 201)
            .Json("""{"data":[""" + invitation + """],"meta":{"current_page":1,"last_page":1,"per_page":25,"total":1}}""")
            .Empty();
        var client = Client(http);

        var sent = await client.Invitations.CreateAsync(new InvitationCreateOptions { Email = "grace@example.com", Role = MemberRole.Admin });

        Assert.Equal(HttpMethod.Post, http.LastRequest.Method);
        Assert.Equal("/v1/invitations", http.LastRequest.Path);
        Assert.Equal("""{"email":"grace@example.com","role":"admin"}""", http.LastRequest.Body);
        Assert.Equal("9", sent.Id);
        Assert.Equal("Ada", sent.InvitedBy);
        Assert.Equal(MemberRole.Admin, sent.Role);
        Assert.False(sent.Expired);
        Assert.Equal(Utc("2026-11-09T09:00:00Z"), sent.ExpiresAt);
        Assert.Equal(Utc("2026-11-02T09:00:00Z"), sent.CreatedAt);

        Assert.Equal("grace@example.com", Assert.Single(await client.Invitations.ListAsync()).Email);

        await client.Invitations.CancelAsync("9");

        Assert.Equal(HttpMethod.Delete, http.LastRequest.Method);
        Assert.Equal("/v1/invitations/9", http.LastRequest.Path);
    }
}
