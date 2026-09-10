# Project Rules

## Mobile UI Design Review

For every feature that adds or changes a user-facing .NET MAUI page, control, or flow:

1. Compare the implementation with the current `design-draft/.../screen.png` for that flow.
2. Verify the layout on a mobile form factor (Windows handysize window 390 × 844 pt
   or iOS-Simulator-Screenshot from `scripts/iOS-Deployment.ps1`).
3. Do not ship horizontal data tables or multiple text buttons in a single row on
   mobile. Use card-based `CollectionView` with `TapGestureRecognizer` +
   `DisplayActionSheet` or `SwipeView` for actions.
4. Ensure `CollectionView`/`ScrollView` are not nested and the list fills the
   remaining screen via `Grid` row `*`.
5. Confirm touch targets are at least 44 × 44 pt and Dark Mode uses
   `AppThemeBinding`.
6. Document the manual UI verification or add an automated UI test before the
   final commit. If no automated UI test exists, capture a screenshot and note
   the tested screen sizes in `docs/help/anwendung/mobile-ui-design.md` or
   `test-results.md`.
