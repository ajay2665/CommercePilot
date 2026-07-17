"use client";

import { useState } from "react";
import Link from "next/link";
import { ArrowLeft, Send, Sparkles } from "lucide-react";
import { useCopilot } from "@/features/inventory/hooks";

const SUGGESTIONS = [
  "What should I reorder today?",
  "Show dead stock and what it's worth",
  "What will run out in the next two weeks?",
  "How healthy is my inventory overall?",
];

export default function CopilotPage() {
  const [question, setQuestion] = useState("");
  const [thread, setThread] = useState<{ q: string; a: string }[]>([]);
  const copilot = useCopilot();

  const ask = (q: string) => {
    if (!q.trim() || copilot.isPending) return;
    setQuestion("");
    copilot
      .mutateAsync(q)
      .then((res) => setThread((t) => [...t, { q, a: res.answer }]))
      .catch(() => setThread((t) => [...t, { q, a: "Request failed — is the API running?" }]));
  };

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <Link href="/inventory" className="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-slate-800">
        <ArrowLeft size={14} /> Inventory health
      </Link>

      <div>
        <h1 className="flex items-center gap-2 text-xl font-semibold">
          <Sparkles size={18} className="text-emerald-600" /> Inventory Copilot
        </h1>
        <p className="text-sm text-slate-500">
          Answers come from the deterministic analysis snapshot — the local model only does the phrasing.
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        {SUGGESTIONS.map((s) => (
          <button
            key={s}
            onClick={() => ask(s)}
            disabled={copilot.isPending}
            className="rounded-full border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-600 hover:bg-slate-50 disabled:opacity-40"
          >
            {s}
          </button>
        ))}
      </div>

      <div className="space-y-3">
        {thread.map((entry, i) => (
          <div key={i} className="space-y-2">
            <div className="ml-auto w-fit max-w-[85%] rounded-2xl rounded-br-sm bg-slate-900 px-4 py-2.5 text-sm text-white">
              {entry.q}
            </div>
            <div className="w-fit max-w-[85%] whitespace-pre-wrap rounded-2xl rounded-bl-sm border border-slate-200 bg-white px-4 py-2.5 text-sm leading-relaxed text-slate-700">
              {entry.a}
            </div>
          </div>
        ))}
        {copilot.isPending && (
          <div className="w-fit rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm text-slate-400">
            Thinking on llama3.1:8b… <span className="animate-pulse">▍</span>
          </div>
        )}
        {thread.length === 0 && !copilot.isPending && (
          <p className="rounded-2xl border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-400">
            Ask about reorders, dead stock, stockouts or overall health.
          </p>
        )}
      </div>

      <div className="flex gap-2">
        <input
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && ask(question)}
          placeholder="Ask the copilot…"
          className="flex-1 rounded-lg border border-slate-200 bg-white px-3 py-2.5 text-sm outline-none focus:border-slate-400"
        />
        <button
          onClick={() => ask(question)}
          disabled={!question.trim() || copilot.isPending}
          className="rounded-lg bg-slate-900 px-4 py-2.5 text-white hover:bg-slate-700 disabled:opacity-40"
        >
          <Send size={16} />
        </button>
      </div>
    </div>
  );
}
