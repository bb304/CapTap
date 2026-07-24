import React, { useEffect, useState } from "react";
import { Animated, StyleSheet, View, type ViewStyle } from "react-native";
import { colors, radius, spacing } from "@/theme";

type SkeletonProps = {
  width?: number | `${number}%`;
  height?: number;
  style?: ViewStyle;
  radiusSize?: number;
};

/** A single pulsing placeholder block. */
export function Skeleton({
  width = "100%",
  height = 16,
  style,
  radiusSize = radius.sm,
}: SkeletonProps) {
  const [opacity] = useState(() => new Animated.Value(0.4));

  useEffect(() => {
    const animation = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, {
          toValue: 1,
          duration: 700,
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 0.4,
          duration: 700,
          useNativeDriver: true,
        }),
      ]),
    );
    animation.start();
    return () => animation.stop();
  }, [opacity]);

  return (
    <Animated.View
      accessibilityElementsHidden
      importantForAccessibility="no-hide-descendants"
      style={[
        { width, height, borderRadius: radiusSize, backgroundColor: colors.border, opacity },
        style,
      ]}
    />
  );
}

/** Card-shaped skeleton used for list/detail loading. */
export function SkeletonCard() {
  return (
    <View style={styles.card}>
      <Skeleton width="60%" height={20} />
      <Skeleton width="40%" height={14} style={{ marginTop: spacing.sm }} />
      <Skeleton width="80%" height={14} style={{ marginTop: spacing.sm }} />
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.lg,
  },
});
