import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import type { WindowMetrics } from '../types/metrics';

// En dev usa VITE_API_URL; en Docker nginx proxy usa rutas relativas
const API_URL = import.meta.env.VITE_API_URL ?? '';

export function useSignalR() {
  const [metrics, setMetrics] = useState<WindowMetrics | null>(null);
  const [history, setHistory] = useState<WindowMetrics[]>([]);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    fetch(`${API_URL}/api/metrics/history?count=10`)
      .then(r => r.json())
      .then((data: WindowMetrics[]) => setHistory([...data].reverse()))
      .catch(() => {});

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/metrics`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('MetricsUpdate', (data: string) => {
      const parsed: WindowMetrics = JSON.parse(data);
      setMetrics(parsed);
      setHistory(prev => [...prev.slice(-9), parsed]);
    });

    connection.onreconnecting(() => setConnected(false));
    connection.onreconnected(() => setConnected(true));
    connection.onclose(() => setConnected(false));

    connection.start()
      .then(() => setConnected(true))
      .catch(() => setConnected(false));

    return () => { connection.stop(); };
  }, []);

  return { metrics, history, connected };
}
