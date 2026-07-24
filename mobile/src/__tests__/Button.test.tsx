import { describe, expect, it, jest } from "@jest/globals";
import React from "react";
import { fireEvent, render, screen } from "@testing-library/react-native";
import { Button } from "@/components/ui/Button";

describe("Button", () => {
  it("renders label and handles press", () => {
    const onPress = jest.fn();
    render(<Button label="Log in" onPress={onPress} />);
    fireEvent.press(screen.getByLabelText("Log in"));
    expect(onPress).toHaveBeenCalled();
  });
});
