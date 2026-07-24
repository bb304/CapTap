import { describe, expect, it } from "@jest/globals";
import React from "react";
import { render, screen } from "@testing-library/react-native";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";

describe("LoadingSpinner", () => {
  it("renders loading label", () => {
    render(<LoadingSpinner label="Loading medications" />);
    expect(screen.getByLabelText("Loading medications")).toBeTruthy();
  });
});
