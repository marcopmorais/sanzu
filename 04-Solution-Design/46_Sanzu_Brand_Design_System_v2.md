# 46_Sanzu_Brand_Design_System (v2 - Updated)

## Brand Identity

### Logo & Visual Mark
**Sanzu** - Modern sans-serif with flowing accent line
- Primary: Deep Ocean Blue `#1E3A5F`
- Accent: Teal `#2DD4BF`

### Color Palette
**Primary:**
- Deep Ocean Blue: `#1E3A5F` (trust, stability)
- White: `#FFFFFF`

**Status:**
- Emerald `#10B981` - Completed ✓
- Amber `#F59E0B` - In Progress ●
- Red `#EF4444` - Blocked ⚠
- Slate `#6B7280` - Pending ○

**Backgrounds:**
- Pale Blue: `#E8F1F8`
- Light Gray: `#F8FAFC`

### Typography
- **Headings:** Inter Bold (18-32px)
- **Body:** Inter Regular (14-16px)
- **UI:** Inter Medium (13-14px)
- **Code:** JetBrains Mono

### Design Principles
1. Progressive disclosure - one step at a time
2. Clear hierarchy - next step prominent
3. Status visibility - progress always clear
4. Minimal cognitive load
5. Respectful, professional tone

## Email-Per-Process Feature

### Process Email Format
**Pattern:** `processo@sanzu.pt/{case-slug}`
**Example:** `processo@sanzu.pt/joao-silva-a1b2c3`

### Email Integration
- Each step can trigger email composer
- Templates pre-filled with case data
- Auto-CC to process email
- Email logged in activity feed
- Attachments from document vault

### UI Components

**Process Email Display:**
```
┌─────────────────────────────────────┐
│ 📧 processo@sanzu.pt/joao-silva-xxx │
│ Email único deste processo          │
└─────────────────────────────────────┘
```

**Email Button in Step:**
```
[📧 Enviar Email] - Opens pre-filled composer
```

**Email Composer:**
- To: Destination (bank/utility/insurer)
- CC: Process email (auto)
- Subject: Template
- Body: Pre-filled from template
- Attachments: From vault
- [Send] → Logs to activity feed

## Core Components

### Progress Ring (Dashboard)
- 200px circular SVG
- Emerald fill for completion
- Center: % + fraction
- Animated transitions

### Next Step Card
- Gradient background (pale blue → white)
- 4px teal left border
- 📌 icon prominent
- Step title bold
- Assignee + deadline
- Email + Details buttons

### Step Card (List View)
- 24px status dot (left)
- Step title (16px bold)
- Meta row: 👤 assignee, 📅 deadline, 📎 docs
- Action buttons: Email + Details
- Hover: shadow + scale

### Workstream Section
- Section header (workstream name + progress)
- 2px gray bottom border
- Steps grouped below
- Collapsible (mobile)

### Progress Bars (Workstream)
- 8px height rounded
- Label + percentage
- Color by status
- Smooth transitions

### Timeline
- 2px gray left line
- 10px teal dots
- Timestamp (12px gray)
- Event text (14px)
- Chronological order

### Email Composer
- Dashed border box
- Read-only fields (To, CC)
- Editable: Subject, Body
- Monospace body font
- Attachment chips
- Send + Edit buttons

## Screen Layouts

### Dashboard
- Header: Logo + process selector + user
- Process email prominent
- Progress ring (center)
- Next step card (highlight)
- Workstream progress bars
- Activity timeline

### Step List
- Grouped by workstream
- Status dots (color-coded)
- Assignee + deadline visible
- Email buttons on active steps
- Expand for details

### Step Detail
- Full step info
- Prerequisites checklist
- Documents section
- Email composer (embedded)
- Complete button (prominent)

## Responsive

**Desktop (1440px):**
- 3-column layout
- Sidebar with workstreams
- Main content + timeline

**Tablet (768px):**
- 2-column
- Collapsible sidebar
- Stacked sections

**Mobile (375px):**
- Single column
- Sticky next step card
- Bottom nav
- Minimal chrome

## Accessibility
- WCAG AA contrast (4.5:1)
- Keyboard navigation
- Focus indicators (2px teal)
- Screen reader labels
- Status = color + icon

## Icon System
- ✓ Completed (green)
- ● In Progress (amber)
- ○ Not Started (gray)
- ⚠ Blocked (red)
- 📌 Next step
- 👤 Person
- 📅 Date
- 📎 Document
- 📧 Email

## Updated Documents
- All wireframes → sanzu-redesigned.html
- Brand doc → this file
- Figma → updated with email feature
