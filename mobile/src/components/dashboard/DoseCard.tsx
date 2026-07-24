import React from "react";
import { StyleSheet, Text, View } from "react-native";
import type { TodayDose } from "@/types/dashboard";
import { formatTime } from "@/utils/format";
import { spacing, typography } from "@/theme";
import { Badge, Button, Card } from "@/components/ui";

type DoseCardProps = {
  dose: TodayDose;
  onMarkTaken?: (dose: TodayDose) => void;
  marking?: boolean;
};

export function DoseCard({ dose, onMarkTaken, marking = false }: DoseCardProps) {
  const canMark =
    Boolean(onMarkTaken) && (dose.status === "Due" || dose.status === "Upcoming");

  return (
    <Card
      accessibilityLabel={`${dose.medicationName} at ${formatTime(dose.scheduledTime)}, ${dose.status}`}
    >
      <View style={styles.row}>
        <View style={styles.copy}>
          <Text style={styles.name} maxFontSizeMultiplier={1.5}>
            {dose.medicationName}
          </Text>
          <Text style={styles.meta} maxFontSizeMultiplier={1.5}>
            {formatTime(dose.scheduledTime)} · {dose.doseQuantity} dose
          </Text>
        </View>
        <Badge status={dose.status} />
      </View>
      {canMark ? (
        <Button
          label="Mark as Taken"
          onPress={() => onMarkTaken?.(dose)}
          loading={marking}
          style={styles.action}
          accessibilityHint="Records that you took this medication dose"
        />
      ) : null}
    </Card>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: spacing.md,
  },
  copy: { flex: 1, gap: 4 },
  name: { ...typography.subtitle, fontSize: 20 },
  meta: { ...typography.bodyMuted, fontSize: 16 },
  action: { marginTop: spacing.md },
});
