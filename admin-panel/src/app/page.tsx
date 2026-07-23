import Link from 'next/link';
import { Hero } from '@/components/landing/Hero';
import { Pricing } from '@/components/landing/Pricing';

export default function LandingPage() {
  return (
    <div className="min-h-screen">
      <header className="border-b border-slate-200 bg-white/80 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <span className="text-xl font-bold text-primary-600">Widget Admin</span>
          <nav className="flex items-center gap-3">
            <Link
              href="/support/login"
              className="text-sm font-medium text-slate-600 hover:text-slate-900"
            >
              Support login
            </Link>
            <Link
              href="/login"
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Log in
            </Link>
            <Link
              href="/register"
              className="rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700"
            >
              Get started
            </Link>
          </nav>
        </div>
      </header>
      <main>
        <Hero />
        <Pricing />
      </main>
      <footer className="border-t border-slate-200 py-8 text-center text-sm text-slate-500">
        © {new Date().getFullYear()} Chat Widget Admin. Frontend demo only.
      </footer>
    </div>
  );
}
