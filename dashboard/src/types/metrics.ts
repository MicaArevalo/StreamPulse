export interface WindowMetrics {
  Total: number;
  Completed: number;
  Failed: number;
  SuccessRate: number;
  TotalAmount: number;
  AvgAmount: number;
  AvgProcessingMs: number;
  P90Ms: number;
  P95Ms: number;
  P99Ms: number;
  AnomalyCount: number;
  FailureReasons: Record<string, number>;
  WindowStart: string;
  SavedAt: string;
}
