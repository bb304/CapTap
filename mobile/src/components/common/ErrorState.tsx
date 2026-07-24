import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { colors, spacing, typography } from "@/theme";
import { Button } from "@/components/ui";
import { toUserMessage } from "@/api/errors";

type ErrorStateProps = {
  /** Raw error; converted to safe, friendly copy via `toUserMessage`. */
  error?: unknown;
  message?: string;
  onRetry?: () => void;
};

export function ErrorState({ error, message, onRetry }: ErrorStateProps) {
  const body =
    message ??
    (error !== undefined
      ? toUserMessage(error)
      : "Something went wrong. Please try again.");
  return (
    <View style={styles.wrap} accessibilityRole="alert">
      <Text style={styles.title} maxFontSizeMultiplier={1.5}>
        We hit a snag
      </Text>
      <Text style={styles.body} maxFontSizeMultiplier={1.5}>
        {body}
      </Text>
      {onRetry ? <Button label="Try again" onPress={onRetry} /> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: "center",
    gap: spacing.md,
    padding: spacing.xl,
  },
  title: { ...typography.subtitle, textAlign: "center", color: colors.danger },
  body: { ...typography.bodyMuted, textAlign: "center" },
});
