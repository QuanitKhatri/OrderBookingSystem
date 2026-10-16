# Test Scenarios

**Distribution Management System — checks to run before handing the software to a customer**

Work down the list. Each row says what to do, and what must happen. Anything that does not match is
a problem worth fixing before delivery.

- **Test data:** use a test database, never a live shop's. Nothing here should be run against real
  customer data.
- **Where things live:** the shop's settings are in
  `C:\ProgramData\DistributionManagementSystem\appsettings.json`; logs are in the `logs` folder
  beside it.

---

## 1. Installation and first run

| # | Do this | Must happen |
|---|---|---|
| 1.1 | Run `build\release.ps1 -Version 1.0.0 -Notes "First release."` | `build\Releases\DistributionManagementSystem-win-Setup.exe` is produced, about 80 MB |
| 1.2 | Run that Setup.exe on a clean PC | Installs without asking for .NET; a desktop shortcut appears |
| 1.3 | Start the application before configuring it | A message: *"This computer has not been set up yet"*, and the settings file opens in Notepad |
| 1.4 | Put the shop's database details and a `SiteId` in that file, then start again | The sign-in screen appears |
| 1.5 | Sign in with the administrator account | The dashboard opens, with the shop's name shown in Settings |
| 1.6 | Check **Settings → Software Update** | Shows *Installed version: 1.0.0* |

> Give each shop its own `SiteId` (for example `AL-MADINA-01`) and write it down. Without it that
> shop can never be locked or unlocked individually.

---

## 2. Masters

| # | Do this | Must happen |
|---|---|---|
| 2.1 | Masters → Companies: add a company | Green *"Company saved."*, the company appears in the list |
| 2.2 | Add a category | *"Category saved."* |
| 2.3 | Add a product using that company and category | *"Product saved."*, **In Stock** shows 0, **Avg Cost** equals the purchase rate |
| 2.4 | Customers: click **+** beside Area/Route, add an area, then add a customer with an opening balance | *"Area added."*, then *"Customer saved."*, with the opening balance in the **Balance** column |
| 2.5 | Suppliers: add a supplier | *"Supplier saved."* |
| 2.6 | **Add a customer, then go straight to Party Billing without restarting** | The new customer is in the Customer list |
| 2.7 | Add a supplier, then open Stock Received | The new supplier is in the Supplier list |
| 2.8 | Try to delete a company that has products | Refused: *"This company has active products linked to it…"* |
| 2.9 | Try to save a company with a code that already exists | An error window about a duplicate key; use a different code |

---

## 3. Stock

| # | Do this | Must happen |
|---|---|---|
| 3.1 | Opening Stock: record a quantity and cost for a product | *"Opening stock saved."*, the entry appears in the list below |
| 3.2 | Open Products | **In Stock** and **Avg Cost** already show the new figures |
| 3.3 | Stock Received: choose a supplier, enter an invoice number, add two item lines, Save Purchase | *"Purchase saved and stock updated."*, the purchase appears under Today's Purchases |
| 3.4 | Enter a Bilti charge on that purchase | An expense `BILTI-n` appears under Expense Entry → Today's Expenses |
| 3.5 | Damage Entry: record damage with a reason | *"Damage entry saved."*, loss = quantity × average cost |
| 3.6 | Try to record damage larger than the stock held | Refused: *"Insufficient stock for '…'. Available: …, requested: …."* |

---

## 4. Billing and payments

| # | Do this | Must happen |
|---|---|---|
| 4.1 | Party Billing: pick a customer | **Previous Balance** shows what they owe |
| 4.2 | Choose a product | The **Rate** fills in from the product's sale rate |
| 4.3 | Add two lines, set a bill discount and some cash, save | *"Bill BILL-000xx saved. Rs N on credit — new balance Rs M."* |
| 4.4 | Enter the full net bill as **Cash Received** and save | The message says *"Fully paid."* |
| 4.5 | Leave **Cash Received** at 0 and save | The whole amount is reported as going on credit |
| 4.6 | Sell more than the stock held | Refused: *"Insufficient stock…"*, and the lines stay so they can be corrected |
| 4.7 | Sign in as an **Operator** and give a line discount above the product's Max Discount % | Refused: *"Discount …% exceeds max allowed …%"* |
| 4.8 | **Receive Payment**: pick a customer, enter an amount, Save Receipt | *"Receipt RCPT-000xx saved. New balance: Rs …"* and the balance drops |
| 4.9 | Receive Payment with amount 0 | Refused: *"Amount must be greater than zero."* |
| 4.10 | Party Ledger for that customer → View Ledger | The bill shows as a **Sale** (Debit) and the payment as a **Receipt** (Credit); the running balance matches |
| 4.11 | Market Credit | The customer's balance matches the ledger, with the payment date under **Last Payment** |

---

## 5. Reports, export and print

Run for each of: Daily Closing Stock, Daily Sale Report, Profit & Loss, Damage Report,
Expense Report, Party Ledger, Market Credit.

| # | Do this | Must happen |
|---|---|---|
| 5.1 | Open the report, set the dates, click **View Report** | Figures appear and match what was entered |
| 5.2 | Click **Export Excel**, save, then open the file | The figures match the screen, numbers are real numbers, and there is a total row |
| 5.3 | Click **Export PDF** and open it | The business name, address and phone from Settings are at the top, with the dates and page numbers |
| 5.4 | Click **Print** | A preview appears that matches the PDF; closing it prints nothing |
| 5.5 | Choose a date range with no data, then export | Refused: *"There is nothing to export for the selected options."* |
| 5.6 | Party Ledger: click Export **before** View Ledger | Refused: *"Click View Ledger first."* |
| 5.7 | Leave an exported file open in Excel and export again to the same name | A clear message that the file could not be saved |

**Figures worth checking by hand:** Daily Sale Report **Net** for the day should equal the sum of the
day's bills; Daily Closing Stock **Closing** should equal opening + received − sold − damage.

---

## 6. Notifications

Needs the Publisher set up on the distributor's PC (see [DISTRIBUTOR_GUIDE.md](DISTRIBUTOR_GUIDE.md)).

| # | Do this | Must happen |
|---|---|---|
| 6.1 | Publisher → Send Notification: title and message only, Send | Within the hour (or at the next start) the customer's bell shows a number |
| 6.2 | While the customer's application is open, send another | A Windows pop-up appears near the clock **and** a card under the bell |
| 6.3 | Click that card | The Notifications window opens at that message |
| 6.4 | Click the **✕** on the card instead | The card closes and the message stays unread |
| 6.5 | Let a card time out | It disappears on its own; the message is still in the list |
| 6.6 | Restart the customer's application | The same message does **not** pop up again |
| 6.7 | Install on a fresh PC where messages already exist | Nothing pops up; only the bell count shows them |
| 6.8 | Send a message with a **picture** | The poster shows above the text |
| 6.9 | Send a message with an **MP4 video** | The video plays with normal controls |
| 6.10 | Send one with a link and link text | The button opens the page in the customer's browser |
| 6.11 | Open a message, then close the window | The red dot is gone and the bell count has dropped |
| 6.12 | **Mark all as read** | Every red dot clears and the bell loses its number |
| 6.13 | Publisher → Sent Messages → Remove selected | At the customer's next check the message disappears |
| 6.14 | Set an expiry date in the past | The message is not shown at all |
| 6.15 | Disconnect the customer's internet and restart | The application works normally; messages already received are still readable |

---

## 7. Software updates

| # | Do this | Must happen |
|---|---|---|
| 7.1 | With 1.0.0 installed, publish 1.0.1 (`release.ps1 -Version 1.0.1 -Notes "…" -Publish -Token …`) | The release appears on GitHub |
| 7.2 | Start the customer's application | Within a few seconds: *"A new version is available — You have 1.0.0. New version: 1.0.1"* |
| 7.3 | Read **What's new** | Your release notes are shown, with line breaks kept |
| 7.4 | Click **Download and Install** | A progress bar runs to 100%, then *"Ready to install"* |
| 7.5 | Click **Restart Now** and confirm | The application closes, installs and reopens on 1.0.1 |
| 7.6 | Check Settings → Software Update | *Installed version: 1.0.1* |
| 7.7 | **Check the shop's data and settings** | Customers, bills, balances and the database settings are all unchanged |
| 7.8 | Click **Later** instead, then Settings → Check for Updates | The update is offered again |
| 7.9 | On an up-to-date copy, Check for Updates | *"You already have the latest version (…)."* |
| 7.10 | Run the application from a copied folder rather than the installed one | *"This copy was not installed by the setup program…"* |
| 7.11 | Disconnect the internet and Check for Updates | A message that the check did not work; the application keeps running |

---

## 8. Customer lock

| # | Do this | Must happen |
|---|---|---|
| 8.1 | Publisher → Customer Locks: enter the shop's Site Id, a shop name and a message, **Lock this shop** | The list shows it as **LOCKED** |
| 8.2 | On the customer's PC, wait for the next check | The lock screen covers the application, showing your message and their Site Id |
| 8.3 | Restart the customer's application | It never reaches the sign-in screen |
| 8.4 | **Unlock this shop**, then press **Check Again** on the lock screen | The sign-in screen appears and all data is intact |
| 8.5 | Lock again, then disconnect the customer's internet and restart | **Still locked** — disconnecting must not get around it |
| 8.6 | Unlock, and with the internet still off restart | Still locked, with *"Could not reach your supplier"* on **Check Again**; reconnect and it unlocks |
| 8.7 | A PC whose Site Id is **not** in the list at all | Works normally — never blocked |
| 8.8 | A PC that has never been locked, with no internet | Works normally |
| 8.9 | Lock a shop using a Site Id that does not exist | No shop is affected; nothing breaks |

> **Treat 8.5 to 8.8 as the important ones.** They are what stop a lock turning into an outage for a
> shop that has simply lost its internet.

---

## 9. Users and permissions

| # | Do this | Must happen |
|---|---|---|
| 9.1 | Users → add a user with role **Operator**, set a password | *"User created."* |
| 9.2 | Sign in as that Operator | The **Users** item is not in the side menu |
| 9.3 | As the Operator, try a discount above a product's limit | Refused |
| 9.4 | Admin → Reset Password for that user | *"Password reset."*, and the old password stops working |
| 9.5 | Admin → Deactivate that user | *"User deactivated."*, and they can no longer sign in |
| 9.6 | Add a user whose username already exists | Refused: *"Username already exists."* |

---

## 10. Backup

| # | Do this | Must happen |
|---|---|---|
| 10.1 | Settings → Backup Database, save to a USB drive | *"Backup created successfully."* and a `.backup` file exists |
| 10.2 | On a PC without the PostgreSQL tools | *"Backup failed: … pg_dump …"* — install the PostgreSQL client tools |
| 10.3 | **Restore** — only on a test database | Warns first, then *"Database restored. Please restart the application."* |

---

## 11. Things that cannot be undone

Check that each of these warns or is understood before delivery, because none can be reversed from
inside the application:

- Saving a bill, a purchase, an opening-stock entry, a damage entry, an expense or a receipt.
- Deleting a company, category, product, customer or supplier.
- **Restore Database** — replaces everything.
- Installing an update — only a newer version can replace it.

---

## 12. Automated tests

From the project folder:

```powershell
dotnet build Distribution.slnx
$env:DISTRIBUTION_CONNECTION = "Host=localhost;Port=5433;Database=<test-db>;Username=<user>;Password=<password>"
dotnet test Distribution.slnx
```

All 21 tests must pass. They cover average-cost calculation, the stock and sales modules, and the
customer receipt service (balance, ledger entry, validation).

> The connection string must point at a **test** database. The tests create and delete records.
