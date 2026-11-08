# LiteraHub: Project Brief (source of truth: Case Study #5 PDF)

Scaled-down Library Management System for STI Balagtas Library. C# + MS SQL Server. Windows Forms only.

## Modules (fields from the PDF)
- [ ] Books: Title, Brief Description, ISBN, Category, Author, Available Copies, Status (Good Condition / Damaged)
- [ ] Authors: Author ID No, First/Middle/Last name, Date of birth, Gender
- [ ] Students: Student ID No, First/Middle/Last name, Address (house no., street, municipality, city or province, zip code), Date of birth, Gender, Mobile, Landline, Email
- [ ] Categories: Category ID No, Category Name, Category Description
- [ ] Borrow and Returns: Student ID No, Borrow date, Return date, Remarks/Comments, ISBN

## Summary lists
- [ ] Books per Category
- [ ] Books per Author
- [ ] Books per Status
- [ ] All Books (regardless of category or author)

## Search requirements
- [ ] Books: ISBN, Title, Author, Category, Status
- [ ] Authors: Author ID No, Name (last, first, middle, or complete), Gender
- [ ] Students: Student ID No, Name (last, first, middle, or complete)
- [ ] Borrow and Returns: Borrow Date, Return Date, Student Name (last + first), ISBN

## Objective
Efficiently manage books, authors, students and borrow/return transactions with accurate records. MS SQL + C#.

## Extras (not in PDF, built after the checklist above)
- RBAC (Admin / Librarian / Borrower), login
- Dark mode, status pills, auto-suggest controls, soft/hard delete
- BONUS: borrower portal, digital books (EPUB/PDF), reader