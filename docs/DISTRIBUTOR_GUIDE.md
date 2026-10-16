# Distributor Guide — Updates and Notifications

**For you, not for your customers.** This explains how to install the software at a customer's shop,
how to send them an update, and how to send them a notification with a poster or video.

Your customers' own manual is in [user-manual/](user-manual/README.md).

---

## 1. How it works

```
   Your PC (Garden)                GitHub                 Customer PC (Korangi)
 ┌──────────────────┐        ┌───────────────┐          ┌─────────────────────┐
 │ release.ps1      │ ─────> │  v1.2.0       │ ───────> │ "A new version is   │
 │ (build + upload) │        │  release      │          │  available"         │
 ├──────────────────┤        ├───────────────┤          ├─────────────────────┤
 │ Publisher app    │ ─────> │  "broadcasts" │ ───────> │ 🔔 bell with the    │
 │ (notifications)  │        │  release      │          │    message          │
 └──────────────────┘        └───────────────┘          └─────────────────────┘
```

- Everything goes through **one GitHub repository**: `QuanitKhatri/OrderBookingSystem`.
- The customer's application **reads** from it. It never needs a password or token.
- Only **your** PC can publish, using a token you create once.
- The customer needs an internet connection. Without one, the software keeps working normally;
  it simply does not see updates or new notifications until it is online again.

---

## 2. One-time setup on your PC

### 2.1 Create a GitHub token

1. Sign in to GitHub as **the account that owns the repository**.
2. Go to **Settings → Developer settings → Personal access tokens → Tokens (classic)**.
3. Click **Generate new token (classic)**.
4. Tick the **repo** box.
5. Copy the token (it starts with `ghp_`). You cannot see it again later.

> The `gh` command line on this PC is currently signed in as a different account
> (`attarmakkiscents`) which has **read-only** access to the repository. Publishing needs a token
> from the owning account.

### 2.2 Set up the Publisher

1. Build it once:
   ```powershell
   dotnet build src\Distribution.Publisher -c Release
   ```
   The program is in `src\Distribution.Publisher\bin\Release\net10.0-windows\Distribution.Publisher.exe`.
   Make a desktop shortcut to it.
2. Open it and go to the **Settings** tab.
3. Fill in:
   - **GitHub owner**: `QuanitKhatri`
   - **Repository**: `OrderBookingSystem`
   - **Access token**: paste the token
4. Click **Save**, then **Test connection**. It should say
   *"Connected as … — can publish to QuanitKhatri/OrderBookingSystem."*

The token is stored only on your PC, encrypted with your Windows account. It is never sent to
customers.

---

## 3. Installing at a customer's shop

Do this once per computer.

1. On your PC, build the installer:
   ```powershell
   .\build\release.ps1 -Version 1.0.0 -Notes "First release."
   ```
2. Take `build\Releases\DistributionManagementSystem-win-Setup.exe` to the shop (USB or download).
3. Run it there. It installs for the current Windows user and makes a desktop shortcut.
   The .NET runtime is **not** needed — it is built into the file.
4. Start the application. The first time it will say **"This computer has not been set up yet"**
   and open a settings file.
5. In that file, fill in the shop's database details:
   ```
   C:\ProgramData\DistributionManagementSystem\appsettings.json
   ```
   ```json
   "ConnectionStrings": {
     "Default": "Host=localhost;Port=5433;Database=distribution;Username=distribution_app;Password=THE-REAL-PASSWORD"
   }
   ```
6. Save the file and start the application again.

> **Why this file sits outside the program folder:** an update replaces everything inside the
> program folder. The shop's database details live in `C:\ProgramData\…` so that they survive
> every update. Never put the password in the program folder.

### Shops with no internet

Set `UpdateSource` in that same file to a shared folder, and copy the contents of
`build\Releases` into it when you want to push an update:

```json
"Connect": {
  "UpdateSource": "\\\\server\\updates",
  "BroadcastsEnabled": false
}
```

---

## 4. Sending an update

1. Make your changes and test them.
2. Decide the new version number. It must be **higher** than the last one (1.0.0 → 1.0.1 → 1.1.0).
3. Build and publish:
   ```powershell
   .\build\release.ps1 -Version 1.0.1 -Notes "Receive Payment screen added.`nReports export to Excel and PDF." -Publish -Token ghp_xxx
   ```
   Or set the token once per session and leave it off the command:
   ```powershell
   $env:GITHUB_TOKEN = "ghp_xxx"
   .\build\release.ps1 -Version 1.0.1 -Notes "..." -Publish
   ```
4. That is all. Each customer's application checks about **every hour**, and on every start.
   When it finds the new version it shows:

   > **A new version is available** — You have 1.0.0. New version: 1.0.1

   The customer clicks **Download and Install**, then **Restart Now**. The application closes,
   installs, and opens again on the new version. Their database and settings are untouched.

**Notes**
- The customer can also check by hand: **Settings → Software Update → Check for Updates**.
- Only the changed part is downloaded after the first install, so updates are small and quick.
- A customer who clicks **Later** is offered it again at the next check.
- The version number shown comes from `<Version>` in `Distribution.WinForms.csproj`; `release.ps1`
  sets it for you.

---

## 5. Sending a notification

1. Open the **Distribution Publisher** on your PC.
2. **Send Notification** tab:

   | Field | What to put |
   |---|---|
   | **Title** | Short headline, for example `Diwali offer on Sunrise Foods` |
   | **Type** | **Info** (news), **Offer** (marketing), **Alert** (needs attention). It changes the colour. |
   | **Message** | The full text. Line breaks are kept. |
   | **Picture** | Optional poster — PNG or JPG |
   | **Video** | Optional — **MP4** plays on every PC |
   | **Link** | Optional `https://…` link, opens in the customer's browser |
   | **Link button** | Text on that button, for example `See the offer` |
   | **Remove on** | Tick to make the message disappear by itself on a date |

3. Click **Preview** to check it.
4. Click **Send to all customers** and confirm.

Within an hour (and at every start) a **🔔 bell with a red number** appears at the top of every
customer's application. They click it and see your message with the poster or video.

**Limits and advice**
- Keep pictures and videos under **200 MB**. Smaller is better — shops often have slow internet.
- A poster around 900 × 500 pixels looks right.
- Use **MP4 (H.264)** for video. Other formats may not play.
- Keep videos short. A 6-second clip is about 30 KB; a 2-minute advertisement can be 20 MB.

### Removing a notification

**Sent Messages** tab → select it → **Remove selected**. It disappears from customers at their next
check. You cannot edit a sent message; remove it and send a corrected one.

---

## 6. What the customer sees

| Where | What |
|---|---|
| Top bar | 🔔 bell with the number of unread messages |
| Notifications window | List on the left, the full message with poster or video on the right |
| Settings screen | Installed version, and a **Check for Updates** button |
| Update window | Version numbers, what's new, a progress bar, then **Restart Now** |

Read and unread is remembered per computer, not per user account.

---

## 7. Settings reference

In `C:\ProgramData\DistributionManagementSystem\appsettings.json` on each customer PC:

| Setting | Meaning | Normal value |
|---|---|---|
| `ConnectionStrings:Default` | That shop's database | Filled in at installation |
| `Connect:RepositoryUrl` | Where updates and notifications come from | `https://github.com/QuanitKhatri/OrderBookingSystem` |
| `Connect:UpdateSource` | A folder or address to use instead of GitHub | Empty |
| `Connect:BroadcastFeedUrl` | A different notification feed | Empty |
| `Connect:UpdatesEnabled` | `false` keeps a shop on a fixed version | `true` |
| `Connect:BroadcastsEnabled` | `false` hides notifications for that shop | `true` |
| `Connect:CheckIntervalMinutes` | How often to check, 5 to 1440 | `60` |
| `Connect:AllowPrerelease` | Also offer test versions | `false` |

---

## 8. If something goes wrong

| Problem | What to do |
|---|---|
| Publisher says *"can read … but cannot publish"* | The token is from an account without write access. Make one from the owning account. |
| Customer never sees the update | Check their internet. Have them use **Settings → Check for Updates** and read the message. |
| *"This copy was not installed by the setup program"* | They are running a copied folder, not the installed one. Reinstall with `Setup.exe`. |
| Customer sees no bell | No notification has been sent, or `BroadcastsEnabled` is `false`, or the PC is offline. |
| Picture or video does not show, only text and buttons | The WebView2 runtime is missing on that PC. Install **Microsoft Edge WebView2 Runtime** (free from Microsoft). The message text still works without it. |
| After an update the app asks to be set up again | `C:\ProgramData\DistributionManagementSystem\appsettings.json` was deleted. Put the database details back. |
| Video will not play | Convert it to MP4 (H.264). |

Technical logs are at `C:\ProgramData\DistributionManagementSystem\logs\`.

---

## 9. Things to know before you rely on this

- **Updates cannot be undone from inside the application.** If you publish a bad version, publish a
  fixed higher version straight away. Test on your own PC first.
- **The installer is not code-signed.** Windows SmartScreen will warn the first time it is run
  ("Windows protected your PC" → **More info** → **Run anyway**). Buying a code-signing certificate
  removes this; `vpk pack` accepts one if you get one later.
- **Notifications are public.** Anyone who finds the repository can read them. Do not put prices,
  customer names or anything private in a notification.
- **A sent notification cannot be edited**, only removed.
