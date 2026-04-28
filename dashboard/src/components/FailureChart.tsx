import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts';
import { motion, AnimatePresence } from 'framer-motion';
import type { WindowMetrics } from '../types/metrics';

interface Props {
  metrics: WindowMetrics | null;
}

const LABELS: Record<string, string> = {
  INSUFFICIENT_FUNDS: 'Fondos insuf.',
  TIMEOUT:            'Timeout',
  FRAUD_DETECTED:     'Fraude detect.',
};

const COLORS: Record<string, string> = {
  INSUFFICIENT_FUNDS: '#f59e0b',
  TIMEOUT:            '#3b82f6',
  FRAUD_DETECTED:     '#ef4444',
};

const DEFAULT_COLOR = '#71717a';

interface CustomLabelProps {
  cx: number;
  cy: number;
  midAngle: number;
  innerRadius: number;
  outerRadius: number;
  percent: number;
}

function CustomLabel({ cx, cy, midAngle, innerRadius, outerRadius, percent }: CustomLabelProps) {
  if (percent < 0.05) return null;
  const RADIAN = Math.PI / 180;
  const radius = innerRadius + (outerRadius - innerRadius) * 0.5;
  const x = cx + radius * Math.cos(-midAngle * RADIAN);
  const y = cy + radius * Math.sin(-midAngle * RADIAN);
  return (
    <text x={x} y={y} fill="white" textAnchor="middle" dominantBaseline="central" fontSize={12} fontWeight={600}>
      {`${(percent * 100).toFixed(0)}%`}
    </text>
  );
}

export function FailureChart({ metrics }: Props) {
  const reasons = metrics?.FailureReasons ?? {};
  const total = Object.values(reasons).reduce((a, b) => a + b, 0);

  const data = Object.entries(reasons)
    .map(([key, value]) => ({ key, name: LABELS[key] ?? key, value }))
    .sort((a, b) => b.value - a.value);

  return (
    <div className="card">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm text-zinc-500 uppercase tracking-wider">
          Distribución de errores
        </h2>
        {total > 0 && (
          <span className="text-xs text-zinc-600">{total} fallos en ventana</span>
        )}
      </div>

      {total === 0 ? (
        <div className="h-48 flex flex-col items-center justify-center gap-2">
          <div className="w-10 h-10 rounded-full bg-emerald-500/10 flex items-center justify-center">
            <span className="text-emerald-400 text-lg">✓</span>
          </div>
          <p className="text-zinc-600 text-sm">Sin errores en esta ventana</p>
        </div>
      ) : (
        <div className="flex gap-4 items-center">
          <ResponsiveContainer width="55%" height={180}>
            <PieChart>
              <Pie
                data={data}
                cx="50%"
                cy="50%"
                innerRadius={45}
                outerRadius={78}
                paddingAngle={3}
                dataKey="value"
                labelLine={false}
                label={CustomLabel}
                isAnimationActive
              >
                {data.map(entry => (
                  <Cell key={entry.key} fill={COLORS[entry.key] ?? DEFAULT_COLOR} />
                ))}
              </Pie>
              <Tooltip
                contentStyle={{ backgroundColor: '#18181b', border: '1px solid #27272a', borderRadius: 8 }}
                itemStyle={{ color: '#fafafa' }}
                formatter={(value: number) => [value, 'fallos']}
              />
            </PieChart>
          </ResponsiveContainer>

          <div className="flex flex-col gap-2 flex-1">
            <AnimatePresence>
              {data.map(entry => (
                <motion.div
                  key={entry.key}
                  className="flex items-center justify-between"
                  initial={{ opacity: 0, x: 8 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0 }}
                  transition={{ duration: 0.3 }}
                >
                  <div className="flex items-center gap-2">
                    <div
                      className="w-2.5 h-2.5 rounded-full flex-shrink-0"
                      style={{ backgroundColor: COLORS[entry.key] ?? DEFAULT_COLOR }}
                    />
                    <span className="text-xs text-zinc-400">{entry.name}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="w-16 h-1.5 bg-zinc-800 rounded-full overflow-hidden">
                      <motion.div
                        className="h-full rounded-full"
                        style={{ backgroundColor: COLORS[entry.key] ?? DEFAULT_COLOR }}
                        initial={{ width: 0 }}
                        animate={{ width: `${(entry.value / total) * 100}%` }}
                        transition={{ duration: 0.5, ease: 'easeOut' }}
                      />
                    </div>
                    <span className="text-xs text-zinc-300 font-medium w-6 text-right">
                      {entry.value}
                    </span>
                  </div>
                </motion.div>
              ))}
            </AnimatePresence>
          </div>
        </div>
      )}
    </div>
  );
}
