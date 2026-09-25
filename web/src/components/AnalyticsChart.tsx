import { useEffect, useRef, useState, type CSSProperties } from 'react';

type Count = { key: string; count: number };
const colors = ['#087f8c', '#6366f1', '#e3a008', '#db5375', '#20a778', '#9163cb', '#4176ad'];

export function AnalyticsChart({ title, values, initialView = 'bars' }: {
  title: string; values: Count[]; initialView?: 'bars' | 'ring';
}) {
  const [view, setView] = useState(initialView);
  const total = values.reduce((sum, item) => sum + item.count, 0);
  const max = Math.max(1, ...values.map((item) => item.count));
  let offset = 0;
  return <section className="analytics-chart" aria-label={title}>
    <div className="chart-heading"><h3>{title}</h3>
      <div className="chart-switch" role="group" aria-label={`${title} chart type`}>
        <button type="button" aria-pressed={view === 'bars'} onClick={() => setView('bars')}>Bars</button>
        <button type="button" aria-pressed={view === 'ring'} onClick={() => setView('ring')}>Ring</button>
      </div>
    </div>
    {total === 0 ? <p className="empty-state">No data yet.</p> : <>
      {view === 'ring' && <div className="chart-ring">
        <svg viewBox="0 0 120 120" role="img" aria-label={`${title}: ${total} total`}>
          {values.map((item, index) => {
            const segment = item.count / total * 100;
            const start = offset; offset += segment;
            return <circle key={item.key} cx="60" cy="60" r="46" pathLength="100" fill="none"
              stroke={colors[index % colors.length]} strokeWidth="13"
              strokeDasharray={`${segment} ${100 - segment}`} strokeDashoffset={-start}
              transform="rotate(-90 60 60)"><title>{item.key}: {item.count}</title></circle>;
          })}
          <text x="60" y="59" textAnchor="middle" className="ring-total">{total}</text>
          <text x="60" y="73" textAnchor="middle" className="ring-caption">TOTAL</text>
        </svg>
      </div>}
      <ul className="chart-legend" aria-label={`${title} values`}>
        {values.map((item, index) => <li key={item.key} style={{ '--chart-color': colors[index % colors.length] } as CSSProperties}>
          <div className="chart-label"><span><i aria-hidden="true" />{item.key.replaceAll('_', ' ')}</span>
            <strong>{item.count} <small>({Math.round(item.count / total * 100)}%)</small></strong></div>
          {view === 'bars' && <div className="chart-track" aria-hidden="true"><div className="chart-fill" style={{ width: `${item.count / max * 100}%` }} /></div>}
        </li>)}
      </ul>
    </>}
  </section>;
}

export function AnalyticsMetric({ label, value, detail, tone = 'progress' }: { label: string; value: string | number; detail?: string; tone?: 'success' | 'pending' | 'failed' | 'progress' }) {
  const numeric = typeof value === 'number' ? value : NaN;
  const previous = useRef(0);
  const [display, setDisplay] = useState<string | number>(Number.isFinite(numeric) ? 0 : value);
  useEffect(() => {
    if (!Number.isFinite(numeric)) { setDisplay(value); return; }
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) { setDisplay(numeric); previous.current = numeric; return; }
    const startValue = previous.current;
    const started = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const progress = Math.min(1, (now - started) / 650);
      const next = startValue + (numeric - startValue) * (1 - Math.pow(1 - progress, 3));
      previous.current = next;
      setDisplay(progress === 1 ? numeric : Math.round(next));
      if (progress < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [numeric, value]);
  return <div className="analytics-metric" data-tone={tone}><span>{label}</span><strong aria-label={String(value)}><span aria-hidden="true">{typeof display === 'number' ? new Intl.NumberFormat().format(display) : display}</span><span className="sr-only">{value}</span></strong>{detail && <small>{detail}</small>}</div>;
}
