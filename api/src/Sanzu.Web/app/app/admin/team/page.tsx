"use client";

import { useEffect, useState } from "react";
import { listTeamMembers, grantAdminRole, revokeAdminRole, type AdminTeamMemberResponse } from "@/lib/api-client/generated/admin";
import { Button } from "@/components/atoms/Button";

const GRANTABLE_ROLES = ["SanzuOps", "SanzuFinance", "SanzuSupport", "SanzuViewer"];

export default function TeamManagementPage() {
  const [members, setMembers] = useState<AdminTeamMemberResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadMembers() {
    try {
      const data = await listTeamMembers();
      setMembers(data);
      setLoading(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load team");
      setLoading(false);
    }
  }

  useEffect(() => {
    loadMembers();
  }, []);

  async function handleGrant(userId: string, role: string) {
    try {
      await grantAdminRole(userId, role);
      await loadMembers();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to grant role");
    }
  }

  async function handleRevoke(userId: string, role: string) {
    try {
      await revokeAdminRole(userId, role);
      await loadMembers();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to revoke role");
    }
  }

  if (loading) {
    return (
      <main>
        <p className="meta">Loading team members...</p>
      </main>
    );
  }

  return (
    <main>
      <h1>Internal Team Management</h1>
      <p className="meta">Manage Sanzu internal team members and their admin roles.</p>

      {error && <p className="meta" style={{ color: "var(--red, #c00)" }}>{error}</p>}

      <section className="panel" style={{ marginTop: 14 }}>
        <table className="table" aria-label="Team members">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>Granted</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {members.map((m) => (
              <tr key={`${m.userId}-${m.role}`}>
                <td>{m.fullName}</td>
                <td>{m.email}</td>
                <td><span className="admin-role-badge">{m.role}</span></td>
                <td>{new Date(m.grantedAt).toLocaleDateString()}</td>
                <td>
                  {m.role !== "SanzuAdmin" && (
                    <Button
                      label="Revoke"
                      variant="secondary"
                      onClick={() => handleRevoke(m.userId, m.role)}
                    />
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="panel" style={{ marginTop: 14 }}>
        <h2>Grant Role</h2>
        <p className="meta">Assign an admin role to an existing team member.</p>
        <div className="actions">
          {GRANTABLE_ROLES.map((role) => (
            <Button
              key={role}
              label={`Grant ${role}`}
              variant="secondary"
              onClick={() => {
                const userId = prompt("Enter user ID:");
                if (userId) handleGrant(userId, role);
              }}
            />
          ))}
        </div>
      </section>
    </main>
  );
}
