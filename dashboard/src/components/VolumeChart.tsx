import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, Cell
} from 'recharts';
import type { WindowMetrics } from '../types/metrics';

interface Props {
  history: WindowMetrics[];
}

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
}

function formatAmount(value: number) {
  return `$${(value / 1000).toFixed(0)}K`;
}

export function VolumeChart({ history }: Props) {
  const data = history.map((m, i) => ({
    time: formatTime(m.WindowStart),
    volume: Math.round(m.TotalAmount),
    isLatest: i === history.length - 1,
  }));

  return (
    <div className="card">
      <h2 className="text-sm text-zinc-500 uppercase tracking-wider mb-4">
        Volumen por ventana (ARS)
      </h2>
      {data.length === 0 ? (
        <div className="h-48 flex items-center justify-center text-zinc-600 text-sm">
          Esperando datos...
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={200}>
          <BarChart data={data} margin={{ top: 4, right: 8, left: -4, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#27272a" vertical={false} />
            <XAxis dataKey="time" tick={{ fill: '#71717a', fontSize: 11 }} />
            <YAxis tick={{ fill: '#71717a', fontSize: 11 }} tickFormatter={v => `$${(v/1000).toFixed(0)}K`} />
            <Tooltip
              contentStyle={{ backgroundColor: '#18181b', border: '1px solid #27272a', borderRadius: 8 }}
              labelStyle={{ color: '#a1a1aa' }}
              formatter={(value: number) => [formatAmount(value), 'Volumen']}
            />
            <Bar dataKey="volume" radius={[4, 4, 0, 0]} isAnimationActive>
              {data.map((entry, index) => (
                <Cell
                  key={index}
                  fill={entry.isLatest ? '#3b82f6' : '#1d4ed8'}
                  opacity={entry.isLatest ? 1 : 0.6}
                />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}
