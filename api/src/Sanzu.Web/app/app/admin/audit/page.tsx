"use client";

import { useEffect, useState } from "react";
import {
  searchAuditEvents,
  exportAuditEvents,
  type AuditEventItem,
  type AuditFilters,
} from "@/lib/api-client/generated/admin";

export default function AuditEventViewerPage() {
  const [events, setEvents] = useState<AuditEventItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [nextCursor, setNextCursor] = useState<string | undefined>();
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());

  // Filters
  const [eventTypeFilter, setEventTypeFilter] = useState("");
  const [actorFilter, setActorFilter] = useState("");
  const [caseFilter, setCaseFilter] = useState("");
  const [dateFromFilter, setDateFromFilter] = useState("");
  const [dateToFilter, setDateToFilter] = useState("");

  const PAGE_SIZE = 50;

  function buildFilters(cursor?: string): AuditFilters {
    return {
      eventType: eventTypeFilter || undefined,
      actorUserId: actorFilter || undefined,
      caseId: caseFilter || undefined,
      dateFrom: dateFromFilter || undefined,
      dateTo: dateToFilter || undefined,
      cursor,
      pageSize: PAGE_SIZE,
    };
  }

  async function loadEvents(cursor?: string) {
    try {
      setLoading(true);
      const data = await searchAuditEvents(buildFilters(cursor));
      if (cursor) {
        setEvents((prev) => [...prev, ...data.items]);
      } else {
        setEvents(data.items);
      }
      setTotalCount(data.totalCount);
      setNextCursor(data.nextCursor ?? undefined);
      setLoading(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load audit events");
      setLoading(false);
    }
  }

  useEffect(() => {
    loadEvents();
  }, [eventTypeFilter, actorFilter, caseFilter, dateFromFilter, dateToFilter]);

  function toggleExpand(id: string) {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  function renderMetadata(raw: string) {
    try {
      const parsed = JSON.parse(raw);
      const entries = Object.entries(parsed);
      if (entries.length === 0) return <em className="meta">No metadata</em>;
      return (
        <table style={{ fontSize: 12, marginTop: 4 }}>
          <tbody>
            {entries.map(([key, value]) => (
              <tr key={key}>
                <td style={{ fontWeight: 600, paddingRight: 12, verticalAlign: "top" }}>{key}</td>
                <td style={{ wordBreak: "break-all" }}>{String(value)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      );
    } catch {
      return <pre style={{ fontSize: 12 }}>{raw}</pre>;
    }
  }

  async function handleExport(format: "csv" | "json") {
    try {
      await exportAuditEvents(format, buildFilters());
    } catch {
      // Silent — user sees no download
    }
  }

  if (error) {
    return (
      <main>
        <h1>Audit Event Viewer</h1>
        <p className="meta" style={{ color: "var(--red, #c00)" }}>{error}</p>
      </main>
    );
  }

  return (
    <main>
      <h1>Audit Event Viewer</h1>
      <p className="meta">Search, filter, and export platform audit events.</p>

      {/* Filters */}
      <div style={{ display: "flex", flexWrap: "wrap", gap: 12, marginTop: 14, marginBottom: 14 }}>
        <label>
          Event Type:{" "}
          <input
            type="text"
            placeholder="e.g. Admin.Tenant"
            value={eventTypeFilter}
            onChange={(e) => setEventTypeFilter(e.target.value)}
            style={inputStyle}
          />
        </label>
        <label>
          Actor (User ID):{" "}
          <input
            type="text"
            placeholder="GUID"
            value={actorFilter}
            onChange={(e) => setActorFilter(e.target.value)}
            style={inputStyle}
          />
        </label>
        <label>
          Case ID:{" "}
          <input
            type="text"
            placeholder="GUID"
            value={caseFilter}
            onChange={(e) => setCaseFilter(e.target.value)}
            style={inputStyle}
          />
        </label>
        <label>
          From:{" "}
          <input
            type="datetime-local"
            value={dateFromFilter}
            onChange={(e) => setDateFromFilter(e.target.value)}
            style={inputStyle}
          />
        </label>
        <label>
          To:{" "}
          <input
            type="datetime-local"
            value={dateToFilter}
            onChange={(e) => setDateToFilter(e.target.value)}
            style={inputStyle}
          />
        </label>
      </div>

      {/* Export + count */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 14 }}>
        <span className="meta">{totalCount} event{totalCount !== 1 ? "s" : ""} found</span>
        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={() => handleExport("csv")} style={exportBtnStyle}>
            Export CSV
          </button>
          <button onClick={() => handleExport("json")} style={exportBtnStyle}>
            Export JSON
          </button>
        </div>
      </div>

      {/* Table */}
      {events.length === 0 && !loading ? (
        <p className="meta">No audit events match the current filters.</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 14 }}>
          <thead>
            <tr>
              <th style={thStyle}>Timestamp</th>
              <th style={thStyle}>Actor</th>
              <th style={thStyle}>Event Type</th>
              <th style={thStyle}>Case</th>
              <th style={thStyle}>Metadata</th>
            </tr>
          </thead>
          <tbody>
            {events.map((event) => (
              <tr key={event.id}>
                <td style={tdStyle}>{new Date(event.timestamp).toLocaleString()}</td>
                <td style={tdStyle} title={event.actorUserId}>{event.actorName}</td>
                <td style={tdStyle}>{event.eventType}</td>
                <td style={tdStyle}>
                  {event.caseId ? (
                    <span title={event.caseId}>{event.caseId.substring(0, 8)}...</span>
                  ) : (
                    "—"
                  )}
                </td>
                <td style={tdStyle}>
                  <button
                    onClick={() => toggleExpand(event.id)}
                    style={expandBtnStyle}
                    aria-expanded={expandedIds.has(event.id)}
                    aria-label={`Toggle metadata for event ${event.eventType}`}
                  >
                    {expandedIds.has(event.id) ? "Hide" : "Show"}
                  </button>
                  {expandedIds.has(event.id) && (
                    <div style={{ marginTop: 4 }}>{renderMetadata(event.metadata)}</div>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {loading && <p className="meta">Loading...</p>}

      {nextCursor && !loading && (
        <div style={{ marginTop: 14, textAlign: "center" }}>
          <button onClick={() => loadEvents(nextCursor)} style={exportBtnStyle}>
            Load More
          </button>
        </div>
      )}
    </main>
  );
}

const thStyle: React.CSSProperties = {
  textAlign: "left",
  padding: "8px 12px",
  borderBottom: "2px solid var(--border, #e5e7eb)",
  fontWeight: 600,
};

const tdStyle: React.CSSProperties = {
  padding: "8px 12px",
  borderBottom: "1px solid var(--border, #e5e7eb)",
  verticalAlign: "top",
};

const inputStyle: React.CSSProperties = {
  padding: "4px 8px",
  fontSize: 13,
  border: "1px solid var(--border, #ccc)",
  borderRadius: 4,
};

const exportBtnStyle: React.CSSProperties = {
  padding: "6px 14px",
  fontSize: 13,
  border: "1px solid var(--border, #ccc)",
  borderRadius: 4,
  background: "transparent",
  cursor: "pointer",
};

const expandBtnStyle: React.CSSProperties = {
  padding: "2px 8px",
  fontSize: 12,
  border: "1px solid var(--border, #ccc)",
  borderRadius: 4,
  background: "transparent",
  cursor: "pointer",
};
