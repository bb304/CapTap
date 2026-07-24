import React from "react";
import { StyleSheet, Text } from "react-native";
import { colors, spacing, typography } from "@/theme";
import { Card } from "@/components/ui";

type StatCardProps = {
  label: string;
  value: string;
  hint?: string;
};

export function StatCard({ label, value, hint }: StatCardProps) {
  return (
    <Card style={styles.card} accessibilityLabel={`${label}: ${value}`}>
      <Text style={styles.label} maxFontSizeMultiplier={1.4}>
        {label}
      </Text>
      <Text style={styles.value} maxFontSizeMultiplier={1.4}>
        {value}
      </Text>
      {hint ? (
        <Text style={styles.hint} maxFontSizeMultiplier={1.4}>
          {hint}
        </Text>
      ) : null}
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    flex: 1,
    minWidth: 140,
    backgroundColor: colors.primarySoft,
    borderColor: colors.primarySoft,
  },
  label: { ...typography.caption, color: colors.primaryDark },
  value: { ...typography.title, fontSize: 32, marginTop: spacing.xs, color: colors.primaryDark },
  hint: { ...typography.caption, marginTop: spacing.xs },
});
