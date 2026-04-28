import { Activity, TrendingUp, DollarSign, Zap, Clock } from 'lucide-react';
import { useSignalR } from './hooks/useSignalR';
import { LiveIndicator } from './components/LiveIndicator';
import { MetricCard } from './components/MetricCard';
import { AnomalyBadge } from './components/AnomalyBadge';
import { LatencyChart } from './components/LatencyChart';
import { VolumeChart } from './components/VolumeChart';
import { FailureChart } from './components/FailureChart';

export default function App() {
  const { metrics, history, connected } = useSignalR();

  const successColor = metrics
    ? metrics.SuccessRate >= 90 ? 'emerald' : metrics.SuccessRate >= 75 ? 'amber' : 'red'
    : 'emerald';

  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-50">
      <div className="max-w-6xl mx-auto px-6 py-8">

        {/* Header */}
        <header className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 bg-emerald-500 rounded-xl flex items-center justify-center shadow-lg shadow-emerald-500/20">
              <Zap className="w-5 h-5 text-black" fill="black" />
            </div>
            <div>
              <h1 className="text-lg font-semibold tracking-tight leading-none">StreamPulse</h1>
              <p className="text-xs text-zinc-500 mt-0.5">Analytics en tiempo real</p>
            </div>
          </div>
          <LiveIndicator connected={connected} />
        </header>

        {/* Anomaly alert */}
        <AnomalyBadge count={metrics?.AnomalyCount ?? 0} />

        {/* Primary metrics */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
          <MetricCard
            label="Transacciones / ventana"
            value={metrics?.Total ?? 0}
            icon={<Activity className="w-4 h-4" />}
            color="emerald"
          />
          <MetricCard
            label="Tasa de éxito"
            value={metrics?.SuccessRate ?? 0}
            decimals={1}
            suffix="%"
            icon={<TrendingUp className="w-4 h-4" />}
            color={successColor}
          />
          <MetricCard
            label="Volumen total"
            value={metrics ? metrics.TotalAmount / 1_000 : 0}
            decimals={1}
            prefix="$"
            suffix="K"
            icon={<DollarSign className="w-4 h-4" />}
            color="blue"
          />
          <MetricCard
            label="Latencia promedio"
            value={metrics?.AvgProcessingMs ?? 0}
            decimals={0}
            suffix=" ms"
            icon={<Clock className="w-4 h-4" />}
            color="violet"
          />
        </div>

        {/* Percentile metrics */}
        <div className="grid grid-cols-3 gap-4 mb-6">
          <MetricCard
            label="P90 latencia"
            value={metrics?.P90Ms ?? 0}
            decimals={0}
            suffix=" ms"
            color="emerald"
          />
          <MetricCard
            label="P95 latencia"
            value={metrics?.P95Ms ?? 0}
            decimals={0}
            suffix=" ms"
            color="amber"
          />
          <MetricCard
            label="P99 latencia"
            value={metrics?.P99Ms ?? 0}
            decimals={0}
            suffix=" ms"
            color="red"
          />
        </div>

        {/* Charts row 1 */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-4">
          <LatencyChart history={history} />
          <VolumeChart history={history} />
        </div>

        {/* Charts row 2 */}
        <FailureChart metrics={metrics} />

        {/* Footer */}
        <footer className="mt-8 text-center text-xs text-zinc-700">
          Conectado via SignalR · API: localhost:5000 · Actualiza cada 2 segundos
        </footer>

      </div>
    </div>
  );
}
