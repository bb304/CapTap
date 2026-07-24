import { describe, expect, it } from "@jest/globals";
import React from "react";
import { render, screen } from "@testing-library/react-native";
import WelcomeScreen from "@/app/(auth)/welcome";

describe("navigation screens", () => {
  it("renders welcome brand", () => {
    render(<WelcomeScreen />);
    expect(screen.getByText("CapTap")).toBeTruthy();
    expect(screen.getByText("Tap. Confirm. Peace of mind.")).toBeTruthy();
  });
});
