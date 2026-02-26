# Post-Build Validation Checklist

Run this checklist after building or publishing InScope.

## Prerequisites

- Windows 10/11 x64
- For publish: copy entire `publish` folder (InScope.exe + Content\) to target

## Checklist

- [ ] **App launches** — Double-click InScope.exe; window opens without error
- [ ] **Start New initializes clean session** — File → Start New → Electrical; document area clears; questions appear
- [ ] **Guided questions append blocks** — Answer Yes to "Does this procedure involve high voltage?"; High Voltage Safety Block appears in document
- [ ] **User edits persist** — Type in document; answer another question; your text remains
- [ ] **RTF blocks load correctly** — Bullets and formatting render in the document
- [ ] **PDF export succeeds** — File → Export to PDF; choose path; PDF opens and shows content
- [ ] **No writes to block source** — Verify Blocks/*.rtf and BlockMetadata/*.json are not modified
- [ ] **Clean Windows test** — Run on VM or machine without .NET SDK; app runs from publish folder

## Sample Procedure Flow (Electrical)

1. File → Start New → Electrical
2. Answer Yes to "Does this procedure involve high voltage?" → elec-000 + elec-001 appear
3. Answer Yes to "Does it use a transformer?" → elec-002 appears
4. Edit document; add notes
5. File → Export to PDF → verify PDF
