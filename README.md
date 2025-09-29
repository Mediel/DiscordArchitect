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
* 🔒 **No secrets in Git** — token & guild ID live in **UserSecrets** only.

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

1. Developer Portal → **New Application** → name it.
2. **Bot** → **Add Bot**.
3. **Reset/Copy Token** and store it safely (reset immediately if leaked).

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

### 3) Secrets (UserSecrets) & config

Run in the project directory:

```bash
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Discord:ServerId" "123456789012345678"
```

`appsettings.json` (safe to commit):

```json
{
  "Discord": {
    "SourceCategoryName": "Template",
    "CreateRolePerCategory": true,
    "EveryoneAccessToNewCategory": false,
    "SyncChannelsToCategory": true
  }
}
```

### 4) Packages

Below is the complete list of NuGet packages this solution uses. Versions are aligned for consistency.

**Required**

* `Discord.Net` **3.18.0** — Discord API client (Socket + REST)
* `Microsoft.Extensions.Configuration` **9.0.9** — base configuration primitives
* `Microsoft.Extensions.Configuration.Json` **9.0.9** — enables `AddJsonFile("appsettings.json", ...)`
* `Microsoft.Extensions.Configuration.UserSecrets` **9.0.9** — enables `AddUserSecrets<Program>()`
* `Microsoft.Extensions.Hosting` **9.0.9** — generic host & `IHostedService`
* `Microsoft.Extensions.Logging.Console` **9.0.9** — console logger provider
* `Microsoft.Extensions.Options` **9.0.9** — `IOptions<T>` pattern

**Optional**

* `Microsoft.Extensions.Configuration.EnvironmentVariables` **9.0.9** — load env vars into config
* `Microsoft.Extensions.Configuration.CommandLine` **9.0.9** — parse CLI args into config
* `Microsoft.Extensions.Configuration.Binder` **9.0.9** — if you later bind `DiscordOptions` via `Bind()`

**`.csproj` snippet:**

```xml
<ItemGroup>
  <!-- Discord -->
  <PackageReference Include="Discord.Net" Version="3.18.0" />

  <!-- Configuration -->
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.9" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.9" />
  <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.9" />
  <!-- Optional -->
  <!-- <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.9" /> -->
  <!-- <PackageReference Include="Microsoft.Extensions.Configuration.CommandLine" Version="9.0.9" /> -->
  <!-- <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.9" /> -->

  <!-- Hosting & Logging & Options -->
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.9" />
  <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.9" />
  <PackageReference Include="Microsoft.Extensions.Options" Version="9.0.9" />
</ItemGroup>
```

---

## Run

```bash
dotnet build
dotnet run
```

You’ll see login logs and be prompted: **“Enter new category name:”**. Type it (e.g., `Event‑042`).

The app will:

1. Find the `SourceCategoryName` template category.
2. Create a new category.
3. (Optionally) create a same‑named role and grant it access; enforce `@everyone` toggle.
4. Clone text/news/voice/forum channels **in template order**.
5. For forum channels, **PATCH available tags** via REST.

---

## How it works

* `DiscordSocketClient` with `GatewayIntents.Guilds` only.
* Clone with `CreateTextChannelAsync`, `CreateNewsChannelAsync`, `CreateVoiceChannelAsync`, `CreateForumChannelAsync`.
* Permission model: new channels are created without per‑channel overwrites → they **inherit** from the new category; optionally call `SyncPermissionsAsync()` on each created channel.
* Forum tags: PATCH `available_tags` with `Authorization: Bot <token>`.

---

## Security

* Never commit your token or ServerId.
* If your token leaks, reset it in the Developer Portal.
* Store secrets in a password manager; this repo reads them from **UserSecrets** only.

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

**“❌ Token not found”**
Set the secret: `dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"`.

**“❌ ServerId not found”**
Set the guild ID: `dotnet user-secrets set "Discord:ServerId" "123456789012345678"`.

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
    "SyncChannelsToCategory": true
  }
}
```

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

Happy clonin
