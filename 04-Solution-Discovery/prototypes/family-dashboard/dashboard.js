/**
 * Family Dashboard Interactive Behaviors
 * Epic 9 Prototype - Plain-language glossary + Blocked recovery
 * Grief-aware UX: Smooth, non-jarring interactions
 */

// ===================================
// GLOSSARY PANEL TOGGLES - EPIC 9.1
// ===================================

/**
 * Initialize all glossary triggers on page load
 */
function initGlossaryToggles() {
  const glossaryTriggers = document.querySelectorAll('.glossary-trigger');

  glossaryTriggers.forEach(trigger => {
    trigger.addEventListener('click', function() {
      const panelId = this.getAttribute('aria-controls');
      const panel = document.getElementById(panelId);
      const isExpanded = this.getAttribute('aria-expanded') === 'true';

      // Toggle panel visibility
      if (isExpanded) {
        panel.setAttribute('hidden', '');
        this.setAttribute('aria-expanded', 'false');
      } else {
        panel.removeAttribute('hidden');
        this.setAttribute('aria-expanded', 'true');
      }
    });
  });
}

// ===================================
// ALL STEPS LIST TOGGLE
// Reduces cognitive load by hiding non-critical info by default
// ===================================

/**
 * Initialize the "View all steps" toggle button
 */
function initStepsListToggle() {
  const toggleButton = document.getElementById('toggle-all-steps');
  const stepsList = document.getElementById('all-steps-list');

  if (!toggleButton || !stepsList) return;

  toggleButton.addEventListener('click', function() {
    const isExpanded = this.getAttribute('aria-expanded') === 'true';

    // Toggle list visibility
    if (isExpanded) {
      stepsList.setAttribute('hidden', '');
      this.setAttribute('aria-expanded', 'false');
      this.textContent = 'Ver todos (12)';
    } else {
      stepsList.removeAttribute('hidden');
      this.setAttribute('aria-expanded', 'true');
      this.textContent = 'Ocultar lista';
    }
  });
}

// ===================================
// REDUCED DENSITY MODE (Future Feature)
// Context-aware UI density adjustment for overwhelmed users
// ===================================

/**
 * Activate reduced density mode (increases spacing, font sizes)
 * Future: Could be triggered by user preference or behavioral signals
 */
function activateReducedDensity() {
  document.body.setAttribute('data-density', 'reduced');
  console.log('Reduced density mode activated');
}

/**
 * Deactivate reduced density mode
 */
function deactivateReducedDensity() {
  document.body.removeAttribute('data-density');
  console.log('Normal density mode restored');
}

// ===================================
// SIMULATED INTERACTIONS (Prototype Only)
// Real implementation would connect to backend APIs
// ===================================

/**
 * Simulate document upload action
 */
function simulateDocumentUpload() {
  const uploadButtons = document.querySelectorAll('.btn-primary');

  uploadButtons.forEach(btn => {
    if (btn.textContent.includes('Carregar Documento')) {
      btn.addEventListener('click', function() {
        // In real app: Open file picker, upload to backend
        alert('Prototype: Documento carregado com sucesso!\n\nEm produção, isto abriria um seletor de ficheiros.');
      });
    }
  });
}

/**
 * Simulate adding NIF to unblock step
 */
function simulateAddNIF() {
  const nifButtons = document.querySelectorAll('.btn-secondary');

  nifButtons.forEach(btn => {
    if (btn.textContent.includes('Adicionar NIF')) {
      btn.addEventListener('click', function() {
        // In real app: Open modal form, submit to backend, trigger workflow event
        alert('Prototype: Formulário para adicionar NIF aberto.\n\nEm produção, isto desbloquearia o passo "Declaração de Herdeiros".');
      });
    }
  });
}

/**
 * Simulate contacting agency for help
 */
function simulateContactAgency() {
  const contactButtons = document.querySelectorAll('button');

  contactButtons.forEach(btn => {
    if (btn.textContent.includes('Contactar Agência')) {
      btn.addEventListener('click', function() {
        // In real app: Open chat widget, create support ticket, or initiate call
        alert('Prototype: Chat com agência funerária iniciado.\n\nEm produção, isto abriria um widget de chat ou agendaria uma chamada.');
      });
    }
  });
}

// ===================================
// KEYBOARD NAVIGATION ENHANCEMENTS
// WCAG 2.1 AA Compliance
// ===================================

/**
 * Add keyboard navigation for card focus
 * Future: Could enable keyboard shortcuts for common actions
 */
function enhanceKeyboardNavigation() {
  // Allow cards to be focusable with keyboard for screen reader users
  const cards = document.querySelectorAll('.card');
  cards.forEach(card => {
    // Cards are already navigable via tab through buttons/links inside
    // This is just a placeholder for future keyboard shortcut features
  });
}

// ===================================
// INITIALIZE ALL INTERACTIONS
// ===================================

document.addEventListener('DOMContentLoaded', function() {
  // Core features
  initGlossaryToggles();
  initStepsListToggle();

  // Prototype simulations
  simulateDocumentUpload();
  simulateAddNIF();
  simulateContactAgency();

  // Accessibility enhancements
  enhanceKeyboardNavigation();

  console.log('✅ Family Dashboard prototype initialized');
  console.log('Epic 9.1: Plain-language glossary - ACTIVE');
  console.log('Epic 9.2: Reason-coded blocked recovery - ACTIVE');
});

// ===================================
// GRIEF-AWARE UX TELEMETRY (Future)
// Track signals of user overwhelm to adjust UI density
// ===================================

/**
 * Future: Track behavioral signals that might indicate cognitive overload
 * Examples:
 * - Repeated clicks without action
 * - Long hover durations without clicking
 * - Rapid navigation between sections
 * - Time spent on glossary definitions (high engagement = good)
 *
 * When overwhelm detected, offer to activate reduced density mode
 */
function trackGriefAwareSignals() {
  // Placeholder for future implementation
  // Would connect to Epic 11 (Trust Telemetry) backend
}
