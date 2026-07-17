"use client";

import { useState } from "react";
import Link from "next/link";
import { ArrowLeft, MessageCircleQuestion, Send } from "lucide-react";
import { useAssistant } from "@/features/shopping/hooks";
import type { AssistantAnswer } from "@/lib/api";

const SUGGESTIONS = [
  "What is the return policy for Aurora Audio?",
  "Which sunscreen do you sell, and is it in stock?",
  "How long does shipping take for Nordic Home orders?",
  "I need a gift for someone who loves vinyl records",
];

export default function AssistantPage() {
  const [question, setQuestion] = useState("");
  const [thread, setThread] = useState<{ q: string; a: AssistantAnswer }[]>([]);
  const assistant = useAssistant();

  const ask = (q: string) => {
    if (!q.trim() || assistant.isPending) return;
    setQuestion("");
    assistant
      .mutateAsync(q)
      .then((res) => setThread((t) => [...t, { q, a: res }]))
      .catch(() =>
        setThread((t) => [...t, { q, a: { answer: "Request failed — is the API running?", sources: [] } }]),
      );
  };

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <Link href="/shopping" className="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-slate-800">
        <ArrowLeft size={14} /> Shopping AI
      </Link>

      <div>
        <h1 className="flex items-center gap-2 text-xl font-semibold">
          <MessageCircleQuestion size={18} className="text-emerald-600" /> AI Shopping Assistant
        </h1>
        <p className="text-sm text-slate-500">
          RAG over the catalog, policies and FAQs — answers cite their sources and stay inside retrieved context.
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        {SUGGESTIONS.map((s) => (
          <button
            key={s}
            onClick={() => ask(s)}
            disabled={assistant.isPending}
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
            <div className="w-fit max-w-[85%] rounded-2xl rounded-bl-sm border border-slate-200 bg-white px-4 py-2.5 text-sm">
              <p className="whitespace-pre-wrap leading-relaxed text-slate-700">{entry.a.answer}</p>
              {entry.a.sources.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1.5 border-t border-slate-100 pt-2">
                  {entry.a.sources.map((s) => (
                    <span key={s.title} className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] text-slate-500">
                      {s.kind}: {s.title}
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>
        ))}
        {assistant.isPending && (
          <div className="w-fit rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm text-slate-400">
            Retrieving & answering… <span className="animate-pulse">▍</span>
          </div>
        )}
        {thread.length === 0 && !assistant.isPending && (
          <p className="rounded-2xl border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-400">
            Ask about products, policies, shipping or gift ideas.
          </p>
        )}
      </div>

      <div className="flex gap-2">
        <input
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && ask(question)}
          placeholder="Ask the assistant…"
          className="flex-1 rounded-lg border border-slate-200 bg-white px-3 py-2.5 text-sm outline-none focus:border-slate-400"
        />
        <button
          onClick={() => ask(question)}
          disabled={!question.trim() || assistant.isPending}
          className="rounded-lg bg-slate-900 px-4 py-2.5 text-white hover:bg-slate-700 disabled:opacity-40"
        >
          <Send size={16} />
        </button>
      </div>
    </div>
  );
}
