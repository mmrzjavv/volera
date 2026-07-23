'use client';

import { useState } from 'react';
import { MOCK_PLANS } from '@/data/plans';
import { ChoosePlanModal } from './ChoosePlanModal';

export function Pricing() {
  const [planModalOpen, setPlanModalOpen] = useState(false);
  const [selectedPlanId, setSelectedPlanId] = useState<string | null>(null);

  return (
    <section id="pricing" className="px-4 py-16 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-7xl">
        <h2 className="text-center text-3xl font-bold text-slate-900">
          Simple pricing
        </h2>
        <p className="mx-auto mt-2 max-w-xl text-center text-slate-600">
          Choose a plan that fits your company size.
        </p>
        <div className="mt-12 grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
          {MOCK_PLANS.map((plan) => (
            <div
              key={plan.id}
              className="flex flex-col rounded-xl border border-slate-200 bg-white p-6 shadow-sm"
            >
              <h3 className="text-lg font-semibold text-slate-900">{plan.name}</h3>
              <p className="mt-2 text-3xl font-bold text-slate-900">
                ${plan.priceMonthly}
                <span className="text-base font-normal text-slate-500">/mo</span>
              </p>
              <ul className="mt-4 flex-1 space-y-2 text-sm text-slate-600">
                {plan.features.map((f) => (
                  <li key={f} className="flex items-center gap-2">
                    <span className="text-primary-600">✓</span> {f}
                  </li>
                ))}
              </ul>
              <button
                type="button"
                onClick={() => {
                  setSelectedPlanId(plan.id);
                  setPlanModalOpen(true);
                }}
                className="mt-6 w-full rounded-lg border border-primary-600 bg-white py-2 text-sm font-medium text-primary-600 hover:bg-primary-50"
              >
                Choose plan
              </button>
            </div>
          ))}
        </div>
      </div>
      <ChoosePlanModal
        open={planModalOpen}
        onClose={() => {
          setPlanModalOpen(false);
          setSelectedPlanId(null);
        }}
        selectedPlanId={selectedPlanId}
      />
    </section>
  );
}
