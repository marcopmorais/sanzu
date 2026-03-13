# Sanzu - Figma Wireframe Package

## Screens to Design (10 Core + 5 Detail)

### Agency Screens (Manager Role)
1. **Cases Dashboard** - List view with filters
2. **Create Case Form** - Quick creation modal
3. **Case Detail (Manager View)** - Full dashboard with admin controls
4. **Multi-Case Analytics** - V2 feature preview
5. **Close Case Modal** - Archive workflow

### Family Screens (Editor Role)
6. **Accept Invite Landing** - First-time user onboarding
7. **Questionnaire Flow** - 5 questions with branching
8. **Case Dashboard (Family View)** - Simplified progress view
9. **Step Detail** - Execute individual step
10. **Upload Document** - Drag & drop interface

### Shared Screens (All Roles)
11. **Document Vault** - List with RBAC
12. **Activity Feed** - Timeline view
13. **Generate PDF** - Template selection
14. **Mobile Dashboard** - Responsive design
15. **Step Checklist** - Grouped by workstream

## Design System

**Colors (PT Funeral Industry Appropriate):**
- Primary: #2C5282 (Deep blue - trust)
- Secondary: #4A5568 (Slate gray - neutral)
- Success: #38A169 (Green - completed)
- Warning: #DD6B20 (Orange - blocked)
- Background: #F7FAFC (Light gray)
- Text: #1A202C (Almost black)

**Typography:**
- Headings: Inter Bold
- Body: Inter Regular
- Buttons: Inter Medium

**Components:**
- Cards with subtle shadows
- Progress bars (visual clarity)
- Status badges (color-coded)
- CTA buttons (clear hierarchy)

## Figma File Structure

```
Sanzu.fig
├── 📄 Cover (Project overview)
├── 🎨 Design System
│   ├── Colors
│   ├── Typography
│   ├── Components
│   └── Icons
├── 📱 Wireframes - Agency
│   ├── 1. Cases Dashboard
│   ├── 2. Create Case
│   ├── 3. Case Detail (Manager)
│   ├── 4. Analytics (V2)
│   └── 5. Close Case
├── 📱 Wireframes - Family
│   ├── 6. Accept Invite
│   ├── 7. Questionnaire
│   ├── 8. Dashboard (Family)
│   ├── 9. Step Detail
│   └── 10. Upload Document
├── 📱 Wireframes - Shared
│   ├── 11. Document Vault
│   ├── 12. Activity Feed
│   ├── 13. Generate PDF
│   ├── 14. Mobile Views
│   └── 15. Step Checklist
└── 🔗 User Flows
    ├── Agency Flow
    ├── Family Flow
    └── Complete Case Flow
```

## Key Interactions

### Agency Manager Flow
1. Login → Cases Dashboard
2. Click "Create Case" → Modal form
3. Fill form → Invite sent
4. Monitor progress → Case detail view
5. Generate documents → PDF preview
6. Close case → Archive

### Family Editor Flow
1. Email invite → Accept landing
2. Accept → Questionnaire
3. Complete questions → Dashboard
4. View next step → Upload docs
5. Execute steps → Mark complete
6. Track progress → Activity feed

### Reader Flow
1. Email invite → Accept landing
2. View-only dashboard
3. See progress & activity
4. Download documents (non-restricted)

## Responsive Breakpoints

- Desktop: 1440px (primary)
- Tablet: 768px
- Mobile: 375px

## Accessibility

- WCAG AA contrast ratios
- Focus states on all interactive elements
- Keyboard navigation support
- Screen reader labels

