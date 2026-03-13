# 14 â€” API Contract Drafts (Extended)

## Notes
Contracts are illustrative; align to chosen stack. All endpoints enforce org scoping through case ownership.

## V1 endpoints
### Auth
- POST /auth/invite/accept
- POST /auth/login
- POST /auth/logout

### Cases
- POST /cases
- GET /cases
- GET /cases/{case_id}
- PATCH /cases/{case_id}
- POST /cases/{case_id}/archive

### Participants
- POST /cases/{case_id}/participants (invite Editor/Reader)
- PATCH /cases/{case_id}/participants/{user_id} (reassign Editor)
- DELETE /cases/{case_id}/participants/{user_id}

### Steps / workflow
- GET /cases/{case_id}/steps
- PATCH /cases/{case_id}/steps/{step_instance_id} (status change)
- GET /cases/{case_id}/workstreams (V2+)

### Documents
- POST /cases/{case_id}/documents (create metadata)
- POST /cases/{case_id}/documents/{doc_id}/versions (upload handshake â†’ signed URL)
- GET /cases/{case_id}/documents
- GET /cases/{case_id}/documents/{doc_id}/download (signed URL)

### Templates
- GET /templates
- POST /cases/{case_id}/templates/{template_id}/generate

### Audit
- GET /cases/{case_id}/audit
- GET /cases/{case_id}/audit/export

### Notifications
- GET/PUT /users/me/notification_preferences

## V2 (agency ops)
- GET /orgs/{org_id}/cases?status=&q=&owner=&blocked=
- GET /orgs/{org_id}/analytics (funnel, cycle time, rework)

## V3 (assisted submissions)
- POST /cases/{case_id}/external_requests
- POST /external_requests/{id}/approve
- POST /external_requests/{id}/submit
- GET /external_requests/{id}/status
