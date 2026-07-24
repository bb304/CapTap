import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { spacing, typography } from "@/theme";
import { Button } from "./Button";

type EmptyStateProps = {
  title: string;
  description: string;
  actionLabel?: string;
  onAction?: () => void;
};

export function EmptyState({ title, description, actionLabel, onAction }: EmptyStateProps) {
  return (
    <View style={styles.wrap} accessibilityRole="summary">
      <Text style={styles.title} maxFontSizeMultiplier={1.5}>
        {title}
      </Text>
      <Text style={styles.description} maxFontSizeMultiplier={1.5}>
        {description}
      </Text>
      {actionLabel && onAction ? <Button label={actionLabel} onPress={onAction} /> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: "center",
    gap: spacing.md,
    paddingVertical: spacing.xxl,
    paddingHorizontal: spacing.lg,
  },
  title: {
    ...typography.subtitle,
    textAlign: "center",
  },
  description: {
    ...typography.bodyMuted,
    textAlign: "center",
  },
});
