interface Props {
  connected: boolean;
}

export function LiveIndicator({ connected }: Props) {
  return (
    <div className="flex items-center gap-2 text-sm">
      <div className="relative flex items-center justify-center">
        {connected && (
          <span className="absolute inline-flex h-3 w-3 rounded-full bg-emerald-400 opacity-75 animate-ping" />
        )}
        <span
          className={`relative inline-flex h-2.5 w-2.5 rounded-full ${
            connected ? 'bg-emerald-500' : 'bg-zinc-600'
          }`}
        />
      </div>
      <span className={connected ? 'text-emerald-400' : 'text-zinc-500'}>
        {connected ? 'LIVE' : 'DISCONNECTED'}
      </span>
    </div>
  );
}
