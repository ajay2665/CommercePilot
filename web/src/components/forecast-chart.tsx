"use client";

import { useMemo, useRef, useState } from "react";
import type { ForecastSeries } from "@/lib/api";

const W = 720;
const H = 240;
const PAD = { top: 16, right: 12, bottom: 24, left: 34 };
const LINE = "#2563eb"; // single series — one hue; text stays in ink tokens

/**
 * Demand line: 7-day rolling average of history (solid) + constant-rate
 * projection (dashed) with a confidence band. One measure, one axis.
 */
export function ForecastChart({ series }: { series: ForecastSeries }) {
  const wrapRef = useRef<HTMLDivElement>(null);
  const [hover, setHover] = useState<{ i: number; x: number; y: number } | null>(null);

  const model = useMemo(() => {
    const smooth = series.history.map((p, i) => {
      const from = Math.max(0, i - 6);
      const window = series.history.slice(from, i + 1);
      return { date: p.date, value: window.reduce((s, q) => s + q.units, 0) / window.length };
    });
    const projDays = series.horizonDays;
    const rate = series.dailyRate;
    const band = (1 - series.confidence) * rate;
    const points = [
      ...smooth.map((p) => ({ ...p, projected: false })),
      ...Array.from({ length: projDays }, (_, d) => ({
        date: series.forecast[d]?.date ?? "",
        value: rate,
        projected: true,
      })),
    ];
    const maxY = Math.max(1, ...points.map((p) => p.value), rate + band) * 1.25;
    const x = (i: number) => PAD.left + (i / Math.max(1, points.length - 1)) * (W - PAD.left - PAD.right);
    const y = (v: number) => H - PAD.bottom - (v / maxY) * (H - PAD.top - PAD.bottom);
    const todayIndex = smooth.length - 1;
    return { points, smooth, rate, band, maxY, x, y, todayIndex };
  }, [series]);

  const { points, smooth, rate, band, x, y, todayIndex, maxY } = model;

  const historyPath = smooth.map((p, i) => `${i === 0 ? "M" : "L"}${x(i).toFixed(1)},${y(p.value).toFixed(1)}`).join(" ");
  const areaPath = `${historyPath} L${x(todayIndex).toFixed(1)},${y(0)} L${x(0)},${y(0)} Z`;
  const projStart = x(todayIndex);
  const projEnd = x(points.length - 1);

  const gridValues = [0.25, 0.5, 0.75, 1].map((f) => maxY * f * 0.8);
  const tickEvery = Math.ceil(points.length / 6);

  function onMove(e: React.MouseEvent<SVGSVGElement>) {
    const rect = e.currentTarget.getBoundingClientRect();
    const px = ((e.clientX - rect.left) / rect.width) * W;
    const i = Math.round(((px - PAD.left) / (W - PAD.left - PAD.right)) * (points.length - 1));
    if (i < 0 || i >= points.length) return setHover(null);
    setHover({ i, x: x(i), y: y(points[i].value) });
  }

  return (
    <div ref={wrapRef} className="relative">
      <svg
        viewBox={`0 0 ${W} ${H}`}
        className="w-full"
        role="img"
        aria-label={`Demand history and ${series.horizonDays}-day projection for ${series.name}`}
        onMouseMove={onMove}
        onMouseLeave={() => setHover(null)}
      >
        {/* recessive grid */}
        {gridValues.map((v) => (
          <g key={v}>
            <line x1={PAD.left} x2={W - PAD.right} y1={y(v)} y2={y(v)} stroke="#e2e8f0" strokeWidth="1" />
            <text x={PAD.left - 6} y={y(v) + 3} textAnchor="end" className="fill-slate-400" fontSize="9">
              {v.toFixed(1)}
            </text>
          </g>
        ))}

        {/* projection region */}
        <rect x={projStart} y={PAD.top} width={projEnd - projStart} height={H - PAD.top - PAD.bottom} fill="#2563eb" opacity="0.04" />
        <line x1={projStart} x2={projStart} y1={PAD.top} y2={H - PAD.bottom} stroke="#94a3b8" strokeWidth="1" strokeDasharray="3 3" />
        <text x={projStart + 4} y={PAD.top + 8} className="fill-slate-400" fontSize="9">today</text>

        {/* confidence band on projection */}
        <rect
          x={projStart}
          y={y(rate + band)}
          width={projEnd - projStart}
          height={Math.max(2, y(Math.max(0, rate - band)) - y(rate + band))}
          fill={LINE}
          opacity="0.08"
        />

        {/* history area + line */}
        <path d={areaPath} fill={LINE} opacity="0.07" />
        <path d={historyPath} fill="none" stroke={LINE} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />

        {/* projected rate line */}
        <line x1={projStart} x2={projEnd} y1={y(rate)} y2={y(rate)} stroke={LINE} strokeWidth="2" strokeDasharray="5 4" strokeLinecap="round" />
        <text x={projEnd} y={y(rate) - 6} textAnchor="end" className="fill-slate-500" fontSize="9">
          projected ~{rate.toFixed(1)}/day
        </text>

        {/* x ticks */}
        {points.map((p, i) =>
          i % tickEvery === 0 ? (
            <text key={i} x={x(i)} y={H - 8} textAnchor="middle" className="fill-slate-400" fontSize="9">
              {p.date.slice(5)}
            </text>
          ) : null,
        )}

        {/* crosshair */}
        {hover && (
          <g>
            <line x1={hover.x} x2={hover.x} y1={PAD.top} y2={H - PAD.bottom} stroke="#64748b" strokeWidth="1" opacity="0.35" />
            <circle cx={hover.x} cy={hover.y} r="4" fill="#fff" stroke={LINE} strokeWidth="2" />
          </g>
        )}
      </svg>

      {hover && (
        <div
          className="pointer-events-none absolute z-10 -translate-x-1/2 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs shadow-sm"
          style={{ left: `${(hover.x / W) * 100}%`, top: 0 }}
        >
          <span className="font-medium text-slate-800">{points[hover.i].value.toFixed(1)} units/day</span>
          <span className="ml-1.5 text-slate-400">
            {points[hover.i].date}
            {points[hover.i].projected ? " · projected" : " · 7d avg"}
          </span>
        </div>
      )}
    </div>
  );
}
