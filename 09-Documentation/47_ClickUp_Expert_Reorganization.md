# ClickUp Expert Reorganization - Sanzu

## Current Issues
❌ 20 duplicate folders (numbered 01-10 appear twice)
❌ No clear hierarchy
❌ Empty folders everywhere
❌ Tasks scattered without structure
❌ No tagging system
❌ No custom fields
❌ No statuses defined

## ClickUp Best Practices (Applied)

### 1. Hierarchy Principles
- **Space** = Company/Product (Sanzu)
- **Folder** = Major theme/epic (max 5-7)
- **List** = Workflow/sprint (actionable)
- **Task** = Deliverable (1-5 days work)

### 2. Recommended Templates
From https://clickup.com/templates:
- **Product Development Roadmap** (base structure)
- **Sprint Planning** (active work)
- **Design System** (UX/brand)
- **Documentation Hub** (central docs)

### 3. Clean Structure

```
Sanzu (Space)
│
├── 📚 DOCS (Central Knowledge Base)
│   ├── Product Documents (Doc Hub: 2kyqyj0n-1175)
│   ├── Technical Specs
│   └── Design System
│
├── 🎯 ACTIVE SPRINT (Current Work - Week View)
│   ├── This Week
│   └── Next Week
│
├── 📦 PRODUCT (Strategic Layer)
│   ├── Vision & Strategy
│   ├── User Research
│   └── Roadmap & OKRs
│
├── 🎨 DESIGN (UX/UI)
│   ├── Wireframes & Prototypes
│   ├── Brand Identity
│   └── Design Reviews
│
├── 🏗️ ENGINEERING (Build)
│   ├── Architecture
│   ├── V1.0 Backlog
│   └── Technical Debt
│
├── 🚀 LAUNCH (GTM)
│   ├── Pilot Preparation
│   ├── Marketing Assets
│   └── Sales Enablement
│
└── 📊 METRICS (Analytics)
    ├── KPIs Dashboard
    └── Event Tracking
```

## Custom Fields (All Lists)

**Universal Fields:**
- Status (dropdown): Draft, In Progress, Review, Done, Blocked
- Priority (dropdown): Critical, High, Medium, Low
- Phase (dropdown): Phase 1-5
- Owner (person)
- Due Date
- Effort (dropdown): XS, S, M, L, XL
- Type (dropdown): Doc, Code, Design, Research, GTM

**Engineering-Specific:**
- Sprint (dropdown): Week 1-4
- Story Points (number): 1,2,3,5,8
- Epic (relationship)

## Tagging System

**Phase Tags:**
- `p1-vision` `p2-discovery` `p3-opportunity` `p4-solution` `p5-definition`

**Type Tags:**
- `documentation` `code` `design` `research` `architecture`

**Status Tags:**
- `next` `waiting` `someday` `blocked`

**Priority Tags:**
- `urgent` `high-impact` `quick-win` `tech-debt`

## Implementation Steps

### Phase 1: Delete Duplicates ✓
Delete folders: 901514323931-954 (10 empty duplicates)

### Phase 2: Create Clean Structure ✓
1. Keep: 901514323341-358 (Phase folders 01-10)
2. Rename to clean names
3. Add lists with custom fields
4. Create Doc Hub pages

### Phase 3: Migrate Content ✓
1. Move all tasks to correct lists
2. Add tags and custom fields
3. Link documents to tasks
4. Archive completed work

### Phase 4: Set Automations
- Auto-assign based on type
- Auto-tag based on folder
- Status transitions notifications
- Deadline reminders

## Document Organization

### ClickUp Doc Hub: 2kyqyj0n-1175

**Structure:**
```
📚 Sanzu Docs
├── 📋 Product
│   ├── Vision & Strategy
│   ├── PRD & Requirements
│   └── User Research
├── 🏗️ Technical
│   ├── Architecture (.NET Azure)
│   ├── Data Models
│   └── API Specs
├── 🎨 Design
│   ├── Brand Identity
│   ├── Wireframes
│   └── Design System
├── 📊 Analytics
│   ├── KPIs & Metrics
│   └── Event Taxonomy
└── 🚀 GTM
    ├── Pricing & Packaging
    └── Pilot Playbook
```

## List Templates

### Template: Development List
**Custom Fields:**
- Sprint, Story Points, Epic, Owner, Status, Priority
**Views:**
- Board (by Status)
- List (by Sprint)
- Calendar (by Due Date)
- Gantt (timeline)

### Template: Documentation List
**Custom Fields:**
- Doc Type, Owner, Status, Phase, Last Updated
**Views:**
- List (all docs)
- Board (by Status)
- Table (searchable)

### Template: Design List
**Custom Fields:**
- Design Phase, Feedback Status, Figma Link, Owner
**Views:**
- Gallery (visual)
- Board (by Status)
- List (detailed)

## Dashboard Setup

**Space Overview Dashboard:**
- Sprint progress (% complete)
- Blockers count
- Due this week
- Phase distribution (pie chart)
- Velocity trend (line chart)
- Document count by type

## Recommended Views Per List

**Active Sprint:**
- Board view (Status columns)
- Me Mode (My Tasks)
- Calendar (This Week)

**Backlog:**
- List view (Priority sorted)
- Box view (Epic grouped)

**Docs:**
- Table view (Searchable)
- Recently Updated

## Execution Plan

### Immediate (Today)
1. ✅ Create master doc with new structure
2. ⏳ Delete duplicate folders
3. ⏳ Rename existing folders
4. ⏳ Create missing lists with templates
5. ⏳ Set up custom fields

### This Week
6. ⏳ Migrate all 24 documents to tasks
7. ⏳ Create Doc Hub pages
8. ⏳ Add tags to everything
9. ⏳ Set up automations
10. ⏳ Create dashboard

### Next Week
11. ⏳ Train on structure
12. ⏳ Archive old/duplicate content
13. ⏳ Document the system

