import React from "react";
import { Pressable, StyleSheet, Text } from "react-native";
import type { Medication } from "@/types/medication";
import { formatDosage, formatTime } from "@/utils/format";
import { colors, spacing, typography } from "@/theme";
import { Card } from "@/components/ui";

type MedicationCardProps = {
  medication: Medication;
  onPress?: () => void;
};

export function MedicationCard({ medication, onPress }: MedicationCardProps) {
  const times = medication.schedules.map((s) => formatTime(s.scheduledTime)).join(" · ");

  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={`${medication.name}, ${formatDosage(medication.dosageAmount, medication.dosageUnit)}`}
      style={({ pressed }) => [pressed ? styles.pressed : null]}
    >
      <Card>
        <Text style={styles.name} maxFontSizeMultiplier={1.5}>
          {medication.name}
        </Text>
        <Text style={styles.meta} maxFontSizeMultiplier={1.5}>
          {formatDosage(medication.dosageAmount, medication.dosageUnit)}
          {medication.form ? ` · ${medication.form}` : ""}
        </Text>
        <Text style={styles.schedule} maxFontSizeMultiplier={1.5}>
          {times || "No schedule yet"}
        </Text>
      </Card>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  pressed: { opacity: 0.92 },
  name: { ...typography.subtitle, fontSize: 22 },
  meta: { ...typography.bodyMuted, marginTop: spacing.xs },
  schedule: { ...typography.caption, marginTop: spacing.sm, color: colors.primaryDark },
});
