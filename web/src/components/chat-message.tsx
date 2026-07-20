"use client";

import Link from "next/link";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { BrainChatResponse, ChatChart } from "@/lib/api";

const SERIES = "#2a78d6"; // validated primary series hue
// Validated 6-slot cycle for multi-segment (doughnut) charts.
const CYCLE = ["#008300", "#2a78d6", "#e87ba4", "#eda100", "#4a3aa7", "#e34948"];
const GRID = "#e2e8f0";
const AXIS = "#94a3b8";

export interface ChatEntry {
  role: "user" | "assistant";
  content: string;
  response?: BrainChatResponse;
}

export function ChatMessage({ entry, onNavigate }: { entry: ChatEntry; onNavigate?: () => void }) {
  if (entry.role === "user") {
    return (
      <div className="flex justify-end">
        <div className="max-w-[85%] rounded-2xl rounded-br-sm bg-slate-900 px-3.5 py-2 text-sm text-white">
          {entry.content}
        </div>
      </div>
    );
  }

  const response = entry.response;
  return (
    <div className="max-w-full space-y-2">
      <div className="rounded-2xl rounded-bl-sm border border-slate-200 bg-white px-3.5 py-2.5">
        <Markdown text={entry.content} />
        {response?.chart && response.chart.labels.length > 0 && (
          <div className="mt-2 rounded-xl border border-slate-100 bg-slate-50/50 p-2">
            <MiniChart chart={response.chart} />
          </div>
        )}
      </div>
      {response && (response.sources.length > 0 || response.actions.length > 0) && (
        <div className="flex flex-wrap items-center gap-1.5 px-1">
          {response.sources.slice(0, 4).map((s) => (
            <span
              key={s.title}
              className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] text-slate-500"
              title={s.kind}
            >
              {s.title}
            </span>
          ))}
          {response.actions.map((a) => (
            <Link
              key={a.route}
              href={a.route}
              onClick={onNavigate}
              className="rounded-full border border-slate-200 px-2 py-0.5 text-[10px] font-medium text-slate-600 hover:bg-slate-50"
            >
              {a.label} →
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

/** Markdown mapped onto the app's Tailwind text styles (no typography plugin). */
function Markdown({ text }: { text: string }) {
  return (
    <div className="space-y-2 text-sm leading-relaxed text-slate-700">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          h1: (p) => <p className="text-sm font-semibold text-slate-900">{p.children}</p>,
          h2: (p) => <p className="text-sm font-semibold text-slate-900">{p.children}</p>,
          h3: (p) => <p className="text-sm font-semibold text-slate-900">{p.children}</p>,
          p: (p) => <p>{p.children}</p>,
          strong: (p) => <strong className="font-semibold text-slate-900">{p.children}</strong>,
          ul: (p) => <ul className="list-disc space-y-0.5 pl-5">{p.children}</ul>,
          ol: (p) => <ol className="list-decimal space-y-0.5 pl-5">{p.children}</ol>,
          li: (p) => <li>{p.children}</li>,
          a: (p) => (
            <a href={p.href} className="text-blue-700 underline underline-offset-2">
              {p.children}
            </a>
          ),
          code: (p) => (
            <code className="rounded bg-slate-100 px-1 py-0.5 font-mono text-[12px] text-slate-700">
              {p.children}
            </code>
          ),
          pre: (p) => (
            <pre className="overflow-x-auto rounded-lg bg-slate-100 p-2.5 font-mono text-[12px] leading-snug">
              {p.children}
            </pre>
          ),
          table: (p) => (
            <div className="overflow-x-auto">
              <table className="w-full border-collapse text-xs">{p.children}</table>
            </div>
          ),
          th: (p) => (
            <th className="border-b border-slate-200 px-2 py-1 text-left font-semibold text-slate-600">
              {p.children}
            </th>
          ),
          td: (p) => <td className="border-b border-slate-100 px-2 py-1">{p.children}</td>,
        }}
      >
        {text}
      </ReactMarkdown>
    </div>
  );
}

function MiniChart({ chart }: { chart: ChatChart }) {
  const dataset = chart.datasets[0];
  if (!dataset) return null;
  const data = chart.labels.map((label, i) => ({ label, value: dataset.data[i] ?? 0 }));
  const interval = Math.max(0, Math.ceil(data.length / 6) - 1);

  if (chart.type === "doughnut") {
    return (
      <div className="flex items-center gap-3">
        <ResponsiveContainer width={110} height={110}>
          <PieChart>
            <Pie
              data={data.filter((d) => d.value > 0)}
              dataKey="value"
              nameKey="label"
              innerRadius="62%"
              outerRadius="100%"
              paddingAngle={2}
              stroke="#ffffff"
              strokeWidth={2}
            >
              {data
                .filter((d) => d.value > 0)
                .map((d, i) => (
                  <Cell key={d.label} fill={CYCLE[i % CYCLE.length]} />
                ))}
            </Pie>
            <Tooltip content={<MiniTooltip unit={dataset.label} />} />
          </PieChart>
        </ResponsiveContainer>
        <ul className="flex-1 space-y-1 text-[11px]">
          {data.map((d, i) => (
            <li key={d.label} className="flex items-center gap-1.5">
              <span className="h-2 w-2 shrink-0 rounded-full" style={{ background: CYCLE[i % CYCLE.length] }} />
              <span className="truncate text-slate-500">{d.label}</span>
              <span className="ml-auto tabular-nums text-slate-700">{d.value}</span>
            </li>
          ))}
        </ul>
      </div>
    );
  }

  if (chart.type === "bar") {
    return (
      <ResponsiveContainer width="100%" height={130}>
        <BarChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: -18 }} barCategoryGap="28%">
          <CartesianGrid vertical={false} stroke={GRID} strokeWidth={1} />
          <XAxis dataKey="label" tick={{ fontSize: 9, fill: AXIS }} tickLine={false} axisLine={false} interval={interval} />
          <YAxis tick={{ fontSize: 9, fill: AXIS }} tickLine={false} axisLine={false} />
          <Tooltip content={<MiniTooltip unit={dataset.label} />} cursor={{ fill: "#64748b", opacity: 0.06 }} />
          <Bar dataKey="value" fill={SERIES} radius={[4, 4, 0, 0]} maxBarSize={18} />
        </BarChart>
      </ResponsiveContainer>
    );
  }

  return (
    <ResponsiveContainer width="100%" height={130}>
      <LineChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: -18 }}>
        <CartesianGrid vertical={false} stroke={GRID} strokeWidth={1} />
        <XAxis dataKey="label" tick={{ fontSize: 9, fill: AXIS }} tickLine={false} axisLine={false} interval={interval} />
        <YAxis tick={{ fontSize: 9, fill: AXIS }} tickLine={false} axisLine={false} />
        <Tooltip content={<MiniTooltip unit={dataset.label} />} cursor={{ stroke: "#64748b", strokeWidth: 1, opacity: 0.35 }} />
        <Line type="monotone" dataKey="value" stroke={SERIES} strokeWidth={2} dot={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}

interface MiniPayload {
  payload?: { label?: string; name?: string; value: number };
}

function MiniTooltip({ active, payload, unit }: { active?: boolean; payload?: MiniPayload[]; unit: string }) {
  const row = payload?.[0]?.payload;
  if (!active || !row) return null;
  return (
    <div className="rounded-lg border border-slate-200 bg-white px-2 py-1 text-[11px] shadow-sm">
      <span className="font-medium text-slate-800">{row.label ?? row.name}</span>
      <span className="ml-1.5 text-slate-500">
        {row.value} {unit}
      </span>
    </div>
  );
}
