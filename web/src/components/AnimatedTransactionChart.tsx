import React, { useState } from 'react';
import type { TransactionTimeSeriesPoint } from '../features/transactions/transactionsApi';
import { formatLkr } from '../utils/currency';

interface AnimatedTransactionChartProps {
  points: TransactionTimeSeriesPoint[];
  height?: number;
}

export function AnimatedTransactionChart({
  points,
  height = 240,
}: AnimatedTransactionChartProps) {
  const [hoveredIndex, setHoveredIndex] = useState<number | null>(null);

  if (points.length === 0) {
    return (
      <div
        style={{
          height,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: '#64748b',
          background: '#f8fafc',
          borderRadius: '12px',
          border: '1px solid #e2e8f0',
        }}
      >
        No transaction data available for the selected period.
      </div>
    );
  }

  const padding = { top: 20, right: 25, bottom: 35, left: 25 };
  const width = 760;
  const innerWidth = width - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;

  const maxValue = Math.max(...points.map((p) => p.totalValue), 100);

  const getX = (index: number) => {
    if (points.length === 1) return padding.left + innerWidth / 2;
    return padding.left + (index / (points.length - 1)) * innerWidth;
  };

  const getY = (val: number) => {
    return padding.top + innerHeight - (val / maxValue) * innerHeight;
  };

  // Generate smooth SVG path
  const linePoints = points.map((p, i) => `${getX(i)},${getY(p.totalValue)}`);
  const pathD = `M ${linePoints.join(' L ')}`;
  const areaD = `M ${getX(0)},${padding.top + innerHeight} L ${linePoints.join(' L ')} L ${getX(points.length - 1)},${padding.top + innerHeight} Z`;

  const hoveredPoint = hoveredIndex !== null ? points[hoveredIndex] : null;

  return (
    <div style={{ position: 'relative', width: '100%', overflow: 'hidden' }}>
      <svg
        viewBox={`0 0 ${width} ${height}`}
        style={{ width: '100%', height: 'auto', display: 'block' }}
        role="img"
        aria-label="Completed transactions analytics graph"
      >
        <defs>
          <linearGradient id="tx-area-gradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#f59e0b" stopOpacity="0.38" />
            <stop offset="100%" stopColor="#f59e0b" stopOpacity="0.02" />
          </linearGradient>
          <linearGradient id="tx-line-gradient" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor="#d97706" />
            <stop offset="100%" stopColor="#f59e0b" />
          </linearGradient>
        </defs>

        {/* Gridlines */}
        {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
          const y = padding.top + innerHeight * ratio;
          return (
            <line
              key={ratio}
              x1={padding.left}
              y1={y}
              x2={width - padding.right}
              y2={y}
              stroke="#e2e8f0"
              strokeDasharray="4 4"
              strokeWidth="1"
            />
          );
        })}

        {/* Area fill */}
        <path d={areaD} fill="url(#tx-area-gradient)" />

        {/* Line stroke */}
        <path
          d={pathD}
          fill="none"
          stroke="url(#tx-line-gradient)"
          strokeWidth="3.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        />

        {/* Data points */}
        {points.map((p, i) => {
          const cx = getX(i);
          const cy = getY(p.totalValue);
          const isHovered = hoveredIndex === i;

          return (
            <g key={p.period}>
              <circle
                cx={cx}
                cy={cy}
                r={isHovered ? 6 : p.totalValue > 0 ? 4 : 2}
                fill={p.totalValue > 0 ? '#ffffff' : '#cbd5e1'}
                stroke={p.totalValue > 0 ? '#d97706' : '#94a3b8'}
                strokeWidth={isHovered ? 3 : 2}
                style={{ transition: 'all 0.15s ease', cursor: 'pointer' }}
                onMouseEnter={() => setHoveredIndex(i)}
                onMouseLeave={() => setHoveredIndex(null)}
              />

              {/* Hover touch target */}
              <rect
                x={cx - 15}
                y={padding.top}
                width={30}
                height={innerHeight}
                fill="transparent"
                style={{ cursor: 'pointer' }}
                onMouseEnter={() => setHoveredIndex(i)}
                onMouseLeave={() => setHoveredIndex(null)}
              />

              {/* X Axis Label */}
              {(points.length <= 12 || i % Math.ceil(points.length / 10) === 0) && (
                <text
                  x={cx}
                  y={height - 10}
                  textAnchor="middle"
                  fontSize="11"
                  fill="#64748b"
                  fontWeight="600"
                >
                  {p.label}
                </text>
              )}
            </g>
          );
        })}
      </svg>

      {/* Floating tooltip */}
      {hoveredPoint && hoveredIndex !== null && (
        <div
          style={{
            position: 'absolute',
            top: '0.5rem',
            left: `${Math.min(85, Math.max(15, (hoveredIndex / (points.length - 1)) * 100))}%`,
            transform: 'translateX(-50%)',
            background: '#0f172a',
            color: '#ffffff',
            padding: '0.45rem 0.75rem',
            borderRadius: '8px',
            fontSize: '0.775rem',
            boxShadow: '0 4px 12px rgba(0,0,0,0.2)',
            pointerEvents: 'none',
            zIndex: 10,
            whiteSpace: 'nowrap',
          }}
        >
          <div style={{ fontWeight: 700, color: '#f59e0b', marginBottom: '0.15rem' }}>
            {hoveredPoint.label}
          </div>
          <div>Value: {formatLkr(hoveredPoint.totalValue)}</div>
          <div style={{ color: '#94a3b8' }}>
            Completed: {hoveredPoint.transactionCount} {hoveredPoint.transactionCount === 1 ? 'deal' : 'deals'}
          </div>
        </div>
      )}
    </div>
  );
}
