# Workflow Guides

**Distribution Management System — step-by-step procedures**

Each guide takes one complete business task from start to finish. Screen and button names match the application exactly. Figure numbers refer to the screenshots in `assets/screenshots/` and are explained in [SCREEN_REFERENCE.md](SCREEN_REFERENCE.md).

> **Example data.** All names, numbers and amounts in these guides are made up for illustration. Replace them with your own.

> **Two habits that prevent most problems**
> 1. **Watch the bottom-right corner after every click on Save.** A green message means it worked. A red message tells you what to fix. Messages disappear after about two seconds.
> 2. **Screens reload when you open them.** A customer, product or supplier you add on one screen is available on the others straight away. Only report results need a click on **View Report**, **View Ledger** or **Apply Filter**.

---

## Contents

| # | Workflow | Typical user |
|---|---|---|
| [W1](#w1-first-time-setup-of-the-application) | First-time setup of the application | Administrator |
| [W2](#w2-sign-in-and-close-the-application) | Sign in and close the application | Everyone |
| [W3](#w3-add-a-company) | Add a company | Admin / Operator |
| [W4](#w4-add-a-category) | Add a category | Admin / Operator |
| [W5](#w5-add-a-product) | Add a product | Admin / Operator |
| [W6](#w6-import-products-from-excel) | Import products from Excel | Admin |
| [W7](#w7-add-a-customer-including-a-new-area) | Add a customer (including a new area) | Admin / Operator |
| [W8](#w8-add-a-supplier) | Add a supplier | Admin / Operator |
| [W9](#w9-edit-an-existing-master-record) | Edit an existing master record | Admin / Operator |
| [W10](#w10-delete-a-master-record) | Delete a master record | Admin |
| [W11](#w11-record-opening-stock) | Record opening stock | Administrator |
| [W12](#w12-receive-stock-from-a-supplier) | Receive stock from a supplier | Store keeper / Operator |
| [W13](#w13-record-damaged-or-lost-stock) | Record damaged or lost stock | Store keeper / Operator |
| [W14](#w14-create-a-sale-bill) | Create a sale bill | Salesperson / Operator |
| [W15](#w15-record-a-payment-against-a-customers-balance) | Record a payment against a customer's balance | Operator / Recovery staff |
| [W16](#w16-check-a-customers-account) | Check a customer's account | Operator / Owner |
| [W17](#w17-see-who-owes-money) | See who owes money | Owner / Recovery staff |
| [W18](#w18-record-an-expense) | Record an expense | Operator |
| [W19](#w19-review-expenses) | Review expenses | Owner |
| [W20](#w20-end-of-day-closing) | End-of-day closing | Owner / Operator |
| [W21](#w21-calculate-profit-for-a-period) | Calculate profit for a period | Owner |
| [W22](#w22-analyse-damage) | Analyse damage | Owner |
| [W23](#w23-create-a-staff-login) | Create a staff login | Administrator |
| [W24](#w24-reset-a-forgotten-password) | Reset a forgotten password | Administrator |
| [W25](#w25-deactivate-or-reactivate-a-staff-login) | Deactivate or reactivate a staff login | Administrator |
| [W26](#w26-update-company-settings) | Update company settings | Administrator |
| [W27](#w27-back-up-the-database) | Back up the database | Administrator |
| [W28](#w28-restore-the-database-from-a-backup) | Restore the database from a backup | Administrator |
| [W29](#w29-export-or-print-a-report) | Export or print a report | Owner / Operator |
| [W30](#w30-read-a-notification-from-your-supplier) | Read a notification from your supplier | Everyone |
| [W31](#w31-install-a-software-update) | Install a software update | Everyone |

---

## W1. First-time setup of the application

**Purpose:** Prepare a new installation so that daily work can start.

**Prerequisites:**
- The application has been installed and connected to its database by your system administrator.
- You have the initial administrator sign-in details (see [USER_MANUAL.md, Section 4](USER_MANUAL.md#4-system-access-and-getting-started)).

**Order matters.** Some records depend on others:

```
Companies ─┬─> Products ──> Opening Stock ──> Stock Received / Party Billing / Damage Entry
Categories ┘
Companies ───> Suppliers ──> Stock Received
Areas ───────> Customers ──> Party Billing
```

**Steps:**

1. Sign in as the administrator ([W2](#w2-sign-in-and-close-the-application)).
2. **Change the administrator password immediately** ([W24](#w24-reset-a-forgotten-password); select the user **admin**).
3. Create a separate login for each staff member ([W23](#w23-create-a-staff-login)).
4. Enter your business details in **Settings** ([W26](#w26-update-company-settings)).
5. Add all **Companies** ([W3](#w3-add-a-company)).
6. Add all **Categories** ([W4](#w4-add-a-category)).
7. Add all **Products** one by one ([W5](#w5-add-a-product)) or from Excel ([W6](#w6-import-products-from-excel)).
8. Add **Customers** with their opening balances ([W7](#w7-add-a-customer-including-a-new-area)).
9. Add **Suppliers** ([W8](#w8-add-a-supplier)).
10. Record **Opening Stock** for every product you hold ([W11](#w11-record-opening-stock)).
11. Make a first **backup** ([W27](#w27-back-up-the-database)).

**Expected result:** All master lists are complete, Products shows correct **In Stock** quantities, and Customers shows correct balances.

---

## W2. Sign in and close the application

**Purpose:** Start and finish a work session.

**Steps — sign in:**

1. Start the application. The **Sign in — Distribution Management System** window appears (Figure 1).
2. Click in **Username** and type your username (upper and lower case matter).
3. Press **Tab** or click in **Password**, and type your password. To check what you typed, click the eye icon.
4. Press **Enter** or click **Login (Enter)**.
5. The main window opens on the **Dashboard**. Your name and role appear at the top-right (Figure 3).

**Steps — close or switch user:**

1. Finish and save any work in progress. Unsaved forms are lost when the application closes.
2. Click the **X** at the top-right of the main window. The application closes.
3. To work as another user, start the application again and sign in with the other account.

**Failure and recovery:**

| Problem | What to do |
|---|---|
| "Invalid username or password." (Figure 2) | Check Caps Lock and the spelling of the username. Use the eye icon to check the password. If it still fails, your account may be deactivated or the password may have been changed; ask an administrator. |
| An "Error" window appears before the Sign-in window, or the application closes immediately | The application cannot reach its database. Contact your system administrator. See [TROUBLESHOOTING.md](TROUBLESHOOTING.md#1-sign-in-and-start-up). |

---

## W3. Add a company

**Purpose:** Register a manufacturer or brand.

**Prerequisites:** None.

**Steps:**

1. In the side menu, under **MASTERS**, click **Companies**. The Companies screen opens (Figure 4).
2. Make sure the button reads **Save (Ctrl+S)**. If it reads **Update (Ctrl+S)**, click **Clear (Esc)** first. Otherwise you would be changing an existing company.
3. In **Short Code**, type a short unique code, for example `SRF`.
4. In **Name**, type the full name, for example `Sunrise Foods`.
5. Optionally type the **Contact Person** (for example `Bilal Ahmed`) and **Phone** (for example `0300-0000001`). See Figure 6.
6. Click **Save (Ctrl+S)**.
7. Check that the green message **"Company saved."** appears, the form empties and the company appears in the list (Figure 7).

**Expected result:** The company is listed on Companies and can be chosen straight away on Products and Suppliers.

**Failure and recovery:**

| Message | Cause | Fix |
|---|---|---|
| "Code and Name are required." (Figure 5) | Short Code or Name is empty. | Fill both and click Save again. |
| Error window "An unexpected error occurred: 23505: duplicate key value violates unique constraint "IX_Companies_Code"…" (Figure 13) | That Short Code is already used, possibly by a deleted company. | Click **OK**, change the Short Code, and click Save again. |

---

## W4. Add a category

**Steps:**

1. Click **Categories** under **MASTERS** (Figure 16).
2. If the button reads **Update (Ctrl+S)**, click **Clear (Esc)**.
3. Type a **Name**, for example `Beverages`, and optionally a **Description**, for example `Soft drinks, juices and water` (Figure 18).
4. Click **Save (Ctrl+S)**.
5. Check for **"Category saved."** and the new row in the list (Figure 19).

**Failure:** "Name is required." (Figure 17): type a name and save again. The application does not stop duplicate category names, so check the list first.

---

## W5. Add a product

**Purpose:** Register an item that you buy and sell.

**Prerequisites:** The product's company and category exist.

**Steps:**

1. Click **Products** under **MASTERS** (Figure 21).
2. If the button reads **Update (Ctrl+S)**, click **Clear (Esc)**.
3. **Basic Information** card:
   1. **Code**: type a unique code, for example `SRF-001`.
   2. **Name**: for example `Sunrise Cream Biscuits`.
   3. **Company**: open the list and choose `Sunrise Foods`.
   4. **Category**: choose `Snacks`.
   5. **Unit**: choose `Ctn`.
4. **Pricing & Inventory** card (click in a box, type the number, then press **Tab**):
   1. **Pcs per Unit**: `24`.
   2. **Purchase Rate**: `1800`.
   3. **Sale Rate**: `2100`.
   4. **Max Discount %**: `5`.
5. Check the form (Figure 23), then click **Save (Ctrl+S)**.
6. Check for **"Product saved."** and the product in the list. **In Stock** shows 0 and **Avg Cost** equals the Purchase Rate (Figure 24).

**Expected result:** The product can be used straight away on Opening Stock, Stock Received, Damage Entry and Party Billing — just open the screen.

**Common mistakes:**
- Leaving **Sale Rate** at 0: on Party Billing the Rate would be 0 and the line would be refused ("Qty and Rate must be greater than zero.").
- Leaving **Max Discount %** at 0: Operators then cannot give any discount on this product.
- **Pcs per Unit** shows 0.00 the first time the screen opens. Enter the real value.

**Failure:** "Code, Name, Company, Category and Unit are required." (Figure 22). A duplicate code shows a technical error window like Figure 13: click **OK** and change the code.

---

## W6. Import products from Excel

**Purpose:** Create or update many products at once.

**Prerequisites:** All companies and categories named in the file exist.

**Steps:**

1. Prepare an Excel (.xlsx) file. In row 1 type these headings: `Code`, `Name`, `Company`, `Category`, `Unit`, `PcsPerUnit`, `PurchaseRate`, `SaleRate`, and optionally `MaxDiscountPct` and `Barcode`. Example:

   | Code | Name | Company | Category | Unit | PcsPerUnit | PurchaseRate | SaleRate | MaxDiscountPct | Barcode |
   |---|---|---|---|---|---|---|---|---|---|
   | DHC-002 | Delta Dishwash Bar | Delta Home Care | Household | Box | 24 | 480 | 576 | 5 | 8900000000017 |
   | SRF-002 | Sunrise Salted Chips | Sunrise Foods | Snacks | Box | 12 | 900 | 1100 | 3 | |

2. Save and **close** the file in Excel.
3. Open **Products** and click **Import from Excel**.
4. In the **Import Products** window, go to the folder, select the file and click **Open** (Figure 29).
5. Read the result:
   - Green message, for example "Imported: 2, Updated: 0, Errors: 0": all rows were accepted.
   - A window titled "Imported: 1, Updated: 1, Errors: 2" (Figure 30) lists each rejected row. Note the row numbers and click **OK**.
6. Check the Products list (Figure 31).
7. Correct the rejected rows in Excel and import the file again. Rows already imported will simply be **updated**, not duplicated.

**What happens to existing products:** a row whose **Code** matches an existing product replaces that product's name, company, category, unit, pcs per unit, rates, max discount and barcode. A missing `MaxDiscountPct` column sets 0, and a missing or blank `Barcode` removes the barcode.

**Failure and recovery:** see the import message table in [SCREEN_REFERENCE.md](SCREEN_REFERENCE.md#import-from-excel).

---

## W7. Add a customer (including a new area)

**Purpose:** Register a shop or party you sell to.

**Steps:**

1. Click **Customers** under **MASTERS** (Figure 34).
2. If the button reads **Update (Ctrl+S)**, click **Clear (Esc)**.
3. If the customer's area is not in the **Area / Route** list:
   1. Click the **+** button next to **Area / Route**.
   2. In the **New Area** window, type the name, for example `Main Bazaar Route` (Figure 35).
   3. Click **OK**. The message **"Area added."** appears (Figure 36).
4. Fill in the form (Figure 38):
   - **Code**: `C-001`
   - **Shop Name**: `Al-Madina General Store`
   - **Owner**: `Kamran Shah` (optional)
   - **Phone**: `0300-0000101` (optional)
   - **Area / Route**: choose `Main Bazaar Route`
   - **Address**: `Shop 12, Main Bazaar` (optional)
   - **Opening Balance**: `5000`, the amount the customer already owed you. Leave 0 for a new customer.
   - **Default Discount %**: optional, for your reference only.
5. Click **Save (Ctrl+S)**.
6. Check for **"Customer saved."** (Figure 39). The list shows the customer with **Balance** equal to the opening balance (Figure 40).

**Expected result:** The customer can be selected straight away on Party Billing, Receive Payment and Party Ledger.

**Failure:** "Code, Shop Name and Area are required." (Figure 37). A duplicate code shows a technical error window: click **OK** and change the code.

---

## W8. Add a supplier

**Steps:**

1. Click **Suppliers** under **MASTERS** (Figure 43).
2. If the button reads **Update (Ctrl+S)**, click **Clear (Esc)**.
3. Fill in **Code** (`S-001`), **Name** (`Sunrise Distribution Co.`), **Company** (choose `Sunrise Foods`), and optionally **Contact Person**, **Phone**, **Address** and **Opening Balance** (amount you owed the supplier) (Figure 45).
4. Click **Save (Ctrl+S)** and check for **"Supplier saved."** (Figure 46).

**Note:** **Payable Balance** in the list only ever shows the opening balance. Purchases do not change it. New suppliers appear on Stock Received as soon as you open that screen.

**Failure:** "Code, Name and Company are required." (Figure 44).

---

## W9. Edit an existing master record

Applies to Companies, Categories, Products, Customers and Suppliers.

**Steps:**

1. Open the screen.
2. Find the record. Type part of its name or code in the search box to shorten the list.
3. Click the record's row. Its details load into the form and the button changes to **Update (Ctrl+S)** (Figures 9, 20, 26, 41, 48).
4. **Check that the form shows the record you clicked** (code and name). If you sorted the list by clicking a column heading, a different record may load (Figure 108). In that case click **Clear (Esc)**, type a letter in the search box and delete it to reset the order, then click the row again.
5. Change the fields you need.
6. Click **Update (Ctrl+S)**.
7. Check for the "… updated." message (for example "Company updated.", Figure 10) and the changed values in the list.

**Alternative:** to abandon your changes, click **Clear (Esc)**. Nothing is saved.

**Effects to remember:**
- Changing a product's **Sale Rate** affects future bills only.
- Changing a customer's **Opening Balance** changes their balance everywhere (ledger, market credit, previous balance on bills).
- Changing **Purchase Rate** does not change a product's **Avg Cost**.

---

## W10. Delete a master record

> **Deletion cannot be undone from the application.** Deleted records disappear from lists, and their codes cannot be reused.

**Steps:**

1. Open the screen and click the record's row. Confirm that the form shows the correct record.
2. Click **Delete**.
3. The **Confirm delete** window asks, for example, "Delete this company?" (Figure 11).
4. Click **Yes** to delete, or **No** to cancel.
5. Check for the "… deleted." message (Figure 12) and that the row is gone.

**When deletion is refused:**

| Screen | Refused when | Message |
|---|---|---|
| Companies | The company still has products, or suppliers | "This company has active products linked to it and cannot be deleted. Deactivate or reassign those products first." (Figure 33) / "This company has active suppliers linked to it and cannot be deleted. Deactivate or reassign those suppliers first." |
| Categories | The category still has products | "This category has active products linked to it and cannot be deleted. Deactivate or reassign those products first." (Figure 32) |

To delete such a company or category, first change or delete the linked products or suppliers. (There is no "Deactivate" button in this version, despite the wording of the message.)

**Not checked by the application (be careful):**
- Products can be deleted while they still have stock. The stock then disappears from Daily Closing Stock.
- Customers can be deleted while they still owe money. The balance then disappears from Market Credit.

---

## W11. Record opening stock

**Purpose:** Enter the goods you already hold when you start using the application.

**Prerequisites:** Products exist. Ideally count physical stock first.

**Steps:**

1. Click **Opening Stock** under **STOCK** (Figure 49).
2. **As On Date**: set the date of your stock count. Choose a date **before** your first real transaction so that reports show it as opening stock.
3. **Product**: choose the first product, for example `SRF-001 - Sunrise Cream Biscuits`.
4. **Qty**: type the counted quantity, for example `50`.
5. **Cost Rate**: type the cost per unit, for example `1750` (Figure 51).
6. Click **Save (Ctrl+S)**.
7. Check for **"Opening stock saved."** and the new row in **Recent Opening Stock Entries** (Figure 52).
8. Qty and Cost Rate are cleared. Date and product stay. Choose the next product and repeat steps 4–7.
9. When finished, review the list (Figure 53).

**Verify:** Open **Products**. **In Stock** and **Avg Cost** show the new values (Figure 55).

> **Warning:** Each save **adds** stock. Saving the same product twice doubles its opening stock, and entries cannot be deleted. If that happens, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md#5-stock-quantities-are-wrong).

**Failure:** "Quantity must be greater than zero." (Figure 50); "Select a product." (no product chosen).

---

## W12. Receive stock from a supplier

**Purpose:** Record a delivery, add it to stock and record freight.

**Prerequisites:** The supplier and all products on the invoice exist.

**Steps:**

1. Click **Stock Received** under **STOCK** (Figure 56).
2. Fill the header:
   1. **Supplier**: choose `S-001 - Sunrise Distribution Co.`
   2. **Invoice No**: type the supplier's invoice number, `INV-SF-1001`.
   3. **Date**: leave as today, or set the delivery date.
   4. **Vehicle No**: optional, for example `ABC-123`.
   5. **Bilti Charges**: freight paid, for example `1500` (leave 0 if none).
3. Add the first item:
   1. **Product**: `SRF-001 - Sunrise Cream Biscuits`.
   2. **Qty**: `20`.
   3. **Rate**: `1800`.
   4. Click **Add Line**. The line appears in the list and **Total** updates. Qty and Rate are cleared.
4. Repeat step 3 for every item on the invoice (Figure 59), for example `SRF-002`, Qty `30`, Rate `900`.
5. Compare the lines and **Total: Rs 63,000** with the paper invoice (Figure 60).
6. Click **Save Purchase (Ctrl+S)**.
7. Check for **"Purchase saved and stock updated."** The purchase appears under **Today's Purchases** (Figure 61).

**Expected result:**
- Stock of each product increases (Products → In Stock; Daily Closing Stock → Received).
- Average cost is recalculated (Products → Avg Cost).
- The bilti of Rs 1,500 is recorded automatically as expense **BILTI-1**, category **Bilti / Freight** (Expense Entry → Today's Expenses, Figure 84).

**Alternative paths:**
- **Wrong line before saving:** click **Clear (Esc)**. This clears all lines, the invoice number, vehicle and bilti. Re-enter them.
- **Several deliveries:** after saving, the form is ready for the next invoice. Supplier and Date stay selected.

**Failure and recovery:**

| Message | Fix |
|---|---|
| "Select a product." | Choose a product before **Add Line**. |
| "Qty and Rate must be greater than zero." (Figure 57) | Enter both before **Add Line**. |
| "Supplier and Invoice No are required." (Figure 58) | Type the invoice number. |
| "Add at least one item line." | Add at least one line before saving. |
| Saved the same invoice twice | The application does not detect duplicate invoice numbers, and purchases cannot be deleted. Record the extra quantity as damage with a clear remark (see [TROUBLESHOOTING.md](TROUBLESHOOTING.md#5-stock-quantities-are-wrong)), and inform your administrator. |

---

## W13. Record damaged or lost stock

**Steps:**

1. Click **Damage Entry** under **STOCK** (Figure 63).
2. If the **Reason** list is empty or lacks the reason you need, click **+** next to **Reason**, type it (for example `Broken in transit`), and click **OK** (Figure 64). Check for "Reason added."
3. Fill in the form (Figure 66):
   - **Date**: when the damage was found.
   - **Product**: `SRF-001 - Sunrise Cream Biscuits`.
   - **Qty**: `2`.
   - **Reason**: `Broken in transit`.
   - **Found At**: `In Transit` (or `Godown`, or `Returned by Party`).
   - **Remarks**: optional, for example `Two cartons crushed during unloading`.
4. Click **Save (Ctrl+S)**.
5. Check for **"Damage entry saved."** (Figure 67) and the new row in **Damage Register** with its Ref No and **Loss Amount** (Figure 68).

**Expected result:** Stock decreases by the quantity. The loss (Qty × average cost) appears in the Damage Report and in Profit & Loss as **Damage Loss**.

**Failure:** "Insufficient stock for '…'. Available: …, requested: …." (Figure 65): you cannot record more damage than the stock held. Check the quantity. "Product, Reason and Found At are required." / "Quantity must be greater than zero.": complete the form.

---

## W14. Create a sale bill

**Purpose:** Sell goods to a customer, collect cash if any, and update stock and the customer's account.

**Prerequisites:** The customer exists and the products have stock.

**Steps:**

1. Click **Party Billing** under **SALES**, or click **New Bill (F2)** on the Dashboard (Figure 69).
2. **Customer**: choose `C-001 - Al-Madina General Store`. Read **Previous Balance** (for example Rs 5,000) and tell the customer if necessary.
3. **Date**: leave as today unless you are entering an older bill.
4. Add the first item:
   1. **Product**: `SRF-001 - Sunrise Cream Biscuits`. **Rate** fills in automatically with the sale rate, `2,100.00` (Figure 71).
   2. **Qty**: `5`.
   3. **Rate**: change it only if you agreed a different price.
   4. **Discount %**: `2` (optional).
   5. Click **Add Line**. The line shows **Net Amount** 10,290.
5. Repeat step 4 for each product, for example `CRB-001`, Qty `4`, Discount `0`.
6. **Bill Discount**: optional extra discount in rupees, for example `100`.
7. **Cash Received**: the money the customer pays now, for example `10000`. **Whatever you do not enter here is added to the customer's balance as credit**, so if the customer is paying in full, type the full Net Bill. Leave 0 only for a fully credit sale.
   - **Received In**: choose **Cash** or **Bank** for that money.
8. Check the line **Net Bill: Rs 15,950 | New Balance: Rs 10,950** (Figure 72).
9. Click **Save Bill (Ctrl+S)**.
10. Check the green message. It gives the bill number and how much went on credit, for example **"Bill BILL-00004 saved. Rs 5,950 on credit — new balance Rs 16,900."** A fully paid bill shows **"… saved. Fully paid."** (Figure 73). Note the bill number.
11. The lines clear and **Previous Balance** now shows the customer's new balance. Scroll down to see the bill under **Today's Bills** (Figure 75).

**Expected result:**

| What changed | Where to check |
|---|---|
| Stock decreased | Products → In Stock, Daily Closing Stock → Sold |
| Customer balance increased by the net bill and decreased by the cash | Customers, Party Ledger, Market Credit |
| Sale recorded | Daily Sale Report, Profit & Loss |
| Cash recorded as a receipt (RCPT-…) | Party Ledger |

**Alternative paths:**
- **Cash sale:** enter the full Net Bill as **Cash Received**. The New Balance stays equal to the Previous Balance.
- **Customer pays more than the bill:** enter the full amount. The extra reduces the old balance.
- **Customer pays after delivery:** save the bill with the cash you actually received, then record the rest later on **Receive Payment** ([W15](#w15-record-a-payment-against-a-customers-balance)).
- **Wrong line added:** click **Clear (Esc)** and add the lines again. (Clear keeps the customer and date.)

**Failure and recovery:**

| Message | Cause | Fix |
|---|---|---|
| "Select a customer." | No customer chosen | Choose a customer. |
| "Select a product." / "Qty and Rate must be greater than zero." | Line incomplete | Choose a product and enter Qty and Rate above 0. |
| "Add at least one item." (Figure 70) | No lines | Add lines before saving. |
| "Insufficient stock for '…'. Available: …, requested: …." (Figure 74) | Not enough stock | The bill is not saved and the lines remain. Click **Clear (Esc)**, re-add the lines with a smaller quantity, and save. Check whether a purchase still needs to be entered. |
| "Discount …% exceeds max allowed …% for …." (Figure 106) | Operator or Viewer gave more than the product's Max Discount % | Reduce the discount (Clear and re-add), or ask an Admin to create the bill. |
| Customer is missing from the list | The customer was not saved, or you are looking at the wrong code | Check the Customers screen, then open Party Billing again — it reloads its list each time. |

> **Before you hand goods over:** a saved bill cannot be edited or cancelled, and individual bills cannot be printed. Check every line before clicking **Save Bill**.

---

## W15. Record a payment against a customer's balance

**Purpose:** Record money a customer pays towards what they already owe, without creating a bill.

**Prerequisites:** The customer exists and has an outstanding balance.

**Steps:**

1. Click **Receive Payment** under **SALES** (Figure 109).
2. **Customer**: choose the customer, for example `C-002 - City Mart`. Read **Current Balance** and tell the customer what is outstanding.
3. **Date**: leave as today, or set the date the money was received.
4. **Amount Received**: type the amount, for example `5000`.
5. **Received In**: choose **Cash** or **Bank**.
6. **Remarks**: optional, for example `Recovery against old balance` (Figure 111).
7. Click **Save Receipt (Ctrl+S)**.
8. Check the green message, which gives the receipt number and the new balance, for example **"Receipt RCPT-00003 saved. New balance: Rs 7,840"** (Figure 112).
9. The payment appears at the top of **Recent Receipts** with **Against Bill** showing **—**, meaning it was not tied to a bill (Figure 113).

**Expected result:**

| What changed | Where to check |
|---|---|
| The customer's balance went down by the amount | **Current Balance** on this screen; Customers → **Balance** |
| A receipt was recorded | Recent Receipts; [Party Ledger](#w16-check-a-customers-account) as a **Receipt** in the **Credit** column |
| Market Credit shows less outstanding and a new Last Payment date | [Market Credit](#w17-see-who-owes-money) |

**Alternative paths:**

- **Customer pays while buying:** enter the money in **Cash Received** on the bill instead ([W14](#w14-create-a-sale-bill)). Do not record it twice.
- **Customer pays more than they owe:** the balance becomes negative, which means you hold an advance for them.

**Failure and recovery:**

| Message | Cause | Fix |
|---|---|---|
| "Select a customer." | No customer chosen | Choose a customer from the list. |
| "Amount must be greater than zero." (Figure 110) | Amount is 0 | Type the amount received. |
| Wrong amount or customer saved | Receipts cannot be edited or deleted | Inform your administrator. Do not try to "fix" it with a second receipt without recording why. |

> **Check before saving.** A saved receipt cannot be changed or removed.

---

## W16. Check a customer's account

**Steps:**

1. Click **Party Ledger** under **SALES** (Figure 76).
2. **Customer**: choose the customer.
3. **From** and **To**: set the period (default: the last month).
4. Click **View Ledger**.
5. Read the result (Figure 77):
   - The first row (**Opening**) is what the customer owed before the From date.
   - **Sale** rows (Debit) are bills, **Receipt** rows (Credit) are cash received.
   - The last **Running Balance** is what the customer owed on the To date.

**Verify:** for an up-to-today period, the last running balance equals the customer's **Balance** on the Customers screen.

**To give the customer a statement:** click **Export PDF** or **Print** ([W29](#w29-export-or-print-a-report)). You must click **View Ledger** first, otherwise the message "Click View Ledger first." appears.

---

## W17. See who owes money

**Steps:**

1. Click **Market Credit** under **SALES**. The figures are recalculated each time you open the screen (click **Refresh** to recalculate without leaving it).
2. Read the total at the top and the list of customers with outstanding balances, largest first (Figure 78). **Last Payment** shows when money was last received from each.
3. To take the list on the road, click **Export PDF** or **Print** ([W29](#w29-export-or-print-a-report)).
4. For details on one customer, use **Party Ledger** ([W16](#w16-check-a-customers-account)). To record a recovery, use **Receive Payment** ([W15](#w15-record-a-payment-against-a-customers-balance)).

**If the list is empty:** "No outstanding credit — All customers are settled." means no customer owes money.

---

## W18. Record an expense

**Steps:**

1. Click **Expense Entry** under **EXPENSES**, or **Add Expense** on the Dashboard (Figure 79).
2. **Date**: the date the money was spent.
3. **Category**: choose one. If it is missing, click **+**, type the name (for example `Fuel`) and click **OK** (Figure 81).
4. **Amount**: for example `2500`.
5. **Paid From**: `Cash` or `Bank`.
6. **Description**: optional, for example `Delivery van fuel` (Figure 82).
7. Click **Save (Ctrl+S)**.
8. Check for **"Expense saved."** and the new voucher (EXP-…) in **Today's Expenses** (Figures 83 and 84).

**Notes:**
- Freight entered as **Bilti Charges** on Stock Received is already recorded as an expense. Do not enter it again.
- Expenses cannot be edited or deleted. Check the amount before saving.

**Failure:** "Category and Amount are required." (Figure 80): choose a category and enter an amount above 0.

---

## W19. Review expenses

**Steps:**

1. Click **Expense Report** under **EXPENSES** (Figure 85).
2. Read **Category-wise Summary (Month to Date)** for this month's totals by category.
3. To list individual expenses, set **From**, **To** and optionally **Category**, then click **Apply Filter**.
4. Read **Expense Details**. If you see "No expenses in this range", widen the dates or choose **All categories**.

> **Apply Filter** also refreshes the month-to-date summary. To save or print the report, use **Export Excel**, **Export PDF** or **Print** ([W29](#w29-export-or-print-a-report)).

---

## W20. End-of-day closing

**Purpose:** Check the day's sales, stock and expenses before closing.

**Steps:**

1. **Daily Sale Report** (REPORTS): check the date is today and click **View Report**. Review the bill-wise totals: **Net** is the day's sales, **Cash** is cash collected on bills, **Credit** is the amount added to customer accounts (Figure 87). Scroll down for product-wise and company-wise sales (Figure 88). Click **Export PDF** or **Print** to keep a copy.
3. **Expense Entry** → **Today's Expenses**: confirm all of today's expenses are entered.
4. **Daily Closing Stock** (REPORTS): click **View Report**. Compare **Closing** quantities with a physical count of key items (Figure 86). Investigate differences and record damage if needed ([W13](#w13-record-damaged-or-lost-stock)).
5. **Profit & Loss** (REPORTS): From and To = today, click **View Report** for the day's profit (Figure 89).
6. **Receive Payment**: check that all recoveries collected during the day are entered ([W15](#w15-record-a-payment-against-a-customers-balance)).
7. **Market Credit**: review outstanding balances (Figure 78).
8. Make a **backup** ([W27](#w27-back-up-the-database)).

**Expected result:** cash in hand ≈ opening cash + **Cash** on the Daily Sale Report + cash receipts recorded on Receive Payment − cash expenses for the day. Any difference should be investigated.

---

## W21. Calculate profit for a period

**Steps:**

1. Click **Profit & Loss** under **REPORTS**.
2. Set **From** (for example the 1st of the month) and **To** (for example today).
3. Leave **With Expenses** ticked for net profit after business expenses. Tick **Include Self Expense** to also subtract personal expenses.
4. Click **View Report** (required after any change).
5. Read **Gross Profit**, **Profit Before Expense** and **Net Profit** (Figure 89).
6. To keep a copy, click **Export Excel**, **Export PDF** or **Print** ([W29](#w29-export-or-print-a-report)).

**Understanding the example (Figure 89):** Net Sale 35,390 − COGS 29,477 = Gross Profit 5,913. Minus Damage Loss 5,312 = Profit Before Expense 601. Minus Business Expenses 19,000 = Net Profit **−18,399** (a loss for the day, mostly because of the salary payment).

---

## W22. Analyse damage

**Steps:**

1. Click **Damage Report** under **REPORTS**.
2. Set **From**, **To** and **Group By** (**Product**, **Company**, **Reason** or **Month**).
3. Click **View Report**.
4. Review **Qty**, **Loss Amount** and **Main Reason** (Figure 90). The largest losses are listed first.
5. To keep a copy, use **Export Excel**, **Export PDF** or **Print** ([W29](#w29-export-or-print-a-report)).

---

## W23. Create a staff login

**Prerequisites:** You are signed in as an **Admin**.

**Steps:**

1. Click **Users** under **SYSTEM** (Figure 92).
2. If the button reads **Update (Ctrl+S)**, click **Clear (Esc)**.
3. **Username**: for example `operator1`. **Full Name**: for example `Sample Operator`.
4. **Role**: choose carefully. **The default is Admin.** For normal staff choose **Operator** (Figure 94).
5. Click **Save (Ctrl+S)**.
6. The **Set Password** window opens (Figure 95). Type an initial password and click **OK**.
   - The password is visible while you type, so make sure nobody is watching.
   - There are no password rules. Choose at least 8 characters with letters and numbers.
7. Check for **"User created."** and the new row with Status **Active** (Figure 96).
8. Tell the user their username and password privately.

**Failure:** "Username and Full Name are required." (Figure 93); "Username already exists." (Figure 97): choose a different username. Clicking **Cancel** in the Set Password window, or leaving it blank, creates no user.

---

## W24. Reset a forgotten password

Also use this to change the built-in administrator's password after installation, and to change your own password.

**Steps:**

1. Sign in as an Admin and open **Users**.
2. Click the user's row. Confirm the **Username** in the form (Figure 98).
3. Click **Reset Password**.
4. In the **Reset Password** window, type the new password and click **OK** (Figure 99).
5. Check for **"Password reset."** (Figure 100).
6. Give the new password to the user privately. The old password no longer works.

---

## W25. Deactivate or reactivate a staff login

**Steps:**

1. Open **Users** (Admin only) and click the user's row.
2. Check the username in the form.
3. Click **Deactivate** (for an active user) or **Activate** (for an inactive user). **There is no confirmation.**
4. Check for **"User deactivated."** or **"User activated."** and the **Status** column (Figure 101).

**Effect:** a deactivated user gets "Invalid username or password." at sign-in. Their past bills and entries remain.

> Do **not** deactivate your own account or the last active Admin. The application allows it, but then nobody may be able to manage users.

---

## W26. Update company settings

**Steps:**

1. Click **Settings** under **SYSTEM** (Figure 102).
2. Enter **Company Name**, **Phone** and **Address** (Figure 103).
3. Click **Save Settings** and check for **"Settings saved."** (Figure 104).

> In this version these values are stored for future use only. **Invoice Prefix** and **Allow negative stock** have no effect: bills always start with BILL- and selling beyond stock is always refused.

---

## W27. Back up the database

**Prerequisites:** The PostgreSQL client tools are installed on the computer (ask your system administrator).

**Steps:**

1. Click **Settings** under **SYSTEM**.
2. In **Backup & Restore**, click **Backup Database**.
3. In the **Save As** window, choose a safe location, preferably a USB drive or network folder. Keep the suggested file name (it contains the date and time) and click **Save**.
4. Wait. Check for **"Backup created successfully."**
5. Check that the `.backup` file exists in the chosen folder and is not empty.

**Recommended routine:** back up at the end of every working day and keep copies on at least two different devices.

**Failure:** "Backup failed: …". If the message mentions `pg_dump` and "cannot find the file specified", the tools are missing. Contact your system administrator.

---

## W28. Restore the database from a backup

> **Danger:** Restoring **replaces all current data** with the backup. Everything entered after the backup was made is lost. This procedure was **not executed** during the preparation of this manual. Perform it only with your system administrator.

**Steps:**

1. Make sure no one else is using the application.
2. Make a fresh backup of the current data ([W27](#w27-back-up-the-database)) in case you need to go back.
3. Open **Settings** and click **Restore Database**.
4. Select the `.backup` file and click **Open**.
5. Read the question **"Restoring will overwrite existing data. Continue?"** (window **Confirm restore**). Click **Yes** to proceed, or **No** to cancel.
6. Wait for **"Database restored. Please restart the application."**
7. Close the application and start it again.
8. Check a few known records (latest bill numbers, customer balances) to confirm the restore.

**Failure:** "Restore failed: …". Note the message and contact your system administrator before trying again.

---

## W29. Export or print a report

**Purpose:** Save a report as an Excel or PDF file, or print it on paper.

**Applies to:** Daily Closing Stock, Daily Sale Report, Profit & Loss, Damage Report, Expense Report, Party Ledger and Market Credit.

**Prerequisites:** For Party Ledger, click **View Ledger** first. For printing, a printer must be installed on the computer.

**Steps:**

1. Open the report from the side menu.
2. Set the date, date range and any filters you need.
3. Click **View Report** (or **View Ledger** / **Apply Filter**) and check that the screen shows what you want. **The export contains exactly what is on screen.**
4. Click one of:
   - **Export Excel** — an .xlsx file you can open in Excel and work with.
   - **Export PDF** — a PDF for sending or filing.
   - **Print** — opens a print preview.
5. **For an export:** choose the folder, keep or change the suggested file name (it already contains today's date), and click **Save** (Figure 114).
6. When the file is saved the application asks **"Open it now?"** (Figure 115). Click **Yes** to check it, or **No** to carry on working.
7. **For printing:** check the preview (Figure 116), then click the printer icon at the top-left of the preview window, choose your printer and click **Print**. Click **Close** to leave the preview without printing.

**Expected result:** The file appears in the folder you chose, or the report prints. Exported reports carry your business name, address and phone from **Settings** at the top, the dates and filters you chose, and page numbers.

**Alternative paths:**

- **Sharing by email or WhatsApp:** use **Export PDF** and attach the file.
- **Doing your own calculations:** use **Export Excel**. Amounts are saved as real numbers, so Excel can sort and total them.

**Failure and recovery:**

| Message | Cause | Fix |
|---|---|---|
| "There is nothing to export for the selected options." | The report is empty | Widen the date range or clear the filters, click View Report, and try again. |
| "Click View Ledger first." | Party Ledger has not been run | Choose a customer and click **View Ledger**. |
| "The file could not be saved. If it is open in another program, close it and try again." | The file is open in Excel or a PDF reader | Close the file, or save under a different name. |
| "No printer is installed on this computer." | No printer available | Use **Export PDF** and print from another computer. |
| "Export failed: …" | Something else went wrong | Note the message, try a different folder (for example Documents), and tell your administrator if it continues. |

---

## W30. Read a notification from your supplier

**Purpose:** See the news, offers and notices your supplier sends to every shop.

**Prerequisites:** The computer has been online at some point since the message was sent.

**Steps:**

1. Look at the top-right of the window, left of the date. When your supplier has sent something, a
   **bell** appears with a red number (Figure 117).
2. Click the **bell**. The **Notifications** window opens (Figure 118).
3. The list on the left shows every current message, newest first. A **red dot** marks one you have
   not read.
4. Click a message. It opens on the right and is marked as read.
5. Read the text. A poster appears above it; a video has play and volume controls (Figure 119).
6. If there is a button such as **See the offer**, clicking it opens that page in your web browser.
7. Use **Mark all as read** to clear every red dot at once.
8. Close with the **X**.

**Expected result:** The bell's number drops as you read messages, and disappears when none are
unread. The bell itself stays while messages exist.

**Notes:**
- You cannot reply. Phone your supplier.
- Read and unread is remembered on this computer only.
- New messages arrive when you start the application and about once an hour afterwards.

**Failure and recovery:**

| Problem | What to do |
|---|---|
| No bell at all | Nothing has been sent, or this PC has no internet. Check with your supplier. |
| A message says only "Open picture" / "Play video" with buttons | The Microsoft Edge WebView2 Runtime is missing. The buttons still work. Ask for it to be installed. |
| A message disappeared | Your supplier removed it, or it reached its expiry date. |

---

## W31. Install a software update

**Purpose:** Move to the newer version your supplier has released.

**Prerequisites:** The application was installed with the setup program, and the PC has internet.

**Steps:**

1. A window appears by itself: **"A new version is available"** (Figure 120). You can also open it
   from **Settings → Software Update → Check for Updates**.
2. Read **What's new**.
3. **Finish or save whatever you are typing.** Installing closes the application.
4. Click **Download and Install**. A progress bar runs. You may keep working while it downloads.
5. When it says **Ready to install**, click **Restart Now**.
6. Confirm **Yes** to the question about closing.
7. The application closes, installs, and opens again. Sign in as usual.
8. Check the new version under **Settings → Software Update**.

**Expected result:** The installed version is the new one. Your database, customers, bills and
settings are exactly as before.

**Alternative paths:**
- **Not now:** click **Later**. You are offered it again at the next check.
- **Download now, restart later:** after the download click **Restart Later**. It installs the next
  time you choose to restart.

**Failure and recovery:**

| Message | Cause | Fix |
|---|---|---|
| "Download failed: …" | Connection dropped | Click **Try Again**. If it keeps failing, check the internet. |
| "This copy was not installed by the setup program, so it cannot update itself." | Running from a copied folder | Ask your supplier for the installer and install properly. |
| "The update check did not work: …" | No internet, usually | Try later. |
| After restarting, the application asks to be set up again | The computer's settings file was lost | Call your supplier: the database details must be entered again in `C:\ProgramData\DistributionManagementSystem\appsettings.json`. |
