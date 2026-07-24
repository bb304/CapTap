import { describe, expect, it } from "@jest/globals";
import React from "react";
import { render, screen } from "@testing-library/react-native";
import { Input } from "@/components/ui/Input";

describe("Input", () => {
  it("shows label and error", () => {
    render(<Input label="Email" error="Required" value="" onChangeText={() => undefined} />);
    expect(screen.getByText("Email")).toBeTruthy();
    expect(screen.getByText("Required")).toBeTruthy();
  });
});
