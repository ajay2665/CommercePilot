const toggles = [
  { name: "LLM provider", local: "Ollama · llama3.1:8b (local)", cloud: "OpenAI wired — set Provider=openai + ApiKey in appsettings.Development.json (see README) · Gemini stub" },
  { name: "Ticket intake", local: "Dashboard form", cloud: "Gmail API poller via Intake:Mode" },
  { name: "Notifications", local: "In-app + console", cloud: "Slack webhooks via Notifications:Mode" },
  { name: "Stock source", local: "Seeded demo data", cloud: "Shopify / WooCommerce sync" },
  { name: "Auth", local: "Open (empty Auth:ApiKey)", cloud: "Static bearer key now · JWT + RBAC in Phase 4" },
];

export default function SettingsPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-semibold">Settings</h1>
        <p className="text-sm text-slate-500">
          Runtime toggles live in <code className="rounded bg-slate-100 px-1">src/StorePilot.Api/appsettings.json</code> —
          local-first defaults, cloud services opt-in. A UI for these arrives with Phase 4 auth.
        </p>
      </div>
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs uppercase tracking-wide text-slate-400">
              <th className="px-5 py-3 font-medium">Capability</th>
              <th className="px-5 py-3 font-medium">Current (local)</th>
              <th className="px-5 py-3 font-medium">Cloud toggle</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-50">
            {toggles.map((t) => (
              <tr key={t.name}>
                <td className="px-5 py-3 font-medium text-slate-800">{t.name}</td>
                <td className="px-5 py-3 text-slate-600">{t.local}</td>
                <td className="px-5 py-3 text-slate-500">{t.cloud}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
