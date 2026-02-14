'use client';

import { useState, useEffect } from 'react';
import { getGlossaryTerm, type GlossaryTermResponse } from '@/lib/api-client/generated/glossary';

export interface GlossaryPopoverProps {
  tenantId: string;
  termKey: string;
  children: React.ReactNode;
  locale?: string;
}

export function GlossaryPopover({
  tenantId,
  termKey,
  children,
  locale = 'pt-PT',
}: GlossaryPopoverProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [term, setTerm] = useState<GlossaryTermResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen && !term && !loading) {
      loadTerm();
    }
  }, [isOpen]);

  const loadTerm = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getGlossaryTerm(tenantId, termKey, locale);
      setTerm(data);
    } catch (err) {
      setError('Failed to load glossary term');
      console.error('Glossary fetch error:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="relative inline-block">
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        onKeyDown={(e) => {
          if (e.key === 'Escape') setIsOpen(false);
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            setIsOpen(!isOpen);
          }
        }}
        className="border-b border-dashed border-blue-600 text-blue-600 hover:border-blue-800 hover:text-blue-800 cursor-help focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
        aria-label={`Get definition for ${termKey}`}
        aria-expanded={isOpen}
      >
        {children}
      </button>

      {isOpen && (
        <>
          <div
            className="fixed inset-0 z-10"
            onClick={() => setIsOpen(false)}
            aria-hidden="true"
          />
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="glossary-term-title"
            className="absolute z-20 mt-2 w-80 rounded-lg border border-gray-200 bg-white p-4 shadow-lg"
          >
            {loading && (
              <div className="text-sm text-gray-600">Loading...</div>
            )}

            {error && (
              <div className="text-sm text-red-600">{error}</div>
            )}

            {term && (
              <div>
                <h3
                  id="glossary-term-title"
                  className="text-lg font-semibold text-gray-900 mb-2"
                >
                  {term.term}
                </h3>
                <p className="text-sm text-gray-700 mb-3">
                  {term.definition}
                </p>
                {term.whyThisMatters && (
                  <div className="border-t border-gray-200 pt-3">
                    <p className="text-xs font-medium text-gray-600 mb-1">
                      Why this matters:
                    </p>
                    <p className="text-sm text-gray-700">
                      {term.whyThisMatters}
                    </p>
                  </div>
                )}
                <button
                  onClick={() => setIsOpen(false)}
                  className="mt-3 text-xs text-blue-600 hover:text-blue-800 focus:outline-none focus:underline"
                >
                  Close (ESC)
                </button>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
}
