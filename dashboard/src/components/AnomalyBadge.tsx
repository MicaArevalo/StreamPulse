import { motion, AnimatePresence } from 'framer-motion';
import { AlertTriangle } from 'lucide-react';

interface Props {
  count: number;
}

export function AnomalyBadge({ count }: Props) {
  return (
    <AnimatePresence>
      {count > 0 && (
        <motion.div
          className="flex items-center gap-3 bg-red-500/10 border border-red-500/30 rounded-xl px-5 py-3 mb-4"
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.95 }}
          transition={{ duration: 0.3 }}
        >
          <div className="relative">
            <span className="absolute inset-0 rounded-full bg-red-500 opacity-30 animate-ping" />
            <AlertTriangle className="w-5 h-5 text-red-400 relative" />
          </div>
          <div>
            <span className="text-red-400 font-semibold">{count} anomalía{count !== 1 ? 's' : ''} detectada{count !== 1 ? 's' : ''}</span>
            <span className="text-zinc-500 text-sm ml-2">en la última ventana</span>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
