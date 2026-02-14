using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sanzu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGlossaryTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlossaryTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    WhyThisMatters = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Locale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTerms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ThresholdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ThresholdValue = table.Column<int>(type: "int", nullable: false),
                    ActualValue = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RouteTarget = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    MetricKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ThresholdValue = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RouteTarget = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DefaultLocale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    DefaultTimeZone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    DefaultWorkflowKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DefaultTemplateKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CaseDefaultsVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    SubscriptionPlan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SubscriptionBillingCycle = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    PaymentMethodType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PaymentMethodReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InvoiceProfileLegalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InvoiceProfileVatNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InvoiceProfileBillingEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InvoiceProfileCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    SubscriptionActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionCancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionCancellationReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousSubscriptionPlan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FailedPaymentAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastPaymentFailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPaymentFailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NextPaymentRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextPaymentReminderAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPaymentReminderSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    IntentType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TeamSize = table.Column<int>(type: "int", nullable: false),
                    TermsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    Qualified = table.Column<bool>(type: "bit", nullable: false),
                    RouteTarget = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RouteStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RouteFailureReason = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    UtmSource = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UtmMedium = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UtmCampaign = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ReferrerPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    LandingPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicLeads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportDiagnosticSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportDiagnosticSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantPolicyControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPolicyControls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BillingCycleStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillingCycleEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlanCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverageUnits = table.Column<int>(type: "int", nullable: false),
                    OverageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    InvoiceSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingRecords_Organizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AzureAdObjectId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organizations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeceasedFullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DateOfDeath = table.Column<DateTime>(type: "date", nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Urgency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TemplateKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntakeData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntakeCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IntakeCompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_Organizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RoleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InvitedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Organizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Users_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    GrantedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.CheckConstraint("CK_UserRoles_RoleType", "[RoleType] IN ('AgencyAdmin','SanzuAdmin')");
                    table.ForeignKey(
                        name: "FK_UserRoles_Organizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_GrantedBy",
                        column: x => x.GrantedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(127)", maxLength: 127, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Classification = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Optional"),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseDocuments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseHandoffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacketTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "PendingAdvisor"),
                    FollowUpRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    StatusNotes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    LastUpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastStatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseHandoffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseHandoffs_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseParticipants_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseParticipants_Organizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseParticipants_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseParticipants_Users_ParticipantUserId",
                        column: x => x.ParticipantUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AliasEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Active"),
                    RotatedFromAliasId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessAliases_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessAliases_ProcessAliases_RotatedFromAliasId",
                        column: x => x.RotatedFromAliasId,
                        principalTable: "ProcessAliases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadlineSource = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsReadinessOverridden = table.Column<bool>(type: "bit", nullable: false),
                    ReadinessOverrideRationale = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReadinessOverrideByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReadinessOverriddenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepInstances_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(127)", maxLength: 127, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseDocumentVersions_CaseDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "CaseDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractionCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CandidateValue = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    SourceVersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractionCandidates_CaseDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "CaseDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessAliasId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThreadId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    RecipientEmails = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    BodyPreview = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessEmails_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessEmails_ProcessAliases_ProcessAliasId",
                        column: x => x.ProcessAliasId,
                        principalTable: "ProcessAliases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependsOnStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepDependencies_WorkflowStepInstances_DependsOnStepId",
                        column: x => x.DependsOnStepId,
                        principalTable: "WorkflowStepInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowStepDependencies_WorkflowStepInstances_StepId",
                        column: x => x.StepId,
                        principalTable: "WorkflowStepInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_ActorUserId",
                table: "AuditEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_TenantId",
                table: "BillingRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_TenantId_InvoiceNumber",
                table: "BillingRecords",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_CaseId",
                table: "CaseDocuments",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_TenantId_CaseId_Classification",
                table: "CaseDocuments",
                columns: new[] { "TenantId", "CaseId", "Classification" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocuments_TenantId_CaseId_CreatedAt",
                table: "CaseDocuments",
                columns: new[] { "TenantId", "CaseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentVersions_DocumentId_VersionNumber",
                table: "CaseDocumentVersions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentVersions_TenantId_CaseId_DocumentId_CreatedAt",
                table: "CaseDocumentVersions",
                columns: new[] { "TenantId", "CaseId", "DocumentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandoffs_CaseId",
                table: "CaseHandoffs",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandoffs_TenantId_CaseId_UpdatedAt",
                table: "CaseHandoffs",
                columns: new[] { "TenantId", "CaseId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseParticipants_CaseId_Email_Status",
                table: "CaseParticipants",
                columns: new[] { "CaseId", "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseParticipants_CaseId_ParticipantUserId_Status",
                table: "CaseParticipants",
                columns: new[] { "CaseId", "ParticipantUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseParticipants_InvitedByUserId",
                table: "CaseParticipants",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseParticipants_ParticipantUserId",
                table: "CaseParticipants",
                column: "ParticipantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseParticipants_TenantId",
                table: "CaseParticipants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ManagerUserId",
                table: "Cases",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_TenantId_CaseNumber",
                table: "Cases",
                columns: new[] { "TenantId", "CaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_TenantId_Status",
                table: "Cases",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionCandidates_DocumentId_Status",
                table: "ExtractionCandidates",
                columns: new[] { "DocumentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionCandidates_TenantId_CaseId_DocumentId_CreatedAt",
                table: "ExtractionCandidates",
                columns: new[] { "TenantId", "CaseId", "DocumentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_TenantId_Key_Locale",
                table: "GlossaryTerms",
                columns: new[] { "TenantId", "Key", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KpiAlerts_MetricKey_TriggeredAt",
                table: "KpiAlerts",
                columns: new[] { "MetricKey", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KpiThresholds_MetricKey",
                table: "KpiThresholds",
                column: "MetricKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessAliases_CaseId",
                table: "ProcessAliases",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessAliases_RotatedFromAliasId",
                table: "ProcessAliases",
                column: "RotatedFromAliasId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessAliases_TenantId_CaseId_UpdatedAt",
                table: "ProcessAliases",
                columns: new[] { "TenantId", "CaseId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_ProcessAliases_AliasEmail",
                table: "ProcessAliases",
                column: "AliasEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessEmails_CaseId",
                table: "ProcessEmails",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessEmails_ProcessAliasId",
                table: "ProcessEmails",
                column: "ProcessAliasId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessEmails_TenantId_CaseId_ThreadId_SentAt",
                table: "ProcessEmails",
                columns: new[] { "TenantId", "CaseId", "ThreadId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicLeads_IntentType_CreatedAt",
                table: "PublicLeads",
                columns: new[] { "IntentType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportDiagnosticSessions_TenantId_ExpiresAt",
                table: "SupportDiagnosticSessions",
                columns: new[] { "TenantId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_InvitedBy",
                table: "TenantInvitations",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Email",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Email_Pending",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Email", "Status" },
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Status",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantPolicyControls_TenantId_ControlType",
                table: "TenantPolicyControls",
                columns: new[] { "TenantId", "ControlType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_GrantedBy",
                table: "UserRoles",
                column: "GrantedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_TenantId",
                table: "UserRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleType_TenantId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleType", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrgId",
                table: "Users",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepDependencies_CaseId_StepId_DependsOnStepId",
                table: "WorkflowStepDependencies",
                columns: new[] { "CaseId", "StepId", "DependsOnStepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepDependencies_DependsOnStepId",
                table: "WorkflowStepDependencies",
                column: "DependsOnStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepDependencies_StepId",
                table: "WorkflowStepDependencies",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepDependencies_TenantId_CaseId",
                table: "WorkflowStepDependencies",
                columns: new[] { "TenantId", "CaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepInstances_CaseId_StepKey",
                table: "WorkflowStepInstances",
                columns: new[] { "CaseId", "StepKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepInstances_TenantId_CaseId_Status",
                table: "WorkflowStepInstances",
                columns: new[] { "TenantId", "CaseId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "BillingRecords");

            migrationBuilder.DropTable(
                name: "CaseDocumentVersions");

            migrationBuilder.DropTable(
                name: "CaseHandoffs");

            migrationBuilder.DropTable(
                name: "CaseParticipants");

            migrationBuilder.DropTable(
                name: "ExtractionCandidates");

            migrationBuilder.DropTable(
                name: "GlossaryTerms");

            migrationBuilder.DropTable(
                name: "KpiAlerts");

            migrationBuilder.DropTable(
                name: "KpiThresholds");

            migrationBuilder.DropTable(
                name: "ProcessEmails");

            migrationBuilder.DropTable(
                name: "PublicLeads");

            migrationBuilder.DropTable(
                name: "SupportDiagnosticSessions");

            migrationBuilder.DropTable(
                name: "TenantInvitations");

            migrationBuilder.DropTable(
                name: "TenantPolicyControls");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "WorkflowStepDependencies");

            migrationBuilder.DropTable(
                name: "CaseDocuments");

            migrationBuilder.DropTable(
                name: "ProcessAliases");

            migrationBuilder.DropTable(
                name: "WorkflowStepInstances");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
