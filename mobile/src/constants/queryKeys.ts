/** Stable query keys for cache reads and mutation invalidation. */
export const queryKeys = {
  dashboard: ["dashboard"] as const,
  medications: ["medications"] as const,
  medication: (id: string) => ["medications", id] as const,
  schedules: (medicationId: string) => ["schedules", medicationId] as const,
  medicationSearch: (query: string) => ["medication-search", query] as const,
  logHistory: ["medication-logs", "history"] as const,
  streak: ["dashboard", "streak"] as const,
  nfcTags: ["nfc", "tags"] as const,
  notificationPreferences: ["notifications", "preferences"] as const,
};
