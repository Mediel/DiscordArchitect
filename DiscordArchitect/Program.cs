using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

class Program
{
    private DiscordSocketClient _client = null!;
    private IConfiguration _config = null!;
    private string _token = null!;
    private ulong _serverId;

    // Feature toggles (read from appsettings.json without Binder)
    private bool _createRolePerCategory;
    private bool _everyoneAccessToNewCategory;
    private bool _syncChannelsToCategory;

    // Wait for gateway ready without blocking the Ready handler itself
    private readonly TaskCompletionSource<bool> _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _ranOnce = false;

    static async Task Main(string[] args) => await new Program().MainAsync();

    public async Task MainAsync()
    {
        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .Build();

        _token = _config["Discord:Token"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_token))
        {
            Console.WriteLine("❌ Token not found. Use: dotnet user-secrets set \"Discord:Token\" \"xxx\"");
            return;
        }

        if (!ulong.TryParse(_config["Discord:ServerId"], out _serverId))
        {
            Console.WriteLine("❌ ServerId not found. Use: dotnet user-secrets set \"Discord:ServerId\" \"123...\"");
            return;
        }

        // toggles
        _createRolePerCategory = bool.TryParse(_config["Discord:CreateRolePerCategory"], out var t1) && t1;
        _everyoneAccessToNewCategory = bool.TryParse(_config["Discord:EveryoneAccessToNewCategory"], out var t2) && t2;
        _syncChannelsToCategory = bool.TryParse(_config["Discord:SyncChannelsToCategory"], out var t3) && t3;

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        });

        _client.Log += log => { Console.WriteLine(log.ToString()); return Task.CompletedTask; };
        _client.Ready += ReadyAsync;

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        // Wait for Ready
        await _readyTcs.Task;

        if (_ranOnce) return;
        _ranOnce = true;

        string sourceCategory = _config["Discord:SourceCategoryName"] ?? "Template";
        Console.Write("Enter new category name: ");
        string newCategory = Console.ReadLine() ?? "NewCategory";

        var server = _client.GetGuild(_serverId);
        if (server == null)
        {
            Console.WriteLine("❌ Server not found");
            return;
        }

        PrintGuildPermsAndRoleStack(server);

        await CloneCategoryAsync(server, sourceCategory, newCategory);

        Console.WriteLine("✅ Done. Press ENTER to exit…");
        Console.ReadLine();
    }

    private Task ReadyAsync()
    {
        Console.WriteLine($"✅ Gateway Ready as {_client.CurrentUser.Username}");
        if (!_readyTcs.Task.IsCompleted) _readyTcs.SetResult(true);
        return Task.CompletedTask;
    }

    private async Task CloneCategoryAsync(SocketGuild server, string sourceCategoryName, string newCategoryName)
    {
        var sourceCategory = server.CategoryChannels.FirstOrDefault(c => string.Equals(c.Name, sourceCategoryName, StringComparison.Ordinal));
        if (sourceCategory == null)
        {
            Console.WriteLine($"❌ Source category '{sourceCategoryName}' not found.");
            Console.WriteLine("ℹ️  Available categories:");
            foreach (var c in server.CategoryChannels.OrderBy(c => c.Position))
                Console.WriteLine($" - {c.Name}");
            return;
        }

        // 1) Create target category
        var newCategory = await server.CreateCategoryChannelAsync(newCategoryName);
        Console.WriteLine($"📁 Created category: {newCategory.Name} (id: {newCategory.Id})");

        // 2) Ensure bot access on category (avoid 50013)
        var me = server.CurrentUser;
        var allowBot = new OverwritePermissions(
            viewChannel: PermValue.Allow,
            manageChannel: PermValue.Allow,
            sendMessages: PermValue.Allow
        );
        await newCategory.AddPermissionOverwriteAsync(me, allowBot);

        // @everyone toggle
        if (_everyoneAccessToNewCategory)
        {
            Console.WriteLine("👥 @everyone can access the new category (no explicit deny set).");
        }
        else
        {
            var denyEveryone = new OverwritePermissions(viewChannel: PermValue.Deny);
            await newCategory.AddPermissionOverwriteAsync(server.EveryoneRole, denyEveryone);
            Console.WriteLine("🚫 @everyone denied on the new category.");
        }

        // 3) Optional: create role named after the category and give it access on the category
        IRole? categoryRole = null;
        if (_createRolePerCategory)
        {
            if (!me.GuildPermissions.ManageRoles)
            {
                Console.WriteLine("⚠️ Skipping role creation: bot lacks Manage Roles (268435456).");
            }
            else
            {
                try
                {
                    var basePerms = new GuildPermissions(
                        viewChannel: true,
                        createInstantInvite: true,
                        sendMessages: true,
                        sendMessagesInThreads: true,
                        attachFiles: true,
                        addReactions: true,
                        readMessageHistory: true
                    );

                    var createdRole = await server.CreateRoleAsync(
                        name: newCategoryName,
                        permissions: basePerms,
                        color: null,
                        isHoisted: false,
                        isMentionable: true
                    );

                    categoryRole = server.GetRole(createdRole.Id); // refetch as SocketRole
                    Console.WriteLine($"🧩 Created role '{categoryRole.Name}' (id: {categoryRole.Id})");

                    var allowRole = new OverwritePermissions(
                        viewChannel: PermValue.Allow,
                        sendMessages: PermValue.Allow
                    );
                    await newCategory.AddPermissionOverwriteAsync(categoryRole, allowRole);
                    Console.WriteLine($"✅ Granted '{categoryRole.Name}' access to the new category.");
                }
                catch (Discord.Net.HttpException ex)
                {
                    Console.WriteLine($"❌ CreateRoleAsync failed: HTTP {ex.HttpCode}, DiscordCode {(ex.DiscordCode?.ToString() ?? "n/a")} — {ex.Reason}");
                    Console.WriteLine("   → Fix on server: enable Manage Roles on the bot’s top non-managed role and place it ABOVE other roles.");
                }
            }
        }

        // 4) Build ordered list of channels from the source category by position
        var text = server.TextChannels
            .Where(c => c.CategoryId == sourceCategory.Id && c is not SocketNewsChannel)
            .Cast<SocketGuildChannel>();
        var news = server.TextChannels
            .Where(c => c.CategoryId == sourceCategory.Id)
            .OfType<SocketNewsChannel>()
            .Cast<SocketGuildChannel>();
        var voice = server.VoiceChannels
            .Where(c => c.CategoryId == sourceCategory.Id)
            .Cast<SocketGuildChannel>();
        var forum = server.ForumChannels
            .Where(c => c.CategoryId == sourceCategory.Id)
            .Cast<SocketGuildChannel>();

        var ordered = text.Concat(news).Concat(voice).Concat(forum)
                          .OrderBy(c => c.Position)
                          .ToList();

        // 5) Create each channel (no custom overwrites), then optionally sync to category
        foreach (var ch in ordered)
        {
            switch (ch)
            {
                case SocketNewsChannel newsCh:
                    {
                        var created = await server.CreateNewsChannelAsync(newsCh.Name, props =>
                        {
                            props.CategoryId = newCategory.Id;
                            props.Topic = newsCh.Topic;
                            // do NOT set PermissionOverwrites here – we want inheritance
                        });
                        Console.WriteLine($"📰 Cloned announcement channel: {created.Name}");

                        if (_syncChannelsToCategory)
                        {
                            await created.SyncPermissionsAsync();
                            Console.WriteLine("   → Permissions synced to category.");
                        }
                        break;
                    }

                case SocketTextChannel textCh when ch is not SocketNewsChannel:
                    {
                        var created = await server.CreateTextChannelAsync(textCh.Name, props =>
                        {
                            props.CategoryId = newCategory.Id;
                            props.Topic = textCh.Topic;
                            props.SlowModeInterval = textCh.SlowModeInterval;
                            props.IsNsfw = textCh.IsNsfw;
                            // no PermissionOverwrites – inherit from category
                        });
                        Console.WriteLine($"💬 Cloned text channel: {created.Name}");

                        if (_syncChannelsToCategory)
                        {
                            await created.SyncPermissionsAsync();
                            Console.WriteLine("   → Permissions synced to category.");
                        }
                        break;
                    }

                case SocketVoiceChannel voiceCh:
                    {
                        var created = await server.CreateVoiceChannelAsync(voiceCh.Name, props =>
                        {
                            props.CategoryId = newCategory.Id;
                            props.Bitrate = voiceCh.Bitrate;
                            props.UserLimit = voiceCh.UserLimit;
                            // no PermissionOverwrites – inherit from category
                        });
                        Console.WriteLine($"🔊 Cloned voice channel: {created.Name}");

                        if (_syncChannelsToCategory)
                        {
                            await created.SyncPermissionsAsync();
                            Console.WriteLine("   → Permissions synced to category.");
                        }
                        break;
                    }

                case SocketForumChannel forumCh:
                    {
                        // Phase 1: create forum with NO custom overwrites (inherit), then sync
                        var createdForum = await server.CreateForumChannelAsync(forumCh.Name, props =>
                        {
                            props.CategoryId = newCategory.Id;
                            props.Topic = forumCh.Topic;
                            if (forumCh.DefaultSortOrder.HasValue)
                                props.DefaultSortOrder = forumCh.DefaultSortOrder.Value;
                            // no PermissionOverwrites – inherit from category
                        });

                        Console.WriteLine($"🗂️  Created forum channel: {createdForum.Name} (id: {createdForum.Id})");

                        if (_syncChannelsToCategory)
                        {
                            await createdForum.SyncPermissionsAsync();
                            Console.WriteLine("   → Permissions synced to category.");
                        }

                        // Phase 2: apply forum available tags
                        if (forumCh.Tags.Any())
                        {
                            var tagsPayload = forumCh.Tags.Select(t =>
                            {
                                object? emojiObj = null;
                                if (t.Emoji is Emote emote) emojiObj = new { id = emote.Id.ToString(), name = emote.Name };
                                else if (t.Emoji is Emoji emoji) emojiObj = new { name = emoji.Name };
                                return new { name = t.Name, emoji = emojiObj, moderated = t.IsModerated };
                            }).ToArray();

                            bool ok = await PatchChannelAvailableTagsAsync(createdForum.Id, tagsPayload);
                            Console.WriteLine(ok
                                ? $"   → Tags applied for forum {createdForum.Name}"
                                : $"   → Failed to apply tags for forum {createdForum.Name}");
                        }
                        else
                        {
                            Console.WriteLine($"   → Forum {createdForum.Name} has no tags; skipped tags apply.");
                        }

                        break;
                    }

                default:
                    Console.WriteLine($"ℹ️  Skipped unsupported channel type: {ch.Name} ({ch.GetType().Name})");
                    break;
            }
        }

        Console.WriteLine($"✅ Category '{newCategoryName}' cloned from '{sourceCategoryName}' in source order.");
    }

    private async Task<bool> PatchChannelAvailableTagsAsync(ulong channelId, object[] tagsPayload)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _token);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("GuildBuilderBot/1.0");

        var body = new { available_tags = tagsPayload };
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var req = new HttpRequestMessage(HttpMethod.Patch, $"https://discord.com/api/v10/channels/{channelId}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var res = await http.SendAsync(req);
        if (res.IsSuccessStatusCode) return true;

        string txt;
        try { txt = await res.Content.ReadAsStringAsync(); }
        catch { txt = "<no body>"; }

        Console.WriteLine($"PATCH tags failed: {res.StatusCode} - {txt}");
        return false;
    }

    private void PrintGuildPermsAndRoleStack(SocketGuild server)
    {
        var me = server.CurrentUser;
        Console.WriteLine($"ℹ️  [DIAG] Admin:{me.GuildPermissions.Administrator} ManageRoles:{me.GuildPermissions.ManageRoles} ManageChannels:{me.GuildPermissions.ManageChannels} ManageThreads:{me.GuildPermissions.ManageThreads}");
        Console.WriteLine($"ℹ️  [DIAG] My roles (top→bottom):");
        foreach (var r in server.Roles.OrderByDescending(r => r.Position))
        {
            var mine = me.Roles.Any(rr => rr.Id == r.Id) ? "*" : " ";
            Console.WriteLine($"{mine} pos={r.Position} name={r.Name} managed={r.IsManaged} perms={r.Permissions.RawValue}");
        }
        if (me.Roles.All(r => r.IsManaged))
        {
            Console.WriteLine("⚠️  The bot only has MANAGED roles (integration). Add a normal role with Manage Roles and place it above others.");
        }
    }
}
