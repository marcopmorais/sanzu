# .NET Azure Build - 4 Week Plan

## Week 1: Backend Core

### Day 1-2: Setup
- Create ASP.NET Core 8 Web API solution
- Set up EF Core with Azure SQL
- Configure Azure AD B2C auth
- Deploy to Azure App Service (staging)

### Day 3-4: Core APIs
- Case CRUD endpoints
- User/org management
- RBAC middleware
- Audit logging interceptor

### Day 5-7: Rules Engine
- Implement RulesEngine service
- Step generation from questionnaire
- Status automation logic
- Unit tests (20 scenarios)

## Week 2: Documents & Templates

### Day 8-9: Blob Storage
- Azure Blob upload/download
- SAS token generation
- Document versioning

### Day 10-12: PDF Generation
- QuestPDF templates (3 types)
- Bank notification letter
- Insurance claim request
- Service cancellation

### Day 13-14: Integration
- Wire PDF service to API
- Background jobs (Azure Functions or Hangfire)
- Error handling + retry

## Week 3: Frontend

### Day 15-17: Blazor Server
- Dashboard (case list, progress)
- Questionnaire (branching logic)
- Document upload UI
- Step checklist

### Day 18-19: Auth Integration
- Azure AD B2C login
- Role-based UI (Manager/Editor/Reader)
- Permission checks

### Day 20-21: Polish
- PT-PT language
- Mobile responsiveness
- Activity feed

## Week 4: Launch

### Day 22-23: Testing
- E2E tests (Playwright or Selenium)
- Security review
- Performance testing

### Day 24-25: Deployment
- Production Azure setup
- CI/CD (Azure DevOps or GitHub Actions)
- Monitoring (App Insights)

### Day 26-28: Pilot Prep
- Documentation
- Admin panel
- Pilot onboarding materials

## Azure Resources

**Development:**
- SQL Basic tier (€4/month)
- App Service B1 (€12/month)
- Storage LRS (€2/month)
**Total Dev: €18/month**

**Production:**
- SQL S0 (€13/month)
- App Service S1 (€55/month)
- Storage GRS (€5/month)
- Redis C0 (€14/month)
**Total Prod: €87/month**

## NuGet Packages
```xml
<!-- API -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0" />
<PackageReference Include="Microsoft.Identity.Web" Version="2.15" />
<PackageReference Include="Azure.Storage.Blobs" Version="12.19" />
<PackageReference Include="QuestPDF" Version="2023.12" />

<!-- Testing -->
<PackageReference Include="xUnit" Version="2.6" />
<PackageReference Include="Moq" Version="4.20" />
```

**Timeline: 28 days to pilot-ready**

