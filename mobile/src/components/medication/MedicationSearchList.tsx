import React from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { colors, spacing, touchTarget, typography } from "@/theme";

type SearchHit = {
  name: string;
  brand?: string;
  identifier?: string;
};

type MedicationSearchListProps = {
  results: SearchHit[];
  onSelect: (item: SearchHit) => void;
};

export function MedicationSearchList({ results, onSelect }: MedicationSearchListProps) {
  return (
    <View style={styles.wrap}>
      {results.map((item) => (
        <Pressable
          key={`${item.name}-${item.identifier ?? item.brand}`}
          onPress={() => onSelect(item)}
          accessibilityRole="button"
          accessibilityLabel={`Select ${item.name}`}
          style={styles.row}
        >
          <Text style={styles.name} maxFontSizeMultiplier={1.5}>
            {item.name}
          </Text>
          <Text style={styles.meta} maxFontSizeMultiplier={1.5}>
            {[item.brand, item.identifier].filter(Boolean).join(" · ")}
          </Text>
        </Pressable>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { gap: spacing.sm },
  row: {
    minHeight: touchTarget,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 14,
    backgroundColor: colors.surface,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    gap: 2,
  },
  name: { ...typography.label, fontSize: 18 },
  meta: { ...typography.caption },
});
