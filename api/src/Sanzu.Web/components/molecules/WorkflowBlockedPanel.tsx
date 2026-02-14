'use client';

export interface WorkflowBlockedPanelProps {
  reasonLabel: string;
  reasonDetail: string;
  allowedActions: Array<{
    action: string;
    label: string;
    guidance: string;
    isAvailable: boolean;
  }>;
}

export function WorkflowBlockedPanel({
  reasonLabel,
  reasonDetail,
  allowedActions,
}: WorkflowBlockedPanelProps) {
  return (
    <div
      role="alert"
      aria-labelledby="blocked-reason-title"
      className="rounded-lg border-2 border-orange-300 bg-orange-50 p-4"
    >
      {/* Blocked Reason */}
      <div className="mb-4">
        <div className="flex items-center gap-2 mb-2">
          <svg
            className="h-5 w-5 text-orange-600"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
            />
          </svg>
          <h3
            id="blocked-reason-title"
            className="text-lg font-semibold text-orange-900"
          >
            {reasonLabel}
          </h3>
        </div>
        <p className="text-sm text-orange-800">{reasonDetail}</p>
      </div>

      {/* Recovery Actions */}
      {allowedActions.length > 0 && (
        <div>
          <h4 className="text-sm font-medium text-orange-900 mb-2">
            What you can do:
          </h4>
          <div className="space-y-2">
            {allowedActions.map((action, index) => (
              <div
                key={index}
                className={`rounded border p-3 ${
                  action.isAvailable
                    ? 'border-orange-200 bg-white'
                    : 'border-gray-200 bg-gray-50 opacity-60'
                }`}
              >
                <div className="flex items-start gap-2">
                  <svg
                    className={`h-5 w-5 mt-0.5 flex-shrink-0 ${
                      action.isAvailable ? 'text-orange-600' : 'text-gray-400'
                    }`}
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                  <div className="flex-1">
                    <p
                      className={`text-sm font-medium ${
                        action.isAvailable
                          ? 'text-gray-900'
                          : 'text-gray-600'
                      }`}
                    >
                      {action.label}
                    </p>
                    <p
                      className={`text-xs mt-1 ${
                        action.isAvailable
                          ? 'text-gray-600'
                          : 'text-gray-500'
                      }`}
                    >
                      {action.guidance}
                    </p>
                    {!action.isAvailable && (
                      <p className="text-xs text-gray-500 mt-1 italic">
                        Not available to your role
                      </p>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
