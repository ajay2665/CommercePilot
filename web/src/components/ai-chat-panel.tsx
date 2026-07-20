"use client";

import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { usePathname } from "next/navigation";
import { RotateCcw, Send, Sparkles, X } from "lucide-react";
import { useBrainChat } from "@/features/brain/hooks";
import { ChatMessage, type ChatEntry } from "@/components/chat-message";

const SUGGESTIONS = [
  "How is the business doing?",
  "Sales report for this month",
  "What should we reorder?",
  "Any escalated tickets?",
  "What's trending right now?",
];

/** Header button + slide-in AI command center panel. */
export function AiChatLauncher() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1.5 rounded-full bg-slate-900 px-3 py-1.5 text-xs font-medium text-white hover:bg-slate-700"
      >
        <Sparkles size={13} />
        Ask AI
      </button>
      <AiChatPanel open={open} onClose={() => setOpen(false)} />
    </>
  );
}

function AiChatPanel({ open, onClose }: { open: boolean; onClose: () => void }) {
  const pathname = usePathname();
  const chat = useBrainChat();
  const [entries, setEntries] = useState<ChatEntry[]>([]);
  const [conversationId, setConversationId] = useState<string | undefined>();
  const [input, setInput] = useState("");
  const scrollRef = useRef<HTMLDivElement>(null);
  const [mounted, setMounted] = useState(false);

  // Portal to <body>: the header uses backdrop-blur, and any ancestor with a
  // filter/backdrop-filter becomes the containing block for position:fixed
  // descendants — without the portal this panel resolves "fixed" against the
  // header's own thin box instead of the viewport.
  useEffect(() => setMounted(true), []);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [entries, chat.isPending]);

  function send(message: string) {
    const text = message.trim();
    if (!text || chat.isPending) return;
    setEntries((prev) => [...prev, { role: "user", content: text }]);
    setInput("");
    chat.mutate(
      { message: text, conversationId, currentPage: pathname },
      {
        onSuccess: (response) => {
          setConversationId(response.conversationId);
          setEntries((prev) => [...prev, { role: "assistant", content: response.reply, response }]);
        },
        onError: (error) => {
          setEntries((prev) => [
            ...prev,
            { role: "assistant", content: `Something went wrong talking to the API: ${error.message}` },
          ]);
        },
      },
    );
  }

  function reset() {
    setEntries([]);
    setConversationId(undefined);
    chat.reset();
  }

  if (!mounted) return null;

  return createPortal(
    <div
      className={`fixed inset-y-0 right-0 z-50 flex w-full max-w-md flex-col border-l border-slate-200 bg-slate-50 shadow-2xl transition-transform duration-300 ${
        open ? "translate-x-0" : "translate-x-full"
      }`}
      role="dialog"
      aria-label="AI command center"
    >
      <header className="flex items-center gap-2 border-b border-slate-200 bg-white px-4 py-3">
        <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-slate-900 text-white">
          <Sparkles size={14} />
        </span>
        <div className="min-w-0 flex-1">
          <p className="text-sm font-semibold text-slate-900">AI Command Center</p>
          <p className="truncate text-[11px] text-slate-400">
            Sales · Inventory · Support · Shopping
          </p>
        </div>
        {entries.length > 0 && (
          <button
            onClick={reset}
            title="New conversation"
            className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
          >
            <RotateCcw size={14} />
          </button>
        )}
        <button
          onClick={onClose}
          title="Close"
          className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
        >
          <X size={16} />
        </button>
      </header>

      <div ref={scrollRef} className="flex-1 space-y-3 overflow-y-auto px-4 py-4">
        {entries.length === 0 && (
          <div className="mt-6 space-y-3 text-center">
            <p className="text-sm text-slate-500">
              Ask anything about your business — I pull live numbers from every module.
            </p>
            <div className="flex flex-wrap justify-center gap-1.5">
              {SUGGESTIONS.map((sug) => (
                <button
                  key={sug}
                  onClick={() => send(sug)}
                  className="rounded-full border border-slate-200 bg-white px-3 py-1.5 text-xs text-slate-600 hover:border-slate-300 hover:text-slate-900"
                >
                  {sug}
                </button>
              ))}
            </div>
          </div>
        )}

        {entries.map((entry, i) => (
          <ChatMessage key={i} entry={entry} onNavigate={onClose} />
        ))}

        {chat.isPending && (
          <div className="flex items-center gap-1.5 px-1 text-slate-400">
            <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-slate-400 [animation-delay:0ms]" />
            <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-slate-400 [animation-delay:120ms]" />
            <span className="h-1.5 w-1.5 animate-bounce rounded-full bg-slate-400 [animation-delay:240ms]" />
            <span className="ml-1 text-xs">Gathering data…</span>
          </div>
        )}
      </div>

      <footer className="border-t border-slate-200 bg-white p-3">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            send(input);
          }}
          className="flex items-end gap-2"
        >
          <textarea
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                send(input);
              }
            }}
            rows={input.includes("\n") ? 3 : 1}
            placeholder="Ask about sales, stock, tickets…"
            className="max-h-28 flex-1 resize-none rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none placeholder:text-slate-300 focus:border-slate-400"
          />
          <button
            type="submit"
            disabled={!input.trim() || chat.isPending}
            className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-slate-900 text-white disabled:opacity-30"
            title="Send"
          >
            <Send size={14} />
          </button>
        </form>
      </footer>
    </div>,
    document.body,
  );
}
