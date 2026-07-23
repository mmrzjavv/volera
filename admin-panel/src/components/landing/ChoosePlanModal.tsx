'use client';

import Link from 'next/link';
import { Modal } from '@/components/ui/Modal';
import { MOCK_PLANS } from '@/data/plans';

interface ChoosePlanModalProps {
  open: boolean;
  onClose: () => void;
  selectedPlanId: string | null;
}

export function ChoosePlanModal({ open, onClose, selectedPlanId }: ChoosePlanModalProps) {
  const plan = selectedPlanId ? MOCK_PLANS.find((p) => p.id === selectedPlanId) : null;

  return (
    <Modal open={open} onClose={onClose} title="Choose plan" size="md">
      {plan ? (
        <div>
          <p className="text-slate-600">
            You selected <strong>{plan.name}</strong> (${plan.priceMonthly}/mo).
          </p>
          <p className="mt-2 text-sm text-slate-500">
            Continue to registration to enter your company details.
          </p>
          <div className="mt-6 flex gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Cancel
            </button>
            <Link
              href={`/register?plan=${plan.id}`}
              onClick={onClose}
              className="rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700"
            >
              Continue to registration
            </Link>
          </div>
        </div>
      ) : (
        <p className="text-slate-500">Select a plan from the pricing cards.</p>
      )}
    </Modal>
  );
}
