# Documentation Coverage Checklist

**Distribution Management System — User Manual v1.3 — 16 October 2026**

This checklist records what the manual covers, how each item was verified, and what is still unverified or missing.

## How the application was verified

| Item | Detail |
|---|---|
| Application version | Built from the project source on branch `dev`, commit `45c2a99` plus the changes made on 15–16 Oct 2026: screen refresh-on-open, the Receive Payment screen, report export/print, automatic updates, supplier notifications (with Windows pop-ups) and the remote lock. |
| Run environment | Windows 11, screen 1920 × 1080, PostgreSQL 18 |
| Test data | A **separate, empty test area** of the database (schema `docs_manual`) filled only with made-up example data. The existing business data (schema `public`) was **not read or changed** by the application during testing. |
| Method | Every screen was opened in the real running application. Representative workflows were performed with example data, and the results were checked on related screens and reports. Screenshots were taken directly from the application windows. |
| Not performed | **Restore Database** (it overwrites data). A **successful** backup could not be made because the PostgreSQL client tools are not on this computer's PATH; the failure message was verified instead. |

**Verification labels used below:**

- **Tested**: performed in the running application and the result observed.
- **Examined**: behaviour determined by examining the application's program logic, not exercised in the running application.

---

## 1. Screen coverage

| # | Screen | Opened in app | Documented (SCREEN_REFERENCE) | Screenshots | Status |
|---|---|---|---|---|---|
| 1 | Sign-in window | ✔ | §1 | 01, 01a, 02 | Complete |
| 2 | Main window / navigation | ✔ | §2 | 03, 03a | Complete |
| 3 | Dashboard | ✔ | §3 | 03, 91, 105 | Complete |
| 4 | Companies | ✔ | §4 | 04–15, 33, 107, 108 | Complete |
| 5 | Categories | ✔ | §5 | 16–20, 32 | Complete |
| 6 | Products (incl. Import from Excel) | ✔ | §6 | 21–31, 54, 55 | Complete |
| 7 | Customers | ✔ | §7 | 34–42 | Complete |
| 8 | Suppliers | ✔ | §8 | 43–48 | Complete |
| 9 | Opening Stock | ✔ | §9 | 49–53 | Complete |
| 10 | Stock Received | ✔ | §10 | 56–62, 60a | Complete |
| 11 | Damage Entry | ✔ | §11 | 63–68 | Complete |
| 12 | Party Billing | ✔ | §12 | 69–75, 72a, 106 | Complete |
| 12a | Receive Payment | ✔ | §12a | 109–113 | Complete |
| 13 | Party Ledger | ✔ | §13 | 76, 77 | Complete |
| 14 | Market Credit | ✔ | §14 | 78 | Complete (empty state not captured) |
| 15 | Expense Entry | ✔ | §15 | 79–84 | Complete |
| 16 | Expense Report | ✔ | §16 | 85 | Complete (filtered and empty states not captured) |
| 17 | Daily Closing Stock | ✔ | §17 | 86 | Complete (filtered state not captured) |
| 18 | Daily Sale Report | ✔ | §18 | 87, 88 | Complete |
| 19 | Profit & Loss | ✔ | §19 | 89 | Complete ("Include Self Expense" state not captured) |
| 20 | Damage Report | ✔ | §20 | 90 | Complete (other Group By options not captured) |
| 21 | Users | ✔ | §21 | 92–101 | Complete |
| 22 | Settings (incl. Backup & Restore and Software Update) | ✔ | §22 | 102–104, 121 | Complete (backup/restore dialogs not captured; see §6) |
| 22a | Notifications window | ✔ | §22a | 117–119 | Complete |
| 22b | Software Update window | ✔ | §22b | 120 | Complete |
| 22c | Locked screen | ✔ | §22c | 123 | Complete |

**Screens discovered: 26. Screens documented: 26.**

The application also contains a "not built yet" placeholder screen that would appear for an unknown menu item. Every menu item opens a real screen, so users cannot reach it. It is not documented further.

### Dialogs and pop-up windows

| Dialog | Documented | Screenshot | Verification |
|---|---|---|---|
| Confirm delete (Yes/No) | SCREEN_REFERENCE §4; USER_MANUAL §15 | 11 | Tested |
| Error — "An unexpected error occurred" | TROUBLESHOOTING §11 | 13 | Tested |
| New Area | SCREEN_REFERENCE §7 | 35 | Tested |
| New Damage Reason | SCREEN_REFERENCE §11 | 64 | Tested |
| New Expense Category | SCREEN_REFERENCE §15 | 81 | Tested |
| Set Password | SCREEN_REFERENCE §21 | 95 | Tested |
| Reset Password | SCREEN_REFERENCE §21 | 99 | Tested |
| Import Products (file selection) | SCREEN_REFERENCE §6 | 29 | Tested |
| Import result "Imported: X, Updated: Y, Errors: Z" | SCREEN_REFERENCE §6 | 30 | Tested |
| Save As (Backup Database) | SCREEN_REFERENCE §22 | — | Tested (screenshot withheld; see §6) |
| Open (Restore Database) | SCREEN_REFERENCE §22 | — | Examined |
| Confirm restore | SCREEN_REFERENCE §22 | — | Examined |
| Toast messages (green/red) | SCREEN_REFERENCE §23; USER_MANUAL §15 | many | Tested |
| Export to Excel / Export to PDF (Save As) | USER_MANUAL §12; W29 | 114 | Tested |
| Export complete — "Open it now?" | USER_MANUAL §12; W29 | 115 | Tested |
| Print Preview | USER_MANUAL §12; W29 | 116 | Tested (preview rendered; sending to a physical printer not tested) |
| Software Update ("A new version is available" / "Ready to install") | SCREEN_REFERENCE §22b; W31 | 120 | Tested end to end on a real installation |
| Restart to install (confirmation) | SCREEN_REFERENCE §22b | — | Tested |
| Setup needed (first run on an unconfigured PC) | DISTRIBUTOR_GUIDE §3 | — | Tested |
| New-message card inside the application | SCREEN_REFERENCE §22a | 122 | Tested |
| Windows notification-area pop-up | USER_MANUAL §16a | — | Tested (seen on screen; not captured as a figure) |

---

## 2. Form coverage

Every form below has a field-by-field table (Field Name, Purpose, Required/Optional, How to Enter, Validation) in SCREEN_REFERENCE.md.

| # | Form | Fields documented | Validation messages | Status |
|---|---|---|---|---|
| 1 | Sign-in | 2 + show/hide | 1 | Tested |
| 2 | Add Company | 4 | 3 (+ duplicate error) | Tested |
| 3 | Add Category | 2 | 2 | Tested |
| 4 | Products (Basic Information + Pricing & Inventory) | 9 | 1 (+ duplicate error) | Tested |
| 5 | Products — Excel import file format | 10 columns | 7 | Tested (5 of 7 messages observed) |
| 6 | Customer | 8 (+ New Area) | 1 | Tested |
| 7 | Supplier | 7 | 1 | Tested |
| 8 | Opening Stock | 4 | 2 | Tested |
| 9 | Stock Received (header + item line) | 5 + 3 | 4 | Tested |
| 10 | Damage Entry | 6 (+ New Damage Reason) | 3 | Tested |
| 11 | Party Billing (header + item line + totals) | 2 + 4 + 2 | 6 | Tested |
| 12 | Expense Entry | 5 (+ New Expense Category) | 1 | Tested |
| 12a | Receive Payment | 5 | 2 | Tested |
| 12b | Notifications window (reading only, no input) | — | — | Tested |
| 12c | Software Update window (no input) | — | — | Tested |
| 13 | Add User (+ Set/Reset Password) | 3 + password | 2 | Tested |
| 14 | Settings — Company & Invoice Settings | 5 | — | Tested (save) |
| 15 | Party Ledger parameters | 3 | — | Tested |
| 16 | Expense Report filter | 3 | — | Partly tested (default range) |
| 17 | Daily Closing Stock parameters | 3 | — | Partly tested (today, all) |
| 18 | Daily Sale Report parameters | 1 | — | Tested (today) |
| 19 | Profit & Loss parameters | 4 | — | Partly tested (default options) |
| 20 | Damage Report parameters | 3 | — | Partly tested (group by Product) |

**Forms documented: 21** (14 data-entry forms, 1 import file format and 6 report-parameter forms), plus 5 small input windows.

---

## 3. Workflow coverage

All workflows are in WORKFLOW_GUIDES.md with prerequisites, numbered steps, expected result, alternatives and failure/recovery.

| ID | Workflow | Verification |
|---|---|---|
| W1 | First-time setup | Tested (whole sequence performed with example data) |
| W2 | Sign in / close | Tested |
| W3 | Add a company | Tested |
| W4 | Add a category | Tested |
| W5 | Add a product | Tested |
| W6 | Import products from Excel | Tested |
| W7 | Add a customer (incl. area) | Tested |
| W8 | Add a supplier | Tested |
| W9 | Edit a master record | Tested (Companies, Categories, Products, Customers, Suppliers, Users loaded; Companies updated) |
| W10 | Delete a master record | Tested for Companies (success + blocked) and Categories (blocked); Examined for Products, Customers, Suppliers |
| W11 | Record opening stock | Tested |
| W12 | Receive stock | Tested (incl. bilti expense) |
| W13 | Record damage | Tested (incl. insufficient stock) |
| W14 | Create a sale bill | Tested (incl. cash, credit, insufficient stock, discount limit) |
| W15 | Record a payment against a balance | Tested (Receive Payment screen; balance, ledger and Market Credit verified) |
| W16 | Check a customer's account | Tested |
| W17 | See who owes money | Tested |
| W18 | Record an expense | Tested |
| W19 | Review expenses | Partly tested |
| W20 | End-of-day closing | Tested (each report opened) |
| W21 | Profit for a period | Tested (single day) |
| W22 | Analyse damage | Partly tested |
| W23 | Create a staff login | Tested |
| W24 | Reset a password | Tested |
| W25 | Deactivate / reactivate a login | Deactivate tested, sign-in refusal tested; Activate examined |
| W26 | Update company settings | Tested |
| W27 | Back up the database | Failure path tested; success path not possible on this computer |
| W28 | Restore the database | **Not performed** (destructive) |
| W29 | Export or print a report | Tested (Excel and PDF files produced and opened; print preview rendered) |
| W30 | Read a notification | Tested (text, poster and video messages, unread counts, mark all as read) |
| W31 | Install a software update | Tested end to end: offered, downloaded, installed, restarted into the new version, site settings preserved |
| W32 | What to do if the software is locked | Tested: locked while running, locked at start-up, Check Again after unlocking, and both offline cases |

**Workflows documented: 32.**

---

## 4. Action-effect coverage

| Action | Effect documented | Cross-screen effect verified |
|---|---|---|
| Save company/category | Available in drop-downs on other screens when reopened | Yes |
| Delete company/category with products | Refused, with message | Yes |
| Save product | Avg Cost = purchase rate; appears in other screens | Yes |
| Excel import (insert/update/errors) | Partial save; overwrites | Yes |
| Opening stock | Stock up; Avg Cost recalculated | Yes (Products after refresh, Daily Closing Stock) |
| Purchase | Stock up; Avg Cost; automatic bilti expense | Yes (Expense Entry, Expense Report, Profit & Loss) |
| Damage | Stock down; loss = qty × avg cost | Yes (Damage Register, Daily Closing Stock, Profit & Loss, Damage Report) |
| Bill | Stock down; balance; receipt; reports | Yes (Party Ledger, Market Credit, Daily Sale Report, Profit & Loss, Daily Closing Stock) |
| Expense | Reports | Yes |
| Deactivate user | Sign-in refused | Yes |
| Role (Operator) | Users menu hidden; discount limit | Yes |
| Settings save | Stored; business name/address/phone appear on exported and printed reports | Yes (seen in the exported PDF and Excel heading) |
| Receive Payment save | Balance down; receipt in ledger; Market Credit updated | Yes |
| Bill with partial cash | Message states the amount left on credit | Yes |
| Navigate away and back | Screen reloads lists and drop-downs | Yes (new customer appeared in Party Billing without restarting) |
| Export Excel / PDF | File written with the on-screen data and totals | Yes (files opened and contents checked) |
| Broadcast published | Bell appears with an unread count; message shows text, poster and video | Yes (against a local test feed) |
| Open a message | Marked read on this PC; bell count drops | Yes |
| New version published | Update offered, downloaded, installed on restart | Yes (against a local test feed) |
| Update installed | Site settings in ProgramData survive; program files replaced | Yes |
| New message arrives | Windows pop-up and in-app card shown once, bell count rises | Yes |
| Shop locked by the distributor | Lock screen covers the application; sign-in blocked at start-up | Yes |
| Shop unlocked | Check Again lets the user straight back in | Yes |
| Offline, never locked | Application works normally | Yes |
| Locked, then offline | Stays locked | Yes |

---

## 5. Unverified behaviour (requires verification)

| # | Behaviour stated in the manual | Where | Why unverified |
|---|---|---|---|
| 1 | Viewer role has the same access as Operator (not read-only) | USER_MANUAL §14; TROUBLESHOOTING §9 | Examined only; no Viewer sign-in was performed |
| 2 | Restore Database dialogs, confirmation and result | SCREEN_REFERENCE §22; W28 | Not executed (destructive) |
| 3 | Successful backup ("Backup created successfully.") | SCREEN_REFERENCE §22; W27 | PostgreSQL client tools missing on test computer |
| 4 | "Today's" lists use the UTC date (entries between midnight and 5 AM Pakistan time) | TROUBLESHOOTING §2 | Examined only |
| 4a | Sending a report to a physical printer | USER_MANUAL §12; W29 | Print preview was verified; no test print was made on paper |
| 4b | Behaviour when a drop-down selection was deleted by another user while a screen was open | SCREEN_REFERENCE §2 | Examined only |
| 5 | Adding the same product on two lines of one bill may bypass the stock check | SCREEN_REFERENCE §12 | Examined only |
| 6 | Text longer than allowed causes an unexpected error | TROUBLESHOOTING §4 | Not tested |
| 7 | Error window or closure when the database cannot be reached at start-up | TROUBLESHOOTING §1 | Not tested |
| 8 | "Import failed: …" when the Excel file is open or invalid | SCREEN_REFERENCE §6 | Not tested |
| 9 | Activate (re-enable) a user; Update a user's role | SCREEN_REFERENCE §21 | Examined only |
| 10 | Deleting a product with stock, or a customer with a balance, is allowed | SCREEN_REFERENCE §6, §7; W10 | Examined only |
| 11 | Company deletion blocked by linked suppliers (second message) | W10 | Examined only (products case tested) |
| 12 | Invoice Prefix and Allow negative stock have no effect | SCREEN_REFERENCE §22 | Examined (bill numbering and stock refusal observed with default settings only) |
| 13 | Self Expenses always 0 / "Include Self Expense" output | SCREEN_REFERENCE §19 | Examined only |
| 14 | A separate procedure for stand-alone customer payments may exist in your organisation | W15 | Business procedure, not software |
| 15 | Report filters other than defaults (company/category filters, Group By Company/Reason/Month, Expense Report category filter) | SCREEN_REFERENCE §16–20 | Examined only |
| 16 | Receipt larger than the balance (creating an advance) | SCREEN_REFERENCE §12a | Examined only |
| 17 | Publishing a notification or an update **to GitHub** | DISTRIBUTOR_GUIDE §4, §5 | The Publisher app and `release.ps1` were built and their screens tested, but no upload was made: the GitHub account signed in on this PC has read-only access to the repository. The whole customer-side path was tested against a local feed instead. |
| 18 | Behaviour of the notification window without the WebView2 runtime | SCREEN_REFERENCE §22a | Examined only; the runtime is present on this PC |
| 19 | Delta (part) updates over the internet | DISTRIBUTOR_GUIDE §4 | A delta package was produced and the update applied from a local folder; not tested over GitHub |
| 20 | SmartScreen warning on an unsigned installer | DISTRIBUTOR_GUIDE §9 | Expected behaviour, not reproduced |
| 21 | Publishing a lock **to GitHub** from the Publisher | DISTRIBUTOR_GUIDE §5a | The Customer Locks screen was built and opened, but no upload was made (read-only GitHub account on this PC). The customer side was tested against a local list. |
| 22 | `lockAll` (locking every shop at once) | DISTRIBUTOR_GUIDE §5a | Supported by the format and the service; no button for it and not exercised |
| 23 | Windows Focus assist holding back the pop-up | TROUBLESHOOTING §9a | Examined only |

---

## 6. Screenshots

| Item | Count |
|---|---|
| Actual application screenshots | **122** (files `01-…` to `123-…`) |
| Annotated copies with numbered markers | **6** (`01a`, `03a`, `09a`, `25a`, `60a`, `72a`) |
| Total image files | **128**, in `assets/screenshots/` |

Screenshots of every screen affected by the 15 Oct 2026 changes were retaken: Party Billing (69–75), Party Ledger (76–77), Market Credit (78), Expense Report (85), Daily Closing Stock (86), Daily Sale Report (87–88), Profit & Loss (89), Damage Report (90), plus the new Receive Payment screen (109–113) and the export/print windows (114–116). The obsolete "stale stock" figure was removed, because screens now refresh on their own.

All screenshots come from the real running application. No mock-ups or generated illustrations are used. The annotated copies only add numbered red circles on top of the real screenshots.

### Screenshots still to be captured manually

| Screenshot | How to capture |
|---|---|
| Sign-in window with the password shown (eye icon clicked) | Type a sample password, click the eye icon, capture the window. |
| Viewer user's main window | Activate a Viewer account, sign in as it, capture the main window. |
| Backup Database: Save As window and "Backup created successfully." | On a computer with PostgreSQL client tools on PATH, click Backup Database and capture both. Choose a neutral folder so personal file names are not visible. |
| Backup failure message | Captured during testing but **withheld**, because it showed an internal temporary folder path. The exact text is quoted in TROUBLESHOOTING §10. Recapture on a production installation if needed. |
| Restore Database: Open window, "Confirm restore" and "Database restored…" | Only on a **test copy** of the database. Never on live data. |
| Market Credit empty state ("No outstanding credit") | Capture on a database where no customer owes money. |
| Expense Report with a category filter applied; empty "No expenses in this range" | Choose a category and dates, click Apply Filter. |
| Daily Closing Stock with company/category filters | Choose a company, click View Report. |
| Profit & Loss with "Include Self Expense" ticked | Tick and click View Report. |
| Damage Report grouped by Company, Reason and Month | Change Group By, click View Report. |
| Start-up error when the database is unavailable | Ask the system administrator to reproduce it in a test environment. |
| "Import failed" error | Open the Excel file in Excel, then import it. |

---

## 7. Final coverage review

| Check | Result |
|---|---|
| Every discovered screen documented | ✔ 26 / 26 |
| Every data-entry form documented field by field | ✔ 14 / 14 (+ import format, report parameter forms and input windows) |
| Every visible button and control explained | ✔ |
| Every meaningful workflow has start-to-finish steps | ✔ 32 workflows |
| Action effects and cross-screen relationships described | ✔ |
| Screenshots for every major screen and form | ✔ (see §6 for remaining optional states) |
| Internal image links resolve | ✔ Checked automatically: all image references point to existing files |
| Menu and button names match the application | ✔ Taken from the running application |
| Unsupported behaviour not presented as fact | ✔ Missing features are explicitly listed; unverified items labelled |
| No developer-only explanations in the end-user manual | ✔ Technical wording limited to quoted error messages and administrator notes |
| No credentials or private data exposed | ✔ Only made-up example data; the initial administrator password is deliberately not printed |
