/**
 * Platform Mission Control Interactive Behaviors - Epic 12
 * Admin operational interface for fleet management
 * Pattern: Detect → Explain → Act → Verify
 */

// ===================================
// QUEUE TABS NAVIGATION
// ===================================

/**
 * Initialize queue tabs for switching between different operational queues
 */
function initQueueTabs() {
  const tabs = document.querySelectorAll('.mc-queue-tab');
  const panels = document.querySelectorAll('.mc-queue-panel');

  tabs.forEach(tab => {
    tab.addEventListener('click', function() {
      // Remove active state from all tabs
      tabs.forEach(t => {
        t.classList.remove('mc-queue-tab-active');
        t.setAttribute('aria-selected', 'false');
      });

      // Hide all panels
      panels.forEach(p => {
        p.setAttribute('hidden', '');
      });

      // Activate clicked tab
      this.classList.add('mc-queue-tab-active');
      this.setAttribute('aria-selected', 'true');

      // Show corresponding panel
      const panelId = this.getAttribute('aria-controls');
      const panel = document.getElementById(panelId);
      if (panel) {
        panel.removeAttribute('hidden');
      }
    });
  });
}

// ===================================
// QUEUE ITEM DETAILS TOGGLE
// Expand/collapse drilldown view (Explain step)
// ===================================

/**
 * Initialize queue item detail toggles for expanding reason/event drilldown
 */
function initQueueItemToggles() {
  const toggleButtons = document.querySelectorAll('.mc-queue-item-toggle');

  toggleButtons.forEach(button => {
    button.addEventListener('click', function() {
      const queueItem = this.closest('.mc-queue-item');
      const details = queueItem.querySelector('.mc-queue-item-details');
      const isExpanded = this.getAttribute('aria-expanded') === 'true';

      // Toggle details visibility
      if (isExpanded) {
        details.setAttribute('hidden', '');
        this.setAttribute('aria-expanded', 'false');
      } else {
        details.removeAttribute('hidden');
        this.setAttribute('aria-expanded', 'true');
      }
    });
  });
}

// ===================================
// REMEDIATION MODAL - Closed-Loop Workflow
// Act → Verify (with Impact Preview + Audit Note)
// ===================================

/**
 * Initialize remediation modal for closed-loop remediation workflow
 */
function initRemediationModal() {
  const modal = document.getElementById('remediation-modal');
  const remediateButtons = document.querySelectorAll('[data-action="remediate"]');
  const closeButton = modal?.querySelector('.mc-modal-close');
  const cancelButton = modal?.querySelector('#modal-cancel');
  const confirmButton = modal?.querySelector('#modal-confirm');
  const auditNoteTextarea = modal?.querySelector('#audit-note');

  if (!modal) return;

  // Open modal when "Remediate" is clicked
  remediateButtons.forEach(button => {
    button.addEventListener('click', function() {
      const queueItemId = this.getAttribute('data-queue-item-id');
      const tenantId = this.closest('.mc-queue-item')?.getAttribute('data-tenant-id');

      // Update modal title with tenant context
      const modalTitle = modal.querySelector('#modal-title');
      if (modalTitle && tenantId) {
        modalTitle.textContent = `Remediate Issue - ${tenantId}`;
      }

      // Reset audit note
      if (auditNoteTextarea) {
        auditNoteTextarea.value = '';
      }

      // Show modal
      modal.removeAttribute('hidden');

      // Focus on audit note textarea for keyboard users
      setTimeout(() => auditNoteTextarea?.focus(), 100);
    });
  });

  // Close modal handlers
  const closeModal = () => {
    modal.setAttribute('hidden', '');
  };

  closeButton?.addEventListener('click', closeModal);
  cancelButton?.addEventListener('click', closeModal);

  // Close on overlay click
  modal.querySelector('.mc-modal-overlay')?.addEventListener('click', closeModal);

  // Close on Escape key
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && !modal.hasAttribute('hidden')) {
      closeModal();
    }
  });

  // Confirm remediation (Act → Verify)
  confirmButton?.addEventListener('click', function() {
    const auditNote = auditNoteTextarea?.value.trim();

    // Validate audit note is provided
    if (!auditNote) {
      alert('Audit note is required for compliance. Please explain why you\'re taking this action.');
      auditNoteTextarea?.focus();
      return;
    }

    // Simulate remediation action
    console.log('Remediation applied with audit note:', auditNote);

    // In production: POST to /api/admin/remediate with action, tenant_id, audit_note
    // Then verify via polling or webhook that the action succeeded

    // Success feedback
    alert('✓ Remediation applied successfully!\n\nStatus: Remediation In Progress\nAudit note recorded.\n\nThe system will auto-verify when the issue resolves or re-escalate if needed.');

    closeModal();

    // In production: Update queue item UI to show "Remediation In Progress" status
    // and move it to a monitoring state
  });
}

// ===================================
// FLEET METRICS REFRESH SIMULATION
// Live indicator behavior
// ===================================

/**
 * Simulate live fleet metrics refresh
 */
function initFleetRefresh() {
  const refreshButton = document.querySelector('[aria-label="Refresh fleet data"]');

  if (!refreshButton) return;

  refreshButton.addEventListener('click', function() {
    // Visual feedback
    this.style.opacity = '0.5';
    this.style.transform = 'rotate(360deg)';
    this.style.transition = 'transform 0.6s ease-in-out';

    // Simulate API call delay
    setTimeout(() => {
      this.style.opacity = '1';
      this.style.transform = 'rotate(0deg)';

      // In production: Fetch updated metrics from /api/admin/fleet/posture
      console.log('Fleet metrics refreshed');

      // Update "Last updated" timestamp
      const timestamp = document.querySelector('.mc-section .text-secondary');
      if (timestamp && timestamp.textContent.includes('Last updated')) {
        timestamp.textContent = 'Last updated: Just now';
      }
    }, 600);
  });
}

// ===================================
// KEYBOARD SHORTCUTS (Admin Power User)
// ===================================

/**
 * Initialize keyboard shortcuts for power users
 */
function initKeyboardShortcuts() {
  document.addEventListener('keydown', function(e) {
    // Ignore if user is typing in input/textarea
    if (e.target.matches('input, textarea')) return;

    // CMD/CTRL + K: Focus search (future feature)
    if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
      e.preventDefault();
      console.log('Search shortcut triggered (not implemented in prototype)');
    }

    // R: Refresh fleet data
    if (e.key === 'r' || e.key === 'R') {
      e.preventDefault();
      document.querySelector('[aria-label="Refresh fleet data"]')?.click();
    }

    // Number keys 1-6: Switch queue tabs
    if (e.key >= '1' && e.key <= '6') {
      const tabs = document.querySelectorAll('.mc-queue-tab');
      const tabIndex = parseInt(e.key) - 1;
      if (tabs[tabIndex]) {
        tabs[tabIndex].click();
      }
    }
  });
}

// ===================================
// TELEMETRY TRACKING (Future)
// Track admin operational efficiency
// ===================================

/**
 * Track time-to-diagnosis and time-to-remediation metrics
 * Target: Diagnosis <= 60s, Remediation <= 2m
 */
function trackOperationalMetrics() {
  let diagnosisStartTime = null;
  let remediationStartTime = null;

  // Track when admin opens queue item details (diagnosis starts)
  document.querySelectorAll('.mc-queue-item-toggle').forEach(button => {
    button.addEventListener('click', function() {
      const isExpanded = this.getAttribute('aria-expanded') === 'true';
      if (!isExpanded) {
        diagnosisStartTime = Date.now();
      }
    });
  });

  // Track when admin opens remediation modal (remediation starts)
  document.querySelectorAll('[data-action="remediate"]').forEach(button => {
    button.addEventListener('click', function() {
      remediationStartTime = Date.now();

      // Calculate time-to-diagnosis if we have start time
      if (diagnosisStartTime) {
        const diagnosisTime = (Date.now() - diagnosisStartTime) / 1000;
        console.log(`Time-to-diagnosis: ${diagnosisTime.toFixed(1)}s (target: ≤60s)`);

        // In production: Send to analytics
        // POST /api/admin/telemetry/diagnosis { duration: diagnosisTime, queue_item_id, ... }
      }
    });
  });

  // Track when admin confirms remediation (remediation completes)
  const confirmButton = document.querySelector('#modal-confirm');
  confirmButton?.addEventListener('click', function() {
    if (remediationStartTime) {
      const remediationTime = (Date.now() - remediationStartTime) / 1000;
      console.log(`Time-to-remediation: ${remediationTime.toFixed(1)}s (target: ≤120s)`);

      // In production: Send to analytics
      // POST /api/admin/telemetry/remediation { duration: remediationTime, queue_item_id, ... }
    }
  });
}

// ===================================
// SAFETY GUARDRAILS (Production)
// Prevent destructive actions without confirmation
// ===================================

/**
 * Safety checks for high-risk admin actions
 * Future: Validate impact preview before allowing action
 */
function initSafetyGuardrails() {
  // In production: Add checks like:
  // - Confirm before tenant deletion
  // - Require multi-factor auth for billing overrides
  // - Rate limit bulk actions
  // - Validate RBAC permissions server-side

  console.log('Safety guardrails initialized (prototype mode)');
}

// ===================================
// INITIALIZE ALL INTERACTIONS
// ===================================

document.addEventListener('DOMContentLoaded', function() {
  // Core features
  initQueueTabs();
  initQueueItemToggles();
  initRemediationModal();
  initFleetRefresh();

  // Power user features
  initKeyboardShortcuts();

  // Operational telemetry
  trackOperationalMetrics();

  // Safety
  initSafetyGuardrails();

  console.log('✅ Platform Mission Control initialized');
  console.log('Epic 12: Fleet + Queues + Event Stream + Closed-Loop Remediation - ACTIVE');
  console.log('Keyboard shortcuts: R = Refresh, 1-6 = Switch queues');
});

// ===================================
// LIVE DATA POLLING (Future)
// Auto-refresh queue counts and fleet metrics
// ===================================

/**
 * Poll for updates to keep dashboard live
 * In production: Use WebSocket or SSE for real-time updates
 */
function startLivePolling() {
  // Prototype: Disabled
  // In production:
  // setInterval(async () => {
  //   const response = await fetch('/api/admin/fleet/posture');
  //   const data = await response.json();
  //   updateFleetMetrics(data);
  //   updateQueueCounts(data.queues);
  // }, 30000); // Poll every 30 seconds
}
