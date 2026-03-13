# Public Website Definition (Non-Authenticated) - Sanzu

## Purpose
Define the complete scope, structure, requirements, and success model for Sanzu's public website used by visitors who are not logged in.

## Inputs used (which files were read)
- `_bmad-output/opportunity/opportunity_brief.md`
- `_bmad-output/strategy/product_brief.md`
- `_bmad-output/definition/prd.md`
- `_bmad-output/gtm/launch_plan.md`
- `_bmad-output/gtm/comms_brief.md`
- `_bmad-output/gtm/positioning_narrative.md`
- `_bmad-output/metrics/metrics_plan.md`
- `_bmad-output/metrics/tracking_spec.md`
- `_bmad-output/discovery/portugal_obito_process_deep_dive_input.md`

## Draft content

### 1) Product role of the public website
The public website is Sanzu's acquisition and trust layer before authentication. It must answer:
- What Sanzu is and who it serves.
- Why funeral agencies should adopt it.
- How it handles sensitive post-loss workflows safely.
- What plan/pricing model exists.
- How prospects move into demo/signup/onboarding.

### 2) Primary audiences (pre-login)
- Agency decision-makers (owners, managers) evaluating adoption.
- Agency operators evaluating daily workflow fit.
- Family stakeholders seeking clarity and reassurance.
- Advisors/partners (legal, accounting, social support ecosystem).
- Institutional/compliance reviewers checking trust and governance posture.

### 3) Jobs-to-be-done (website layer)
- Understand Sanzu value in under 60 seconds.
- Validate legal/process seriousness and trustworthiness.
- See pricing/commercial model and onboarding path.
- Request demo or start account creation with low friction.
- Access clear FAQ/resources before committing.

### 4) Conversion goals
- Primary conversions:
  - `request_demo_submitted`
  - `start_agency_signup_clicked`
- Secondary conversions:
  - `pricing_viewed`
  - `legal_policy_viewed`
  - `faq_engaged`
  - `contact_submitted`

### 5) Mandatory sitemap (MVP)
- `/` Home
- `/como-funciona` (How it works)
- `/para-agencias` (Agency value)
- `/para-familias` (Family experience)
- `/precos` (Pricing)
- `/seguranca-e-privacidade` (Security/Privacy trust page)
- `/faq`
- `/recursos` (guides, legal/process explainers)
- `/contacto` (contact/demo form)
- `/sobre` (company credibility)
- `/termos` and `/privacidade` and `/cookies`
- `/login` and `/criar-conta` entry points

### 6) Page-level requirements

#### Home
- Headline: clear Portugal-first post-loss orchestration value.
- Hero CTAs:
  - `Pedir demonstracao`
  - `Criar conta de agencia`
- Core proof blocks:
  - Deterministic workflow reliability
  - Role-safe collaboration
  - Audit/compliance posture
- Social proof placeholders and partner badges.

#### How it works
- Step narrative from case creation to completion.
- Separate lane visualization for agency and family roles.
- Explicit mention of deadlines, dependencies, and guided next actions.

#### For agencies
- Operational outcomes: reduced rework, better coordination, deadline adherence.
- Team/role model overview.
- Onboarding expectations and activation timeline.

#### For families
- Reassurance language and plain explanations.
- Visibility/permissions model.
- What family editors/readers can do.

#### Pricing
- Starter/Growth/Enterprise presentation aligned with PRD.
- Explain overage model, billing cadence, and payment methods.
- CTA: begin account setup.

#### Security and privacy
- GDPR-aligned handling summary.
- Auditability and access controls.
- Data retention and sensitive-document policy summary.
- Link to full legal pages.

#### FAQ and resources
- Questions about legal process timing, roles, documents, and support.
- Distinguish informational guidance from legal advice.
- Link to official Portuguese channels where relevant.

#### Contact/demo
- Form fields: name, agency, role, email, phone optional, case volume band.
- Consent checkbox and privacy notice.
- Lead routing rules and SLA expectations.

### 7) Content and messaging system
- Messaging pillars:
  - Clarity in high-stress process
  - Reliability through deterministic workflows
  - Trust via compliance and audit controls
  - Operational outcomes for agencies
- Copy requirements:
  - Plain Portuguese, low-jargon language
  - Sensitive tone appropriate to bereavement context
  - Explicit "not legal advice" disclaimer where needed

### 8) UX requirements (public, non-auth)
- Mobile-first responsive behavior.
- Clear conversion path visible above the fold on key pages.
- Strong readability and accessibility baseline.
- Fast page loads on average mobile connections.
- No dependency on login to understand value and pricing.

### 9) Accessibility requirements
- WCAG 2.1 AA baseline.
- Keyboard navigation for all interactive controls.
- Form error messaging accessible and specific.
- Color contrast and non-color status cues.

### 10) SEO requirements
- Portuguese-first SEO architecture (`pt-PT`).
- Unique page titles/meta descriptions.
- Structured data where relevant (Organization, FAQ).
- Canonical URLs and sitemap.xml.
- Core keyword clusters:
  - `burocracia apos obito portugal`
  - `habilitacao de herdeiros`
  - `apoio administrativo funeral`
  - agency-oriented workflow terms.

### 11) Analytics and KPI instrumentation
- Event instrumentation (minimum):
  - `page_view`
  - `cta_clicked`
  - `demo_request_submitted`
  - `signup_started`
  - `pricing_interaction`
  - `faq_item_opened`
- Funnel definitions:
  - Visitor -> key page view -> CTA click -> form submit -> qualified lead.
- Dashboard requirements:
  - Traffic by channel
  - Conversion rates by page and CTA
  - Lead quality by segment
  - Drop-off points in demo/signup journey

### 12) Governance and operations
- Owners:
  - Product/GTM owner for message and conversion outcomes
  - Legal/compliance reviewer for policy pages
  - Content owner for resources and FAQ updates
- Cadence:
  - Monthly conversion review
  - Quarterly full content/legal review
- Quality gates:
  - content approval
  - tracking QA
  - accessibility QA
  - legal sign-off

### 13) Integration requirements
- CRM/lead pipeline integration for demo/contact forms.
- Handoff from public CTAs to agency account creation flow.
- Marketing attribution fields preserved through conversion path.

### 14) Risks and mitigations
- Risk: low trust conversion due weak legal/compliance clarity.
  - Mitigation: strong security/privacy page and references to official process guidance.
- Risk: traffic without qualified conversion.
  - Mitigation: tighter audience-specific messaging and CTA experiments.
- Risk: outdated legal/process content.
  - Mitigation: legal-content review schedule and source-date labeling.

### 15) Definition of done (public website scope)
- Public sitemap implemented for all required pages.
- CTA and form flows tracked end-to-end.
- Legal pages and consent flows approved.
- Accessibility baseline and performance targets met.
- KPI dashboard available for admin/GTM review.

### 16) Open questions
- Final brand voice and design system direction for public site.
- CRM platform choice and lead scoring logic.
- Required language coverage beyond Portuguese at launch.
- Which resources are public vs gated.

## Promotion Instructions
- Target canonical folder: `03-Opportunity-Shaping/` (and later `07-GTM-Launch/` for execution assets)
- This status artifact remains in `_bmad-output/opportunity/` until formal promotion is approved.
