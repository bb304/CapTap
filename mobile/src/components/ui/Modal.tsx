import React from "react";
import {
  Modal as RNModal,
  Pressable,
  StyleSheet,
  Text,
  View,
} from "react-native";
import { colors, radius, spacing, typography } from "@/theme";
import { Button } from "./Button";

type ModalProps = {
  visible: boolean;
  title: string;
  children: React.ReactNode;
  onClose: () => void;
  primaryLabel?: string;
  onPrimary?: () => void;
};

export function Modal({
  visible,
  title,
  children,
  onClose,
  primaryLabel,
  onPrimary,
}: ModalProps) {
  return (
    <RNModal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
      <View style={styles.backdrop}>
        <Pressable style={StyleSheet.absoluteFill} onPress={onClose} accessibilityLabel="Dismiss dialog" />
        <View style={styles.sheet} accessibilityRole="summary" accessibilityViewIsModal>
          <Text style={styles.title} maxFontSizeMultiplier={1.5}>
            {title}
          </Text>
          <View style={styles.body}>{children}</View>
          <View style={styles.actions}>
            <Button label="Close" variant="ghost" onPress={onClose} />
            {primaryLabel && onPrimary ? (
              <Button label={primaryLabel} onPress={onPrimary} />
            ) : null}
          </View>
        </View>
      </View>
    </RNModal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: "rgba(28, 36, 48, 0.45)",
    justifyContent: "center",
    padding: spacing.xl,
  },
  sheet: {
    backgroundColor: colors.surface,
    borderRadius: radius.xl,
    padding: spacing.xl,
    gap: spacing.lg,
  },
  title: {
    ...typography.subtitle,
  },
  body: {
    gap: spacing.md,
  },
  actions: {
    gap: spacing.sm,
  },
});
