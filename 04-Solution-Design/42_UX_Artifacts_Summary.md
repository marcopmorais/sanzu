# Sanzu UX Artifacts - Complete Package

## Diagrams Created (FigJam)

### 1. Core User Flow
**Type:** Flowchart  
**Shows:** Complete case lifecycle from creation to archive  
**Users:** All roles  
**Key steps:** Create → Invite → Questionnaire → Execute → Close

### 2. Sequence Diagram
**Type:** Sequence  
**Shows:** Interaction between Manager, Editor, Reader, and System  
**Timeline:** Full case workflow with all touchpoints

### 3. Step Status State Machine
**Type:** State diagram  
**Shows:** Status transitions (NotStarted → Blocked → Ready → InProgress → Completed)  
**Purpose:** Understand workflow engine logic

### 4. Rules Engine Decision Tree
**Type:** Flowchart  
**Shows:** How questionnaire responses trigger workstreams  
**Logic:** Conditional step generation based on inputs

## Screen Specifications (15 screens)

### Agency Screens (5)
1. Cases Dashboard - List/filter/search
2. Create Case - Quick form
3. Case Detail (Manager) - Full control
4. Analytics - V2 preview
5. Close Case - Archive flow

### Family Screens (5)
6. Accept Invite - Onboarding
7. Questionnaire - 5 questions
8. Dashboard (Family) - Simplified
9. Step Detail - Execute
10. Upload Document - Drag/drop

### Shared Screens (5)
11. Document Vault - RBAC
12. Activity Feed - Timeline
13. Generate PDF - Templates
14. Mobile Views - Responsive
15. Step Checklist - Grouped

## Design System

**Colors:**
- Primary: #2C5282 (Trust blue)
- Success: #38A169 (Complete green)
- Warning: #DD6B20 (Blocked orange)

**Typography:** Inter (Bold/Medium/Regular)

**Components:** Cards, progress bars, badges, CTAs

## User Journeys Mapped

1. **Manager Journey:** Create → Monitor → Generate → Close
2. **Editor Journey:** Accept → Questionnaire → Upload → Execute
3. **Reader Journey:** View → Track (read-only)

## Files Location

- sanzu-ux-screens.md - Screen definitions
- 41_Figma_Wireframe_Spec.md - Figma structure
- Diagrams - Embedded in conversation (FigJam widgets)

## Next Steps

To create actual Figma files:
1. Use screen specs to build wireframes
2. Apply design system
3. Create interactive prototypes
4. User testing flows

**Note:** Diagrams displayed as interactive widgets above. For production Figma files, convert specs to actual designs.

