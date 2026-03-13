"use client";

import { useEffect, useState } from "react";
import {
  getDashboardSummary,
  type DashboardResponse,
  type AdminDashboardSummary,
} from "@/lib/api-client/generated/admin";
import { useAdminPermissions } from "@/lib/admin/AdminPermissionsContext";
import { StalenessIndicator } from "@/components/admin/widgets/StalenessIndicator";
import { TenantSummaryWidget } from "@/components/admin/widgets/TenantSummaryWidget";
import { RevenuePulseWidget } from "@/components/admin/widgets/RevenuePulseWidget";
import { HealthOverviewWidget } from "@/components/admin/widgets/HealthOverviewWidget";
import { TopAtRiskWidget } from "@/components/admin/widgets/TopAtRiskWidget";
import { AlertInboxWidget } from "@/components/admin/widgets/AlertInboxWidget";
import { OnboardingStatusWidget } from "@/components/admin/widgets/OnboardingStatusWidget";

export default function AdminDashboardPage() {
  const [dashboard, setDashboard] = useState<DashboardResponse<AdminDashboardSummary> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { permissions } = useAdminPermissions();

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const data = await getDashboardSummary();
        if (!cancelled) {
          setDashboard(data);
          setLoading(false);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load dashboard");
          setLoading(false);
        }
      }
    }

    load();
    return () => { cancelled = true; };
  }, []);

  if (loading) {
    return (
      <main>
        <p className="meta">Loading dashboard...</p>
      </main>
    );
  }

  if (error || !dashboard) {
    return (
      <main>
        <h1>Admin Dashboard</h1>
        <p className="meta" style={{ color: "var(--red, #c00)" }}>
          {error ?? "Unable to load dashboard data."}
        </p>
      </main>
    );
  }

  const summary = dashboard.data;
  const canSeeRevenue = permissions?.accessibleWidgets.includes("revenue") ?? false;

  return (
    <main>
      <h1>Admin Dashboard</h1>
      <p className="meta">Cockpit overview of platform health, tenants, and operations.</p>
      <StalenessIndicator
        computedAt={dashboard.computedAt}
        isStale={dashboard.isStale}
      />

      <div
        className="admin-dashboard-grid"
        style={{
          display: "grid",
          gap: 14,
          gridTemplateColumns: "repeat(3, minmax(0, 1fr))",
          marginTop: 14,
        }}
      >
        <TenantSummaryWidget tenants={summary.tenants} />
        {canSeeRevenue && <RevenuePulseWidget revenue={summary.revenue} />}
        <HealthOverviewWidget health={summary.health} />
        <TopAtRiskWidget topAtRisk={summary.health.topAtRisk} />
        <AlertInboxWidget alerts={summary.alerts} />
        <OnboardingStatusWidget onboarding={summary.onboarding} />
      </div>
    </main>
  );
}
