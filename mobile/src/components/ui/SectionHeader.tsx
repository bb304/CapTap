import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { spacing, typography } from "@/theme";

type SectionHeaderProps = {
  title: string;
  subtitle?: string;
};

export function SectionHeader({ title, subtitle }: SectionHeaderProps) {
  return (
    <View style={styles.wrap} accessibilityRole="header">
      <Text style={styles.title} maxFontSizeMultiplier={1.5}>
        {title}
      </Text>
      {subtitle ? (
        <Text style={styles.subtitle} maxFontSizeMultiplier={1.5}>
          {subtitle}
        </Text>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    gap: spacing.xs,
  },
  title: {
    ...typography.subtitle,
  },
  subtitle: {
    ...typography.bodyMuted,
    fontSize: 16,
  },
});
