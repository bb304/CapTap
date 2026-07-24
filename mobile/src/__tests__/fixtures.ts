import type { TodayDose } from "@/types/dashboard";
import type { Medication } from "@/types/medication";

export const sampleMedication: Medication = {
  id: "med-metformin",
  name: "Metformin",
  genericName: "Metformin Hydrochloride",
  brandName: "Glucophage",
  dosageAmount: 500,
  dosageUnit: "mg",
  form: "Tablet",
  instructions: "Take with food",
  isArchived: false,
  schedules: [
    { id: "sch-m-1", scheduledTime: "08:00:00", doseQuantity: 1 },
    { id: "sch-m-2", scheduledTime: "20:00:00", doseQuantity: 1 },
  ],
};

export const sampleDose: TodayDose = {
  id: "dose-1",
  medicationId: "med-metformin",
  medicationName: "Metformin",
  scheduledTime: "08:00:00",
  doseQuantity: 1,
  status: "Taken",
  scheduleId: "sch-m-1",
};
