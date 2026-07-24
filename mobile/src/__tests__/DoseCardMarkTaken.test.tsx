import { describe, expect, it, jest } from "@jest/globals";
import React from "react";
import { fireEvent, render, screen } from "@testing-library/react-native";
import { DoseCard } from "@/components/dashboard/DoseCard";
import { sampleDose } from "./fixtures";

describe("DoseCard Mark as Taken", () => {
  it("shows Mark as Taken for Due doses and invokes the callback", () => {
    const onMarkTaken = jest.fn();
    render(
      <DoseCard
        dose={{ ...sampleDose, status: "Due", scheduleId: "s1" }}
        onMarkTaken={onMarkTaken}
      />,
    );

    fireEvent.press(screen.getByLabelText("Mark as Taken"));
    expect(onMarkTaken).toHaveBeenCalled();
  });

  it("hides Mark as Taken when already Taken", () => {
    render(<DoseCard dose={{ ...sampleDose, status: "Taken", scheduleId: "s1" }} />);
    expect(screen.queryByLabelText("Mark as Taken")).toBeNull();
  });
});
