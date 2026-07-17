"use client";

import { useState } from "react";
import { X } from "lucide-react";
import { useSubmitTicket } from "@/features/support/hooks";

const SAMPLES = [
  {
    subject: "Charged twice for my order",
    sender: "sofia.martins@example.com",
    body: "I ordered the Vitamin C Serum from Luma Beauty last Tuesday and my card statement shows two identical charges. Please refund the duplicate as soon as possible.",
  },
  {
    subject: "Tent arrived with a broken pole",
    sender: "jonas.weber@example.com",
    body: "My Peak Outdoors 2-Person Tent Ridge arrived today but one of the poles is snapped in half. I leave for a hiking trip on Friday — can you send a replacement pole or a new tent before then?",
  },
  {
    subject: "Where is my order?",
    sender: "amelie.laurent@example.com",
    body: "I ordered an Oak Coffee Table from Nordic Home 12 days ago and the tracking hasn't updated since last Monday. Nobody replies to my emails. This is really frustrating.",
  },
];

export function SubmitTicketDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [subject, setSubject] = useState("");
  const [sender, setSender] = useState("");
  const [body, setBody] = useState("");
  const submit = useSubmitTicket();

  if (!open) return null;

  const fill = (i: number) => {
    setSubject(SAMPLES[i].subject);
    setSender(SAMPLES[i].sender);
    setBody(SAMPLES[i].body);
  };

  const canSubmit = subject.trim() && sender.trim() && body.trim() && !submit.isPending;

  const handleSubmit = async () => {
    await submit.mutateAsync({ subject, body, sender });
    setSubject("");
    setSender("");
    setBody("");
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold">Submit ticket</h2>
          <button onClick={onClose} className="rounded-md p-1 text-slate-400 hover:bg-slate-100">
            <X size={18} />
          </button>
        </div>

        <div className="mb-3 flex flex-wrap gap-2">
          {SAMPLES.map((s, i) => (
            <button
              key={i}
              onClick={() => fill(i)}
              className="rounded-full border border-slate-200 px-2.5 py-1 text-xs text-slate-500 hover:bg-slate-50"
            >
              Sample {i + 1}
            </button>
          ))}
        </div>

        <div className="space-y-3">
          <input
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            placeholder="Subject"
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm outline-none focus:border-slate-400"
          />
          <input
            value={sender}
            onChange={(e) => setSender(e.target.value)}
            placeholder="Customer email"
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm outline-none focus:border-slate-400"
          />
          <textarea
            value={body}
            onChange={(e) => setBody(e.target.value)}
            placeholder="Message body"
            rows={6}
            className="w-full resize-none rounded-lg border border-slate-200 px-3 py-2 text-sm outline-none focus:border-slate-400"
          />
        </div>

        {submit.isError && (
          <p className="mt-2 text-sm text-red-600">Submit failed — is the API running on :5080?</p>
        )}

        <div className="mt-4 flex justify-end gap-2">
          <button
            onClick={onClose}
            className="rounded-lg px-4 py-2 text-sm text-slate-600 hover:bg-slate-100"
          >
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            disabled={!canSubmit}
            className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-40"
          >
            {submit.isPending ? "Submitting…" : "Submit ticket"}
          </button>
        </div>
      </div>
    </div>
  );
}
