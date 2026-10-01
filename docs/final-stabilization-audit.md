# Current implementation audit (2026-10-01)

Inspected before editing on `main`; implementation branch: `fix/final-universal-matching-forms-stabilization`.

* Listing: `Quantity`/`ReservedQuantity` are base equivalents; `PackageCount`/`ReservedPackageCount` are physical counts. `UnitPrice` is per physical package for PACKAGE/PIECE. Preserve this model.
* Templates: catalog seeds and JSON attribute schemas define modes, units, options and buyer preferences. No package-size source metadata. Sealant incorrectly uses cartridge as a measurement. Flutter and React identify tiles by name substring (also catches Tile Adhesive).
* Custom DTO: uses the same listing request with nullable template ID and custom-review flag. Package type is enum constrained. Category unit validation rejects valid custom measurements. Flutter defaults to can without a package selector and sends lowercase values to an uppercase DTO constraint.
* Requirement DTO: retains template ID, canonical preference JSON, normalized quantity/unit and original entry mode. Existing forms have partial Any support.
* QuantitySemantics: converts compatible dimensions and derives availability from physical counts. It rejects different known template IDs but accepts missing identities without relevance evidence.
* MatchAllocation: physical package count plus listing-base quantity. Selection computes package costs correctly; backend overage validation removes an entire allocation instead of one package.
* Candidate retrieval/recommendation: category-wide retrieval; eligibility and refresh enforce dimensions and known IDs. Missing IDs bypass identity. Scoring formula is separate and must remain unchanged.
* Agent workflow: backend snapshots omit item identity; Python matching compares category, unit, stock and budget. Validation/logistics operate on that result. No distinct relevance stage.
* Seller create/update: validates and saves DRAFT. Publish changes to PENDING_VERIFICATION and notifies managers. Flutter Add/Edit calls only create/update; no Submit action.
* Validation: service returns message-only errors; mobile discards field errors and scrolls only to invalid package count. DTO errors can produce a generic highlighted-fields message without mapping.
* Recommended Matches: all overage disables submit; remaining can be negative. Confirmation multiplies base amount by package price while summary uses package count.
* Selection submission: Flutter calls `/select-matches`, but RequirementsController exposes only `/select-match`. Its 404 becomes “These matches are not available.” This is an endpoint defect, distinct from stock validation.
* React manager approval/details: uses existing pending-listing query and details API, including photos. Keep its approval architecture.
* Localization: Flutter global LocaleController/ARB and React LanguageProvider persist language; many screens and catalog labels still contain untranslated literals. Full coverage requires screen-by-screen conversion and language QA; existing partial infrastructure is not proof of full localization.

Physical Android and live manager E2E remain separate verification gates; automated checks alone cannot certify them.
