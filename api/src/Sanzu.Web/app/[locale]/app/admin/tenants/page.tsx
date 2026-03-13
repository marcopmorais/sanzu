"use client";

import { useEffect, useState, useCallback } from "react";
import {
  getTenants,
  type TenantListItem,
  type PaginatedResponse,
  type TenantListParams,
} from "@/lib/api-client/generated/admin";

const STATUS_OPTIONS = ["", "Active", "Onboarding", "PaymentIssue", "Suspended", "Terminated"];
const HEALTH_BAND_OPTIONS = ["", "Green", "Yellow", "Red"];
const PLAN_OPTIONS = ["", "Inicial", "Profissional", "Agência", "Enterprise"];

const BAND_COLORS: Record<string, string> = {
  Green: "#1e8f4d",
  Yellow: "#b85a2a",
  Red: "#cc0000",
};

export default function TenantListPage() {
  const [data, setData] = useState<PaginatedResponse<TenantListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [name, setName] = useState("");
  const [status, setStatus] = useState("");
  const [healthBand, setHealthBand] = useState("");
  const [planTier, setPlanTier] = useState("");
  const [signupDateFrom, setSignupDateFrom] = useState("");
  const [signupDateTo, setSignupDateTo] = useState("");
  const [page, setPage] = useState(1);

  const loadTenants = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params: TenantListParams = { page, pageSize: 25 };
      if (name) params.name = name;
      if (status) params.status = status;
      if (healthBand) params.healthBand = healthBand;
      if (planTier) params.planTier = planTier;
      if (signupDateFrom) params.signupDateFrom = signupDateFrom;
      if (signupDateTo) params.signupDateTo = signupDateTo;

      const result = await getTenants(params);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load tenants");
    } finally {
      setLoading(false);
    }
  }, [name, status, healthBand, planTier, signupDateFrom, signupDateTo, page]);

  useEffect(() => {
    loadTenants();
  }, [loadTenants]);

  // Debounced name search
  const [nameInput, setNameInput] = useState("");
  useEffect(() => {
    const timer = setTimeout(() => {
      setName(nameInput);
      setPage(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [nameInput]);

  return (
    <main>
      <h1>Tenant Portfolio</h1>
      <p className="meta">Browse and search the tenant portfolio.</p>

      <section className="panel" style={{ marginTop: 14 }}>
        <div
          style={{
            display: "flex",
            gap: 10,
            flexWrap: "wrap",
            alignItems: "end",
            marginBottom: 14,
          }}
        >
          <label>
            <span className="meta">Name</span>
            <input
              type="text"
              placeholder="Search by name..."
              value={nameInput}
              onChange={(e) => setNameInput(e.target.value)}
              style={{ display: "block", marginTop: 2 }}
              data-testid="filter-name"
            />
          </label>

          <label>
            <span className="meta">Status</span>
            <select
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1); }}
              style={{ display: "block", marginTop: 2 }}
              data-testid="filter-status"
            >
              {STATUS_OPTIONS.map((s) => (
                <option key={s} value={s}>{s || "All"}</option>
              ))}
            </select>
          </label>

          <label>
            <span className="meta">Health</span>
            <select
              value={healthBand}
              onChange={(e) => { setHealthBand(e.target.value); setPage(1); }}
              style={{ display: "block", marginTop: 2 }}
              data-testid="filter-health"
            >
              {HEALTH_BAND_OPTIONS.map((h) => (
                <option key={h} value={h}>{h || "All"}</option>
              ))}
            </select>
          </label>

          <label>
            <span className="meta">Plan</span>
            <select
              value={planTier}
              onChange={(e) => { setPlanTier(e.target.value); setPage(1); }}
              style={{ display: "block", marginTop: 2 }}
              data-testid="filter-plan"
            >
              {PLAN_OPTIONS.map((p) => (
                <option key={p} value={p}>{p || "All"}</option>
              ))}
            </select>
          </label>

          <label>
            <span className="meta">From</span>
            <input
              type="date"
              value={signupDateFrom}
              onChange={(e) => { setSignupDateFrom(e.target.value); setPage(1); }}
              style={{ display: "block", marginTop: 2 }}
              data-testid="filter-date-from"
            />
          </label>

          <label>
            <span className="meta">To</span>
            <input
              type="date"
              value={signupDateTo}
              onChange={(e) => { setSignupDateTo(e.target.value); setPage(1); }}
              style={{ display: "block", marginTop: 2 }}
              data-testid="filter-date-to"
            />
          </label>
        </div>

        {loading && <p className="meta">Loading tenants...</p>}

        {error && (
          <p className="meta" style={{ color: "var(--red, #c00)" }}>{error}</p>
        )}

        {!loading && !error && data && (
          <>
            <table className="table" data-testid="tenant-list-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Status</th>
                  <th>Plan</th>
                  <th>Health</th>
                  <th>Signup Date</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((tenant) => (
                  <tr key={tenant.id}>
                    <td>
                      <a href={`/app/admin/tenants/${tenant.id}`}>{tenant.name}</a>
                    </td>
                    <td>{tenant.status}</td>
                    <td>{tenant.planTier ?? "—"}</td>
                    <td>
                      {tenant.healthScore !== null && tenant.healthBand ? (
                        <span
                          style={{
                            color: BAND_COLORS[tenant.healthBand] ?? "#666",
                            fontWeight: 600,
                          }}
                          data-testid="health-badge"
                        >
                          {tenant.healthScore} ({tenant.healthBand})
                        </span>
                      ) : (
                        <span className="meta">—</span>
                      )}
                    </td>
                    <td>{new Date(tenant.signupDate).toLocaleDateString()}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} style={{ textAlign: "center" }}>
                      <p className="meta">No tenants found.</p>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>

            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: 10 }}>
              <p className="meta">
                Page {data.page} of {data.totalPages} ({data.totalCount} total)
              </p>
              <div style={{ display: "flex", gap: 8 }}>
                <button
                  disabled={data.page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Previous
                </button>
                <button
                  disabled={data.page >= data.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </button>
              </div>
            </div>
          </>
        )}
      </section>
    </main>
  );
}
