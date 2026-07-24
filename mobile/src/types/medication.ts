export type AdherenceStatus = "Upcoming" | "Due" | "Taken" | "Missed";

export type MedicationSchedule = {
  id: string;
  scheduledTime: string;
  doseQuantity: number;
};

export type Medication = {
  id: string;
  name: string;
  genericName?: string;
  brandName?: string;
  dosageAmount: number;
  dosageUnit: string;
  form?: string;
  instructions?: string;
  isArchived: boolean;
  schedules: MedicationSchedule[];
};
