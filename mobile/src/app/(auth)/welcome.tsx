import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { Button, Screen } from "@/components/ui";
import { routes } from "@/constants/routes";
import { colors, spacing, typography } from "@/theme";

export default function WelcomeScreen() {
  const router = useRouter();

  return (
    <Screen scroll={false} contentStyle={styles.content}>
      <View style={styles.hero}>
        <Text style={styles.brand} accessibilityRole="header" maxFontSizeMultiplier={1.3}>
          CapTap
        </Text>
        <Text style={styles.tagline} maxFontSizeMultiplier={1.4}>
          Tap. Confirm. Peace of mind.
        </Text>
        <Text style={styles.support} maxFontSizeMultiplier={1.5}>
          A calm way to know you took today's medication — without overthinking it.
        </Text>
      </View>

      <View style={styles.actions}>
        <Button label="Log in" onPress={() => router.push(routes.login)} />
        <Button
          label="Create account"
          variant="secondary"
          onPress={() => router.push(routes.register)}
        />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: {
    flex: 1,
    justifyContent: "space-between",
    paddingBottom: spacing.xl,
  },
  hero: {
    flexShrink: 1,
    marginTop: spacing.xxl,
    gap: spacing.md,
  },
  brand: {
    fontSize: 52,
    lineHeight: 58,
    fontWeight: "800",
    color: colors.primaryDark,
    letterSpacing: -1,
  },
  tagline: {
    ...typography.subtitle,
    color: colors.ink,
  },
  support: {
    ...typography.bodyMuted,
    maxWidth: 340,
  },
  actions: {
    gap: spacing.md,
  },
});
