import React from "react";
import { Pressable, StyleSheet, TextInput, View } from "react-native";
import { Search, X } from "lucide-react-native";
import { colors, radius, spacing, touchTarget } from "@/theme";

type SearchBarProps = {
  value: string;
  onChangeText: (value: string) => void;
  placeholder?: string;
  onClear?: () => void;
};

export function SearchBar({
  value,
  onChangeText,
  placeholder = "Search medications",
  onClear,
}: SearchBarProps) {
  return (
    <View style={styles.wrap} accessibilityRole="search">
      <Search color={colors.inkMuted} size={22} accessibilityElementsHidden />
      <TextInput
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={colors.inkMuted}
        style={styles.input}
        accessibilityLabel={placeholder}
        returnKeyType="search"
        autoCapitalize="none"
        autoCorrect={false}
      />
      {value.length > 0 ? (
        <Pressable
          onPress={onClear ?? (() => onChangeText(""))}
          accessibilityRole="button"
          accessibilityLabel="Clear search"
          hitSlop={8}
          style={styles.clear}
        >
          <X color={colors.inkMuted} size={20} />
        </Pressable>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    minHeight: touchTarget,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    paddingHorizontal: spacing.lg,
    backgroundColor: colors.surface,
  },
  input: {
    flex: 1,
    fontSize: 18,
    color: colors.ink,
    paddingVertical: spacing.md,
  },
  clear: {
    minWidth: touchTarget,
    minHeight: touchTarget,
    alignItems: "center",
    justifyContent: "center",
  },
});
