import { AiChatLauncher } from "@/components/ai-chat-panel";
import { NotificationsBell } from "@/components/notifications-bell";
import { StatusPill } from "@/components/status-pill";
import Link from "next/link";

const nav = [
  { href: "/", label: "Overview", emoji: "🏠", disabled: false },
  { href: "/support", label: "Customer Support", emoji: "💬", disabled: false },
  { href: "/inventory", label: "Inventory", emoji: "📦", disabled: false },
  { href: "/shopping", label: "Orders", emoji: "🛒", disabled: false },
  { href: "/analytics", label: "Analytics", emoji: "📊", disabled: true },
  { href: "/settings", label: "Settings", emoji: "⚙", disabled: true },
];

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="flex min-h-screen">
      <aside className="fixed inset-y-0 w-56 border-r border-slate-800 bg-slate-950 text-slate-300">
        <div className="flex items-center gap-2 px-5 py-5">
          <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-emerald-500 text-sm font-bold text-slate-950">
            S
          </span>
          <span className="text-sm font-semibold tracking-wide text-white">
            StorePilot
          </span>
        </div>
        <nav className="mt-2 space-y-0.5 px-3">
          {nav.map(({ href, label, emoji, disabled }) =>
            disabled ? (
              <span
                key={href}
                aria-disabled="true"
                title="Coming soon"
                className="flex cursor-not-allowed items-center gap-3 rounded-lg px-3 py-2 text-sm text-slate-600"
              >
                <span className="w-4 text-center">{emoji}</span>
                {label}
              </span>
            ) : (
              <Link
                key={href}
                href={href}
                className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm hover:bg-slate-900 hover:text-white"
              >
                <span className="w-4 text-center">{emoji}</span>
                {label}
              </Link>
            ),
          )}
        </nav>
        <p className="absolute bottom-4 px-5 text-[11px] leading-relaxed text-slate-600">
          Local-first build
          <br />
          LocalDB · Ollama · llama3.1:8b
        </p>
      </aside>

      <div className="ml-56 flex-1">
        <header className="sticky top-0 z-40 flex items-center justify-end gap-3 border-b border-slate-200 bg-white/80 px-6 py-3 backdrop-blur">
          <AiChatLauncher />
          <StatusPill />
          <NotificationsBell />
        </header>
        <main className="p-6">{children}</main>
      </div>
    </div>
  );
}
