import React from "react";
import { StyleSheet, Text } from "react-native";
import { useRouter } from "expo-router";
import { Button, Card, Screen, SectionHeader } from "@/components/ui";
import { colors, spacing, typography } from "@/theme";

export default function AboutSettingsScreen() {
  const router = useRouter();

  return (
    <Screen>
      <SectionHeader title="About CapTap" subtitle="Tap. Confirm. Peace of mind." />
      <Card style={styles.card}>
        <Text style={styles.body} maxFontSizeMultiplier={1.5}>
          CapTap helps you track daily medications with clear reminders, simple logging, and
          optional NFC bottle tags. It is not a medical provider and does not give dosage advice.
        </Text>
        <Text style={[styles.body, styles.gap]} maxFontSizeMultiplier={1.5}>
          Built for calm, readable screens with large touch targets. Use system text size settings —
          CapTap respects larger fonts on these pages.
        </Text>
      </Card>
      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: { marginBottom: spacing.md },
  body: { ...typography.body, color: colors.ink, lineHeight: 24 },
  gap: { marginTop: spacing.md },
});
