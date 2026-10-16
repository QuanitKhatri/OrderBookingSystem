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
 │ Publisher app    │ ─────> │  "broadcasts" │ ───────> │ Windows pop-up +    │
 │ (notifications)  │        │  release      │          │ 🔔 bell             │
 ├──────────────────┤        ├───────────────┤          ├─────────────────────┤
 │ Publisher app    │ ─────> │  licenses     │ ───────> │ software locked     │
 │ (customer locks) │        │  .json        │          │ until you unlock    │
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
5. In that file, fill in the shop's database details **and give the shop a Site Id** (you need it
   to lock or unlock that shop later):
   ```
   C:\ProgramData\DistributionManagementSystem\appsettings.json
   ```
   ```json
   "ConnectionStrings": {
     "Default": "Host=localhost;Port=5433;Database=distribution;Username=distribution_app;Password=THE-REAL-PASSWORD"
   },
   "Connect": {
     "SiteId": "AL-MADINA-01"
   }
   ```

   Give each shop its own Site Id and write it down. Keep a list of which id belongs to which shop.
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

## 5a. Locking a customer's software

If a shop stops paying, you can switch their software off from your PC, and switch it back on when
they settle.

1. Open the **Distribution Publisher**, **Customer Locks** tab.
2. **Site Id**: the id you wrote into that shop's settings file, for example `AL-MADINA-01`.
   It must match exactly.
3. **Shop name**: your own note so you can recognise it in the list.
4. **Lock message**: what they will read on their screen. Put a reason and your phone number,
   for example `Payment for October is pending. Please call 0300-0000000.`
5. Click **Lock this shop** and confirm.

At their next check (within the hour, or when they next start the application) the shop sees a
full-screen message and cannot use the software at all.

**To switch it back on:** select the shop in the list, click **Unlock this shop**. They can press
**Check Again** on the lock screen to come back in straight away, or simply restart.

**How it behaves**

| Situation | What happens |
|---|---|
| Shop is online when you lock | Locked within the hour, or at their next start. |
| Shop is offline when you lock | Keeps working until it next reaches the internet, then locks. |
| Shop is locked and then goes offline | **Stays locked.** Disconnecting the internet does not unlock it. |
| Shop is not listed at all | Allowed to work. A shop is never stopped because you forgot to add it. |
| Your internet is down | Nothing changes for anybody; the last published list keeps applying. |

> **Be careful.** Locking stops the shop billing customers completely. Make sure the Site Id is the
> right one before you confirm: the list in the tab shows which shop each id belongs to.

> **Lock everything at once** is possible in the published file (`lockAll`) but there is no button
> for it, on purpose. Do not use it unless you really mean every shop.

---

## 6. What the customer sees

| Where | What |
|---|---|
| Windows notification area | A pop-up the moment a new message arrives, even when the window is behind something |
| Inside the application | A card under the bell for a few seconds, which they can click to read |
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
| `Connect:SiteId` | This shop's id, used for locking | Set at installation, for example `AL-MADINA-01` |
| `Connect:RepositoryUrl` | Where updates and notifications come from | `https://github.com/QuanitKhatri/OrderBookingSystem` |
| `Connect:UpdateSource` | A folder or address to use instead of GitHub | Empty |
| `Connect:BroadcastFeedUrl` | A different notification feed | Empty |
| `Connect:UpdatesEnabled` | `false` keeps a shop on a fixed version | `true` |
| `Connect:BroadcastsEnabled` | `false` hides notifications for that shop | `true` |
| `Connect:LicenseCheckEnabled` | `false` means that PC can never be locked | `true` |
| `Connect:LicenseFeedUrl` | A different lock list | Empty |
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
| Customer gets no pop-up but the bell shows messages | Messages that were already there when the application first ran are not popped up, only new ones. Windows Focus assist can also hold notifications back. |
| Lock does not take effect | The Site Id does not match that shop's settings file exactly, or the shop has not been online since you locked it. Check the id in the Customer Locks list. |
| A locked shop needs to work right now | Unlock them in the Publisher and ask them to press **Check Again** on the lock screen. |
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
- **Locking is a serious step.** The shop cannot bill anyone while locked. Agree it with them first,
  and always put a phone number in the lock message so they can reach you.
- **A shop with no Site Id cannot be locked.** Set one at installation.
