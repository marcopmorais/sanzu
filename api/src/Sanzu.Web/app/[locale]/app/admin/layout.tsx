"use client";

import { AdminPermissionsProvider, useAdminPermissions } from "@/lib/admin/AdminPermissionsContext";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

const NAV_TABS: { pattern: string; label: string; href: string }[] = [
  { pattern: "/admin/dashboard/*", label: "Dashboard", href: "/app/admin" },
  { pattern: "/admin/tenants", label: "Tenants", href: "/app/admin/tenants" },
  { pattern: "/admin/alerts", label: "Alerts", href: "/app/admin/alerts" },
  { pattern: "/admin/audit", label: "Audit", href: "/app/admin/audit" },
  { pattern: "/admin/revenue", label: "Revenue", href: "/app/admin/revenue" },
  { pattern: "/admin/config/*", label: "Config", href: "/app/admin/config" },
  { pattern: "/admin/team", label: "Team", href: "/app/admin/team" },
  { pattern: "/admin/platform/*", label: "Platform", href: "/app/admin/platform" },
];

function AdminLayoutInner({ children }: { children: ReactNode }) {
  const { permissions, loading, error } = useAdminPermissions();
  const pathname = usePathname();
  const router = useRouter();

  useEffect(() => {
    if (!loading && (error || !permissions)) {
      router.replace("/app");
    }
  }, [loading, error, permissions, router]);

  if (loading) {
    return (
      <main>
        <p className="meta">Loading admin permissions...</p>
      </main>
    );
  }

  if (error || !permissions) {
    return null;
  }

  const visibleTabs = NAV_TABS.filter((tab) =>
    permissions.accessibleEndpoints.some((ep) => ep === tab.pattern || tab.pattern.startsWith(ep.replace("/*", "")))
  );

  return (
    <div className="admin-layout">
      <nav className="admin-nav" aria-label="Admin navigation">
        {visibleTabs.map((tab) => {
          const isActive = pathname === tab.href || (tab.href !== "/app/admin" && pathname.startsWith(tab.href));
          return (
            <a
              key={tab.href}
              href={tab.href}
              className={`admin-nav-tab${isActive ? " admin-nav-tab--active" : ""}`}
              aria-current={isActive ? "page" : undefined}
            >
              {tab.label}
            </a>
          );
        })}
      </nav>
      <div className="admin-content">{children}</div>
    </div>
  );
}

export default function AdminLayout({ children }: { children: ReactNode }) {
  return (
    <AdminPermissionsProvider>
      <AdminLayoutInner>{children}</AdminLayoutInner>
    </AdminPermissionsProvider>
  );
}
