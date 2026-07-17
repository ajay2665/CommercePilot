import Link from "next/link";
import {
  BarChart3,
  Boxes,
  LayoutDashboard,
  MessageSquareText,
  Settings,
  ShoppingCart,
} from "lucide-react";
import { StatusPill } from "@/components/status-pill";
import { NotificationsBell } from "@/components/notifications-bell";

const nav = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/support", label: "Support AI", icon: MessageSquareText },
  { href: "/inventory", label: "Inventory AI", icon: Boxes, phase: "Phase 2" },
  { href: "/shopping", label: "Shopping AI", icon: ShoppingCart, phase: "Phase 3" },
  { href: "/analytics", label: "Analytics", icon: BarChart3, phase: "Later" },
  { href: "/settings", label: "Settings", icon: Settings, phase: "Phase 4" },
];

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen">
      <aside className="fixed inset-y-0 w-56 border-r border-slate-800 bg-slate-950 text-slate-300">
        <div className="flex items-center gap-2 px-5 py-5">
          <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-emerald-500 text-sm font-bold text-slate-950">
            C
          </span>
          <span className="text-sm font-semibold tracking-wide text-white">CommercePilot</span>
        </div>
        <nav className="mt-2 space-y-0.5 px-3">
          {nav.map(({ href, label, icon: Icon, phase }) =>
            phase ? (
              <span
                key={href}
                className="flex cursor-not-allowed items-center gap-3 rounded-lg px-3 py-2 text-sm text-slate-600"
                title={`Coming in ${phase}`}
              >
                <Icon size={16} />
                {label}
                <span className="ml-auto text-[10px] uppercase tracking-wide">{phase}</span>
              </span>
            ) : (
              <Link
                key={href}
                href={href}
                className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm hover:bg-slate-900 hover:text-white"
              >
                <Icon size={16} />
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
          <StatusPill />
          <NotificationsBell />
        </header>
        <main className="p-6">{children}</main>
      </div>
    </div>
  );
}
