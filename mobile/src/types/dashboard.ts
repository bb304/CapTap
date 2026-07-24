import type { AdherenceStatus } from "./medication";

export type TodayDose = {
  id: string;
  medicationId: string;
  medicationName: string;
  scheduledTime: string;
  scheduledDoseTime?: string;
  doseQuantity: number;
  status: AdherenceStatus;
  scheduleId: string;
  logId?: string | null;
};

export type DashboardSummary = {
  today: TodayDose[];
  upcoming: TodayDose[];
  missed: TodayDose[];
  currentStreakDays: number;
  longestStreakDays: number;
  completionPercent: number;
  totalMedicationsToday: number;
  takenCount: number;
  missedCount: number;
};
