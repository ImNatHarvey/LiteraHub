# LiteraHub — Project Brief and Academic Scope

**Updated:** 2026-11-25  
**Academic source:** `05 Case Study -Library Management System` (STI Balagtas, Case Study #5)  
**Repository:** https://github.com/ImNatHarvey/LiteraHub

## Objective

Develop a scaled-down C# and Microsoft SQL library management system for STI Balagtas library staff to manage books, authors, students, and borrowing/return transactions accurately. The academic case study is authoritative; the mockup is a visual reference rather than a replacement for a working WinForms application.

## Required modules and case-study data

| Module | Required information |
|---|---|
| Books | Title, brief description, ISBN, category, author, available copies, condition/status (Good Condition or Damaged) |
| Authors | Author ID, first/middle/last name, birth date, gender |
| Students | Student ID number; first/middle/last name; full address (house number, street, municipality, city/province, ZIP code); birth date; gender; mobile, landline, email |
| Categories | Category ID, category name, category description |
| Borrow and Returns | Student ID, borrow date, return date, remarks/comments, ISBN |

## Required summary lists

1. Books per category.
2. Books per author.
3. Books per status.
4. All books regardless of category or author.

## Required search facilities

- **Books:** ISBN, title, author, category, status.
- **Authors:** Author ID, first/middle/last/complete name, gender.
- **Students:** Student ID, first/middle/last/complete name.
- **Borrow/Return:** Borrow date, return date, student last/first name, ISBN.

## Implementation checkpoint — 2026-11-25

**Developer-tested and pushed:** Categories, Authors, Books and Students catalog/detail CRUD; role-aware actions; shared detail headers/footers; Associated Books gallery; loading/error/empty/retry states; CRUD success feedback; hybrid validation and responsive desktop polish. Latest verified commit: `852b6a77d21a776a892abe294d4f4fdd4a52f249` (F4 validation). This is developer-reported acceptance, not assistant-executed compilation.

**Existing but pending polish:** Staff login, password change, shell navigation and theme presets. **Not yet built/verified:** login carousel and forgot-password recovery, offline PSGC address selection, Borrow & Return UI/business logic, mandatory Reports and end-to-end case-study acceptance.

## Optional enhancements and scope control

- Nine-themed Login carousel, application-only admin-assisted password recovery, offline PSGC address lookup, metadata/cover lookup and student photo previews are product enhancements, not substitutes for mandatory case-study functionality.
- The case study names Municipality and City/Province address fields but does not require Region, Province and Barangay selectors. Adding such selectors must preserve the existing schema or use a separately reviewed versioned migration.
- Staff roles and student records are distinct; a borrower login portal is not required for the academic MVP.
