# LiteraHub — Project Brief

**Source of truth:** STI Balagtas Case Study #5 PDF  
**Technology:** C#, Windows Forms, Microsoft SQL Server

> This document preserves the previously recorded academic requirements. Recheck the original case-study PDF before changing any mandatory scope. UI progress does not automatically mean a requirement is fully completed.

## Required modules and fields

- [ ] **Books:** Title, brief description, ISBN, category, author, available copies, status (Good Condition / Damaged).
- [ ] **Authors:** Author ID No., first/middle/last name, date of birth, gender.
- [ ] **Students:** Student ID No., first/middle/last name, address (house no., street, municipality, city or province, ZIP code), date of birth, gender, mobile, landline, email.
- [x] **Categories:** Category ID No., category name, category description. CRUD UI developer-tested; verify final case-study acceptance separately.
- [ ] **Borrow and Returns:** Student ID No., borrow date, return date, remarks/comments, ISBN.

## Required summary lists

- [ ] Books per category
- [ ] Books per author
- [ ] Books per status
- [ ] All books regardless of category or author

## Required search

- [ ] Books: ISBN, title, author, category, status (catalog UI implemented; full acceptance not yet established).
- [ ] Authors: Author ID No., name (last/first/middle/complete), gender.
- [ ] Students: Student ID No., name (last/first/middle/complete).
- [ ] Borrow and Returns: Borrow date, return date, student name (last + first), ISBN.

## Objective

Efficient management of books, authors, students, and borrowing/return transactions with accurate records.

## Implementation status note (2026-11-12)

The Books catalog and Book Editor UI exist, including ISBN lookup, cover preview, themed category selection and inventory UI validation. Author relationships and Save Book persistence are not yet complete; therefore Books remains unchecked above.

## Enhancements beyond the case study

- Authentication and role-based access control.
- Nine application appearance presets.
- Themed tables, controls and responsive layouts.
- ISBN metadata import and cover retrieval.
- Planned member requests, digital catalog and PDF/EPUB reader.

These extras must not replace required case-study features.
