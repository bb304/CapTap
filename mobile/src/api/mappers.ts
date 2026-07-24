/**
 * Translate API wire DTOs into the domain shapes the UI components consume.
 * Keeping this mapping in one place lets components stay unchanged as the
 * backend contract evolves.
 */
import type { DashboardSummary, TodayDose } from "@/types/dashboard";
import type { Medication, MedicationSchedule } from "@/types/medication";
import type {
  DashboardTodayResponse,
  MedicationDto,
  ScheduleDto,
  TodayDoseDto,
} from "./types";

export function mapMedication(dto: MedicationDto): Medication {
  const schedules: MedicationSchedule[] = (dto.schedules ?? []).map((s) => ({
    id: s.id,
    scheduledTime: s.scheduledTime,
    doseQuantity: s.doseQuantity,
  }));

  return {
    id: dto.id,
    name: dto.name,
    genericName: dto.genericName ?? undefined,
    brandName: dto.brandName ?? undefined,
    dosageAmount: dto.dosageAmount,
    dosageUnit: dto.dosageUnit,
    form: dto.form ?? undefined,
    instructions: dto.instructions ?? undefined,
    isArchived: dto.isArchived,
    schedules,
  };
}

export function mapSchedule(dto: ScheduleDto): MedicationSchedule {
  return {
    id: dto.id,
    scheduledTime: dto.scheduledTime,
    doseQuantity: dto.doseQuantity,
  };
}

export function mapDose(dto: TodayDoseDto, index: number): TodayDose {
  return {
    id: `${dto.scheduleId}-${dto.scheduledTime}-${index}`,
    medicationId: dto.medicationId,
    medicationName: dto.medicationName,
    scheduledTime: dto.scheduledTime,
    scheduledDoseTime: dto.scheduledDoseTime,
    doseQuantity: dto.doseQuantity,
    status: dto.status,
    scheduleId: dto.scheduleId,
    logId: dto.logId,
  };
}

/** Map the Phase 8 dashboard/today payload into the UI summary model. */
export function mapDashboardToday(
  today: DashboardTodayResponse,
  missedDtos: TodayDoseDto[],
): DashboardSummary {
  const doses = today.doses.map(mapDose);
  const missed = missedDtos.map(mapDose);
  const upcoming = doses.filter((dose) => dose.status === "Upcoming");

  return {
    today: doses,
    upcoming,
    missed,
    completionPercent: today.completionPercent,
    currentStreakDays: today.currentStreakDays,
    longestStreakDays: today.longestStreakDays,
    totalMedicationsToday: today.totalMedicationsToday,
    takenCount: today.takenCount,
    missedCount: today.missedCount,
  };
}

/**
 * @deprecated Prefer {@link mapDashboardToday}. Kept temporarily for tests that
 * still compose summaries from raw dose lists.
 */
export function buildDashboardSummary(
  todayDtos: TodayDoseDto[],
  missedDtos: TodayDoseDto[],
  streaks?: { currentStreakDays: number; longestStreakDays: number },
): DashboardSummary {
  return mapDashboardToday(
    {
      doses: todayDtos,
      completionPercent:
        todayDtos.length === 0
          ? 0
          : Math.round(
              (100 * todayDtos.filter((d) => d.status === "Taken").length) /
                todayDtos.length,
            ),
      currentStreakDays: streaks?.currentStreakDays ?? 0,
      longestStreakDays: streaks?.longestStreakDays ?? 0,
      totalMedicationsToday: todayDtos.length,
      takenCount: todayDtos.filter((d) => d.status === "Taken").length,
      missedCount: todayDtos.filter((d) => d.status === "Missed").length,
    },
    missedDtos,
  );
}
