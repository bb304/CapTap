import React from "react";
import { StyleSheet, View, ViewProps } from "react-native";
import { colors, radius, shadows, spacing } from "@/theme";

type CardProps = ViewProps & {
  children: React.ReactNode;
};

export function Card({ children, style, ...rest }: CardProps) {
  return (
    <View style={[styles.card, style]} {...rest}>
      {children}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    padding: spacing.lg,
    borderWidth: 1,
    borderColor: colors.border,
    ...shadows.card,
  },
});
