# Distribution Management System — User Documentation

This folder contains the complete end-user manual for the **Distribution Management System** desktop application (Windows). The manual is for the people who operate the software every day: owners, operators, store keepers, salespeople and administrators.

## Start here

| If you want to… | Read |
|---|---|
| Understand the application, sign in, find your way around, learn about roles, messages and terms | [USER_MANUAL.md](USER_MANUAL.md) |
| Look up a particular screen, field or button | [SCREEN_REFERENCE.md](SCREEN_REFERENCE.md) |
| Follow step-by-step instructions for a task (make a bill, receive stock, close the day…) | [WORKFLOW_GUIDES.md](WORKFLOW_GUIDES.md) |
| Fix a problem or understand an error message | [TROUBLESHOOTING.md](TROUBLESHOOTING.md) |
| See what is covered and what still needs verification | [DOCUMENTATION_COVERAGE.md](DOCUMENTATION_COVERAGE.md) |

**New users:** read USER_MANUAL.md Sections 4–6, then the workflow guide for your job.
**Administrators setting up a new installation:** start with [WORKFLOW_GUIDES.md → W1 First-time setup](WORKFLOW_GUIDES.md#w1-first-time-setup-of-the-application).

## Folder contents

```
docs/user-manual/
├── README.md                  ← this file
├── USER_MANUAL.md             ← main manual
├── SCREEN_REFERENCE.md        ← every screen, field and control
├── WORKFLOW_GUIDES.md         ← 31 step-by-step procedures
├── TROUBLESHOOTING.md         ← problems and messages
├── DOCUMENTATION_COVERAGE.md  ← coverage checklist and open items
├── USER_MANUAL.pdf            ← print version of all of the above (if present)
└── assets/
    └── screenshots/           ← 120 real screenshots + 6 annotated copies
```

## About the screenshots

- All screenshots were taken from the real running application on 15 and 16 October 2026 (manual version 1.2).
- They show **made-up example data** only (for example "Sunrise Foods", "Al-Madina General Store", "Sample Operator"). No real customer or business data is shown.
- File names start with a number, and that number is the **figure number** used in the text. For example, *Figure 72* is `72-party-billing-ready.png`.
- Files ending in `a` (for example `72a-party-billing-annotated.png`) are copies with red numbered markers that match the numbered tables in the text.

## Important things every user should know

These points apply to the current version and are explained in detail in the manual:

1. **Bills, purchases, stock entries, expenses and receipts cannot be edited or deleted after saving.** Check before you save.
2. **Screens reload when you open them**, so a new customer or product is available on the other screens straight away. Only report results need a click on **View Report**, **View Ledger** or **Apply Filter**.
3. **Whatever you do not type into Cash Received goes on the customer's credit.** Record later payments under **SALES → Receive Payment**.
4. **Reports can be exported to Excel or PDF and printed** using the buttons next to View Report. Individual bills still cannot be printed.
5. **Do not sort master lists by clicking column headings.** Doing so can make the wrong record open for editing.
6. **There is no sign-out button and no working dashboard figures** in this version.
7. **Keyboard shortcuts shown on buttons (Ctrl+S, Esc, F2) are not active.** Click the buttons.
8. **Back up every day** (Settings → Backup Database), and only restore with your administrator.
9. **A bell at the top means your supplier has sent a message.** Click it to read.
10. **When an update is offered, save your work first**, then Download and Install and Restart.

## Viewing this documentation

The files are standard Markdown and display correctly on GitHub, in Visual Studio Code (press `Ctrl+Shift+V` for preview) and in other Markdown viewers. Images use relative paths, so keep the `assets` folder next to the `.md` files when copying the documentation.

## Keeping the manual up to date

When the application changes, update the affected screen in SCREEN_REFERENCE.md, any workflow that uses it, and the coverage checklist. Replace the matching screenshot, keeping the same file name and number so that figure references remain correct.
