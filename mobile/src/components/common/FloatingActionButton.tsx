import React from "react";
import { Pressable, StyleSheet } from "react-native";
import { Plus } from "lucide-react-native";
import { colors, shadows, touchTarget } from "@/theme";

type FloatingActionButtonProps = {
  onPress: () => void;
  accessibilityLabel?: string;
};

export function FloatingActionButton({
  onPress,
  accessibilityLabel = "Add medication",
}: FloatingActionButtonProps) {
  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      style={({ pressed }) => [styles.fab, pressed ? styles.pressed : null]}
    >
      <Plus color={colors.white} size={28} strokeWidth={2.5} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  fab: {
    position: "absolute",
    right: 24,
    bottom: 28,
    width: 56,
    height: 56,
    minWidth: touchTarget,
    minHeight: touchTarget,
    borderRadius: 28,
    backgroundColor: colors.primary,
    alignItems: "center",
    justifyContent: "center",
    ...shadows.fab,
  },
  pressed: {
    opacity: 0.9,
  },
});
