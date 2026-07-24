/**
 * Wire types mirroring the CapTap ASP.NET Core API.
 *
 * The API serializes JSON as camelCase, enums as strings, and `TimeOnly`
 * as `"HH:mm:ss"`. Every response is wrapped in {@link ApiResponse}.
 */

export type ApiError = {
  code: string;
  message: string;
};

export type ApiResponse<T> = {
  success: boolean;
  data?: T;
  message?: string;
  error?: ApiError;
};

// ── Auth ────────────────────────────────────────────────────────────────

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
  confirmPassword: string;
};

export type ForgotPasswordRequest = {
  email: string;
};

export type ResetPasswordRequest = {
  token: string;
  newPassword: string;
};

export type RefreshTokenRequest = {
  refreshToken: string;
};

/** Token pair returned by login / refresh. `expiresIn` is seconds. */
export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
};

export type RegisterResponse = {
  userId: string;
  message: string;
};

// ── Medications ─────────────────────────────────────────────────────────

export type MedicationDto = {
  id: string;
  name: string;
  genericName?: string | null;
  brandName?: string | null;
  dosageAmount: number;
  dosageUnit: string;
  form?: string | null;
  instructions?: string | null;
  isArchived: boolean;
  schedules?: MedicationScheduleSummaryDto[];
};

export type MedicationScheduleSummaryDto = {
  id: string;
  frequency: FrequencyType;
  scheduledTime: string;
  doseQuantity: number;
  isActive: boolean;
};

export type CreateMedicationRequest = {
  name: string;
  genericName?: string | null;
  brandName?: string | null;
  fdaIdentifier?: string | null;
  dosageAmount: number;
  dosageUnit: string;
  form?: string | null;
  instructions?: string | null;
};

export type UpdateMedicationRequest = Partial<CreateMedicationRequest>;

export type MedicationSearchResult = {
  name: string;
  brand?: string | null;
  identifier?: string | null;
};

// ── Scheduling & adherence ──────────────────────────────────────────────

export type FrequencyType = "OnceDaily" | "TwiceDaily" | "ThreeTimesDaily";

export type AdherenceStatus = "Upcoming" | "Due" | "Taken" | "Missed";

export type ScheduleDto = {
  id: string;
  medicationId: string;
  frequency: FrequencyType;
  /** "HH:mm:ss" */
  scheduledTime: string;
  doseQuantity: number;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
};

export type CreateScheduleRequest = {
  frequency: FrequencyType;
  /** "HH:mm:ss" */
  scheduledTime: string;
  doseQuantity: number;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
};

export type UpdateScheduleRequest = Partial<
  Omit<CreateScheduleRequest, "frequency"> & {
    frequency: FrequencyType;
    isActive: boolean;
  }
>;

export type TodayDoseDto = {
  medicationId: string;
  medicationName: string;
  /** "HH:mm:ss" */
  scheduledTime: string;
  /** UTC ISO for POST /medication-logs */
  scheduledDoseTime?: string;
  doseQuantity: number;
  status: AdherenceStatus;
  logId?: string | null;
  scheduleId: string;
};

export type DashboardTodayResponse = {
  doses: TodayDoseDto[];
  completionPercent: number;
  currentStreakDays: number;
  longestStreakDays: number;
  totalMedicationsToday: number;
  takenCount: number;
  missedCount: number;
};

export type AdherenceStreakDto = {
  currentStreakDays: number;
  longestStreakDays: number;
};

export type LoggingMethod = "Manual" | "Nfc";

export type CreateMedicationLogRequest = {
  medicationId: string;
  scheduledDoseTime: string;
  loggingMethod: LoggingMethod;
  scheduleId?: string | null;
  notes?: string | null;
};

export type MedicationLogDto = {
  id: string;
  medicationId: string;
  medicationName: string;
  scheduleId?: string | null;
  scheduledDoseTime: string;
  loggedAt: string;
  loggingMethod: LoggingMethod;
  notes?: string | null;
};

export type MedicationLogHistoryItem = {
  id: string;
  medicationId: string;
  medicationName: string;
  scheduledDoseTime: string;
  loggedAt: string;
  loggingMethod: LoggingMethod;
  notes?: string | null;
};

export type PaginatedMedicationLogHistory = {
  items: MedicationLogHistoryItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type AssignNfcTagRequest = {
  medicationId: string;
  tagIdentifier: string;
};

export type UnassignNfcTagRequest = {
  medicationId: string;
};

export type NfcTagDto = {
  id: string;
  medicationId: string;
  medicationName: string;
  tagIdentifier: string;
  isAssigned: boolean;
  assignedAt: string;
  lastScannedAt?: string | null;
};

export type NfcResolveDto = {
  medicationId: string;
  medicationName: string;
  dosageAmount: number;
  dosageUnit: string;
  form?: string | null;
  scheduleId?: string | null;
  scheduledTime?: string | null;
  scheduledDoseTime?: string | null;
  status?: AdherenceStatus | null;
  alreadyLogged: boolean;
  tagIdentifier: string;
};

export type UserProfileDto = {
  id: string;
  email: string;
  timeZoneId: string;
};

export type UpdateTimeZoneRequest = {
  timeZoneId: string;
};
