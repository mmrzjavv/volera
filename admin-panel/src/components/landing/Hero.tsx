import Link from 'next/link';

export function Hero() {
  return (
    <section className="border-b border-slate-200 bg-slate-50 px-4 py-20 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-3xl text-center">
        <h1 className="text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
          Manage your chat widget in one place
        </h1>
        <p className="mt-4 text-lg text-slate-600">
          Add branches, support users, and embed your widget with a few clicks.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-4">
          <Link
            href="/register"
            className="rounded-lg bg-primary-600 px-6 py-3 text-base font-medium text-white hover:bg-primary-700"
          >
            Register your company
          </Link>
          <a
            href="#pricing"
            className="rounded-lg border border-slate-300 bg-white px-6 py-3 text-base font-medium text-slate-700 hover:bg-slate-50"
          >
            View pricing
          </a>
        </div>
      </div>
    </section>
  );
}
