# Installing at a customer's shop

Everything that goes on the customer's laptop, and the order to do it in.
Allow about 30 minutes for a shop you have not set up before.

---

## What to take with you

| # | What | Where it comes from | Size |
|---|---|---|---|
| 1 | **PostgreSQL 18** installer | postgresql.org/download/windows | ~350 MB |
| 2 | **DistributionManagementSystem-win-Setup.exe** | `build\Releases\` after `release.ps1` | ~81 MB |
| 3 | **01-create-database.sql** | `deploy\` in this repo | 28 KB |

Put all three on one USB stick. Download PostgreSQL before you leave:
the shop's internet may be slow or down.

**You do not need to install .NET.** It is already inside the Setup.exe.

**WebView2** is only used to show pictures and video inside notifications.
Windows 11 already has it. On Windows 10 the application still works
without it and shows the text of the message instead, so it is optional.
If you want it: search "Microsoft Edge WebView2 Runtime", take the
Evergreen Standalone Installer.

---

## Step 1 — Install PostgreSQL

1. Run the PostgreSQL installer.
2. **Password for the `postgres` user**: choose one and **write it down**.
   You cannot recover it later.
3. **Port**: `5433`.
   The default it offers is 5432. If you keep 5432, you must write 5432
   in the settings file in step 4. Keep it the same at every shop so you
   do not have to remember which is which.
4. Leave everything else as it is. Finish.
5. At the end it offers **Stack Builder** — tick nothing, just close it.

---

## Step 2 — Create the database

Open **SQL Shell (psql)** from the Start menu. Press Enter at every
question until it asks for the password, then type the `postgres`
password from step 1.

Paste these three lines one at a time (use your own password in the
second one, and write it down):

```sql
CREATE DATABASE distribution;
CREATE USER distribution_app WITH PASSWORD 'CHOOSE-A-PASSWORD';
GRANT ALL PRIVILEGES ON DATABASE distribution TO distribution_app;
```

Then connect to the new database and give the account rights inside it:

```sql
\c distribution
GRANT ALL ON SCHEMA public TO distribution_app;
```

Type `\q` and press Enter to leave.

---

## Step 3 — Create the tables

The application does **not** create its own tables. Run the script.

Open **Command Prompt** and run this, with the USB drive letter and the
password you chose in step 2:

```
set PGPASSWORD=THE-PASSWORD-FROM-STEP-2
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U distribution_app -d distribution -f E:\01-create-database.sql
```

It should finish with no line starting `ERROR`. To check, run:

```
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -p 5433 -U distribution_app -d distribution -c "\dt"
```

You should see **23 tables**. If you see none, the script did not run.

---

## Step 4 — Install the application

1. Run **DistributionManagementSystem-win-Setup.exe**.
2. It installs on its own and puts a shortcut on the desktop. No questions.
3. Open it. You get:
   *"This computer has not been set up yet"*, and Notepad opens a file.
4. In that file fill in the two things that belong to this shop:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=distribution;Username=distribution_app;Password=THE-PASSWORD-FROM-STEP-2"
  },
  "Connect": {
    "RepositoryUrl": "https://github.com/QuanitKhatri/OrderBookingSystem",
    "SiteId": "SHOP-NAME-01",
    "UpdateSource": "",
    "UpdatesEnabled": true,
    "BroadcastsEnabled": true,
    "CheckIntervalMinutes": 60,
    "AllowPrerelease": false,
    "LicenseCheckEnabled": true
  }
}
```

| Line | What to put |
|---|---|
| `Password` | The one from step 2 |
| `Port` | 5433, or 5432 if you kept the default |
| `SiteId` | A name for this shop, no spaces, e.g. `AL-MADINA-01` |

**Write the `SiteId` down against the shop's name.** It is the only way
to lock or unlock that shop later, and it must match exactly.

5. Save the file (Ctrl+S), close Notepad, open the application again.

---

## Step 5 — First sign-in

Username `admin`, password `admin123`.

**Change this password straight away**, and do it in front of the owner:
**Users** -> select `admin` -> **Reset Password**.

Anyone who knows the default can otherwise open their books.

---

## Step 6 — Set the shop up

1. **Settings** — business name, address, phone. These print on every
   report, so get the spelling right.
2. **Masters** — companies, categories, products.
3. **Customers** and **Suppliers**, with their opening balances.
4. **Opening Stock** — what is on the shelves today.

---

## Step 7 — Check it before you leave

Do not skip this. Ten minutes here saves a phone call at night.

| Check | Should happen |
|---|---|
| Make a test bill | Saves, and the balance changes |
| Receive a payment against it | Saves, and the balance drops |
| Daily Sale Report -> Export PDF | A PDF opens with the shop's name on it |
| Print a report | A preview appears |
| Settings -> Backup Database | *"Backup created successfully."* |
| Settings -> Software Update | Shows the installed version |
| Dashboard | Today's figures match the test bill |

Then **delete the test bill's effect** by making the shop aware of it, or
do the test before entering their real opening stock. A saved bill
cannot be deleted from inside the application.

---

## Step 8 — Tell the owner

- Keep the laptop on the internet, or updates and messages will not arrive.
- **Take a backup every evening** to a USB stick: Settings -> Backup Database.
- A bill, receipt, purchase or expense **cannot be edited or deleted** once
  saved. Check before saving.
- Your phone number, for when something goes wrong.

---

## If something does not work

| What you see | What it means |
|---|---|
| "This computer has not been set up yet" | The settings file still has the placeholder, or it was not saved |
| Cannot connect to the database | Wrong password or port, or PostgreSQL is not running (services.msc -> postgresql) |
| Signs in, but every screen is empty and errors | Step 3 was skipped: the tables are not there |
| "Backup failed: ... pg_dump" | PostgreSQL client tools missing; re-run its installer |
| No bell, no update offer | No internet, or `RepositoryUrl` was changed |

Logs are in `C:\ProgramData\DistributionManagementSystem\logs`.
Send the newest file if you need to ask about an error.
