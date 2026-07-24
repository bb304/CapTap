import { describe, expect, it } from "@jest/globals";
import React from "react";
import { render, screen } from "@testing-library/react-native";
import { DoseCard } from "@/components/dashboard/DoseCard";
import { sampleDose } from "./fixtures";

describe("DoseCard", () => {
  it("renders dose status badge", () => {
    render(<DoseCard dose={sampleDose} />);
    expect(screen.getByText("Metformin")).toBeTruthy();
    expect(screen.getByText("Taken")).toBeTruthy();
  });
});
