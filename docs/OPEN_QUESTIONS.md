# Open Questions

- Does ReaLTaiizor support .NET 10 and is it maintained? (check in Step 4)
- Which icon package: FontAwesome.Sharp or another? (check in Step 4)
- Dark scrollbars: which option works? (spike in Step 4)
- Does the demo laptop have LocalDB or Express? Which server name does it use?
- Release format: plain zip vs installer (decide in Step 8)
- Per-copy tracking is out of scope for the MVP; revisit only if needed.
- Login lockout/throttling after repeated failures: optional, not in MVP.
- Admin password recovery during development without SSMS: ask Claude for a reset option if needed.
- Does FontAwesome.Sharp work cleanly on .NET 10, and are all IconChar names valid? (4A test)
- Does WinForms auto-scale Padding/Margin/TableLayoutPanel sizes at 125%/150%? (4A test)
- Does Application.SetColorMode work without the experimental warning in .NET 10, and does it darken scrollbars? (4B)
- Context menu and title bar theming for dark mode (4B).