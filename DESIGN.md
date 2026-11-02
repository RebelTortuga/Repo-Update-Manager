# Design system

## Surface

The Windows MVP is an **Operate** surface: a person scans tracked projects, understands update state, and starts a read-only check without friction.

## Direction

The interface uses an operations-ledger world rather than a package-manager costume. Its visual character comes from a dark field, compact high-contrast rows, a disciplined acid-lime action color, and a status line that records the most recent check. The functional signature move is the dense release ledger: every row exposes the project, installed value, upstream value, and state in one scan.

The system deliberately borrows the Japanese high-density web direction's density—not its crowded mosaic layout or visual ornament. Standard desktop controls and a conventional three-column manager layout remain familiar.

## Tokens

- Field: `#111A17`
- Panel: `#17231F`
- Quiet panel: `#20302A`
- Rule: `#2B3B35`
- Primary text: `#F1F5F3`
- Secondary text: `#A9B8B1`
- Accent/action: `#DDEB4F`

## Behavior

- The primary action is always `Check now`; it is visually reserved for the lime treatment.
- System status is shown in writing and never by color alone.
- The add form stays visible beside the tracker, so manual tracking is a first-class workflow rather than a hidden dialog.
- Long source/error details wrap rather than truncate silently.

