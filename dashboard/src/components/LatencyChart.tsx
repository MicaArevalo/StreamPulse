import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer
} from 'recharts';
import type { WindowMetrics } from '../types/metrics';

interface Props {
  history: WindowMetrics[];
}

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
}

export function LatencyChart({ history }: Props) {
  const data = history.map(m => ({
    time: formatTime(m.WindowStart),
    P90: Math.round(m.P90Ms),
    P95: Math.round(m.P95Ms),
    P99: Math.round(m.P99Ms),
  }));

  return (
    <div className="card">
      <h2 className="text-sm text-zinc-500 uppercase tracking-wider mb-4">
        Latencia de procesamiento
      </h2>
      {data.length === 0 ? (
        <div className="h-48 flex items-center justify-center text-zinc-600 text-sm">
          Esperando datos...
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={200}>
          <LineChart data={data} margin={{ top: 4, right: 8, left: -16, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#27272a" />
            <XAxis dataKey="time" tick={{ fill: '#71717a', fontSize: 11 }} />
            <YAxis tick={{ fill: '#71717a', fontSize: 11 }} unit="ms" />
            <Tooltip
              contentStyle={{ backgroundColor: '#18181b', border: '1px solid #27272a', borderRadius: 8 }}
              labelStyle={{ color: '#a1a1aa' }}
              itemStyle={{ color: '#fafafa' }}
            />
            <Legend wrapperStyle={{ fontSize: 12, color: '#a1a1aa' }} />
            <Line type="monotone" dataKey="P90" stroke="#10b981" strokeWidth={2} dot={false} isAnimationActive />
            <Line type="monotone" dataKey="P95" stroke="#f59e0b" strokeWidth={2} dot={false} isAnimationActive />
            <Line type="monotone" dataKey="P99" stroke="#ef4444" strokeWidth={2} dot={false} isAnimationActive />
          </LineChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}
