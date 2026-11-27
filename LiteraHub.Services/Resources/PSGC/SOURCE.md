# LiteraHub offline PSGC dataset

## Attribution

**Source:** Philippine Statistics Authority (PSA), Philippine Standard Geographic Code (PSGC), 3rd Quarter 2026.

- Official website: https://psa.gov.ph/classification/psgc
- Original workbook: `PSGC-3Q-2026-Publication-Datafile.xlsx`
- Publication date stated in workbook metadata: 30 September 2026
- Source worksheet: `PSGC`; validation worksheet: `National Summary`
- Dataset version: `2026-Q3`
- Source workbook SHA-256: `8aad47f84e4f6317dc4222b9fe35d36b5d71f2b74ad8f0c7622289c131dc9a59`
- Converted JSON SHA-256: `6e61c81b14e11982d21861d126a2f1e8ed0e2cb0bb8e05bd382f1eb8e98fd1b0`

The source workbook's Metadata sheet states that access constraints are none, and its use constraint is acknowledgment of PSA as the source. The metadata also disclaims warranties and states that interpretation and use are the user's responsibility. LiteraHub's converted dataset is not an official PSA publication and does not imply PSA endorsement.

## Record counts and conversion validation

| Geographic level | Records |
| --- | ---: |
| Regions | 18 |
| Provinces | 82 |
| Cities | 149 |
| Municipalities | 1,493 |
| Manila municipal districts | 14 |
| Barangays | 42,010 |
| Total | 43,766 |

All ten-digit geographic codes are preserved as strings, including leading zeros. The converter checks duplicate codes, parent relationships, national counts, and regional counts. The fourteen City of Manila municipal districts are retained separately, with their 897 barangays mapped to the City of Manila for the four-level picker. District captions are included in the barangay picker. Districts are not counted as independent municipalities.

The converted resource excludes population, income, and urban/rural statistics because they are not required for address selection. Leading/trailing formatting whitespace in names is trimmed. The footnote marker in the source name `City of Lucenaa/` is removed from the display name; the unmodified trimmed source name is retained in `sourceName` for code `0431200000`.

Highly urbanized cities and independent component cities use a nullable province relationship for the application's administrative hierarchy, even when they have a geographic association with a province. The workbook notes that Lucena is treated as a highly urbanized city for statistical purposes, which is not a legal determination of its status. NCR, City of Isabela (Region IX), and province-free Special Geographic Area municipalities (BARMM) are handled without creating fictional provinces. Postal ZIP codes are separate from PSGC and are manually entered. This dataset is not a postal-address validator.

## Application resource

`LiteraHub.Services/Resources/PSGC/psgc.json` is embedded in the Services assembly under the logical resource name `LiteraHub.PSGC.Data`. The application does not make internet requests for geographic selection and does not require a separately deployed JSON file or Python installation.

Structured student records store region, optional province, city/municipality and barangay codes alongside readable names. The existing `Municipality` column stores the city/municipality name, and `CityProvince` stores the province name or, where no province applies, the region name. `RegionName` and `BarangayName` store readable names. Existing unstructured addresses retain their original fields and null PSGC columns until deliberately converted by an authorized user.

## Updating to a future PSA release

1. Obtain the original official publication workbook from PSA. Keep it unchanged.
2. Confirm its release date, geographic levels, column definitions and use constraints.
3. From a development terminal, install the converter dependency if needed: `py -m pip install openpyxl`.
4. From the repository root, run the following command, substituting the new source path, version and date:

```powershell
py tools/psgc/convert_psgc.py "C:\path\to\PSGC-3Q-2026-Publication-Datafile.xlsx" "LiteraHub.Services\Resources\PSGC\psgc.json" --version 2026-Q3 --as-of 2026-09-30
```

5. Update this source note and the SHA-256 values. Review the conversion and compare new/deleted/reclassified geographic codes before deploying the new resource.
6. Rebuild the solution and test cascading selectors, independent areas, Manila, legacy address preservation and an isolated database upgrade.

**Important:** The converter depends on the publication workbook layout inspected for 2026-Q3. Future releases may change that layout or contain new geographic exceptions. Do not silently reassign old student codes by name. If a code has been retired, the stored readable address remains viewable; staff must confirm an appropriate replacement before saving a newly structured address. Review any official correspondence/mapping information when updating the dataset.
