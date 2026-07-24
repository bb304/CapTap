import { describe, expect, it } from "@jest/globals";
import React from "react";
import { render, screen } from "@testing-library/react-native";
import { MedicationCard } from "@/components/medication/MedicationCard";
import { sampleMedication } from "./fixtures";

describe("MedicationCard", () => {
  it("renders medication name and dosage", () => {
    render(<MedicationCard medication={sampleMedication} />);
    expect(screen.getByText("Metformin")).toBeTruthy();
    expect(screen.getByText(/500 mg/)).toBeTruthy();
  });
});
