import { useEffect } from 'react';
import { motion, useMotionValue, useSpring, useTransform } from 'framer-motion';
import type { ReactNode } from 'react';

const colorMap = {
  emerald: 'text-emerald-400 bg-emerald-500/10',
  blue:    'text-blue-400 bg-blue-500/10',
  violet:  'text-violet-400 bg-violet-500/10',
  amber:   'text-amber-400 bg-amber-500/10',
  red:     'text-red-400 bg-red-500/10',
};

interface Props {
  label: string;
  value: number;
  decimals?: number;
  prefix?: string;
  suffix?: string;
  icon?: ReactNode;
  color?: keyof typeof colorMap;
}

function AnimatedNumber({ value, decimals = 0, prefix = '', suffix = '' }: {
  value: number;
  decimals?: number;
  prefix?: string;
  suffix?: string;
}) {
  const mv = useMotionValue(value);
  const spring = useSpring(mv, { stiffness: 80, damping: 20 });
  const display = useTransform(spring, v =>
    `${prefix}${v.toFixed(decimals)}${suffix}`
  );

  useEffect(() => { mv.set(value); }, [value, mv]);

  return <motion.span>{display}</motion.span>;
}

export function MetricCard({ label, value, decimals = 0, prefix = '', suffix = '', icon, color = 'emerald' }: Props) {
  const colors = colorMap[color];
  const [textColor, bgColor] = colors.split(' ');

  return (
    <motion.div
      className="card flex flex-col gap-3"
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4 }}
    >
      <div className="flex items-center justify-between">
        <span className="text-xs text-zinc-500 uppercase tracking-wider">{label}</span>
        {icon && (
          <div className={`p-1.5 rounded-lg ${bgColor} ${textColor}`}>
            {icon}
          </div>
        )}
      </div>
      <div className={`text-3xl font-bold tracking-tight ${textColor}`}>
        <AnimatedNumber value={value} decimals={decimals} prefix={prefix} suffix={suffix} />
      </div>
    </motion.div>
  );
}
