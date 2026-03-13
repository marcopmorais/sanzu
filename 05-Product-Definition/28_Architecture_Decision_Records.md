# Architecture Decision Records - Sanzu

---

## ADR-001: Tech Stack Selection

**Date:** 2026-02-11  
**Status:** Proposed  
**Deciders:** Engineering Lead, CTO, Product Manager

### Context

Need to select primary tech stack for Sanzu V1.0 with constraints:
- **Time:** <3 months to pilot launch
- **Team:** 1-2 engineers initially (Portugal-based preferred)
- **Budget:** Early-stage startup (<€50k infra for Year 1)
- **Requirements:** RBAC, file storage, PDF generation, audit trails, future agentic features

### Decision

**Frontend:**
- Next.js 14 (App Router) + React 18 + TypeScript
- Tailwind CSS + shadcn/ui components
- React Hook Form + Zod (validation)

**Backend:**
- Node.js 20 LTS + Express (or Fastify for performance)
- TypeScript (strict mode)
- Prisma ORM (PostgreSQL)

**Database:**
- PostgreSQL 15 (primary data + audit logs)
- Redis 7 (sessions, cache, job queue)

**File Storage:**
- AWS S3 or Cloudflare R2
- Signed URLs for secure access (15-min expiry)

**Auth:**
- Clerk (or Auth0 if budget allows)
- RBAC via metadata + Prisma middleware

**PDF Generation:**
- Puppeteer (headless Chrome)
- React-based templates → HTML → PDF

**Job Queue:**
- BullMQ (Redis-based)
- For PDF generation, email sending

**Deployment:**
- Frontend: Vercel
- Backend: Railway or Render
- Database: Neon (serverless PostgreSQL) or Railway

**Monitoring:**
- Sentry (errors)
- PostHog (product analytics)
- Vercel Analytics (performance)

### Rationale

**Why Next.js:**
- Fastest path to production (file-based routing, API routes, SSR)
- Great DX for Portuguese engineers (large community)
- Vercel deployment = zero-config CI/CD
- Server components reduce client bundle size

**Why PostgreSQL:**
- Proven for complex relational data (cases, steps, documents)
- JSONB for flexible questionnaire responses
- Strong audit log support with row-level versioning
- Mature ecosystem (Prisma ORM)

**Why TypeScript Everywhere:**
- Type safety critical for rules engine (step dependencies, triggers)
- Better refactoring as product evolves
- Reduces bugs in production

**Why Clerk:**
- Fastest auth implementation (< 1 day)
- Built-in RBAC metadata storage
- Good EU data residency options
- Generous free tier

**Why Puppeteer over alternatives:**
- Full control over PDF output (vs paid services like DocRaptor)
- React templates = reuse frontend components
- Handles complex layouts (tables, multi-page)

### Consequences

**Positive:**
- Rapid prototyping (leverage Next.js ecosystem)
- Large talent pool for hiring
- Low infrastructure costs (<€500/month for pilot)
- Type safety reduces production bugs

**Negative:**
- Node.js may hit performance limits at scale (acceptable for V1-V2)
- Vendor lock-in risk with Vercel/Clerk (mitigated by standard APIs)
- Puppeteer requires more memory than lighter PDF libs

### Alternatives Considered

**Python (FastAPI + Django):**
- Pro: Great for ML/agentic features (V3+)
- Con: Slower frontend integration; harder to find full-stack talent in PT
- **Decision:** Revisit for V3 if agentic features require Python

**Ruby on Rails:**
- Pro: Rapid development for CRUD apps
- Con: Smaller talent pool in Portugal; less modern ecosystem
- **Decision:** Rejected

**Supabase (full-stack):**
- Pro: Fastest MVP (auth + DB + storage built-in)
- Con: Less control over agentic architecture; vendor lock-in
- **Decision:** Rejected for V1; could use for prototyping

**LaTeX/Typst for PDFs:**
- Pro: Better typography than HTML→PDF
- Con: Harder to template; steeper learning curve
- **Decision:** Rejected; prioritize speed

### Implementation Notes

- Start with monorepo (Turborepo) for frontend/backend
- Use feature flags (PostHog) for gradual rollouts
- Set up CI/CD with GitHub Actions (lint → test → deploy)

### Review Date

**After Pilot (Week 16):** Re-evaluate if stack supports 100+ concurrent cases/month

---

## ADR-002: Database Schema Design for Audit & RBAC

**Date:** 2026-02-11  
**Status:** Proposed  
**Deciders:** Backend Lead, Security Advisor

### Context

Need to design database schema that supports:
- Strict RBAC (Manager/Editor/Reader roles per case)
- Comprehensive audit trail (all state changes)
- Efficient querying for dashboard (multi-case agency view)
- Document versioning
- Privacy controls (restricted docs)

### Decision

**Multi-tenancy Strategy:** Case-centric with org-scoping
- All queries scoped by `case_id` or `org_id`
- No shared data between organizations
- Prisma middleware enforces scoping

**Audit Strategy:** Append-only audit_events table
- Every write operation emits event
- Include: actor_user_id, case_id, event_type, metadata_json, created_at
- Retention: 24 months (configurable)

**Document Versioning:** Separate versions table
- documents table = metadata only
- document_versions table = S3 keys, checksums, created_by
- Soft delete (deleted_at) to maintain history

**RBAC Enforcement:** Prisma middleware + API middleware
- Check case_participants table for role
- Editor can only update family-owned steps
- Manager has full case access

### Schema (Key Tables)

```sql
-- Organizations (funeral agencies)
CREATE TABLE organizations (
  org_id UUID PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Users (agency staff + family members)
CREATE TABLE users (
  user_id UUID PRIMARY KEY,
  email VARCHAR(255) UNIQUE NOT NULL,
  clerk_user_id VARCHAR(255) UNIQUE,
  org_id UUID REFERENCES organizations(org_id),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Cases
CREATE TABLE cases (
  case_id UUID PRIMARY KEY,
  org_id UUID NOT NULL REFERENCES organizations(org_id),
  deceased_full_name VARCHAR(255) NOT NULL,
  date_of_death DATE NOT NULL,
  status VARCHAR(50) DEFAULT 'Draft', -- Draft/Active/Closing/Archived
  created_by UUID REFERENCES users(user_id),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_cases_org_status ON cases(org_id, status, updated_at);

-- Case Participants (RBAC)
CREATE TABLE case_participants (
  participant_id UUID PRIMARY KEY,
  case_id UUID NOT NULL REFERENCES cases(case_id) ON DELETE CASCADE,
  user_id UUID NOT NULL REFERENCES users(user_id),
  role VARCHAR(50) NOT NULL, -- Manager/Editor/Reader
  invited_at TIMESTAMPTZ DEFAULT NOW(),
  accepted_at TIMESTAMPTZ,
  UNIQUE(case_id, user_id)
);

CREATE INDEX idx_participants_case ON case_participants(case_id);
CREATE INDEX idx_participants_user ON case_participants(user_id);

-- Workflow Step Instances
CREATE TABLE workflow_step_instances (
  step_instance_id UUID PRIMARY KEY,
  case_id UUID NOT NULL REFERENCES cases(case_id) ON DELETE CASCADE,
  step_key VARCHAR(100) NOT NULL, -- e.g., 'core_death_registration_proof'
  owner_type VARCHAR(50), -- Agency/Family
  status VARCHAR(50) DEFAULT 'NotStarted', -- NotStarted/Blocked/Ready/InProgress/Completed
  criticality VARCHAR(50), -- mandatory/optional
  completed_by UUID REFERENCES users(user_id),
  completed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_wsi_case_status ON workflow_step_instances(case_id, status);

-- Documents
CREATE TABLE documents (
  doc_id UUID PRIMARY KEY,
  case_id UUID NOT NULL REFERENCES cases(case_id) ON DELETE CASCADE,
  doc_type VARCHAR(100) NOT NULL, -- e.g., 'death_registration_proof'
  sensitivity VARCHAR(50) DEFAULT 'normal', -- normal/restricted
  latest_version_id UUID,
  created_by UUID REFERENCES users(user_id),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  deleted_at TIMESTAMPTZ -- soft delete
);

CREATE INDEX idx_documents_case ON documents(case_id) WHERE deleted_at IS NULL;

-- Document Versions
CREATE TABLE document_versions (
  version_id UUID PRIMARY KEY,
  doc_id UUID NOT NULL REFERENCES documents(doc_id) ON DELETE CASCADE,
  s3_key VARCHAR(500) NOT NULL,
  filename VARCHAR(255),
  content_type VARCHAR(100),
  size_bytes INTEGER,
  checksum_sha256 VARCHAR(64),
  created_by UUID REFERENCES users(user_id),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Audit Events (append-only)
CREATE TABLE audit_events (
  event_id UUID PRIMARY KEY,
  case_id UUID REFERENCES cases(case_id),
  actor_user_id UUID REFERENCES users(user_id),
  event_type VARCHAR(100) NOT NULL, -- e.g., 'case_created', 'step_status_changed'
  metadata_json JSONB, -- flexible payload
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_audit_case ON audit_events(case_id, created_at);
CREATE INDEX idx_audit_type ON audit_events(event_type, created_at);
```

### Consequences

**Positive:**
- Strict RBAC enforcement at DB level
- Complete audit trail for compliance
- Efficient queries for dashboards (indexes optimized)

**Negative:**
- audit_events table grows quickly (plan for partitioning at scale)
- Soft deletes add complexity to queries (require WHERE deleted_at IS NULL)

### Alternatives Considered

**Row-level security (RLS):**
- Pro: Automatic scoping at DB level
- Con: Harder to debug; less flexible for complex logic
- **Decision:** Use Prisma middleware instead for clarity

**Event sourcing:**
- Pro: Perfect audit trail; rebuild state from events
- Con: Much higher complexity; not needed for V1-V2
- **Decision:** Revisit for V4 if needed

### Review Date

After pilot (Week 16): Assess query performance under real load

---

## ADR-003: PDF Generation Strategy

**Date:** 2026-02-11  
**Status:** Proposed  
**Deciders:** Backend Lead, Frontend Lead

### Context

Need to generate professional PDFs for:
- Bank notification letters
- Insurance claim requests
- Service cancellation requests
- Case checklists
- (Future) Evidence export packages

Requirements:
- Pre-fill with case data (deceased name, dates, etc.)
- Professional formatting (letterhead, signatures)
- Multi-page support (long documents)
- Fast generation (<10 seconds)
- Cost-effective (<€0.10/PDF)

### Decision

**Tool:** Puppeteer (headless Chrome)

**Template System:**
- React components for each template type
- Shared layout component (header, footer, page numbers)
- CSS for print media (@page rules)
- Tailwind for rapid styling

**Generation Flow:**
1. API endpoint receives template_id + case_data
2. BullMQ job enqueued (async)
3. Worker renders React template with data
4. Puppeteer converts HTML → PDF
5. Upload PDF to S3
6. Return signed URL to client
7. Emit audit event

**Example Template Structure:**
```typescript
// templates/BankNotificationLetter.tsx
export default function BankNotificationLetter({ data }) {
  return (
    <PrintLayout>
      <Letterhead orgName={data.orgName} />
      
      <section className="mt-8">
        <p>Exmo(a). Senhor(a),</p>
        <p className="mt-4">
          Venho por este meio informar do falecimento de {data.deceasedName},
          ocorrido em {formatDate(data.dateOfDeath)}.
        </p>
        {/* ... */}
      </section>
      
      <Signature userName={data.requesterName} />
    </PrintLayout>
  );
}
```

**Puppeteer Config:**
```typescript
const pdf = await page.pdf({
  format: 'A4',
  printBackground: true,
  margin: { top: '2cm', bottom: '2cm', left: '2cm', right: '2cm' },
  displayHeaderFooter: true,
  headerTemplate: '<div></div>',
  footerTemplate: '<div class="text-xs text-gray-500">Página <span class="pageNumber"></span></div>'
});
```

### Rationale

**Why Puppeteer over paid services (DocRaptor, PDFShift):**
- Full control over output (no API rate limits)
- Zero per-PDF cost (only server compute)
- Can reuse React components (DRY principle)

**Why not LaTeX/Typst:**
- Steeper learning curve
- Harder to maintain templates
- Not worth complexity for business letters

**Why not server-side PDF libs (pdfkit, jsPDF):**
- Harder to achieve professional layouts
- More code for complex tables/formatting

### Consequences

**Positive:**
- Reuse frontend components for templates
- Professional output quality
- No per-PDF costs

**Negative:**
- Puppeteer requires ~200MB RAM per instance
- Slightly slower than native PDF libs (~5-10 sec vs 1-2 sec)
- Need to manage job queue for async processing

### Performance Optimization

- Run Puppeteer in separate worker process
- Reuse browser instance (warm pool)
- Cache rendered HTML if data unchanged
- Use BullMQ for queue management

### Implementation Checklist

- [ ] Set up BullMQ with Redis
- [ ] Create PrintLayout component with @media print styles
- [ ] Implement 3 core templates (bank, insurance, service)
- [ ] Add PDF generation endpoint with validation
- [ ] Test with 50+ concurrent jobs (load test)

### Review Date

After V1 pilot (Week 16): Measure generation time distribution

---

## ADR-004: File Upload Strategy

**Date:** 2026-02-11  
**Status:** Proposed  
**Deciders:** Backend Lead, Frontend Lead

### Context

Need secure file upload for:
- Family documents (IDs, death certificates, bank statements)
- Agency-uploaded evidence
- Sizes: 100KB - 50MB typical
- Types: PDF, JPG, PNG primarily

Requirements:
- Secure (no direct S3 access from client)
- Fast (resume/retry for large files)
- RBAC-enforced (Editor/Manager only)
- Virus scanning (nice-to-have for V1)

### Decision

**Upload Flow:** Signed URL (presigned POST)

1. Client requests upload permission:
   ```
   POST /cases/{case_id}/documents/{doc_id}/versions
   { "filename": "cert.pdf", "content_type": "application/pdf" }
   ```

2. Backend validates:
   - User has Editor/Manager role
   - Content-type allowed
   - Case exists and is Active

3. Backend generates signed URL (S3 presigned POST):
   ```json
   {
     "version_id": "ver_abc123",
     "upload_url": "https://s3.../signed-url",
     "upload_fields": { "key": "...", "policy": "..." },
     "expires_at": "2026-02-11T12:15:00Z"
   }
   ```

4. Client uploads directly to S3 using signed URL

5. Client confirms upload:
   ```
   POST /cases/{case_id}/documents/{doc_id}/versions/{version_id}/confirm
   ```

6. Backend verifies file exists in S3, saves metadata

**File Naming Convention:**
```
{org_id}/{case_id}/{doc_id}/{version_id}/{sanitized_filename}
```

**Download Flow:** Signed GET URLs (15-min expiry)

```
GET /cases/{case_id}/documents/{doc_id}/download
→ Returns: { "download_url": "https://s3.../signed-url" }
```

### Rationale

**Why signed URLs over direct backend upload:**
- Reduces backend load (files never touch our servers)
- Faster for user (direct S3 connection)
- Scalable (no bottleneck)

**Why 15-min expiry:**
- Long enough for normal download
- Short enough to prevent link sharing

**Why not client-side S3 SDK:**
- Would expose bucket name and region
- Harder to enforce RBAC

### Security Measures

- [ ] Validate content-type matches file extension
- [ ] Set max file size (50MB) in signed URL policy
- [ ] Sanitize filenames (remove special chars)
- [ ] Virus scan uploaded files (V1.1 - use ClamAV or service)
- [ ] Rate limit upload requests (10/min per case)

### Implementation Checklist

- [ ] Set up S3 bucket with CORS config
- [ ] Implement presigned POST generation (AWS SDK)
- [ ] Add upload confirmation endpoint
- [ ] Frontend: handle upload progress, retry on failure
- [ ] Test with 50MB files

### Review Date

After V1 pilot: Measure upload success rate

