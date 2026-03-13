// Rules Engine - Sanzu V1
// Generates case plan from questionnaire responses

import { StepLibrary } from './step-library';

interface QuestionnaireResponse {
  case_id: string;
  has_banks?: boolean;
  has_insurance?: boolean;
  has_benefits?: boolean;
  has_employer?: boolean;
  has_services?: boolean;
}

interface WorkflowStepInstance {
  case_id: string;
  step_key: string;
  title: string;
  owner_type: 'Agency' | 'Family';
  status: 'NotStarted' | 'Blocked' | 'Ready';
  criticality: 'mandatory' | 'optional';
  prerequisites: string[];
  required_docs: string[];
}

export class RulesEngine {
  
  /**
   * Generate workflow plan from questionnaire responses
   */
  static generatePlan(response: QuestionnaireResponse): WorkflowStepInstance[] {
    const steps: WorkflowStepInstance[] = [];
    
    // MANDATORY: Core registration (always included)
    steps.push(
      this.createStep('A1_core_medical_death_confirmation', response.case_id, 'mandatory', []),
      this.createStep('A2_core_death_registration_proof', response.case_id, 'mandatory', ['A1_core_medical_death_confirmation']),
      this.createStep('A3_core_certified_copies', response.case_id, 'optional', ['A2_core_death_registration_proof'])
    );
    
    // MANDATORY: Family onboarding (always included)
    steps.push(
      this.createStep('B1_family_invite_and_roles', response.case_id, 'mandatory', []),
      this.createStep('B2_family_intake_questionnaire', response.case_id, 'mandatory', ['B1_family_invite_and_roles']),
      this.createStep('B3_family_upload_baseline_docs', response.case_id, 'mandatory', ['B2_family_intake_questionnaire'])
    );
    
    // CONDITIONAL: Banks (triggered by has_banks)
    if (response.has_banks === true || response.has_banks === undefined) {
      steps.push(
        this.createStep('C1_bank_identify_accounts', response.case_id, 'optional', ['B2_family_intake_questionnaire']),
        this.createStep('C2_bank_prepare_notification_package', response.case_id, 'optional', [
          'A2_core_death_registration_proof',
          'C1_bank_identify_accounts',
          'B3_family_upload_baseline_docs'
        ]),
        this.createStep('C3_bank_track_responses', response.case_id, 'optional', ['C2_bank_prepare_notification_package'])
      );
    }
    
    // CONDITIONAL: Insurance (triggered by has_insurance)
    if (response.has_insurance === true || response.has_insurance === undefined) {
      steps.push(
        this.createStep('D1_insurance_identify_policies', response.case_id, 'optional', ['B2_family_intake_questionnaire']),
        this.createStep('D2_insurance_prepare_claim_package', response.case_id, 'optional', [
          'A2_core_death_registration_proof',
          'D1_insurance_identify_policies',
          'B3_family_upload_baseline_docs'
        ]),
        this.createStep('D3_insurance_track_claim_status', response.case_id, 'optional', ['D2_insurance_prepare_claim_package'])
      );
    }
    
    // CONDITIONAL: Benefits/pension (triggered by has_benefits)
    if (response.has_benefits === true || response.has_benefits === undefined) {
      steps.push(
        this.createStep('E1_benefits_assess_and_select', response.case_id, 'optional', ['B2_family_intake_questionnaire']),
        this.createStep('E2_benefits_notify_entities', response.case_id, 'optional', [
          'A2_core_death_registration_proof',
          'B3_family_upload_baseline_docs'
        ]),
        this.createStep('E3_benefits_collect_claim_docs', response.case_id, 'optional', ['E2_benefits_notify_entities'])
      );
    }
    
    // CONDITIONAL: Employer (triggered by has_employer)
    if (response.has_employer === true) {
      steps.push(
        this.createStep('F1_employment_notify_employer', response.case_id, 'optional', ['A2_core_death_registration_proof'])
      );
    }
    
    // CONDITIONAL: Utilities/services (triggered by has_services)
    if (response.has_services === true || response.has_services === undefined) {
      steps.push(
        this.createStep('G1_services_identify_recurring', response.case_id, 'optional', ['B2_family_intake_questionnaire']),
        this.createStep('G2_services_generate_requests', response.case_id, 'optional', [
          'G1_services_identify_recurring',
          'A2_core_death_registration_proof'
        ])
      );
    }
    
    // MANDATORY: Closure (always included)
    steps.push(
      this.createStep('H1_closure_review_and_archive', response.case_id, 'mandatory', [
        'A2_core_death_registration_proof',
        'B3_family_upload_baseline_docs'
      ])
    );
    
    // Set initial status based on prerequisites
    return steps.map(step => ({
      ...step,
      status: step.prerequisites.length === 0 ? 'Ready' : 'Blocked'
    }));
  }
  
  /**
   * Create step instance from library
   */
  private static createStep(
    step_key: string,
    case_id: string,
    criticality: 'mandatory' | 'optional',
    prerequisites: string[]
  ): WorkflowStepInstance {
    const stepDef = StepLibrary[step_key];
    
    if (!stepDef) {
      throw new Error(`Step not found: ${step_key}`);
    }
    
    return {
      case_id,
      step_key,
      title: stepDef.title,
      owner_type: stepDef.owner_type,
      status: 'NotStarted',
      criticality,
      prerequisites,
      required_docs: stepDef.required_docs || []
    };
  }
  
  /**
   * Update step statuses based on completed prerequisites
   */
  static updateStatuses(
    steps: WorkflowStepInstance[],
    completedStepKeys: string[]
  ): WorkflowStepInstance[] {
    return steps.map(step => {
      if (step.status === 'Completed') {
        return step;
      }
      
      // Check if all prerequisites are completed
      const prereqsMet = step.prerequisites.every(prereq =>
        completedStepKeys.includes(prereq)
      );
      
      return {
        ...step,
        status: prereqsMet ? 'Ready' : 'Blocked'
      };
    });
  }
  
  /**
   * Get next best step (highest priority Ready step)
   */
  static getNextStep(steps: WorkflowStepInstance[]): WorkflowStepInstance | null {
    // Priority: mandatory + Ready + Family-owned
    const familyMandatory = steps.find(
      s => s.status === 'Ready' && s.criticality === 'mandatory' && s.owner_type === 'Family'
    );
    if (familyMandatory) return familyMandatory;
    
    // Next: mandatory + Ready + Agency-owned
    const agencyMandatory = steps.find(
      s => s.status === 'Ready' && s.criticality === 'mandatory' && s.owner_type === 'Agency'
    );
    if (agencyMandatory) return agencyMandatory;
    
    // Next: optional + Ready (any owner)
    const optional = steps.find(s => s.status === 'Ready' && s.criticality === 'optional');
    if (optional) return optional;
    
    return null;
  }
}

// Example usage:
/*
const response = {
  case_id: 'abc-123',
  has_banks: true,
  has_insurance: true,
  has_benefits: false,
  has_employer: true,
  has_services: true
};

const plan = RulesEngine.generatePlan(response);
console.log(`Generated ${plan.length} steps`);

const nextStep = RulesEngine.getNextStep(plan);
console.log(`Next step: ${nextStep?.title}`);
*/
