-- Sanzu Database Schema (Supabase/PostgreSQL)
-- Based on 13_Data_Model_Extended.md

-- Organizations (Funeral Agencies)
CREATE TABLE organizations (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(255) NOT NULL,
  location VARCHAR(100),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Users (Agency staff + Family members)
CREATE TABLE users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email VARCHAR(255) UNIQUE NOT NULL,
  full_name VARCHAR(255),
  org_id UUID REFERENCES organizations(id),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Cases
CREATE TABLE cases (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  org_id UUID NOT NULL REFERENCES organizations(id),
  deceased_full_name VARCHAR(255) NOT NULL,
  date_of_death DATE NOT NULL,
  municipality VARCHAR(100),
  status VARCHAR(50) DEFAULT 'Draft', -- Draft/Active/Closing/Archived
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_cases_org_status ON cases(org_id, status, updated_at);

-- Case Participants (RBAC)
CREATE TABLE case_participants (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  case_id UUID NOT NULL REFERENCES cases(id) ON DELETE CASCADE,
  user_id UUID NOT NULL REFERENCES users(id),
  role VARCHAR(50) NOT NULL CHECK (role IN ('Manager', 'Editor', 'Reader')),
  invited_at TIMESTAMPTZ DEFAULT NOW(),
  accepted_at TIMESTAMPTZ,
  UNIQUE(case_id, user_id)
);

-- Workflow Step Instances
CREATE TABLE workflow_step_instances (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  case_id UUID NOT NULL REFERENCES cases(id) ON DELETE CASCADE,
  step_key VARCHAR(100) NOT NULL,
  title VARCHAR(255) NOT NULL,
  owner_type VARCHAR(50) CHECK (owner_type IN ('Agency', 'Family')),
  status VARCHAR(50) DEFAULT 'NotStarted' CHECK (status IN ('NotStarted', 'Blocked', 'Ready', 'InProgress', 'Completed')),
  criticality VARCHAR(50) CHECK (criticality IN ('mandatory', 'optional')),
  completed_by UUID REFERENCES users(id),
  completed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_wsi_case_status ON workflow_step_instances(case_id, status);

-- Documents
CREATE TABLE documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  case_id UUID NOT NULL REFERENCES cases(id) ON DELETE CASCADE,
  doc_type VARCHAR(100) NOT NULL,
  sensitivity VARCHAR(50) DEFAULT 'normal' CHECK (sensitivity IN ('normal', 'restricted')),
  latest_version_id UUID,
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_documents_case ON documents(case_id) WHERE deleted_at IS NULL;

-- Document Versions
CREATE TABLE document_versions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  doc_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
  storage_path VARCHAR(500) NOT NULL,
  filename VARCHAR(255),
  content_type VARCHAR(100),
  size_bytes INTEGER,
  checksum_sha256 VARCHAR(64),
  created_by UUID REFERENCES users(id),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Questionnaire Responses
CREATE TABLE questionnaire_responses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  case_id UUID NOT NULL REFERENCES cases(id) ON DELETE CASCADE,
  version VARCHAR(10) DEFAULT 'v1',
  responses JSONB NOT NULL,
  completed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  UNIQUE(case_id)
);

-- Audit Events
CREATE TABLE audit_events (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  case_id UUID REFERENCES cases(id),
  actor_user_id UUID REFERENCES users(id),
  event_type VARCHAR(100) NOT NULL,
  metadata JSONB,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_audit_case ON audit_events(case_id, created_at);
CREATE INDEX idx_audit_type ON audit_events(event_type, created_at);

-- Row Level Security Policies

-- Organizations: Users can only see their org
ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;

CREATE POLICY org_access ON organizations
  FOR ALL USING (
    id IN (SELECT org_id FROM users WHERE id = auth.uid())
  );

-- Cases: Managers see all org cases, Editors/Readers see assigned cases
ALTER TABLE cases ENABLE ROW LEVEL SECURITY;

CREATE POLICY case_access ON cases
  FOR ALL USING (
    org_id IN (SELECT org_id FROM users WHERE id = auth.uid())
    OR id IN (SELECT case_id FROM case_participants WHERE user_id = auth.uid())
  );

-- Documents: Restricted docs only visible to Manager/Editor
ALTER TABLE documents ENABLE ROW LEVEL SECURITY;

CREATE POLICY document_access ON documents
  FOR SELECT USING (
    CASE 
      WHEN sensitivity = 'restricted' THEN
        EXISTS (
          SELECT 1 FROM case_participants cp
          WHERE cp.case_id = documents.case_id
          AND cp.user_id = auth.uid()
          AND cp.role IN ('Manager', 'Editor')
        )
      ELSE
        EXISTS (
          SELECT 1 FROM case_participants cp
          WHERE cp.case_id = documents.case_id
          AND cp.user_id = auth.uid()
        )
    END
  );

-- Audit trigger function
CREATE OR REPLACE FUNCTION audit_trigger() RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO audit_events (case_id, actor_user_id, event_type, metadata)
  VALUES (
    COALESCE(NEW.case_id, OLD.case_id),
    auth.uid(),
    TG_TABLE_NAME || '_' || lower(TG_OP),
    jsonb_build_object('old', to_jsonb(OLD), 'new', to_jsonb(NEW))
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Apply audit triggers
CREATE TRIGGER cases_audit AFTER INSERT OR UPDATE OR DELETE ON cases
  FOR EACH ROW EXECUTE FUNCTION audit_trigger();

CREATE TRIGGER workflow_step_instances_audit AFTER UPDATE ON workflow_step_instances
  FOR EACH ROW EXECUTE FUNCTION audit_trigger();

CREATE TRIGGER documents_audit AFTER INSERT OR DELETE ON documents
  FOR EACH ROW EXECUTE FUNCTION audit_trigger();
