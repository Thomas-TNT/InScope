# Standard Procedures for InScope Content

Research summary of documented procedure standards that can be leveraged as InScope block content. Content is authored as RTF blocks with BlockMetadata, following the [content-lifecycle](content-lifecycle.md) workflow.

---

## Electrical Procedures

### NFPA 70B (Electrical Equipment Maintenance)
- **What:** Primary U.S. standard for electrical maintenance in industrial, commercial, institutional settings. Now a mandatory Standard (2023).
- **Content:** Circuit breaker inspection, transformer tests, disconnect switches, panelboards, batteries, grounding, IR thermography intervals.
- **Forms:** Annex H includes inspection/test report templates (circuit breakers, transformers, overload relays, meters).
- **Licensing:** NFPA standards are copyrighted; purchase required for full text. Eaton and ESFI publish free summaries and white papers.
- **Use:** Adapt structure and headings for RTF blocks; do not copy full text without license. Create procedure blocks inspired by equipment types (e.g., "Transformer inspection," "Circuit breaker test").

### IEEE 3007.2-2010
- **What:** Recommended practice for industrial/commercial power system maintenance.
- **Content:** Equipment maintenance fundamentals, strategies, testing methods.
- **Licensing:** IEEE standards are copyrighted; purchase required.
- **Use:** Reference for procedure scope and technical accuracy; content authored by technical writer.

### OSHA and State OSHA Resources (Public Domain)
- **OSHA 3120** – "Control of Hazardous Energy Lockout/Tagout" (federal publication, public domain).
- **State samples:** South Carolina OSHA, Oregon OSHA publish sample LOTO procedures as free PDFs.
- **Content:** Lockout/tagout steps, energy control procedures, training requirements.
- **Use:** Direct adaptation possible; LOTO is universally required before electrical work. Create blocks such as "Lockout/Tagout procedure" with steps derived from OSHA guidance.

---

## Hydraulic Procedures

### ASTM D4174-17
- **What:** Standard practice for cleaning, flushing, and purification of petroleum fluid hydraulic systems.
- **Content:** Cleaning and purification procedures.
- **Licensing:** ASTM standards are copyrighted; purchase required.
- **Use:** Reference for technical accuracy; author blocks inspired by scope.

### IFPS Hydraulic Maintenance Handbook
- **What:** Training and maintenance guidance from International Fluid Power Society.
- **Content:** Fluid management, temperature control, contamination prevention, filter replacement.
- **Use:** Structure and topics for blocks; verify licensing for direct use.

### NIMS Hydraulic Systems Specialist Standard
- **What:** Performance requirements for hydraulic specialists (maintenance, troubleshooting, planning).
- **Content:** Maintenance tasks, inspection procedures, documentation.
- **Use:** Checklist for procedure coverage.

### OEM and Industry Guides
- **Mobil, Gates** and others publish free care/maintenance guides (e.g., Gates Safe Hydraulics Pocket Guide). Typically allow internal/customer use.
- **Use:** Adapt preventive maintenance steps into RTF blocks; verify terms of use per source.

---

## Mechanical Procedures

### DOE FEMP Operations and Maintenance Best Practices Guide (Release 3.0)
- **What:** Federal Energy Management Program guide; public domain.
- **URL:** https://www.energy.gov/sites/prod/files/2020/04/f74/omguide_complete_w-eo-disclaimer.pdf
- **Content:** O&M structure, equipment types, maintenance checklists, lockout-tagout, safety procedures.
- **Use:** Strong source for mechanical O&M blocks. Public domain allows direct adaptation into RTF blocks.

### Maintenance SOP Templates and Frameworks
- **Sources:** ServiceChannel, GetSockeye, GetMaintainX, Asset Management Professionals publish SOP structure guidance.
- **Content:** Purpose, scope, step-by-step instructions, safety requirements, responsibilities.
- **Use:** Template structure for authoring; content written by technical writer.

---

## Licensing Summary

| Source Type | Examples | InScope Use |
|-------------|----------|-------------|
| Public domain | OSHA publications, DOE FEMP guide | Can adapt and include text in blocks |
| Free summaries/guides | Eaton, ESFI, OEM guides | Use structure; verify terms for direct copying |
| Paid standards | NFPA 70B, IEEE, ASTM | Reference only; author original content |

---

## Implemented Content (from this plan)

- **Electrical:** LOTO block (OSHA-derived), transformer inspection, circuit breaker inspection (NFPA 70B-inspired)
- **Hydraulic:** Fluid management, filter servicing, system inspection (ASTM/NIMS/OEM-inspired); high-pressure safety block (conditional)
- **Mechanical:** LOTO, O&M checklist, inspection (DOE FEMP-derived); rotating equipment safety (conditional)
- **Config:** Added InvolvesHighPressure, InvolvesRotatingEquipment questions

---

## References

- NFPA 70B: https://www.nfpa.org/70b
- OSHA LOTO: https://www.osha.gov/etools/lockout-tagout
- DOE FEMP O&M Guide: https://www.energy.gov/sites/prod/files/2020/04/f74/omguide_complete_w-eo-disclaimer.pdf
- ASTM D4174: https://store.astm.org/d4174-17.html
- IFPS Hydraulic Handbook: https://www.ifps.org/the-hydraulic-maintenance-handbook
