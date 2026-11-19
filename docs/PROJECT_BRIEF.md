# LiteraHub — Project Brief and Academic Scope

**Updated:** 2026-11-20  
**Academic source:** `05 Case Study -Library Management System` (STI Balagtas, Case Study #5)  
**Repository:** https://github.com/ImNatHarvey/LiteraHub

## Objective

Develop a scaled-down C# and Microsoft SQL library management system for STI Balagtas library staff to manage books, authors, students, and borrowing/return transactions accurately. The academic case study is authoritative for required scope; the mockup is a visual reference, not a replacement for the working UI.

## Required modules and exact case-study data

- **Books:** Title, brief description, ISBN, category, author, available copies, condition/status (Good Condition or Damaged).
- **Authors:** Author ID, first/middle/last name, birth date, gender.
- **Students:** Student ID number; first/middle/last name; full address (house number, street, municipality, city/province, ZIP code); birth date; gender; mobile, landline and email.
- **Categories:** Category ID, category name, category description.
- **Borrow and Returns:** Student ID, borrow date, return date, remarks/comments, ISBN.

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

## Current implementation checkpoint

The developer reports Categories, Authors and Books CRUD, filter/sort polish, and the Step 5B.6 inventory fix tested and pushed. These are **developer-confirmed implementation milestones**, not a complete case-study acceptance test. Student Management, Borrow/Return, required reports, and final search acceptance remain to be completed/verified.

## Additional features beyond case study

Staff login, role permissions, theme presets, ISBN lookup, cover preview, enhanced sorting/filtering, future digital reader, and other optional enhancements. Do not let extras displace the academic requirements.
