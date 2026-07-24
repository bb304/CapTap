import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { toUserMessage } from "@/api/errors";
import { colors, radius, spacing, typography } from "@/theme";

type FormErrorProps = {
  error?: unknown;
};

/** Inline, accessible error banner for form submission failures. */
export function FormError({ error }: FormErrorProps) {
  if (error === undefined || error === null) return null;

  return (
    <View style={styles.wrap} accessibilityRole="alert" accessibilityLiveRegion="polite">
      <Text style={styles.text} maxFontSizeMultiplier={1.5}>
        {toUserMessage(error)}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    backgroundColor: "#FCEAEA",
    borderRadius: radius.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  },
  text: {
    ...typography.body,
    color: colors.danger,
  },
});
