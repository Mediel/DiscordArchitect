using Discord;
using Discord.WebSocket;
using DiscordArchitect.Options;
using Microsoft.Extensions.Logging;

namespace DiscordArchitect.Services
{
    /// <summary>
    /// Provides functionality to clone a Discord category and its channels within a guild, including optional role
    /// creation and permission configuration.
    /// </summary>
    /// <remarks>This class is intended for use in Discord bot scenarios where duplicating an existing
    /// category structure—including text, voice, news, and forum channels—is required. It supports copying channel
    /// order, synchronizing permissions, and optionally creating a dedicated role for the new category. Thread safety
    /// is not guaranteed; each instance is intended for use within a single operation context.</remarks>
    public sealed class CategoryCloner
    {
        private readonly ILogger<CategoryCloner> _log;
        private readonly DiagnosticsService _diag;
        private readonly PermissionPlanner _perms;
        private readonly ForumTagService _forumTags;

        public CategoryCloner(
            ILogger<CategoryCloner> log,
            DiagnosticsService diag,
            PermissionPlanner perms,
            ForumTagService forumTags)
        {
            _log = log;
            _diag = diag;
            _perms = perms;
            _forumTags = forumTags;
        }

        /// <summary>
        /// Asynchronously clones a category and its channels from a source category in the specified Discord server,
        /// creating a new category with the given name and applying the provided options.
        /// </summary>
        /// <remarks>If the source category is not found, the method logs an error and returns without
        /// making changes. The method preserves the order and type of channels when cloning. Depending on the options,
        /// it can create a dedicated role for the new category and synchronize permissions for the cloned channels. The
        /// method requires appropriate permissions for the bot to create categories, channels, and roles as specified
        /// by the options.</remarks>
        /// <param name="server">The Discord server in which the category and its channels will be cloned.</param>
        /// <param name="sourceCategoryName">The name of the existing category to clone. The search is case-sensitive and uses ordinal comparison.</param>
        /// <param name="newCategoryName">The name for the newly created category that will receive the cloned channels.</param>
        /// <param name="opt">Options that control cloning behavior, such as whether to create a role for the new category, grant
        /// @everyone access, and synchronize channel permissions.</param>
        /// <returns>A task that represents the asynchronous clone operation. The task completes when the category and its
        /// channels have been cloned.</returns>
        public async Task CloneAsync(SocketGuild server, string sourceCategoryName, string newCategoryName, DiscordOptions opt)
        {
            _diag.PrintGuildPermsAndRoleStack(server);

            var source = server.CategoryChannels
                .FirstOrDefault(c => string.Equals(c.Name, sourceCategoryName, StringComparison.Ordinal));

            if (source == null)
            {
                _log.LogError("❌ Source category '{Name}' not found.", sourceCategoryName);
                foreach (var c in server.CategoryChannels.OrderBy(c => c.Position))
                    _log.LogInformation(" - {Cat}", c.Name);
                return;
            }

            // 1) Create target category
            var newCategory = await server.CreateCategoryChannelAsync(newCategoryName);
            _log.LogInformation("📁 Created category: {Name} (id: {Id})", newCategory.Name, newCategory.Id);

            // 2) Ensure bot + everyone toggles on the category
            var me = server.CurrentUser;

            // IMPORTANT: newCategory is RestCategoryChannel; cast to ICategoryChannel for helper methods
            await _perms.EnsureBotAccessAsync((ICategoryChannel)newCategory, me);
            await _perms.ApplyEveryoneToggleAsync((ICategoryChannel)newCategory, server, opt.EveryoneAccessToNewCategory);

            if (opt.EveryoneAccessToNewCategory)
                _log.LogInformation("👥 @everyone can access the new category.");

            // 3) Optional role per category
            IRole? categoryRole = null;
            if (opt.CreateRolePerCategory)
            {
                if (!me.GuildPermissions.ManageRoles)
                {
                    _log.LogWarning("⚠️ Skipping role creation: bot lacks Manage Roles (268435456).");
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

                        categoryRole = server.GetRole(createdRole.Id);
                        _log.LogInformation("🧩 Created role '{Role}' (id: {Id})", categoryRole!.Name, categoryRole!.Id);

                        await _perms.GrantCategoryRoleAsync((ICategoryChannel)newCategory, categoryRole);
                        _log.LogInformation("✅ Granted '{Role}' access to the new category.", categoryRole.Name);
                    }
                    catch (Discord.Net.HttpException ex)
                    {
                        _log.LogError("❌ CreateRoleAsync failed: HTTP {Code}, DiscordCode {DCode} — {Reason}", ex.HttpCode, ex.DiscordCode, ex.Reason);
                        _log.LogError("   → Fix: give the bot a non-managed role with Manage Roles placed ABOVE other roles.");
                    }
                }
            }

            // 4) Collect channels from source (preserve order)
            var text = server.TextChannels
                .Where(c => c.CategoryId == source.Id && c is not SocketNewsChannel)
                .Cast<SocketGuildChannel>();

            var news = server.TextChannels
                .Where(c => c.CategoryId == source.Id)
                .OfType<SocketNewsChannel>()
                .Cast<SocketGuildChannel>();

            var voice = server.VoiceChannels
                .Where(c => c.CategoryId == source.Id)
                .Cast<SocketGuildChannel>();

            var forum = server.ForumChannels
                .Where(c => c.CategoryId == source.Id)
                .Cast<SocketGuildChannel>();

            var ordered = text.Concat(news).Concat(voice).Concat(forum)
                              .OrderBy(c => c.Position)
                              .ToList();

            // 5) Create channels and (optionally) sync to category
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
                                // inherit permissions from category (no per-channel overwrites here)
                            });

                            _log.LogInformation("📰 Cloned announcement channel: {Name}", created.Name);

                            if (opt.SyncChannelsToCategory)
                                await created.SyncPermissionsAsync();

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
                                // inherit permissions from category
                            });

                            _log.LogInformation("💬 Cloned text channel: {Name}", created.Name);

                            if (opt.SyncChannelsToCategory)
                                await created.SyncPermissionsAsync();

                            break;
                        }

                    case SocketVoiceChannel voiceCh:
                        {
                            var created = await server.CreateVoiceChannelAsync(voiceCh.Name, props =>
                            {
                                props.CategoryId = newCategory.Id;
                                props.Bitrate = voiceCh.Bitrate;
                                props.UserLimit = voiceCh.UserLimit;
                                // inherit permissions from category
                            });

                            _log.LogInformation("🔊 Cloned voice channel: {Name}", created.Name);

                            if (opt.SyncChannelsToCategory)
                                await created.SyncPermissionsAsync();

                            break;
                        }

                    case SocketForumChannel forumCh:
                        {
                            var createdForum = await server.CreateForumChannelAsync(forumCh.Name, props =>
                            {
                                props.CategoryId = newCategory.Id;
                                props.Topic = forumCh.Topic;
                                if (forumCh.DefaultSortOrder.HasValue)
                                    props.DefaultSortOrder = forumCh.DefaultSortOrder.Value;
                                // inherit permissions from category
                            });

                            _log.LogInformation("🗂️  Created forum channel: {Name} (id: {Id})", createdForum.Name, createdForum.Id);

                            if (opt.SyncChannelsToCategory)
                                await createdForum.SyncPermissionsAsync();

                            // Apply available tags (REST PATCH)
                            if (forumCh.Tags.Any())
                            {
                                var tagsPayload = forumCh.Tags.Select(t =>
                                {
                                    object? emojiObj = null;
                                    if (t.Emoji is Emote emote) emojiObj = new { id = emote.Id.ToString(), name = emote.Name };
                                    else if (t.Emoji is Emoji emoji) emojiObj = new { name = emoji.Name };
                                    return new { name = t.Name, emoji = emojiObj, moderated = t.IsModerated };
                                }).ToArray();

                                var ok = await _forumTags.PatchAvailableTagsAsync(createdForum.Id, tagsPayload);
                                _log.LogInformation(ok
                                    ? "   → Tags applied for forum {Name}"
                                    : "   → Failed to apply tags for forum {Name}", createdForum.Name);
                            }
                            else
                            {
                                _log.LogInformation("   → Forum {Name} has no tags; skipped tags apply.", createdForum.Name);
                            }

                            break;
                        }

                    default:
                        _log.LogInformation("ℹ️  Skipped unsupported channel type: {Name} ({Type})", ch.Name, ch.GetType().Name);
                        break;
                }
            }

            _log.LogInformation("✅ Category '{New}' cloned from '{Src}' in source order.", newCategoryName, sourceCategoryName);
        }
    }
}
