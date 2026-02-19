"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { getAdminPermissions, type AdminPermissionsResponse } from "@/lib/api-client/generated/admin";

interface AdminPermissionsContextValue {
  permissions: AdminPermissionsResponse | null;
  loading: boolean;
  error: string | null;
}

const AdminPermissionsContext = createContext<AdminPermissionsContextValue>({
  permissions: null,
  loading: true,
  error: null,
});

export function AdminPermissionsProvider({ children }: { children: ReactNode }) {
  const [permissions, setPermissions] = useState<AdminPermissionsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadPermissions() {
      try {
        const data = await getAdminPermissions();
        if (!cancelled) {
          setPermissions(data);
          setLoading(false);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load permissions");
          setLoading(false);
        }
      }
    }

    loadPermissions();
    return () => { cancelled = true; };
  }, []);

  return (
    <AdminPermissionsContext.Provider value={{ permissions, loading, error }}>
      {children}
    </AdminPermissionsContext.Provider>
  );
}

export function useAdminPermissions() {
  return useContext(AdminPermissionsContext);
}
