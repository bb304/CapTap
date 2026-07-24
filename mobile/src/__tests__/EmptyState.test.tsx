import { describe, expect, it } from "@jest/globals";
import React from "react";
import { render, screen } from "@testing-library/react-native";
import { EmptyState } from "@/components/ui/EmptyState";

describe("EmptyState", () => {
  it("renders title and description", () => {
    render(<EmptyState title="Nothing here" description="Add your first medication." />);
    expect(screen.getByText("Nothing here")).toBeTruthy();
    expect(screen.getByText("Add your first medication.")).toBeTruthy();
  });
});
