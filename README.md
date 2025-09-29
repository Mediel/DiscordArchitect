# GuildBuilder — Discord Category/Channel Cloner (C# / .NET)

**TL;DR:** A small console app that clones a *template* **category** on your Discord server (guild) into a brand‑new category — including **text**, **announcement/news**, **voice**, and **forum** channels, their **topics**, **slow‑mode**, **bitrate/user limits**, **permission overwrites**, and (for forums) **available tags** via a REST patch.

---

## What this app does

* ✅ Connects to your Discord server using your **bot token**.
* ✅ Finds a **source (template) category** by name (from `appsettings.json`).
* ✅ Creates a **new category** (you enter its name at runtime).
* ✅ Clones, in the **same order** as the template:

  * **Text channels** — name, topic, slow mode, permission overwrites (or inherited, see toggles).
  * **Announcement/News channels** (type 5).
  * **Voice channels** — name, bitrate, user limit, permission overwrites (or inherited).
  * **Forum channels** — name, topic, default sort order, permission overwrites (or inherited), and **available tags** (patched via Discord REST API v10).
* 🔒 **No secrets in Git** — token & guild ID live in **UserSecrets** only.

---

## What’s new

* **Announcement/News cloning.**
* **Preserved channel order** (mirrors template order).
* **Optional per‑category role** — role named after the new category with sensible base permissions (view channels, create invite, send messages, send messages in threads, attach files, add reactions, read message history) and explicit access to the new category.
* **`@everyone` access toggle** — let everyone see the new category, or deny it by default.
* **Safer defaults** — bot gets explicit allow on the new category to avoid `Missing Permissions (50013)`.
* **Optional sync** — auto‑`SyncPermissionsAsync()` on each created channel to inherit category permissions consistently.

---

## Requirements

* **.NET SDK 10.0+**
* A **Discord Application** with a **Bot** user ([Discord Developer Portal](https://discord.com/developers/applications))
* Bot **invited** to the target server with sufficient **permissions**
* On the server: bot needs **View Channels**, **Manage Channels**, and (for forums) **Manage Threads**
* To clone **Announcement/News channels**, the guild must have **Community** features enabled.

### Gateway Intents

Only the standard **Guilds** intent is needed. **Privileged intents** can stay **OFF**:

* Presence — OFF
* Server Members (GUILD_MEMBERS) — OFF
* Message Content — OFF

---

## Quick Start

```bash
# 1) Clone & restore
# git clone <your repo>
# cd GuildBuilder

# 2) Secrets (never committed)
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Discord:ServerId" "123456789012345678"  # numeric guild ID

# 3) Configure template & toggles
# edit appsettings.json

# 4) Build & run
dotnet build
dotnet run
```

When prompted, type the **new category name** (e.g., `Event‑042`). The tool will create the category, (optionally) create and grant a matching role, clone channels in order, and sync permissions.

---

## Install & Configure (step‑by‑step)

### 1) Create a Discord bot + get the token

1. Developer Portal → **New Application** → name it.
2. **Bot** → **Add Bot** (confirm).
3. On **Bot** page, **Reset/Copy Token** and store safely. If it leaks, **reset** it.

### 2) Invite the bot to your server (guild)

**Invite URL pattern (replace `YOUR_CLIENT_ID` and the integer):**

```
https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=PERMISSIONS_INT
```

**Permission presets:**

* Minimal (clone categories/channels): **1040**
  `https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1040`

* Forum‑friendly (Manage Threads): **1073742864**
  `https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1073742864`

* Also create roles (Manage Roles): **1342178320**
  `https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1342178320`

> Notes
>
> * `Manage Channels` covers creating/moving channels, topics, slow mode, overwrites, and forum tag PATCH.
> * `Manage Roles` is **only** required if you create/edit roles.
> * If you hit permission errors, try inviting with **Administrator** (`8`) to verify logic, then reduce to a preset.

### 3) Project info

* Target framework: **net10.0**
* Packages: `Discord.Net (3.18.0)`, `Microsoft.Extensions.Configuration (9.0.9)`, `...UserSecrets (9.0.9)`

### 4) Secrets (UserSecrets)

```bash
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Discord:ServerId" "123456789012345678"
```

> **ServerId**: Discord → **Developer Mode** → right‑click server icon → **Copy Server ID**.

### 5) Configure `appsettings.json`

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

* `SourceCategoryName` — exact name of the template category to clone.
* `CreateRolePerCategory` — create `NEW_CATEGORY_NAME` role with base perms and grant it category access.
* `EveryoneAccessToNewCategory` — if `false`, `@everyone` is **denied** on the new category.
* `SyncChannelsToCategory` — after channel creation, call `SyncPermissionsAsync()` so every channel inherits the category permissions.

---

## How it works (high level)

* Uses **DiscordSocketClient** (only **Guilds** intent).
* Clones with `CreateTextChannelAsync`, `CreateNewsChannelAsync`, `CreateVoiceChannelAsync`, `CreateForumChannelAsync`.
* **Default**: create channels **without per‑channel overwrites**, then **sync to category** (clean inheritance).
* Forums: PATCH `available_tags` via REST (`PATCH /api/v10/channels/{id}`) with `Authorization: Bot <token>`.

---

## Security & Safety

* **Never commit** your token or server ID.
* If your token leaks, **reset** it in the Developer Portal.
* Store tokens in a password manager; this repo reads them from **UserSecrets** only.

`.gitignore` snippet:

```gitignore
# user secrets/config variants
appsettings.Development.json
*.user
*.suo
*.swp
*.tmp
# build output
bin/
obj/
```

---

## Troubleshooting

**“❌ Token not found”**
Set the secret:

```bash
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
```

**“❌ ServerId not found”**
Set the guild ID:

```bash
dotnet user-secrets set "Discord:ServerId" "123456789012345678"
```

**“❌ Server not found” / Bot is offline**

* Ensure the bot is **invited** and not disabled.
* Verify the **ServerId**.

**Category not found**

* Check the exact `Discord:SourceCategoryName`.

**Forum tags didn’t clone**

* Forums are cloned, then tags are **PATCHed**.
* Ensure **Manage Channels** (and typically **Manage Threads**).

**Announcement channel creation fails (403/50013)**

* Guild must have **Community** enabled.
* Verify **Manage Channels**.

**Role creation fails — `Discord.Net.HttpException 50013: Missing Permissions`**

* The bot needs **Manage Roles (268435456)** **on a non‑managed role** that is **above** the roles it manages.
* Fix on server: create a normal role (e.g., `GuildBuilder`) with **Manage Roles**, assign it to the bot, and move it **above** other roles. Or reinvite with `permissions=1342178320`.

**“Bot Permissions” checkboxes in Developer Portal don’t persist**

* They only help build the **invite URL**; they don’t change live server permissions.
* Real permissions = the bot’s server role(s) and the permissions integer used at invite time.

**New category is invisible to everyone**

* You set `EveryoneAccessToNewCategory = false` and didn’t create/grant any role.
* Enable `CreateRolePerCategory = true` or grant access to another role.

**Permission errors in general**

* First run: try invite with **Administrator**; if it works, reduce to minimal perms.

**Rate limits**

* Large templates can be throttled by Discord; the client handles it, just give it a moment.

---

## Limitations & Notes

* Clones **structure & metadata**, not messages/threads content.
* Only **same‑guild** cloning (no cross‑guild).
* With `SyncChannelsToCategory=true`, individual overwrites from the template aren’t copied; inheritance from the new category is enforced.

---

## Tech details (for devs)

* **Target Framework:** `net10.0`
* **Packages:** `Discord.Net 3.18.0`, `Microsoft.Extensions.Configuration 9.0.9`, `...UserSecrets 9.0.9`
* **Config sources:** `appsettings.json` + **UserSecrets**
* **Runtime prompt:** asks only for the new category name.

---

## Release Notes

### v1.0.0

* Add: cloning of text, announcement/news, voice, forum channels.
* Add: forum tag PATCH via REST.
* Add: preserved channel order.
* Add: toggles `CreateRolePerCategory`, `EveryoneAccessToNewCategory`, `SyncChannelsToCategory`.
* Sec: UserSecrets for token & guild ID, no secrets in Git.

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

## Commands cheat‑sheet

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

Happy cloning! 🚀

# GuildBuilder — Discord Category/Channel Cloner (C# / .NET)

**TL;DR:** A small console app that clones a *template* **category** on your Discord server (guild) into a brand‑new category — including **text**, **announcement/news**, **voice**, and **forum** channels, their **topics**, **slow‑mode**, **bitrate/user limits**, **permission overwrites**, and (for forums) **available tags** via a REST patch.

---

## What this app does

* ✅ Connects to your Discord server using your **bot token**.
* ✅ Finds a **source (template) category** by name (from `appsettings.json`).
* ✅ Creates a **new category** (you enter its name at runtime).
* ✅ Clones, in the **same order** as the template:

  * **Text channels** — name, topic, slow mode, permission overwrites (or inherited, see toggles).
  * **Announcement/News channels** (type 5).
  * **Voice channels** — name, bitrate, user limit, permission overwrites (or inherited).
  * **Forum channels** — name, topic, default sort order, permission overwrites (or inherited), and **available tags** (patched via Discord REST API v10).
* 🔒 **No secrets in Git** — token & guild ID live in **UserSecrets** only.

---

## What’s new

* **Announcement/News cloning.**
* **Preserved channel order** (mirrors template order).
* **Optional per‑category role** — role named after the new category with sensible base permissions (view channels, create invite, send messages, send messages in threads, attach files, add reactions, read message history) and explicit access to the new category.
* **`@everyone` access toggle** — let everyone see the new category, or deny it by default.
* **Safer defaults** — bot gets explicit allow on the new category to avoid `Missing Permissions (50013)`.
* **Optional sync** — auto‑`SyncPermissionsAsync()` on each created channel to inherit category permissions consistently.

---

## Requirements

* **.NET SDK 10.0+**
* A **Discord Application** with a **Bot** user ([Discord Developer Portal](https://discord.com/developers/applications))
* Bot **invited** to the target server with sufficient **permissions**
* On the server: bot needs **View Channels**, **Manage Channels**, and (for forums) **Manage Threads**
* To clone **Announcement/News channels**, the guild must have **Community** features enabled.

### Gateway Intents

Only the standard **Guilds** intent is needed. **Privileged intents** can stay **OFF**:

* Presence — OFF
* Server Members (GUILD_MEMBERS) — OFF
* Message Content — OFF

---

## Quick Start

```bash
# 1) Clone & restore
# git clone <your repo>
# cd GuildBuilder

# 2) Secrets (never committed)
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Discord:ServerId" "123456789012345678"  # numeric guild ID

# 3) Configure template & toggles
# edit appsettings.json

# 4) Build & run
dotnet build
dotnet run
```

When prompted, type the **new category name** (e.g., `Event‑042`). The tool will create the category, (optionally) create and grant a matching role, clone channels in order, and sync permissions.

---

## Install & Configure (step‑by‑step)

### 1) Create a Discord bot + get the token

1. Developer Portal → **New Application** → name it.
2. **Bot** → **Add Bot** (confirm).
3. On **Bot** page, **Reset/Copy Token** and store safely. If it leaks, **reset** it.

### 2) Invite the bot to your server (guild)

**Invite URL pattern (replace `YOUR_CLIENT_ID` and the integer):**

```
https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=PERMISSIONS_INT
```

**Permission presets:**

* Minimal (clone categories/channels): **1040**
  `https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1040`

* Forum‑friendly (Manage Threads): **1073742864**
  `https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1073742864`

* Also create roles (Manage Roles): **1342178320**
  `https://discord.com/oauth2/authorize?client_id=YOUR_CLIENT_ID&scope=bot&permissions=1342178320`

> Notes
>
> * `Manage Channels` covers creating/moving channels, topics, slow mode, overwrites, and forum tag PATCH.
> * `Manage Roles` is **only** required if you create/edit roles.
> * If you hit permission errors, try inviting with **Administrator** (`8`) to verify logic, then reduce to a preset.

### 3) Project info

* Target framework: **net10.0**
* Packages: `Discord.Net (3.18.0)`, `Microsoft.Extensions.Configuration (9.0.9)`, `...UserSecrets (9.0.9)`

### 4) Secrets (UserSecrets)

```bash
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Discord:ServerId" "123456789012345678"
```

> **ServerId**: Discord → **Developer Mode** → right‑click server icon → **Copy Server ID**.

### 5) Configure `appsettings.json`

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

* `SourceCategoryName` — exact name of the template category to clone.
* `CreateRolePerCategory` — create `NEW_CATEGORY_NAME` role with base perms and grant it category access.
* `EveryoneAccessToNewCategory` — if `false`, `@everyone` is **denied** on the new category.
* `SyncChannelsToCategory` — after channel creation, call `SyncPermissionsAsync()` so every channel inherits the category permissions.

---

## How it works (high level)

* Uses **DiscordSocketClient** (only **Guilds** intent).
* Clones with `CreateTextChannelAsync`, `CreateNewsChannelAsync`, `CreateVoiceChannelAsync`, `CreateForumChannelAsync`.
* **Default**: create channels **without per‑channel overwrites**, then **sync to category** (clean inheritance).
* Forums: PATCH `available_tags` via REST (`PATCH /api/v10/channels/{id}`) with `Authorization: Bot <token>`.

---

## Security & Safety

* **Never commit** your token or server ID.
* If your token leaks, **reset** it in the Developer Portal.
* Store tokens in a password manager; this repo reads them from **UserSecrets** only.

`.gitignore` snippet:

```gitignore
# user secrets/config variants
appsettings.Development.json
*.user
*.suo
*.swp
*.tmp
# build output
bin/
obj/
```

---

## Troubleshooting

**“❌ Token not found”**
Set the secret:

```bash
dotnet user-secrets set "Discord:Token" "YOUR_BOT_TOKEN"
```

**“❌ ServerId not found”**
Set the guild ID:

```bash
dotnet user-secrets set "Discord:ServerId" "123456789012345678"
```

**“❌ Server not found” / Bot is offline**

* Ensure the bot is **invited** and not disabled.
* Verify the **ServerId**.

**Category not found**

* Check the exact `Discord:SourceCategoryName`.

**Forum tags didn’t clone**

* Forums are cloned, then tags are **PATCHed**.
* Ensure **Manage Channels** (and typically **Manage Threads**).

**Announcement channel creation fails (403/50013)**

* Guild must have **Community** enabled.
* Verify **Manage Channels**.

**Role creation fails — `Discord.Net.HttpException 50013: Missing Permissions`**

* The bot needs **Manage Roles (268435456)** **on a non‑managed role** that is **above** the roles it manages.
* Fix on server: create a normal role (e.g., `GuildBuilder`) with **Manage Roles**, assign it to the bot, and move it **above** other roles. Or reinvite with `permissions=1342178320`.

**“Bot Permissions” checkboxes in Developer Portal don’t persist**

* They only help build the **invite URL**; they don’t change live server permissions.
* Real permissions = the bot’s server role(s) and the permissions integer used at invite time.

**New category is invisible to everyone**

* You set `EveryoneAccessToNewCategory = false` and didn’t create/grant any role.
* Enable `CreateRolePerCategory = true` or grant access to another role.

**Permission errors in general**

* First run: try invite with **Administrator**; if it works, reduce to minimal perms.

**Rate limits**

* Large templates can be throttled by Discord; the client handles it, just give it a moment.

---

## Limitations & Notes

* Clones **structure & metadata**, not messages/threads content.
* Only **same‑guild** cloning (no cross‑guild).
* With `SyncChannelsToCategory=true`, individual overwrites from the template aren’t copied; inheritance from the new category is enforced.

---

## Tech details (for devs)

* **Target Framework:** `net10.0`
* **Packages:** `Discord.Net 3.18.0`, `Microsoft.Extensions.Configuration 9.0.9`, `...UserSecrets 9.0.9`
* **Config sources:** `appsettings.json` + **UserSecrets**
* **Runtime prompt:** asks only for the new category name.

---

## Release Notes

### v1.0.0

* Add: cloning of text, announcement/news, voice, forum channels.
* Add: forum tag PATCH via REST.
* Add: preserved channel order.
* Add: toggles `CreateRolePerCategory`, `EveryoneAccessToNewCategory`, `SyncChannelsToCategory`.
* Sec: UserSecrets for token & guild ID, no secrets in Git.

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

## Commands cheat‑sheet

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

Happy cloning! 🚀
