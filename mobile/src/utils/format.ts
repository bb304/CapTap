export function formatTime(value: string): string {
  // Accept TimeOnly ("08:00:00") or ISO datetime ("2026-07-24T08:00:00Z").
  const timePart = value.includes("T")
    ? value.split("T")[1]?.slice(0, 8) ?? value
    : value;
  const [hours, minutes] = timePart.split(":").map(Number);
  if (Number.isNaN(hours) || Number.isNaN(minutes)) {
    return value;
  }
  const period = hours >= 12 ? "PM" : "AM";
  const hour12 = hours % 12 || 12;
  return `${hour12}:${minutes.toString().padStart(2, "0")} ${period}`;
}

export function formatDosage(amount: number, unit: string): string {
  return `${amount} ${unit}`;
}

/**
 * Normalize a user-entered clock time to the API's `TimeOnly` format
 * ("HH:mm:ss"). Accepts "H:mm", "HH:mm", or "HH:mm:ss".
 */
export function toApiTime(value: string): string {
  const parts = value.trim().split(":");
  const hours = (parts[0] ?? "0").padStart(2, "0");
  const minutes = (parts[1] ?? "00").padStart(2, "0");
  const seconds = (parts[2] ?? "00").padStart(2, "0");
  return `${hours}:${minutes}:${seconds}`;
}

/**
 * Build an ISO UTC datetime for today's scheduled dose from a TimeOnly string.
 * Uses the device's local calendar day so midnight crossings match the backend
 * user time zone (synced via PUT /users/me/timezone).
 */
export function toScheduledDoseIso(scheduledTime: string, day: Date = new Date()): string {
  const [h, m, s] = toApiTime(scheduledTime).split(":").map(Number);
  const local = new Date(
    day.getFullYear(),
    day.getMonth(),
    day.getDate(),
    h,
    m,
    s ?? 0,
  );
  return local.toISOString();
}
