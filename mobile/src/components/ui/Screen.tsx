import React from "react";
import { ScrollView, StyleSheet, useWindowDimensions, View, ViewStyle } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { colors, spacing } from "@/theme";

type ScreenProps = {
  children: React.ReactNode;
  scroll?: boolean;
  style?: ViewStyle;
  contentStyle?: ViewStyle;
  /** Optional RefreshControl for pull-to-refresh (scroll screens only). */
  refreshControl?: React.ComponentProps<typeof ScrollView>["refreshControl"];
};

export function Screen({
  children,
  scroll = true,
  style,
  contentStyle,
  refreshControl,
}: ScreenProps) {
  const { width } = useWindowDimensions();
  const maxWidth = width >= 768 ? 720 : undefined;

  const body = (
    <View style={[styles.content, maxWidth ? styles.centered : null, { maxWidth }, contentStyle]}>
      {children}
    </View>
  );

  if (scroll) {
    return (
      <SafeAreaView style={[styles.safe, style]} edges={["top", "left", "right"]}>
        <ScrollView
          contentContainerStyle={[styles.scroll, maxWidth ? styles.scrollCentered : null]}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
          refreshControl={refreshControl}
        >
          {body}
        </ScrollView>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={[styles.safe, style]} edges={["top", "left", "right"]}>
      <View style={[styles.fill, maxWidth ? styles.centered : null]}>{body}</View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.muted,
  },
  scroll: {
    flexGrow: 1,
  },
  scrollCentered: {
    alignItems: "center",
  },
  fill: {
    flex: 1,
  },
  centered: {
    alignSelf: "center",
    width: "100%",
  },
  content: {
    flexGrow: 1,
    paddingHorizontal: spacing.xl,
    paddingBottom: spacing.xxxl,
    paddingTop: spacing.lg,
    gap: spacing.lg,
    width: "100%",
  },
});
