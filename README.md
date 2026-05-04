# GuildBuilder — Discord Category/Channel Cloner (C# / .NET)

**TL;DR:** A small console app that clones a *template* **category** on your Discord server (guild) into a brand‑new category — including **text**, **announcement/news**, **voice**, and **forum** channels, their **topics**, **slow‑mode**, **bitrate/user limits**, **permission overwrites**, and (for forums) **available tags** via a REST patch.

---

## What this app does

* ✅ Connects to your Discord server using your **bot token**.
* ✅ Finds a **source (template) category** by name (from `appsettings.json`).
* ✅ Creates a **new category** (you enter its name at runtime).
* ✅ Clones channels **in the same order** as in the template:

  * **Text channels** — name, topic, slow mode, NSFW; new channels inherit permissions from the category by default.
  * **Announcement/News channels** (type 5).
  * **Voice channels** — name, bitrate, user limit.
  * **Forum channels** — name, topic, default sort order, and **available tags** (patched via Discord REST API v10).
* 🔒 **No secrets in Git** — `Discord:Token` and `Discord:ServerId` belong in **`dotnet user-secrets`** (or private env overrides), not in committed `appsettings.json`.

---

## Project structure

```
DiscordArchitect/
├─ Program.cs
├─ DiscordArchitect.csproj
├─ appsettings.json
├─ Options/
│  └─ DiscordOptions.cs
├─ Hosting/
│  └─ DiscordHostedService.cs
├─ Discord/
│  ├─ DiscordClientFactory.cs
│  └─ RestClientFactory.cs
├─ Services/
│  ├─ CategoryCloner.cs
│  ├─ ForumTagService.cs
│  ├─ PermissionPlanner.cs
│  ├─ DiagnosticsService.cs
│  └─ Prompt.cs
└─ Utils/
   └─ HttpExtensions.cs
```

**Responsibilities**

* `Program.cs` — builds the Host, wires DI, reads config (UserSecrets + appsettings), starts hosted service.
* `DiscordHostedService` — logs in the bot, waits for Gateway **Ready**, prompts for a new category name, calls `CategoryCloner`.
* `CategoryCloner` — core logic: find template, create target category, toggles (role/@everyone), clone channels in order, apply forum tags.
* `PermissionPlanner` — minimal helpers to set category overwrites (bot, @everyone, optional per‑category role).
* `ForumTagService` — PATCH `/api/v10/channels/{id}` with `available_tags`.
* `DiagnosticsService` — prints bot guild permissions & role stack.
* `DiscordClientFactory` & `RestClientFactory` — construct `DiscordSocketClient` and preconfigured `HttpClient`.

---

## Requirements

* **.NET SDK 10.0+**
* A **Discord Application** with a **Bot** user ([Discord Developer Portal](https://discord.com/developers/applications)).
* Bot invited to the target guild with permissions below.
* If you want to clone **Announcement/News channels**, the guild must have **Community** features enabled.

### Gateway Intents

Only the standard **Guilds** intent is needed. **Privileged intents** can stay **OFF**:

* Presence — OFF
* Server Members (GUILD_MEMBERS) — OFF
* Message Content — OFF

---

## Install & Configure

### 1) Create a Discord bot + token

Official entry points (bookmark these):

* [Discord Developer Portal — Applications](https://discord.com/developers/applications) — create/select your app, configure the **Bot**, copy the token.
* [Discord Developer Documentation — Introduction](https://discord.com/developers/docs/intro) — overview of apps, bots, and API concepts.
* [OAuth2 and bot accounts](https://discord.com/developers/docs/topics/oauth2#bots) — how bot tokens relate to the OAuth2 model.

Steps in the portal:

1. **New Application** → name it (this is only the app name in the dashboard).
2. Left sidebar → **Bot** → **Add Bot** (if you have not already).
3. Under **TOKEN**, use **Reset Token** or **Copy** and store it safely. If it ever leaks, **reset it here** immediately.

### 2) Invite the bot to your guild

**Invite URL pattern** (replace `YOUR_CLIENT_ID` and integer):

```
https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=PERMISSIONS_INT
```

**Permission presets**

* Minimal (clone categories/channels): **1040**

  * `View Channels (1024)` + `Manage Channels (16)`

  ```
  https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1040
  ```

* Forum‑friendly: **1073742864**

  * `Manage Threads (1073741824)`

  ```
  https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1073742864
  ```

* If you also **create roles**: **1342178320**

  * `Manage Roles (268435456)`

  ```
  https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1342178320
  ```

> Notes
>
> * The **Bot Permissions** checkboxes in the Developer Portal only help **build the URL**; they do **not** change live server permissions. Real permissions = the bot’s role(s) on the server + the permissions integer used at invite time.
> * To create/edit roles, the bot needs a **non‑managed role** with **Manage Roles** placed **above** the roles it will manage.

### 3) Where to get `Discord:Token` and `Discord:ServerId`

These two values are what the app reads as **`Discord:Token`** (bot token) and **`Discord:ServerId`** (numeric guild/server ID).

**Bot token → `Discord:Token`**

* Same as in step **1)** above: [Developer Portal](https://discord.com/developers/applications) → your application → **Bot** → copy or reset the **token**. That string is your `Discord:Token`.

**Server (guild) ID → `Discord:ServerId`**

* Official help: [Discord Support — Where can I find my User/Server/Message ID?](https://support.discord.com/hc/en-us/articles/206346498-Where-can-I-find-my-User-Server-Message-ID-)
* In the Discord app: **User Settings** (gear) → **App Settings** → **Advanced** → turn **Developer Mode** **On**.
* Right‑click your **server icon** in the left sidebar (not a channel) → **Copy Server ID**. Store that 17–19 digit string as `Discord:ServerId` via **user-secrets** (step **4)**).

**Invite link only — Client ID (not the same as token)**

* [Developer Portal](https://discord.com/developers/applications) → your app → **OAuth2** → **General** → **Client ID**. Use it in the invite URLs in step **2)**. More detail: [OAuth2 documentation](https://discord.com/developers/docs/topics/oauth2).

---

### 4) Secrets ([User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)) & config

For a **public** clone of this repo, keep **`Discord:Token`** and **`Discord:ServerId`** out of Git. Set them locally:

```bash
cd DiscordArchitect
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Discord:ServerId" "123456789012345678"
```

`appsettings.json` in the repo stays **free of secrets** — only non‑sensitive options, safe to commit:

```json
{
  "Discord": {
    "SourceCategoryName": "Template",
    "CreateRolePerCategory": true,
    "EveryoneAccessToNewCategory": false,
    "SyncChannelsToCategory": true,
    "TestMode": false
  }
}
```

(Later configuration sources override earlier ones; see [`DiscordArchitect/Program.cs`](DiscordArchitect/Program.cs).)

### 5) NuGet packages

Exact versions live in [`DiscordArchitect/DiscordArchitect.csproj`](DiscordArchitect/DiscordArchitect.csproj). At a glance:

* **Discord.Net** — Socket + REST client for Discord.
* **Microsoft.Extensions.*** — hosting, configuration (including user-secrets), logging, options.
* **Serilog** + sinks / enrichers — structured and file/console logging.

---

## Run

### Normal Mode
```bash
dotnet build
dotnet run
```

### Test Mode
```bash
# Command line
dotnet run -- --test-mode

# Or via configuration
dotnet run -- --TestMode=true
```

You'll see login logs and be prompted: **"Enter new category name:"**. Type it (e.g., `Event‑042`).

The app will:

1. Find the `SourceCategoryName` template category.
2. Create a new category.
3. (Optionally) create a same‑named role and grant it access; enforce `@everyone` toggle.
4. Clone text/news/voice/forum channels **in template order**.
5. For forum channels, **PATCH available tags** via REST.

### Test Mode Features

When running in test mode (`--test-mode` or `TestMode: true` in configuration):

1. **Resource Tracking**: All created resources (category, channels, role) are tracked
2. **Post-Run Verification**: Automatic verification that all resources exist and have correct permissions
3. **Verification Prompt**: After creation, you'll be asked to verify resources in Discord
4. **Cleanup Option**: You can choose to delete all created resources automatically
5. **Safe Testing**: Perfect for testing configurations without cluttering your server

**Test Mode Flow:**
```
🧪 Running in TEST MODE - resources will be tracked for cleanup
✅ Test mode: Resources created successfully!
   📁 Category: 123456789012345678
   📺 Channels: 3
   🧩 Role: 987654321098765432

🔍 Running post-creation verification...
📊 Verification Results:
  ✅ [Category] Category 'TestCategory' exists and is accessible
  ✅ [Channel] Channel 'general' exists and is accessible
  ✅ [Channel] Channel 'voice' exists and is accessible
  ✅ [Role] Role 'TestCategory' exists
  ✅ [Permissions] All channels are synced to category
  ℹ️ [Channels] Verified 2/2 channels
📋 Verification Summary: 5 ✅ Success, 0 ⚠️ Warnings, 0 ❌ Errors, 1 ℹ️ Info

🔍 Please verify the created resources in Discord, then press ENTER to continue...

🗑️  Do you want to delete the created resources? (y/n): y
🧹 Starting cleanup...
✅ Deleted category: TestCategory (ID: 123456789012345678)
✅ Deleted channel: general (ID: 111111111111111111)
✅ Deleted channel: voice (ID: 222222222222222222)
✅ Deleted role: TestCategory (ID: 987654321098765432)
🎉 Cleanup completed successfully!
```

---

## Post-Run Verification

The application now includes comprehensive post-run verification that automatically checks:

### ✅ **Resource Existence**
- Verifies that all created categories, channels, and roles exist
- Checks for any missing or deleted resources

### 🔍 **Permission Validation**
- Validates that channels are properly synced to their category
- Checks @everyone access permissions
- Verifies role permissions on categories

### 📊 **Visibility Checks**
- Ensures channels are visible to appropriate users
- Identifies hidden channels that may need attention
- Validates permission inheritance

### 💡 **Smart Recommendations**
- Provides actionable recommendations for permission issues
- Suggests fixes for common configuration problems
- Helps optimize Discord server setup

### 📋 **Detailed Reporting**
- Color-coded verification results (✅ Success, ⚠️ Warning, ❌ Error, ℹ️ Info)
- Summary statistics of verification findings
- Clear categorization of issues by type

**Example Verification Output:**
```
📊 Verification Results:
  ✅ [Category] Category 'MyCategory' exists and is accessible
  ✅ [Channel] Channel 'general' exists and is accessible
  ⚠️ [Channel] Channel 'private' is hidden from @everyone
  ✅ [Role] Role 'MyCategory' exists
  ✅ [Permissions] All channels are synced to category
  ℹ️ [Channels] Verified 2/2 channels

💡 Recommendations:
  • Consider reviewing permissions for 1 hidden channels.

📋 Verification Summary: 4 ✅ Success, 1 ⚠️ Warnings, 0 ❌ Errors, 1 ℹ️ Info
```

---

## How it works

* `DiscordSocketClient` with `GatewayIntents.Guilds` only.
* Clone with `CreateTextChannelAsync`, `CreateNewsChannelAsync`, `CreateVoiceChannelAsync`, `CreateForumChannelAsync`.
* Permission model: new channels are created without per‑channel overwrites → they **inherit** from the new category; optionally call `SyncPermissionsAsync()` on each created channel.
* Forum tags: PATCH `available_tags` with `Authorization: Bot <token>`.

---

## Security

* Never commit your token or ServerId.
* If your token leaks, reset it in the [Developer Portal](https://discord.com/developers/applications) → **Bot** → **Reset Token**.
* Store secrets in a password manager; this workflow expects **`Discord:Token`** and **`Discord:ServerId`** in **UserSecrets**, not in tracked files.

`.gitignore` essentials:

```gitignore
.vs/
bin/
obj/
TestResults/
appsettings.Development.json
```

---

## Troubleshooting

**“❌ Token not found” / validation says `Discord:Token` is required**
Run `dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"`. For where the value comes from, see step **3)** above.

**“❌ ServerId not found” / `Discord:ServerId` is required**
Run `dotnet user-secrets set "Discord:ServerId" "123456789012345678"`. See step **3)** for **Copy Server ID**.

**“❌ Server not found” / Bot offline**
Ensure the bot is invited and not disabled; verify the `ServerId`.

**Category not found**
Check the exact `Discord:SourceCategoryName` (case‑sensitive match on the raw string).

**Forum tags didn’t clone**
The app clones the forum channel, then PATCHes `available_tags`. Ensure **Manage Channels** (and usually **Manage Threads**).

**Announcement channel creation fails (403/50013)**
The guild must have **Community** enabled; verify **Manage Channels**.

**Role creation fails — `Discord.Net.HttpException 50013: Missing Permissions`**
Give the bot a **non‑managed** role with **Manage Roles (268435456)** placed **above** target roles; or reinvite with the **1342178320** preset.

**Namespace collision: `DiscordArchitect.Discord` vs `Discord` (Discord.Net)**
If you ever see *“The type or namespace name 'Net' does not exist in the namespace 'DiscordArchitect.Discord'”*, qualify the exception as `global::Discord.Net.HttpException` or rename your app folder (e.g., `Infrastructure`).

**Why don’t Bot Permissions checkboxes persist in the Developer Portal?**
They only build the **invite URL**; they don’t change live server permissions. Adjust the bot’s **server role** instead.

**Rate limits**
Discord throttles bursts; the client handles it. Large templates may take a bit.

---

## Limitations

* Clones **structure & metadata**, not messages/threads content.
* Only **same‑guild** cloning (no cross‑guild).
* With `SyncChannelsToCategory = true`, individual overwrites from the template are not copied; inheritance from the new category is enforced.

---

## Example `appsettings.json`

```json
{
  "Discord": {
    "SourceCategoryName": "Template",
    "CreateRolePerCategory": true,
    "EveryoneAccessToNewCategory": false,
    "SyncChannelsToCategory": true,
    "TestMode": false
  }
}
```

Set **`Discord:Token`** and **`Discord:ServerId`** with `dotnet user-secrets` (see **Install & Configure** step **4)**); do not add them here in a public fork.

---

## Commands

```bash
# Build & run
dotnet build
dotnet run

# Initialize secrets
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Discord:ServerId" "123456789012345678"
```

Happy cloning.
